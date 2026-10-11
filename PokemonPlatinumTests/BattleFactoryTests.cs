using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The Battle Factory (plan 06 · R18b). The numbers are the decompilation's (<c>src/overlay104/battle_factory.c</c>,
/// <c>battle_factory_helpers.c</c>, <c>frscrcmd_battle_factory.c</c>, <c>src/battle_factory_save.c</c>,
/// <c>scripts_battle_factory.s</c>, <c>frontier_scripts_battle_factory.s</c> and the rental application,
/// <c>src/applications/frontier/battle_factory/main.c</c>).
/// </summary>
[Collection("MapDatabase")]
public class BattleFactoryTests
{
    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static bool Said(HeadlessScriptHost host, string part) => host.Transcript.Any(l => l.Text.Contains(part));

    /// <summary>The draws a test hands over one after another, as the original's generator would give them.</summary>
    private static Func<int> Draws(params int[] values)
    {
        int at = 0;
        return () => at < values.Length ? values[at++] : throw new InvalidOperationException($"more than {values.Length} draws were asked for");
    }

    private static Func<int> Seeded(int seed)
    {
        var random = new Random(seed);
        return () => random.Next(0x10000);
    }

    private static int SetOf(string species) => FrontierDatabase.Sets.ToList().FindIndex(s => s.Species == species) is var n && n > 0 ? n : throw new ArgumentException(species);

    private static FactoryRental Rental(string species) => new() { Set = SetOf(species), Ivs = 0, Personality = 1, OtId = 2 };

    /// <summary>A challenge whose streak has come through so many rounds already, its round drawn.</summary>
    private static BattleFactoryChallenge InRound(FrontierRecords records, int round, FrontierChallenge way = FrontierChallenge.Single, bool open = false, int trades = 0, int seed = 7)
    {
        if (round > 0)
        {
            records.Set(BattleFactoryChallenge.LatestStreakStat(way, open), round * 7);
            records.Set(BattleFactoryChallenge.LatestTradesStat(way, open), trades);
            records.SetRunning(BattleFactoryChallenge.FacilityOf(open), (int)way, true);
        }
        var challenge = BattleFactoryChallenge.Begin(records, way, open);
        challenge.DrawRound(Seeded(seed));
        return challenge;
    }

    // ------------------------------------------------------------------ the tables

