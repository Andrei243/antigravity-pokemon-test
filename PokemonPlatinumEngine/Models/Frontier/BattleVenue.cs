using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Facilities;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// The two venues of the Battle Frontier's annex that plan 14 · B9 builds, each a round of seven trainers, and Kiloude City's
/// Battle Maison (plan 23, Kalos's last places), whose rounds of seven are plain single battles against the later sets.
/// </summary>
public enum AnnexVenue { Palace, Arena, Maison }

/// <summary>
/// Hoenn's three Battle Tents (plan 21 · H3's last part; <c>docs/mechanics/rulings.md</c>, "Hoenn's Battle Tents"):
/// Emerald's small rounds of three on the rules of a facility. Verdanturf's is the Palace's, Fallarbor's the Arena's,
/// Slateport's the Factory's, on rentals.
/// </summary>
public enum BattleTent { Verdanturf, Fallarbor, Slateport }

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

    // ---- Hoenn's Battle Tents (plan 21 · H3's last part) ----

    /// <summary>A Battle Tent's round (Emerald's <c>TENT_STAGES_PER_CHALLENGE</c>): three trainers.</summary>
    public const int TentLength = 3;

    /// <summary>The lowest level a tent's trainers are met at (Emerald's <c>TENT_MIN_LEVEL</c>), before a region's levels map it.</summary>
    public const int TentMinimumLevel = 30;

    /// <summary>The prize a round won in each tent hands over (Emerald's <c>sVerdanturfTentRewards</c> and its two fellows).</summary>
    public static string TentPrize(BattleTent tent) => tent switch
    {
        BattleTent.Verdanturf => "Nest Ball",
        BattleTent.Fallarbor => "Hyper Potion",
        _ => "Full Heal"
    };

    /// <summary>The tent this round is played in, or null for a round of the annex.</summary>
    public BattleTent? Tent { get; private init; }

    /// <summary>How many battles the round is: seven in the annex, three in a tent.</summary>
    public int Length => Tent == null ? RoundLength : TentLength;

    /// <summary>The level the round's trainers' Pokémon are made at.</summary>
    public int FoeLevel { get; private init; } = FrontierRules.Level;

    /// <summary>Slateport's six rentals on offer at the start of the round (none in any other round).</summary>
    public List<Pokemon> Offer { get; } = new();

    /// <summary>Slateport's three rented, as the Factory lends them; the player's own stay out of it.</summary>
    public List<Pokemon> Rented { get; } = new();

    /// <summary>
    /// A tent's round begun (Emerald's <c>InitVerdanturfTentChallenge</c> and its fellows, <c>SetTentPtrsGetLevel</c>):
    /// three trainers drawn from the Frontier's first row, met at the highest level of the three entered and never under
    /// <paramref name="minimumLevel"/> (Emerald's thirty, mapped by the region's levels); Slateport's rentals and trainers
    /// at the minimum alone, with six offered to rent from. The player's own go in as copies at their own levels. A tent
    /// keeps no streak on any board.
    /// </summary>
    public static BattleVenueChallenge BeginTent(BattleTent tent, IReadOnlyList<int> slots, Party party, int minimumLevel, Func<int> next16)
    {
        int level = minimumLevel;
        if (tent != BattleTent.Slateport)
            foreach (int slot in slots)
                if (slot >= 0 && slot < party.Count) level = Math.Max(level, party.Members[slot].Level);
        int[] row = FrontierDatabase.TowerRooms[0];
        var foes = new List<FrontierOpponent>();
        var taken = new HashSet<int>();
        while (foes.Count < TentLength)
        {
            int trainer = row[0] + (next16() & 0xFFFF) % (row[1] - row[0] + 1);
            if (!taken.Add(trainer)) continue;
            foes.Add(FrontierTrainers.ForTrainers(TeamSize, trainer, trainer, false, next16, level)[0]);
        }
        var venue = tent == BattleTent.Fallarbor ? AnnexVenue.Arena : AnnexVenue.Palace;
        var round = new BattleVenueChallenge(venue, tent == BattleTent.Slateport ? Array.Empty<int>() : slots.ToList(), foes, 0)
        {
            Tent = tent, FoeLevel = level
        };
        if (tent == BattleTent.Slateport)
        {
            var pool = FrontierDatabase.Factory.Level50[0];
            foreach (var (set, ivs) in BattleFactoryChallenge.SelectFromPool(Array.Empty<FactoryRental>(), 6, pool, null, 0, next16))
            {
                uint otId = ((uint)next16() & 0xFFFF) | ((uint)next16() & 0xFFFF) << 16;
                var record = FrontierDatabase.Set(set);
                round.Offer.Add(LevelScaling.At(FrontierTrainers.Build(record, FrontierTrainers.PersonalityFor(record.Nature, otId, next16), ivs, level), level));
            }
        }
        return round;
    }

    /// <summary>Slateport's three rented, by their places among the six offered.</summary>
    public void Rent(IReadOnlyList<int> chosen)
    {
        Rented.Clear();
        foreach (int place in chosen)
            if (place >= 0 && place < Offer.Count) Rented.Add(Offer[place]);
    }

    /// <summary>The trainer just beaten, whose three may be traded for after a win in Slateport.</summary>
    public FrontierOpponent? Beaten => Battle > 0 && Battle <= Foes.Count ? Foes[Battle - 1] : null;

    /// <summary>
    /// A trade after a win in Slateport (Emerald's <c>SwapRentalMons</c>): the rental at <paramref name="mine"/> goes, the beaten
    /// trainer's Pokémon at <paramref name="theirs"/> comes over as it was. False where either place is not there.
    /// </summary>
    public bool Trade(int mine, int theirs)
    {
        if (Beaten is not { } beaten || mine < 0 || mine >= Rented.Count || theirs < 0 || theirs >= beaten.Pokemon.Count) return false;
        Rented[mine] = beaten.Pokemon[theirs].Clone();
        return true;
    }

    /// <summary>The venue's own part of the records.</summary>
    public static VenueRecord RecordOf(FrontierRecords records, AnnexVenue venue) => venue switch
    {
        AnnexVenue.Palace => records.Palace,
        AnnexVenue.Maison => records.Maison,
        _ => records.Arena
    };

    // ---- Kiloude City's Battle Maison (plan 23, Kalos's last places; plan 37's streak house on the venues' frame) ----

    /// <summary>
    /// The tiers of the later sets (<see cref="FacilitySetDatabase"/>, 1 the weakest, 4 the strongest) a round of the Maison
    /// draws from, by the rounds won in a row before it: a table of ours, the stronger the longer the streak.
    /// </summary>
    public static (int Lowest, int Highest) MaisonTiers(int rounds) => rounds switch
    {
        0 => (1, 2),
        1 => (1, 3),
        2 => (2, 3),
        3 => (2, 4),
        _ => (3, 4)
    };

    /// <summary>
    /// A round of the Maison begun with the three entered: one walked out on ends the streak first, then seven trainers of
    /// the later places (<see cref="FacilityTrainers.Draw"/>) are drawn, each with three of the later sets of the round's
    /// tiers at level 50 and IVs rising with the rounds won (<see cref="FacilityTrainers.IvsFor"/>), no two trainers alike.
    /// Every draw is on the sixteen-bit draw handed over.
    /// </summary>
    public static BattleVenueChallenge BeginMaison(FrontierRecords records, IReadOnlyList<int> slots, Func<int> next16)
    {
        var record = records.Maison;
        if (record.InProgress) record.Streak = 0;
        record.InProgress = true;
        int rounds = record.Streak / RoundLength;
        var (low, high) = MaisonTiers(rounds);
        var foes = new List<FrontierOpponent>();
        var taken = new HashSet<string>(StringComparer.Ordinal);
        while (foes.Count < RoundLength)
        {
            var drawn = FacilityTrainers.Draw(TeamSize, low, high, FacilityTrainers.IvsFor(rounds), next16);
            if (!taken.Add(drawn.Id)) continue;
            foes.Add(new FrontierOpponent
            {
                Number = 0, Pokemon = drawn.Pokemon, OtId = drawn.OtId, Personalities = drawn.Personalities,
                Dress = new FrontierDress(drawn.Id, drawn.Class, drawn.Name, drawn.Look)
            });
        }
        return new BattleVenueChallenge(AnnexVenue.Maison, slots.ToList(), foes, record.Streak);
    }

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
    public bool RoundWon => Battle >= Length;

    /// <summary>
    /// The team the battle is fought with: copies at level 50 of the three entered, whole, in the order they were entered;
    /// in a tent copies at their own levels, or Slateport's three rentals.
    /// </summary>
    public Party TeamForBattle(Party party)
    {
        if (Tent == BattleTent.Slateport)
        {
            var team = new Party();
            foreach (var rental in Rented) team.Add(FrontierRules.ForBattle(rental, FoeLevel));
            return team;
        }
        return FrontierRules.TeamFor(party, Slots, Tent == null ? FrontierRules.Level : int.MaxValue);
    }

    /// <summary>The trainer of the battle to come as a trainer of the battle, with their three as copies.</summary>
    public Trainer OpponentAsTrainer()
    {
        var o = Opponent ?? throw new InvalidOperationException("The round is over: nobody is left to meet.");
        var party = new Party();
        foreach (var p in o.Pokemon) party.Add(FrontierRules.ForBattle(p));
        return new Trainer
        {
            Id = o.Dress?.Id ?? "frontier_" + o.Record.Id, Name = o.ShownName, TrainerClass = o.ShownClass, PrizeMoney = 0,
            Ai = FrontierTrainers.Mind, Look = o.ShownLook, Party = party
        };
    }

    /// <summary>What the venue's rules hand the battle: the Palace's choosing by nature, or the Arena's three-turn bouts.</summary>
    public FrontierConditions Conditions => Tent == BattleTent.Slateport ? new FrontierConditions()
        : Venue switch
        {
            AnnexVenue.Palace => new FrontierConditions(Palace: true),
            AnnexVenue.Arena => new FrontierConditions(TurnLimit: ArenaJudge.Turns),
            _ => new FrontierConditions()
        };

    /// <summary>The battle was won: the streak lengthens, and the next trainer is the one to come. True when it was the seventh (a tent's third).</summary>
    public bool Won(FrontierRecords records)
    {
        if (Tent != null)
        {
            Battle++;
            return RoundWon;
        }
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
        if (Tent != null) return;
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
    public int PointsEarned => RoundWon && Tent == null ?BattleDomeChallenge.PointsFor(StreakBefore / RoundLength) : 0;
}
