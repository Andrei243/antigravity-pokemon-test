using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The commands of plan 14 · B10: <c>pike</c>, a round of the Battle Pike from the desk through its fourteen rooms
/// (<see cref="BattlePikeChallenge"/>), and <c>pyramid</c>, a round of the Battle Pyramid up its seven floors
/// (<see cref="BattlePyramidChallenge"/>). What they share with the Frontier's facilities (the team entered, the copies at
/// level 50, a battle of the Frontier, Battle Points) goes through <c>frontier</c>'s host words, as the Battle Palace's does.
/// </summary>
public sealed partial class ScriptRunner
{
    // What a desk of the annex shares: three that could go in together, the team opened for them, their check
    private void AnnexDesk(Instruction i, int size)
    {
        switch (i.Name)
        {
            case "enough":
                lastNumber = size;
                lastBanned = Listed(FrontierRules.BannedSeen(species => Data.PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = FrontierRules.EnoughToEnter(host.Party, size) ? 1 : 0;
                break;
            case "choose":
                Result = host.FrontierAct("enter", size, Subject);
                afterBusy = () =>
                {
                    venueSlots = host.Entered.ToList();
                    Result = host.Answer == 1 && venueSlots.Count == size ? 1 : 0;
                };
                break;
            case "check":
            {
                var chosen = venueSlots.Where(s => s >= 0 && s < host.Party.Count).Select(s => host.Party.Members[s]).ToList();
                Result = (int)FrontierRules.WhyNot(chosen);
                break;
            }
        }
    }

    // How a Frontier battle the script waited on came out: 1 won, 2 run from, 0 lost
    private int Outcome() => host.Outcome switch { BattleOutcome.Won => 1, BattleOutcome.Fled => 2, _ => 0 };

    /// <summary>
    /// At the desk: <c>enough</c>, <c>choose</c> and <c>check</c> as <c>venue</c>'s; <c>begin</c> starts a round with the
    /// three chosen. Before the doors: <c>active</c>; <c>hint</c> draws what one door hides and is the hint word (0
    /// nostalgia, 1 whispering, 2 Pokémon, 3 people, 4 the Brain; <c>{number}</c> the room to come, 1 to 14);
    /// <c>hintdoor</c> is the door it is of (0 left, 1 middle, 2 right). <c>door 0|1|2</c> chooses a door and draws the
    /// room behind it. In the room: <c>room</c> is what it is (0 a trainer, 1 a full rest, 2 a visitor, 3 a Pokémon's
    /// strange power, 4 a rest for some, 5 a wild Pokémon, 6 a hard trainer, 7 two trainers, 8 the Pike Queen;
    /// <c>{number}</c> the room); <c>opponents</c> stands its trainers in the room (<c>{foe}</c> names the first);
    /// <c>fight</c> is its battle (RESULT 1 won, 2 run from, 0 lost); <c>queen</c> is which of her teams she brings (0
    /// none, 1, 2); <c>status</c> what the strange power gave (the condition's number, 0 nothing; <c>{foe}</c> the
    /// Pokémon, <c>{number}</c> how many) and <c>giver</c> which Pokémon gave it (1 Kirlia, 2 Dusclops); <c>healed</c> how
    /// many a rest healed; <c>lost</c> is 1 when all three have fainted; <c>passed</c> leaves the room (RESULT 1 when it
    /// was the fourteenth). At the end: <c>finish won|lost</c>, <c>points</c>, <c>streak</c>, <c>best</c> and <c>end</c>,
    /// as <c>venue</c>'s.
    /// </summary>
    private void PikeRound(Instruction i)
    {
        var records = host.Frontier;
        BattlePikeChallenge Round() => host.Pike ?? throw Wrong(i, $"'pike {i.Name}' needs a round under way");
        int Draw() => host.Chance.Next(0x10000);
        switch (i.Name)
        {
            case "enough" or "choose" or "check":
                AnnexDesk(i, BattlePikeChallenge.TeamSize);
                break;
            case "begin":
                if (venueSlots.Count != BattlePikeChallenge.TeamSize) throw Wrong(i, "'pike begin' needs the three to have been chosen ('pike choose')");
                host.Pike = BattlePikeChallenge.Begin(records, host.Party, venueSlots);
                records.ChallengesStarted++;
                lastNumber = host.Pike.StreakBefore;
                break;
            case "active":
                Result = host.Pike != null ? 1 : 0;
                break;
            case "hint":
                Result = (int)Round().Hint(records, Draw);
                lastNumber = Round().Next;
                break;
            case "hintdoor":
                Result = Round().HintedDoor % Models.Frontier.PikeRooms.Doors;
                lastNumber = Round().Next;
                break;
            case "door":
                Result = (int)Round().Enter(records, int.Parse(i.Other), Draw);
                lastNumber = Round().Next;
                break;
            case "room":
                Result = Round().Current is { } room ? (int)room : 255;
                lastNumber = Round().Next;
                break;
            case "opponents":
            {
                var round = Round();
                towerFoes = round.Foes.ToList();
                lastFoe = round.Foes.Count > 0 ? $"{round.Foes[0].Record.Class} {round.Foes[0].Record.Name}" : "";
                if (round.Foes.Count > 0) host.FrontierAct("opponents", 0, Subject);
                Result = round.Foes.Count;
                break;
            }
            case "fight":
            {
                var round = Round();
                towerFoes = new List<FrontierOpponent>();
                switch (round.Current)
                {
                    case PikeRoom.WildPokemon when round.Wild is { } wild:
                        host.FrontierBattle(round.Team, new List<Trainer>(), null, doubles: false, new FrontierConditions(Wild: wild));
                        break;
                    case PikeRoom.Brain:
                        host.FrontierBattle(round.Team, new List<Trainer> { round.Queen(records) }, null, doubles: false);
                        break;
                    case PikeRoom.SingleBattle or PikeRoom.HardBattle or PikeRoom.DoubleBattle when round.Foes.Count > 0:
                        host.FrontierBattle(round.Team, round.FoesAsTrainers(), null, doubles: round.Current == PikeRoom.DoubleBattle);
                        break;
                    default:
                        throw Wrong(i, "nobody in this room of the Pike is there to battle");
                }
                afterBusy = () => Result = Outcome();
                break;
            }
            case "queen":
                Result = BattlePikeChallenge.QueenFor(Round().StreakAt(records));
                break;
            case "status":
            {
                var done = Round().Inflicted;
                lastFoe = done?.Giver is { } giver ? Data.LocalNames.Species(giver) : "";
                lastNumber = done?.Given ?? 0;
                Result = done is { Given: > 0 } given ? (int)given.Status : 0;
                break;
            }
            case "giver":
                Result = Round().Inflicted?.Giver switch { "Kirlia" => 1, "Dusclops" => 2, _ => 0 };
                break;
            case "healed":
                Result = lastNumber = Round().Healed;
                break;
            case "lost":
                Result = Round().AllDown ? 1 : 0;
                break;
            case "passed":
                Result = Round().Passed(records) ? 1 : 0;
                lastNumber = Round().Room;
                break;
            case "finish":
                Round().Finish(records, i.Other == "won");
                Result = lastNumber = records.Pike.Streak;
                break;
            case "points":
            {
                int points = Round().PointsEarned;
                if (points > 0)
                {
                    records.GivePoints(points);
                    host.FrontierAct("points", 1, Subject);
                }
                Result = lastNumber = points;
                break;
            }
            case "streak":
                Result = lastNumber = records.Pike.Streak;
                break;
            case "best":
                Result = lastNumber = records.Pike.Best;
                break;
            case "end":
                host.Pike = null;
                venueSlots = Array.Empty<int>();
                towerFoes = new List<FrontierOpponent>();
                break;
        }
    }

    /// <summary>
    /// At the desk: <c>enough</c>, <c>choose</c>, <c>check</c> as <c>venue</c>'s; <c>begin</c> starts a round with the three
    /// chosen and draws its seven floors' seeds. <c>active</c>; <c>enter</c> lays the floor the player is on (a script's
    /// warp to <c>BattlePyramidFloor</c> then goes to it) and <c>arrive</c> puts the player on its way in; <c>floor</c> is
    /// the floor, 1 to 7 (<c>{number}</c>). On the floor: <c>opponent</c> is 1 when whoever the script belongs to is a
    /// trainer of the floor not yet beaten (<c>{foe}</c> names them); <c>fight</c> is their battle, or the Pyramid King's
    /// where the script belongs to nobody of the floor (RESULT 1 won, 0 lost; a trainer beaten looks for nobody again);
    /// <c>wild</c> is the battle of the wild Pokémon met (1 won, 2 run from, 0 lost); <c>light</c> widens the light after
    /// a battle won; <c>find</c> uses the script's own find on the three (<c>{item}</c>, <c>{entrant}</c> who it was used on;
    /// RESULT 1 used, 0 needed by nobody); <c>lost</c> is 1 when all three have fainted; <c>climb</c> takes the stairs
    /// (RESULT 1 once the seventh is climbed, <c>{number}</c> the floor come to); <c>king</c> is which of his teams the
    /// Pyramid King brings to the top (0 none, 1, 2). At the end: <c>finish won|lost</c>, <c>points</c>, <c>streak</c>,
    /// <c>best</c> and <c>end</c>, as <c>venue</c>'s.
    /// </summary>
    private void PyramidRound(Instruction i)
    {
        var records = host.Frontier;
        BattlePyramidChallenge Round() => host.Pyramid ?? throw Wrong(i, $"'pyramid {i.Name}' needs a round under way");
        int Draw() => host.Chance.Next(0x10000);
        switch (i.Name)
        {
            case "enough" or "choose" or "check":
                AnnexDesk(i, BattlePyramidChallenge.TeamSize);
                break;
            case "begin":
                if (venueSlots.Count != BattlePyramidChallenge.TeamSize) throw Wrong(i, "'pyramid begin' needs the three to have been chosen ('pyramid choose')");
                host.Pyramid = BattlePyramidChallenge.Begin(records, host.Party, venueSlots, Draw);
                records.ChallengesStarted++;
                lastNumber = host.Pyramid.StreakBefore;
                break;
            case "active":
                Result = host.Pyramid != null ? 1 : 0;
                break;
            case "enter":
                Round().MakeFloor();
                Result = lastNumber = Round().Floor;
                break;
            case "arrive":
            {
                var (x, y) = Round().Layout?.Entrance ?? throw Wrong(i, "'pyramid arrive' needs a floor laid ('pyramid enter')");
                host.Place(null, x, y, Data.Direction.Up);
                break;
            }
            case "floor":
                Result = lastNumber = Round().Floor;
                break;
            case "opponent":
            {
                var foe = Round().FoeOf(Subject);
                lastFoe = foe != null ? $"{foe.Record.Class} {foe.Record.Name}" : "";
                Result = foe != null && Subject is { HasBattled: false } ? 1 : 0;
                break;
            }
            case "fight":
            {
                var round = Round();
                var person = Subject;
                var foe = round.FoeOf(person);
                var trainer = foe != null ? BattlePyramidChallenge.AsTrainer(foe)
                    : BattlePyramidChallenge.King(Math.Max(1, BattlePyramidChallenge.KingFor(records.Pyramid.Streak)));
                host.FrontierBattle(round.Team, new List<Trainer> { trainer }, null, doubles: false);
                afterBusy = () =>
                {
                    Result = Outcome() == 1 ? 1 : 0;
                    if (Result == 1 && foe != null && person != null) person.HasBattled = true;
                };
                break;
            }
            case "wild":
            {
                var round = Round();
                var wild = round.PendingWild ?? throw Wrong(i, "no wild Pokémon of the Pyramid is waiting for its battle");
                host.FrontierBattle(round.Team, new List<Trainer>(), null, doubles: false, new FrontierConditions(Wild: wild));
                afterBusy = () =>
                {
                    Result = Outcome();
                    round.PendingWild = null;
                };
                break;
            }
            case "light":
                Round().Brighten();
                Result = lastNumber = Round().Light;
                break;
            case "find":
            {
                var (name, _) = ownItem ?? throw Wrong(i, "this script has no find of its own: no hidden item started it");
                lastItem = name;
                var used = Round().UseFind(name);
                lastEntrant = used?.DisplayName ?? "";
                Result = used != null ? 1 : 0;
                break;
            }
            case "lost":
                Result = Round().AllDown ? 1 : 0;
                break;
            case "climb":
                Result = Round().Climb(records) ? 1 : 0;
                lastNumber = Round().Floor;
                break;
            case "king":
                Result = BattlePyramidChallenge.KingFor(records.Pyramid.Streak);
                break;
            case "finish":
                Round().Finish(records, i.Other == "won");
                Result = lastNumber = records.Pyramid.Streak;
                break;
            case "points":
            {
                int points = Round().PointsEarned;
                if (points > 0)
                {
                    records.GivePoints(points);
                    host.FrontierAct("points", 1, Subject);
                }
                Result = lastNumber = points;
                break;
            }
            case "streak":
                Result = lastNumber = records.Pyramid.Streak;
                break;
            case "best":
                Result = lastNumber = records.Pyramid.Best;
                break;
            case "end":
                host.Pyramid = null;
                venueSlots = Array.Empty<int>();
                towerFoes = new List<FrontierOpponent>();
                break;
        }
    }
}
