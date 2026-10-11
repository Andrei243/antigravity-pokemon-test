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
/// The Battle Castle (plan 06 · R18c). The numbers are the decompilation's (<c>src/overlay104/battle_castle.c</c>,
/// <c>battle_castle_helpers.c</c>, <c>frscrcmd_battle_castle.c</c>, <c>src/battle_castle_save.c</c>, the valets' two
/// applications, <c>src/applications/frontier/battle_castle/self_app.c</c> and <c>opponent_app.c</c>,
/// <c>scripts_battle_castle.s</c> and <c>frontier_scripts_battle_castle.s</c>).
/// </summary>
[Collection("MapDatabase")]
public class BattleCastleTests
{
    private const int Silver = 313, Gold = 314;
    private const FrontierChallenge Single = FrontierChallenge.Single, Double = FrontierChallenge.Double;

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static bool Said(HeadlessScriptHost host, string part) => host.Transcript.Any(l => l.Text.Contains(part));

    /// <summary>The draws a test hands over one after another, and then a generator's.</summary>
    private static Func<int> Draws(int seed, params int[] values)
    {
        int at = 0;
        var random = new Random(seed);
        return () => at < values.Length ? values[at++] : random.Next(0x10000);
    }

    private static Pokemon Mon(string species, int level, string? item = null)
    {
        var pokemon = new Pokemon(PokemonDatabase.Get(species)!, level);
        if (item != null) pokemon.HeldItem = ItemDatabase.Get(item);
        return pokemon;
    }

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    /// <summary>Three that may go in: one over level 50 holding something, one at it, one under it.</summary>
    private static Party Three() => PartyOf(Mon("Garchomp", 60, "Leftovers"), Mon("Luxray", 50, "Sitrus Berry"), Mon("Staraptor", 45));

    private static readonly int[] All = { 0, 1, 2 };

    /// <summary>A challenge begun and its first round drawn.</summary>
    private static BattleCastleChallenge Begun(FrontierRecords records, FrontierChallenge way = Single, int seed = 1)
    {
        var challenge = BattleCastleChallenge.Begin(records, way, Three(), All);
        challenge.DrawRound(Draws(seed));
        return challenge;
    }

    // ------------------------------------------------------------------ the tables

    [Fact]
    public void TheCastlesTablesAreTheOriginals()
    {
        var castle = FrontierDatabase.Castle;
        // sTrainerIDRanges: each round's trainers, and the last of each seven from further on
        Assert.Equal(8, castle.Opponents.Count);
        Assert.Equal(new[] { 0, 99, 100, 119 }, castle.Opponents[0]);
        Assert.Equal(new[] { 80, 119, 120, 139 }, castle.Opponents[1]);
        Assert.Equal(new[] { 180, 219, 220, 239 }, castle.Opponents[6]);
        Assert.Equal(new[] { 200, 299, 200, 299 }, castle.Opponents[7]);
        // sBerriesForRent with sBerryPrices, sItemsForRent with sItemPrices, and how many of each a rank lends
        Assert.Equal((32, 27), (castle.Berries.Count, castle.Items.Count));
        Assert.Equal(new[] { ("Cheri Berry", 2), ("Chesto Berry", 2), ("Pecha Berry", 2), ("Rawst Berry", 2), ("Aspear Berry", 2), ("Persim Berry", 2), ("Lum Berry", 5), ("Sitrus Berry", 5), ("Liechi Berry", 5) },
            castle.Berries.Take(9).Select(b => (b.Item, b.Price)));
        Assert.Equal(new[] { ("King's Rock", 10), ("Quick Claw", 15), ("Power Herb", 5) }, castle.Items.Take(3).Select(i => (i.Item, i.Price)));
        Assert.Equal(new[] { ("Light Ball", 15), ("Thick Club", 15), ("White Herb", 5), ("Focus Band", 15), ("Focus Sash", 10), ("Leftovers", 20) }, castle.Items.Skip(10).Take(6).Select(i => (i.Item, i.Price)));
        Assert.Equal(new[] { 8, 8, 32 }, castle.BerriesByRank);
        Assert.Equal(new[] { 12, 12, 27 }, castle.ItemsByRank);
        Assert.All(castle.Berries.Concat(castle.Items), lent => Assert.True(ItemDatabase.Get(lent.Item) != null && lent.Price > 0));

        // The prices of the counters (sHealingCosts, sRankUpCosts, CP_COST_…), the Battle Points and the Castle Valet
        Assert.Equal(new[] { 10, 8, 12 }, BattleCastleChallenge.HealPrices);
        Assert.Equal((1, 1, 15, 2, 5, 50, 10), (BattleCastleChallenge.IdentifyPrice, BattleCastleChallenge.LevelUpPrice, BattleCastleChallenge.LevelDownPrice,
            BattleCastleChallenge.StatsPrice, BattleCastleChallenge.MovesPrice, BattleCastleChallenge.PassPrice, BattleCastleChallenge.RoundPoints));
        Assert.Equal(new[] { 0, 3, 3, 4, 4, 5, 5, 7, 7 }, BattleCastleChallenge.PointsByRound);
        Assert.Equal((Silver, Gold), FrontierBrains.Trainers(FrontierFacility.Castle));
        Assert.Equal((21, 49), FrontierBrains.Battles(FrontierFacility.Castle));
        Assert.Equal(("Castle Valet", "Darach"), (FrontierDatabase.Trainer(Silver).Class, FrontierDatabase.Trainer(Silver).Name));
        // The stats: eight to a way, in the original's order
        Assert.Equal((FrontierStat.CastleRecordStreakSingle, FrontierStat.CastleRecordStreakDouble), (BattleCastleChallenge.RecordStreakStat(Single), BattleCastleChallenge.RecordStreakStat(Double)));
        Assert.Equal(8, (int)BattleCastleChallenge.RecordStreakStat(Double) - (int)BattleCastleChallenge.RecordStreakStat(Single));
        Assert.Equal(7, (int)BattleCastleChallenge.RankStat(Single, CastleRank.Summary) - (int)BattleCastleChallenge.RecordStreakStat(Single));
    }

    // ------------------------------------------------------------------ who may go in, and how

    [Fact]
    public void ThreePokemonOfDifferentSpeciesGoIn()
    {
        // BC_LOBBY_FUNC_CHECK_PARTY_ELIGIBLE: three that may enter, no two of a species; what they hold doesn't matter
        var party = PartyOf(Mon("Garchomp", 60, "Leftovers"), Mon("Luxray", 50, "Leftovers"), Mon("Bidoof", 3, "Leftovers"));
        Assert.True(BattleCastleChallenge.EnoughToEnter(party));
        Assert.True(BattleCastleChallenge.ValidEntry(party, All));
        Assert.False(BattleCastleChallenge.EnoughToEnter(PartyOf(Mon("Garchomp", 60), Mon("Luxray", 50))));
        Assert.False(BattleCastleChallenge.EnoughToEnter(PartyOf(Mon("Garchomp", 60), Mon("Luxray", 50), Mon("Luxray", 44))));
        Assert.False(BattleCastleChallenge.EnoughToEnter(PartyOf(Mon("Garchomp", 60), Mon("Luxray", 50), Mon("Dialga", 70))));
        var egg = Mon("Togepi", 5);
        egg.IsEgg = true;
        Assert.False(BattleCastleChallenge.EnoughToEnter(PartyOf(Mon("Garchomp", 60), Mon("Luxray", 50), egg)));
        var four = PartyOf(Mon("Garchomp", 60), Mon("Luxray", 50), Mon("Luxray", 44), Mon("Staraptor", 45));
        Assert.True(BattleCastleChallenge.EnoughToEnter(four));
        Assert.False(BattleCastleChallenge.ValidEntry(four, All));
        Assert.True(BattleCastleChallenge.ValidEntry(four, new[] { 0, 2, 3 }));
        Assert.False(BattleCastleChallenge.ValidEntry(four, new[] { 0, 3 }));
        Assert.False(BattleCastleChallenge.ValidEntry(four, new[] { 0, 0, 3 }));
        Assert.Throws<InvalidOperationException>(() => BattleCastleChallenge.Begin(new FrontierRecords(), FrontierChallenge.Multi, four, new[] { 0, 2, 3 }));
    }

    [Fact]
    public void AChallengeBeginsWithTenCastlePointsAndNothingHeld()
    {
        // BattleCastle_Init: the points nought and every rank the first, then ten points for the round; the three as
        // copies, at level 50 where they were above it, holding nothing
        var records = new FrontierRecords();
        var party = Three();
        var challenge = BattleCastleChallenge.Begin(records, Single, party, All);
        Assert.Equal((0, 0, 0, 10), (challenge.Battle, challenge.Streak, challenge.Round, challenge.Points(records)));
        Assert.Equal((1, 1, 1), (challenge.Rank(records, CastleRank.Healing), challenge.Rank(records, CastleRank.Rentals), challenge.Rank(records, CastleRank.Summary)));
        Assert.Equal(new[] { ("Garchomp", 50), ("Luxray", 50), ("Staraptor", 45) }, challenge.Team.Members.Select(p => (p.Species.Name, p.Level)));
        Assert.All(challenge.Team.Members, p => Assert.True(p.HeldItem == null && p.CurrentHP == p.MaxHP));
        // The player's own are as they were
        Assert.Equal((60, "Leftovers", "Sitrus Berry"), (party.Members[0].Level, party.Members[0].HeldItem?.Name, party.Members[1].HeldItem?.Name));
        Assert.DoesNotContain(party.Members[0], challenge.Team.Members);

        // A streak still running carries its wins, its Castle Points and its ranks into the next round, with ten more
        records.SetRunning(FrontierFacility.Castle, (int)Single, true);
        records.Set(BattleCastleChallenge.LatestStreakStat(Single), 15);
        records.Set(BattleCastleChallenge.LatestPointsStat(Single), 37);
        records.Set(BattleCastleChallenge.RankStat(Single, CastleRank.Healing), 2);
        var next = BattleCastleChallenge.Begin(records, Single, party, All);
        Assert.Equal((0, 15, 2, 47, 2, 1), (next.Battle, next.Streak, next.Round, next.Points(records), next.Rank(records, CastleRank.Healing), next.Rank(records, CastleRank.Rentals)));
        // The other way of taking the challenge has points and ranks of its own
        var twice = BattleCastleChallenge.Begin(records, Double, party, All);
        Assert.Equal((0, 10, 1, 47), (twice.Streak, twice.Points(records), twice.Rank(records, CastleRank.Healing), records.Get(BattleCastleChallenge.LatestPointsStat(Single))));
        // One that isn't running starts from nothing
        records.SetRunning(FrontierFacility.Castle, (int)Single, false);
        var fresh = BattleCastleChallenge.Begin(records, Single, party, All);
        Assert.Equal((0, 0, 10, 1), (fresh.Streak, fresh.Round, fresh.Points(records), fresh.Rank(records, CastleRank.Healing)));
    }

