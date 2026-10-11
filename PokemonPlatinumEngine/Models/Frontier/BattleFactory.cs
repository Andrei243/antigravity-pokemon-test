using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>
/// A rental Pokémon of the Battle Factory as the Factory keeps it (the three numbers its save holds for each:
/// <c>FACTORY_SAVE_PLAYER_SETS</c>, <c>…_IVS</c>, <c>…_PERSONALITY</c>), with the number it carries as its first
/// trainer's. The same four build the same Pokémon however often it is made (<see cref="BattleFactoryChallenge.Build"/>).
/// </summary>
public sealed class FactoryRental
{
    /// <summary>Its set among the Frontier's (<see cref="FrontierDatabase.Set"/>).</summary>
    public int Set { get; set; }

    /// <summary>The IV of all six of its stats: its row's of the Factory's tables.</summary>
    public int Ivs { get; set; }
    public uint Personality { get; set; }
    public uint OtId { get; set; }

    [JsonIgnore] public FrontierSetRecord Record => FrontierDatabase.Set(Set);
    [JsonIgnore] public string Species => Record.Species;
    [JsonIgnore] public string? Item => Record.Item;

    public FactoryRental Copy() => new() { Set = Set, Ivs = Ivs, Personality = Personality, OtId = OtId };
}

/// <summary>A Battle Factory challenge put down between two battles (what <c>BattleFactory_Save</c> writes): who is still to come, the three in hand, and the three just beaten, one of which may yet be traded for.</summary>
public sealed class FactoryPause
{
    public FrontierChallenge Way { get; set; }
    public bool OpenLevel { get; set; }
    public int Battle { get; set; }
    public List<int> Trainers { get; set; } = new();
    public List<FactoryRental> Team { get; set; } = new();
    public List<FactoryRental> Foe { get; set; } = new();
}

/// <summary>What the Battle Factory keeps beside the stats (the original's <c>BattleFactorySave</c> and the game record of its trades).</summary>
public sealed class FactoryRecord
{
    /// <summary>A challenge saved between two battles, to be taken up again; null when none is.</summary>
    public FactoryPause? Paused { get; set; }

    /// <summary><c>RECORD_BATTLE_FACTORY_TRADES</c>: every Pokémon swapped for a beaten trainer's, whatever the challenge.</summary>
    public int Trades { get; set; }
}

/// <summary>What the staff tell of the trainer to come (the corridor's scene, by the round).</summary>
public enum FactoryHint
{
    /// <summary>The first round: all three of the trainer's Pokémon.</summary>
    ThreeSpecies,
    /// <summary>The second: the first two.</summary>
    TwoSpecies,
    /// <summary>The third: the first, and the first of its moves.</summary>
    SpeciesAndMove,
    /// <summary>The fourth: the first move of the first, and no more.</summary>
    Move,
    /// <summary>From the fifth: the type most of the three share.</summary>
    CommonType,
    /// <summary>From the fifth, when no type is shared by two.</summary>
    VariedTypes
}

/// <summary>
/// A Battle Factory challenge under way (plan 06 · R18b): the original's <c>BattleFactory</c> and the functions that
/// work on it (<c>src/overlay104/battle_factory.c</c>, <c>battle_factory_helpers.c</c>, <c>frscrcmd_battle_factory.c</c>).
/// Nothing of the player's own goes in. Six rental Pokémon are offered, three are taken, and seven trainers are met
/// one after another with them; after each win one of the three may be swapped for one of the beaten trainer's. A
/// round of seven won goes on to the next, where the rentals and the trainers are stronger, and the trades a streak
/// has made lift some of the six offered into the next row of the table. The Factory Head stands in the way at
/// battles 21 and 49 of a Single challenge.
///
/// The original draws on the game's one generator (<c>LCRNG_Next</c>), so every draw here is on the sixteen-bit draw
/// the caller hands over, in the original's order; what was drawn is kept (the trainers, each rental's set, IVs and
/// personality), which is what a challenge put down is taken up again from. No drawing or input.
/// </summary>
public sealed class BattleFactoryChallenge
{
    /// <summary><c>FACTORY_BATTLES_PER_ROUND</c>, <c>FACTORY_INITIAL_RENTAL_OPTIONS</c>, <c>FACTORY_PARTY_SIZE_SOLO</c>, <c>FACTORY_MAX_DISTINCT_ROUNDS</c>.</summary>
    public const int BattlesPerRound = 7, Offered = 6, TeamSize = 3, DistinctRounds = 8;

