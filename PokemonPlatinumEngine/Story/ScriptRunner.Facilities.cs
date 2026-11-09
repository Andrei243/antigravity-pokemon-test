using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 37 · FC2: <c>facility &lt;place&gt; &lt;word&gt;</c>, a challenge of one of the later games' battle
/// places from the desk, a battle at a time, and back (<see cref="StreakChallenge"/>; the place's rooms are
/// <see cref="FacilityPlaces"/>'). What they share with the Frontier (the team entered, the copies, a battle of the
/// Frontier, Battle Points, the records' board, saving) goes through <c>frontier</c>'s host words
/// (<see cref="IScriptHost.FrontierAct"/>, <see cref="IScriptHost.FrontierBattle"/>), as the venues' does. Each later place
/// adds its words here, never a command of its own.
/// </summary>
public sealed partial class ScriptRunner
{
    // The way and the room (normal or hard) chosen at a desk, and the places in the team of those chosen
    private string facilityWay = "";
    private bool facilityHard;
    private IReadOnlyList<int> facilitySlots = Array.Empty<int>();
    private StreakWin facilityWin;
    private FrontierOpponent? facilityPartner;

    /// <summary>The partner of the Multi room the script has stood in the room (<c>facility tree partner</c>), dressed by the engine.</summary>
    public FrontierOpponent? FacilityPartner => facilityPartner;

