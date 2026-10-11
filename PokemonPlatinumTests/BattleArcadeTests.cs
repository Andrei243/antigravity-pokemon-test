using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The Battle Arcade (plan 06 · R18d). The numbers are the decompilation's (<c>src/overlay104/battle_arcade.c</c>,
/// <c>battle_arcade_helpers.c</c>, <c>frscrcmd_battle_arcade.c</c>, <c>src/battle_arcade_save.c</c>, the game board,
/// <c>src/applications/frontier/battle_arcade/main.c</c>, <c>scripts_battle_arcade.s</c> and
/// <c>frontier_scripts_battle_arcade.s</c>).
/// </summary>
[Collection("MapDatabase")]
public class BattleArcadeTests
{
    private const int Silver = 311, Gold = 312;
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

    private static Pokemon Mon(string species, int level, string? item = null, string? ability = null)
    {
        var pokemon = new Pokemon(PokemonDatabase.Get(species)!, level);
        if (item != null) pokemon.HeldItem = ItemDatabase.Get(item);
        if (ability != null)
        {
            Assert.Contains(ability, pokemon.Abilities);
            pokemon.AbilityName = ability;
        }
        return pokemon;
    }

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    /// <summary>Three that may go in: one over level 50 holding something, one at it, one under it. None is kept from anything by its ability.</summary>
    private static Party Three() => PartyOf(Mon("Garchomp", 60, "Leftovers", "Sand Veil"), Mon("Luxray", 50, "Sitrus Berry", "Intimidate"), Mon("Staraptor", 45, null, "Intimidate"));

    private static readonly int[] All = { 0, 1, 2 };

    /// <summary>A challenge begun and its first round drawn; with a streak, one that is still running at so many wins.</summary>
    private static BattleArcadeChallenge Begun(FrontierRecords records, FrontierChallenge way = Single, int seed = 1, int streak = 0)
    {
        if (streak > 0)
        {
            records.SetRunning(FrontierFacility.Arcade, (int)way, true);
            records.Set(BattleArcadeChallenge.LatestStreakStat(way), streak);
        }
        var challenge = BattleArcadeChallenge.Begin(records, way, Three(), All);
        challenge.DrawRound(Draws(seed));
        return challenge;
    }

    private static readonly ArcadeEffect[] Every = Enumerable.Range(0, 32).Select(e => (ArcadeEffect)e).ToArray();

    // ------------------------------------------------------------------ the tables

    [Fact]
    public void TheArcadesTablesAreTheOriginals()
    {
        var arcade = FrontierDatabase.Arcade;
        // sBattleArcadeOpponentPools: each round's trainers, and the last of each seven from further on
        Assert.Equal(8, arcade.Opponents.Count);
        Assert.Equal(new[] { 0, 99, 100, 119 }, arcade.Opponents[0]);
        Assert.Equal(new[] { 80, 119, 120, 139 }, arcade.Opponents[1]);
        Assert.Equal(new[] { 180, 219, 220, 239 }, arcade.Opponents[6]);
        Assert.Equal(new[] { 200, 299, 200, 299 }, arcade.Opponents[7]);
        // sItemPools and sBerryPools: three lists each, by the round
        Assert.Equal(new[] { 11, 13, 11 }, arcade.Items.Select(l => l.Count));
        Assert.Equal(new[] { 8, 20, 10 }, arcade.Berries.Select(l => l.Count));
        Assert.Equal(("King's Rock", "Black Sludge", "White Herb", "Focus Band", "Choice Scarf"),
            (arcade.Items[0][0], arcade.Items[0][10], arcade.Items[1][0], arcade.Items[2][0], arcade.Items[2][10]));
        Assert.Equal(("Cheri Berry", "Lum Berry", "Occa Berry", "Chilan Berry", "Persim Berry", "Starf Berry"),
            (arcade.Berries[0][0], arcade.Berries[0][7], arcade.Berries[1][0], arcade.Berries[1][16], arcade.Berries[2][0], arcade.Berries[2][9]));
        Assert.All(arcade.Items.Concat(arcade.Berries).SelectMany(l => l), held => Assert.NotNull(ItemDatabase.Get(held)));

        // The thirty-two events, in the order of ARCADE_EFFECT_…, which is the order of ArcadeEffect
        Assert.Equal(32, arcade.Effects.Count);
        Assert.Equal(Every.Select(e => e.ToString().ToUpperInvariant()),
            arcade.Effects.Select(e => e.Name.Replace("_", "") switch
            {
                "LOWERFOEHP" => "LOWERFOEHP", "SUNNYBATTLE" => "SUNNY", "RAINYBATTLE" => "RAINY", "SANDYBATTLE" => "SANDY", "HAILBATTLE" => "HAIL",
                "FOGGYBATTLE" => "FOGGY", var other => other
            }));
        Assert.Equal((int)ArcadeEffect.None, arcade.Effects.Count);
        int[] Rounds(ArcadeEffect effect) => arcade.Effects[(int)effect].Rounds;
        int[]? Battles(ArcadeEffect effect) => arcade.Effects[(int)effect].Battles;
        // sEffectAvailabilityByRound
        Assert.Equal(new[] { 0, 1, 1, 1, 1, 1, 1 }, Rounds(ArcadeEffect.LowerFoeHp));
        Assert.Equal(new[] { 1, 0, 1, 0, 0, 0, 1 }, Rounds(ArcadeEffect.PoisonAlly));
        Assert.Equal(new[] { 0, 0, 0, 0, 1, 1, 1 }, Rounds(ArcadeEffect.SleepFoe));
        Assert.Equal(new[] { 1, 1, 1, 0, 0, 0, 1 }, Rounds(ArcadeEffect.AllyGetBerry));
        Assert.Equal(new[] { 0, 0, 0, 1, 1, 1, 1 }, Rounds(ArcadeEffect.FoeGetItem));
        Assert.Equal(new[] { 0, 1, 1, 0, 0, 0, 1 }, Rounds(ArcadeEffect.Hail));
        Assert.Equal(new[] { 0, 0, 0, 1, 0, 1, 1 }, Rounds(ArcadeEffect.TrickRoom));
        Assert.Equal(new[] { 1, 1, 1, 1, 1, 1, 1 }, Rounds(ArcadeEffect.NoEvent));
        Assert.Equal(new[] { 0, 0, 0, 0, 0, 0, 1 }, Rounds(ArcadeEffect.Get3Bp));
        // sEffectAvailabilityByBattle: seven events go by the battle of the round too
        Assert.Equal(new[] { ArcadeEffect.SpeedUp, ArcadeEffect.SlowDown, ArcadeEffect.RandomizeCursor, ArcadeEffect.SwapMons, ArcadeEffect.Get1Bp, ArcadeEffect.NoBattle, ArcadeEffect.Get3Bp },
            Every.Where(e => Battles(e) != null));
        Assert.Equal(new[] { 1, 1, 1, 1, 1, 1, 0 }, Battles(ArcadeEffect.SwapMons));
        Assert.Equal(new[] { 1, 0, 1, 0, 1, 0, 0 }, Battles(ArcadeEffect.Get1Bp));
        Assert.Equal(new[] { 0, 1, 0, 1, 0, 1, 0 }, Battles(ArcadeEffect.NoBattle));
        Assert.Equal(new[] { 0, 1, 0, 1, 0, 1, 0 }, Battles(ArcadeEffect.Get3Bp));
        // sCategoryWeights, sCategoryTierPerformanceThresholds, sCursorMoveFrames, sRouletteSpeedByRound
        Assert.Equal(new[] { new[] { 15, 15, 40, 30 }, new[] { 35, 20, 30, 15 }, new[] { 30, 30, 35, 5 }, new[] { 25, 40, 30, 5 }, new[] { 10, 75, 10, 5 } }, arcade.Weights);
        Assert.Equal(new[] { 21, 16, 10, 5 }, arcade.Thresholds);
        Assert.Equal(new[] { 20, 16, 8, 4, 3, 2, 1, 0 }, arcade.CursorFrames);
        Assert.Equal(new[] { new[] { 7, 5 }, new[] { 4, 4 }, new[] { 0, 3 } }, arcade.SpeedByRound);
        // sPerformanceByNumWithStatus, …ByNumFainted, …ByTurnsElapsed, sBPPerRoundSolo
        Assert.Equal(new[] { 8, 6, 4, 0, 0 }, arcade.ScoreByStatus);
        Assert.Equal(new[] { 6, 4, 2, 0, 0 }, arcade.ScoreByFainted);
        Assert.Equal(new[] { new[] { 3, 10 }, new[] { 5, 6 }, new[] { 7, 4 }, new[] { 9, 2 }, new[] { 10, 0 } }, arcade.ScoreByTurns);
        Assert.Equal(new[] { 0, 2, 2, 2, 2, 4, 4, 5, 6 }, arcade.PointsByRound);

        // The Arcade Star, her two places in a streak, and the stats: two to a way, in the original's order
        Assert.Equal((Silver, Gold), FrontierBrains.Trainers(FrontierFacility.Arcade));
        Assert.Equal((21, 49), FrontierBrains.Battles(FrontierFacility.Arcade));
        Assert.Equal(("Arcade Star", "Dahlia"), (FrontierDatabase.Trainer(Silver).Class, FrontierDatabase.Trainer(Silver).Name));
        Assert.Equal((FrontierStat.ArcadeRecordStreakSingle, FrontierStat.ArcadeLatestStreakSingle, FrontierStat.ArcadeRecordStreakDouble, FrontierStat.ArcadeLatestStreakDouble),
            (BattleArcadeChallenge.RecordStreakStat(Single), BattleArcadeChallenge.LatestStreakStat(Single), BattleArcadeChallenge.RecordStreakStat(Double), BattleArcadeChallenge.LatestStreakStat(Double)));
        Assert.Equal((7, 3, 8, 50, 7), (BattleArcadeChallenge.BattlesPerRound, BattleArcadeChallenge.TeamSize, BattleArcadeChallenge.DistinctRounds, BattleArcadeChallenge.Level, BattleArcadeChallenge.RoundScore));
        // Whom each event falls on (BattleArcade_GetCategoryFromEffect)
        Assert.Equal((9, 9, 9, 5), (Every.Count(e => ArcadeBoard.CategoryOf(e) == ArcadeCategory.Foe), Every.Count(e => ArcadeBoard.CategoryOf(e) == ArcadeCategory.Ally),
            Every.Count(e => ArcadeBoard.CategoryOf(e) == ArcadeCategory.Field), Every.Count(e => ArcadeBoard.CategoryOf(e) == ArcadeCategory.Bonus)));
        Assert.Equal((ArcadeCategory.Field, ArcadeCategory.Field, ArcadeCategory.Bonus, ArcadeCategory.Bonus),
            (ArcadeBoard.CategoryOf(ArcadeEffect.Sunny), ArcadeBoard.CategoryOf(ArcadeEffect.RandomizeCursor), ArcadeBoard.CategoryOf(ArcadeEffect.SwapMons), ArcadeBoard.CategoryOf(ArcadeEffect.NoEvent)));
        Assert.All(Every, e => Assert.NotEmpty(ArcadeBoard.NameOf(e)));
    }

    // ------------------------------------------------------------------ who may go in, and how

    [Fact]
    public void ThreePokemonOfDifferentSpeciesGoInWithNothingHeld()
    {
        // BA_LOBBY_FUNC_CHECK_PARTY_ELIGIBLE: three that may enter, no two of a species; what they hold doesn't matter
        var same = PartyOf(Mon("Garchomp", 60, "Leftovers"), Mon("Luxray", 50, "Leftovers"), Mon("Bidoof", 3, "Leftovers"));
        Assert.True(BattleArcadeChallenge.EnoughToEnter(same));
        Assert.True(BattleArcadeChallenge.ValidEntry(same, All));
        Assert.False(BattleArcadeChallenge.EnoughToEnter(PartyOf(Mon("Garchomp", 60), Mon("Luxray", 50))));
        Assert.False(BattleArcadeChallenge.EnoughToEnter(PartyOf(Mon("Garchomp", 60), Mon("Luxray", 50), Mon("Luxray", 44))));
        Assert.False(BattleArcadeChallenge.EnoughToEnter(PartyOf(Mon("Garchomp", 60), Mon("Luxray", 50), Mon("Dialga", 70))));
        var egg = Mon("Togepi", 5);
        egg.IsEgg = true;
        Assert.False(BattleArcadeChallenge.EnoughToEnter(PartyOf(Mon("Garchomp", 60), Mon("Luxray", 50), egg)));
        var four = PartyOf(Mon("Garchomp", 60), Mon("Luxray", 50), Mon("Luxray", 44), Mon("Staraptor", 45));
        Assert.False(BattleArcadeChallenge.ValidEntry(four, All));
        Assert.True(BattleArcadeChallenge.ValidEntry(four, new[] { 0, 2, 3 }));
        Assert.False(BattleArcadeChallenge.ValidEntry(four, new[] { 0, 0, 3 }));
        Assert.Throws<InvalidOperationException>(() => BattleArcadeChallenge.Begin(new FrontierRecords(), FrontierChallenge.Multi, four, new[] { 0, 2, 3 }));

        // BattleArcade_Init: the three as copies, at level 50 where they were above it, holding nothing; the cursor at the fourth of its eight speeds
        var records = new FrontierRecords();
        var party = Three();
        var challenge = BattleArcadeChallenge.Begin(records, Single, party, All);
        Assert.Equal((0, 0, 0, 3, false, 0, ArcadeEffect.None), (challenge.Battle, challenge.Streak, challenge.Round, challenge.Speed, challenge.Randomized, challenge.Performance, challenge.Effect));
        Assert.Equal(new[] { ("Garchomp", 50), ("Luxray", 50), ("Staraptor", 45) }, challenge.Team.Members.Select(p => (p.Species.Name, p.Level)));
        Assert.All(challenge.Team.Members, p => Assert.True(p.HeldItem == null && p.CurrentHP == p.MaxHP));
        // The player's own are as they were
        Assert.Equal((60, "Leftovers", "Sitrus Berry"), (party.Members[0].Level, party.Members[0].HeldItem?.Name, party.Members[1].HeldItem?.Name));
        Assert.DoesNotContain(party.Members[0], challenge.Team.Members);
        // A streak still running is carried on, and the round with it
        Assert.Equal((15, 2), (Begun(new FrontierRecords(), streak: 15).Streak, Begun(new FrontierRecords(), streak: 15).Round));
    }