    // ------------------------------------------------------------------ the trainers

    [Fact]
    public void ARoundsTrainersAreDrawnFromItsRowAndTheLastOfEachSevenFromFurtherOn()
    {
        // BattleCastle_GetOpponentTrainerID. The first round's row is trainers 0 to 99 and, for the seventh, 100 to
        // 119; the draw is taken modulo the row's span, which leaves its last trainer out
        Assert.Equal(5, BattleCastleChallenge.TrainerFor(Single, 0, 0, () => 5));
        Assert.Equal(98, BattleCastleChallenge.TrainerFor(Single, 0, 5, () => 98));
        Assert.Equal(0, BattleCastleChallenge.TrainerFor(Single, 0, 5, () => 99));
        Assert.Equal(105, BattleCastleChallenge.TrainerFor(Single, 0, 6, () => 5));
        Assert.Equal(100, BattleCastleChallenge.TrainerFor(Single, 0, 13, () => 19));
        Assert.Equal(85, BattleCastleChallenge.TrainerFor(Single, 1, 7, () => 5));
        // The eighth round's row is the last, for every round after it too: 200 to 299, whatever the place
        Assert.Equal(205, BattleCastleChallenge.TrainerFor(Double, 7, 6, () => 5));
        Assert.Equal(205, BattleCastleChallenge.TrainerFor(Double, 30, 0, () => 5));
        // The Castle Valet stands at the place that is a Single streak's 21st battle, and its 49th
        Assert.Equal(Silver, BattleCastleChallenge.TrainerFor(Single, 2, 6, () => 5));
        Assert.Equal(Gold, BattleCastleChallenge.TrainerFor(Single, 6, 6, () => 5));
        Assert.Equal(145, BattleCastleChallenge.TrainerFor(Double, 2, 6, () => 5));
        Assert.Equal(105, BattleCastleChallenge.TrainerFor(Single, 2, 5, () => 5));

        // BattleCastle_PickOpponentTrainers: fourteen, none twice (a trainer drawn again is drawn afresh)
        var picked = BattleCastleChallenge.PickTrainers(Single, 0, 14, Draws(3, 5, 5, 6));
        Assert.Equal((14, 14, 5, 6), (picked.Count, picked.Distinct().Count(), picked[0], picked[1]));
        Assert.All(picked.Where((_, i) => i != 6 && i != 13), t => Assert.InRange(t, 0, 98));
        Assert.InRange(picked[6], 100, 118);
        Assert.InRange(picked[13], 100, 118);

        // The round drawn: the first trainer has three of their own sets at level 50, none of a species twice
        var challenge = Begun(new FrontierRecords());
        Assert.Equal(14, challenge.Trainers.Count);
        var foe = challenge.Foe!;
        Assert.Equal((challenge.Trainers[0], 3, 3), (foe.Number, foe.Pokemon.Count, foe.Pokemon.Select(p => p.Species.Name).Distinct().Count()));
        Assert.All(foe.Pokemon, p => Assert.Equal(50, p.Level));
        Assert.All(foe.Sets, set => Assert.Contains(set, foe.Record.Sets));
        var trainer = challenge.OpponentForBattle();
        Assert.Equal((3, 0, AiFlags.None), (trainer.Party.Count, trainer.PrizeMoney, trainer.Ai));
    }

    [Fact]
    public void TheTrainersThinkHarderRoundByRound()
    {
        // BattleCastle_GetAIMask
        var expected = new[] { AiFlags.None, AiFlags.None, AiFlags.Basic, AiFlags.Basic, FrontierTrainers.Mind, FrontierTrainers.Mind };
        for (int round = 0; round < expected.Length; round++)
        {
            var records = new FrontierRecords();
            records.SetRunning(FrontierFacility.Castle, (int)Double, true);
            records.Set(BattleCastleChallenge.LatestStreakStat(Double), round * 7);
            var challenge = Begun(records, Double);
            Assert.Equal((round, expected[round]), (challenge.Round, challenge.Mind));
        }
        // The Castle Valet thinks with all three routines, though his round's trainers don't
        var single = new FrontierRecords();
        single.SetRunning(FrontierFacility.Castle, (int)Single, true);
        single.Set(BattleCastleChallenge.LatestStreakStat(Single), 14);
        var third = Begun(single);
        Assert.Equal((Silver, AiFlags.Basic, FrontierPrint.None), (third.Trainers[6], third.Mind, third.DarachNext));
        for (int battle = 0; battle < 6; battle++)
        {
            third.Won(single);
            third.DrawFoe(Draws(battle));
        }
        Assert.Equal((20, FrontierPrint.Silver, FrontierTrainers.Mind, Silver), (third.Streak, third.DarachNext, third.Mind, third.Foe!.Number));
        Assert.Equal(3, third.Foe.Pokemon.Count);
        Assert.All(third.Foe.Sets, set => Assert.Contains(set, new[] { 362, 381, 426, 498, 517, 562 }));
        // He is announced once
        Assert.False(third.AnnounceValet());
        Assert.True(third.AnnounceValet());
    }

    // ------------------------------------------------------------------ Castle Points

    [Fact]
    public void ABattlesCastlePointsGoByHowItWasWon()
    {
        // BattleCastle_CalcCPEarnedFromBattle. Untouched: each of the three standing (3), at full HP (3), with no
        // status (1): 21; and no PP used at all: 8 more
        var records = new FrontierRecords();
        var challenge = Begun(records);
        var team = challenge.Team.Members;
        Assert.Equal(29, challenge.PointsFromBattle());
        Assert.False(challenge.Fainted);

        // The first at exactly half its HP (2 for that, by whole-number halving), having used six PP; the second
        // fainted; the third with a single HP and poisoned: 3+2+1, nothing, 3+1; six PP used is ten or fewer: 6
        team[0].CurrentHP = team[0].MaxHP / 2;
        team[0].Moves[0].CurrentPP -= 6;
        team[1].CurrentHP = 0;
        team[2].CurrentHP = 1;
        team[2].Status = StatusCondition.Poison;
        Assert.Equal(6 + 0 + 4 + 6, challenge.PointsFromBattle());
        Assert.True(challenge.Fainted);
        // One HP under half is worth one
        team[0].CurrentHP = team[0].MaxHP / 2 - 1;
        Assert.Equal(5 + 0 + 4 + 6, challenge.PointsFromBattle());
        // The PP used: five or fewer 8, ten or fewer 6, fifteen or fewer 4, more nothing (whoever fainted counts too)
        team[0].Moves[0].CurrentPP += 1;
        Assert.Equal(5 + 4 + 8, challenge.PointsFromBattle());
        team[1].Moves[0].CurrentPP -= 6;
        Assert.Equal(5 + 4 + 4, challenge.PointsFromBattle());
        team[1].Moves[0].CurrentPP -= 5;
        Assert.Equal(5 + 4 + 0, challenge.PointsFromBattle());
        // Seven for each of the trainer's Pokémon that was made five levels higher, and nothing for one made lower
        challenge.Receive(records, 100);
        Assert.Equal(CastleAnswer.Done, challenge.AdjustLevel(records, 0, up: true));
        Assert.Equal(CastleAnswer.Done, challenge.AdjustLevel(records, 2, up: true));
        Assert.Equal(CastleAnswer.Done, challenge.AdjustLevel(records, 1, up: false));
        Assert.Equal(9 + 14, challenge.PointsFromBattle());
        // Never less than one
        foreach (var p in team) p.CurrentHP = 0;
        challenge.AdjustLevel(records, 0, up: false);
        challenge.AdjustLevel(records, 2, up: false);
        Assert.Equal(1, challenge.PointsFromBattle());
    }

    [Fact]
    public void AWinIsCountedPaidForAndTheThreeMadeReadyForTheNext()
    {
        var records = new FrontierRecords();
        var challenge = Begun(records);
        var team = challenge.Team.Members;
        challenge.Identify(records, 0);
        challenge.AdjustLevel(records, 1, up: true);
        team[0].CurrentHP = 0;
        team[1].Status = StatusCondition.Paralyze;
        team[2].Moves[0].CurrentPP -= 3;
        int hp = team[2].CurrentHP, pp = team[2].Moves[0].CurrentPP;
        // The second: 3+3, no point for a status; the third 3+3+1; three PP used 8; one raised 7. Eight held after the counter
        Assert.Equal(8, challenge.Points(records));
        int earned = challenge.Won(records);
        Assert.Equal(6 + 7 + 8 + 7, earned);
        Assert.Equal((1, 1, 8 + 28, 1, 10 + 28), (challenge.Battle, challenge.Streak, challenge.Points(records), records.VictoriesAt(FrontierFacility.Castle), records.Castle.PointsReceived + 10));
        // BattleCastle_PrepForNextBattle: whoever fainted is back with one HP, every status is gone, nothing else is restored
        Assert.Equal((1, StatusCondition.None, hp, pp), (team[0].CurrentHP, team[1].Status, team[2].CurrentHP, team[2].Moves[0].CurrentPP));
        Assert.True(challenge.Fainted);
        // What was bought about the trainer just beaten is forgotten, and the PP the next battle starts with is noted
        Assert.All(challenge.Identified, Assert.False);
        Assert.All(challenge.Levels, l => Assert.Equal(CastleLevel.Same, l));
        Assert.Equal(pp, challenge.StartingPp[2][0]);
        // The next battle, fought without a scratch: the first has one HP of many (3+1+1), the others are whole
        challenge.DrawFoe(Draws(2));
        Assert.Equal(5 + 7 + 7 + 8, challenge.Won(records));
        Assert.False(challenge.Fainted);
        Assert.False(challenge.RoundDone);
    }