    /// <summary>The trades a streak must have passed for one more of the six offered to come from the next row (<c>sTradeCountsNeededForUpgrades</c>).</summary>
    public static readonly int[] TradesForUpgrades = { 6, 13, 20, 27, 34 };

    /// <summary>The Battle Points a round earns by the round the streak goes on to (<c>sBPPerRoundSolo</c>), and for beating the Factory Head.</summary>
    public static readonly int[] PointsByRound = { 0, 5, 5, 5, 5, 7, 7, 8, 9 };
    public const int ThortonPoints = 20;

    /// <summary>What a round won adds to the Trainer Card's score (<c>TRAINER_SCORE_EVENT_BATTLE_FACTORY_ROUND_COMPLETED</c>).</summary>
    public const int RoundScore = 7;

    /// <summary>The story's own variables the lobby keeps the challenge's kind in, as the original's scripts do.</summary>
    public const string WayVariable = "VAR_BATTLE_FACTORY_CHALLENGE_TYPE", LevelVariable = "VAR_BATTLE_FACTORY_CHALLENGE_LEVEL";

    /// <summary>What a rental's summary names as its trainer and as where it was met (<c>SetRentalPokemonSummaryInfo</c>).</summary>
    public const string RentalTrainer = "???", MetAt = "Battle Factory";

    public FrontierChallenge Way { get; }
    public bool OpenLevel { get; }

    /// <summary><c>BattleFactory_GetPokemonLevel</c>: every Pokémon of a challenge is of the one level.</summary>
    public int Level => OpenLevel ? 100 : 50;

    /// <summary>The battles won in this round, 0 to 7 (<c>currentBattle</c>).</summary>
    public int Battle { get; private set; }

    /// <summary>The streak as it stands (<c>currentStreak</c>) and the trades made in it (<c>tradeCount</c>: a round's first three count as one).</summary>
    public int Streak { get; private set; }
    public int Trades { get; private set; }

    /// <summary>The rounds the streak has come through (<c>currentRound</c>).</summary>
    public int Round { get; private set; }

    /// <summary>The round's trainers by their numbers: fourteen, as the original draws them, of whom the first seven are met.</summary>
    public List<int> Trainers { get; private set; } = new();

    /// <summary>The six offered as a round begins (<c>initialRentalMons</c>).</summary>
    public List<FactoryRental> Offer { get; private set; } = new();

    /// <summary>The three in hand (<c>playersParty</c>), in the order they go into battle.</summary>
    public List<FactoryRental> Team { get; private set; } = new();

    /// <summary>The trainer's three as they stand in the battle room (<c>opponentsParty</c>): once beaten, what may be traded for.</summary>
    public List<FactoryRental> Foe { get; private set; } = new();

    /// <summary>The three of the trainer to come (<c>opponentMons</c>): what the staff's hint tells of.</summary>
    public List<FactoryRental> NextFoe { get; private set; } = new();

    /// <summary>The challenge was taken up again after a rest: the menu between battles comes first (<c>VAR_0x8003</c>).</summary>
    public bool Resumed { get; private set; }

    /// <summary>The Factory Head has been announced (<c>seenFactoryHeadIntro</c>).</summary>
    public bool HeadAnnounced { get; private set; }