    [Fact]
    public void ARoundsTrainersAreDrawnFromItsRowAndTheArcadeStarStandsInHerTwoPlaces()
    {
        // BattleArcade_GetOpponentTrainerID: min + draw % (max - min), the seventh of each seven from the row's second half
        Assert.Equal(0 + 250 % 99, BattleArcadeChallenge.TrainerFor(Single, 0, 0, () => 250));
        Assert.Equal(100 + 40 % 19, BattleArcadeChallenge.TrainerFor(Single, 0, 6, () => 40));
        Assert.Equal(100 + 40 % 19, BattleArcadeChallenge.TrainerFor(Single, 0, 13, () => 40));
        Assert.Equal(80 + 1000 % 39, BattleArcadeChallenge.TrainerFor(Single, 1, 3, () => 1000));
        // The last row is every round's from the eighth on
        Assert.Equal(200 + 1000 % 99, BattleArcadeChallenge.TrainerFor(Single, 12, 6, () => 1000));
        // The 21st and the 49th battle of a Single streak are Dahlia's, without a draw; a Double streak never meets her
        Assert.Equal(Silver, BattleArcadeChallenge.TrainerFor(Single, 2, 6, () => throw new InvalidOperationException()));
        Assert.Equal(Gold, BattleArcadeChallenge.TrainerFor(Single, 6, 6, () => throw new InvalidOperationException()));
        Assert.Equal(140 + 5 % 19, BattleArcadeChallenge.TrainerFor(Double, 2, 6, () => 5));
        Assert.Equal(140 + 5 % 19, BattleArcadeChallenge.TrainerFor(Single, 2, 13, () => 5));

        // BattleArcade_PickOpponentTrainers: fourteen, none twice (a trainer drawn again is drawn anew)
        var picked = BattleArcadeChallenge.PickTrainers(Single, 0, 14, Draws(0, 5, 5, 6, 6, 6, 7));
        Assert.Equal((14, 14), (picked.Count, picked.Distinct().Count()));
        Assert.Equal(new[] { 5, 6, 7 }, picked.Take(3));
        Assert.All(picked.Take(6).Concat(picked.Skip(7).Take(6)), t => Assert.InRange(t, 0, 98));
        Assert.All(new[] { picked[6], picked[13] }, t => Assert.InRange(t, 100, 118));

        // SelectTrainersAndFirstBattlesMons: the first trainer's three at level 50 with nothing held, and the cursor's speed by the round
        var challenge = Begun(new FrontierRecords());
        Assert.Equal((14, challenge.Trainers[0], 3, 3), (challenge.Trainers.Count, challenge.Foe!.Number, challenge.Foe.Pokemon.Count, challenge.Speed));
        Assert.All(challenge.Foe.Pokemon, p => Assert.True(p.Level == 50 && p.HeldItem == null && p.CurrentHP == p.MaxHP));
        Assert.Equal(3, challenge.Foe.Pokemon.Select(p => p.Species.Name).Distinct().Count());
        Assert.Equal(new[] { 3, 3, 4, 4, 5, 5 }, new[] { 0, 21, 28, 42, 49, 70 }.Select(streak => Begun(new FrontierRecords(), streak: streak).Speed));
        Assert.Equal(Silver, Begun(new FrontierRecords(), streak: 14).Trainers[6]);
        Assert.NotEqual(Silver, Begun(new FrontierRecords(), Double, streak: 14).Trainers[6]);

        // BattleArcade_GetAIMask: no routine in the first two rounds, the basic one in the next two, all three after, and always for Dahlia
        Assert.Equal(new[] { AiFlags.None, AiFlags.None, AiFlags.Basic, AiFlags.Basic, FrontierTrainers.Mind, FrontierTrainers.Mind },
            new[] { 0, 7, 14, 21, 28, 63 }.Select(streak => Begun(new FrontierRecords(), streak: streak).Mind));
        var third = Begun(new FrontierRecords(), streak: 14);
        for (int i = 0; i < 6; i++) third.Won(new FrontierRecords());
        Assert.Equal((FrontierPrint.Silver, FrontierTrainers.Mind), (third.DahliaNext, third.Mind));
    }

    // ------------------------------------------------------------------ the game board

    [Fact]
    public void TheEventsThatMayComeUpGoByTheRoundAndTheBattle()
    {
        ArcadeEffect[] At(int round, int battle) => ArcadeBoard.AvailableAt(round, round * 7 + battle).ToArray();
        // The first round: three statuses and a berry for each side, the board's three, and nothing
        Assert.Equal(new[]
        {
            ArcadeEffect.PoisonFoe, ArcadeEffect.ParalyzeFoe, ArcadeEffect.BurnFoe, ArcadeEffect.FoeGetBerry,
            ArcadeEffect.PoisonAlly, ArcadeEffect.ParalyzeAlly, ArcadeEffect.BurnAlly, ArcadeEffect.AllyGetBerry,
            ArcadeEffect.SpeedUp, ArcadeEffect.SlowDown, ArcadeEffect.RandomizeCursor, ArcadeEffect.NoEvent
        }, At(0, 0));
        // Before its last battle the board is left alone: nothing of the battle's kind is left at all
        Assert.Equal(9, At(0, 6).Length);
        Assert.DoesNotContain(At(0, 6), e => ArcadeBoard.CategoryOf(e) == ArcadeCategory.Field);
        // The second round: HP, a berry and levels for each side, the four weathers and the board's three
        Assert.Equal(new[]
        {
            ArcadeEffect.LowerFoeHp, ArcadeEffect.FoeGetBerry, ArcadeEffect.FoeLevelUp, ArcadeEffect.LowerAllyHp, ArcadeEffect.AllyGetBerry, ArcadeEffect.AllyLevelUp,
            ArcadeEffect.Sunny, ArcadeEffect.Rainy, ArcadeEffect.Sandy, ArcadeEffect.Hail, ArcadeEffect.SpeedUp, ArcadeEffect.SlowDown, ArcadeEffect.RandomizeCursor, ArcadeEffect.NoEvent
        }, At(1, 0));
        // The fifth: sleep and ice, items, and the bonuses, a Battle Point before the odd battles and a battle passed before the even ones
        Assert.Equal(new[]
        {
            ArcadeEffect.LowerFoeHp, ArcadeEffect.SleepFoe, ArcadeEffect.FreezeFoe, ArcadeEffect.FoeGetItem, ArcadeEffect.FoeLevelUp,
            ArcadeEffect.LowerAllyHp, ArcadeEffect.SleepAlly, ArcadeEffect.FreezeAlly, ArcadeEffect.AllyGetItem, ArcadeEffect.AllyLevelUp,
            ArcadeEffect.SpeedUp, ArcadeEffect.RandomizeCursor, ArcadeEffect.SwapMons, ArcadeEffect.Get1Bp, ArcadeEffect.NoEvent
        }, At(4, 0));
        Assert.Equal(new[] { ArcadeEffect.SwapMons, ArcadeEffect.NoBattle, ArcadeEffect.NoEvent }, At(4, 1).Where(e => ArcadeBoard.CategoryOf(e) == ArcadeCategory.Bonus));
        Assert.Equal(new[] { ArcadeEffect.NoEvent }, At(4, 6).Where(e => ArcadeBoard.CategoryOf(e) is ArcadeCategory.Bonus or ArcadeCategory.Field));
        // From the seventh on everything may come up: thirty of the thirty-two before an odd battle, thirty-one before an even one, twenty-five before the last
        Assert.Equal((30, 31, 25), (At(6, 0).Length, At(6, 1).Length, At(6, 6).Length));
        Assert.Equal(At(6, 1), At(11, 1));
        Assert.Contains(ArcadeEffect.Get3Bp, At(6, 3));
        Assert.DoesNotContain(ArcadeEffect.Get3Bp, At(5, 3));
        // A streak that can count no further is always before a round's last battle
        Assert.Equal(At(6, 6), ArcadeBoard.AvailableAt(8, 9999));
        // Every list is in the events' own order, and has something for the trainer, the challenger and a bonus
        foreach (int round in Enumerable.Range(0, 9))
            foreach (int battle in Enumerable.Range(0, 7))
            {
                var list = At(round, battle);
                Assert.Equal(list.OrderBy(e => e), list);
                Assert.Contains(list, e => ArcadeBoard.CategoryOf(e) == ArcadeCategory.Foe);
                Assert.Contains(list, e => ArcadeBoard.CategoryOf(e) == ArcadeCategory.Ally);
                Assert.Contains(ArcadeEffect.NoEvent, list);
            }
    }

    [Fact]
    public void ThePanelsAreDrawnByHowTheLastBattleWent()
    {
        // GetEffectCategoryCounts: the first score the last battle reached
        Assert.Equal(new[] { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4 }, new[] { 24, 21, 20, 16, 15, 10, 9, 5, 4, 0 }.Select(ArcadeBoard.TierOf));

        // AssignEffectsToGrid over the first battle's twelve events, with the best row of weights (15, 15, 40, 30): a
        // draw by a hundred for the kind and a draw by the kind's count for the event
        var available = ArcadeBoard.AvailableAt(0, 0);
        var panels = ArcadeBoard.Assign(available, 0, Draws(0,
            0, 5,        // the trainer's; 5 % 4 = 1
            14, 3,       // the trainer's still
            15, 0,       // the challenger's
            29, 7,       // 7 % 4 = 3
            30, 2,       // the battle's; 2 % 3
            69, 3,
            70, 9999,    // a bonus: only one to be had
            99, 1,
            114, 4,      // 114 % 100 = 14: the trainer's
            40, 1, 40, 1, 40, 1, 40, 1, 40, 1, 40, 1, 40, 1));
        Assert.Equal(new[]
        {
            ArcadeEffect.ParalyzeFoe, ArcadeEffect.FoeGetBerry, ArcadeEffect.PoisonAlly, ArcadeEffect.AllyGetBerry, ArcadeEffect.RandomizeCursor, ArcadeEffect.SpeedUp,
            ArcadeEffect.NoEvent, ArcadeEffect.NoEvent, ArcadeEffect.PoisonFoe,
            ArcadeEffect.SlowDown, ArcadeEffect.SlowDown, ArcadeEffect.SlowDown, ArcadeEffect.SlowDown, ArcadeEffect.SlowDown, ArcadeEffect.SlowDown, ArcadeEffect.SlowDown
        }, panels);
        // The worst row (10, 75, 10, 5) is mostly the challenger's own
        Assert.Equal(new[] { ArcadeCategory.Foe, ArcadeCategory.Ally, ArcadeCategory.Ally, ArcadeCategory.Field, ArcadeCategory.Bonus },
            new[] { 9, 10, 84, 85, 95 }.Select(roll => ArcadeBoard.CategoryOf(ArcadeBoard.Assign(available, 4, Draws(0, roll, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))[0])));

        // A kind with no event to give, before the first round's last battle: the walk goes on to the next kind that
        // has one, which is the bonuses' "nothing" (and the draw among none is still made)
        var last = ArcadeBoard.AvailableAt(0, 6);
        int drawn = 0;
        var walked = ArcadeBoard.Assign(last, 0, () => drawn++ % 2 == 0 ? 40 : 777);
        Assert.All(walked, e => Assert.Equal(ArcadeEffect.NoEvent, e));
        Assert.Equal(32, drawn);

        // BattleArcadeApp_Init: the board's number, the panel the cursor starts on, and then two draws a panel
        drawn = 0;
        var values = new[] { 0x1234, 21 };
        var board = new ArcadeBoard(0, 0, 16, 3, false, () => drawn < 2 ? values[drawn++] : drawn++ % 2 == 0 ? 0 : 1);
        Assert.Equal((0x1234, 5, 34, 1, 12), (board.RandomIndex, board.Cursor, drawn, board.Tier, board.Available.Count));
        Assert.All(board.Panels, e => Assert.Equal(ArcadeEffect.ParalyzeFoe, e));
        Assert.Equal((ArcadeBoardStep.Opening, ArcadeEffect.None, ArcadeBoard.TimeLimit), (board.Step, board.Effect, board.TimeLeft));
        // Whatever the draws, a board holds only events that may come up
        foreach (int round in new[] { 0, 3, 4, 9 })
            foreach (int battle in new[] { 0, 1, 6 })
            {
                var any = new ArcadeBoard(round, round * 7 + battle, battle * 4, 3, false, Draws(round * 10 + battle));
                Assert.Equal(16, any.Panels.Count);
                Assert.All(any.Panels, e => Assert.Contains(e, any.Available));
            }
    }