    // ------------------------------------------------------------------ the first counter

    [Fact]
    public void HealingCostsCastlePointsAndItsRanksOpenMoreOfIt()
    {
        var records = new FrontierRecords();
        var challenge = Begun(records);
        var mon = challenge.Team.Members[0];
        mon.CurrentHP = 7;
        mon.Moves[0].CurrentPP = 1;
        // HP for ten points: all ten are spent, and counted as spent
        Assert.Equal(CastleAnswer.Done, challenge.Heal(records, 0, CastleHeal.Hp));
        Assert.Equal((mon.MaxHP, 1, 0, 10), (mon.CurrentHP, mon.Moves[0].CurrentPP, challenge.Points(records), records.Get(BattleCastleChallenge.SpentPointsStat(Single))));
        // The original asks in this order: the rank, the points, and only then whether there is anything to restore
        Assert.Equal(CastleAnswer.RankTooLow, challenge.Heal(records, 0, CastleHeal.Pp));
        Assert.Equal(CastleAnswer.NotEnough, challenge.Heal(records, 0, CastleHeal.Hp));
        challenge.Receive(records, 300);
        Assert.Equal(CastleAnswer.NoUse, challenge.Heal(records, 0, CastleHeal.Hp));
        Assert.Equal(300, challenge.Points(records));
        // The second rank, for a hundred, restores PP for eight
        Assert.Equal(100, challenge.RankPrice(records, CastleRank.Healing));
        Assert.Equal(CastleAnswer.Done, challenge.RankUp(records, CastleRank.Healing));
        Assert.Equal((2, 200), (challenge.Rank(records, CastleRank.Healing), challenge.Points(records)));
        Assert.Equal(CastleAnswer.RankTooLow, challenge.Heal(records, 0, CastleHeal.All));
        Assert.Equal(CastleAnswer.Done, challenge.Heal(records, 0, CastleHeal.Pp));
        Assert.Equal((mon.Moves[0].MaxPP, 192), (mon.Moves[0].CurrentPP, challenge.Points(records)));
        Assert.Equal(CastleAnswer.NoUse, challenge.Heal(records, 0, CastleHeal.Pp));
        // The third, for a hundred more, restores both for twelve; there is no fourth
        Assert.Equal(CastleAnswer.Done, challenge.RankUp(records, CastleRank.Healing));
        Assert.Equal((3, 92, 0), (challenge.Rank(records, CastleRank.Healing), challenge.Points(records), challenge.RankPrice(records, CastleRank.Healing)));
        Assert.Equal(CastleAnswer.NoUse, challenge.RankUp(records, CastleRank.Healing));
        Assert.Equal(CastleAnswer.NoUse, challenge.Heal(records, 0, CastleHeal.All));
        mon.CurrentHP = 3;
        Assert.Equal(CastleAnswer.Done, challenge.Heal(records, 0, CastleHeal.All));
        Assert.Equal((mon.MaxHP, 80), (mon.CurrentHP, challenge.Points(records)));
        mon.Moves[1].CurrentPP = 0;
        Assert.Equal(CastleAnswer.Done, challenge.Heal(records, 0, CastleHeal.All));
        Assert.Equal((mon.Moves[1].MaxPP, 68, 10 + 100 + 8 + 100 + 12 + 12), (mon.Moves[1].CurrentPP, challenge.Points(records), records.Get(BattleCastleChallenge.SpentPointsStat(Single))));
        // A rank the points don't reach
        Assert.Equal(CastleAnswer.NotEnough, challenge.RankUp(records, CastleRank.Rentals));
        Assert.Equal(1, challenge.Rank(records, CastleRank.Rentals));
    }

    [Fact]
    public void BerriesAndItemsAreRentedByTheRentalCountersRank()
    {
        var records = new FrontierRecords();
        var challenge = Begun(records);
        var team = challenge.Team.Members;
        // The first rank lends the first eight berries and no items
        Assert.Equal((8, 0), (challenge.Lent(records, berries: true).Count, challenge.Lent(records, berries: false).Count));
        Assert.Equal(CastleAnswer.Done, challenge.Rent(records, 0, "Cheri Berry"));
        Assert.Equal(("Cheri Berry", 8), (team[0].HeldItem?.Name, challenge.Points(records)));
        Assert.Equal(CastleAnswer.RankTooLow, challenge.Rent(records, 1, "Liechi Berry"));
        Assert.Equal(CastleAnswer.RankTooLow, challenge.Rent(records, 1, "Quick Claw"));
        // One that holds something is asked about first; agreed to, what it held is given up
        Assert.Equal(CastleAnswer.Holding, challenge.Rent(records, 0, "Sitrus Berry"));
        Assert.Equal(8, challenge.Points(records));
        Assert.Equal(CastleAnswer.Done, challenge.Rent(records, 0, "Sitrus Berry", swap: true));
        Assert.Equal(("Sitrus Berry", 3), (team[0].HeldItem?.Name, challenge.Points(records)));
        Assert.Equal(CastleAnswer.NotEnough, challenge.Rent(records, 1, "Lum Berry"));
        Assert.Null(team[1].HeldItem);

        // The second rank, for a hundred: the first twelve items, and the same eight berries
        challenge.Receive(records, 400);
        Assert.Equal(CastleAnswer.Done, challenge.RankUp(records, CastleRank.Rentals));
        Assert.Equal((8, 12, 303), (challenge.Lent(records, berries: true).Count, challenge.Lent(records, berries: false).Count, challenge.Points(records)));
        Assert.Equal(CastleAnswer.Done, challenge.Rent(records, 1, "Quick Claw"));
        Assert.Equal(CastleAnswer.RankTooLow, challenge.Rent(records, 2, "Leftovers"));
        // The third, for a hundred and fifty: every berry and every item
        Assert.Equal(150, challenge.RankPrice(records, CastleRank.Rentals));
        Assert.Equal(CastleAnswer.Done, challenge.RankUp(records, CastleRank.Rentals));
        Assert.Equal((32, 27), (challenge.Lent(records, berries: true).Count, challenge.Lent(records, berries: false).Count));
        Assert.Equal(CastleAnswer.Done, challenge.Rent(records, 2, "Leftovers"));
        Assert.Equal(CastleAnswer.Done, challenge.Rent(records, 1, "Liechi Berry", swap: true));
        Assert.Equal(new[] { "Sitrus Berry", "Liechi Berry", "Leftovers" }, team.Select(p => p.HeldItem?.Name));
        Assert.Equal(303 - 15 - 150 - 20 - 5, challenge.Points(records));
        Assert.Equal(CastleAnswer.NoUse, challenge.RankUp(records, CastleRank.Rentals));
    }

    // ------------------------------------------------------------------ the second counter