    /// <summary>The Factory Head was beaten in this challenge: for the silver print, or for the gold one.</summary>
    public FrontierPrint BeatThorton { get; private set; }

    /// <summary>The round was won through (<c>saveStreak</c>): the streak runs on.</summary>
    public bool RoundWon { get; private set; }

    private BattleFactoryChallenge(FrontierChallenge way, bool openLevel)
    {
        Way = way;
        OpenLevel = openLevel;
    }

    /// <summary>The facility a level's streaks are kept under: the Factory counts twice.</summary>
    public static FrontierFacility FacilityOf(bool openLevel) => openLevel ? FrontierFacility.FactoryOpen : FrontierFacility.Factory;

    // BattleFrontierStats_GetFactoryRecordStreakIdx and its three fellows: four numbers for each way and level
    private static FrontierStat Stat(FrontierChallenge way, bool openLevel, int which) =>
        (FrontierStat)((int)FrontierStat.FactoryRecordStreak50Single + (int)way * 8 + (openLevel ? 4 : 0) + which);

    public static FrontierStat RecordStreakStat(FrontierChallenge way, bool openLevel) => Stat(way, openLevel, 0);
    public static FrontierStat LatestStreakStat(FrontierChallenge way, bool openLevel) => Stat(way, openLevel, 1);
    public static FrontierStat RecordTradesStat(FrontierChallenge way, bool openLevel) => Stat(way, openLevel, 2);
    public static FrontierStat LatestTradesStat(FrontierChallenge way, bool openLevel) => Stat(way, openLevel, 3);

    // ------------------------------------------------------------------ beginning

    /// <summary>
    /// <c>BattleFactory_Init</c> for a new challenge: no battle won yet, and where a streak of this way and level is
    /// still running, its wins and its trades. Whatever challenge was put down is gone (<c>BattleFactorySave_Init</c>).
    /// </summary>
    public static BattleFactoryChallenge Begin(FrontierRecords records, FrontierChallenge way, bool openLevel)
    {
        if (way == FrontierChallenge.Multi) throw new InvalidOperationException("The Battle Factory's Multi challenge needs another player");
        var challenge = new BattleFactoryChallenge(way, openLevel);
        records.Factory.Paused = null;
        if (records.IsRunning(FacilityOf(openLevel), (int)way))
        {
            challenge.Streak = records.Get(LatestStreakStat(way, openLevel));
            challenge.Trades = records.Get(LatestTradesStat(way, openLevel));
        }
        challenge.Round = challenge.Streak / BattlesPerRound;
        return challenge;
    }

    /// <summary>
    /// <c>BattleFactory_Init</c> and <c>LoadTrainersAndMonsFromSave</c> for a challenge taken up again: as it was
    /// put down (<see cref="Pause"/>), its streak and trades the latest kept; null when none was.
    /// </summary>
    public static BattleFactoryChallenge? Resume(FrontierRecords records)
    {
        if (records.Factory.Paused is not { } paused) return null;
        var challenge = new BattleFactoryChallenge(paused.Way, paused.OpenLevel)
        {
            Battle = paused.Battle,
            Streak = records.Get(LatestStreakStat(paused.Way, paused.OpenLevel)),
            Trades = records.Get(LatestTradesStat(paused.Way, paused.OpenLevel)),
            Trainers = paused.Trainers.ToList(),
            Team = paused.Team.Select(r => r.Copy()).ToList(),
            Foe = paused.Foe.Select(r => r.Copy()).ToList(),
            Resumed = true
        };
        challenge.Round = challenge.Streak / BattlesPerRound;
        records.Factory.Paused = null;
        return challenge;
    }