    [Fact]
    public void TheFactorysTablesAreTheOriginals()
    {
        var tables = FrontierDatabase.Factory;
        // sBattleFactoryOpponentPools: eight rounds, each a span for its first six battles and one for its seventh
        Assert.Equal(8, tables.Opponents.Count);
        int N(string id) => FrontierDatabase.NumberOf(id);
        Assert.Equal(new[] { N("youngster_jim"), N("reporter_gingham"), N("cyclist_gaspar"), N("socialite_carmen") }, tables.Opponents[0]);
        Assert.Equal(new[] { N("jogger_colt"), N("breeder_antonia"), N("ace_trainer_sawyer"), N("veteran_alfred") }, tables.Opponents[6]);
        Assert.Equal(new[] { N("camper_freddy"), N("idol_nissa"), N("camper_freddy"), N("idol_nissa") }, tables.Opponents[7]);
        Assert.Equal((0, 100), (N("youngster_jim"), N("cyclist_gaspar")));

        // sLevel50Pools, sOpenLevelOpponentPools, sOpenLevelPlayerPools: ten rows each
        Assert.All(new[] { tables.Level50, tables.OpenFoes, tables.OpenRentals }, rows => Assert.Equal(10, rows.Count));
        (int, int, int, int, bool) Row(FactoryPoolRecord r) => (r.TrainerMax, r.SetMin, r.SetMax, r.Ivs, r.Upgrades);
        Assert.Equal((N("cyclist_gaspar"), 1, 150, 0, true), Row(tables.Level50[0]));
        Assert.Equal((N("psychic_alpha"), 151, 250, 4, true), Row(tables.Level50[1]));
        Assert.Equal((N("ace_trainer_sawyer"), 759, 894, 24, true), Row(tables.Level50[6]));
        Assert.Equal((N("trainer_cheryl_cheryl"), 351, 950, 31, false), Row(tables.Level50[7]));
        Assert.Equal((N("factory_head_thorton_gold"), 351, 486, 12, false), Row(tables.Level50[8]));
        Assert.Equal((N("arcade_star_dahlia_silver"), 351, 486, 31, false), Row(tables.Level50[9]));
        Assert.Equal((N("cyclist_gaspar"), 351, 486, 0, true), Row(tables.OpenFoes[0]));
        Assert.Equal((N("jogger_colt"), 351, 950, 16, true), Row(tables.OpenFoes[4]));
        Assert.Equal((N("jogger_colt"), 623, 950, 16, true), Row(tables.OpenRentals[4]));
        Assert.Equal((N("factory_head_thorton_gold"), 759, 950, 12, false), Row(tables.OpenRentals[8]));
        // The Factory Head's two numbers lie in the rows meant for him
        var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Factory);
        Assert.Equal(8, tables.Level50.FindIndex(r => silver < r.TrainerMax));
        Assert.Equal(9, tables.Level50.FindIndex(r => gold < r.TrainerMax));
        Assert.Equal(new[] { 6, 13, 20, 27, 34 }, BattleFactoryChallenge.TradesForUpgrades);
        Assert.Equal(new[] { 0, 5, 5, 5, 5, 7, 7, 8, 9 }, BattleFactoryChallenge.PointsByRound);
    }

    // ------------------------------------------------------------------ the round's trainers

    [Fact]
    public void ARoundsTrainersAreDrawnFromItsRowsAndNoneTwice()
    {
        // BattleFactory_GetOpponentTrainerID: min + draw % (max - min), so the row's last trainer is never drawn. The
        // first round's rows are 0 to 99 and 100 to 119: a 5 is trainer 5, a second 5 is drawn again, 98 is trainer
        // 98 and 99 comes round to trainer 0; the seventh and the fourteenth take the second row (18 is 118, 19 is 100)
        var trainers = BattleFactoryChallenge.PickTrainers(FrontierChallenge.Single, 0, 14, Draws(5, 5, 6, 98, 99, 7, 8, 18, 19, 20, 21, 22, 23, 24, 19));
        Assert.Equal(new[] { 5, 6, 98, 0, 7, 8, 118, 19, 20, 21, 22, 23, 24, 100 }, trainers);

        // Every round, both ways: fourteen different trainers, each from its place's span
        var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Factory);
        foreach (var way in new[] { FrontierChallenge.Single, FrontierChallenge.Double })
            for (int round = 0; round < 12; round++)
            {
                var row = FrontierDatabase.Factory.Opponents[Math.Min(round, 7)];
                var drawn = BattleFactoryChallenge.PickTrainers(way, round, 14, Seeded(round * 31 + (int)way));
                Assert.Equal(14, drawn.Distinct().Count());
                for (int place = 0; place < 14; place++)
                {
                    if (drawn[place] == silver || drawn[place] == gold) continue;
                    bool last = place is 6 or 13;
                    Assert.InRange(drawn[place], last ? row[2] : row[0], (last ? row[3] : row[1]) - 1);
                }
            }
    }

    [Fact]
    public void TheFactoryHeadStandsAtTheTwentyFirstAndTheFortyNinthBattleOfASingleStreak()
    {
        var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Factory);
        var none = Draws();
        // The third round's seventh battle is the streak's 21st, and the seventh round's the 49th: no draw is made
        Assert.Equal(silver, BattleFactoryChallenge.TrainerFor(FrontierChallenge.Single, 2, 6, none));
        Assert.Equal(gold, BattleFactoryChallenge.TrainerFor(FrontierChallenge.Single, 6, 6, none));
        // A Double challenge never meets him, and nor does a Single one once past its eighth round
        Assert.InRange(BattleFactoryChallenge.TrainerFor(FrontierChallenge.Double, 2, 6, Draws(3)), 140, 158);
        Assert.NotEqual(gold, BattleFactoryChallenge.TrainerFor(FrontierChallenge.Single, 9, 6, Draws(3)));
        // The original counts on through the seven the solo challenge never meets: the second round's fourteenth
        // would be a 21st too, and is him, unseen
        Assert.Equal(silver, BattleFactoryChallenge.TrainerFor(FrontierChallenge.Single, 1, 13, none));

        var records = new FrontierRecords();
        var third = InRound(records, 2);
        Assert.Equal(silver, third.Trainers[6]);
        Assert.Equal((14, 2, FrontierPrint.None), (third.Streak, third.Round, third.ThortonNext));
    }

    // ------------------------------------------------------------------ the rentals

    [Fact]
    public void TradesLiftSomeOfTheSixIntoTheNextRow()
    {
        // sTradeCountsNeededForUpgrades: none up to six trades, one more for every seven after, five at most
        Assert.Equal(new[] { 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 },
            new[] { 0, 1, 6, 7, 13, 14, 20, 21, 27, 28, 34, 35, 9999 }.Select(BattleFactoryChallenge.Lifted));

        // BattleFactory_SelectPokemonFromPool: a set is the row's last less the draw's remainder by the row's count, so
        // in the first row at Level 50 (sets 1 to 150) a draw of 0 is set 150 and a draw of 149 set 1
        var first = BattleFactoryChallenge.RentalPool(0, false, out var second);
        var one = BattleFactoryChallenge.SelectFromPool(Array.Empty<FactoryRental>(), 1, first, second, 0, Draws(149));
        Assert.Equal((1, 0), one.Single());
        Assert.Equal(150, BattleFactoryChallenge.SelectFromPool(Array.Empty<FactoryRental>(), 1, first, second, 0, Draws(300)).Single().Set);

        foreach (int trades in new[] { 0, 6, 7, 14, 35 })
        {
            int lifted = BattleFactoryChallenge.Lifted(trades);
            var six = BattleFactoryChallenge.SelectFromPool(Array.Empty<FactoryRental>(), 6, first, second, trades, Seeded(trades + 1));
            // The last of the six come from the second row (sets 151 to 250), with its IVs
            Assert.All(six.Take(6 - lifted), s => Assert.True(s.Set is >= 1 and <= 150 && s.Ivs == 0, $"{trades} trades: set {s.Set}"));
            Assert.All(six.Skip(6 - lifted), s => Assert.True(s.Set is >= 151 and <= 250 && s.Ivs == 4, $"{trades} trades: set {s.Set}"));
        }

        // The eighth round's row can't be lifted, however many the trades
        var eighth = BattleFactoryChallenge.RentalPool(7, false, out var ninth);
        Assert.False(eighth.Upgrades);
        Assert.All(BattleFactoryChallenge.SelectFromPool(Array.Empty<FactoryRental>(), 6, eighth, ninth, 35, Seeded(3)), s => Assert.Equal(31, s.Ivs));
        // And every round from the eighth on draws from it
        Assert.Same(eighth, BattleFactoryChallenge.RentalPool(30, false, out _));
        Assert.Same(FrontierDatabase.Factory.OpenRentals[3], BattleFactoryChallenge.RentalPool(3, true, out _));
    }

    [Fact]
    public void NoTwoOfADrawShareASpeciesOrAnItemNorWithThoseTakenAlready()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            var records = new FrontierRecords();
            var challenge = InRound(records, seed % 9, seed % 2 == 0 ? FrontierChallenge.Single : FrontierChallenge.Double, seed % 3 == 0, trades: seed, seed: seed);
            Assert.Equal((6, 3, 14), (challenge.Offer.Count, challenge.NextFoe.Count, challenge.Trainers.Count));
            var all = challenge.Offer.Concat(challenge.NextFoe).ToList();
            Assert.Equal(9, all.Select(r => r.Species).Distinct().Count());
            Assert.Equal(9, all.Select(r => r.Item).Distinct().Count());

            // The next trainer's three keep clear of the three in hand and of the three just beaten
            challenge.Rent(new[] { 5, 0, 3 });
            challenge.Won(records);
            challenge.DrawNextFoe(Seeded(seed + 100));
            var six = challenge.Team.Concat(challenge.Foe).Concat(challenge.NextFoe).ToList();
            Assert.Equal(9, six.Select(r => r.Species).Distinct().Count());
            Assert.Equal(9, six.Select(r => r.Item).Distinct().Count());
        }
    }

    [Fact]
    public void ARoundIsTheSameFromTheSameDraws()
    {
        var a = InRound(new FrontierRecords(), 3, seed: 99);
        var b = InRound(new FrontierRecords(), 3, seed: 99);
        Assert.Equal(a.Trainers, b.Trainers);
        Assert.Equal(a.Offer.Select(r => (r.Set, r.Ivs, r.Personality, r.OtId)), b.Offer.Select(r => (r.Set, r.Ivs, r.Personality, r.OtId)));
        Assert.Equal(a.NextFoe.Select(r => (r.Set, r.Ivs, r.Personality)), b.NextFoe.Select(r => (r.Set, r.Ivs, r.Personality)));
        Assert.NotEqual(a.Offer.Select(r => r.Set), InRound(new FrontierRecords(), 3, seed: 100).Offer.Select(r => r.Set));
    }

    [Fact]
    public void ARentalIsItsSetAtTheChallengesLevel()
    {
        foreach (bool open in new[] { false, true })
        {
            var challenge = InRound(new FrontierRecords(), 2, open: open);
            foreach (var rental in challenge.Offer.Concat(challenge.NextFoe))
            {
                var p = challenge.Build(rental);
                var set = rental.Record;
                Assert.Equal((set.Species, open ? 100 : 50, set.Nature, set.Item), (p.Species.Name, p.Level, p.Nature, p.HeldItem?.Name));
                Assert.Equal(set.Moves, p.Moves.Select(m => m.Name));
                Assert.All(new[] { p.IvHP, p.IvAttack, p.IvDefense, p.IvSpeed, p.IvSpAttack, p.IvSpDefense }, iv => Assert.Equal(rental.Ivs, iv));
                Assert.False(p.IsShiny);
                Assert.Equal((p.MaxHP, StatusCondition.None), (p.CurrentHP, p.Status));
                // SetRentalPokemonSummaryInfo: nobody's one knows, met here, in a Poké Ball
                Assert.Equal((BattleFactoryChallenge.RentalTrainer, "Battle Factory", "Poké Ball"), (p.OriginalTrainer!.Name, p.MetLocation, p.Ball));
                // The same four numbers make the same Pokémon again
                var again = challenge.Build(rental.Copy());
                Assert.Equal((p.Personality, p.AbilityName, p.Gender, p.Attack, p.Speed), (again.Personality, again.AbilityName, again.Gender, again.Attack, again.Speed));
            }
            // The third round's six at Level 50 are of its row (sets 251 to 350, IVs of 8); at the Open Level of the row 623 to 758
            Assert.All(challenge.Offer, r => Assert.InRange(r.Set, open ? 623 : 251, open ? 758 : 350));
            Assert.All(challenge.Offer, r => Assert.Equal(8, r.Ivs));
        }
    }

    // ------------------------------------------------------------------ what the staff tell

    [Fact]
    public void WhatTheStaffTellGrowsLessWithEveryRound()
    {
        (FactoryHint Kind, IReadOnlyList<string> Names) HintIn(int round, out BattleFactoryChallenge challenge)
        {
            challenge = InRound(new FrontierRecords(), round, FrontierChallenge.Double);
            return challenge.Hint();
        }

        var (kind, names) = HintIn(0, out var c);
        Assert.Equal(FactoryHint.ThreeSpecies, kind);
        Assert.Equal(c.NextFoe.Select(r => r.Species), names);
        (kind, names) = HintIn(1, out c);
        Assert.Equal(FactoryHint.TwoSpecies, kind);
        Assert.Equal(c.NextFoe.Take(2).Select(r => r.Species), names);
        (kind, names) = HintIn(2, out c);
        Assert.Equal(FactoryHint.SpeciesAndMove, kind);
        Assert.Equal(new[] { c.NextFoe[0].Species, c.NextFoe[0].Record.Moves[0] }, names);
        (kind, names) = HintIn(3, out c);
        Assert.Equal(FactoryHint.Move, kind);
        Assert.Equal(new[] { c.NextFoe[0].Record.Moves[0] }, names);
        foreach (int round in new[] { 4, 5, 8, 20 })
        {
            (kind, names) = HintIn(round, out c);
            var type = BattleFactoryChallenge.CommonType(c.NextFoe);
            Assert.Equal(type == null ? FactoryHint.VariedTypes : FactoryHint.CommonType, kind);
            Assert.Equal(type == null ? Array.Empty<string>() : new[] { type.ToString()! }, names);
        }
    }

    [Fact]
    public void TheCommonTypeIsTheOneMostOfTheThreeHave()
    {
        // BF_FUNC_GET_COMMON_TYPE: each Pokémon counts once for each of its types; a type only one has is none
        Assert.Equal(PokemonType.Water, BattleFactoryChallenge.CommonType(new[] { Rental("Vaporeon"), Rental("Quagsire"), Rental("Arcanine") }));
        Assert.Null(BattleFactoryChallenge.CommonType(new[] { Rental("Vaporeon"), Rental("Arcanine"), Rental("Jolteon") }));
        // Two types shared by two each: the first in the original's order of types wins (Flying is its third, Fire its eleventh)
        Assert.Equal(PokemonType.Flying, BattleFactoryChallenge.CommonType(new[] { Rental("Charizard"), Rental("Gyarados"), Rental("Arcanine") }));
        // A type all three have beats one two have
        Assert.Equal(PokemonType.Water, BattleFactoryChallenge.CommonType(new[] { Rental("Gyarados"), Rental("Mantine"), Rental("Vaporeon") }));
    }

    // ------------------------------------------------------------------ battle by battle

    [Fact]
    public void ThreeAreRentedInTheirOrderAndTheTakingCountsAsATrade()
    {
        var records = new FrontierRecords();
        var challenge = InRound(records, 0);
        var first = challenge.NextFoe.Select(r => r.Set).ToList();
        challenge.Rent(new[] { 4, 0, 2 });
        Assert.Equal(new[] { challenge.Offer[4].Set, challenge.Offer[0].Set, challenge.Offer[2].Set }, challenge.Team.Select(r => r.Set));
        Assert.Equal(first, challenge.Foe.Select(r => r.Set));
        // BF_FUNC_INCREMENT_TRADE_COUNT, which the corridor's scene calls once the three are chosen
        Assert.Equal((1, 0), (challenge.Trades, records.Factory.Trades));
        Assert.Throws<InvalidOperationException>(() => challenge.Rent(new[] { 1, 1, 2 }));
        Assert.Throws<InvalidOperationException>(() => challenge.Rent(new[] { 1, 2 }));

        // The team goes into battle whole, in that order, against the trainer of this place with the three drawn
        var team = challenge.TeamForBattle();
        Assert.Equal(challenge.Team.Select(r => r.Species), team.Members.Select(p => p.Species.Name));
        var foe = challenge.Opponent();
        Assert.Equal((challenge.Trainers[0], 3), (foe.Number, foe.Pokemon.Count));
        Assert.Equal(challenge.Foe.Select(r => r.Species), foe.Pokemon.Select(p => p.Species.Name));
        // A Single challenge puts one Pokémon out, and the script a Double one two: both bring three
        Assert.Equal(0, foe.AsTrainer(challenge.Mind).PrizeMoney);
    }

    [Fact]
    public void ATradeTakesOneOfTheBeatenThreeAsItWas()
    {
        var records = new FrontierRecords();
        var challenge = InRound(records, 0);
        challenge.Rent(new[] { 0, 1, 2 });
        Assert.False(challenge.Won(records));
        Assert.Equal((1, 1, 1), (challenge.Battle, challenge.Streak, records.VictoriesAt(FrontierFacility.Factory)));

        // The next trainer is drawn before the trade is offered, and the beaten three stay as they are
        var beaten = challenge.Foe.Select(r => r.Copy()).ToList();
        challenge.DrawNextFoe(Seeded(5));
        Assert.Equal(beaten.Select(r => r.Set), challenge.Foe.Select(r => r.Set));
        Assert.NotEqual(beaten.Select(r => r.Set), challenge.NextFoe.Select(r => r.Set));

        var kept = (challenge.Team[0].Set, challenge.Team[2].Set);
        challenge.Trade(records, 1, 2);
        Assert.Equal(kept, (challenge.Team[0].Set, challenge.Team[2].Set));
        Assert.Equal((beaten[2].Set, beaten[2].Ivs, beaten[2].Personality), (challenge.Team[1].Set, challenge.Team[1].Ivs, challenge.Team[1].Personality));
        // The rental and one trade; the game's own record counts the trade alone
        Assert.Equal((2, 1), (challenge.Trades, records.Factory.Trades));
        Assert.Throws<InvalidOperationException>(() => challenge.Trade(records, 3, 0));

        // BattleFactory_SetupNextOpponentsParty: now the next trainer's three stand ready
        challenge.TakeNextFoe();
        Assert.Equal(challenge.NextFoe.Select(r => r.Set), challenge.Foe.Select(r => r.Set));
        Assert.Equal(challenge.Trainers[1], challenge.Opponent().Number);
    }

    [Fact]
    public void TheTrainersOfTheFirstRoundsThinkLess()
    {
        // BattleFactory_GetAIMask: no routine at all in the first two rounds, the basic one in the next two, all three after
        AiFlags MindIn(int round, FrontierChallenge way = FrontierChallenge.Double) => InRound(new FrontierRecords(), round, way).Mind;
        Assert.Equal(new[] { AiFlags.None, AiFlags.None, AiFlags.Basic, AiFlags.Basic, FrontierTrainers.Mind, FrontierTrainers.Mind, FrontierTrainers.Mind },
            new[] { 0, 1, 2, 3, 4, 7, 12 }.Select(r => MindIn(r)));

        // The Factory Head thinks with all three, in a round whose other trainers don't
        var records = new FrontierRecords();
        var third = InRound(records, 2);
        third.Rent(new[] { 0, 1, 2 });
        Assert.Equal(AiFlags.Basic, third.Mind);
        for (int i = 0; i < 6; i++) third.Won(records);
        Assert.Equal((FrontierPrint.Silver, FrontierTrainers.Mind), (third.ThortonNext, third.Mind));
        Assert.Equal(FrontierTrainers.Mind, third.Opponent().AsTrainer(third.Mind).Ai);
        Assert.Equal("Factory Head", third.Opponent().Record.Class);
        // His three are rentals like anyone's, from the row kept for him: sets 351 to 486 at Level 50, IVs of 12
        third.DrawNextFoe(Seeded(1));
        Assert.All(third.NextFoe, r => Assert.True(r.Set is >= 351 and <= 486 && r.Ivs == 12));
    }

    // ------------------------------------------------------------------ the end of a round, and of a challenge

    private static void WinRound(BattleFactoryChallenge challenge, FrontierRecords records, int tradesInRound = 0, int seed = 3)
    {
        var draw = Seeded(seed);
        challenge.Rent(new[] { 0, 1, 2 });
        for (int battle = 0; battle < 7; battle++)
        {
            if (battle > 0)
            {
                challenge.DrawNextFoe(draw);
                if (tradesInRound-- > 0) challenge.Trade(records, 0, 0);
                challenge.TakeNextFoe();
            }
            bool seven = challenge.Won(records);
            Assert.Equal(battle == 6, seven);
        }
        challenge.EndRound(records);
    }

    [Fact]
    public void RoundsWonEarnBattlePointsAndTheStreakRunsOn()
    {
        // sBPPerRoundSolo by the round the streak goes on to, and 20 for a Single streak of 21 or of 49
        foreach (var (way, expected) in new[]
                 {
                     (FrontierChallenge.Single, new[] { 5, 5, 20, 5, 7, 7, 20, 9, 9, 9 }),
                     (FrontierChallenge.Double, new[] { 5, 5, 5, 5, 7, 7, 8, 9, 9, 9 })
                 })
        {
            var records = new FrontierRecords();
            var earned = new List<int>();
            for (int round = 0; round < 10; round++)
            {
                var challenge = BattleFactoryChallenge.Begin(records, way, false);
                Assert.Equal((round * 7, round, 0), (challenge.Streak, challenge.Round, challenge.Battle));
                challenge.DrawRound(Seeded(round));
                WinRound(challenge, records);
                earned.Add(challenge.PointsEarned);
                // The round counts on to the eighth and no further; a streak taken up past it keeps the count it began with
                Assert.Equal((round < 8 ? round + 1 : round, 0, true), (challenge.Round, challenge.Battle, challenge.RoundWon));
                Assert.Equal(((round + 1) * 7, (round + 1) * 7, true), (records.Get(BattleFactoryChallenge.LatestStreakStat(way, false)),
                    records.Get(BattleFactoryChallenge.RecordStreakStat(way, false)), records.IsRunning(FrontierFacility.Factory, (int)way)));
                // A round's three count as one trade
                Assert.Equal(round + 1, records.Get(BattleFactoryChallenge.LatestTradesStat(way, false)));
            }
            Assert.Equal(expected, earned);
            Assert.Equal(70, records.VictoriesAt(FrontierFacility.Factory));
        }
    }

    [Fact]
    public void ALossEndsTheStreakWhereItStands()
    {
        var records = new FrontierRecords();
        var challenge = InRound(records, 0, FrontierChallenge.Single, open: true);
        WinRound(challenge, records, tradesInRound: 2);
        var stat = (Record: FrontierStat.FactoryRecordStreakOpenSingle, Latest: FrontierStat.FactoryLatestStreakOpenSingle,
            RecordTrades: FrontierStat.FactoryRecordTradeCountOpenSingle, LatestTrades: FrontierStat.FactoryLatestTradeCountOpenSingle);
        Assert.Equal((7, 7, 3, 3), (records.Get(stat.Record), records.Get(stat.Latest), records.Get(stat.RecordTrades), records.Get(stat.LatestTrades)));
        // The Open Level's streak is its own: the Level 50 one hasn't begun
        Assert.True(records.IsRunning(FrontierFacility.FactoryOpen, 0));
        Assert.False(records.IsRunning(FrontierFacility.Factory, 0));
        Assert.Equal(0, records.Get(FrontierStat.FactoryRecordStreak50Single));

        // The second round: three won, and the fourth lost
        var second = BattleFactoryChallenge.Begin(records, FrontierChallenge.Single, true);
        second.DrawRound(Seeded(8));
        second.Rent(new[] { 3, 4, 5 });
        for (int i = 0; i < 3; i++) second.Won(records);
        second.EndLost(records);
        Assert.Equal((10, 10, 4, 4, false), (records.Get(stat.Record), records.Get(stat.Latest), records.Get(stat.RecordTrades), records.Get(stat.LatestTrades),
            records.IsRunning(FrontierFacility.FactoryOpen, 0)));

        // The next challenge starts from nothing, and a shorter streak leaves the record and its trades alone
        var third = BattleFactoryChallenge.Begin(records, FrontierChallenge.Single, true);
        Assert.Equal((0, 0, 0), (third.Streak, third.Trades, third.Round));
        third.DrawRound(Seeded(9));
        third.Rent(new[] { 0, 1, 2 });
        third.Won(records);
        third.EndLost(records);
        Assert.Equal((10, 1, 4, 1), (records.Get(stat.Record), records.Get(stat.Latest), records.Get(stat.RecordTrades), records.Get(stat.LatestTrades)));
    }

    [Fact]
    public void ARecordEqualledKeepsTheMoreTradesOfTheTwoStreaks()
    {
        // BattleFactory_Save: a streak that only equals the record betters its trades if it made more; one that beats
        // it sets them, more or fewer
        var records = new FrontierRecords();
        var a = InRound(records, 0, FrontierChallenge.Double);
        WinRound(a, records, tradesInRound: 4);
        Assert.Equal((7, 5), (records.Get(FrontierStat.FactoryRecordStreak50Double), records.Get(FrontierStat.FactoryRecordTradeCount50Double)));
        // Lost in the second round with nothing won: the streak ends at seven, and its trades are one more
        var b = BattleFactoryChallenge.Begin(records, FrontierChallenge.Double, false);
        b.DrawRound(Seeded(2));
        b.Rent(new[] { 0, 1, 2 });
        b.EndLost(records);
        Assert.Equal((7, 6), (records.Get(FrontierStat.FactoryRecordStreak50Double), records.Get(FrontierStat.FactoryRecordTradeCount50Double)));
        // A new streak of eight with two trades beats it, and the record's trades are its own two
        var c = BattleFactoryChallenge.Begin(records, FrontierChallenge.Double, false);
        c.DrawRound(Seeded(4));
        WinRound(c, records);
        var d = BattleFactoryChallenge.Begin(records, FrontierChallenge.Double, false);
        d.DrawRound(Seeded(5));
        d.Rent(new[] { 0, 1, 2 });
        d.Won(records);
        d.EndLost(records);
        Assert.Equal((8, 2), (records.Get(FrontierStat.FactoryRecordStreak50Double), records.Get(FrontierStat.FactoryRecordTradeCount50Double)));
    }

    [Fact]
    public void AChallengePutDownIsTakenUpWhereItStood()
    {
        var records = new FrontierRecords();
        var challenge = InRound(records, 1);
        challenge.Rent(new[] { 2, 3, 4 });
        challenge.Won(records);
        challenge.DrawNextFoe(Seeded(1));
        challenge.Trade(records, 0, 1);
        challenge.TakeNextFoe();
        challenge.Won(records);
        challenge.Pause(records);
        // BF_FUNC_QUICKSAVE: the latest streak and trades are written, the record is not
        Assert.Equal((9, 0, 2), (records.Get(FrontierStat.FactoryLatestStreak50Single), records.Get(FrontierStat.FactoryRecordStreak50Single),
            records.Get(FrontierStat.FactoryLatestTradeCount50Single)));

        // Through a save and back
        var saved = GameDataFiles.Deserialize<FrontierRecords>(GameDataFiles.Serialize(records));
        Assert.Equal(1, saved.Factory.Trades);
        var resumed = BattleFactoryChallenge.Resume(saved)!;
        Assert.Null(saved.Factory.Paused);
        Assert.Null(BattleFactoryChallenge.Resume(saved));
        Assert.Equal((challenge.Way, challenge.OpenLevel, 2, 9, 2, 1, true), (resumed.Way, resumed.OpenLevel, resumed.Battle, resumed.Streak, resumed.Trades, resumed.Round, resumed.Resumed));
        Assert.Equal(challenge.Trainers, resumed.Trainers);
        Assert.Equal(challenge.Team.Select(r => (r.Set, r.Ivs, r.Personality, r.OtId)), resumed.Team.Select(r => (r.Set, r.Ivs, r.Personality, r.OtId)));
        Assert.Equal(challenge.Foe.Select(r => (r.Set, r.Ivs, r.Personality)), resumed.Foe.Select(r => (r.Set, r.Ivs, r.Personality)));
        // It goes on: the trainer of the third battle, and the beaten three still there to trade for
        resumed.DrawNextFoe(Seeded(2));
        Assert.False(resumed.Resumed);
        resumed.Trade(saved, 2, 0);
        resumed.TakeNextFoe();
        Assert.Equal(resumed.Trainers[2], resumed.Opponent().Number);

        // A new challenge lets go of whatever was put down
        challenge.Pause(records);
        BattleFactoryChallenge.Begin(records, FrontierChallenge.Double, false);
        Assert.Null(records.Factory.Paused);
    }

    [Fact]
    public void AChallengeWalkedOutOnLosesItsStreak()
    {
        var records = new FrontierRecords();
        WinRound(InRound(records, 0), records);
        var second = BattleFactoryChallenge.Begin(records, FrontierChallenge.Single, false);
        second.Pause(records);
        // ScrCmd_DeleteActiveBattleFactoryStreak: no longer running, its latest streak and trades nought; the record stays
        BattleFactoryChallenge.Forfeit(records, FrontierChallenge.Single, false);
        Assert.Equal((false, 0, 0, 7), (records.IsRunning(FrontierFacility.Factory, 0), records.Get(FrontierStat.FactoryLatestStreak50Single),
            records.Get(FrontierStat.FactoryLatestTradeCount50Single), records.Get(FrontierStat.FactoryRecordStreak50Single)));
        Assert.Null(records.Factory.Paused);
        Assert.Equal(0, BattleFactoryChallenge.Begin(records, FrontierChallenge.Single, false).Streak);
    }

    [Fact]
    public void TheStatsAreTheOriginalsFourForEachWayAndLevel()
    {
        // BattleFrontierStats_GetFactoryRecordStreakIdx and its fellows
        Assert.Equal(FrontierStat.FactoryRecordStreak50Single, BattleFactoryChallenge.RecordStreakStat(FrontierChallenge.Single, false));
        Assert.Equal(FrontierStat.FactoryLatestStreakOpenSingle, BattleFactoryChallenge.LatestStreakStat(FrontierChallenge.Single, true));
        Assert.Equal(FrontierStat.FactoryRecordTradeCount50Double, BattleFactoryChallenge.RecordTradesStat(FrontierChallenge.Double, false));
        Assert.Equal(FrontierStat.FactoryLatestTradeCountOpenDouble, BattleFactoryChallenge.LatestTradesStat(FrontierChallenge.Double, true));
        Assert.Throws<InvalidOperationException>(() => BattleFactoryChallenge.Begin(new FrontierRecords(), FrontierChallenge.Multi, false));
    }

    [Fact]
    public void TheBoardShowsTheFactorysFourWaysWithTheirTrades()
    {
        var records = new FrontierRecords();
        var story = new StoryState();
        Assert.Equal(4, FrontierBoard.For(FrontierFacility.Factory, records, story).Rows.Count);
        WinRound(InRound(records, 0, FrontierChallenge.Double, open: true), records, tradesInRound: 3);
        records.GivePoints(5);
        story.SetVar("VAR_BATTLE_FACTORY_PRINT_STATE", 2);
        var board = FrontierBoard.For(FrontierFacility.Factory, records, story);
        Assert.Equal(("Battle Factory", "TRADES", 5, FrontierPrint.Silver, 7), (board.Title, board.CountLabel, board.BattlePoints, board.Print, board.Victories));
        Assert.Equal(new[] { "Lv. 50 Single", "Lv. 50 Double", "Open Level Single", "Open Level Double" }, board.Rows.Select(r => r.Label));
        Assert.Equal(new FrontierBoardRow("Open Level Double", 7, 7, true, 4, 4), board.Rows[3]);
        Assert.Equal(new FrontierBoardRow("Lv. 50 Single", 0, 0, false, 0, 0), board.Rows[0]);
        // The Tower's rows count nothing beside their streaks
        Assert.Null(FrontierBoard.For(FrontierFacility.Tower, records, story).CountLabel);
        // A win at the Factory is one of the five the Villa's bust asks for
        Assert.True(records.VictoriesAt(FrontierFacility.Factory) > 0);
    }

    // ------------------------------------------------------------------ the screen

    private static List<Pokemon> Built(BattleFactoryChallenge challenge, IEnumerable<FactoryRental> rentals) => rentals.Select(challenge.Build).ToList();

    [Fact]
    public void ThreeAreChosenOnTheScreenInTheirOrder()
    {
        var challenge = InRound(new FrontierRecords(), 0);
        var screen = new FactoryScreen();
        screen.OpenToRent(Built(challenge, challenge.Offer), 3);
        Assert.Equal((FactoryScreenMode.Rent, FactoryStep.Browse, 0, 6), (screen.Mode, screen.Step, screen.Cursor, screen.Mine.Count));
        Assert.Contains("first", screen.Prompt);

        // A Pokémon's menu: SUMMARY, RENT, CANCEL
        screen.Move(1, 0);
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal((FactoryStep.Options, 2), (screen.Step, screen.Cursor));
        Assert.Equal(new[] { FactoryOption.Summary, FactoryOption.Rent, FactoryOption.Cancel }, screen.Options);
        // The summary of the one under the cursor, turned to the next, and back to the menu
        screen.Confirm();
        Assert.Equal((FactoryStep.Summary, challenge.Offer[2].Species), (screen.Step, screen.Shown!.Species.Name));
        screen.Move(0, 1);
        Assert.Equal(challenge.Offer[3].Species, screen.Shown!.Species.Name);
        screen.Cancel();
        Assert.Equal((FactoryStep.Options, 3), (screen.Step, screen.Cursor));
        // RENT
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(FactoryStep.Browse, screen.Step);
        Assert.Equal(new[] { 3 }, screen.Chosen);
        Assert.Contains("second", screen.Prompt);

        // One already rented offers REMOVE in RENT's place
        screen.Confirm();
        Assert.Equal(FactoryOption.Remove, screen.Options[1]);
        screen.Cancel();
        // The left of the first is the last
        for (int i = 0; i < 4; i++) screen.Move(-1, 0);
        Assert.Equal(5, screen.Cursor);
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(new[] { 3, 5 }, screen.Chosen);
        // RemoveSelectedMonUnderCursor: the first is taken out and the second moves up
        screen.Move(1, 0);
        for (int i = 0; i < 3; i++) screen.Move(1, 0);
        Assert.Equal(3, screen.Cursor);
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(new[] { 5 }, screen.Chosen);
        // B takes the last one back
        screen.Cancel();
        Assert.Empty(screen.Chosen);
        screen.Cancel();
        Assert.True(screen.IsActive);

        // Three chosen: the question, a no takes the third back, a yes closes with the three in their order
        void RentAt(int place)
        {
            while (screen.Cursor != place) screen.Move(1, 0);
            screen.Confirm();
            screen.Move(0, 1);
            screen.Confirm();
        }
        RentAt(4);
        RentAt(0);
        RentAt(2);
        Assert.Equal(FactoryStep.Confirm, screen.Step);
        Assert.Equal(new[] { 4, 0, 2 }, screen.Chosen);
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal(FactoryStep.Browse, screen.Step);
        Assert.Equal(new[] { 4, 0 }, screen.Chosen);
        RentAt(1);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Equal(new[] { 4, 0, 1 }, screen.Outcome);
        challenge.Rent(screen.Outcome);
        Assert.Equal(challenge.Offer[4].Set, challenge.Team[0].Set);
    }

    [Fact]
    public void ATradeIsChosenBackedOutOfAndMadeOnTheScreen()
    {
        var records = new FrontierRecords();
        var challenge = InRound(records, 0);
        challenge.Rent(new[] { 0, 1, 2 });
        challenge.Won(records);
        var screen = new FactoryScreen();
        void Open() => screen.OpenToTrade(Built(challenge, challenge.Team), Built(challenge, challenge.Foe), "Jim");

        // The way out, asked twice over: no goes back, yes leaves with nothing traded
        Open();
        Assert.Equal((FactoryScreenMode.Trade, 3, 3), (screen.Mode, screen.Mine.Count, screen.Theirs.Count));
        screen.Cancel();
        Assert.Equal((FactoryStep.Quit, 1), (screen.Step, screen.MenuCursor));
        screen.Confirm();
        Assert.Equal(FactoryStep.Browse, screen.Step);
        screen.Move(-1, 0);
        Assert.Equal(screen.CancelPlace, screen.Cursor);
        screen.Confirm();
        screen.Move(-1, 0);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Empty(screen.Outcome);

        // One of one's own to give up: its menu is SUMMARY, EXCHANGE, CANCEL
        Open();
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal(new[] { FactoryOption.Summary, FactoryOption.Exchange, FactoryOption.Cancel }, screen.Options);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal((FactoryStep.Receive, 1, 0), (screen.Step, screen.Giving, screen.TheirCursor));
        // BACK lets another be chosen
        screen.Move(-1, 0);
        screen.Move(-1, 0);
        Assert.Equal(screen.BackPlace, screen.TheirCursor);
        screen.Confirm();
        Assert.Equal((FactoryStep.Browse, -1), (screen.Step, screen.Giving));
        screen.Move(1, 0);
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(2, screen.Giving);
        // One of the beaten three: a no goes back to them, a yes makes the trade and says so
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal(FactoryStep.Accept, screen.Step);
        Assert.Contains(challenge.Foe[1].Species, screen.Prompt);
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal(FactoryStep.Receive, screen.Step);
        screen.Confirm();
        screen.Confirm();
        Assert.Equal(FactoryStep.Traded, screen.Step);
        Assert.Contains("was traded for", screen.Prompt);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Equal(new[] { 2, 1 }, screen.Outcome);
    }

    // ------------------------------------------------------------------ the rooms

    [Fact]
    public void TheFactorysRoomsAreLaidOutAsTheOriginalHasThem()
    {
        // The lobby (events_battle_factory, land data 321): come into from the west, three tiles of mat leading back
        // out onto the grounds before the Factory's porch
        var lobby = Room("BattleFactory");
        Assert.Equal((27, 14, true), (lobby.Width, lobby.Height, lobby.IsIndoors));
        var out_ = lobby.Warps.Where(w => w.TargetMap == "BattleFrontier").ToList();
        Assert.Equal(new[] { (0, 6), (0, 7), (0, 8) }, out_.Select(w => (w.SourceX, w.SourceY)));
        Assert.All(out_, w => Assert.Equal((72, 34, Direction.Left), (w.TargetX, w.TargetY, w.TargetFacing)));
        // The original comes down three flights of stairs here; the floor is level in this game (the rulings), and
        // where the rails of each flight stand either side of the aisle, benches do
        Assert.Equal(lobby.HeightAt(1, 7), lobby.HeightAt(13, 7));
        Assert.All(new[] { 3, 4, 7, 8, 11, 12 }, x => Assert.True(lobby.IsSolid(x, 6) && lobby.IsSolid(x, 8) && !lobby.IsSolid(x, 7)));
        Assert.Equal(new[] { (3, 6), (3, 8), (7, 6), (7, 8), (11, 6), (11, 8) }, lobby.Props.Where(p => p.Type == PropType.Bench).Select(p => (p.X, p.Y)).OrderBy(p => p));
        // The counter, with an attendant in each gap, the records' machine between them and the door behind
        Assert.All(new[] { 19, 20, 22, 24 }, x => Assert.True(lobby.IsSolid(x, 6)));
        Assert.Equal("RecordsMachine", lobby.SignScripts[(22, 6)]);
        var inward = Assert.Single(lobby.Warps, w => w.TargetMap == "BattleFactoryCorridor");
        Assert.Equal((22, 2, 8, 11), (inward.SourceX, inward.SourceY, inward.TargetX, inward.TargetY));
        Assert.Equal(new[] { "MultiAttendant", "SingleAttendant" }, lobby.Everyone.Where(n => n.Key!.EndsWith("attendant")).Select(n => n.Script).OrderBy(s => s));
        Assert.Equal(9, lobby.Everyone.Count());
        Assert.All(lobby.Everyone, n => Assert.NotNull(ScriptLibrary.Default.Find(n.Script!, "BattleFactory")));

        // The corridor and the battle room, laid out from the scene's own places: the staff before the door at the
        // back, the challenger at the west end of the battle room's floor and the trainer at the east
        var corridor = Room("BattleFactoryCorridor");
        var staff = Assert.Single(corridor.Everyone);
        Assert.Equal((8, 5, Direction.Down), (staff.GridX, staff.GridY, staff.Facing));
        Assert.Equal(new[] { "BattleFactory", "BattleFactoryBattleRoom" }, corridor.Warps.Select(w => w.TargetMap).OrderBy(n => n));
        var room = Room("BattleFactoryBattleRoom");
        var opponent = Assert.Single(room.Everyone);
        Assert.Equal(("opponent", "FLAG_HIDE_BATTLE_FACTORY_BATTLE_ROOM_OPPONENT"), (opponent.Key, opponent.HiddenBy));
        Assert.True(room.IsWalkable(4, 8) && room.IsWalkable(12, 8));
        Assert.All(new[] { "BattleFactory", "BattleFactoryCorridor", "BattleFactoryBattleRoom" }, name =>
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id));
    }

    // ------------------------------------------------------------------ a challenge played by its scripts

    private static void Play(HeadlessScriptHost host, ScriptRunner runner, string place, string script, string? who = null)
    {
        if (host.Map?.Name != place) host.Map = Room(place);
        runner.Start(ScriptLibrary.Default.Find(script, place)!, who == null ? null : host.Map!.Everyone.First(n => n.Key == who));
        runner.RunToEnd();
    }

    private static string LastWarp(HeadlessScriptHost host) => host.Log.Last(l => l.StartsWith("warp ")).Split(' ')[1];

    /// <summary>Plays the arrival script of wherever the last warp led, room after room, until the lobby's has been played.</summary>
    private static void PlayOn(HeadlessScriptHost host, ScriptRunner runner)
    {
        for (int i = 0; i < 80; i++)
        {
            string next = LastWarp(host);
            Play(host, runner, next, "OnEnter");
            if (next == "BattleFactory") return;
        }
        throw new InvalidOperationException("The challenge never came back to the lobby");
    }

    /// <summary>The counter's talk: the challenge and the level by their places in the menus, and yes to the save.</summary>
    private static void Take(HeadlessScriptHost host, ScriptRunner runner, int way = 0, int level = 0)
    {
        host.Answers.Enqueue(way);
        host.Answers.Enqueue(level);
        host.Answers.Enqueue(0);
        Play(host, runner, "BattleFactory", "SingleAttendant", "single_attendant");
    }

    [Fact]
    public void ASingleChallengeIsPlayedFromTheCounterToTheBattlePoints()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        Take(host, runner);

        // The counter: the challenge begun, the game saved, the round drawn, the way in
        var factory = host.Factory!;
        Assert.Equal((FrontierChallenge.Single, false, 14, 6, 3), (factory.Way, factory.OpenLevel, factory.Trainers.Count, factory.Offer.Count, factory.NextFoe.Count));
        Assert.Contains("frontier save 0", host.Log);
        Assert.Contains($"frontier journal {(int)FrontierFacility.Factory}", host.Log);
        Assert.Equal((255, 0, 0, 1), (host.Story.Var("VAR_BATTLE_FACTORY_LOBBY_LOAD_ACTION"), host.Story.Var(BattleFactoryChallenge.WayVariable),
            host.Story.Var(BattleFactoryChallenge.LevelVariable), host.Frontier.ChallengesStarted));
        Assert.Equal("BattleFactoryCorridor", LastWarp(host));
        var trainers = factory.Trainers.Take(7).ToList();
        var offered = factory.Offer.Select(r => r.Species).ToList();
        var first = factory.NextFoe.Select(r => r.Species).ToList();

        PlayOn(host, runner);
        // The corridor told of all three of the first trainer's, and the first three of the six were rented
        Assert.True(Said(host, $"Their Pokémon are {first[0]}, {first[1]} and {first[2]}."));
        Assert.Contains("frontier rent 3", host.Log);
        // Seven battles, each with three rentals at Level 50 against the trainer of its place with three
        Assert.Equal(7, host.FrontierBattles.Count);
        Assert.Equal(offered.Take(3), host.FrontierBattles[0].Team.Members.Select(p => p.Species.Name));
        Assert.All(host.FrontierBattles, b => Assert.Equal((3, 3, false, 50), (b.Team.Count, b.Foes.Single().Party.Count, b.Doubles, b.Team.Members[0].Level)));
        Assert.Equal(trainers.Select(t => "frontier_" + FrontierDatabase.Trainer(t).Id), host.FrontierBattles.Select(b => b.Foes[0].Id));
        Assert.Equal(first, host.FrontierBattles[0].Foes[0].Party.Members.Select(p => p.Species.Name));
        // The first round's trainers choose at random
        Assert.All(host.FrontierBattles, b => Assert.Equal(AiFlags.None, b.Foes[0].Ai));
        // No two battles in a row against the same three, and nobody meets a species or an item of their own
        Assert.All(host.FrontierBattles, b => Assert.Empty(b.Team.Members.Select(p => p.Species.Name).Intersect(b.Foes[0].Party.Members.Select(p => p.Species.Name))));
        // Between battles: restored six times, a trade offered six times and none made
        Assert.Equal(6, host.Transcript.Count(l => l.Text.Contains("restored to full health")));
        Assert.Equal(6, host.Log.Count(l => l == "frontier trade 0"));
        // The records, the score, the Battle Points and the lobby
        Assert.Equal((7, 7, 1, 1, true), (host.Frontier.Get(FrontierStat.FactoryLatestStreak50Single), host.Frontier.Get(FrontierStat.FactoryRecordStreak50Single),
            host.Frontier.Get(FrontierStat.FactoryLatestTradeCount50Single), host.Frontier.Get(FrontierStat.FactoryRecordTradeCount50Single),
            host.Frontier.IsRunning(FrontierFacility.Factory, 0)));
        Assert.Equal((5, 7, BattleFactoryChallenge.RoundScore), (host.Frontier.BattlePoints, host.Frontier.VictoriesAt(FrontierFacility.Factory), host.FrontierScore));
        Assert.True(Said(host, "received 5 BP"));
        Assert.True(Said(host, "all seven"));
        Assert.Null(host.Factory);
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_FACTORY_LOBBY_LOAD_ACTION"));
        Assert.Equal(2, host.Log.Count(l => l == "frontier save 0"));

        // The next challenge takes the streak up: the second round, whose staff tell of two
        host.Transcript.Clear();
        Take(host, runner);
        Assert.Equal((7, 1, 1), (host.Factory!.Streak, host.Factory.Round, host.Factory.Trades));
        Play(host, runner, "BattleFactoryCorridor", "OnEnter");
        Assert.True(Said(host, "Two of their Pokémon are"));
    }

    [Fact]
    public void ADoubleChallengeAtTheOpenLevelPutsTwoOutAtLevelAHundred()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        Take(host, runner, way: 1, level: 1);
        Assert.Equal((FrontierChallenge.Double, true, 1, 1), (host.Factory!.Way, host.Factory.OpenLevel, host.Story.Var(BattleFactoryChallenge.WayVariable), host.Story.Var(BattleFactoryChallenge.LevelVariable)));
        PlayOn(host, runner);
        Assert.Equal(7, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.True(b.Doubles && b.Team.Members.Concat(b.Foes[0].Party.Members).All(p => p.Level == 100)));
        Assert.Equal((7, true, false), (host.Frontier.Get(FrontierStat.FactoryRecordStreakOpenDouble), host.Frontier.IsRunning(FrontierFacility.FactoryOpen, 1), host.Frontier.IsRunning(FrontierFacility.Factory, 1)));
    }

    [Fact]
    public void ATradeAskedForInTheCorridorIsMade()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // The six: the last three; then after the first battle the second in hand for the third of the beaten
        host.Entering.Enqueue(new[] { 5, 4, 3 });
        host.Entering.Enqueue(new[] { 1, 2 });
        Take(host, runner);
        var offered = host.Factory!.Offer.Select(r => r.Species).ToList();
        PlayOn(host, runner);
        Assert.Equal(new[] { offered[5], offered[4], offered[3] }, host.FrontierBattles[0].Team.Members.Select(p => p.Species.Name));
        string taken = host.FrontierBattles[0].Foes[0].Party.Members[2].Species.Name;
        Assert.Equal(new[] { offered[5], taken, offered[3] }, host.FrontierBattles[1].Team.Members.Select(p => p.Species.Name));
        Assert.Equal(new[] { offered[5], taken, offered[3] }, host.FrontierBattles[6].Team.Members.Select(p => p.Species.Name));
        // The rental and the one trade
        Assert.Equal((2, 1), (host.Frontier.Get(FrontierStat.FactoryLatestTradeCount50Single), host.Frontier.Factory.Trades));
    }

    [Fact]
    public void TheFactoryHeadIsMetAtTheTwentyFirstBattleAndLeavesHisPrint()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // Two rounds won already
        host.Frontier.Set(FrontierStat.FactoryLatestStreak50Single, 14);
        host.Frontier.Set(FrontierStat.FactoryRecordStreak50Single, 14);
        host.Frontier.SetRunning(FrontierFacility.Factory, 0, true);
        host.Rolls.Enqueue(32);
        Take(host, runner);
        var rented = host.Factory!.Offer.Take(3).Select(r => r.Species).ToList();
        PlayOn(host, runner);

        Assert.Equal(7, host.FrontierBattles.Count);
        var last = host.FrontierBattles[6].Foes[0];
        Assert.Equal(("frontier_factory_head_thorton_silver", "Factory Head", FrontierTrainers.Mind), (last.Id, last.TrainerClass, last.Ai));
        // The trainers before him think with the basic routine alone
        Assert.All(host.FrontierBattles.Take(6), b => Assert.Equal(AiFlags.Basic, b.Foes[0].Ai));
        // He is announced once, names the three rentals and the number his machine came up with (GetRandom 90, and ten)
        Assert.Equal(1, host.Transcript.Count(l => l.Text.Contains("The Factory Head himself")));
        Assert.True(Said(host, $"{rented[0]}. {rented[1]}. {rented[2]}."));
        Assert.True(Said(host, "42 per cent"));
        Assert.Equal(1, host.Frontier.BrainsMet);
        // Twenty Battle Points, the silver print handed over in the lobby, and a streak of 21 running
        Assert.True(Said(host, "received 20 BP"));
        Assert.True(Said(host, "Silver Print"));
        Assert.Equal((2, FrontierPrint.Silver, 20, 21), (host.Story.Var("VAR_BATTLE_FACTORY_PRINT_STATE"), FrontierBrains.PrintOf(host.Story, FrontierFacility.Factory),
            host.Frontier.BattlePoints, host.Frontier.Get(FrontierStat.FactoryRecordStreak50Single)));
        Assert.True(FrontierBrains.AnyPrint(host.Story));
    }

    [Fact]
    public void AChallengeIsPutDownTakenUpAgainAndGivenUp()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // The corridor's menu after the second battle: REST, and yes; the lobby's question: not now
        int menus = 0;
        host.Decide = (asked, answers) => asked.Contains("Are you ready?") ? (++menus == 2 ? 1 : 0) : asked.Contains("go on with it now") ? 1 : 0;
        Take(host, runner);
        var team = host.Factory!.Offer.Take(3).Select(r => r.Set).ToList();
        PlayOn(host, runner);
        Assert.Equal(2, host.FrontierBattles.Count);
        Assert.Null(host.Factory);
        var paused = host.Frontier.Factory.Paused!;
        Assert.Equal(2, paused.Battle);
        Assert.Equal(team, paused.Team.Select(r => r.Set));
        Assert.Equal((2, 2, 0), (host.Story.Var("VAR_BATTLE_FACTORY_LOBBY_LOAD_ACTION"), host.Frontier.Get(FrontierStat.FactoryLatestStreak50Single), host.Frontier.Get(FrontierStat.FactoryRecordStreak50Single)));
        Assert.True(Said(host, "Speak to me whenever you are ready"));

        // Spoken to again, she takes it up: the menu comes first, with no rentals chosen anew; this time RETIRE
        menus = 10;
        host.Decide = (asked, answers) => asked.Contains("Are you ready?") ? 2 : 0;
        host.Transcript.Clear();
        Play(host, runner, "BattleFactory", "SingleAttendant", "single_attendant");
        Assert.Equal((true, 2), (host.Factory!.Resumed, host.Factory.Battle));
        Assert.Equal(team, host.Factory.Team.Select(r => r.Set));
        Assert.Equal("BattleFactoryCorridor", LastWarp(host));
        PlayOn(host, runner);
        Assert.Equal(2, host.FrontierBattles.Count);
        Assert.Equal(1, host.Log.Count(l => l == "frontier rent 3"));
        Assert.True(Said(host, "Thank you for taking the challenge"));
        Assert.Equal((2, 2, false, 0), (host.Frontier.Get(FrontierStat.FactoryRecordStreak50Single), host.Frontier.Get(FrontierStat.FactoryLatestStreak50Single),
            host.Frontier.IsRunning(FrontierFacility.Factory, 0), host.Frontier.BattlePoints));
        Assert.Null(host.Factory);
        Assert.Null(host.Frontier.Factory.Paused);
    }

    [Fact]
    public void ABattleLostEndsTheChallengeAndAGameClosedInTheMiddleEndsTheStreak()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        Take(host, runner);
        PlayOn(host, runner);
        Assert.True(host.Frontier.IsRunning(FrontierFacility.Factory, 0));

        // The second round: the third battle is lost
        int fought = 0;
        host.Fight = _ => ++fought < 3 ? BattleOutcome.Won : BattleOutcome.Lost;
        host.FrontierBattles.Clear();
        host.Transcript.Clear();
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.Equal((9, 9, false, 5), (host.Frontier.Get(FrontierStat.FactoryRecordStreak50Single), host.Frontier.Get(FrontierStat.FactoryLatestStreak50Single),
            host.Frontier.IsRunning(FrontierFacility.Factory, 0), host.Frontier.BattlePoints));
        Assert.True(Said(host, "Thank you for taking the challenge"));
        Assert.False(Said(host, "all seven"));

        // A challenge under way when the game was closed: the lobby finds its mark, and the streak is over
        host.Fight = null;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.True(host.Frontier.IsRunning(FrontierFacility.Factory, 0));
        Take(host, runner);
        host.Factory = null;
        host.Transcript.Clear();
        Play(host, runner, "BattleFactory", "OnEnter");
        Assert.True(Said(host, "left before it was over"));
        Assert.Equal((false, 0, 0), (host.Frontier.IsRunning(FrontierFacility.Factory, 0), host.Frontier.Get(FrontierStat.FactoryLatestStreak50Single), host.Story.Var("VAR_BATTLE_FACTORY_LOBBY_LOAD_ACTION")));
    }

    [Fact]
    public void TheLobbysOtherPeopleAndItsMachine()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // The second counter's challenge needs another player, and says so (decision 06 · 7)
        Play(host, runner, "BattleFactory", "MultiAttendant", "multi_attendant");
        Assert.True(Said(host, "nobody can be linked up with"));
        Assert.Null(host.Factory);
        Play(host, runner, "BattleFactory", "RecordsMachine");
        Assert.Contains($"frontier records {(int)FrontierFacility.Factory}", host.Log);
        // INFO ends the talk
        host.Answers.Enqueue(2);
        Play(host, runner, "BattleFactory", "SingleAttendant", "single_attendant");
        Assert.True(Said(host, "six rental Pokémon"));
        Assert.Null(host.Factory);
        // A challenge the game isn't saved for is let go of
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(1);
        Play(host, runner, "BattleFactory", "SingleAttendant", "single_attendant");
        Assert.Null(host.Factory);
        Assert.Equal(0, host.Frontier.ChallengesStarted);
    }

    [Fact]
    public void TheFactoryCommandsSayWhatIsWrong()
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, new HeadlessScriptHost());
        var script = ScriptParser.Parse("test", "script A\n  factory hint\n  end\n")[0];
        runner.Start(script, null);
        Assert.Throws<ScriptException>(() => runner.RunToEnd());
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script A\n  factory begin multi 50\n  end\n"));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script A\n  factory begin single\n  end\n"));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script A\n  factory swap\n  end\n"));
    }
}