    [Fact]
    public void TheTrainersThreeAreToldAndTheirLevelsMovedForCastlePoints()
    {
        var records = new FrontierRecords();
        var challenge = Begun(records);
        var foe = challenge.Foe!.Pokemon;
        // Which Pokémon it is, for a point, once
        Assert.Equal(CastleAnswer.Done, challenge.Identify(records, 1));
        Assert.Equal(CastleAnswer.NoUse, challenge.Identify(records, 1));
        Assert.Equal(new[] { false, true, false }, challenge.Identified);
        Assert.Equal(9, challenge.Points(records));
        // Five levels up for a point; it goes no higher
        int hp50 = foe[0].MaxHP;
        Assert.Equal(CastleAnswer.Done, challenge.AdjustLevel(records, 0, up: true));
        Assert.Equal((55, CastleLevel.Up, 8), (foe[0].Level, challenge.Levels[0], challenge.Points(records)));
        Assert.True(foe[0].MaxHP > hp50 && foe[0].CurrentHP == foe[0].MaxHP);
        Assert.Equal(CastleAnswer.NoUse, challenge.AdjustLevel(records, 0, up: true));
        // Five down costs fifteen, which eight don't reach
        Assert.Equal(CastleAnswer.NotEnough, challenge.AdjustLevel(records, 0, up: false));
        challenge.Receive(records, 100);
        // From 55 it comes back to 50, and from there goes to 45; it goes no lower
        Assert.Equal(CastleAnswer.Done, challenge.AdjustLevel(records, 0, up: false));
        Assert.Equal((50, CastleLevel.Same, 93, hp50), (foe[0].Level, challenge.Levels[0], challenge.Points(records), foe[0].MaxHP));
        Assert.Equal(CastleAnswer.Done, challenge.AdjustLevel(records, 0, up: false));
        Assert.Equal((45, CastleLevel.Down, 78), (foe[0].Level, challenge.Levels[0], challenge.Points(records)));
        Assert.Equal(CastleAnswer.NoUse, challenge.AdjustLevel(records, 0, up: false));
        // Its stats for two points, once: afterwards they are looked at for nothing
        Assert.Equal(CastleAnswer.Done, challenge.ShowStats(records, 2));
        Assert.Equal(CastleAnswer.Done, challenge.ShowStats(records, 2));
        Assert.Equal((true, 76), (challenge.StatsShown[2], challenge.Points(records)));
        // Its moves for five, from the counter's second rank, which costs fifty and is its highest
        Assert.Equal(CastleAnswer.RankTooLow, challenge.ShowMoves(records, 2));
        Assert.Equal(50, challenge.RankPrice(records, CastleRank.Summary));
        Assert.Equal(CastleAnswer.Done, challenge.RankUp(records, CastleRank.Summary));
        Assert.Equal(CastleAnswer.NoUse, challenge.RankUp(records, CastleRank.Summary));
        Assert.Equal(CastleAnswer.Done, challenge.ShowMoves(records, 2));
        Assert.Equal(CastleAnswer.Done, challenge.ShowMoves(records, 2));
        Assert.Equal((true, 21), (challenge.MovesShown[2], challenge.Points(records)));
        // The trainer goes into the battle whole, with the levels as they were left
        foe[1].CurrentHP = 1;
        var trainer = challenge.OpponentForBattle();
        Assert.Equal(new[] { 45, 50, 50 }, trainer.Party.Members.Select(p => p.Level));
        Assert.All(trainer.Party.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
    }

    [Fact]
    public void ABattleIsPassedForFiftyCastlePointsFromTheFourthRound()
    {
        // The first three rounds offer no pass
        var records = new FrontierRecords();
        var challenge = Begun(records);
        challenge.Receive(records, 500);
        Assert.False(challenge.MayPass);
        Assert.Equal(CastleAnswer.RankTooLow, challenge.Pass(records));
        // The fourth does: the battle counts for the round and the streak, but not as a battle won, and earns nothing
        records = new FrontierRecords();
        records.SetRunning(FrontierFacility.Castle, (int)Single, true);
        records.Set(BattleCastleChallenge.LatestStreakStat(Single), 21);
        challenge = Begun(records);
        Assert.Equal((3, true, 10), (challenge.Round, challenge.MayPass, challenge.Points(records)));
        Assert.Equal(CastleAnswer.NotEnough, challenge.Pass(records));
        challenge.Receive(records, 45);
        challenge.Team.Members[0].Status = StatusCondition.Burn;
        Assert.Equal(CastleAnswer.Done, challenge.Pass(records));
        Assert.Equal((1, 22, 5, 0, 50), (challenge.Battle, challenge.Streak, challenge.Points(records), records.VictoriesAt(FrontierFacility.Castle), records.Get(BattleCastleChallenge.SpentPointsStat(Single))));
        Assert.Equal(StatusCondition.None, challenge.Team.Members[0].Status);
        // The Castle Valet is never passed: a Single streak of 48, in the seventh round
        records = new FrontierRecords();
        records.SetRunning(FrontierFacility.Castle, (int)Single, true);
        records.Set(BattleCastleChallenge.LatestStreakStat(Single), 42);
        challenge = Begun(records);
        challenge.Receive(records, 1000);
        for (int battle = 0; battle < 6; battle++) Assert.Equal(CastleAnswer.Done, challenge.Pass(records));
        Assert.Equal((48, FrontierPrint.Gold, false), (challenge.Streak, challenge.DarachNext, challenge.MayPass));
        Assert.Equal(CastleAnswer.RankTooLow, challenge.Pass(records));
        Assert.Equal(Gold, challenge.Trainers[6]);
    }

    // ------------------------------------------------------------------ rounds, records and Battle Points

    private static void WinRound(BattleCastleChallenge challenge, FrontierRecords records, int seed = 3)
    {
        var draws = Draws(seed);
        challenge.DrawRound(draws);
        while (true)
        {
            challenge.Won(records);
            if (challenge.RoundDone) break;
            challenge.DrawFoe(draws);
        }
        challenge.EndRound(records);
    }

    [Fact]
    public void RoundsWonEarnBattlePointsAndTheCastlePointsCarryOn()
    {
        var records = new FrontierRecords();
        var party = Three();
        // sBPPerRoundSolo, by the round the streak goes on to
        int[] expected = { 3, 3, 4, 4, 5, 5, 7, 7, 7, 7 };
        for (int round = 0; round < expected.Length; round++)
        {
            var challenge = BattleCastleChallenge.Begin(records, Double, party, All);
            Assert.Equal((round * 7, round), (challenge.Streak, challenge.Round));
            WinRound(challenge, records, seed: round);
            Assert.Equal(expected[round], challenge.PointsEarned);
            // Ten for the round and twenty-nine for each battle won without a scratch
            int points = (round + 1) * (10 + 7 * 29);
            Assert.Equal(((round + 1) * 7, (round + 1) * 7, points, points, true), (records.Get(BattleCastleChallenge.LatestStreakStat(Double)), records.Get(BattleCastleChallenge.RecordStreakStat(Double)),
                records.Get(BattleCastleChallenge.LatestPointsStat(Double)), records.Get(BattleCastleChallenge.RecordPointsStat(Double)), records.IsRunning(FrontierFacility.Castle, (int)Double)));
        }
        Assert.Equal((70, 0), (records.VictoriesAt(FrontierFacility.Castle), records.Get(BattleCastleChallenge.RecordStreakStat(Single))));
        // A Single round that ends with the Castle Valet beaten earns twenty
        records = new FrontierRecords();
        records.SetRunning(FrontierFacility.Castle, (int)Single, true);
        records.Set(BattleCastleChallenge.LatestStreakStat(Single), 14);
        var third = BattleCastleChallenge.Begin(records, Single, party, All);
        WinRound(third, records);
        Assert.Equal((21, BattleCastleChallenge.DarachPoints), (third.Streak, third.PointsEarned));
        var story = new StoryState();
        third.BeatValet(story, FrontierPrint.Silver);
        Assert.Equal((FrontierPrint.Silver, 1), (third.BeatDarach, story.Var("VAR_BATTLE_CASTLE_PRINT_STATE")));
    }

    [Fact]
    public void ALossEndsTheStreakAndTheRanks()
    {
        var records = new FrontierRecords();
        var party = Three();
        var challenge = BattleCastleChallenge.Begin(records, Single, party, All);
        WinRound(challenge, records);
        challenge = BattleCastleChallenge.Begin(records, Single, party, All);
        challenge.DrawRound(Draws(4));
        challenge.Won(records);
        challenge.DrawFoe(Draws(5));
        challenge.Won(records);
        challenge.RankUp(records, CastleRank.Healing);
        int points = challenge.Points(records);
        Assert.Equal(2 * 10 + 9 * 29 - 100, points);
        // BattleCastle_SaveOnLoss: the streak stands at nine for the record, with the Castle Points it had; every
        // counter is back at its first rank, and it runs no more
        challenge.EndLost(records);
        Assert.Equal((9, 9, points, false, 1), (records.Get(BattleCastleChallenge.LatestStreakStat(Single)), records.Get(BattleCastleChallenge.RecordStreakStat(Single)),
            records.Get(BattleCastleChallenge.RecordPointsStat(Single)), records.IsRunning(FrontierFacility.Castle, (int)Single), records.Get(BattleCastleChallenge.RankStat(Single, CastleRank.Healing))));
        // The next begins with nothing but its ten points; a shorter streak leaves the record and its points alone
        challenge = BattleCastleChallenge.Begin(records, Single, party, All);
        Assert.Equal((0, 10), (challenge.Streak, challenge.Points(records)));
        challenge.DrawRound(Draws(6));
        challenge.Won(records);
        challenge.EndLost(records);
        Assert.Equal((1, 9, points), (records.Get(BattleCastleChallenge.LatestStreakStat(Single)), records.Get(BattleCastleChallenge.RecordStreakStat(Single)), records.Get(BattleCastleChallenge.RecordPointsStat(Single))));
        // A streak let go of (BattleCastle_ClearActiveStreak) loses its Castle Points too
        challenge = BattleCastleChallenge.Begin(records, Single, party, All);
        WinRound(challenge, records);
        Assert.True(records.IsRunning(FrontierFacility.Castle, (int)Single));
        BattleCastleChallenge.Forfeit(records, Single);
        Assert.Equal((false, 0, 0, 9), (records.IsRunning(FrontierFacility.Castle, (int)Single), records.Get(BattleCastleChallenge.LatestStreakStat(Single)),
            records.Get(BattleCastleChallenge.LatestPointsStat(Single)), records.Get(BattleCastleChallenge.RecordStreakStat(Single))));
    }

    [Fact]
    public void AChallengePutDownIsTakenUpWithTheThreeAsTheyStood()
    {
        var records = new FrontierRecords();
        var party = Three();
        var challenge = BattleCastleChallenge.Begin(records, Single, party, All);
        var draws = Draws(8);
        challenge.DrawRound(draws);
        challenge.Rent(records, 1, "Chesto Berry");
        challenge.Team.Members[0].CurrentHP = 33;
        challenge.Team.Members[2].Moves[0].CurrentPP = 2;
        challenge.Won(records);
        challenge.DrawFoe(draws);
        challenge.Won(records);
        var trainers = challenge.Trainers.ToList();
        int points = challenge.Points(records);
        challenge.Pause(records, party);
        // BC_FUNC_QUICKSAVE: the latest streak is written; the record and whether the streak runs are not
        Assert.Equal((2, 0, false), (records.Get(BattleCastleChallenge.LatestStreakStat(Single)), records.Get(BattleCastleChallenge.RecordStreakStat(Single)), records.IsRunning(FrontierFacility.Castle, (int)Single)));
        Assert.True(BattleCastleChallenge.StillValid(records, party));
        // It survives a save
        var saved = GameDataFiles.Deserialize<FrontierRecords>(GameDataFiles.Serialize(records));
        var resumed = BattleCastleChallenge.Resume(saved, party)!;
        Assert.Equal((2, 2, 0, true, points), (resumed.Battle, resumed.Streak, resumed.Round, resumed.Resumed, resumed.Points(saved)));
        Assert.Equal(trainers, resumed.Trainers);
        Assert.Equal((33, "Chesto Berry", 2, 50), (resumed.Team.Members[0].CurrentHP, resumed.Team.Members[1].HeldItem?.Name, resumed.Team.Members[2].Moves[0].CurrentPP, resumed.Team.Members[0].Level));
        Assert.Null(resumed.Team.Members[0].HeldItem);
        Assert.Null(saved.Castle.Paused);
        Assert.Null(BattleCastleChallenge.Resume(saved, party));
        // The trainer to come is drawn as the challenge goes on, and it is no longer one just taken up
        Assert.Null(resumed.Foe);
        resumed.DrawFoe(draws);
        Assert.Equal((trainers[2], false), (resumed.Foe!.Number, resumed.Resumed));
        // The first battle after a rest counts as having used no PP (the original never notes it again): 3+2+1 for
        // the first, at 33 HP of many... which is under half: 3+1+1; the others whole
        Assert.Equal(5 + 7 + 7 + 8, resumed.Won(saved));

        // Only the three it was entered with take it up
        (party.Members[0], party.Members[2]) = (party.Members[2], party.Members[0]);
        Assert.False(BattleCastleChallenge.StillValid(records, party));
    }

    // ------------------------------------------------------------------ the counters' screen

    [Fact]
    public void TheCounterForOnesOwnThreeHealsRentsAndRaisesItsRanks()
    {
        var records = new FrontierRecords();
        var challenge = Begun(records);
        challenge.Receive(records, 200);
        var team = challenge.Team.Members;
        team[0].CurrentHP = 20;
        var screen = new CastleScreen();
        screen.Open(challenge, records, own: true);
        Assert.Equal((true, true, CastleStep.Browse, 0, 210, 3), (screen.IsActive, screen.Own, screen.Step, screen.Cursor, screen.Points, screen.Shown.Count));
        // The cursor goes round the three and the way out
        screen.Move(-1, 0);
        Assert.Equal(CastleScreen.ExitPlace, screen.Cursor);
        Assert.Null(screen.Subject);
        screen.Move(1, 0);
        Assert.Equal((0, "Garchomp"), (screen.Cursor, screen.Subject!.Species.Name));

        // A Pokémon's menu, and the healing counter's under it
        screen.Confirm();
        Assert.Equal(CastleStep.Menu, screen.Step);
        Assert.Equal(new[] { CastleOption.Heal, CastleOption.Rental, CastleOption.Summary, CastleOption.Moves, CastleOption.Cancel }, screen.Options);
        screen.Confirm();
        Assert.Equal(new[] { CastleOption.RestoreHp, CastleOption.RestorePp, CastleOption.RestoreAll, CastleOption.RankUp, CastleOption.Cancel }, screen.Options);
        Assert.Equal((10, 8, 12, 100, 1, 2, 3), (screen.PriceOf(CastleOption.RestoreHp), screen.PriceOf(CastleOption.RestorePp), screen.PriceOf(CastleOption.RestoreAll), screen.PriceOf(CastleOption.RankUp),
            screen.RankNeeded(CastleOption.RestoreHp), screen.RankNeeded(CastleOption.RestorePp), screen.RankNeeded(CastleOption.RestoreAll)));
        // RESTORE HP: asked, refused, asked again and agreed to
        screen.Confirm();
        Assert.Equal((CastleStep.Ask, "That will cost 10 CP. Is that all right?"), (screen.Step, screen.Line));
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal((CastleStep.Menu, 20, 210), (screen.Step, team[0].CurrentHP, screen.Points));
        screen.Confirm();
        screen.Confirm();
        Assert.Equal((CastleStep.Note, "Garchomp's HP was restored.", team[0].MaxHP, 200), (screen.Step, screen.Line, team[0].CurrentHP, screen.Points));
        screen.Confirm();
        Assert.Equal((CastleStep.Menu, 0), (screen.Step, screen.MenuCursor));
        // Again, with nothing to restore
        screen.Confirm();
        screen.Confirm();
        Assert.Equal((CastleStep.Note, "It would have no effect.", 200), (screen.Step, screen.Line, screen.Points));
        screen.Confirm();
        // RESTORE PP wants the second rank
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal((CastleStep.Note, "This counter's rank is too low for that. Raise its rank first."), (screen.Step, screen.Line));
        screen.Confirm();
        // RANK UP
        screen.Move(0, 1);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal((CastleStep.Ask, "Raise this counter's rank for 100 CP?"), (screen.Step, screen.Line));
        screen.Confirm();
        Assert.Equal((CastleStep.Note, "PP can now be restored.", 2, 100), (screen.Step, screen.Line, challenge.Rank(records, CastleRank.Healing), screen.Points));
        screen.Confirm();
        Assert.Equal((CastleStep.Menu, 3), (screen.Step, screen.MenuCursor));
        // Back out to the Pokémon's menu, the cursor on the counter left
        screen.Cancel();
        Assert.Equal((CastleStep.Menu, CastleOption.Cancel, 0), (screen.Step, screen.Opened, screen.MenuCursor));

        // The rental counter: the items want the second rank, the berries are a list
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(new[] { CastleOption.Berries, CastleOption.Items, CastleOption.RankUp, CastleOption.Cancel }, screen.Options);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(CastleStep.Note, screen.Step);
        screen.Confirm();
        screen.Move(0, -1);
        screen.Confirm();
        Assert.Equal((CastleStep.List, 8, 0), (screen.Step, screen.Listed.Count, screen.MenuCursor));
        screen.Move(0, -1);
        Assert.Equal(0, screen.MenuCursor);
        for (int i = 0; i < 7; i++) screen.Move(0, 1);
        screen.Move(0, 1);
        Assert.Equal((7, 1, "Sitrus Berry"), (screen.MenuCursor, screen.ListTop, screen.Listed[screen.MenuCursor].Item));
        screen.Confirm();
        Assert.Equal((CastleStep.Ask, "Sitrus Berry", "That will cost 5 CP. Is that all right?"), (screen.Step, screen.Title, screen.Line));
        screen.Confirm();
        Assert.Equal((CastleStep.Note, "Garchomp was given the Sitrus Berry to hold.", "Sitrus Berry", 95), (screen.Step, screen.Line, team[0].HeldItem?.Name, screen.Points));
        screen.Confirm();
        Assert.Equal((CastleStep.List, 7), (screen.Step, screen.MenuCursor));
        // Another for the same Pokémon: the two are swapped only if agreed to
        screen.Move(0, -1);
        screen.Confirm();
        screen.Confirm();
        Assert.Equal((CastleStep.Ask, "Garchomp is already holding the Sitrus Berry. Swap the two?", 95), (screen.Step, screen.Line, screen.Points));
        screen.Confirm();
        Assert.Equal((CastleStep.Note, "The Sitrus Berry was handed back, and Garchomp was given the Lum Berry to hold.", "Lum Berry", 90), (screen.Step, screen.Line, team[0].HeldItem?.Name, screen.Points));
        screen.Confirm();
        screen.Cancel();
        Assert.Equal((CastleStep.Menu, CastleOption.Rental, 0), (screen.Step, screen.Opened, screen.MenuCursor));
        screen.Cancel();
        Assert.Equal((CastleStep.Menu, CastleOption.Cancel, 1), (screen.Step, screen.Opened, screen.MenuCursor));

        // The summary and the moves are read for nothing
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(CastleStep.Summary, screen.Step);
        // ... one after another, as the team's summary is
        screen.Move(0, 1);
        Assert.Equal((1, "Luxray"), (screen.Cursor, screen.Subject!.Species.Name));
        screen.Move(0, -1);
        Assert.Equal(0, screen.Cursor);
        screen.Cancel();
        Assert.Equal((CastleStep.Menu, 2), (screen.Step, screen.MenuCursor));
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(CastleStep.Moves, screen.Step);
        screen.Confirm();
        // CANCEL, the way out, and out
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(CastleStep.Browse, screen.Step);
        screen.Cancel();
        Assert.Equal((CastleScreen.ExitPlace, true), (screen.Cursor, screen.IsActive));
        screen.Cancel();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void TheCounterAboutTheTrainerTellsWhatIsPaidFor()
    {
        var records = new FrontierRecords();
        var challenge = Begun(records);
        var foe = challenge.Foe!.Pokemon;
        var screen = new CastleScreen();
        screen.Open(challenge, records, own: false);
        Assert.Equal((false, 3, "the Pokémon", foe[0]), (screen.Own, screen.Shown.Count, screen.NameOf(0), screen.Subject));
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal(new[] { CastleOption.Check, CastleOption.Level, CastleOption.Info, CastleOption.Cancel }, screen.Options);
        // IDENTIFY, for a point
        Assert.Equal(1, screen.PriceOf(CastleOption.Check));
        screen.Confirm();
        Assert.Equal((CastleStep.Ask, "Have the Pokémon identified for 1 CP?"), (screen.Step, screen.Line));
        screen.Confirm();
        Assert.Equal((CastleStep.Note, $"It is {foe[1].DisplayName}!", true, 9), (screen.Step, screen.Line, challenge.Identified[1], screen.Points));
        screen.Confirm();
        Assert.Equal((foe[1].DisplayName, 0), (screen.NameOf(1), screen.PriceOf(CastleOption.Check)));
        // Asked again, it is told for nothing
        screen.Confirm();
        Assert.Equal((CastleStep.Note, 9), (screen.Step, screen.Points));
        screen.Confirm();
        // LEVEL: five up for a point; it goes no higher; five down is fifteen, which eight don't reach
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal(new[] { CastleOption.LevelUp, CastleOption.LevelDown, CastleOption.Cancel }, screen.Options);
        screen.Confirm();
        Assert.Equal((CastleStep.Ask, "That will cost 1 CP. Is that all right?"), (screen.Step, screen.Line));
        screen.Confirm();
        Assert.Equal((CastleStep.Note, $"{foe[1].DisplayName} is now at level 55.", 55, 8), (screen.Step, screen.Line, foe[1].Level, screen.Points));
        screen.Confirm();
        screen.Confirm();
        Assert.Equal((CastleStep.Note, "Its level can't go any higher."), (screen.Step, screen.Line));
        screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal("That will cost 15 CP. Is that all right?", screen.Line);
        screen.Confirm();
        Assert.Equal((CastleStep.Note, "There aren't enough Castle Points for that.", 55), (screen.Step, screen.Line, foe[1].Level));
        screen.Confirm();
        screen.Cancel();
        // INFO: the stats for two points, read as a summary; the moves want the second rank, which is fifty
        screen.Move(0, 1);
        Assert.Equal((CastleOption.Cancel, 2), (screen.Opened, screen.MenuCursor));
        screen.Confirm();
        Assert.Equal(new[] { CastleOption.Stats, CastleOption.Moves, CastleOption.RankUp, CastleOption.Cancel }, screen.Options);
        Assert.Equal((2, 5, 50, 2), (screen.PriceOf(CastleOption.Stats), screen.PriceOf(CastleOption.Moves), screen.PriceOf(CastleOption.RankUp), screen.RankNeeded(CastleOption.Moves)));
        screen.Confirm();
        Assert.Equal((CastleStep.Ask, "Its stats can be told for 2 CP. Is that all right?"), (screen.Step, screen.Line));
        screen.Confirm();
        Assert.Equal((CastleStep.Summary, true, 6, 0), (screen.Step, challenge.StatsShown[1], screen.Points, screen.PriceOf(CastleOption.Stats)));
        // Only the one paid for is told: there is no stepping on to the next
        screen.Move(0, 1);
        Assert.Equal((1, CastleStep.Summary), (screen.Cursor, screen.Step));
        screen.Confirm();
        Assert.Equal(CastleStep.Menu, screen.Step);
        // Looked at again for nothing
        screen.Confirm();
        Assert.Equal((CastleStep.Summary, 6), (screen.Step, screen.Points));
        screen.Cancel();
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal((CastleStep.Note, "This counter's rank is too low for that. Raise its rank first."), (screen.Step, screen.Line));
        screen.Confirm();
        challenge.Receive(records, 60);
        screen.Move(0, 1);
        screen.Confirm();
        Assert.Equal("Raise this counter's rank for 50 CP?", screen.Line);
        screen.Confirm();
        Assert.Equal((CastleStep.Note, "The moves of the trainer's Pokémon can now be told.", 2, 16), (screen.Step, screen.Line, challenge.Rank(records, CastleRank.Summary), screen.Points));
        screen.Confirm();
        screen.Move(0, -1);
        screen.Confirm();
        Assert.Equal("Its moves can be told for 5 CP. Is that all right?", screen.Line);
        screen.Confirm();
        Assert.Equal((CastleStep.Moves, true, 11), (screen.Step, challenge.MovesShown[1], screen.Points));
        screen.Confirm();
        // The way out, straight from the three
        screen.Cancel();
        screen.Cancel();
        Assert.Equal(CastleStep.Browse, screen.Step);
        screen.Move(1, 0);
        screen.Move(1, 0);
        Assert.Equal(CastleScreen.ExitPlace, screen.Cursor);
        screen.Confirm();
        Assert.False(screen.IsActive);
    }

    // ------------------------------------------------------------------ the rooms

    [Fact]
    public void TheCastlesRoomsAreLaidOutAsTheOriginalHasThem()
    {
        // The lobby (events_battle_castle, land data 320): come into from the east, the way into the castle to the west
        var lobby = Room("BattleCastle");
        Assert.Equal((23, 17, true, InteriorStyle.Gym), (lobby.Width, lobby.Height, lobby.IsIndoors, lobby.Interior));
        var out_ = lobby.Warps.Where(w => w.TargetMap == "BattleFrontier").ToList();
        Assert.Equal(new[] { (22, 8), (22, 9), (22, 10) }, out_.Select(w => (w.SourceX, w.SourceY)));
        Assert.All(out_, w => Assert.Equal((37, 62, Direction.Right), (w.TargetX, w.TargetY, w.TargetFacing)));
        // The original has mezzanines here; the floor is level in this game (the rulings)
        Assert.Equal(lobby.HeightAt(20, 9), lobby.HeightAt(3, 9));
        // The attendants across the passage west, the records' machine and the PC beside them
        Assert.Equal(new[] { ("MultiAttendant", 5, 10), ("SingleAttendant", 5, 9) }, lobby.Everyone.Where(n => n.Key?.EndsWith("attendant") == true).Select(n => (n.Script!, n.GridX, n.GridY)).OrderBy(n => n));
        Assert.Equal("RecordsMachine", lobby.SignScripts[(6, 8)]);
        Assert.Contains(lobby.Everyone, n => n.IsPCTerminal && (n.GridX, n.GridY) == (7, 8));
        var west = Assert.Single(lobby.Warps, w => w.TargetMap == "BattleCastleCorridor");
        Assert.Equal((0, 9, 17, 7), (west.SourceX, west.SourceY, west.TargetX, west.TargetY));
        // The original's ten people less the reporter, and the PC
        Assert.Equal(10, lobby.Everyone.Count());
        Assert.All(lobby.Everyone.Where(n => !n.IsPCTerminal), n => Assert.NotNull(ScriptLibrary.Default.Find(n.Script!, "BattleCastle")));

        // The valet's room and the throne room, laid out from the scene's own places
        var corridor = Room("BattleCastleCorridor");
        var valet = Assert.Single(corridor.Everyone);
        Assert.Equal(("valet", 13, 7, Direction.Right), (valet.Key, valet.GridX, valet.GridY, valet.Facing));
        Assert.Equal(new[] { "BattleCastle", "BattleCastleBattleRoom" }, corridor.Warps.Select(w => w.TargetMap).OrderBy(n => n));
        Assert.True(corridor.IsWalkable(14, 7) && corridor.IsWalkable(14, 3));
        var throne = Room("BattleCastleBattleRoom");
        Assert.Equal((BattleArena.League, "FLAG_HIDE_BATTLE_CASTLE_ROOM_OPPONENT", "FLAG_HIDE_BATTLE_CASTLE_ROOM_VALET"),
            (throne.Arena, throne.Everyone.Single(n => n.Key == "opponent").HiddenBy, throne.Everyone.Single(n => n.Key == "valet").HiddenBy));
        Assert.Equal((8, 3, Direction.Down), throne.Everyone.Where(n => n.Key == "lady").Select(n => (n.GridX, n.GridY, n.Facing)).Single());
        // The challenger's and the trainer's places, either side of the carpet
        Assert.True(throne.IsWalkable(6, 9) && throne.IsWalkable(10, 9));
        var door = Assert.Single(throne.Warps);
        Assert.Equal(("BattleCastleCorridor", 14, 4), (door.TargetMap, door.TargetX, door.TargetY));
        Assert.All(RoomPlanTests.BattleHallAndCastle.Skip(3), name => Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id));
    }

    // ------------------------------------------------------------------ a challenge played by its scripts

    private static void Play(HeadlessScriptHost host, ScriptRunner runner, string place, string script, string? who = null)
    {
        if (host.Map?.Name != place) host.Map = Room(place);
        runner.Start(ScriptLibrary.Default.Find(script, place)!, who == null ? null : host.Map!.Everyone.First(n => n.Key == who));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
    }

    private static string LastWarp(HeadlessScriptHost host) => host.Log.Last(l => l.StartsWith("warp ")).Split(' ')[1];

    /// <summary>Plays the arrival script of wherever the last warp led, room after room, until the lobby's has been played.</summary>
    private static void PlayOn(HeadlessScriptHost host, ScriptRunner runner)
    {
        for (int i = 0; i < 60; i++)
        {
            string next = LastWarp(host);
            Play(host, runner, next, "OnEnter");
            if (next == "BattleCastle") return;
        }
        throw new InvalidOperationException("The challenge never came back to the lobby");
    }

    private static HeadlessScriptHost Host(params Pokemon[] team)
    {
        var host = new HeadlessScriptHost();
        foreach (var p in team) host.Party.Add(p);
        return host;
    }

    private static HeadlessScriptHost HostOfThree()
    {
        var host = new HeadlessScriptHost();
        foreach (var p in Three().Members) host.Party.Add(p);
        return host;
    }

    /// <summary>The counter's talk: the challenge by its place in the menu, and yes to whatever is asked.</summary>
    private static void Take(HeadlessScriptHost host, ScriptRunner runner, int way = 0)
    {
        host.Answers.Enqueue(way);
        if (host.Map?.Name != "BattleCastle") host.Map = Room("BattleCastle");
        host.PlayerTile = (6, 9);
        Play(host, runner, "BattleCastle", "SingleAttendant", "single_attendant");
    }

    private static int Points(HeadlessScriptHost host, FrontierChallenge way = Single) => host.Frontier.Get(BattleCastleChallenge.LatestPointsStat(way));

    [Fact]
    public void ASingleChallengeIsPlayedFromTheCounterToTheBattlePoints()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        Take(host, runner);

        // The counter: the three entered, the challenge begun, the game saved, the round drawn, the way west
        var castle = host.Castle!;
        Assert.Equal((Single, 0, 14, 10), (castle.Way, castle.Battle, castle.Trainers.Count, Points(host)));
        Assert.Equal(All, castle.Slots);
        Assert.NotNull(castle.Foe);
        Assert.Contains("frontier enter 3", host.Log);
        Assert.Contains("frontier save 0", host.Log);
        Assert.Contains($"frontier journal {(int)FrontierFacility.Castle}", host.Log);
        Assert.Equal((255, 0, 1), (host.Story.Var("VAR_BATTLE_CASTLE_LOBBY_LOAD_ACTION"), host.Story.Var(BattleCastleChallenge.WayVariable), host.Frontier.ChallengesStarted));
        Assert.Equal("BattleCastleCorridor", LastWarp(host));

        PlayOn(host, runner);
        // Seven battles in the throne room, three against three at level 50, with the three as the challenge carries them
        Assert.Equal(7, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.Equal((3, 3, false), (b.Team.Count, b.Foes.Single().Party.Count, b.Doubles)));
        Assert.All(host.FrontierBattles, b => Assert.Same(castle.Team, b.Team));
        Assert.All(host.FrontierBattles, b => Assert.All(b.Foes[0].Party.Members, p => Assert.Equal(50, p.Level)));
        Assert.All(host.FrontierBattles, b => Assert.Equal(AiFlags.None, b.Foes[0].Ai));
        Assert.Equal(castle.Trainers.Take(7).Select(t => "frontier_" + FrontierDatabase.Trainer(t).Id), host.FrontierBattles.Select(b => b.Foes[0].Id));
        Assert.Equal(new[] { 50, 50, 45 }, castle.Team.Members.Select(p => p.Level));
        // The valet's welcome, once, and what each battle earned: nothing was scratched, so twenty-nine a battle
        Assert.True(Said(host, "whatever your Pokémon hold is kept at the door"));
        Assert.True(host.Frontier.Castle.HeardIntro);
        Assert.Equal(7, host.Transcript.Count(l => l.Text.Contains("received 29 Castle Points")));
        Assert.Equal(7, host.Transcript.Count(l => l.Text.Contains("A remarkable victory")));
        Assert.True(Said(host, "Battle no. 7."));
        Assert.Equal(10 + 7 * 29, Points(host));
        // The records, the score, the Battle Points and the lobby
        Assert.Equal((7, 7, 213, true), (host.Frontier.Get(BattleCastleChallenge.LatestStreakStat(Single)), host.Frontier.Get(BattleCastleChallenge.RecordStreakStat(Single)),
            host.Frontier.Get(BattleCastleChallenge.RecordPointsStat(Single)), host.Frontier.IsRunning(FrontierFacility.Castle, (int)Single)));
        Assert.Equal((3, 7, BattleCastleChallenge.RoundScore), (host.Frontier.BattlePoints, host.Frontier.VictoriesAt(FrontierFacility.Castle), host.FrontierScore));
        Assert.True(Said(host, "received 3 BP"));
        Assert.True(Said(host, "all seven"));
        Assert.Null(host.Castle);
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_CASTLE_LOBBY_LOAD_ACTION"));
        Assert.Equal(2, host.Log.Count(l => l == "frontier save 0"));
        // The player's own are as they were, items and all
        Assert.Equal((60, "Leftovers"), (host.Party.Members[0].Level, host.Party.Members[0].HeldItem?.Name));

        // The next round takes the streak and the Castle Points up, and the valet is brief
        host.FrontierBattles.Clear();
        host.Transcript.Clear();
        Take(host, runner);
        Assert.Equal((7, 1, 223), (host.Castle!.Streak, host.Castle.Round, Points(host)));
        Play(host, runner, "BattleCastleCorridor", "OnEnter");
        Assert.True(Said(host, "in safe keeping, as before"));
        Assert.False(Said(host, "kept at the door"));
    }