    /// <summary>
    /// At a desk: <c>way single|double|triple|rotation</c> and <c>hard normal|hard</c> choose the room;
    /// <c>hardopen</c> is 1 once the way's hard room is open; <c>size</c> is how many go in (<c>{number}</c>);
    /// <c>enough</c> is 1 when the team holds that many who could go in together (<c>{banned}</c>); <c>choose</c> has
    /// them chosen (RESULT 1, 0 when backed out of); <c>check</c> is 0, or 1 for two of a species and 2 for two holding
    /// the same item; <c>begin</c> begins the challenge; <c>paused</c> is 1 when a challenge put down waits in any room of
    /// the place (and chooses that room); <c>resume</c> takes it up (RESULT 0 when the team has changed, which ends the
    /// streak); <c>streak</c>, <c>best</c> and <c>latest</c> are the chosen room's (<c>{number}</c>); <c>board</c> opens the
    /// place's records' board. In the battle room: <c>active</c> is 1 while one of the place's challenges is under way;
    /// <c>next</c> is the battle to come (<c>{number}</c>); <c>host</c> is 1 when it is the host's; <c>opponent</c> stands
    /// its trainer in the room (<c>{foe}</c>), or in the Multi room its two (<c>opponent</c> and <c>opponent_2</c>, RESULT 2);
    /// <c>partner</c> stands the Multi room's partner beside the player (<c>{partner}</c>, RESULT 1, 0 in a room with none); <c>battle</c> is fought (RESULT 1 won, 0 lost); <c>won</c> counts the win;
    /// <c>points</c> hands over its Battle Points (<c>{number}</c>); <c>opened</c> is 1 when it opened the hard room;
    /// <c>trophy</c> is 1 when it won the hard room's trophy, which it keeps; <c>pause</c> puts the challenge down to be
    /// saved; <c>finish won|lost</c> ends it and its streak; <c>end</c> lets it go.
    /// </summary>
    private void FacilityCommand(Instruction i)
    {
        string place = i.Name;
        var records = host.Frontier;
        StreakChallenge Challenge() => host.Facility is { } c && c.Room.Place == place ? c : throw Wrong(i, $"'facility {place} {i.Other}' needs a challenge of the {place} under way");
        StreakRoom Chosen() => facilityWay.Length > 0 ? FacilityPlaces.RoomOf(place, facilityWay, facilityHard)
            : throw Wrong(i, $"'facility {place} {i.Other}' needs a room chosen ('facility {place} way ...')");
        FacilityRecord? RecordOfChosen() => records.PeekFacility(place, Chosen().Way, Chosen().Hard);
        int Draw() => host.Chance.Next(0x10000);
        switch (i.Other)
        {
            case "way":
                facilityWay = i.Move;
                facilityHard = false;
                facilitySlots = Array.Empty<int>();
                break;
            case "hard":
                facilityHard = i.Move == "hard";
                break;
            case "hardopen":
                Result = records.PeekFacility(place, Chosen().Way, false)?.HardOpen == true ? 1 : 0;
                break;
            case "size":
                Result = lastNumber = Chosen().Entered;
                break;
            case "enough":
                lastNumber = Chosen().Entered;
                lastBanned = Listed(FrontierRules.BannedSeen(species => Data.PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = FrontierRules.EnoughToEnter(host.Party, Chosen().Entered) ? 1 : 0;
                break;
            case "choose":
            {
                int size = Chosen().Entered;
                Result = host.FrontierAct("enter", size, Subject);
                afterBusy = () =>
                {
                    facilitySlots = host.Entered.ToList();
                    Result = host.Answer == 1 && facilitySlots.Count == size ? 1 : 0;
                };
                break;
            }
            case "check":
            {
                var chosen = facilitySlots.Where(s => s >= 0 && s < host.Party.Count).Select(s => host.Party.Members[s]).ToList();
                Result = (int)FrontierRules.WhyNot(chosen);
                break;
            }
            case "begin":
                if (facilitySlots.Count != Chosen().Entered) throw Wrong(i, $"'facility {place} begin' needs those entered to have been chosen ('facility {place} choose')");
                host.Facility = StreakChallenge.Begin(records, Chosen(), facilitySlots, host.Party, Draw);
                records.ChallengesStarted++;
                Result = lastNumber = host.Facility.StreakBefore;
                break;
            case "paused":
            {
                Result = 0;
                foreach (string way in FacilityPlaces.WaysOf(place))
                    foreach (bool hard in new[] { false, true })
                        if (Result == 0 && StreakChallenge.IsPaused(records, FacilityPlaces.RoomOf(place, way, hard)))
                        {
                            (facilityWay, facilityHard) = (way, hard);
                            Result = 1;
                        }
                break;
            }
            case "resume":
                host.Facility = StreakChallenge.Resume(records, Chosen(), host.Party);
                Result = host.Facility != null ? 1 : 0;
                lastNumber = RecordOfChosen()?.Streak ?? 0;
                break;
            case "streak":
                Result = lastNumber = RecordOfChosen()?.Streak ?? 0;
                break;
            case "best":
                Result = lastNumber = RecordOfChosen()?.Best ?? 0;
                break;
            case "latest":
                Result = lastNumber = RecordOfChosen()?.Latest ?? 0;
                break;
            case "board":
                Result = host.FrontierAct("facilityboard", FacilityPlaces.Index(place), Subject);
                break;
            case "active":
                Result = host.Facility is { } running && running.Room.Place == place ? 1 : 0;
                if (Result == 1) (facilityWay, facilityHard) = (host.Facility!.Room.Way, host.Facility.Room.Hard);
                break;
            case "next":
                Result = lastNumber = Challenge().NextBattle(records);
                break;
            case "host":
                Result = Challenge().HostComes(records) ? 1 : 0;
                break;
            case "opponent":
            {
                var opponents = Challenge().Opponents(records);
                towerFoes = opponents.ToList();
                lastFoe = string.Join(" and ", opponents.Select(o => $"{o.ShownClass} {o.ShownName}"));
                host.FrontierAct("opponents", 0, Subject);
                Result = opponents.Count;
                break;
            }
            case "partner":
            {
                facilityPartner = Challenge().Partner;
                if (facilityPartner != null) host.FrontierAct("facilitypartner", 0, Subject);
                Result = facilityPartner != null ? 1 : 0;
                break;
            }
            case "battle":
            {
                var challenge = Challenge();
                var foes = challenge.OpponentsAsTrainers(records);
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(challenge.TeamForBattle(host.Party), foes, challenge.PartnerAsTrainer(), challenge.Doubles, challenge.Conditions);
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
            {
                var challenge = Challenge();
                string? trophy = challenge.Room.TrophyVariable;
                facilityWin = challenge.Won(records, trophyHeld: trophy != null && host.Story.Var(trophy) > 0);
                Result = facilityWin.Host ? 1 : 0;
                lastNumber = challenge.RecordOf(records).Streak;
                break;
            }
            case "points":
            {
                int points = facilityWin.Points;
                if (points > 0)
                {
                    records.GivePoints(points);
                    host.FrontierAct("points", 1, Subject);
                }
                Result = lastNumber = points;
                facilityWin = facilityWin with { Points = 0 };
                break;
            }
            case "opened":
                Result = facilityWin.OpenedHard ? 1 : 0;
                break;
            case "trophy":
                Result = 0;
                if (facilityWin.Trophy && Challenge().Room.TrophyVariable is { } variable)
                {
                    host.Story.SetVar(variable, 1);
                    Result = 1;
                }
                break;
            case "pause":
                Challenge().Pause(records);
                break;
            case "finish":
                Challenge().Finish(records);
                Result = lastNumber = Challenge().RecordOf(records).Latest;
                break;
            case "end":
                host.Facility = null;
                facilitySlots = Array.Empty<int>();
                facilityPartner = null;
                towerFoes = new List<FrontierOpponent>();
                facilityWin = default;
                break;
        }
    }
}