    /// <summary>
    /// <c>SetupTrainersAndInitialRentalOptions</c>, as a round begins: its fourteen trainers, the six rentals on
    /// offer (the last two each swapped with one of the six, so that a lifted one may stand anywhere), and the
    /// first trainer's three, which share neither a species nor an item with any of the six.
    /// </summary>
    public void DrawRound(Func<int> next16)
    {
        Trainers = PickTrainers(Way, Round, BattlesPerRound * 2, next16);
        Offer = Load(SelectFromPool(Array.Empty<FactoryRental>(), Offered, RentalPool(Round, OpenLevel, out var lifted), lifted, Trades, next16), next16);
        SwapOffered(4, next16);
        SwapOffered(5, next16);
        NextFoe = Load(SelectFromPool(Offer, TeamSize, FoePool(Trainers[Battle], OpenLevel, out _), null, 0, next16), next16);
        Team = new List<FactoryRental>();
        Foe = new List<FactoryRental>();
    }

    // SwapMonIndices
    private void SwapOffered(int at, Func<int> next16)
    {
        int other = (next16() & 0xFFFF) % Offered;
        (Offer[at], Offer[other]) = (Offer[other], Offer[at]);
    }

    // ------------------------------------------------------------------ the round's trainers

    /// <summary>
    /// <c>BattleFactory_GetOpponentTrainerID</c>: who stands at a place of a round. In a Single challenge the battle
    /// that would be the streak's 21st is the Factory Head's and the 49th his again; otherwise the trainer is drawn
    /// from the round's row, the seventh of each seven from its second half. The row's last trainer is never drawn
    /// (the original takes the remainder by the row's span, not by its count).
    /// </summary>
    public static int TrainerFor(FrontierChallenge way, int round, int place, Func<int> next16)
    {
        var rows = FrontierDatabase.Factory.Opponents;
        round = Math.Min(round, rows.Count - 1);
        if (way == FrontierChallenge.Single)
        {
            var (silverAt, goldAt) = FrontierBrains.Battles(FrontierFacility.Factory);
            var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Factory);
            int streak = round * BattlesPerRound + place + 1;
            if (streak == silverAt) return silver;
            if (streak == goldAt) return gold;
        }
        int[] row = rows[round];
        bool last = place == BattlesPerRound - 1 || place == BattlesPerRound * 2 - 1;
        int min = last ? row[2] : row[0], span = last ? row[3] - row[2] : row[1] - row[0];
        return min + (next16() & 0xFFFF) % span;
    }

    /// <summary><c>BattleFactory_PickOpponentTrainers</c>: a round's trainers, none twice.</summary>
    public static List<int> PickTrainers(FrontierChallenge way, int round, int count, Func<int> next16)
    {
        var trainers = new List<int>();
        while (trainers.Count < count)
        {
            int trainer = TrainerFor(way, round, trainers.Count, next16);
            if (!trainers.Contains(trainer)) trainers.Add(trainer);
        }
        return trainers;
    }

    // ------------------------------------------------------------------ the tables' rows

    /// <summary>
    /// <c>GetPlayerPoolForRound</c>: the row the six on offer are drawn from, by the round (the eighth's from then
    /// on), and the row after it, which a streak's trades lift some of them into.
    /// </summary>
    public static FactoryPoolRecord RentalPool(int round, bool openLevel, out FactoryPoolRecord? lifted)
    {
        var rows = openLevel ? FrontierDatabase.Factory.OpenRentals : FrontierDatabase.Factory.Level50;
        round = Math.Min(round, DistinctRounds - 1);
        lifted = round + 1 < rows.Count ? rows[round + 1] : null;
        return rows[round];
    }

    /// <summary><c>BattleFactory_GetPokemonPoolForTrainer</c>: the row a trainer's three are drawn from, the first whose bound the trainer's number is under.</summary>
    public static FactoryPoolRecord FoePool(int trainer, bool openLevel, out int row)
    {
        var rows = openLevel ? FrontierDatabase.Factory.OpenFoes : FrontierDatabase.Factory.Level50;
        row = rows.ToList().FindIndex(r => trainer < r.TrainerMax);
        if (row < 0) row = rows.Count - 1;
        return rows[row];
    }

    /// <summary>How many of the six offered come from the next row, by the trades a streak has made: none up to six, then one more for every seven, five at most.</summary>
    public static int Lifted(int trades)
    {
        if (trades == 0) return 0;
        for (int i = 0; i < TradesForUpgrades.Length; i++)
            if (trades <= TradesForUpgrades[i]) return i;
        return TradesForUpgrades.Length;
    }

    /// <summary>
    /// <c>BattleFactory_SelectPokemonFromPool</c>: draws <paramref name="count"/> sets of a row, the last few from
    /// the row after it when the trades have earned that and the row can be lifted. One of a species or holding an
    /// item already drawn is drawn again, and so is one that shares either with <paramref name="taken"/>.
    /// </summary>
    public static List<(int Set, int Ivs)> SelectFromPool(IReadOnlyList<FactoryRental> taken, int count, FactoryPoolRecord pool, FactoryPoolRecord? lifted,
        int trades, Func<int> next16)
    {
        int upgradable = Lifted(trades);
        var chosen = new List<(int Set, int Ivs)>();
        while (chosen.Count < count)
        {
            var from = chosen.Count >= count - upgradable && pool.Upgrades && lifted != null ? lifted : pool;
            int number = from.SetMax - (next16() & 0xFFFF) % (from.SetMax - from.SetMin + 1);
            var set = FrontierDatabase.Set(number);
            // The original compares the items as they are, so two sets holding nothing count as holding the same
            if (chosen.Select(c => FrontierDatabase.Set(c.Set)).Any(c => c.Species == set.Species || c.Item == set.Item)) continue;
            if (taken.Any(t => t.Species == set.Species || t.Item == set.Item)) continue;
            chosen.Add((number, from.Ivs));
        }
        return chosen;
    }

    // BattleFrontier_LoadFrontierPokemon: for each, the number it carries (two draws) and a personality that gives its nature
    private static List<FactoryRental> Load(List<(int Set, int Ivs)> sets, Func<int> next16)
    {
        var rentals = new List<FactoryRental>();
        foreach (var (number, ivs) in sets)
        {
            uint otId = ((uint)next16() & 0xFFFF) | ((uint)next16() & 0xFFFF) << 16;
            rentals.Add(new FactoryRental { Set = number, Ivs = ivs, OtId = otId, Personality = FrontierTrainers.PersonalityFor(FrontierDatabase.Set(number).Nature, otId, next16) });
        }
        return rentals;
    }

    // ------------------------------------------------------------------ the Pokémon

    /// <summary>
    /// A rental as a Pokémon (<c>FrontierPokemon_InitPokemon</c>, <c>SetRentalPokemonSummaryInfo</c>): its set at the
    /// challenge's level with the IVs of its row, in a Poké Ball, met at the Battle Factory, its first trainer nobody
    /// one knows. Made afresh it is whole, which is how the Factory heals between battles.
    /// </summary>
    public Pokemon Build(FactoryRental rental)
    {
        var pokemon = FrontierTrainers.Build(rental.Record, rental.Personality, rental.Ivs, Level);
        pokemon.Ball = "Poké Ball";
        pokemon.OriginalTrainer = new TrainerMark(RentalTrainer, (int)(rental.OtId & 0xFFFF), Core.PlayerLook.Boy);
        pokemon.MetLocation = MetAt;
        pokemon.MetLevel = Level;
        return pokemon;
    }

    /// <summary>The three in hand as a team for a battle, whole (<c>Party_HealAllMembers</c>).</summary>
    public Party TeamForBattle()
    {
        var party = new Party();
        foreach (var rental in Team) party.Add(Build(rental));
        return party;
    }

    /// <summary>
    /// The trainer of the battle about to be fought with their three (<c>BattleFactory_SetupBattle</c>), thinking as
    /// the round has them think (<see cref="Mind"/>).
    /// </summary>
    public FrontierOpponent Opponent() => new()
    {
        Number = Trainers[Battle],
        Sets = Foe.Select(r => r.Set).ToList(),
        Pokemon = Foe.Select(Build).ToList(),
        OtId = Foe.FirstOrDefault()?.OtId ?? 0,
        Personalities = Foe.Select(r => r.Personality).ToList()
    };

    /// <summary>
    /// <c>BattleFactory_GetAIMask</c>: the trainers of the first two rounds choose their moves at random, those of
    /// the third and fourth with the basic routine alone, and everyone after, like the Factory Head whenever he
    /// comes, with all three.
    /// </summary>
    public AiFlags Mind
    {
        get
        {
            var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Factory);
            if (Way == FrontierChallenge.Single && Battle < Trainers.Count && (Trainers[Battle] == silver || Trainers[Battle] == gold)) return FrontierTrainers.Mind;
            return Round switch
            {
                0 or 1 => AiFlags.None,
                2 or 3 => AiFlags.Basic,
                _ => FrontierTrainers.Mind
            };
        }
    }

    // ------------------------------------------------------------------ what the staff tell

    /// <summary>
    /// What is told of the trainer to come (<c>frontier_scripts_battle_factory.s</c>, by the round): its Pokémon's
    /// names, a move, or the type most of the three have. A type counts once for a Pokémon; the first in the
    /// original's order of types wins a tie, and a type only one of them has is none (<c>BF_FUNC_GET_COMMON_TYPE</c>).
    /// </summary>
    public (FactoryHint Kind, IReadOnlyList<string> Names) Hint()
    {
        string Species(int i) => NextFoe[i].Species;
        string Move(int i) => NextFoe[i].Record.Moves.FirstOrDefault() ?? "";
        switch (Round)
        {
            case 0: return (FactoryHint.ThreeSpecies, new[] { Species(0), Species(1), Species(2) });
            case 1: return (FactoryHint.TwoSpecies, new[] { Species(0), Species(1) });
            case 2: return (FactoryHint.SpeciesAndMove, new[] { Species(0), Move(0) });
            case 3: return (FactoryHint.Move, new[] { Move(0) });
        }
        return CommonType(NextFoe) is { } type ? (FactoryHint.CommonType, new[] { type.ToString() }) : (FactoryHint.VariedTypes, Array.Empty<string>());
    }

    /// <summary>The original's own numbering of the types (<c>generated/pokemon_types.txt</c>, less its "mystery" type): a tie goes to the first.</summary>
    private static readonly PokemonType[] TypeOrder =
    {
        PokemonType.Normal, PokemonType.Fighting, PokemonType.Flying, PokemonType.Poison, PokemonType.Ground, PokemonType.Rock,
        PokemonType.Bug, PokemonType.Ghost, PokemonType.Steel, PokemonType.Fire, PokemonType.Water,
        PokemonType.Grass, PokemonType.Electric, PokemonType.Psychic, PokemonType.Ice, PokemonType.Dragon, PokemonType.Dark
    };

    /// <summary>The type two or more of the rentals have; null when none is shared.</summary>
    public static PokemonType? CommonType(IEnumerable<FactoryRental> rentals)
    {
        var counts = new Dictionary<PokemonType, int>();
        foreach (var rental in rentals)
        {
            if (PokemonDatabase.Get(rental.Species) is not { } species) continue;
            counts[species.PrimaryType] = counts.GetValueOrDefault(species.PrimaryType) + 1;
            if (species.SecondaryType is { } second && second != species.PrimaryType) counts[second] = counts.GetValueOrDefault(second) + 1;
        }
        PokemonType? common = null;
        int most = 1;
        foreach (var type in TypeOrder)
            if (counts.GetValueOrDefault(type) > most) (common, most) = (type, counts[type]);
        return common;
    }

    // ------------------------------------------------------------------ battle by battle

    /// <summary>
    /// The three chosen of the six, in the order chosen (<c>BattleFactory_AddSelectedRentalsToParty</c>): they are
    /// the team, the first trainer's three stand ready, and the taking counts as one trade
    /// (<c>BF_FUNC_INCREMENT_TRADE_COUNT</c>, which the corridor's scene calls once the six have been chosen from).
    /// </summary>
    public void Rent(IReadOnlyList<int> chosen)
    {
        if (chosen.Count != TeamSize || chosen.Distinct().Count() != TeamSize || chosen.Any(c => c < 0 || c >= Offer.Count))
            throw new InvalidOperationException("Three different Pokémon of the six on offer are rented");
        Team = chosen.Select(c => Offer[c].Copy()).ToList();
        Foe = NextFoe.Select(r => r.Copy()).ToList();
        CountTrade();
    }

    // BattleFactory_IncrementTradeCount
    private void CountTrade() => Trades = Math.Min(FrontierRecords.Most, Trades + 1);

    /// <summary>Whether the battle about to be fought is the Factory Head's (<c>BF_FUNC_GET_NEXT_BATTLE_TYPE</c>): a Single streak of 20 or of 48.</summary>
    public FrontierPrint ThortonNext => Way == FrontierChallenge.Single ? FrontierBrains.NextBattle(FrontierFacility.Factory, Streak) : FrontierPrint.None;

    /// <summary><c>BF_FUNC_CHECK_SEEN_HEAD_INTRO</c>: whether the Factory Head had been announced already; he has been now.</summary>
    public bool AnnounceHead()
    {
        bool before = HeadAnnounced;
        HeadAnnounced = true;
        return before;
    }

    /// <summary>The Factory Head was beaten (the battle room's script): the print waits in the lobby, the first time for each.</summary>
    public void BeatHead(Story.StoryState story, FrontierPrint which)
    {
        BeatThorton = which;
        FrontierBrains.Beaten(story, FrontierFacility.Factory, which);
    }

    /// <summary>
    /// After a battle won (<c>RECORD_BATTLE_FACTORY_VICTORIES</c>, <c>BF_FUNC_INCREMENT_CURRENT_BATTLE</c>,
    /// <c>BF_FUNC_INCREMENT_CURRENT_STREAK</c>): one more for the record of battles won here, for the round and for
    /// the streak. True once the round's seven are beaten.
    /// </summary>
    public bool Won(FrontierRecords records)
    {
        records.CountVictories(FrontierFacility.Factory, 1);
        Battle++;
        if (Streak < FrontierRecords.Most) Streak++;
        return Battle >= BattlesPerRound;
    }

    /// <summary>
    /// <c>BattleFactory_SetupNextOpponent</c>, before the next battle of a round: the next trainer's three are
    /// drawn from the trainer's row, clear of the species and the items of the three in hand and of the three just
    /// beaten, who stay as they are to be traded for.
    /// </summary>
    public void DrawNextFoe(Func<int> next16)
    {
        NextFoe = Load(SelectFromPool(Team.Concat(Foe).ToList(), TeamSize, FoePool(Trainers[Battle], OpenLevel, out _), null, 0, next16), next16);
        // The challenge goes on: it is no longer one just taken up again (VAR_0x8003)
        Resumed = false;
    }

    /// <summary>
    /// <c>BattleFactory_ApplyTrade</c>: one of the three in hand is given up for one of the beaten trainer's three,
    /// which takes its place. A trade counts for the streak and for the game's record of trades.
    /// </summary>
    public void Trade(FrontierRecords records, int mine, int theirs)
    {
        if (mine < 0 || mine >= Team.Count || theirs < 0 || theirs >= Foe.Count) throw new InvalidOperationException("There is no such Pokémon to trade");
        Team[mine] = Foe[theirs].Copy();
        CountTrade();
        records.Factory.Trades = Math.Min(999_999, records.Factory.Trades + 1);
    }

    /// <summary><c>BattleFactory_SetupNextOpponentsParty</c>: the trainer to come stands ready with the three drawn for them.</summary>
    public void TakeNextFoe() => Foe = NextFoe.Select(r => r.Copy()).ToList();

    // ------------------------------------------------------------------ the end of a round, and of a challenge

    // BattleFactory_Save: the latest streak and trades always; the record and whether the streak runs on unless the
    // challenge is only being put down
    private void Save(FrontierRecords records, bool pausing)
    {
        records.Set(LatestTradesStat(Way, OpenLevel), Trades);
        records.Set(LatestStreakStat(Way, OpenLevel), Streak);
        if (pausing) return;
        int before = records.Get(RecordStreakStat(Way, OpenLevel));
        int after = records.SetIfBetter(RecordStreakStat(Way, OpenLevel), Streak);
        // The record's trades are those of the streak that set it: the more of them when the record is only equalled
        if (Streak == before) records.SetIfBetter(RecordTradesStat(Way, OpenLevel), Trades);
        else if (before < after) records.Set(RecordTradesStat(Way, OpenLevel), Trades);
        records.SetRunning(FacilityOf(OpenLevel), (int)Way, RoundWon);
    }

    /// <summary>
    /// <c>BattleFactory_SaveOnCompletingRound</c>, when all seven are beaten: the streak runs on, the next round is
    /// one further (up to the eighth) and begins with no battle won. The caller adds <see cref="RoundScore"/> to the
    /// trainer's score.
    /// </summary>
    public void EndRound(FrontierRecords records)
    {
        RoundWon = true;
        if (Round < DistinctRounds) Round++;
        Battle = 0;
        Save(records, pausing: false);
    }

    /// <summary><c>BattleFactory_SaveOnLoss</c>, when a battle is lost or the challenge given up: the streak ends where it stands.</summary>
    public void EndLost(FrontierRecords records)
    {
        RoundWon = false;
        Save(records, pausing: false);
    }

    /// <summary>
    /// <c>BattleFactory_GetEarnedBP</c>, after <see cref="EndRound"/>: by the round the streak goes on to, nine from
    /// the eighth on, and twenty for a Single round that ended with the Factory Head beaten (a streak of 21 or of 49).
    /// </summary>
    public int PointsEarned
    {
        get
        {
            var (silverAt, goldAt) = FrontierBrains.Battles(FrontierFacility.Factory);
            if (Way == FrontierChallenge.Single && (Streak == silverAt || Streak == goldAt)) return ThortonPoints;
            return PointsByRound[Math.Min(Round, DistinctRounds)];
        }
    }

    /// <summary>
    /// <c>BF_FUNC_QUICKSAVE</c>: the challenge is put down between two battles, to be taken up where it stands: the
    /// trainers still to come, the three in hand and the three just beaten.
    /// </summary>
    public void Pause(FrontierRecords records)
    {
        Save(records, pausing: true);
        records.Factory.Paused = new FactoryPause
        {
            Way = Way,
            OpenLevel = OpenLevel,
            Battle = Battle,
            Trainers = Trainers.ToList(),
            Team = Team.Select(r => r.Copy()).ToList(),
            Foe = Foe.Select(r => r.Copy()).ToList()
        };
    }

    /// <summary>
    /// <c>ScrCmd_DeleteActiveBattleFactoryStreak</c>: a challenge was left without being put down (the game was
    /// closed in the middle of it). The streak of its way and level is over: no longer running, its latest streak and
    /// trades nought.
    /// </summary>
    public static void Forfeit(FrontierRecords records, FrontierChallenge way, bool openLevel)
    {
        records.SetRunning(FacilityOf(openLevel), (int)way, false);
        records.Set(LatestStreakStat(way, openLevel), 0);
        records.Set(LatestTradesStat(way, openLevel), 0);
        records.Factory.Paused = null;
    }
}