    private static void Run(ArcadeBoard board, int frames)
    {
        for (int i = 0; i < frames; i++) board.Advance();
    }

    [Fact]
    public void TheBoardCountsDownAndItsCursorRunsUntilItIsStopped()
    {
        // State_OperateRoulette at the fourth speed: a frame to begin, ten of nothing, then three, two and one for
        // twenty-four frames each, and the cursor shown on the frame the count ends
        var board = new ArcadeBoard(0, 0, 0, 3, false, Draws(4, 99, 5));
        Assert.Equal(5, board.Cursor);
        Run(board, 11);
        Assert.Equal(ArcadeBoardStep.Opening, board.Step);
        board.Advance();
        Assert.Equal((ArcadeBoardStep.Three, true), (board.Step, board.Stepped));
        Run(board, 23);
        Assert.Equal(ArcadeBoardStep.Three, board.Step);
        board.Advance();
        Assert.Equal(ArcadeBoardStep.Two, board.Step);
        Run(board, 24);
        Assert.Equal(ArcadeBoardStep.One, board.Step);
        // The button does nothing before the cursor runs
        for (int i = 0; i < 23; i++) board.Advance(press: true);
        Assert.Equal(ArcadeBoardStep.One, board.Step);
        board.Advance();
        Assert.Equal((ArcadeBoardStep.Running, 84, 5, ArcadeBoard.TimeLimit), (board.Step, board.Frame, board.Cursor, board.TimeLeft));
        // UpdateCursorPosition: a panel every four frames at this speed, on round the board
        Run(board, 3);
        Assert.Equal((5, false), (board.Cursor, board.Ticked));
        board.Advance();
        Assert.Equal((6, true, ArcadeBoard.TimeLimit - 4), (board.Cursor, board.Ticked, board.TimeLeft));
        Run(board, 40);
        Assert.Equal(0, board.Cursor);
        Assert.Equal(ArcadeEffect.None, board.Effect);
        // The cursor moves before the button is looked at: pressed on the frame it moves, the panel it came to is taken
        Run(board, 3);
        board.Advance(press: true);
        Assert.Equal((1, ArcadeBoardStep.Running, true), (board.Cursor, board.Step, board.Pressed));
        board.Advance();
        Assert.Equal((ArcadeBoardStep.Stopped, board.Panels[1]), (board.Step, board.Effect));
        // The event stays up for thirty frames
        Run(board, 29);
        Assert.False(board.IsDone);
        board.Advance();
        Assert.True(board.IsDone);
        Assert.Equal(board.Panels[1], board.Effect);

        // Left alone, it stops by itself after thirty seconds: 900 frames, 225 panels on, which is once round fourteen times and one more
        var alone = new ArcadeBoard(0, 0, 0, 3, false, Draws(4, 99, 5));
        Run(alone, 84 + 899);
        Assert.Equal((ArcadeBoardStep.Running, 1), (alone.Step, alone.TimeLeft));
        Run(alone, 2);
        Assert.Equal((ArcadeBoardStep.Stopped, false, 6, alone.Panels[6]), (alone.Step, alone.Pressed, alone.Cursor, alone.Effect));

        // sCursorMoveFrames: twenty frames a panel at the slowest, one at the two fastest
        int After(int speed, int frames)
        {
            var paced = new ArcadeBoard(0, 0, 0, speed, false, Draws(4, 99, 0));
            Run(paced, 84 + frames);
            return paced.Cursor;
        }
        Assert.Equal(new[] { 2, 2, 5, 10, 13, 4, 8, 8 }, Enumerable.Range(0, 8).Select(speed => After(speed, 40)));
        // (40 frames: 2 panels at twenty frames each, 2 at sixteen, 5 at eight, 10 at four, 13 at three, 20 at two, 40 at one or at none)

        // A cursor made random jumps to a panel drawn afresh at every move
        int at = 0;
        var jumps = new[] { 7, 16 + 9, 3, 12 };
        var random = new ArcadeBoard(0, 0, 0, 3, true, () => at < 34 ? (at++ == 1 ? 2 : 0) : jumps[(at++ - 34) % jumps.Length]);
        Run(random, 84);
        Assert.Equal((2, true), (random.Cursor, random.Randomized));
        var seen = new List<int>();
        for (int i = 0; i < 16; i++)
        {
            random.Advance();
            if (random.Ticked) seen.Add(random.Cursor);
        }
        Assert.Equal(new[] { 7, 9, 3, 12 }, seen);

        // A tool stops the cursor on the panel it wants
        var aimed = new ArcadeBoard(4, 28, 24, 5, false, Draws(11));
        Assert.Equal(aimed.Panels[13], aimed.PlayTo(13));
        Assert.Equal((13, true, true), (aimed.Cursor, aimed.IsDone, aimed.Pressed));
        var rigged = new ArcadeBoard(0, 0, 0, 3, false, Draws(11));
        rigged.Rig(ArcadeEffect.Get3Bp);
        Assert.Equal(ArcadeEffect.Get3Bp, rigged.PlayTo());
    }

    [Fact]
    public void TheBoardsScreenPlaysItByTheClockAndStopsItWithOneButton()
    {
        var records = new FrontierRecords();
        var challenge = Begun(records);
        var screen = new ArcadeBoardScreen();
        // Nothing to show until the challenge has brought a board up
        screen.Open(challenge);
        Assert.False(screen.IsActive);
        var board = challenge.OpenBoard(Draws(3));
        screen.Open(challenge);
        Assert.True(screen.IsActive);
        Assert.Same(board, screen.Board);
        // A frame of the board is a thirtieth of a second, however the time comes in
        screen.Advance(0.51f);
        Assert.Equal(15, board.Frame);
        screen.Advance(1f / 60f);
        screen.Advance(1f / 60f);
        Assert.Equal(16, board.Frame);
        // Pressed too soon, the button is forgotten
        screen.PressStop();
        screen.Frames(84 - 16);
        Assert.Equal(ArcadeBoardStep.Running, board.Step);
        screen.Frames(10);
        Assert.Equal(ArcadeBoardStep.Running, board.Step);
        // Pressed while the cursor runs, it stops it on the next frame, and the screen closes once the event has been shown
        screen.PressStop();
        screen.Frames(2);
        Assert.Equal((ArcadeBoardStep.Stopped, true), (board.Step, screen.IsActive));
        screen.Frames(40);
        Assert.False(screen.IsActive);
        Assert.True(board.IsDone);
        Assert.Equal(board.Effect, challenge.TakeBoard(Draws(3)));
        Assert.Equal(board.Effect, challenge.Effect);
    }

    // ------------------------------------------------------------------ what each event does

    /// <summary>A challenge whose trainer's three are these, so that a test knows whom an event falls on.</summary>
    private static BattleArcadeChallenge Against(FrontierRecords records, int streak, params Pokemon[] foes)
    {
        var challenge = Begun(records, streak: streak);
        for (int i = 0; i < foes.Length; i++) challenge.Foe!.Pokemon[i] = foes[i];
        return challenge;
    }

