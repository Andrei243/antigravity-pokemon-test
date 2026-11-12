using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// The command of plan 14 · B9: <c>venue</c>, a round of the Battle Palace or the Battle Arena from the desk to the
/// seventh battle and back (<see cref="BattleVenueChallenge"/>). What they share with the Frontier's facilities (the team
/// entered, the copies at level 50, a battle of the Frontier, Battle Points) goes through <c>frontier</c>'s host words
/// (<see cref="IScriptHost.FrontierAct"/>, <see cref="IScriptHost.FrontierBattle"/>), as the Battle Dome's does.
/// </summary>
public sealed partial class ScriptRunner
{
    // The places in the team of the three chosen at a desk, before a round is begun with them
    private IReadOnlyList<int> venueSlots = Array.Empty<int>();

    /// <summary>
    /// At a desk: <c>enough</c> is 1 when the team holds three that could go in together (<c>{banned}</c> lists the
    /// species of the banlist the Pokédex has seen); <c>choose</c> has the three chosen (RESULT 1, 0 when backed out of)
    /// and <c>check</c> is 0, or 1 for two of a species and 2 for two holding the same item; <c>begin palace|arena</c>
    /// draws the round's seven trainers; <c>streak</c> and <c>best</c> with the venue's name are the battles won in a row
    /// and the most ever (<c>{number}</c>). In the hall: <c>active</c> is 1 while a round is under way; <c>next</c> is the
    /// battle to come (0 to 6; <c>{number}</c> 1 to 7); <c>opponent</c> stands its trainer in the room (<c>{foe}</c>);
    /// <c>fight</c> is fought under the venue's rules (RESULT 1 won, 0 lost); <c>won</c> counts the win (RESULT 1 when it
    /// was the seventh). At the end: <c>finish won|lost</c> writes the record, <c>points</c> hands over the Battle Points a
    /// round won earns (RESULT and <c>{number}</c>), and <c>end</c> lets the round go.
    /// </summary>
    private void VenueRound(Instruction i)
    {
        var records = host.Frontier;
        BattleVenueChallenge Round() => host.Venue ?? throw Wrong(i, $"'venue {i.Name}' needs a round under way");
        AnnexVenue Named() => i.Other == "arena" ? AnnexVenue.Arena : AnnexVenue.Palace;
        int Draw() => host.Chance.Next(0x10000);
        switch (i.Name)
        {
            case "enough":
                lastNumber = BattleVenueChallenge.TeamSize;
                lastBanned = Listed(FrontierRules.BannedSeen(species => Data.PokemonDatabase.Get(species) is { } s && host.FrontierAct("seen", s.DexNumber, Subject) == 1));
                Result = FrontierRules.EnoughToEnter(host.Party, BattleVenueChallenge.TeamSize) ? 1 : 0;
                break;
            case "choose":
                Result = host.FrontierAct("enter", BattleVenueChallenge.TeamSize, Subject);
                afterBusy = () =>
                {
                    venueSlots = host.Entered.ToList();
                    Result = host.Answer == 1 && venueSlots.Count == BattleVenueChallenge.TeamSize ? 1 : 0;
                };
                break;
            case "check":
            {
                var chosen = venueSlots.Where(s => s >= 0 && s < host.Party.Count).Select(s => host.Party.Members[s]).ToList();
                Result = (int)FrontierRules.WhyNot(chosen);
                break;
            }
            case "begin":
                if (venueSlots.Count != BattleVenueChallenge.TeamSize) throw Wrong(i, "'venue begin' needs the three to have been chosen ('venue choose')");
                host.Venue = BattleVenueChallenge.Begin(records, Named(), venueSlots, Draw);
                records.ChallengesStarted++;
                lastNumber = host.Venue.StreakBefore;
                break;
            case "tent":
            {
                // Hoenn's Battle Tents (plan 21 · H3's last part): three trainers, at the highest level of the three
                // entered and never under Emerald's thirty as the region maps it; Slateport's on rentals, with no three chosen
                var tent = i.Other switch { "fallarbor" => BattleTent.Fallarbor, "slateport" => BattleTent.Slateport, _ => BattleTent.Verdanturf };
                if (tent != BattleTent.Slateport && venueSlots.Count != BattleVenueChallenge.TeamSize)
                    throw Wrong(i, "'venue tent' needs the three to have been chosen ('venue choose')");
                host.Venue = BattleVenueChallenge.BeginTent(tent, venueSlots, host.Party, host.LevelHere(BattleVenueChallenge.TentMinimumLevel), Draw);
                lastNumber = host.Venue.FoeLevel;
                break;
            }
            case "rent":
                // Slateport's six on offer, three to be rented (the Factory's screen); RESULT 1 once three are chosen
                Result = host.FrontierAct("rent", BattleVenueChallenge.TeamSize, Subject);
                afterBusy = () =>
                {
                    var round = Round();
                    if (host.Answer == 1 && host.Entered.Count == BattleVenueChallenge.TeamSize) round.Rent(host.Entered);
                    Result = round.Rented.Count == BattleVenueChallenge.TeamSize ? 1 : 0;
                };
                break;
            case "trade":
                // After a win in Slateport: one of the three for one of the trainer just beaten; RESULT 1 when one was made
                Result = host.FrontierAct("trade", 0, Subject);
                afterBusy = () => Result = host.Entered.Count == 2 && Round().Trade(host.Entered[0], host.Entered[1]) ? 1 : 0;
                break;
            case "length":
                Result = lastNumber = Round().Length;
                break;
            case "active":
                Result = host.Venue != null ? 1 : 0;
                break;
            case "next":
                Result = Round().Battle;
                lastNumber = Result + 1;
                break;
            case "opponent":
            {
                var opponent = Round().Opponent ?? throw Wrong(i, "the round is over: nobody is left to meet");
                towerFoes = new List<FrontierOpponent> { opponent };
                lastFoe = $"{opponent.ShownClass} {opponent.ShownName}";
                host.FrontierAct("opponents", 0, Subject);
                Result = 1;
                break;
            }
            case "fight":
            {
                var round = Round();
                var foe = round.OpponentAsTrainer();
                towerFoes = new List<FrontierOpponent>();
                host.FrontierBattle(round.TeamForBattle(host.Party), new List<Trainer> { foe }, null, doubles: false, round.Conditions);
                afterBusy = () => Result = host.Outcome == BattleOutcome.Won ? 1 : 0;
                break;
            }
            case "won":
                Result = Round().Won(records) ? 1 : 0;
                lastNumber = Round().Battle + 1;
                break;
            case "finish":
                Round().Finish(records, i.Other == "won");
                Result = lastNumber = BattleVenueChallenge.RecordOf(records, Round().Venue).Streak;
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
                Result = lastNumber = BattleVenueChallenge.RecordOf(records, Named()).Streak;
                break;
            case "best":
                Result = lastNumber = BattleVenueChallenge.RecordOf(records, Named()).Best;
                break;
            // The Frontier island's nature girl (Emerald's ShowNatureGirlMessage; plan 21's leftovers): how the Pokémon RESULT
            // names (after choosepokemon) would choose in the Palace. RESULT the group it leans to with more than half its HP
            // (0 attacking, 1 looking after itself, 2 working on the foe or the field), {thing} what it does with half or less;
            // 3 for an Egg, 4 with nobody chosen
            case "nature":
            {
                var team = host.Party.Members;
                var p = Result >= 0 && Result < team.Count ? team[Result] : null;
                if (p == null) { Result = 4; break; }
                lastWearer = p.DisplayName;
                if (p.IsEgg) { Result = 3; break; }
                Result = (int)Data.PalaceNatures.Leaning(p.Nature, low: false);
                string lean = Data.PalaceNatures.Leaning(p.Nature, low: true) switch { Data.PalaceGroup.Attack => "attack", Data.PalaceGroup.Defence => "defence", _ => "support" };
                lastThing = Core.Text.Of($"palaceLean.{lean}");
                break;
            }
            case "brain":
                // The venue's Brain (EmeraldBrains): RESULT 1 when the battle to come is theirs
                Result = host.Venue is { BrainNext: true } ? 1 : 0;
                break;
            case "end":
                host.Venue = null;
                venueSlots = Array.Empty<int>();
                towerFoes = new List<FrontierOpponent>();
                break;
        }
    }
}
