using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>The two venues of the Battle Frontier's annex that plan 14 · B9 builds, each a round of seven trainers.</summary>
public enum AnnexVenue { Palace, Arena }

/// <summary>
/// What the Battle Palace and the Battle Arena each keep beside the Frontier's other records (plan 14 · B9;
/// <see cref="FrontierRecords.Palace"/>, <see cref="FrontierRecords.Arena"/>): the battles won in a row and the most
/// ever, the rounds of seven won, and whether a round was walked out on, which ends the streak the next time one is begun.
/// </summary>
public sealed class VenueRecord
{
    /// <summary>The battles won in a row, counting every round's.</summary>
    public int Streak { get; set; }

    /// <summary>The longest run of battles won in a row.</summary>
    public int Best { get; set; }

    /// <summary>Every round of seven won.</summary>
    public int Rounds { get; set; }

    /// <summary>A round has been begun and not finished: one walked out on ends the streak.</summary>
    public bool InProgress { get; set; }
}

/// <summary>
/// A round of the Battle Palace or the Battle Arena (plan 14 · B9; <c>docs/mechanics/rulings.md</c>, "The Battle Palace"
/// and "The Battle Arena"): Emerald's two facilities as venues of the Battle Frontier's annex, on the Frontier's frame as
/// the Battle Dome is. The player enters three Pokémon, which go in as copies at level 50 (<see cref="FrontierRules"/>) for
/// every battle, whole again each time; seven trainers of the Frontier's table are drawn by the Battle Tower's rows for the
/// rounds already won in a row, each with three of their own (<see cref="FrontierTrainers.ForTrainers"/>), and met one
/// after another, three against three. In the Palace every Pokémon chooses by its nature
/// (<see cref="BattleCore.Palace"/>); in the Arena each bout is judged after three turns (<see cref="ArenaJudge"/>).
/// Seven wins take the round; a loss ends it. Every draw is on the sixteen-bit draw the caller hands over. No drawing or input.
/// </summary>
public sealed class BattleVenueChallenge
{
    public const int TeamSize = 3, RoundLength = 7;

    public AnnexVenue Venue { get; }

    /// <summary>The places in the team of the three entered, in the order they were entered.</summary>
    public IReadOnlyList<int> Slots { get; }

    /// <summary>The seven trainers of the round, in the order they are met.</summary>
    public IReadOnlyList<FrontierOpponent> Foes { get; }

    /// <summary>The battle of the round to come (0 to 6), or 7 once all seven are won.</summary>
    public int Battle { get; private set; }

    /// <summary>The battles won in a row before this round.</summary>
    public int StreakBefore { get; }

    private BattleVenueChallenge(AnnexVenue venue, IReadOnlyList<int> slots, IReadOnlyList<FrontierOpponent> foes, int streakBefore)
    {
        Venue = venue;
        Slots = slots;
        Foes = foes;
        StreakBefore = streakBefore;
    }

    /// <summary>The venue's own part of the records.</summary>
    public static VenueRecord RecordOf(FrontierRecords records, AnnexVenue venue) => venue == AnnexVenue.Palace ? records.Palace : records.Arena;

    /// <summary>
    /// A round begun with the three entered (<paramref name="slots"/>): one walked out on ends the streak first, then seven
    /// trainers are drawn (distinct, from the Tower's row of the room the rounds won in a row have come to) and given three each.
    /// </summary>
    public static BattleVenueChallenge Begin(FrontierRecords records, AnnexVenue venue, IReadOnlyList<int> slots, Func<int> next16)
    {
        var record = RecordOf(records, venue);
        if (record.InProgress) record.Streak = 0;
        record.InProgress = true;

        int room = Math.Min(7, record.Streak / RoundLength);
        int[] row = FrontierDatabase.TowerRooms[room];
        var foes = new List<FrontierOpponent>();
        var taken = new HashSet<int>();
        while (foes.Count < RoundLength)
        {
            int trainer = row[0] + (next16() & 0xFFFF) % (row[1] - row[0] + 1);
            if (!taken.Add(trainer)) continue;
            foes.Add(FrontierTrainers.ForTrainers(TeamSize, trainer, trainer, false, next16)[0]);
        }
        return new BattleVenueChallenge(venue, slots.ToList(), foes, record.Streak);
    }

    /// <summary>The trainer of the battle to come, or null once the round is won.</summary>
    public FrontierOpponent? Opponent => Battle < Foes.Count ? Foes[Battle] : null;

    /// <summary>The round is won: all seven beaten.</summary>
    public bool RoundWon => Battle >= RoundLength;

    /// <summary>The team the battle is fought with: copies at level 50 of the three entered, whole, in the order they were entered.</summary>
    public Party TeamForBattle(Party party) => FrontierRules.TeamFor(party, Slots);

    /// <summary>The trainer of the battle to come as a trainer of the battle, with their three as copies.</summary>
    public Trainer OpponentAsTrainer()
    {
        var o = Opponent ?? throw new InvalidOperationException("The round is over: nobody is left to meet.");
        var party = new Party();
        foreach (var p in o.Pokemon) party.Add(FrontierRules.ForBattle(p));
        return new Trainer
        {
            Id = "frontier_" + o.Record.Id, Name = o.Record.Name, TrainerClass = o.Record.Class, PrizeMoney = 0,
            Ai = FrontierTrainers.Mind, Look = FrontierTrainers.LookOf(o.Record), Party = party
        };
    }

    /// <summary>What the venue's rules hand the battle: the Palace's choosing by nature, or the Arena's three-turn bouts.</summary>
    public FrontierConditions Conditions => Venue == AnnexVenue.Palace ? new FrontierConditions(Palace: true) : new FrontierConditions(TurnLimit: ArenaJudge.Turns);

    /// <summary>The battle was won: the streak lengthens, and the next trainer is the one to come. True when it was the seventh.</summary>
    public bool Won(FrontierRecords records)
    {
        var record = RecordOf(records, Venue);
        record.Streak = Math.Min(FrontierRecords.Most, record.Streak + 1);
        record.Best = Math.Max(record.Best, record.Streak);
        Battle++;
        return RoundWon;
    }

    /// <summary>
    /// The round is over (won, or lost and left): the record is written. A round won counts; a loss ends the streak.
    /// Either way the round is no longer in progress.
    /// </summary>
    public void Finish(FrontierRecords records, bool won)
    {
        var record = RecordOf(records, Venue);
        record.InProgress = false;
        if (!won)
        {
            record.Streak = 0;
            return;
        }
        record.Rounds = Math.Min(FrontierRecords.Most, record.Rounds + 1);
    }

    /// <summary>The Battle Points a round won earns: the Dome's rule of ours, by the rounds won in a row before it.</summary>
    public int PointsEarned => RoundWon ? BattleDomeChallenge.PointsFor(StreakBefore / RoundLength) : 0;
}