    [Fact]
    public void AnEventLowersHpOrGivesAStatusToEveryoneNothingKeepsFromIt()
    {
        // ArcadeEffect_LowerHP: max - ((u32)(max * 1.2) - max)
        Assert.Equal(new[] { 80, 123, 1, 4, 160 }, new[] { 100, 153, 1, 5, 200 }.Select(BattleArcadeChallenge.LoweredHp));
        var records = new FrontierRecords();
        var challenge = Against(records, 0, Mon("Gengar", 50), Mon("Skarmory", 50), Mon("Snorlax", 50, null, "Immunity"));
        var none = Draws(0);
        challenge.Apply(ArcadeEffect.LowerFoeHp, 0, none);
        Assert.All(challenge.Foe!.Pokemon, p => Assert.Equal(BattleArcadeChallenge.LoweredHp(p.MaxHP), p.CurrentHP));
        Assert.All(challenge.Team.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
        challenge.Apply(ArcadeEffect.LowerAllyHp, 0, none);
        Assert.All(challenge.Team.Members, p => Assert.True(p.CurrentHP == BattleArcadeChallenge.LoweredHp(p.MaxHP) && p.CurrentHP < p.MaxHP));

        // ArcadeEffect_Poison: Poison and Steel types and Immunity are kept from it; with everyone kept from it, the event came to nothing
        challenge.Apply(ArcadeEffect.PoisonFoe, 0, none);
        Assert.True(challenge.Immune);
        Assert.All(challenge.Foe.Pokemon, p => Assert.Equal(StatusCondition.None, p.Status));
        challenge.Apply(ArcadeEffect.PoisonAlly, 0, none);
        Assert.False(challenge.Immune);
        Assert.All(challenge.Team.Members, p => Assert.Equal(StatusCondition.Poison, p.Status));
        challenge.Foe.Pokemon[1] = Mon("Luxray", 50);
        challenge.Apply(ArcadeEffect.PoisonFoe, 0, none);
        Assert.False(challenge.Immune);
        Assert.Equal(new[] { StatusCondition.None, StatusCondition.Poison, StatusCondition.None }, challenge.Foe.Pokemon.Select(p => p.Status));

        // ArcadeEffect_Paralyze: Ground types and Limber (the challenger's Garchomp is one)
        challenge = Against(records, 0, Mon("Persian", 50, null, "Limber"), Mon("Persian", 50, null, "Technician"), Mon("Golem", 50));
        challenge.Apply(ArcadeEffect.ParalyzeFoe, 0, none);
        Assert.Equal(new[] { StatusCondition.None, StatusCondition.Paralyze, StatusCondition.None }, challenge.Foe!.Pokemon.Select(p => p.Status));
        challenge.Apply(ArcadeEffect.ParalyzeAlly, 0, none);
        Assert.Equal((false, StatusCondition.None, StatusCondition.Paralyze, StatusCondition.Paralyze),
            (challenge.Immune, challenge.Team.Members[0].Status, challenge.Team.Members[1].Status, challenge.Team.Members[2].Status));

        // ArcadeEffect_Burn: Fire types and Water Veil
        challenge = Against(records, 0, Mon("Infernape", 50), Mon("Wailord", 50, null, "Water Veil"), Mon("Wailord", 50, null, "Oblivious"));
        challenge.Apply(ArcadeEffect.BurnFoe, 0, none);
        Assert.Equal(new[] { StatusCondition.None, StatusCondition.None, StatusCondition.Burn }, challenge.Foe!.Pokemon.Select(p => p.Status));
        challenge.Foe.Pokemon[2] = Mon("Infernape", 50);
        challenge.Apply(ArcadeEffect.BurnFoe, 0, none);
        Assert.True(challenge.Immune);
    }

    [Fact]
    public void SleepAndIceFallOnOneWhomTheBoardsNumberNames()
    {
        var records = new FrontierRecords();
        // ArcadeEffect_Sleep: the place is the board's number by three; sleep lasts two to five turns, on a draw of its own
        var challenge = Against(records, 28, Mon("Honchkrow", 50, null, "Insomnia"), Mon("Primeape", 50, null, "Vital Spirit"), Mon("Luxray", 50));
        int drawn = 0;
        Func<int> seven = () => { drawn++; return 7; };
        challenge.Apply(ArcadeEffect.SleepAlly, 4, seven);
        Assert.Equal(new[] { StatusCondition.None, StatusCondition.Sleep, StatusCondition.None }, challenge.Team.Members.Select(p => p.Status));
        Assert.Equal((7 % 4 + 2, 1, false), (challenge.Team.Members[1].SleepTurns, drawn, challenge.Immune));
        // Insomnia and Vital Spirit are passed over, on round the three
        challenge.Apply(ArcadeEffect.SleepFoe, 0, () => 0);
        Assert.Equal(new[] { StatusCondition.None, StatusCondition.None, StatusCondition.Sleep }, challenge.Foe!.Pokemon.Select(p => p.Status));
        Assert.Equal(2, challenge.Foe.Pokemon[2].SleepTurns);
        challenge.Foe.Pokemon[2] = Mon("Honchkrow", 50, null, "Insomnia");
        drawn = 0;
        challenge.Apply(ArcadeEffect.SleepFoe, 5, seven);
        Assert.Equal((true, 0), (challenge.Immune, drawn));
        Assert.All(challenge.Foe.Pokemon, p => Assert.Equal(StatusCondition.None, p.Status));

        // ArcadeEffect_Freeze: Ice types and Magma Armor
        challenge = Against(records, 28, Mon("Weavile", 50), Mon("Camerupt", 50, null, "Magma Armor"), Mon("Luxray", 50));
        challenge.Apply(ArcadeEffect.FreezeFoe, 3, seven);
        Assert.Equal(new[] { StatusCondition.None, StatusCondition.None, StatusCondition.Freeze }, challenge.Foe!.Pokemon.Select(p => p.Status));
        challenge.Apply(ArcadeEffect.FreezeAlly, 5, seven);
        Assert.Equal(new[] { StatusCondition.None, StatusCondition.None, StatusCondition.Freeze }, challenge.Team.Members.Select(p => p.Status));
        challenge.Foe.Pokemon[2] = Mon("Weavile", 50);
        challenge.Apply(ArcadeEffect.FreezeFoe, 1, seven);
        Assert.True(challenge.Immune);
    }

    [Fact]
    public void ABerryOrAnItemIsLentByTheRoundAndLevelsAreRaisedByThree()
    {
        var arcade = FrontierDatabase.Arcade;
        var records = new FrontierRecords();
        var none = Draws(0);
        // ArcadeEffect_GetBerry and _GetItem: the first list in rounds one to three, the second in four to six, the third after; the board's number picks
        var first = Begun(records);
        first.Apply(ArcadeEffect.AllyGetBerry, 10, none);
        Assert.Equal((arcade.Berries[0][10 % 8], "Pecha Berry"), (first.Given, first.Given));
        Assert.All(first.Team.Members, p => Assert.Equal("Pecha Berry", p.HeldItem?.Name));
        Assert.All(first.Foe!.Pokemon, p => Assert.Null(p.HeldItem));
        first.Apply(ArcadeEffect.FoeGetItem, 23, none);
        Assert.Equal("Quick Claw", first.Given);
        Assert.All(first.Foe.Pokemon, p => Assert.Equal("Quick Claw", p.HeldItem?.Name));
        var fourth = Begun(new FrontierRecords(), streak: 21);
        fourth.Apply(ArcadeEffect.AllyGetItem, 13, none);
        Assert.Equal((3, "White Herb"), (fourth.Round, fourth.Given));
        fourth.Apply(ArcadeEffect.FoeGetBerry, 16, none);
        Assert.Equal("Chilan Berry", fourth.Given);
        var seventh = Begun(new FrontierRecords(), streak: 42);
        seventh.Apply(ArcadeEffect.AllyGetItem, 21, none);
        Assert.Equal("Choice Scarf", seventh.Given);
        seventh.Apply(ArcadeEffect.AllyGetBerry, 19, none);
        Assert.Equal("Starf Berry", seventh.Given);
        Assert.All(seventh.Team.Members, p => Assert.Equal("Starf Berry", p.HeldItem?.Name));
        // An event that lends nothing names nothing
        seventh.Apply(ArcadeEffect.NoEvent, 0, none);
        Assert.Null(seventh.Given);

        // ArcadeEffect_LevelUp: three levels, the stats with them, and whole
        var challenge = Begun(new FrontierRecords());
        int[] before = challenge.Team.Members.Select(p => p.MaxHP).ToArray();
        challenge.Apply(ArcadeEffect.AllyLevelUp, 0, none);
        Assert.Equal(new[] { 53, 53, 48 }, challenge.Team.Members.Select(p => p.Level));
        Assert.All(challenge.Team.Members.Zip(before), p => Assert.True(p.First.MaxHP > p.Second && p.First.CurrentHP == p.First.MaxHP));
        challenge.Apply(ArcadeEffect.FoeLevelUp, 0, none);
        Assert.All(challenge.Foe!.Pokemon, p => Assert.True(p.Level == 53 && p.CurrentHP == p.MaxHP));
    }

    [Fact]
    public void TheBattlesEventsSetItsWeatherAndTheBoardsOwnChangeTheNextBoard()
    {
        var records = new FrontierRecords();
        var challenge = Begun(records);
        var none = Draws(0);
        // ArcadeEffect_Sunny to _TrickRoom: the field's weather the battle is handed
        var expected = new[]
        {
            (ArcadeEffect.Sunny, BattleWeather.Sun, false), (ArcadeEffect.Rainy, BattleWeather.Rain, false), (ArcadeEffect.Sandy, BattleWeather.Sandstorm, false),
            (ArcadeEffect.Hail, BattleWeather.Hail, false), (ArcadeEffect.Foggy, BattleWeather.Fog, false), (ArcadeEffect.TrickRoom, BattleWeather.None, true)
        };
        foreach (var (effect, weather, room) in expected)
        {
            challenge.Apply(effect, 0, none);
            Assert.Equal(new FrontierConditions(weather, room), challenge.Conditions);
            Assert.Equal(challenge.Conditions, challenge.ForBattle().Conditions);
        }
        // The next board clears it (SetupBattleArcadeAppArgs)
        challenge.OpenBoard(none);
        Assert.Equal(new FrontierConditions(), challenge.Conditions);

        // ArcadeEffect_SpeedUp and _SlowDown: a step either way, within the eight speeds, for the rest of the round
        Assert.Equal(3, challenge.Speed);
        challenge.Apply(ArcadeEffect.SpeedUp, 0, none);
        Assert.Equal((4, 4), (challenge.Speed, challenge.OpenBoard(none).Speed));
        for (int i = 0; i < 6; i++) challenge.Apply(ArcadeEffect.SpeedUp, 0, none);
        Assert.Equal(7, challenge.Speed);
        for (int i = 0; i < 9; i++) challenge.Apply(ArcadeEffect.SlowDown, 0, none);
        Assert.Equal(0, challenge.Speed);
        // ArcadeEffect_RandomizeCursor: the next board's cursor jumps, and that board alone
        challenge.Apply(ArcadeEffect.RandomizeCursor, 0, none);
        Assert.True(challenge.Randomized);
        Assert.True(challenge.OpenBoard(none).Randomized);
        Assert.False(challenge.Randomized);
        Assert.False(challenge.OpenBoard(none).Randomized);

        // The bonuses change nothing of either three: a Battle Point or three on the spot (BA_FUNC_GET_FREE_BP, RECORD_UNK_067)
        challenge.Apply(ArcadeEffect.Get1Bp, 0, none);
        Assert.Equal((1, 1, 1, 1), (challenge.FreePoints, challenge.GiveFreePoints(records), records.BattlePoints, records.Arcade.BoardPoints));
        challenge.Apply(ArcadeEffect.Get3Bp, 0, none);
        Assert.Equal((3, 3, 4, 4, 4), (challenge.FreePoints, challenge.GiveFreePoints(records), records.BattlePoints, records.Arcade.BoardPoints, records.PointsReceived));
        challenge.Apply(ArcadeEffect.NoEvent, 0, none);
        Assert.Equal((0, 0, 4), (challenge.FreePoints, challenge.GiveFreePoints(records), records.BattlePoints));
        Assert.All(challenge.Team.Members.Concat(challenge.Foe!.Pokemon), p => Assert.True(p.Status == StatusCondition.None && p.HeldItem == null && p.CurrentHP == p.MaxHP));
    }

    // ------------------------------------------------------------------ a battle, and what it leaves

    [Fact]
    public void ABattleIsFoughtWithCopiesAndOnlyWhatIsHeldCarriesOn()
    {
        var records = new FrontierRecords();
        var challenge = Begun(records);
        var none = Draws(0);
        challenge.Apply(ArcadeEffect.AllyGetBerry, 2, none);
        challenge.Apply(ArcadeEffect.PoisonAlly, 0, none);
        // BattleArcade_SetupBattle: the battle is handed copies as the board left them, and the three themselves are whole again
        var fought = challenge.ForBattle();
        Assert.Equal((false, 3, 3), (fought.Swapped, fought.Mine.Count, fought.Theirs.Party.Count));
        Assert.Same(fought.Mine, fought.Own);
        Assert.All(fought.Mine.Members, p => Assert.True(p.Status == StatusCondition.Poison && p.HeldItem?.Name == "Pecha Berry" && !challenge.Team.Members.Contains(p)));
        Assert.All(challenge.Team.Members, p => Assert.True(p.Status == StatusCondition.None && p.HeldItem?.Name == "Pecha Berry"));
        Assert.Equal(challenge.Foe!.Pokemon.Select(p => p.Species.Name), fought.Theirs.Party.Members.Select(p => p.Species.Name));
        Assert.All(fought.Theirs.Party.Members, p => Assert.DoesNotContain(p, challenge.Foe.Pokemon));
        Assert.Equal(("frontier_" + challenge.Foe.Record.Id, AiFlags.None, 0), (fought.Theirs.Id, fought.Theirs.Ai, fought.Theirs.PrizeMoney));

        // The battle: the first ate its berry and fainted, the second is still poisoned, the third came through; four turns
        fought.Mine.Members[0].HeldItem = null;
        fought.Mine.Members[0].CurrentHP = 0;
        fought.Mine.Members[0].Status = StatusCondition.None;
        fought.Mine.Members[2].Status = StatusCondition.None;
        // BattleArcade_GetPerformance: 6 for one with a status, 4 for one fainted, 6 for under five turns
        Assert.Equal(16, BattleArcadeChallenge.PerformanceOf(fought.Mine, 4));
        challenge.AfterBattle(fought, 4);
        Assert.Equal((16, 1), (challenge.Performance, challenge.OpenBoard(none).Tier));
        // BattleArcade_SaveItemsAfterBattle: what is still held is kept, and nothing else of the battle
        Assert.Equal(new[] { null, "Pecha Berry", "Pecha Berry" }, challenge.Team.Members.Select(p => p.HeldItem?.Name));
        Assert.All(challenge.Team.Members, p => Assert.True(p.Status == StatusCondition.None && p.CurrentHP == p.MaxHP));
        Assert.Equal(new[] { null, "Pecha Berry", "Pecha Berry" }, challenge.ForBattle().Mine.Members.Select(p => p.HeldItem?.Name));

        // Every score by hand
        Party Fought(int fainted, int ailing)
        {
            var three = PartyOf(Mon("Garchomp", 50), Mon("Luxray", 50), Mon("Staraptor", 50));
            for (int i = 0; i < fainted; i++) three.Members[i].CurrentHP = 0;
            for (int i = 0; i < ailing; i++) three.Members[2 - i].Status = StatusCondition.Burn;
            return three;
        }
        Assert.Equal(new[] { 24, 24, 24, 20, 20, 18, 18, 16, 16, 14, 14 }, Enumerable.Range(0, 11).Select(turns => BattleArcadeChallenge.PerformanceOf(Fought(0, 0), turns)));
        Assert.Equal(new[] { 8 + 6 + 10, 6 + 4 + 10, 4 + 2 + 10, 0 + 0 + 10 }, Enumerable.Range(0, 4).Select(n => BattleArcadeChallenge.PerformanceOf(Fought(n, n), 0)));
        Assert.Equal(0, BattleArcadeChallenge.PerformanceOf(Fought(3, 3), 30));
    }

    [Fact]
    public void ASwapHandsEachSideTheOthersThreeAndLevelsRaisedComeOffAgain()
    {
        var records = new FrontierRecords();
        var challenge = Begun(records, streak: 28);
        var none = Draws(0);
        string[] mine = { "Garchomp", "Luxray", "Staraptor" }, theirs = challenge.Foe!.Pokemon.Select(p => p.Species.Name).ToArray();
        challenge.Apply(ArcadeEffect.AllyGetItem, 0, none);
        var fought = challenge.ForBattle();
        fought.Own.Members[1].HeldItem = null;
        challenge.AfterBattle(fought, 9);
        // ARCADE_EFFECT_SWAP_MONS: the challenger battles with the trainer's three and the trainer with theirs
        challenge.Apply(ArcadeEffect.SwapMons, 0, none);
        var swapped = challenge.ForBattle();
        Assert.True(swapped.Swapped);
        Assert.Equal(theirs, swapped.Mine.Members.Select(p => p.Species.Name));
        Assert.Equal(mine, swapped.Theirs.Party.Members.Select(p => p.Species.Name));
        Assert.Same(swapped.Theirs.Party, swapped.Own);
        Assert.Equal(new[] { "White Herb", null, "White Herb" }, swapped.Own.Members.Select(p => p.HeldItem?.Name));
        Assert.All(swapped.Mine.Members, p => Assert.Null(p.HeldItem));
        // The battle is scored by the three the challenger fought with, and what their own three hold comes back from the other side
        swapped.Mine.Members[0].CurrentHP = 0;
        swapped.Mine.Members[1].CurrentHP = 0;
        swapped.Own.Members[0].HeldItem = null;
        challenge.AfterBattle(swapped, 2);
        Assert.Equal(8 + 2 + 10, challenge.Performance);
        Assert.Equal(new[] { null, null, "White Herb" }, challenge.Team.Members.Select(p => p.HeldItem?.Name));

        // BattleArcade_CleanupBattle: three levels the board added come off again, and the stats with them
        int[] hp = challenge.Team.Members.Select(p => p.MaxHP).ToArray();
        challenge.Apply(ArcadeEffect.AllyLevelUp, 0, none);
        var raised = challenge.ForBattle();
        Assert.Equal(new[] { 53, 53, 48 }, raised.Mine.Members.Select(p => p.Level));
        challenge.AfterBattle(raised, 3);
        Assert.Equal(new[] { 50, 50, 45 }, challenge.Team.Members.Select(p => p.Level));
        Assert.Equal(hp, challenge.Team.Members.Select(p => p.MaxHP));
        Assert.All(challenge.Team.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
    }

    // ------------------------------------------------------------------ rounds, records and pauses

    /// <summary>Seven battles won, or the first so many of them, a foe drawn before each.</summary>
    private static void Win(BattleArcadeChallenge challenge, FrontierRecords records, Func<int> draws, int battles = 7)
    {
        for (int i = 0; i < battles; i++)
        {
            if (challenge.Battle > 0) challenge.DrawFoe(draws);
            challenge.Won(records);
        }
    }

    [Fact]
    public void RoundsWonEarnBattlePointsAndTheStreakRunsOn()
    {
        // BattleArcade_GetEarnedBP: two for each of the first four rounds, four for the next two, five, and six from the eighth; twenty for a Single round that Dahlia ended
        foreach (var (way, expected) in new[] { (Single, new[] { 2, 2, 20, 2, 4, 4, 20, 6, 6, 6 }), (Double, new[] { 2, 2, 2, 2, 4, 4, 5, 6, 6, 6 }) })
        {
            var records = new FrontierRecords();
            var earned = new List<int>();
            for (int round = 0; round < expected.Length; round++)
            {
                var challenge = BattleArcadeChallenge.Begin(records, way, Three(), All);
                var draws = Draws(round);
                challenge.DrawRound(draws);
                Assert.Equal((round * 7, round, round == 2 && way == Single ? Silver : -1), (challenge.Streak, challenge.Round, round == 2 && way == Single ? challenge.Trainers[6] : -1));
                Win(challenge, records, draws);
                Assert.True(challenge.RoundDone);
                challenge.EndRound(records);
                earned.Add(challenge.PointsEarned);
                Assert.Equal((round * 7 + 7, round * 7 + 7, true), (records.Get(BattleArcadeChallenge.LatestStreakStat(way)), records.Get(BattleArcadeChallenge.RecordStreakStat(way)), records.IsRunning(FrontierFacility.Arcade, (int)way)));
            }
            Assert.Equal(expected, earned);
            Assert.Equal(70, records.VictoriesAt(FrontierFacility.Arcade));
            Assert.False(records.IsRunning(FrontierFacility.Arcade, (int)(way == Single ? Double : Single)));
        }

        // A battle the board passed counts for the round and the streak, not for the battles won (_1FFC)
        var passing = new FrontierRecords();
        var passed = Begun(passing, streak: 28);
        passed.Won(passing);
        passed.Skipped();
        Assert.Equal((2, 30, 1), (passed.Battle, passed.Streak, passing.VictoriesAt(FrontierFacility.Arcade)));
        // The streak counts no further than 9,999
        var far = Begun(new FrontierRecords(), streak: 9999);
        far.Won(new FrontierRecords());
        Assert.Equal((9999, 1), (far.Streak, far.Battle));
    }

    [Fact]
    public void ALossEndsTheStreakAndAWalkOutForgetsIt()
    {
        var records = new FrontierRecords();
        var draws = Draws(5);
        var challenge = Begun(records);
        Win(challenge, records, draws);
        challenge.EndRound(records);
        // BattleArcade_SaveOnLoss, three battles into the next round: the streak stands at ten, is the record, and runs no more
        challenge = BattleArcadeChallenge.Begin(records, Single, Three(), All);
        challenge.DrawRound(draws);
        Win(challenge, records, draws, 3);
        challenge.EndLost(records);
        Assert.Equal((10, 10, false, false), (records.Get(BattleArcadeChallenge.LatestStreakStat(Single)), records.Get(BattleArcadeChallenge.RecordStreakStat(Single)),
            records.IsRunning(FrontierFacility.Arcade, (int)Single), challenge.RoundWon));
        // The next challenge begins from nothing, and a shorter streak leaves the record alone
        challenge = BattleArcadeChallenge.Begin(records, Single, Three(), All);
        Assert.Equal((0, 0), (challenge.Streak, challenge.Round));
        challenge.DrawRound(draws);
        Win(challenge, records, draws, 2);
        challenge.EndLost(records);
        Assert.Equal((2, 10), (records.Get(BattleArcadeChallenge.LatestStreakStat(Single)), records.Get(BattleArcadeChallenge.RecordStreakStat(Single))));

        // BattleArcade_ClearActiveStreak: a challenge left without being put down
        records.SetRunning(FrontierFacility.Arcade, (int)Double, true);
        records.Set(BattleArcadeChallenge.LatestStreakStat(Double), 21);
        records.Set(BattleArcadeChallenge.RecordStreakStat(Double), 21);
        BattleArcadeChallenge.Forfeit(records, Double);
        Assert.Equal((0, 21, false), (records.Get(BattleArcadeChallenge.LatestStreakStat(Double)), records.Get(BattleArcadeChallenge.RecordStreakStat(Double)), records.IsRunning(FrontierFacility.Arcade, (int)Double)));
    }

    [Fact]
    public void AChallengePutDownIsTakenUpWithTheBoardAsItStoodAndWhatWasHeld()
    {
        var records = new FrontierRecords();
        var party = Three();
        var challenge = BattleArcadeChallenge.Begin(records, Single, party, All);
        var draws = Draws(8);
        challenge.DrawRound(draws);
        challenge.Apply(ArcadeEffect.AllyGetBerry, 6, draws);
        challenge.Team.Members[1].HeldItem = null;
        challenge.Apply(ArcadeEffect.SpeedUp, 0, draws);
        challenge.Apply(ArcadeEffect.RandomizeCursor, 0, draws);
        challenge.AfterBattle(challenge.ForBattle(), 1);
        challenge.Team.Members[1].HeldItem = null;
        Win(challenge, records, draws, 2);
        var trainers = challenge.Trainers.ToList();
        challenge.Pause(records, party);
        // BA_FUNC_QUICKSAVE: the latest streak is written; the record and whether the streak runs are not
        Assert.Equal((2, 0, false), (records.Get(BattleArcadeChallenge.LatestStreakStat(Single)), records.Get(BattleArcadeChallenge.RecordStreakStat(Single)), records.IsRunning(FrontierFacility.Arcade, (int)Single)));
        Assert.True(BattleArcadeChallenge.StillValid(records, party));
        // It survives a save
        var saved = GameDataFiles.Deserialize<FrontierRecords>(GameDataFiles.Serialize(records));
        var resumed = BattleArcadeChallenge.Resume(saved, party)!;
        Assert.Equal((2, 2, 0, true, 4, true), (resumed.Battle, resumed.Streak, resumed.Round, resumed.Resumed, resumed.Speed, resumed.Randomized));
        Assert.Equal(trainers, resumed.Trainers);
        Assert.Equal(new[] { "Sitrus Berry", null, "Sitrus Berry" }, resumed.Team.Members.Select(p => p.HeldItem?.Name));
        Assert.Equal(new[] { 50, 50, 45 }, resumed.Team.Members.Select(p => p.Level));
        Assert.Null(saved.Arcade.Paused);
        Assert.Null(BattleArcadeChallenge.Resume(saved, party));
        // How the last battle went isn't kept: the first board afterwards is laid out by the worst row
        Assert.Equal(0, resumed.Performance);
        // The trainer to come is drawn as the challenge goes on, and it is no longer one just taken up
        Assert.Null(resumed.Foe);
        Assert.Throws<InvalidOperationException>(() => resumed.OpenBoard(draws));
        resumed.DrawFoe(draws);
        Assert.Equal((trainers[2], false), (resumed.Foe!.Number, resumed.Resumed));
        var board = resumed.OpenBoard(draws);
        Assert.Equal((4, true, 4, 2), (board.Speed, board.Randomized, board.Tier, board.Streak));

        // Only the three it was entered with take it up
        (party.Members[0], party.Members[2]) = (party.Members[2], party.Members[0]);
        Assert.False(BattleArcadeChallenge.StillValid(records, party));
    }

    // ------------------------------------------------------------------ the rooms

    [Fact]
    public void TheArcadesRoomsAreLaidOutAsTheOriginalHasThem()
    {
        // The lobby (events_battle_arcade, land data 324): come into from the west, the doorway north behind the attendants
        var lobby = Room("BattleArcade");
        Assert.Equal((23, 17, true, InteriorStyle.GameHall), (lobby.Width, lobby.Height, lobby.IsIndoors, lobby.Interior));
        var out_ = lobby.Warps.Where(w => w.TargetMap == "BattleFrontier").ToList();
        Assert.Equal(new[] { (0, 5), (0, 6), (0, 7) }, out_.Select(w => (w.SourceX, w.SourceY)));
        Assert.All(out_, w => Assert.Equal((59, 62, Direction.Left), (w.TargetX, w.TargetY, w.TargetFacing)));
        // The original comes down three flights from its door and has a lounge below; the floor is level in this game (the rulings)
        Assert.Equal(lobby.HeightAt(2, 6), lobby.HeightAt(20, 6));
        Assert.Equal(lobby.HeightAt(2, 6), lobby.HeightAt(20, 14));
        // The attendants before the doorway, the records' machine and the PC beside them
        Assert.Equal(new[] { ("MultiAttendant", 15, 5), ("SingleAttendant", 14, 5) }, lobby.Everyone.Where(n => n.Key?.EndsWith("attendant") == true).Select(n => (n.Script!, n.GridX, n.GridY)).OrderBy(n => n));
        Assert.Equal("RecordsMachine", lobby.SignScripts[(16, 5)]);
        Assert.Contains(lobby.Everyone, n => n.IsPCTerminal && (n.GridX, n.GridY) == (17, 5));
        var north = Assert.Single(lobby.Warps, w => w.TargetMap == "BattleArcadeCorridor");
        Assert.Equal((15, 2, 11, 11), (north.SourceX, north.SourceY, north.TargetX, north.TargetY));
        // Nobody walks round the attendants to the doorway: the way there is hers to lead
        Assert.True(lobby.IsWalkable(14, 6) && lobby.IsWalkable(14, 4) && lobby.IsWalkable(15, 3));
        Assert.False(lobby.IsWalkable(13, 4) || lobby.IsWalkable(16, 4) || lobby.IsWalkable(13, 5) || lobby.IsWalkable(16, 5));
        // The lounge is come to round the inner walls, as the original's ramps go
        Assert.True(lobby.IsWalkable(15, 10) && lobby.IsWalkable(5, 12) && lobby.IsWalkable(5, 13) && lobby.IsWalkable(20, 15));
        Assert.False(lobby.IsWalkable(10, 10) || lobby.IsWalkable(10, 13));
        // The original's thirteen people less the reporter, and the PC
        Assert.Equal(13, lobby.Everyone.Count());
        Assert.All(lobby.Everyone.Where(n => !n.IsPCTerminal), n => Assert.NotNull(ScriptLibrary.Default.Find(n.Script!, "BattleArcade")));

        // The corridor and the game room, laid out from the scene's own places
        var corridor = Room("BattleArcadeCorridor");
        var attendant = Assert.Single(corridor.Everyone);
        Assert.Equal(("attendant", 11, 9, Direction.Up), (attendant.Key, attendant.GridX, attendant.GridY, attendant.Facing));
        Assert.Equal(new[] { "BattleArcade", "BattleArcadeBattleRoom" }, corridor.Warps.Select(w => w.TargetMap).OrderBy(n => n));
        Assert.True(corridor.IsWalkable(11, 11) && corridor.IsWalkable(11, 8) && corridor.IsWalkable(4, 8) && corridor.IsWalkable(4, 3));
        var room = Room("BattleArcadeBattleRoom");
        Assert.Equal((17, 18, BattleArena.Gym, InteriorStyle.GameHall), (room.Width, room.Height, room.Arena, room.Interior));
        Assert.Equal("FLAG_HIDE_BATTLE_ARCADE_ROOM_OPPONENT", room.Everyone.Single(n => n.Key == "opponent").HiddenBy);
        Assert.Equal((8, 4, Direction.Down), room.Everyone.Where(n => n.Key == "host").Select(n => (n.GridX, n.GridY, n.Facing)).Single());
        // The crowd of thirteen at the foot of the room, clear of the ways the challenger, the trainer and the attendant walk
        var crowd = room.Everyone.Where(n => n.Key!.StartsWith("spectator_")).ToList();
        Assert.Equal(13, crowd.Count);
        Assert.All(crowd, n => Assert.True(n.GridY >= 12 && n.GridX != 8 && n.GridX != 3 && !(n.GridX == 14 && n.GridY < 15)));
        // The challenger's and the trainer's places, either end of the floor, and the great screen behind the host
        Assert.True(room.IsWalkable(4, 6) && room.IsWalkable(12, 6) && room.IsWalkable(5, 6) && room.IsWalkable(11, 6));
        Assert.Equal(4, room.Props.Count(p => p.Type == PropType.Television));
        var door = Assert.Single(room.Warps);
        Assert.Equal(("BattleArcade", 14, 6, Direction.Up), (door.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        Assert.All(RoomPlanTests.BattleArcade, name => Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id));
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
            if (next == "BattleArcade") return;
        }
        throw new InvalidOperationException("The challenge never came back to the lobby");
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
        if (host.Map?.Name != "BattleArcade") host.Map = Room("BattleArcade");
        host.PlayerTile = (14, 6);
        Play(host, runner, "BattleArcade", "SingleAttendant", "single_attendant");
    }

    private static void Running(HeadlessScriptHost host, FrontierChallenge way, int streak)
    {
        host.Frontier.SetRunning(FrontierFacility.Arcade, (int)way, true);
        host.Frontier.Set(BattleArcadeChallenge.LatestStreakStat(way), streak);
    }

    [Fact]
    public void ASingleChallengeIsPlayedFromTheCounterToTheBattlePoints()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var boards = new List<ArcadeBoard>();
        host.AtArcadeBoard = boards.Add;
        Take(host, runner);

        // The counter: the three entered, the challenge begun, the game saved, the round drawn, the way round to the doorway
        var arcade = host.Arcade!;
        Assert.Equal((Single, 0, 14, 3), (arcade.Way, arcade.Battle, arcade.Trainers.Count, arcade.Speed));
        Assert.Equal(All, arcade.Slots);
        Assert.NotNull(arcade.Foe);
        Assert.Contains("frontier enter 3", host.Log);
        Assert.Contains("frontier save 0", host.Log);
        Assert.Contains($"frontier journal {(int)FrontierFacility.Arcade}", host.Log);
        Assert.Equal((255, 0, 1), (host.Story.Var("VAR_BATTLE_ARCADE_LOBBY_LOAD_ACTION"), host.Story.Var(BattleArcadeChallenge.WayVariable), host.Frontier.ChallengesStarted));
        Assert.True(host.Story.Has("FLAG_HIDE_BATTLE_ARCADE_ROOM_OPPONENT"));
        Assert.Equal("BattleArcadeCorridor", LastWarp(host));
        Assert.Equal((11, 11), host.PlayerTile);

        PlayOn(host, runner);
        // Seven games in the game room: a board before each, and then three against three with copies of the challenge's own
        Assert.Equal((7, 7, 7), (boards.Count, host.FrontierBattles.Count, host.Log.Count(l => l == "frontier arcadeboard 0")));
        Assert.All(boards, b => Assert.True(b.IsDone && b.Round == 0 && b.Speed is >= 0 and <= ArcadeBoard.FastestSpeed));
        Assert.Equal(Enumerable.Range(0, 7), boards.Select(b => b.Streak));
        Assert.All(host.FrontierBattles, b => Assert.Equal((3, 3, false), (b.Team.Count, b.Foes.Single().Party.Count, b.Doubles)));
        Assert.All(host.FrontierBattles, b => Assert.All(b.Team.Members, p => Assert.DoesNotContain(p, arcade.Team.Members)));
        Assert.All(host.FrontierBattles, b => Assert.Equal(AiFlags.None, b.Foes[0].Ai));
        Assert.Equal(arcade.Trainers.Take(7).Select(t => "frontier_" + FrontierDatabase.Trainer(t).Id), host.FrontierBattles.Select(b => b.Foes[0].Id));
        // Only the first round's events came up, each was announced, and none of them was the battle's weather
        Assert.All(boards, b => Assert.Contains(b.Effect, ArcadeBoard.AvailableAt(0, b.Streak)));
        Assert.Equal(7, host.Transcript.Count(l => l.Text.StartsWith("And the game board says...")));
        Assert.All(boards, b => Assert.True(Said(host, $"And the game board says... {ArcadeBoard.NameOf(b.Effect)}!")));
        Assert.All(host.FrontierFields, f => Assert.Equal(new FrontierConditions(), f));
        Assert.True(Said(host, "Welcome to the Battle Arcade, where anything can happen"));
        Assert.True(Said(host, "in safe keeping until you leave"));
        Assert.True(Said(host, "Game no. 7!"));
        Assert.Equal(6, host.Transcript.Count(l => l.Text.Contains("Let me see to your Pokémon")));
        // The records, the score, the Battle Points and the lobby
        Assert.Equal((7, 7, true), (host.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Single)), host.Frontier.Get(BattleArcadeChallenge.RecordStreakStat(Single)), host.Frontier.IsRunning(FrontierFacility.Arcade, (int)Single)));
        Assert.Equal((2, 7, BattleArcadeChallenge.RoundScore), (host.Frontier.BattlePoints, host.Frontier.VictoriesAt(FrontierFacility.Arcade), host.FrontierScore));
        Assert.True(Said(host, "received 2 BP"));
        Assert.True(Said(host, "Seven games, seven wins"));
        Assert.Null(host.Arcade);
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_ARCADE_LOBBY_LOAD_ACTION"));
        Assert.Equal(2, host.Log.Count(l => l == "frontier save 0"));
        Assert.Equal((14, 6), host.PlayerTile);
        // The player's own are as they were, items and all
        Assert.Equal((60, "Leftovers"), (host.Party.Members[0].Level, host.Party.Members[0].HeldItem?.Name));