    [Fact]
    public void TheValetsCountersAreVisitedBetweenBattlesAndWhatABattleCostsIsCarriedOn()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // Before the second battle: MY POKéMON, then THE OPPONENT, then BATTLE
        int battle = 0, menus = 0;
        host.Decide = (asked, _) => asked.Contains("Castle Points.") && battle == 1 ? Math.Min(++menus, 3) % 3 : 0;
        var visited = new List<string>();
        host.AtCastleCounter = (castle, what) =>
        {
            visited.Add(what);
            if (what == "castleself") Assert.Equal(CastleAnswer.Done, castle.Heal(host.Frontier, 0, CastleHeal.Hp));
            else Assert.Equal(CastleAnswer.Done, castle.AdjustLevel(host.Frontier, 2, up: true));
        };
        // The first battle costs the first of the three most of its HP and the second all of it
        host.Fight = _ =>
        {
            var team = host.Castle!.Team.Members;
            if (++battle == 1) { team[0].CurrentHP = 10; team[1].CurrentHP = 0; }
            return BattleOutcome.Won;
        };
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal(new[] { "castleself", "castlefoe" }, visited);
        Assert.Equal(7, host.FrontierBattles.Count);
        // The first battle: 3+1+1, nothing, 3+3+1, and 8 for the PP: twenty, "a hard-fought victory", and the
        // fainted one brought round
        Assert.True(Said(host, "received 20 Castle Points"));
        Assert.True(Said(host, "A hard-fought victory"));
        Assert.True(Said(host, "has been brought round"));
        // The second, after the first was healed for ten and one of the trainer's raised for one: 7, 3+1+1 for the
        // one with a single HP, 7, 8 for the PP and 7 for the level: thirty-four
        Assert.True(Said(host, "received 34 Castle Points"));
        Assert.Equal(55, host.FrontierBattles[1].Foes[0].Party.Members[2].Level);
        Assert.All(host.FrontierBattles.Skip(2), b => Assert.All(b.Foes[0].Party.Members, p => Assert.Equal(50, p.Level)));
        // The other five: 7 + 5 + 7 + 8 each, since nothing is restored between battles
        Assert.Equal(5, host.Transcript.Count(l => l.Text.Contains("received 27 Castle Points")));
        Assert.Equal(10 + 20 - 10 - 1 + 34 + 5 * 27, Points(host));
        Assert.Equal(11, host.Frontier.Get(BattleCastleChallenge.SpentPointsStat(Single)));
    }

    [Fact]
    public void ADoubleChallengeIsTwoAgainstTwoAndNeverMeetsTheCastleValet()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Frontier.SetRunning(FrontierFacility.Castle, (int)Double, true);
        host.Frontier.Set(BattleCastleChallenge.LatestStreakStat(Double), 14);
        host.Frontier.Set(BattleCastleChallenge.LatestPointsStat(Double), 5);
        Take(host, runner, way: 1);
        Assert.Equal((Double, 14, 2, 15, 1), (host.Castle!.Way, host.Castle.Streak, host.Castle.Round, Points(host, Double), host.Story.Var(BattleCastleChallenge.WayVariable)));
        PlayOn(host, runner);
        Assert.Equal(7, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.Equal((true, 3, 3, AiFlags.Basic), (b.Doubles, b.Team.Count, b.Foes[0].Party.Count, b.Foes[0].Ai)));
        Assert.DoesNotContain(host.FrontierBattles, b => b.Foes[0].TrainerClass == "Castle Valet");
        Assert.False(Said(host, "Castle Valet himself"));
        Assert.Equal((21, 4, 0), (host.Frontier.Get(BattleCastleChallenge.LatestStreakStat(Double)), host.Frontier.BattlePoints, host.Frontier.Get(BattleCastleChallenge.LatestStreakStat(Single))));
        Assert.True(Said(host, "received 4 BP"));

        // Who can't go in is told why
        var two = Host(Mon("Garchomp", 60), Mon("Luxray", 50));
        Take(two, new ScriptRunner(ScriptLibrary.Default, two));
        Assert.True(Said(two, "You need three Pokémon, each of a different kind"));
        Assert.Null(two.Castle);
        var same = Host(Mon("Garchomp", 60), Mon("Luxray", 50), Mon("Luxray", 44), Mon("Staraptor", 45));
        same.Entering.Enqueue(new[] { 0, 1, 2 });
        Take(same, new ScriptRunner(ScriptLibrary.Default, same));
        Assert.True(Said(same, "must each be of a different kind"));
        Assert.Null(same.Castle);
        // A save refused lets the challenge go
        var unsaved = HostOfThree();
        unsaved.Decide = (asked, _) => asked.Contains("must be saved") ? 1 : 0;
        Take(unsaved, new ScriptRunner(ScriptLibrary.Default, unsaved));
        Assert.True(Said(unsaved, "come back when you are ready"));
        Assert.Null(unsaved.Castle);
        // ... before it had begun: no Castle Points were handed out for a round that wasn't entered
        Assert.Equal((0, 0), (unsaved.Story.Var("VAR_BATTLE_CASTLE_LOBBY_LOAD_ACTION"), Points(unsaved)));
    }

    [Fact]
    public void ABattleIsPassedFromTheFourthRoundOn()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Frontier.SetRunning(FrontierFacility.Castle, (int)Single, true);
        host.Frontier.Set(BattleCastleChallenge.LatestStreakStat(Single), 21);
        host.Frontier.Set(BattleCastleChallenge.LatestPointsStat(Single), 45);
        // PASS is the menu's fourth answer here; taken once, before the first battle
        bool passed = false;
        host.Decide = (asked, answers) =>
        {
            if (!asked.Contains("Castle Points.") || passed) return 0;
            Assert.Equal(new[] { "BATTLE", "MY POKéMON", "THE OPPONENT", "PASS" }, answers);
            passed = true;
            return 3;
        };
        Take(host, runner);
        PlayOn(host, runner);
        Assert.True(Said(host, "From this round on you may pass a battle"));
        Assert.True(Said(host, "presented a pass"));
        // Six battles fought, seven counted for the round and the streak; fifty points gone, and nothing earned for the pass
        Assert.Equal((6, 28, 6, 4), (host.FrontierBattles.Count, host.Frontier.Get(BattleCastleChallenge.LatestStreakStat(Single)), host.Frontier.VictoriesAt(FrontierFacility.Castle), host.Frontier.BattlePoints));
        Assert.Equal(45 + 10 - 50 + 6 * 29, Points(host));
        Assert.Equal(6, host.Transcript.Count(l => l.Text.Contains("Castle Points!")));
        // The trainer passed is the round's first; the one battled first is its second
        Assert.True(Said(host, "Battle no. 23."));
        Assert.False(Said(host, "Battle no. 22."));

        // With too few points the valet regrets it, and the battle is fought
        var poor = HostOfThree();
        runner = new ScriptRunner(ScriptLibrary.Default, poor);
        poor.Frontier.SetRunning(FrontierFacility.Castle, (int)Single, true);
        poor.Frontier.Set(BattleCastleChallenge.LatestStreakStat(Single), 21);
        bool asked1 = false;
        poor.Decide = (asked, _) =>
        {
            if (!asked.Contains("Castle Points.") || asked1) return 0;
            asked1 = true;
            return 3;
        };
        Take(poor, runner);
        Play(poor, runner, "BattleCastleCorridor", "OnEnter");
        Assert.True(Said(poor, "you have not the Castle Points for it"));
        Assert.Equal((0, 10, "BattleCastleBattleRoom"), (poor.Castle!.Battle, Points(poor), LastWarp(poor)));
        // In the first three rounds the menu has no PASS
        var early = HostOfThree();
        runner = new ScriptRunner(ScriptLibrary.Default, early);
        var offered = new List<string>();
        early.Decide = (asked, answers) =>
        {
            if (asked == "You have 10 Castle Points.") offered.AddRange(answers);
            return 0;
        };
        Take(early, runner);
        Play(early, runner, "BattleCastleCorridor", "OnEnter");
        Assert.Equal(new[] { "BATTLE", "MY POKéMON", "THE OPPONENT" }, offered);
    }

    [Fact]
    public void TheCastleValetIsMetAtTheTwentyFirstBattleAndLeavesHisPrint()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Frontier.SetRunning(FrontierFacility.Castle, (int)Single, true);
        host.Frontier.Set(BattleCastleChallenge.LatestStreakStat(Single), 14);
        Take(host, runner);
        PlayOn(host, runner);

        Assert.Equal(7, host.FrontierBattles.Count);
        var last = host.FrontierBattles[6].Foes[0];
        Assert.Equal(("frontier_castle_valet_darach_silver", "Castle Valet", FrontierTrainers.Mind, 3), (last.Id, last.TrainerClass, last.Ai, last.Party.Count));
        Assert.All(host.FrontierBattles.Take(6), b => Assert.Equal(AiFlags.Basic, b.Foes[0].Ai));
        // He is announced before the battle, once, and speaks before and after it
        Assert.Equal(1, host.Transcript.Count(l => l.Text.Contains("the Castle Valet himself has asked to meet you")));
        Assert.True(Said(host, "I am Darach, valet to the lady of this castle"));
        Assert.True(Said(host, "Admirably done"));
        Assert.Equal(1, host.Frontier.BrainsMet);
        // Twenty Battle Points from his own hand, the silver print in the lobby, and a streak of 21 running
        Assert.True(Said(host, "You have beaten the Castle Valet. He asked"));
        Assert.True(Said(host, "received 20 BP"));
        Assert.True(Said(host, "Silver Print"));
        Assert.Equal((2, FrontierPrint.Silver, 20, 21, true), (host.Story.Var("VAR_BATTLE_CASTLE_PRINT_STATE"), FrontierBrains.PrintOf(host.Story, FrontierFacility.Castle),
            host.Frontier.BattlePoints, host.Frontier.Get(BattleCastleChallenge.LatestStreakStat(Single)), host.Frontier.IsRunning(FrontierFacility.Castle, (int)Single)));
        var board = FrontierBoard.For(FrontierFacility.Castle, host.Frontier, host.Story);
        Assert.Equal((FrontierPrint.Silver, "CP", "Battle Castle"), (board.Print, board.CountLabel, board.Title));
        Assert.Equal(new[] { ("Single Battle", 21, 21, true, (int?)(10 + 7 * 29), (int?)(10 + 7 * 29)), ("Double Battle", 0, 0, false, (int?)0, (int?)0) },
            board.Rows.Select(r => (r.Label, r.Record, r.Latest, r.Running, r.RecordCount, r.LatestCount)));

        // The gold print at the forty-ninth
        host = HostOfThree();
        runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Story.SetVar("VAR_BATTLE_CASTLE_PRINT_STATE", 2);
        host.Frontier.SetRunning(FrontierFacility.Castle, (int)Single, true);
        host.Frontier.Set(BattleCastleChallenge.LatestStreakStat(Single), 42);
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal("frontier_castle_valet_darach_gold", host.FrontierBattles[6].Foes[0].Id);
        Assert.True(Said(host, "We meet again"));
        Assert.True(Said(host, "It has been an honour"));
        Assert.True(Said(host, "Gold Print"));
        Assert.Equal((4, FrontierPrint.Gold, 20), (host.Story.Var("VAR_BATTLE_CASTLE_PRINT_STATE"), FrontierBrains.PrintOf(host.Story, FrontierFacility.Castle), host.Frontier.BattlePoints));
    }

    [Fact]
    public void AChallengeIsPutDownTakenUpAgainAndGivenUp()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // The first battle leaves a mark, so what is taken up again can be told from a team made afresh
        int fought = 0;
        host.Fight = _ =>
        {
            if (++fought == 1) host.Castle!.Team.Members[2].CurrentHP = 40;
            return BattleOutcome.Won;
        };
        // The valet's question after the third battle: REST, and yes; the lobby's question: not now
        int menus = 0;
        host.Decide = (asked, _) => asked.Contains("Are you ready?") ? (++menus == 3 ? 1 : 0) : asked.Contains("go on with it now") ? 1 : 0;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.Null(host.Castle);
        var paused = host.Frontier.Castle.Paused!;
        Assert.Equal((3, 3, 40), (paused.Battle, host.Frontier.Get(BattleCastleChallenge.LatestStreakStat(Single)), paused.Team[2].Hp));
        Assert.Equal(new[] { "Garchomp", "Luxray", "Staraptor" }, paused.Species);
        Assert.Equal((2, 0, false), (host.Story.Var("VAR_BATTLE_CASTLE_LOBBY_LOAD_ACTION"), host.Frontier.Get(BattleCastleChallenge.RecordStreakStat(Single)), host.Frontier.IsRunning(FrontierFacility.Castle, (int)Single)));
        Assert.True(Said(host, "Speak to me whenever you are ready"));
        int points = Points(host);

        // Spoken to again, she takes it up: the valet's question comes first; KEEP GOING for one battle, then RETIRE
        menus = 0;
        host.Decide = (asked, _) => asked.Contains("Are you ready?") ? (++menus == 2 ? 2 : 0) : 0;
        host.Transcript.Clear();
        host.PlayerTile = (6, 9);
        Play(host, runner, "BattleCastle", "SingleAttendant", "single_attendant");
        Assert.Equal((true, 3, 40, points), (host.Castle!.Resumed, host.Castle.Battle, host.Castle.Team.Members[2].CurrentHP, Points(host)));
        Assert.Equal("BattleCastleCorridor", LastWarp(host));
        PlayOn(host, runner);
        Assert.Equal(4, host.FrontierBattles.Count);
        Assert.False(Said(host, "Welcome to the Battle Castle. I am to see"));
        Assert.True(Said(host, "Battle no. 4."));
        Assert.True(Said(host, "Thank you for taking the challenge"));
        Assert.Equal((4, 4, false, 0), (host.Frontier.Get(BattleCastleChallenge.LatestStreakStat(Single)), host.Frontier.Get(BattleCastleChallenge.RecordStreakStat(Single)),
            host.Frontier.IsRunning(FrontierFacility.Castle, (int)Single), host.Frontier.BattlePoints));
        Assert.Null(host.Castle);
        Assert.Null(host.Frontier.Castle.Paused);

        // A challenge put down whose three are no longer in their places can't be taken up
        menus = 0;
        host.Decide = (asked, _) => asked.Contains("Are you ready?") ? 1 : asked.Contains("go on with it now") ? 1 : 0;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.NotNull(host.Frontier.Castle.Paused);
        host.Party.Members[1] = Mon("Bidoof", 30);
        host.Decide = null;
        host.Transcript.Clear();
        host.PlayerTile = (6, 9);
        Play(host, runner, "BattleCastle", "SingleAttendant", "single_attendant");
        Assert.True(Said(host, "can't be taken up again"));
        Assert.Null(host.Castle);
        Assert.Null(host.Frontier.Castle.Paused);
        Assert.Equal((0, 0), (host.Story.Var("VAR_BATTLE_CASTLE_LOBBY_LOAD_ACTION"), Points(host)));
    }

    [Fact]
    public void ABattleLostEndsTheChallengeAndAGameClosedInTheMiddleEndsTheStreak()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        int fought = 0;
        host.Fight = _ => ++fought == 3 ? BattleOutcome.Lost : BattleOutcome.Won;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.Equal((2, 2, false, 2, 0), (host.Frontier.Get(BattleCastleChallenge.LatestStreakStat(Single)), host.Frontier.Get(BattleCastleChallenge.RecordStreakStat(Single)),
            host.Frontier.IsRunning(FrontierFacility.Castle, (int)Single), host.Frontier.VictoriesAt(FrontierFacility.Castle), host.Frontier.BattlePoints));
        Assert.True(Said(host, "Thank you for taking the challenge"));
        Assert.Null(host.Castle);
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_CASTLE_LOBBY_LOAD_ACTION"));

        // A challenge under way when the game was closed: the lobby finds its mark, and the streak and its points are over
        host.Fight = null;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.True(host.Frontier.IsRunning(FrontierFacility.Castle, (int)Single));
        Take(host, runner);
        Assert.Equal(255, host.Story.Var("VAR_BATTLE_CASTLE_LOBBY_LOAD_ACTION"));
        host.Castle = null;
        host.Transcript.Clear();
        Play(host, runner, "BattleCastle", "OnEnter");
        Assert.True(Said(host, "left before it was over"));
        Assert.Equal((false, 0, 0, 0), (host.Frontier.IsRunning(FrontierFacility.Castle, (int)Single), host.Frontier.Get(BattleCastleChallenge.LatestStreakStat(Single)), Points(host),
            host.Story.Var("VAR_BATTLE_CASTLE_LOBBY_LOAD_ACTION")));
        Assert.Equal(7, host.Frontier.Get(BattleCastleChallenge.RecordStreakStat(Single)));
    }

    [Fact]
    public void TheLobbysPeopleAndTheCommandsErrors()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var lobby = Room("BattleCastle");
        foreach (var person in lobby.Everyone.Where(n => n.Script != null && n.Key?.EndsWith("attendant") != true))
        {
            host.Transcript.Clear();
            Play(host, runner, "BattleCastle", person.Script!, person.Key);
            Assert.NotEmpty(host.Transcript);
        }
        Play(host, runner, "BattleCastle", "MultiAttendant", "multi_attendant");
        Assert.True(Said(host, "nobody can be linked up with"));
        Play(host, runner, "BattleCastle", "RecordsMachine");
        Assert.Contains($"frontier records {(int)FrontierFacility.Castle}", host.Log);
        // INFO ends the talk
        host.Answers.Enqueue(2);
        host.PlayerTile = (6, 9);
        Play(host, runner, "BattleCastle", "SingleAttendant", "single_attendant");
        Assert.True(Said(host, "Every win earns Castle Points"));
        Assert.Null(host.Castle);

        // What a command needs is said
        var library = ScriptLibrary.FromSources(("test", "script Battle\n  castle battle\n  end\nscript Begin\n  castle begin single\n  end\nscript Quiet\n  castle active\n  castle round\n  castle fainted\n  castle beaten\n  castle way\n  end\n"));
        runner = new ScriptRunner(library, host);
        runner.Start(library.Find("Quiet", "test")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        runner.Start(library.Find("Battle", "test")!, null);
        Assert.Contains("needs a challenge under way", Assert.Throws<ScriptException>(() => runner.RunToEnd()).Message);
        runner = new ScriptRunner(library, host);
        runner.Start(library.Find("Begin", "test")!, null);
        Assert.Contains("castle choose", Assert.Throws<ScriptException>(() => runner.RunToEnd()).Message);
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script X\n  castle begin triple\n  end\n"));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script X\n  castle joust\n  end\n"));
    }
}