        // The next round takes the streak up
        host.FrontierBattles.Clear();
        Take(host, runner);
        Assert.Equal((7, 1, 3), (host.Arcade!.Streak, host.Arcade.Round, host.Arcade.Speed));
    }

    /// <summary>One game played by the room's scripts with the board showing one event, and the challenge given up after it.</summary>
    private static (HeadlessScriptHost Host, BattleArcadeChallenge Arcade) OneGame(ArcadeEffect effect, int streak = 0, Action<BattleArcadeChallenge>? before = null)
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        if (streak > 0) Running(host, Single, streak);
        host.Decide = (asked, _) => asked.StartsWith("Game no.") ? 2 : 0;
        host.AtArcadeBoard = board =>
        {
            before?.Invoke(host.Arcade!);
            board.Rig(effect);
        };
        host.BattleTurns = 2;
        Take(host, runner);
        var arcade = host.Arcade!;
        PlayOn(host, runner);
        Assert.Equal(effect, arcade.Effect);
        Assert.True(Said(host, $"And the game board says... {ArcadeBoard.NameOf(effect)}!"));
        Assert.True(Said(host, "Thank you for playing"));
        return (host, arcade);
    }

    [Fact]
    public void EveryEventIsAnnouncedAndCarriedOutInTheGameRoom()
    {
        string Trainer(BattleArcadeChallenge arcade) => $"{arcade.Foe!.Record.Class} {arcade.Foe.Record.Name}";
        // What falls on a side: the line names whose Pokémon, and the battle's copies have it
        foreach (var effect in new[] { ArcadeEffect.LowerFoeHp, ArcadeEffect.LowerAllyHp })
        {
            var (host, arcade) = OneGame(effect);
            bool foe = effect == ArcadeEffect.LowerFoeHp;
            Assert.True(Said(host, $"{(foe ? Trainer(arcade) : host.PlayerName)}'s Pokémon short of their full HP"));
            var battle = Assert.Single(host.FrontierBattles);
            Assert.All(foe ? battle.Foes[0].Party.Members : battle.Team.Members, p => Assert.Equal(BattleArcadeChallenge.LoweredHp(p.MaxHP), p.CurrentHP));
            Assert.All(foe ? battle.Team.Members : battle.Foes[0].Party.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
            // One win, and then the challenge given up: the streak is one, the record one, and it runs no more
            Assert.Equal((1, 1, false, 1, 0), (host.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Single)), host.Frontier.Get(BattleArcadeChallenge.RecordStreakStat(Single)),
                host.Frontier.IsRunning(FrontierFacility.Arcade, (int)Single), host.Frontier.VictoriesAt(FrontierFacility.Arcade), host.Frontier.BattlePoints));
        }
        foreach (var (effect, status, word) in new[]
        {
            (ArcadeEffect.PoisonAlly, StatusCondition.Poison, "poisoned"), (ArcadeEffect.BurnAlly, StatusCondition.Burn, "burned"),
            (ArcadeEffect.SleepAlly, StatusCondition.Sleep, "fast asleep"), (ArcadeEffect.FreezeAlly, StatusCondition.Freeze, "frozen solid")
        })
        {
            var (host, _) = OneGame(effect);
            Assert.True(Said(host, word));
            Assert.Contains(Assert.Single(host.FrontierBattles).Team.Members, p => p.Status == status);
            Assert.All(host.FrontierBattles[0].Foes[0].Party.Members, p => Assert.Equal(StatusCondition.None, p.Status));
        }
        // Paralysis: the challenger's Garchomp, a Ground type, is kept from it, and the other two are not
        var paralyzed = OneGame(ArcadeEffect.ParalyzeAlly).Host;
        Assert.True(Said(paralyzed, $"{paralyzed.PlayerName}'s Pokémon paralyzed"));
        Assert.Equal(new[] { StatusCondition.None, StatusCondition.Paralyze, StatusCondition.Paralyze }, paralyzed.FrontierBattles[0].Team.Members.Select(p => p.Status));
        // With all three of the trainer's kept from it, the host says so
        var (safe, _) = OneGame(ArcadeEffect.ParalyzeFoe, before: arcade => { for (int i = 0; i < 3; i++) arcade.Foe!.Pokemon[i] = Mon("Golem", 50); });
        Assert.True(Said(safe, "What a stroke of luck! Not one of them can be paralyzed"));
        Assert.All(safe.FrontierBattles[0].Foes[0].Party.Members, p => Assert.Equal(StatusCondition.None, p.Status));
        foreach (var (effect, said) in new[] { (ArcadeEffect.PoisonFoe, "can be poisoned"), (ArcadeEffect.BurnFoe, "can be burned"), (ArcadeEffect.SleepFoe, "can be put to sleep"), (ArcadeEffect.FreezeFoe, "can be frozen") })
        {
            string[] kept = effect switch { ArcadeEffect.PoisonFoe => new[] { "Skarmory", null! }, ArcadeEffect.BurnFoe => new[] { "Infernape", null! },
                ArcadeEffect.SleepFoe => new[] { "Honchkrow", "Insomnia" }, _ => new[] { "Weavile", null! } };
            var (host, _) = OneGame(effect, before: arcade => { for (int i = 0; i < 3; i++) arcade.Foe!.Pokemon[i] = Mon(kept[0], 50, null, kept[1]); });
            Assert.True(Said(host, said));
        }

        // Something to hold, and three levels
        foreach (var effect in new[] { ArcadeEffect.FoeGetBerry, ArcadeEffect.AllyGetBerry, ArcadeEffect.FoeGetItem, ArcadeEffect.AllyGetItem })
        {
            var (host, arcade) = OneGame(effect);
            bool foe = ArcadeBoard.CategoryOf(effect) == ArcadeCategory.Foe;
            Assert.True(Said(host, $"{(foe ? Trainer(arcade) : host.PlayerName)}'s Pokémon are each lent a {arcade.Given} to hold"));
            var battle = host.FrontierBattles[0];
            Assert.All(foe ? battle.Foes[0].Party.Members : battle.Team.Members, p => Assert.Equal(arcade.Given, p.HeldItem?.Name));
            Assert.All(foe ? battle.Team.Members : battle.Foes[0].Party.Members, p => Assert.Null(p.HeldItem));
        }
        var higher = OneGame(ArcadeEffect.AllyLevelUp);
        Assert.True(Said(higher.Host, "three levels higher"));
        Assert.Equal(new[] { 53, 53, 48 }, higher.Host.FrontierBattles[0].Team.Members.Select(p => p.Level));
        Assert.Equal(new[] { 50, 50, 45 }, higher.Arcade.Team.Members.Select(p => p.Level));
        Assert.All(OneGame(ArcadeEffect.FoeLevelUp).Host.FrontierBattles[0].Foes[0].Party.Members, p => Assert.Equal(53, p.Level));

        // The battle's own: what it is fought under
        foreach (var (effect, conditions, said) in new[]
        {
            (ArcadeEffect.Sunny, new FrontierConditions(BattleWeather.Sun), "blazing sun"), (ArcadeEffect.Rainy, new FrontierConditions(BattleWeather.Rain), "steady downpour"),
            (ArcadeEffect.Sandy, new FrontierConditions(BattleWeather.Sandstorm), "raging sandstorm"), (ArcadeEffect.Hail, new FrontierConditions(BattleWeather.Hail), "pelting of hail"),
            (ArcadeEffect.Foggy, new FrontierConditions(BattleWeather.Fog), "fog"), (ArcadeEffect.TrickRoom, new FrontierConditions(TrickRoom: true), "The slow go first")
        })
        {
            var (host, _) = OneGame(effect);
            Assert.True(Said(host, said));
            Assert.Equal(conditions, Assert.Single(host.FrontierFields));
        }

        // The board's own: the next board's cursor
        Assert.Equal(4, OneGame(ArcadeEffect.SpeedUp).Arcade.Speed);
        Assert.Equal(2, OneGame(ArcadeEffect.SlowDown).Arcade.Speed);
        Assert.True(OneGame(ArcadeEffect.RandomizeCursor).Arcade.Randomized);

        // The bonuses. A swap: the challenger battles with the trainer's three
        var (swapped, swap) = OneGame(ArcadeEffect.SwapMons);
        Assert.True(Said(swapped, "change sides"));
        Assert.Equal(swap.Foe!.Pokemon.Select(p => p.Species.Name), swapped.FrontierBattles[0].Team.Members.Select(p => p.Species.Name));
        Assert.Equal(new[] { "Garchomp", "Luxray", "Staraptor" }, swapped.FrontierBattles[0].Foes[0].Party.Members.Select(p => p.Species.Name));
        // Battle Points on the spot, and the battle all the same
        foreach (var (effect, points) in new[] { (ArcadeEffect.Get1Bp, 1), (ArcadeEffect.Get3Bp, 3) })
        {
            var (host, _) = OneGame(effect);
            Assert.True(Said(host, $"received {points} BP"));
            Assert.Equal((points, points, 1), (host.Frontier.BattlePoints, host.Frontier.Arcade.BoardPoints, host.FrontierBattles.Count));
        }
        // A battle passed: none is fought, it counts for the streak and not for the battles won, and nobody restores what wasn't hurt
        var (passed, _) = OneGame(ArcadeEffect.NoBattle, streak: 28);
        Assert.True(Said(passed, "This battle is passed"));
        Assert.Empty(passed.FrontierBattles);
        Assert.Equal((29, 0), (passed.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Single)), passed.Frontier.VictoriesAt(FrontierFacility.Arcade)));
        Assert.False(Said(passed, "Let me see to your Pokémon"));
        Assert.False(Said(passed, "let the battle begin"));
        // Nothing at all
        var (nothing, _) = OneGame(ArcadeEffect.NoEvent);
        Assert.True(Said(nothing, "Nothing happens this time"));
        Assert.Equal(new FrontierConditions(), Assert.Single(nothing.FrontierFields));
    }

    [Fact]
    public void ABattleIsScoredAndItsGiftsKeptThroughTheScripts()
    {
        // Two games: the board lends a berry in the first, whose battle uses one of the three up and takes six turns; the second board is laid out by that
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        int games = 0, menus = 0;
        var boards = new List<ArcadeBoard>();
        host.Decide = (asked, _) => asked.StartsWith("Game no.") ? (++menus == 2 ? 2 : 0) : 0;
        host.AtArcadeBoard = board =>
        {
            boards.Add(board);
            board.Rig(++games == 1 ? ArcadeEffect.AllyGetBerry : ArcadeEffect.NoEvent);
        };
        host.Fight = _ =>
        {
            var team = host.FrontierBattles[^1].Team.Members;
            if (host.FrontierBattles.Count == 1)
            {
                team[0].HeldItem = null;
                team[1].CurrentHP = 0;
                host.BattleTurns = 6;
            }
            return BattleOutcome.Won;
        };
        Take(host, runner);
        var arcade = host.Arcade!;
        PlayOn(host, runner);
        Assert.Equal((2, 2), (boards.Count, host.FrontierBattles.Count));
        // 8 for no status, 4 for one fainted, 4 for under seven turns: sixteen, the second row of weights
        Assert.Equal((0, 4, 16, 1), (boards[0].Performance, boards[0].Tier, boards[1].Performance, boards[1].Tier));
        string berry = host.FrontierBattles[1].Team.Members[1].HeldItem!.Name;
        Assert.Contains(berry, FrontierDatabase.Arcade.Berries[0]);
        Assert.Equal(new[] { null, berry, berry }, host.FrontierBattles[1].Team.Members.Select(p => p.HeldItem?.Name));
        // Whole again for the second battle, whatever the first did
        Assert.All(host.FrontierBattles[1].Team.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
        Assert.Equal(new[] { null, berry, berry }, arcade.Team.Members.Select(p => p.HeldItem?.Name));
    }

    [Fact]
    public void ADoubleChallengeIsTwoAgainstTwoAndNeverMeetsTheArcadeStar()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        Running(host, Double, 14);
        host.AtArcadeBoard = board => board.Rig(ArcadeEffect.NoEvent);
        Take(host, runner, way: 1);
        Assert.Equal((Double, 14, 2, 1), (host.Arcade!.Way, host.Arcade.Streak, host.Arcade.Round, host.Story.Var(BattleArcadeChallenge.WayVariable)));
        PlayOn(host, runner);
        Assert.Equal(7, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.True(b.Doubles && b.Foes[0].TrainerClass != "Arcade Star" && b.Foes[0].Ai == AiFlags.Basic));
        Assert.False(Said(host, "Arcade Star"));
        Assert.Equal((21, true, 2, 0), (host.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Double)), host.Frontier.IsRunning(FrontierFacility.Arcade, (int)Double), host.Frontier.BattlePoints,
            host.Story.Var("VAR_BATTLE_ARCADE_PRINT_STATE")));
        Assert.Equal(0, host.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Single)));
    }

    [Fact]
    public void TheArcadeStarIsMetAtTheTwentyFirstBattleAndLeavesHerPrint()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        Running(host, Single, 14);
        var boards = new List<ArcadeBoard>();
        host.AtArcadeBoard = board =>
        {
            boards.Add(board);
            board.Rig(ArcadeEffect.NoEvent);
        };
        Take(host, runner);
        PlayOn(host, runner);
        // The seventh game of the third round: announced once, before the question; she comes in through the crowd; the board is played for her as for anyone
        Assert.Equal((7, 7), (host.FrontierBattles.Count, boards.Count));
        Assert.Equal(("frontier_arcade_star_dahlia_silver", "Arcade Star", FrontierTrainers.Mind), (host.FrontierBattles[6].Foes[0].Id, host.FrontierBattles[6].Foes[0].TrainerClass, host.FrontierBattles[6].Foes[0].Ai));
        Assert.All(host.FrontierBattles.Take(6), b => Assert.True(b.Foes[0].TrainerClass != "Arcade Star" && b.Foes[0].Ai == AiFlags.Basic));
        Assert.Equal(1, host.Transcript.Count(l => l.Text.Contains("she insists on playing the next game herself")));
        Assert.True(Said(host, "I'm Dahlia, the Arcade Star"));
        Assert.True(Said(host, "What a lovely win"));
        Assert.Equal(1, host.Frontier.BrainsMet);
        // Twenty Battle Points, the silver print handed over in the lobby, and the streak runs on at twenty-one
        Assert.True(Said(host, "received 20 BP"));
        Assert.True(Said(host, "A Silver Print was added"));
        Assert.Equal((20, 2, FrontierPrint.Silver, 21, true), (host.Frontier.BattlePoints, host.Story.Var("VAR_BATTLE_ARCADE_PRINT_STATE"), FrontierBrains.PrintOf(host.Story, FrontierFacility.Arcade),
            host.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Single)), host.Frontier.IsRunning(FrontierFacility.Arcade, (int)Single)));

        // The 49th: her better three, the gold print, twenty more; and the silver one is not handed over twice
        Running(host, Single, 42);
        host.FrontierBattles.Clear();
        host.Transcript.Clear();
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal("frontier_arcade_star_dahlia_gold", host.FrontierBattles[6].Foes[0].Id);
        Assert.True(Said(host, "hold nothing back"));
        Assert.True(Said(host, "A Gold Print was added"));
        Assert.False(Said(host, "A Silver Print was added"));
        Assert.Equal((40, 4, 2), (host.Frontier.BattlePoints, host.Story.Var("VAR_BATTLE_ARCADE_PRINT_STATE"), host.Frontier.BrainsMet));
    }

    [Fact]
    public void AChallengeIsPutDownTakenUpAgainAndGivenUp()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // The second board speeds the cursor up; the question after the third battle: REST, and yes; the lobby's question: not now
        int games = 0, menus = 0;
        host.AtArcadeBoard = board => board.Rig(++games == 2 ? ArcadeEffect.SpeedUp : games == 3 ? ArcadeEffect.AllyGetBerry : ArcadeEffect.NoEvent);
        host.Decide = (asked, _) => asked.StartsWith("Game no.") ? (++menus == 3 ? 1 : 0) : asked.Contains("go on with it now") ? 1 : 0;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.Null(host.Arcade);
        var paused = host.Frontier.Arcade.Paused!;
        Assert.Equal((3, 3, 4, false), (paused.Battle, host.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Single)), paused.Speed, paused.Randomized));
        Assert.Equal(new[] { "Garchomp", "Luxray", "Staraptor" }, paused.Species);
        string berry = paused.Items[0]!;
        Assert.Equal(new[] { berry, berry, berry }, paused.Items);
        Assert.Equal((2, 0, false), (host.Story.Var("VAR_BATTLE_ARCADE_LOBBY_LOAD_ACTION"), host.Frontier.Get(BattleArcadeChallenge.RecordStreakStat(Single)), host.Frontier.IsRunning(FrontierFacility.Arcade, (int)Single)));
        Assert.True(Said(host, "Speak to me whenever you are ready"));

        // Spoken to again, she takes it up: the host's question comes first; KEEP GOING for one game, then RETIRE
        menus = 0;
        host.Decide = (asked, _) => asked.StartsWith("Game no.") ? (++menus == 2 ? 2 : 0) : 0;
        host.AtArcadeBoard = board =>
        {
            Assert.Equal((4, 4, 3), (board.Speed, board.Tier, board.Streak));
            board.Rig(ArcadeEffect.NoEvent);
        };
        host.Transcript.Clear();
        host.PlayerTile = (14, 6);
        Play(host, runner, "BattleArcade", "SingleAttendant", "single_attendant");
        Assert.Equal((true, 3, 4), (host.Arcade!.Resumed, host.Arcade.Battle, host.Arcade.Speed));
        Assert.Equal(new[] { berry, berry, berry }, host.Arcade.Team.Members.Select(p => p.HeldItem?.Name));
        Assert.Equal("BattleArcadeCorridor", LastWarp(host));
        PlayOn(host, runner);
        Assert.Equal(4, host.FrontierBattles.Count);
        Assert.False(Said(host, "Welcome to the Battle Arcade, where anything can happen"));
        Assert.True(Said(host, "Game no. 4!"));
        Assert.True(Said(host, "Thank you for playing"));
        Assert.Equal((4, 4, false, 0), (host.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Single)), host.Frontier.Get(BattleArcadeChallenge.RecordStreakStat(Single)),
            host.Frontier.IsRunning(FrontierFacility.Arcade, (int)Single), host.Frontier.BattlePoints));
        Assert.Null(host.Arcade);
        Assert.Null(host.Frontier.Arcade.Paused);

        // Asked whether to give up and answering no, the question is asked again
        int asked2 = 0;
        menus = 0;
        host.AtArcadeBoard = board => board.Rig(ArcadeEffect.NoEvent);
        host.Decide = (asked, _) => asked.StartsWith("Game no.") ? (++menus <= 2 ? 2 : 1) : asked.Contains("Give the challenge up") ? (++asked2 == 1 ? 1 : 0) : asked.Contains("go on with it now") ? 1 : 0;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal((2, 2), (asked2, menus));
        Assert.Null(host.Frontier.Arcade.Paused);

        // A challenge put down whose three are no longer in their places can't be taken up
        menus = 0;
        host.Decide = (asked, _) => asked.StartsWith("Game no.") ? 1 : asked.Contains("go on with it now") ? 1 : 0;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.NotNull(host.Frontier.Arcade.Paused);
        host.Party.Members[1] = Mon("Bidoof", 30);
        host.Decide = null;
        host.Transcript.Clear();
        host.PlayerTile = (14, 6);
        Play(host, runner, "BattleArcade", "SingleAttendant", "single_attendant");
        Assert.True(Said(host, "can't be taken up again"));
        Assert.Null(host.Arcade);
        Assert.Null(host.Frontier.Arcade.Paused);
        Assert.Equal((0, 0), (host.Story.Var("VAR_BATTLE_ARCADE_LOBBY_LOAD_ACTION"), host.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Single))));
    }

    [Fact]
    public void ABattleLostEndsTheChallengeAndAGameClosedInTheMiddleEndsTheStreak()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.AtArcadeBoard = board => board.Rig(ArcadeEffect.NoEvent);
        int fought = 0;
        host.Fight = _ => ++fought == 3 ? BattleOutcome.Lost : BattleOutcome.Won;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.Equal((2, 2, false, 2, 0), (host.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Single)), host.Frontier.Get(BattleArcadeChallenge.RecordStreakStat(Single)),
            host.Frontier.IsRunning(FrontierFacility.Arcade, (int)Single), host.Frontier.VictoriesAt(FrontierFacility.Arcade), host.Frontier.BattlePoints));
        Assert.True(Said(host, "Thank you for playing."));
        Assert.Null(host.Arcade);
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_ARCADE_LOBBY_LOAD_ACTION"));

        // A challenge under way when the game was closed: the lobby finds its mark, and the streak is over
        host.Fight = null;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.True(host.Frontier.IsRunning(FrontierFacility.Arcade, (int)Single));
        Take(host, runner);
        Assert.Equal(255, host.Story.Var("VAR_BATTLE_ARCADE_LOBBY_LOAD_ACTION"));
        host.Arcade = null;
        host.Transcript.Clear();
        Play(host, runner, "BattleArcade", "OnEnter");
        Assert.True(Said(host, "left before it was over"));
        Assert.Equal((false, 0, 0), (host.Frontier.IsRunning(FrontierFacility.Arcade, (int)Single), host.Frontier.Get(BattleArcadeChallenge.LatestStreakStat(Single)), host.Story.Var("VAR_BATTLE_ARCADE_LOBBY_LOAD_ACTION")));
        Assert.Equal(7, host.Frontier.Get(BattleArcadeChallenge.RecordStreakStat(Single)));
    }

    [Fact]
    public void TheLobbysPeopleAndTheCommandsErrors()
    {
        var host = HostOfThree();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var lobby = Room("BattleArcade");
        foreach (var person in lobby.Everyone.Where(n => n.Script != null && n.Key?.EndsWith("attendant") != true))
        {
            host.Transcript.Clear();
            Play(host, runner, "BattleArcade", person.Script!, person.Key);
            Assert.NotEmpty(host.Transcript);
        }
        Play(host, runner, "BattleArcade", "MultiAttendant", "multi_attendant");
        Assert.True(Said(host, "nobody can be linked up with"));
        Play(host, runner, "BattleArcade", "RecordsMachine");
        Assert.Contains($"frontier records {(int)FrontierFacility.Arcade}", host.Log);
        // INFO ends the talk
        host.Answers.Enqueue(2);
        host.PlayerTile = (14, 6);
        Play(host, runner, "BattleArcade", "SingleAttendant", "single_attendant");
        Assert.True(Said(host, "whatever its cursor stops on happens"));
        Assert.Null(host.Arcade);
        // Too few who may go in, and two of a kind
        var two = new HeadlessScriptHost();
        two.Party.Add(Mon("Garchomp", 60));
        two.Party.Add(Mon("Luxray", 50));
        two.Answers.Enqueue(0);
        two.PlayerTile = (14, 6);
        Play(two, new ScriptRunner(ScriptLibrary.Default, two), "BattleArcade", "SingleAttendant", "single_attendant");
        Assert.True(Said(two, "You need three Pokémon, each of a different kind"));
        Assert.Null(two.Arcade);
        // The save refused: nothing is begun
        var wary = HostOfThree();
        wary.Decide = (asked, _) => asked.Contains("Shall I save it") ? 1 : 0;
        wary.PlayerTile = (14, 6);
        Play(wary, new ScriptRunner(ScriptLibrary.Default, wary), "BattleArcade", "SingleAttendant", "single_attendant");
        Assert.True(Said(wary, "come and see me when you are ready"));
        Assert.Null(wary.Arcade);
        Assert.Equal((0, 0), (wary.Story.Var("VAR_BATTLE_ARCADE_LOBBY_LOAD_ACTION"), wary.Frontier.ChallengesStarted));

        // The grounds' porch walks whoever steps under it in
        var porch = ScriptLibrary.Default.Find("EnterBattleArcade", "battle_frontier")!;
        var outside = new HeadlessScriptHost();
        var walker = new ScriptRunner(ScriptLibrary.Default, outside);
        walker.Start(porch, null);
        walker.RunToEnd();
        Assert.Contains("warp BattleArcade 1 6", outside.Log);

        // What a command needs is said
        var library = ScriptLibrary.FromSources(("test", "script Battle\n  arcade battle\n  end\nscript Board\n  arcade board\n  end\nscript Begin\n  arcade begin single\n  end\nscript Quiet\n  arcade active\n  arcade round\n  arcade beaten\n  arcade way\n  arcade paused\n  end\n"));
        runner = new ScriptRunner(library, host);
        runner.Start(library.Find("Quiet", "test")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        runner.Start(library.Find("Battle", "test")!, null);
        Assert.Contains("needs a challenge under way", Assert.Throws<ScriptException>(() => runner.RunToEnd()).Message);
        runner = new ScriptRunner(library, host);
        runner.Start(library.Find("Board", "test")!, null);
        Assert.Contains("needs a challenge under way", Assert.Throws<ScriptException>(() => runner.RunToEnd()).Message);
        runner = new ScriptRunner(library, host);
        runner.Start(library.Find("Begin", "test")!, null);
        Assert.Contains("arcade choose", Assert.Throws<ScriptException>(() => runner.RunToEnd()).Message);
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script X\n  arcade begin triple\n  end\n"));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script X\n  arcade spin\n  end\n"));
    }

    // ------------------------------------------------------------------ the Frontier, all five built

    [Fact]
    public void WithTheArcadeEveryFacilityHasItsBoardItsPrintAndItsPlaceInTheFrame()
    {
        var records = new FrontierRecords();
        var story = new StoryState();
        var five = new[] { FrontierFacility.Tower, FrontierFacility.Factory, FrontierFacility.Hall, FrontierFacility.Castle, FrontierFacility.Arcade };
        Assert.Equal(five, FrontierBrains.Printed);
        // Every facility's board of records has rows of its own, the Arcade's a streak for each way it is taken
        Assert.All(five, facility => Assert.NotEmpty(FrontierBoard.For(facility, records, story).Rows));
        records.Set(BattleArcadeChallenge.RecordStreakStat(Single), 21);
        records.Set(BattleArcadeChallenge.LatestStreakStat(Single), 14);
        records.SetRunning(FrontierFacility.Arcade, (int)Single, true);
        var board = FrontierBoard.For(FrontierFacility.Arcade, records, story);
        Assert.Equal(("Battle Arcade", null, 2), (board.Title, board.CountLabel, board.Rows.Count));
        Assert.Equal(new FrontierBoardRow("Single Battle", 21, 14, true), board.Rows[0]);
        Assert.Equal(new FrontierBoardRow("Double Battle", 0, 0, false), board.Rows[1]);

        // The Villa's Pokémon Bust asks a battle won at all five (FrontierEverywhere): the Arcade's is the last it was waiting for
        foreach (var facility in five.Take(4)) records.CountVictories(facility, 1);
        Assert.False(records.WonEverywhere);
        var challenge = Begun(records);
        challenge.Skipped();
        Assert.False(records.WonEverywhere);
        challenge.Won(records);
        Assert.True(records.WonEverywhere);
        Assert.Contains(VillaRules.Catalogue(story, new VillaRecords(records.WonEverywhere, FrontierBrains.AnyPrint(story), 0, 0, 0, 0, 0)), p => p.Name == "Pokémon Bust");
        // Its Silver Pokémon Bust asks a print anywhere (FrontierPrint): the Arcade's is one
        Assert.False(FrontierBrains.AnyPrint(story));
        challenge.BeatStar(story, FrontierPrint.Silver);
        Assert.Equal((1, FrontierPrint.None, false, FrontierPrint.Silver), (story.Var("VAR_BATTLE_ARCADE_PRINT_STATE"), FrontierBrains.PrintOf(story, FrontierFacility.Arcade), FrontierBrains.AnyPrint(story), challenge.BeatDahlia));
        Assert.Equal(FrontierPrint.Silver, FrontierBrains.HandOver(story, FrontierFacility.Arcade, out bool all));
        Assert.True(FrontierBrains.AnyPrint(story) && !all);
        Assert.Contains(VillaRules.Catalogue(story, new VillaRecords(records.WonEverywhere, FrontierBrains.AnyPrint(story), 0, 0, 0, 0, 0)), p => p.Name == "Silver Pokémon Bust");
        Assert.Equal(FrontierPrint.Silver, FrontierBoard.For(FrontierFacility.Arcade, records, story).Print);

        // All five gold prints set the flag once, whichever comes last; here the Arcade's does, at its lobby's counter
        foreach (var facility in five.Take(4)) story.SetVar(FrontierBrains.PrintVariable(facility), 4);
        challenge.BeatStar(story, FrontierPrint.Gold);
        Assert.Equal(3, story.Var("VAR_BATTLE_ARCADE_PRINT_STATE"));
        Assert.False(story.Has(FrontierBrains.AllGoldFlag));
        var host = new HeadlessScriptHost(story);
        var library = ScriptLibrary.FromSources(("test", "script Print\n  frontier print arcade\n  end\n"));
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find("Print", "test")!, null);
        runner.RunToEnd();
        Assert.Equal((2, 4, true, FrontierBrains.AllGoldScore), (runner.Result, story.Var("VAR_BATTLE_ARCADE_PRINT_STATE"), story.Has(FrontierBrains.AllGoldFlag), host.FrontierScore));
        // Asked again, nothing is handed over and nothing is scored twice
        runner.Start(library.Find("Print", "test")!, null);
        runner.RunToEnd();
        Assert.Equal((0, FrontierBrains.AllGoldScore), (runner.Result, host.FrontierScore));
        Assert.All(five, facility => Assert.Equal(FrontierPrint.Gold, FrontierBoard.For(facility, records, story).Print));

        // The Trainer Card's stars ask nothing of four of the five: only a hundred in a row at the Battle Tower is worth one
        // (TrainerCase_CalculateTrainerCardLevel), however long the other halls' streaks
        records.Set(BattleArcadeChallenge.RecordStreakStat(Single), 9999);
        Assert.Equal(0, TrainerCardRules.Level(false, 0, records.Get(BattleTowerChallenge.RecordStat(TowerMode.Single)), false, 0));
        Assert.Equal(1, TrainerCardRules.Level(false, 0, 100, false, 0));

        // A save keeps the Arcade's part with the rest
        records.Arcade.BoardPoints = 7;
        var saved = GameDataFiles.Deserialize<FrontierRecords>(GameDataFiles.Serialize(records));
        Assert.Equal((7, 9999, true), (saved.Arcade.BoardPoints, saved.Get(BattleArcadeChallenge.RecordStreakStat(Single)), saved.WonEverywhere));
    }

    [Fact]
    public void ABattleOpensUnderWhatTheBoardSet()
    {
        // The frame's seam: a Frontier battle handed the conditions opens under them (BattleSetup.Conditions, as the
        // field's own weather does), and one handed none opens under a clear sky
        PokemonPlatinumEngine.Battle.BattleEngine Battle(FrontierConditions? conditions)
        {
            var foe = new Trainer { Id = "frontier_test", Name = "Test", TrainerClass = "Youngster", PrizeMoney = 0, Ai = AiFlags.None };
            foe.Party.Add(Mon("Bidoof", 50));
            return new PokemonPlatinumEngine.Battle.BattleEngine(new PokemonPlatinumEngine.Battle.BattleSetup
            {
                PlayerParty = PartyOf(Mon("Garchomp", 50)),
                Inventory = new Inventory(),
                Pokedex = new Pokedex(),
                Trainers = new List<Trainer> { foe },
                Frontier = true,
                Random = new BattleRandom(1),
                Conditions = new BattleConditions { Weather = conditions?.Weather ?? BattleWeather.None, TrickRoom = conditions?.TrickRoom ?? false }
            });
        }
        var sunny = Battle(new FrontierConditions(BattleWeather.Sun));
        Assert.Equal((BattleWeather.Sun, true, false), (sunny.Core.Field.Weather, sunny.Core.Field.WeatherLasts, sunny.Core.Field.TrickRoom));
        var foggy = Battle(new FrontierConditions(BattleWeather.Fog));
        Assert.Equal(BattleWeather.Fog, foggy.Core.Field.Weather);
        var room = Battle(new FrontierConditions(TrickRoom: true));
        Assert.Equal((BattleWeather.None, 5), (room.Core.Field.Weather, room.Core.Field.TrickRoomTurns));
        var clear = Battle(null);
        Assert.Equal((BattleWeather.None, 0, true), (clear.Core.Field.Weather, clear.Core.Field.TrickRoomTurns, clear.IsFrontier));
    }
}
