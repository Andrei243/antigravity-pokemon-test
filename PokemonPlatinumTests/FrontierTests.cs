using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The Battle Frontier's frame and the Battle Tower (plan 06 · R18a). The numbers are the decompilation's
/// (<c>src/unk_02049D08.c</c>, <c>src/unk_0204AEE8.c</c>, <c>src/unk_0206B9D8.c</c>, <c>src/overlay104/ov104_0223A0C4.c</c>,
/// <c>frontier_opponents.c</c>, <c>src/battle_frontier_save.c</c>, <c>scripts_battle_tower.s</c>,
/// <c>frontier_scripts_battle_tower_battle_room.s</c> and the Frontier branches of <c>src/battle</c>).
/// </summary>
[Collection("MapDatabase")]
public class FrontierTests
{
    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static Pokemon A(string species, int level = 50, string? item = null)
    {
        var p = Mon(species, level, "Tackle");
        if (item != null) p.HeldItem = ItemDatabase.Get(item) ?? throw new ArgumentException(item);
        return p;
    }

    private static Party Team(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    private static bool Said(HeadlessScriptHost host, string part) => host.Transcript.Any(l => l.Text.Contains(part));

    /// <summary>A place's script played to its end on that place's own map, for whoever it belongs to.</summary>
    private static void Play(HeadlessScriptHost host, ScriptRunner runner, string place, string script, string? who = null)
    {
        if (host.Map?.Name != place) host.Map = Room(place);
        runner.Start(ScriptLibrary.Default.Find(script, place)!, who == null ? null : host.Map!.Everyone.First(n => n.Key == who));
        runner.RunToEnd();
    }

    // ------------------------------------------------------------------ the tables

    [Fact]
    public void TheFrontiersTrainersAndSetsAreTheOriginals()
    {
        // generated/frontier_trainers.txt has 315 names and frontier_pokemon.order 951, the first the empty set
        Assert.Equal(315, FrontierDatabase.Trainers.Count);
        Assert.Equal(951, FrontierDatabase.Sets.Count);
        Assert.Equal("", FrontierDatabase.Set(0).Species);
        Assert.Equal(("youngster_jim", "Jim", "Youngster", "youngster"), (FrontierDatabase.Trainer(0).Id, FrontierDatabase.Trainer(0).Name, FrontierDatabase.Trainer(0).Class, FrontierDatabase.Trainer(0).Look));
        // FRONTIER_TRAINER_TOWER_TYCOON_PALMER_SILVER and _GOLD, and the three sets each has (the empty fourth isn't counted)
        var (silver, gold) = FrontierBrains.Trainers(FrontierFacility.Tower);
        Assert.Equal((305, 306), (silver, gold));
        Assert.Equal(new[] { "Rhyperior", "Milotic", "Dragonite" }, FrontierDatabase.Trainer(silver).Sets.Select(s => FrontierDatabase.Set(s).Species));
        Assert.Equal(new[] { "Heatran", "Cresselia", "Regigigas" }, FrontierDatabase.Trainer(gold).Sets.Select(s => FrontierDatabase.Set(s).Species));
        Assert.Equal("Tower Tycoon", FrontierDatabase.Trainer(gold).Class);
        // The five partners follow the three hundred, and the other four Brains follow the Tower Tycoon
        Assert.Equal(new[] { 300, 301, 302, 303, 304 }, BattleTowerChallenge.PartnerTrainers.Select(FrontierDatabase.NumberOf));
        Assert.Equal((307, 308), FrontierBrains.Trainers(FrontierFacility.Hall));
        Assert.Equal((309, 310), FrontierBrains.Trainers(FrontierFacility.Factory));
        Assert.Equal((311, 312), FrontierBrains.Trainers(FrontierFacility.Arcade));
        Assert.Equal((313, 314), FrontierBrains.Trainers(FrontierFacility.Castle));

        // Everything a set names is something the game has
        foreach (var set in FrontierDatabase.Sets.Skip(1))
        {
            Assert.True(PokemonDatabase.Get(set.Species) != null, $"{set.Id}: no species {set.Species}");
            Assert.InRange(set.Moves.Count, 1, 4);
            Assert.All(set.Moves, m => Assert.True(MoveDatabase.Get(m).Name == m, $"{set.Id}: no move {m}"));
            if (set.Item != null) Assert.True(ItemDatabase.Get(set.Item) != null, $"{set.Id}: no item {set.Item}");
            Assert.InRange(set.EvFlags, 1, 63);
        }
        // bulbasaur_1: Modest, Bright Powder, effort in the two special stats (bits 4 and 5)
        var bulbasaur = FrontierDatabase.Set(1);
        Assert.Equal(("Bulbasaur", Nature.Modest, "Bright Powder", 48), (bulbasaur.Species, bulbasaur.Nature, bulbasaur.Item, bulbasaur.EvFlags));
        // Every trainer the Tower can draw has sets enough for a Double Battle's four
        Assert.All(FrontierDatabase.Trainers.Take(300), t => Assert.True(t.Sets.Count >= 4 && t.Sets.All(s => s > 0 && s < 951), t.Id));
    }

    [Fact]
    public void TheTowersRoomsTheExchangeAndTheBanlistAreTheOriginals()
    {
        // sBattleTowerTrainerRangesPerRoom and sBattleTowerBossTrainerRangesPerRoom, by the constants' places in the list
        Assert.Equal(new[] { (0, 99), (80, 119), (100, 139), (120, 159), (140, 179), (160, 199), (180, 219), (200, 299) },
            FrontierDatabase.TowerRooms.Select(r => (r[0], r[1])));
        Assert.Equal(new[] { (100, 119), (120, 139), (140, 159), (160, 179), (180, 199), (200, 219), (220, 239), (200, 299) },
            FrontierDatabase.TowerBossRooms.Select(r => (r[0], r[1])));

        // ScrCmd_GetExchangeServiceCornerItemAndCost: 26 things for 1 to 48 points, 15 machines for 32 to 80
        Assert.Equal(26, FrontierDatabase.ExchangeItems.Count);
        Assert.Equal(15, FrontierDatabase.ExchangeMachines.Count);
        Assert.Equal(new[] { 1, 1, 1, 1, 1, 1, 16, 16, 16, 16, 16, 16, 16, 16, 32, 32, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48 }, FrontierDatabase.ExchangeItems.Select(p => p.Price));
        Assert.Equal(new[] { 32, 32, 32, 32, 40, 40, 48, 48, 64, 64, 64, 80, 80, 80, 80 }, FrontierDatabase.ExchangeMachines.Select(p => p.Price));
        Assert.Equal(("Protein", "Rare Candy"), (FrontierDatabase.ExchangeItems[0].Item, FrontierDatabase.ExchangeItems[^1].Item));
        Assert.Equal(("TM06", "TM26"), (FrontierDatabase.ExchangeMachines[0].Item, FrontierDatabase.ExchangeMachines[^1].Item));
        Assert.All(FrontierDatabase.ExchangeItems.Concat(FrontierDatabase.ExchangeMachines), p => Assert.NotNull(ItemDatabase.Get(p.Item)));
        Assert.Equal(16, FrontierExchange.PriceOf("Toxic Orb"));
        Assert.Equal(0, FrontierExchange.PriceOf("Potion"));

        // sBattleFrontierBanlist
        Assert.Equal(new[] { "Mewtwo", "Mew", "Lugia", "Ho-Oh", "Celebi", "Kyogre", "Groudon", "Rayquaza", "Jirachi", "Deoxys", "Dialga", "Palkia", "Giratina",
            "Phione", "Manaphy", "Darkrai", "Shaymin", "Arceus" }, FrontierDatabase.Banned);
    }

    // ------------------------------------------------------------------ the records

    [Fact]
    public void TheStatsAreTheOriginalsHundredInItsOrder()
    {
        // enum BattleFrontierStatsIndex: the Tower's ten, the Factory's from 10, the Hall's from 34, the Castle's from 70, the Arcade's from 94
        Assert.Equal(100, FrontierRecords.StatCount);
        Assert.Equal(0, (int)FrontierStat.TowerRecordStreakSingle);
        Assert.Equal(5, (int)FrontierStat.TowerLatestStreakMulti);
        Assert.Equal(10, (int)FrontierStat.FactoryRecordStreak50Single);
        Assert.Equal(34, (int)FrontierStat.HallCurrentStreakSingle);
        Assert.Equal(70, (int)FrontierStat.CastleRecordStreakSingle);
        Assert.Equal(94, (int)FrontierStat.ArcadeRecordStreakSingle);
        Assert.Equal(99, (int)FrontierStat.ArcadeLatestStreakMulti);
        Assert.Equal((FrontierStat.TowerRecordStreakDouble, FrontierStat.TowerLatestStreakDouble),
            (BattleTowerChallenge.RecordStat(TowerMode.Double), BattleTowerChallenge.LatestStat(TowerMode.Double)));
        // enum BattleFacility
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, new[] { FrontierFacility.Tower, FrontierFacility.Factory, FrontierFacility.FactoryOpen, FrontierFacility.Castle, FrontierFacility.Hall, FrontierFacility.Arcade }.Select(f => (int)f));
    }

    [Fact]
    public void ARecordIsKeptOnlyWhenBetteredAndNothingPastNineThousandNineHundredAndNinetyNine()
    {
        var records = new FrontierRecords();
        // BattleFrontierSave_SetStat, _SetIfBetter, _AddToStat, _SubtractFromStat
        Assert.Equal(12, records.Set(FrontierStat.CastleLatestCpSingle, 12));
        Assert.Equal(12, records.SetIfBetter(FrontierStat.CastleLatestCpSingle, 9));
        Assert.Equal(40, records.SetIfBetter(FrontierStat.CastleLatestCpSingle, 40));
        Assert.Equal(45, records.Add(FrontierStat.CastleLatestCpSingle, 5));
        Assert.Equal(0, records.Subtract(FrontierStat.CastleLatestCpSingle, 60));
        Assert.Equal(9999, records.Set(FrontierStat.TowerRecordStreakSingle, 12000));
        Assert.Equal(9999, records.Add(FrontierStat.TowerRecordStreakSingle, 1));

        // A streak runs for one way of taking one facility's challenge, and for no other
        records.SetRunning(FrontierFacility.Factory, (int)FrontierChallenge.Double, true);
        Assert.True(records.IsRunning(FrontierFacility.Factory, (int)FrontierChallenge.Double));
        Assert.False(records.IsRunning(FrontierFacility.Factory, (int)FrontierChallenge.Single));
        Assert.False(records.IsRunning(FrontierFacility.FactoryOpen, (int)FrontierChallenge.Double));
        records.SetRunning(FrontierFacility.Factory, (int)FrontierChallenge.Double, false);
        Assert.False(records.IsRunning(FrontierFacility.Factory, (int)FrontierChallenge.Double));
    }

    [Fact]
    public void BattlePointsAreGivenUpToTheMostAndSpentAllOrNothing()
    {
        var records = new FrontierRecords();
        // WifiBattleTowerRecord_UpdateBattlePoints: BATTLE_POINTS_FUNC_ADD stops at 9999
        Assert.Equal(20, records.GivePoints(20));
        Assert.False(records.SpendPoints(21));
        Assert.Equal(20, records.BattlePoints);
        Assert.True(records.SpendPoints(16));
        Assert.Equal((4, 20, 16), (records.BattlePoints, records.PointsReceived, records.PointsSpent));
        Assert.Equal(9999, records.GivePoints(20000));
    }

    [Fact]
    public void TheRecordsComeBackFromASaveAsTheyWere()
    {
        var records = new FrontierRecords();
        records.Set(FrontierStat.TowerRecordStreakSingle, 35);
        records.SetRunning(FrontierFacility.Tower, (int)TowerMode.Single, true);
        records.GivePoints(11);
        records.CountVictories(FrontierFacility.Tower, 35);
        records.Tower.Rooms[0] = 5;
        records.Tower.RngState = 123456789;
        records.Tower.BronzeTrophy = true;
        records.Tower.Paused = new TowerPause { Mode = TowerMode.Multi, NextOpponent = 4, Slots = { 2, 0 }, Trainers = { 5, 6, 7 }, Seed = 99,
            Partner = new TowerPartner { Id = 2, OtId = 77, Sets = { 400, 401 }, Personalities = { 1, 2 } } };

        var back = GameDataFiles.Deserialize<FrontierRecords>(GameDataFiles.Serialize(records));
        Assert.Equal(35, back.Get(FrontierStat.TowerRecordStreakSingle));
        Assert.True(back.IsRunning(FrontierFacility.Tower, 0));
        Assert.Equal((11, 35, 5, 123456789u, true), (back.BattlePoints, back.VictoriesAt(FrontierFacility.Tower), back.Tower.Rooms[0], back.Tower.RngState, back.Tower.BronzeTrophy));
        Assert.Equal(100, back.Stats.Count);
        var paused = back.Tower.Paused!;
        Assert.Equal((TowerMode.Multi, 4, 99u, 2), (paused.Mode, paused.NextOpponent, paused.Seed, paused.Partner!.Id));
        Assert.Equal(new[] { 2, 0 }, paused.Slots);
        Assert.Equal(new[] { 400, 401 }, paused.Partner.Sets);
    }

    [Fact]
    public void ANewDayStartsTheTowersDrawsFromTheDaysNumber()
    {
        // sub_0206C008: the day's number times 1566083941, plus one (7 gives 2372652996 in 32 bits)
        var records = new FrontierRecords();
        records.NewDay(7);
        Assert.Equal(2372652996u, records.Tower.RngState);

        var story = new StoryState();
        var encounters = SpecialEncounters.NewGame(new Random(3));
        DailyEvents.DaysPass(1, story, encounters, null, new Random(5), null, records);
        Assert.Equal(unchecked(encounters.DailyNumber * 1566083941u + 1u), records.Tower.RngState);
    }

    // ------------------------------------------------------------------ who may enter

    [Fact]
    public void AnEggOrABannedSpeciesEntersNothing()
    {
        Assert.True(FrontierRules.MayEnter(A("Garchomp")));
        Assert.False(FrontierRules.MayEnter(A("Mewtwo")));
        Assert.False(FrontierRules.MayEnter(A("Phione")));
        var egg = A("Togepi", 1);
        egg.IsEgg = true;
        Assert.False(FrontierRules.MayEnter(egg));
        Assert.Equal(new[] { "Dialga", "Giratina" }, FrontierRules.BannedSeen(s => s is "Dialga" or "Giratina" or "Bidoof"));
    }

    [Fact]
    public void EnoughMayEnterOnlyWhenThatManyDifferInSpeciesAndItem()
    {
        // sub_02049EC4: three of three
        Assert.True(FrontierRules.EnoughToEnter(Team(A("Garchomp"), A("Lucario"), A("Togekiss")), 3));
        // Too few in the team, or too few once the banned and the Eggs are left out
        Assert.False(FrontierRules.EnoughToEnter(Team(A("Garchomp"), A("Lucario")), 3));
        Assert.False(FrontierRules.EnoughToEnter(Team(A("Garchomp"), A("Lucario"), A("Mewtwo")), 3));
        // Two of a species count once
        Assert.False(FrontierRules.EnoughToEnter(Team(A("Garchomp"), A("Garchomp"), A("Lucario")), 3));
        Assert.True(FrontierRules.EnoughToEnter(Team(A("Garchomp"), A("Garchomp"), A("Lucario"), A("Togekiss")), 3));
        // Three that hold the same item can't go in together, but any two of them can beside one that holds nothing
        var same = Team(A("Garchomp", item: "Leftovers"), A("Lucario", item: "Leftovers"), A("Togekiss", item: "Leftovers"));
        Assert.False(FrontierRules.EnoughToEnter(same, 3));
        Assert.False(FrontierRules.EnoughToEnter(same, 2));
        Assert.True(FrontierRules.EnoughToEnter(Team(A("Garchomp", item: "Leftovers"), A("Lucario", item: "Leftovers"), A("Togekiss"), A("Gengar", item: "Lum Berry")), 3));
        // Holding nothing is no item in common
        Assert.True(FrontierRules.EnoughToEnter(Team(A("Garchomp"), A("Lucario"), A("Togekiss"), A("Gengar")), 4));
    }

    [Fact]
    public void TheChosenAreRefusedForASpeciesBeforeAnItem()
    {
        // BattleTower_CheckDuplicateSpeciesAndHeldItems: 1 for a species, 2 for an item, each newcomer against those before it
        Assert.Equal(FrontierRefusal.None, FrontierRules.WhyNot(new[] { A("Garchomp", item: "Leftovers"), A("Lucario"), A("Togekiss") }));
        Assert.Equal(FrontierRefusal.SameSpecies, FrontierRules.WhyNot(new[] { A("Garchomp"), A("Lucario"), A("Garchomp") }));
        Assert.Equal(FrontierRefusal.SameItem, FrontierRules.WhyNot(new[] { A("Garchomp", item: "Leftovers"), A("Lucario", item: "Leftovers"), A("Togekiss") }));
        Assert.Equal(FrontierRefusal.SameSpecies, FrontierRules.WhyNot(new[] { A("Garchomp", item: "Leftovers"), A("Garchomp", item: "Leftovers") }));
        Assert.Equal(FrontierRefusal.None, FrontierRules.WhyNot(new[] { A("Garchomp"), A("Lucario") }));
    }

    [Fact]
    public void APokemonAboveTheLevelIsBroughtDownToItForTheBattleAndItselfUntouched()
    {
        // FieldBattleDTO_NewBattleTower: a copy; above level 50 its EXP is set to level 50's and its stats worked out again
        var own = A("Garchomp", 78);
        own.CurrentHP = 10;
        own.Status = StatusCondition.Burn;
        own.Moves[0].CurrentPP = 1;
        var copy = FrontierRules.ForBattle(own);
        Assert.Equal(50, copy.Level);
        Assert.Equal(Pokemon.GetExpForLevel(50, own.Species.GrowthRate), copy.CurrentExp);
        // Garchomp's base HP 108, no IVs or effort: 2 × 108 × 50 / 100 + 50 + 10 = 168
        Assert.Equal((168, 168, StatusCondition.None), (copy.MaxHP, copy.CurrentHP, copy.Status));
        Assert.Equal(copy.Moves[0].MaxPP, copy.Moves[0].CurrentPP);
        Assert.Equal((78, 10, StatusCondition.Burn, 1), (own.Level, own.CurrentHP, own.Status, own.Moves[0].CurrentPP));
        // One under the level keeps its own
        Assert.Equal(34, FrontierRules.ForBattle(A("Pikachu", 34)).Level);
        // The team is the chosen, in the order chosen
        var team = FrontierRules.TeamFor(Team(A("Garchomp", 60), A("Lucario", 40), A("Togekiss", 55)), new[] { 2, 0 });
        Assert.Equal(new[] { ("Togekiss", 50), ("Garchomp", 50) }, team.Members.Select(p => (p.Species.Name, p.Level)));
    }

    // ------------------------------------------------------------------ the trainers' Pokémon

    [Fact]
    public void ATrainersNumberSaysHowGoodItsPokemonAre()
    {
        // GetIVsForTrainer: 31 / 8 is 3, times one to seven by twenties from trainer 100, and 31 from 220
        Assert.Equal(new[] { 3, 3, 6, 6, 9, 12, 15, 18, 21, 21, 31, 31 },
            new[] { 0, 99, 100, 119, 120, 140, 160, 180, 200, 219, 220, 314 }.Select(FrontierTrainers.IvsFor));
    }

    [Fact]
    public void ASetIsBuiltWithItsEffortSharedEvenlyAndItsAbilityByItsPersonality()
    {
        // FrontierPokemon_Init: 510 between the stats named, 255 at most each
        var two = new FrontierSetRecord { Id = "t", Species = "Garchomp", Moves = { "Earthquake", "Dragon Claw" }, EvFlags = 0b001010, Nature = Nature.Jolly, Item = "Choice Scarf" };
        var p = FrontierTrainers.Build(two, personality: 0x0000_000D, ivs: 21);
        Assert.Equal((50, "Garchomp", "Choice Scarf"), (p.Level, p.Species.Name, p.HeldItem!.Name));
        Assert.Equal((0, 255, 0, 255, 0, 0), (p.EvHP, p.EvAttack, p.EvDefense, p.EvSpeed, p.EvSpAttack, p.EvSpDefense));
        Assert.Equal((21, 21, 21, 21, 21, 21), (p.IvHP, p.IvAttack, p.IvDefense, p.IvSpeed, p.IvSpAttack, p.IvSpDefense));
        // 13 % 25 is 13, Jolly; the personality is the Pokémon's own
        Assert.Equal((Nature.Jolly, 0x0000_000Du, false, 255), (p.Nature, p.Personality, p.IsShiny, p.Friendship));
        Assert.Equal(new[] { "Earthquake", "Dragon Claw" }, p.Moves.Select(m => m.Name));
        // Garchomp's Speed at level 50: (2 × 102 + 21 + 255 / 4) × 50 / 100 + 5 = 149, a tenth more for Jolly: 163
        Assert.Equal(163, p.Speed);

        var three = new FrontierSetRecord { Id = "t", Species = "Blissey", Moves = { "Frustration" }, EvFlags = 0b100101, Nature = Nature.Bold };
        var blissey = FrontierTrainers.Build(three, personality: 5, ivs: 3, item: "Lum Berry");
        Assert.Equal((170, 0, 170, 0, 0, 170), (blissey.EvHP, blissey.EvAttack, blissey.EvDefense, blissey.EvSpeed, blissey.EvSpAttack, blissey.EvSpDefense));
        // One that knows Frustration has no friendship, and a handed item stands in for the set's own
        Assert.Equal((0, "Lum Berry"), (blissey.Friendship, blissey.HeldItem!.Name));
        // Blissey's two abilities, by the personality's lowest bit: Natural Cure, Serene Grace
        Assert.Equal("Serene Grace", blissey.AbilityName);
        Assert.Equal("Natural Cure", FrontierTrainers.Build(three, personality: 30, ivs: 3).AbilityName);

        var six = new FrontierSetRecord { Id = "t", Species = "Mew", Moves = { "Psychic" }, EvFlags = 0b111111, Nature = Nature.Hardy };
        Assert.Equal(85, FrontierTrainers.Build(six, 0, 31).EvSpDefense);
    }

    [Fact]
    public void APersonalityIsDrawnInPairsUntilItGivesTheNatureAndIsntShiny()
    {
        // The low half first: 0x0003 then 0x0001 make 0x00010003 (65539 % 25 = 14, Naive); 0x0001, 0x0000 make 1 (Lonely)
        var draws = new Queue<int>(new[] { 0x0003, 0x0001, 0x0001, 0x0000 });
        Assert.Equal(1u, FrontierTrainers.PersonalityFor(Nature.Lonely, otId: 0x12345678, draws.Dequeue));
        Assert.Empty(draws);
        // One that would be shiny for the number the Pokémon carry is drawn again: 0 against trainer 0 is shiny
        draws = new Queue<int>(new[] { 0, 0, 25, 0 });
        Assert.Equal(25u, FrontierTrainers.PersonalityFor(Nature.Hardy, otId: 0, draws.Dequeue));
    }

    [Fact]
    public void AHallsOpponentsAreDrawnWithNoSpeciesOrItemTwiceAndKeptClearOfAPartners()
    {
        // BattleFrontier_GetPokemonForTrainers, for the facilities that share it
        var rng = new Random(11);
        var single = FrontierTrainers.ForTrainers(3, 150, 0, withPartner: false, () => rng.Next(1 << 16));
        var team = Assert.Single(single);
        Assert.Equal(3, team.Pokemon.Count);
        Assert.Equal(3, team.Pokemon.Select(p => p.Species.Name).Distinct().Count());
        Assert.Equal(3, team.Sets.Select(s => FrontierDatabase.Set(s).Item ?? "").Distinct().Count());
        Assert.All(team.Pokemon, p => Assert.Equal((50, 12, false), (p.Level, p.IvAttack, p.IsShiny)));
        Assert.All(team.Pokemon.Zip(team.Sets), pair => Assert.Equal(FrontierDatabase.Set(pair.Second).Nature, pair.First.Nature));
        var asTrainer = team.AsTrainer();
        Assert.Equal((AiFlags.Basic | AiFlags.EvalAttack | AiFlags.Expert, 0), (asTrainer.Ai, asTrainer.PrizeMoney));
        Assert.Empty(asTrainer.Items);

        for (int seed = 0; seed < 20; seed++)
        {
            var draw = new Random(seed);
            var pair = FrontierTrainers.ForTrainers(4, 210, 230, withPartner: true, () => draw.Next(1 << 16));
            Assert.Equal(new[] { 2, 2 }, pair.Select(t => t.Pokemon.Count));
            Assert.Equal((21, 31), (pair[0].Pokemon[0].IvHP, pair[1].Pokemon[0].IvHP));
            Assert.Equal(4, pair.SelectMany(t => t.Pokemon).Select(p => p.Species.Name).Distinct().Count());
        }
    }

    // ------------------------------------------------------------------ the Brains' frame

    [Fact]
    public void ABrainStandsInTheWayAtTheSameTwoBattlesAndLeavesAPrintEachTime()
    {
        // FACTORY_, CASTLE_ and ARCADE_STREAK_SILVER_BATTLE 21 and _GOLD_BATTLE 49; the Hall's 50 and 170
        Assert.Equal((21, 49), FrontierBrains.Battles(FrontierFacility.Tower));
        Assert.Equal((21, 49), FrontierBrains.Battles(FrontierFacility.Arcade));
        Assert.Equal((50, 170), FrontierBrains.Battles(FrontierFacility.Hall));
        Assert.Equal((7, 10), (FrontierBrains.BattlesPerRound(FrontierFacility.Castle), FrontierBrains.BattlesPerRound(FrontierFacility.Hall)));
        Assert.Equal(FrontierPrint.Silver, FrontierBrains.NextBattle(FrontierFacility.Factory, 20));
        Assert.Equal(FrontierPrint.Gold, FrontierBrains.NextBattle(FrontierFacility.Factory, 48));
        Assert.Equal(FrontierPrint.None, FrontierBrains.NextBattle(FrontierFacility.Factory, 21));
        Assert.Equal(FrontierPrint.Silver, FrontierBrains.NextBattle(FrontierFacility.Hall, 49));

        // VAR_BATTLE_TOWER_PRINT_STATE: 0, 1 beaten, 2 handed the silver print, 3 beaten again, 4 handed the gold one
        var story = new StoryState();
        Assert.Equal(FrontierPrint.None, FrontierBrains.HandOver(story, FrontierFacility.Tower, out _));
        FrontierBrains.Beaten(story, FrontierFacility.Tower, FrontierPrint.Gold);
        Assert.Equal(0, story.Var("VAR_BATTLE_TOWER_PRINT_STATE"));
        FrontierBrains.Beaten(story, FrontierFacility.Tower, FrontierPrint.Silver);
        Assert.Equal(1, story.Var("VAR_BATTLE_TOWER_PRINT_STATE"));
        Assert.Equal(FrontierPrint.None, FrontierBrains.PrintOf(story, FrontierFacility.Tower));
        Assert.False(FrontierBrains.AnyPrint(story));
        Assert.Equal(FrontierPrint.Silver, FrontierBrains.HandOver(story, FrontierFacility.Tower, out bool all));
        Assert.False(all);
        Assert.Equal((2, FrontierPrint.Silver, true), (story.Var("VAR_BATTLE_TOWER_PRINT_STATE"), FrontierBrains.PrintOf(story, FrontierFacility.Tower), FrontierBrains.AnyPrint(story)));
        // Beaten for the silver print again changes nothing
        FrontierBrains.Beaten(story, FrontierFacility.Tower, FrontierPrint.Silver);
        Assert.Equal(2, story.Var("VAR_BATTLE_TOWER_PRINT_STATE"));
        FrontierBrains.Beaten(story, FrontierFacility.Tower, FrontierPrint.Gold);
        Assert.Equal(3, story.Var("VAR_BATTLE_TOWER_PRINT_STATE"));

        // Common_CheckAllFrontierGoldPrintsObtained: the fifth gold print sets the flag for them all, once
        foreach (var other in new[] { FrontierFacility.Factory, FrontierFacility.Hall, FrontierFacility.Castle, FrontierFacility.Arcade })
            story.SetVar(FrontierBrains.PrintVariable(other), 4);
        Assert.Equal(FrontierPrint.Gold, FrontierBrains.HandOver(story, FrontierFacility.Tower, out all));
        Assert.True(all);
        Assert.True(story.Has(FrontierBrains.AllGoldFlag));
        Assert.Equal("VAR_BATTLE_FACTORY_PRINT_STATE", FrontierBrains.PrintVariable(FrontierFacility.FactoryOpen));
    }

    [Fact]
    public void TheVillasBustsWaitForABattleWonEverywhereAndForAPrint()
    {
        // ScrCmd_CheckMetFurnitureRequirements: each facility's victories at least one; any print state of 2 or more
        var records = new FrontierRecords();
        foreach (var facility in new[] { FrontierFacility.Tower, FrontierFacility.Castle, FrontierFacility.Hall, FrontierFacility.Arcade }) records.CountVictories(facility, 3);
        Assert.False(records.WonEverywhere);
        records.CountVictories(FrontierFacility.FactoryOpen, 1);
        Assert.True(records.WonEverywhere);

        var story = new StoryState();
        story.SetVar("VAR_BATTLE_HALL_PRINT_STATE", 1);
        Assert.False(FrontierBrains.AnyPrint(story));
        story.SetVar("VAR_BATTLE_HALL_PRINT_STATE", 2);
        Assert.True(FrontierBrains.AnyPrint(story));
        Assert.Contains(VillaRules.Catalogue(story, new VillaRecords(records.WonEverywhere, FrontierBrains.AnyPrint(story), 0, 0, 0, 0, 0)), p => p.Name == "Pokémon Bust");
        Assert.Contains(VillaRules.Catalogue(story, new VillaRecords(records.WonEverywhere, FrontierBrains.AnyPrint(story), 0, 0, 0, 0, 0)), p => p.Name == "Silver Pokémon Bust");
    }

    // ------------------------------------------------------------------ the Tower's generator and its trainers

    private static BattleTowerChallenge Begun(FrontierRecords records, TowerMode mode, Party party)
    {
        var challenge = BattleTowerChallenge.Begin(records, mode);
        challenge.Enter(party, Enumerable.Range(0, challenge.PartySize));
        challenge.TakeSeed(records);
        challenge.DrawTrainers();
        return challenge;
    }

    private static Party Six() => Team(A("Garchomp", 60, "Leftovers"), A("Lucario", 55, "Lum Berry"), A("Togekiss", 50), A("Gengar", 52, "Focus Sash"), A("Milotic", 48), A("Staraptor", 50));

    [Fact]
    public void TheTowersGeneratorIsItsOwn()
    {
        // sub_0206BFF0: state × 48828125 + 1; BattleTower_GetRandom: the state over 65535. From 1: 48828126 gives 745,
        // 2249193895 gives 34320, 577808172 gives 8816, 1668331773 gives 25457
        var records = new FrontierRecords();
        records.Tower.RngState = 12345;
        var challenge = BattleTowerChallenge.Begin(records, TowerMode.Single);
        // sub_0206C02C for a new streak: the kept number × 1566083941 + 1 (1658452350), and the generator starts a step on (1367295431)
        challenge.TakeSeed(records);
        Assert.Equal((1658452350u, 1367295431u), (records.Tower.RngState, challenge.Seed));
        Assert.Equal(48828126u, BattleTowerChallenge.Step(1));
        Assert.Equal(new[] { 48828126u, 2249193895u, 577808172u, 1668331773u },
            new[] { 1u, 48828126u, 2249193895u, 577808172u }.Select(BattleTowerChallenge.Step));
        Assert.Equal(unchecked(1367295431u * 48828125u + 1u), BattleTowerChallenge.Step(challenge.Seed));
        Assert.Equal((int)((BattleTowerChallenge.Step(1367295431u) / 65535u) & 0xFFFF), challenge.Random());

        // sub_0206C068 for a streak going on: a step, and twenty-four more for every room come through; the kept number stays
        var running = new FrontierRecords();
        running.Tower.RngState = 12345;
        running.SetRunning(FrontierFacility.Tower, (int)TowerMode.Single, true);
        running.Tower.Rooms[0] = 2;
        var next = BattleTowerChallenge.Begin(running, TowerMode.Single);
        next.TakeSeed(running);
        Assert.Equal((12345u, 4230985542u, 2), (running.Tower.RngState, next.Seed, next.Room));
    }

    [Fact]
    public void ASetsTrainersAreDrawnFromTheRoomsRowsAndNoneTwice()
    {
        var records = new FrontierRecords();
        records.Tower.RngState = 2024;
        var first = Begun(records, TowerMode.Single, Six());
        Assert.Equal(7, first.Trainers.Count);
        Assert.Equal(7, first.Trainers.Distinct().Count());
        // The first room: six of trainers 0 to 99, and a seventh of the bosses' row, 100 to 119
        Assert.All(first.Trainers.Take(6), t => Assert.InRange(t, 0, 99));
        Assert.InRange(first.Trainers[6], 100, 119);

        // The same kept number draws the same set
        var again = new FrontierRecords();
        again.Tower.RngState = 2024;
        Assert.Equal(first.Trainers, Begun(again, TowerMode.Single, Six()).Trainers);

        // Beside a partner: seven pairs, each of the room's own row, the seventh pair both bosses
        var multi = Begun(records, TowerMode.Multi, Six());
        Assert.Equal(14, multi.Trainers.Distinct().Count());
        Assert.All(multi.Trainers.Take(12), t => Assert.InRange(t, 0, 99));
        Assert.All(multi.Trainers.Skip(12), t => Assert.InRange(t, 100, 119));
    }

    [Fact]
    public void TheTowerTycoonIsTheSeventhOfTheThirdAndOfTheSeventhSingleRoom()
    {
        // BattleTower_GetTrainerIDForRoomAndOpponentNum
        var records = new FrontierRecords();
        var single = BattleTowerChallenge.Begin(records, TowerMode.Single);
        Assert.Equal(305, single.TrainerFor(2, 6));
        Assert.Equal(306, single.TrainerFor(6, 6));
        Assert.InRange(single.TrainerFor(2, 5), 100, 139);
        Assert.InRange(single.TrainerFor(3, 6), 160, 179);
        Assert.InRange(single.TrainerFor(7, 6), 200, 299);
        Assert.InRange(single.TrainerFor(40, 0), 200, 299);
        // The Double Battle Room has no Tycoon: the third room's seventh is of the bosses' row, 140 to 159
        var doubles = BattleTowerChallenge.Begin(records, TowerMode.Double);
        Assert.InRange(doubles.TrainerFor(2, 6), 140, 159);
        // The room is counted in a byte, as the original passes it: room 258 is the third room again
        Assert.Equal(305, single.TrainerFor(258, 6));
        Assert.InRange(single.TrainerFor(256, 0), 0, 99);
    }

    [Fact]
    public void AnOpponentsTeamIsOfDifferentSpeciesAndItemsAndTheSameEveryTimeItIsComeTo()
    {
        var records = new FrontierRecords();
        records.Tower.RngState = 77;
        var challenge = Begun(records, TowerMode.Double, Six());
        uint before = challenge.Seed;
        var team = Assert.Single(challenge.MakeOpponents());
        Assert.Equal(challenge.Trainers[0], team.Number);
        Assert.Equal(4, team.Pokemon.Count);
        Assert.Equal(4, team.Pokemon.Select(p => p.Species.Name).Distinct().Count());
        Assert.Equal(4, team.Pokemon.Select(p => p.HeldItem?.Name ?? "").Distinct().Count());
        Assert.All(team.Pokemon, p => Assert.Equal((50, FrontierTrainers.IvsFor(team.Number), false), (p.Level, p.IvSpeed, p.IsShiny)));
        Assert.All(team.Pokemon.Zip(team.Sets), pair => Assert.Equal(FrontierDatabase.Set(pair.Second).Nature, pair.First.Nature));
        Assert.False(team.ReservedItems);
        Assert.NotEqual(before, challenge.Seed);

        // The same kept number, the same room, the same trainer: the same Pokémon
        var again = new FrontierRecords();
        again.Tower.RngState = 77;
        var same = Assert.Single(Begun(again, TowerMode.Double, Six()).MakeOpponents());
        Assert.Equal(team.Pokemon.Select(p => (p.Species.Name, p.Personality)), same.Pokemon.Select(p => (p.Species.Name, p.Personality)));
        Assert.Equal(team.OtId, same.OtId);
    }

    [Fact]
    public void TwoOpponentsOfAMultiBattleShareNoSpeciesAndAPartnerKeepsClearOfThePlayers()
    {
        var party = Team(A("Milotic", 50, "Leftovers"), A("Snorlax", 50, "Lum Berry"));
        for (uint seed = 1; seed < 12; seed++)
        {
            var records = new FrontierRecords();
            records.Tower.RngState = seed * 7919;
            var challenge = Begun(records, TowerMode.Multi, party);
            var pair = challenge.MakeOpponents();
            Assert.Equal(new[] { challenge.Trainers[0], challenge.Trainers[1] }, pair.Select(t => t.Number));
            Assert.Equal(new[] { 2, 2 }, pair.Select(t => t.Pokemon.Count));
            Assert.Equal(4, pair.SelectMany(t => t.Pokemon).Select(p => p.Species.Name).Distinct().Count());

            // sub_0204A97C: five partners with two Pokémon each, none of the player's species
            challenge.MakePartners(party);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, challenge.Candidates.Select(c => c.Id));
            foreach (var candidate in challenge.Candidates)
            {
                var team = challenge.TeamOf(candidate);
                Assert.Equal(300 + candidate.Id, team.Number);
                Assert.Equal(2, team.Pokemon.Count);
                Assert.DoesNotContain(team.Pokemon, p => p.Species.Name is "Milotic" or "Snorlax");
                if (!candidate.ReservedItems) Assert.DoesNotContain(team.Pokemon, p => p.HeldItem?.Name is "Leftovers" or "Lum Berry");
                // The same sets, number and personalities build the same two (sub_0204B404, when a challenge is taken up again)
                Assert.Equal(team.Pokemon.Select(p => (p.Species.Name, p.Personality, p.Nature)), challenge.TeamOf(candidate).Pokemon.Select(p => (p.Species.Name, p.Personality, p.Nature)));
                var (species, move) = BattleTowerChallenge.Tells(candidate, 0);
                Assert.Equal((team.Pokemon[0].Species.Name, team.Pokemon[0].Moves[0].Name), (species, move));
            }
            challenge.ChoosePartner(3);
            Assert.Equal(3, challenge.Partner!.Id);
        }
    }

    // ------------------------------------------------------------------ a streak

    private static void WinSeven(BattleTowerChallenge challenge)
    {
        for (int i = 0; i < 7; i++)
        {
            Assert.False(challenge.DefeatedSeven);
            challenge.MakeOpponents();
            challenge.Won();
        }
        Assert.True(challenge.DefeatedSeven);
    }

    [Fact]
    public void ASetWonGoesOnToTheNextRoomAndEarnsItsBattlePoints()
    {
        var records = new FrontierRecords();
        records.Tower.RngState = 5;
        var party = Six();
        // soloBattlePoints by the room the streak is left in: 3, 3, then the Tycoon's 20, 4, 5, 5, the Tycoon's 20, 7, 7
        int[] expected = { 3, 3, 20, 4, 5, 5, 20, 7, 7 };
        for (int set = 0; set < expected.Length; set++)
        {
            var challenge = Begun(records, TowerMode.Single, party);
            Assert.Equal((set * 7, set), (challenge.StreakBefore, challenge.Room));
            for (int i = 0; i < 7; i++)
            {
                var foe = Assert.Single(challenge.MakeOpponents());
                // The Tower Tycoon comes at a streak of 20 and of 48, and at no other time
                var palmer = challenge.Streak == 20 ? FrontierPrint.Silver : challenge.Streak == 48 ? FrontierPrint.Gold : FrontierPrint.None;
                Assert.Equal(palmer, challenge.PalmerNext);
                Assert.Equal(palmer != FrontierPrint.None, foe.Record.Class == "Tower Tycoon");
                if (palmer != FrontierPrint.None)
                {
                    Assert.Equal(palmer == FrontierPrint.Silver ? new[] { "Dragonite", "Milotic", "Rhyperior" } : new[] { "Cresselia", "Heatran", "Regigigas" },
                        foe.Pokemon.Select(p => p.Species.Name).OrderBy(n => n));
                    Assert.All(foe.Pokemon, p => Assert.Equal(31, p.IvHP));
                    challenge.BeatPalmer = palmer;
                }
                Assert.Equal(i + 2, challenge.Won());
            }
            Assert.True(challenge.DefeatedSeven);
            challenge.EndWon(records);
            Assert.Equal(expected[set], challenge.GivePoints(records));
            Assert.Equal(((set + 1) * 7, (set + 1) * 7, set + 1, true), (records.Get(FrontierStat.TowerLatestStreakSingle), records.Get(FrontierStat.TowerRecordStreakSingle),
                records.Tower.Rooms[0], records.IsRunning(FrontierFacility.Tower, 0)));
        }
        Assert.Equal((expected.Sum(), 63, 9), (records.BattlePoints, records.VictoriesAt(FrontierFacility.Tower), records.TowerChallenges));
    }

    [Fact]
    public void ALossEndsTheStreakWhereItStandsAndKeepsTheRecord()
    {
        var records = new FrontierRecords();
        records.Tower.RngState = 9;
        var party = Six();
        var first = Begun(records, TowerMode.Single, party);
        WinSeven(first);
        first.EndWon(records);
        uint kept = records.Tower.RngState;

        var second = Begun(records, TowerMode.Single, party);
        // A streak going on leaves the kept number alone, so its rooms are the same however often they are come to
        Assert.Equal(kept, records.Tower.RngState);
        second.MakeOpponents();
        second.Won();
        second.Won();
        Assert.Equal(9, second.Streak);
        // BattleTower_UpdateGameRecords
        second.EndLost(records);
        Assert.Equal((9, 9, 0, false, 9, 2), (records.Get(FrontierStat.TowerLatestStreakSingle), records.Get(FrontierStat.TowerRecordStreakSingle), records.Tower.Rooms[0],
            records.IsRunning(FrontierFacility.Tower, 0), records.VictoriesAt(FrontierFacility.Tower), records.TowerChallenges));

        // The next challenge starts from nothing, and a shorter streak leaves the record
        var third = Begun(records, TowerMode.Single, party);
        Assert.Equal((0, 0), (third.StreakBefore, third.Room));
        Assert.NotEqual(kept, records.Tower.RngState);
        third.Won();
        third.EndLost(records);
        Assert.Equal((1, 9), (records.Get(FrontierStat.TowerLatestStreakSingle), records.Get(FrontierStat.TowerRecordStreakSingle)));
        // Each way of taking the Tower keeps its own
        Assert.Equal(0, records.Get(FrontierStat.TowerRecordStreakDouble));
    }

    [Fact]
    public void AChallengePutDownIsTakenUpWhereItStood()
    {
        var records = new FrontierRecords();
        records.Tower.RngState = 31;
        var party = Six();
        var challenge = Begun(records, TowerMode.Single, party);
        challenge.MakeOpponents();
        challenge.Won();
        challenge.MakeOpponents();
        challenge.Won();
        // sub_0204A8C8, and BattleTower_Init with the challenge kept
        challenge.Pause(records);
        var paused = GameDataFiles.Deserialize<FrontierRecords>(GameDataFiles.Serialize(records));
        var resumed = BattleTowerChallenge.Resume(paused)!;
        Assert.Null(paused.Tower.Paused);
        Assert.Equal((TowerMode.Single, 3, 2, challenge.Seed), (resumed.Mode, resumed.NextOpponent, resumed.Wins, resumed.Seed));
        Assert.Equal(challenge.Trainers, resumed.Trainers);
        Assert.Equal(challenge.Slots, resumed.Slots);
        Assert.True(resumed.StillValid(party));
        // The third trainer's team is the one it would have been
        Assert.Equal(challenge.MakeOpponents()[0].Pokemon.Select(p => (p.Species.Name, p.Personality)), resumed.MakeOpponents()[0].Pokemon.Select(p => (p.Species.Name, p.Personality)));
        Assert.Null(BattleTowerChallenge.Resume(paused));

        // Whoever took one of the Pokémon entered out of the team in the meantime can't go on
        Assert.False(resumed.StillValid(Team(party.Members[0], party.Members[1])));
        var changed = Six();
        changed.Members[1].HeldItem = ItemDatabase.Get("Leftovers");
        Assert.False(resumed.StillValid(changed));

        // sub_0204A064: a challenge walked out on ends its streak and moves the kept number on
        records.SetRunning(FrontierFacility.Tower, 0, true);
        records.Tower.Rooms[0] = 4;
        records.Set(FrontierStat.TowerLatestStreakSingle, 28);
        records.Set(FrontierStat.TowerRecordStreakSingle, 28);
        uint was = records.Tower.RngState;
        BattleTowerChallenge.Forfeit(records, TowerMode.Single);
        Assert.Equal((false, 0, 0, 28), (records.IsRunning(FrontierFacility.Tower, 0), records.Tower.Rooms[0], records.Get(FrontierStat.TowerLatestStreakSingle), records.Get(FrontierStat.TowerRecordStreakSingle)));
        Assert.Equal(BattleTowerChallenge.NextStreak(was), records.Tower.RngState);
        Assert.Null(records.Tower.Paused);
    }

    [Fact]
    public void TheTowersRibbonsGoToThePokemonEntered()
    {
        var party = Six();
        var records = new FrontierRecords();
        var single = BattleTowerChallenge.Begin(records, TowerMode.Single);
        single.Enter(party, new[] { 1, 3, 5 });
        // sub_0204AB68: nothing without the Tycoon beaten; the Ability Ribbon for the silver time, the Great Ability Ribbon for the gold
        Assert.False(single.GivePalmerRibbon(party));
        single.BeatPalmer = FrontierPrint.Silver;
        Assert.True(single.GivePalmerRibbon(party));
        Assert.Equal(new[] { false, true, false, true, false, true }, party.Members.Select(p => p.Ribbons.Contains("RIBBON_ABILITY")));
        Assert.False(single.GivePalmerRibbon(party));
        single.BeatPalmer = FrontierPrint.Gold;
        Assert.True(single.GivePalmerRibbon(party));
        Assert.Contains("RIBBON_GREAT_ABILITY", party.Members[3].Ribbons);
        Assert.False(single.GiveStreakRibbon(party));

        // sub_0204ACC8 and sub_0204ABA0: a streak of fifty in the Double or the Multi Battle Room
        records.SetRunning(FrontierFacility.Tower, (int)TowerMode.Double, true);
        records.Set(FrontierStat.TowerLatestStreakDouble, 42);
        records.Tower.Rooms[(int)TowerMode.Double] = 6;
        var doubles = BattleTowerChallenge.Begin(records, TowerMode.Double);
        doubles.Enter(party, new[] { 0, 1, 2, 3 });
        for (int i = 0; i < 7; i++) doubles.Won();
        doubles.EndWon(records);
        Assert.False(doubles.StreakRibbonDue);
        Assert.False(doubles.GiveStreakRibbon(party));
        var more = BattleTowerChallenge.Begin(records, TowerMode.Double);
        more.Enter(party, new[] { 0, 1, 2, 3 });
        more.Won();
        more.EndLost(records);
        Assert.True(more.StreakRibbonDue);
        Assert.True(more.GiveStreakRibbon(party));
        Assert.Equal(new[] { true, true, true, true, false, false }, party.Members.Select(p => p.Ribbons.Contains("RIBBON_DOUBLE_ABILITY")));

        records.SetRunning(FrontierFacility.Tower, (int)TowerMode.Multi, true);
        records.Set(FrontierStat.TowerLatestStreakMulti, 49);
        var multi = BattleTowerChallenge.Begin(records, TowerMode.Multi);
        multi.Enter(party, new[] { 4, 5 });
        multi.Won();
        multi.EndLost(records);
        Assert.True(multi.GiveStreakRibbon(party));
        Assert.Contains("RIBBON_MULTI_ABILITY", party.Members[5].Ribbons);
        // Every ribbon given is one of Platinum's eighty
        Assert.All(new[] { "RIBBON_ABILITY", "RIBBON_GREAT_ABILITY", "RIBBON_DOUBLE_ABILITY", "RIBBON_MULTI_ABILITY" }, r => Assert.NotNull(RibbonDatabase.Get(r)));
    }

    [Fact]
    public void ATrophyIsDueForASingleRecordOfTwentyFiftyAndAHundred()
    {
        // sub_0206BDBC and sub_0206BF04
        var records = new FrontierRecords();
        Assert.Equal((0, 0), (BattleTowerChallenge.TrophyWaiting(records), BattleTowerChallenge.TrophyDue(records, () => true)));
        records.Set(FrontierStat.TowerRecordStreakSingle, 21);
        Assert.Equal(1, BattleTowerChallenge.TrophyWaiting(records));
        // No room in the Underground's PC: it waits, and that is remembered
        Assert.Equal(4, BattleTowerChallenge.TrophyDue(records, () => false));
        Assert.Equal(4, BattleTowerChallenge.TrophyWaiting(records));
        Assert.Equal(1, BattleTowerChallenge.TrophyDue(records, () => true));
        Assert.Equal((0, 0), (BattleTowerChallenge.TrophyWaiting(records), BattleTowerChallenge.TrophyDue(records, () => true)));
        // One at a time, the silver before the gold
        records.Set(FrontierStat.TowerRecordStreakSingle, 140);
        Assert.Equal(2, BattleTowerChallenge.TrophyWaiting(records));
        Assert.Equal(2, BattleTowerChallenge.TrophyDue(records, () => true));
        Assert.Equal(3, BattleTowerChallenge.TrophyWaiting(records));
        Assert.Equal(4, BattleTowerChallenge.TrophyDue(records, () => false));
        Assert.Equal(6, BattleTowerChallenge.TrophyWaiting(records));
        Assert.Equal(3, BattleTowerChallenge.TrophyDue(records, () => true));
        Assert.Equal(0, BattleTowerChallenge.TrophyDue(records, () => true));
        // They are goods of the Underground
        Assert.All(BattleTowerChallenge.Trophies, t => Assert.NotEqual(0, UndergroundDatabase.GoodId(t.Good)));
        Assert.Equal(new[] { 20, 50, 100 }, BattleTowerChallenge.Trophies.Select(t => t.Streak));
    }

    [Fact]
    public void TheBoardShowsEachWayOfTakingTheTower()
    {
        var records = new FrontierRecords();
        records.Set(FrontierStat.TowerRecordStreakDouble, 30);
        records.Set(FrontierStat.TowerLatestStreakDouble, 14);
        records.SetRunning(FrontierFacility.Tower, (int)TowerMode.Double, true);
        records.GivePoints(9);
        var story = new StoryState();
        story.SetVar("VAR_BATTLE_TOWER_PRINT_STATE", 2);
        var board = FrontierBoard.For(FrontierFacility.Tower, records, story);
        Assert.Equal(("Battle Tower", 9, FrontierPrint.Silver), (board.Title, board.BattlePoints, board.Print));
        Assert.Equal(new[] { "Single Battle", "Double Battle", "Multi Battle" }, board.Rows.Select(r => r.Label));
        Assert.Equal(new FrontierBoardRow("Double Battle", 30, 14, true), board.Rows[1]);
        Assert.Equal(new FrontierBoardRow("Single Battle", 0, 0, false), board.Rows[0]);
        // A facility not built yet has a board with no rows
        Assert.Empty(FrontierBoard.For(FrontierFacility.Arcade, records, story).Rows);
    }

    // ------------------------------------------------------------------ a Frontier battle

    private static BattleCore FrontierCore(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null, bool frontier = true)
    {
        var trainer = new Trainer { Id = "frontier_test", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, Ai = FrontierTrainers.Mind };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = Team(mine), Trainers = new List<Trainer> { trainer }, Random = rolls ?? Calm(), Rules = Ruleset.Platinum,
            Conditions = new BattleConditions { Money = 5000, Terrain = BattleTerrain.Special }, PlayerController = null, EnemyController = null, PlayerName = "Lucas", Frontier = frontier
        });
        core.Start();
        return core;
    }

    [Fact]
    public void AFrontierBattleGivesNoExpAndNoMoney()
    {
        // BATTLE_TYPE_NO_EXPERIENCE, and subscript_battle_won's jump past PayPrizeMoney for BATTLE_TYPE_FRONTIER
        var mine = Mon("Garchomp", 50, "Earthquake");
        int exp = mine.CurrentExp;
        var core = FrontierCore(new[] { mine }, new[] { Mon("Pikachu", 5, "Growl") });
        var said = Turn(core);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Equal(exp, mine.CurrentExp);
        Assert.DoesNotContain(said, l => l.Contains("EXP") || l.Contains("received $"));
        Assert.Contains(said, l => l.Contains("defeated Ace Trainer Vera"));

        // The same battle anywhere else pays, and teaches
        var other = Mon("Garchomp", 50, "Earthquake");
        var plain = FrontierCore(new[] { other }, new[] { Mon("Pikachu", 5, "Growl") }, frontier: false);
        Assert.Contains(Turn(plain), l => l.Contains("received $"));
        Assert.True(other.CurrentExp > exp);
    }

    [Fact]
    public void AFrontierBattleLostCostsNothingAndNobodyBlacksOut()
    {
        // subscript_battle_lost for BATTLE_TYPE_FRONTIER
        var core = FrontierCore(new[] { Mon("Pikachu", 5, "Growl") }, new[] { Mon("Garchomp", 50, "Earthquake") });
        var said = Turn(core);
        Assert.Equal(BattleResult.PlayerDefeat, core.Result);
        Assert.Equal(0, core.MoneyLost);
        Assert.DoesNotContain(said, l => l.Contains("whited out") || l.Contains("handed $"));
        Assert.IsType<Ended>(core.Log[^1]);
    }

    [Fact]
    public void NothingFromTheBagIsUsedAndRunGivesTheMatchUp()
    {
        var core = FrontierCore(new[] { Mon("Garchomp", 50, "Earthquake") }, new[] { Mon("Blissey", 50, "Growl") });
        // PLAYER_INPUT_ITEM in a Frontier battle
        Assert.Equal(BattleCore.FrontierBagRefusal, core.WhyNot(BattleChoice.UseItem(Mine, "Potion", 0)));
        // COMMAND_SELECTION_RUN_SELECT: nothing holds the player back, and the match is lost
        Assert.Null(core.WhyNot(BattleChoice.Run(Mine)));
        core.Submit(BattleChoice.Run(Mine), BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleResult.PlayerDefeat, core.Result);
        Assert.Contains(Lines(core.Log), l => l.Contains("forfeited"));
    }

    [Fact]
    public void ASoulDewDoesNothingInTheFrontier()
    {
        // BattleSystem_CalcMoveDamage: HOLD_EFFECT_LATI_SPECIAL is left out when the battle type has BATTLE_TYPE_FRONTIER
        int Hit(bool frontier, bool dew)
        {
            var latios = Mon("Latios", 50, "Dragon Pulse");
            if (dew) latios.HeldItem = ItemDatabase.Get("Soul Dew");
            var foe = Mon("Blissey", 50, "Growl");
            int before = foe.CurrentHP;
            Turn(FrontierCore(new[] { latios }, new[] { foe }, frontier: frontier));
            return before - foe.CurrentHP;
        }
        int plain = Hit(frontier: true, dew: false);
        Assert.Equal(plain, Hit(frontier: true, dew: true));
        Assert.True(Hit(frontier: false, dew: true) > plain);
    }

    [Fact]
    public void AFoesThiefTakesThePlayersItemInTheFrontierOnly()
    {
        // BtlCmd_TryStealItem: an AI trainer's Pokémon steals nothing outside the Battle Frontier
        Pokemon Robbed(bool frontier)
        {
            var mine = With(Mon("Blissey", 50, "Growl"), item: "Leftovers");
            Turn(FrontierCore(new[] { mine }, new[] { Mon("Sneasel", 50, "Thief") }, frontier: frontier));
            return mine;
        }
        Assert.NotNull(Robbed(frontier: false).HeldItem);
        var core = FrontierCore(new[] { With(Mon("Blissey", 50, "Growl"), item: "Leftovers") }, new[] { Mon("Sneasel", 50, "Thief") });
        var said = Turn(core);
        Assert.Contains(said, l => l.Contains("stole"));
    }

    private static BattleEngine FrontierBattle(Pokedex pokedex, Party team)
    {
        var trainer = new Trainer { Id = "frontier_test", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 0, Ai = FrontierTrainers.Mind, Look = "Lass" };
        trainer.Party.Add(Mon("Blissey", 50, "Growl"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = team, Inventory = new Inventory(), Pokedex = pokedex, Trainers = new List<Trainer> { trainer }, Random = Steady(), Rules = Ruleset.Platinum, Frontier = true
        });
        Settle(battle);
        return battle;
    }

    [Fact]
    public void RunAsksWhetherToForfeitAndThePokedexIsToldNothing()
    {
        var pokedex = new Pokedex();
        var battle = FrontierBattle(pokedex, Team(Mon("Garchomp", 50, "Earthquake")));
        Assert.True(battle.IsFrontier);
        // BattleSystem_DexFlagSeen leaves the Frontier's Pokémon out
        Assert.False(pokedex.IsSeen(PokemonDatabase.Get("Blissey")!.DexNumber));

        // BAG is refused at once, and the menu is back
        battle.SelectMainMenuOption(1);
        Assert.Equal("Nothing from the Bag may be used here.", battle.CurrentMessage);
        battle.ConfirmMessage();
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);

        battle.SelectMainMenuOption(3);
        Assert.True(battle.IsLearningMove);
        Assert.Equal((LearnStep.Forfeit, 1), (battle.LearnStep, battle.LearnCursor));
        Assert.Contains("Give the match up", battle.CurrentMessage);
        // Read on, the answer is No, and the menu is back
        battle.ConfirmMessage();
        Assert.False(battle.IsLearningMove);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Equal(BattleResult.None, battle.Result);

        battle.SelectMainMenuOption(3);
        battle.ChooseLearn(0);
        Settle(battle);
        Assert.Equal(BattleResult.PlayerDefeat, battle.Result);
    }

    // ------------------------------------------------------------------ the team entered on the party screen

    [Fact]
    public void ThePartyScreenEntersThePokemonInTheOrderChosen()
    {
        var party = Team(A("Garchomp"), A("Mewtwo"), A("Lucario"), A("Togekiss"));
        var screen = new PartyScreen();
        screen.OpenToEnter(3, FrontierRules.MayEnter);
        Assert.True(screen.Entering);
        Assert.Contains("3 to go", screen.Prompt);

        void Enter(int slot)
        {
            screen.SelectedIndex = slot;
            screen.Confirm(party);
        }
        Enter(2);
        // One that isn't let in is refused, and says so on its card
        Enter(1);
        Assert.Equal(new[] { 2 }, screen.Entered);
        Assert.Contains("can't take part", screen.Message);
        Assert.Equal("NOT ABLE", screen.EnterTag(party, party.Members[1]));
        Enter(0);
        Assert.Equal(("1ST", "2ND", (string?)null), (screen.EnterTag(party, party.Members[2]), screen.EnterTag(party, party.Members[0]), screen.EnterTag(party, party.Members[3])));
        // Chosen again, it is taken out; the cancel button takes the last one back
        Enter(2);
        Assert.Equal(new[] { 0 }, screen.Entered);
        Enter(3);
        screen.Cancel();
        Assert.Equal(new[] { 0 }, screen.Entered);
        Assert.True(screen.IsActive);
        Enter(3);
        Enter(2);
        Assert.Equal(new[] { 0, 3, 2 }, screen.Entered);
        Assert.Contains("Go in", screen.Prompt);
        Assert.False(screen.EnteredAll);
        screen.Confirm(party);
        Assert.True(screen.EnteredAll);
        Assert.False(screen.IsActive);

        // Backed out of with nobody entered, it closes with none
        screen.OpenToEnter(2, FrontierRules.MayEnter);
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.False(screen.EnteredAll);
    }

    // ------------------------------------------------------------------ the rooms

    [Fact]
    public void TheLobbyIsOnTheOriginalsPlanAndLeadsOutOntoTheFrontiersGrounds()
    {
        var lobby = Room("BattleTower");
        Assert.Equal((31, 21), (lobby.Width, lobby.Height));
        Assert.True(lobby.IsIndoors);
        // The three tiles of mat (the original's warps at 14, 15 and 16 of row 19), and the doors under them
        var out_ = lobby.Warps.Where(w => w.TargetMap == "BattleFrontier").OrderBy(w => w.SourceX).ToList();
        Assert.Equal(new[] { (14, 20), (15, 20), (16, 20) }, out_.Select(w => (w.SourceX, w.SourceY)));
        Assert.All(out_, w => Assert.Equal(18, w.TargetY));
        Assert.All(new[] { 14, 15, 16 }, x => Assert.True(lobby.IsWalkable(x, 19)));
        // Each attendant stands in the gap of a booth's counter, the lift's door two tiles behind
        foreach (int x in new[] { 11, 15, 19 })
        {
            Assert.True(lobby.IsSolid(x - 1, 5) && lobby.IsSolid(x + 1, 5));
            Assert.False(lobby.IsSolid(x, 3) || lobby.IsSolid(x, 4));
            Assert.NotNull(lobby.GetNpcAt(x, 5));
        }
        Assert.Equal(new[] { 11, 15 }, lobby.Warps.Where(w => w.TargetMap == "BattleTowerElevator").Select(w => w.SourceX).OrderBy(x => x));
        // The machine of the Tower's records beside the west booth, and the PC
        Assert.Equal("RecordsMachine", lobby.SignScripts[(13, 5)]);
        Assert.Contains(lobby.Everyone, n => n.IsPCTerminal && (n.GridX, n.GridY) == (22, 6));
        Assert.Equal("FLAG_HIDE_BATTLE_TOWER_TEALA", lobby.Everyone.Single(n => n.Key == "teala").HiddenBy);

        // The Battle Frontier's two doors of the Tower lead in, and the four halls stay shut
        var grounds = MapDatabase.Get("BattleFrontier");
        foreach (int x in new[] { 48, 49 })
        {
            var door = grounds.GetWarpAt(x, 17)!;
            Assert.Equal(("BattleTower", 15, 19), (door.TargetMap, door.TargetX, door.TargetY));
        }
        Assert.True(grounds.IsWalkable(48, 18) && grounds.IsWalkable(49, 18));
    }

    [Fact]
    public void TheTowersOtherRoomsAreOnTheOriginalsPlans()
    {
        // events_battle_tower_elevator, _corridor, _battle_room, _corridor_multi, _multi_battle_room, _battle_salon
        var sizes = new Dictionary<string, (int, int)>
        {
            ["BattleTowerElevator"] = (7, 8), ["BattleTowerCorridor"] = (22, 8), ["BattleTowerBattleRoom"] = (15, 12),
            ["BattleTowerCorridorMulti"] = (18, 9), ["BattleTowerMultiBattleRoom"] = (16, 11), ["BattleTowerBattleSalon"] = (17, 13)
        };
        foreach (var (name, size) in sizes)
        {
            var room = Room(name);
            Assert.Equal(size, (room.Width, room.Height));
            Assert.True(room.IsIndoors);
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id);
            // Whoever finds themselves in one with no challenge under way has a way back to the lobby
            Assert.Contains(room.Warps, w => w.TargetMap == "BattleTower");
            Assert.All(room.Warps, w => Assert.True(MapDatabase.MapNames.Contains(w.TargetMap), w.TargetMap));
        }
        // The corridor's four doors, four tiles apart (WARP_NORTH at 6, 10, 14 and 18)
        Assert.Equal(new[] { 6, 10, 14, 18 }, Room("BattleTowerCorridor").Warps.Where(w => w.TargetMap == "BattleTowerBattleRoom").Select(w => w.SourceX).OrderBy(x => x));
        // The battle rooms' people: the opponents behind their flags, at the head of the room
        var single = Room("BattleTowerBattleRoom");
        Assert.Equal("FLAG_HIDE_BATTLE_TOWER_BATTLE_ROOM_OPPONENT", single.Everyone.Single(n => n.Key == "opponent").HiddenBy);
        var multi = Room("BattleTowerMultiBattleRoom");
        Assert.Equal(new[] { "FLAG_HIDE_MULTI_BATTLE_ROOM_OPPONENT_1", "FLAG_HIDE_MULTI_BATTLE_ROOM_OPPONENT_2" },
            new[] { "opponent", "opponent_2" }.Select(k => multi.Everyone.Single(n => n.Key == k).HiddenBy));
        Assert.All(new[] { single, multi }, r => Assert.Equal(BattleArena.Gym, r.Arena));
        // The Battle Salon's five wait behind their flags
        var salon = Room("BattleTowerBattleSalon");
        Assert.Equal(BattleTowerChallenge.SalonFlags, new[] { "cheryl", "mira", "riley", "marley", "buck" }.Select(k => salon.Everyone.Single(n => n.Key == k).HiddenBy));
    }

    // ------------------------------------------------------------------ a challenge played by its scripts

    private static HeadlessScriptHost Challenger()
    {
        var host = new HeadlessScriptHost();
        foreach (var p in Six().Members) host.Party.Add(p);
        host.Frontier.Tower.RngState = 4242;
        return host;
    }

    /// <summary>The counter's talk for a Single Battle: CHALLENGE, SINGLE BATTLE, the first three of the team, yes to the save.</summary>
    private static void TakeSingle(HeadlessScriptHost host, ScriptRunner runner, int room = 0)
    {
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(room);
        host.Answers.Enqueue(0);
        Play(host, runner, "BattleTower", "SingleAttendant", "single_attendant");
    }

    [Fact]
    public void ASingleChallengeIsPlayedFromTheCounterToTheBattlePoints()
    {
        var host = Challenger();
        host.Party.Members[0].CurrentHP = 1;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        TakeSingle(host, runner);

        // The counter: the challenge begun, the three entered, the team healed, the game saved, the set drawn, the lift
        var tower = host.Tower!;
        Assert.Equal((TowerMode.Single, 3, 7), (tower.Mode, tower.Slots.Count, tower.Trainers.Count));
        Assert.Equal(host.Party.Members[0].MaxHP, host.Party.Members[0].CurrentHP);
        Assert.Contains("frontier save 0", host.Log);
        Assert.Equal((255, 1, 1), (host.Story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION"), host.Story.Var("VAR_BATTLE_TOWER_ELEVATOR_LOAD_ACTION"), host.Frontier.ChallengesStarted));
        Assert.Contains(host.Log, l => l.Contains("warp BattleTowerElevator"));

        Play(host, runner, "BattleTowerElevator", "OnEnter");
        Assert.Contains(host.Log, l => l.Contains("warp BattleTowerCorridor"));
        Play(host, runner, "BattleTowerCorridor", "OnEnter");
        Assert.Contains(host.Log, l => l.Contains("warp BattleTowerBattleRoom"));

        // Seven battles, each with the three entered at level 50 and against one of the set's trainers
        Play(host, runner, "BattleTowerBattleRoom", "OnEnter");
        Assert.Equal(7, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.Equal(new[] { ("Garchomp", 50), ("Lucario", 50), ("Togekiss", 50) }, b.Team.Members.Select(p => (p.Species.Name, p.Level))));
        Assert.All(host.FrontierBattles, b => Assert.False(b.Doubles));
        Assert.Equal(tower.Trainers.Select(t => "frontier_" + FrontierDatabase.Trainer(t).Id), host.FrontierBattles.Select(b => b.Foes.Single().Id));
        Assert.All(host.FrontierBattles, b => Assert.Equal(3, b.Foes[0].Party.Count));
        // The records, the trainer's score and the Battle Points, and back down to the lobby
        Assert.Equal((7, 7, 3, 1, true), (host.Frontier.Get(FrontierStat.TowerLatestStreakSingle), host.Frontier.Get(FrontierStat.TowerRecordStreakSingle), host.Frontier.BattlePoints,
            host.Frontier.Tower.Rooms[0], host.Frontier.IsRunning(FrontierFacility.Tower, 0)));
        Assert.Equal(BattleTowerChallenge.SetScore, host.FrontierScore);
        Assert.True(Said(host, "received 3 BP"));
        Assert.Equal(1, host.Story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION"));
        Assert.Contains(host.Log, l => l.Contains("warp BattleTower 15 6"));

        // The lobby: congratulations, the game saved, the challenge let go of
        host.Transcript.Clear();
        Play(host, runner, "BattleTower", "OnEnter");
        Assert.True(Said(host, "all seven"));
        Assert.Null(host.Tower);
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION"));
        Assert.True(host.Story.Has("FLAG_HIDE_BATTLE_TOWER_TEALA"));
    }

    [Fact]
    public void ABattleLostEndsTheChallengeWithNothingWon()
    {
        var host = Challenger();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        TakeSingle(host, runner);
        int fought = 0;
        host.Fight = _ => ++fought < 3 ? BattleOutcome.Won : BattleOutcome.Lost;
        Play(host, runner, "BattleTowerBattleRoom", "OnEnter");
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.Equal((2, 2, 0, false, 0), (host.Frontier.Get(FrontierStat.TowerLatestStreakSingle), host.Frontier.Get(FrontierStat.TowerRecordStreakSingle), host.Frontier.BattlePoints,
            host.Frontier.IsRunning(FrontierFacility.Tower, 0), host.FrontierScore));
        // The team heals between battles: twice here
        Assert.Equal(2, host.Transcript.Count(l => l.Text.Contains("restore your Pokémon")));

        Play(host, runner, "BattleTower", "OnEnter");
        Assert.True(Said(host, "Thank you for taking the challenge"));
        Assert.Null(host.Tower);
    }

    [Fact]
    public void AChallengeIsGivenUpOrPutDownBetweenBattles()
    {
        // RETIRE after the first battle
        var host = Challenger();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        TakeSingle(host, runner);
        host.Answers.Enqueue(2);
        host.Answers.Enqueue(0);
        Play(host, runner, "BattleTowerBattleRoom", "OnEnter");
        Assert.Single(host.FrontierBattles);
        Assert.Equal((1, false, 1), (host.Frontier.Get(FrontierStat.TowerLatestStreakSingle), host.Frontier.IsRunning(FrontierFacility.Tower, 0), host.Story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION")));

        // REST after the second battle: the challenge waits, the lobby saves it and asks; then it goes on from the third
        host = Challenger();
        runner = new ScriptRunner(ScriptLibrary.Default, host);
        TakeSingle(host, runner);
        var trainers = host.Tower!.Trainers.ToList();
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(1);
        host.Answers.Enqueue(0);
        Play(host, runner, "BattleTowerBattleRoom", "OnEnter");
        Assert.Equal(2, host.FrontierBattles.Count);
        Assert.Null(host.Tower);
        Assert.Equal((3, 2), (host.Frontier.Tower.Paused!.NextOpponent, host.Story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION")));
        // Not now: it goes on waiting
        host.Answers.Enqueue(1);
        Play(host, runner, "BattleTower", "OnEnter");
        Assert.Null(host.Tower);
        Assert.NotNull(host.Frontier.Tower.Paused);
        Assert.Equal(2, host.Story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION"));
        // Now, at the counter
        host.Answers.Enqueue(0);
        Play(host, runner, "BattleTower", "SingleAttendant", "single_attendant");
        Assert.Equal((3, 2), (host.Tower!.NextOpponent, host.Story.Var("VAR_BATTLE_TOWER_BATTLE_ROOM_LOAD_ACTION")));
        Assert.Equal(trainers, host.Tower.Trainers);
        Play(host, runner, "BattleTowerBattleRoom", "OnEnter");
        // Five more battles make the seven
        Assert.Equal(7, host.FrontierBattles.Count);
        Assert.Equal((7, 3), (host.Frontier.Get(FrontierStat.TowerLatestStreakSingle), host.Frontier.BattlePoints));
        Assert.Equal(trainers.Select(t => "frontier_" + FrontierDatabase.Trainer(t).Id), host.FrontierBattles.Select(b => b.Foes.Single().Id));
    }

    [Fact]
    public void TheTowerTycoonIsMetAtTheTwentyFirstBattleAndLeavesAPrintAndARibbon()
    {
        var host = Challenger();
        // Two sets won: a streak of fourteen, the third room next
        host.Frontier.Set(FrontierStat.TowerLatestStreakSingle, 14);
        host.Frontier.Set(FrontierStat.TowerRecordStreakSingle, 14);
        host.Frontier.SetRunning(FrontierFacility.Tower, 0, true);
        host.Frontier.Tower.Rooms[0] = 2;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        TakeSingle(host, runner);
        Play(host, runner, "BattleTowerBattleRoom", "OnEnter");

        Assert.Equal(7, host.FrontierBattles.Count);
        var last = host.FrontierBattles[^1].Foes.Single();
        Assert.Equal(("frontier_tower_tycoon_palmer_silver", "Tower Tycoon", "Palmer"), (last.Id, last.TrainerClass, last.Name));
        Assert.True(Said(host, "Tower Tycoon is on his way"));
        Assert.True(Said(host, "that boy of mine"));
        // BattleTower_GiveBattlePointsReward: twenty for the Tycoon; the print waits to be handed over
        Assert.Equal((21, 20, 1, 1), (host.Frontier.Get(FrontierStat.TowerRecordStreakSingle), host.Frontier.BattlePoints, host.Story.Var("VAR_BATTLE_TOWER_PRINT_STATE"), host.Frontier.BrainsMet));

        host.Transcript.Clear();
        Play(host, runner, "BattleTower", "OnEnter");
        Assert.Equal(2, host.Story.Var("VAR_BATTLE_TOWER_PRINT_STATE"));
        Assert.True(Said(host, "Silver Print"));
        Assert.Equal(new[] { true, true, true, false, false, false }, host.Party.Members.Select(p => p.Ribbons.Contains("RIBBON_ABILITY")));
        // A streak past twenty: the bronze trophy goes to the Underground's PC
        Assert.True(Said(host, "Bronze Trophy"));
        Assert.Contains(UndergroundDatabase.GoodId("Bronze Trophy"), host.Underground.PcGoods);
        Assert.True(host.Frontier.Tower.BronzeTrophy);
    }

    [Fact]
    public void TheCounterTurnsAwayATeamThatCantGoInTogether()
    {
        // Too few that may take part: the banned species the Pokédex has seen are named
        var host = new HeadlessScriptHost();
        host.Party.Add(A("Garchomp"));
        host.Party.Add(A("Dialga"));
        host.Party.Add(A("Lucario"));
        host.Seen(PokemonDatabase.Get("Dialga")!);
        host.Seen(PokemonDatabase.Get("Giratina")!);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        TakeSingle(host, runner);
        Assert.True(Said(host, "You need 3 Pokémon"));
        Assert.True(Said(host, "nor can Dialga and Giratina"));
        Assert.Null(host.Tower);

        // Two holding the same item: the counter says so, and nothing is begun
        host = new HeadlessScriptHost();
        host.Party.Add(A("Garchomp", item: "Leftovers"));
        host.Party.Add(A("Lucario", item: "Leftovers"));
        host.Party.Add(A("Togekiss"));
        host.Party.Add(A("Gengar"));
        host.Entering.Enqueue(new[] { 0, 1, 2 });
        runner = new ScriptRunner(ScriptLibrary.Default, host);
        TakeSingle(host, runner);
        Assert.True(Said(host, "may hold the same item"));
        Assert.Null(host.Tower);
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION"));

        // A Double Battle takes four
        host = Challenger();
        runner = new ScriptRunner(ScriptLibrary.Default, host);
        TakeSingle(host, runner, room: 1);
        Assert.Equal((TowerMode.Double, 4), (host.Tower!.Mode, host.Tower.Slots.Count));
        Play(host, runner, "BattleTowerBattleRoom", "OnEnter");
        Assert.All(host.FrontierBattles, b => Assert.True(b.Doubles && b.Team.Count == 4 && b.Foes.Single().Party.Count == 4));
        Assert.Equal(7, host.Frontier.Get(FrontierStat.TowerLatestStreakDouble));
    }

    [Fact]
    public void AGameClosedInTheMiddleOfAChallengeFindsItOver()
    {
        var host = Challenger();
        host.Frontier.SetRunning(FrontierFacility.Tower, 0, true);
        host.Frontier.Set(FrontierStat.TowerLatestStreakSingle, 7);
        host.Frontier.Tower.Rooms[0] = 1;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        TakeSingle(host, runner);
        // The save of the counter is come back to: the lobby's own script finds the challenge left open
        host.Tower = null;
        Play(host, runner, "BattleTower", "OnEnter");
        Assert.True(Said(host, "left before it was over"));
        Assert.Equal((false, 0, 0, 0), (host.Frontier.IsRunning(FrontierFacility.Tower, 0), host.Frontier.Get(FrontierStat.TowerLatestStreakSingle), host.Frontier.Tower.Rooms[0],
            host.Story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION")));
    }

    [Fact]
    public void AMultiChallengeIsFoughtBesideAPartnerOfTheBattleSalon()
    {
        var host = Challenger();
        host.Story.Set("FLAG_TRAVELED_WITH_CHERYL");
        host.Story.Set("FLAG_RECEIVED_RIOLU_EGG_FROM_RILEY");
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        Play(host, runner, "BattleTower", "MultiAttendant", "multi_attendant");
        Assert.Equal((TowerMode.Multi, 2, 14), (host.Tower!.Mode, host.Tower.Slots.Count, host.Tower.Trainers.Count));
        Assert.Equal((3, 1), (host.Story.Var("VAR_BATTLE_TOWER_ELEVATOR_LOAD_ACTION"), host.Story.Var("VAR_BATTLE_TOWER_BATTLE_SALON_STATE")));
        Play(host, runner, "BattleTowerElevator", "OnEnter");
        Assert.Contains(host.Log, l => l.Contains("warp BattleTowerBattleSalon"));

        // The Battle Salon: those the story has had the player meet, each with two Pokémon to tell of
        Play(host, runner, "BattleTowerBattleSalon", "OnEnter");
        Assert.Equal(new[] { false, true, false, true, true }, BattleTowerChallenge.SalonFlags.Select(host.Story.Has));
        Assert.Equal(5, host.Tower.Candidates.Count);
        host.Transcript.Clear();
        host.Answers.Enqueue(0);
        Play(host, runner, "BattleTowerBattleSalon", "Riley", "riley");
        var riley = host.Tower.Partner!;
        Assert.Equal(2, riley.Id);
        var (species, move) = BattleTowerChallenge.Tells(riley, 0);
        Assert.True(Said(host, $"I have {species}, which knows {move}"));
        Assert.Equal(2, host.Story.Var("VAR_BATTLE_TOWER_ELEVATOR_LOAD_ACTION"));

        Play(host, runner, "BattleTowerElevator", "OnEnter");
        Assert.Contains(host.Log, l => l.Contains("warp BattleTowerCorridorMulti"));
        Play(host, runner, "BattleTowerCorridorMulti", "OnEnter");
        Play(host, runner, "BattleTowerMultiBattleRoom", "OnEnter");
        Assert.Equal(7, host.FrontierBattles.Count);
        foreach (var battle in host.FrontierBattles)
        {
            Assert.True(battle.Doubles);
            Assert.Equal(2, battle.Team.Count);
            Assert.Equal(2, battle.Foes.Count);
            Assert.All(battle.Foes, f => Assert.Equal(2, f.Party.Count));
            Assert.Equal(("frontier_trainer_riley_riley", 2), (battle.Partner!.Id, battle.Partner.Party.Count));
        }
        // The partner's two are the same from battle to battle
        Assert.Single(host.FrontierBattles.Select(b => string.Join(",", b.Partner!.Party.Members.Select(p => p.Species.Name + p.Personality))).Distinct());
        Assert.Equal((7, 3), (host.Frontier.Get(FrontierStat.TowerLatestStreakMulti), host.Frontier.BattlePoints));

        // Nobody chosen: the attendant at the door lets the player out, and the challenge is called off
        host = Challenger();
        runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        Play(host, runner, "BattleTower", "MultiAttendant", "multi_attendant");
        host.Answers.Enqueue(0);
        Play(host, runner, "BattleTowerBattleSalon", "Attendant", "teala");
        Assert.Equal(3, host.Story.Var("VAR_BATTLE_TOWER_LOBBY_LOAD_ACTION"));
        Play(host, runner, "BattleTower", "OnEnter");
        Assert.Null(host.Tower);
    }

    // ------------------------------------------------------------------ the lobby's and the grounds' other people

    [Fact]
    public void TheExchangeServiceCornerTradesBattlePointsForPrizes()
    {
        var grounds = MapDatabase.Get("BattleFrontier");
        var runner = new ScriptRunner(ScriptLibrary.Default, new HeadlessScriptHost());
        HeadlessScriptHost Ask(string who, int points, int answer)
        {
            var host = new HeadlessScriptHost { Map = grounds };
            host.Frontier.GivePoints(points);
            host.Answers.Enqueue(answer);
            runner = new ScriptRunner(ScriptLibrary.Default, host);
            var attendant = grounds.Everyone.First(n => n.Key == who);
            runner.Start(ScriptLibrary.Default.Find(attendant.Script!, "battle_frontier")!, attendant);
            runner.RunToEnd();
            return host;
        }
        // The west booth: the seventh prize is a Power Bracer for 16 points
        var bought = Ask("attendant_southwest", 20, 6);
        Assert.Equal((4, 16, 1), (bought.Frontier.BattlePoints, bought.Frontier.PointsSpent, bought.Bag.GetQuantity(ItemDatabase.Get("Power Bracer")!)));
        Assert.True(Said(bought, "traded 16 BP for the Power Bracer"));
        // Too few points: nothing changes hands
        var poor = Ask("attendant_southwest", 20, 16);
        Assert.Equal((20, 0), (poor.Frontier.BattlePoints, poor.Bag.GetQuantity(ItemDatabase.Get("Bright Powder")!)));
        Assert.True(Said(poor, "don't have the Battle Points"));
        // The east booth: Technical Machines, the last TM26 for 80
        var machine = Ask("attendant_southeast", 100, 14);
        Assert.Equal((20, 1), (machine.Frontier.BattlePoints, machine.Bag.GetQuantity(ItemDatabase.Get("TM26")!)));
        // Backing out
        Assert.Equal(50, Ask("attendant_southeast", 50, 15).Frontier.BattlePoints);
    }

    [Fact]
    public void TheJudgeReadsAPokemonsPotential()
    {
        // JudgeStats: the six IVs added up, and the highest of them
        var host = new HeadlessScriptHost();
        var p = A("Garchomp");
        (p.IvHP, p.IvAttack, p.IvDefense, p.IvSpeed, p.IvSpAttack, p.IvSpDefense) = (31, 20, 20, 31, 10, 9);
        host.Party.Add(p);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        Play(host, runner, "BattleTower", "StatsJudge", "stats_judge");
        // 121: the third rank of four (121 to 150); 31 is the best there is; HP comes before Speed among the highest
        Assert.True(Said(host, "has a great deal in it"));
        Assert.True(Said(host, "What it has most of is HP"));
        Assert.True(Said(host, "nothing to wish for"));
        Assert.True(host.Story.Has("FLAG_TALKED_TO_STATS_JUDGE"));

        (p.IvHP, p.IvAttack, p.IvDefense, p.IvSpeed, p.IvSpAttack, p.IvSpDefense) = (15, 15, 15, 15, 15, 15);
        host.Transcript.Clear();
        Play(host, runner, "BattleTower", "StatsJudge", "stats_judge");
        // 90 and 15: the lowest rank of each
        Assert.True(Said(host, "nothing out of the ordinary"));
        Assert.True(Said(host, "only fair"));
    }

    [Fact]
    public void TealaHandsOverTheTrophyThatWaits()
    {
        var host = new HeadlessScriptHost();
        host.Frontier.Set(FrontierStat.TowerRecordStreakSingle, 56);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        Play(host, runner, "BattleTower", "OnEnter");
        Assert.False(host.Story.Has("FLAG_HIDE_BATTLE_TOWER_TEALA"));
        Play(host, runner, "BattleTower", "Teala", "teala");
        Play(host, runner, "BattleTower", "Teala", "teala");
        Assert.Equal(new[] { "Bronze Trophy", "Silver Trophy" }, host.Underground.PcGoods.Select(UndergroundDatabase.GoodName));
        host.Transcript.Clear();
        Play(host, runner, "BattleTower", "Teala", "teala");
        Assert.True(Said(host, "look after the Tower's trophies"));
        Play(host, runner, "BattleTower", "OnEnter");
        Assert.True(host.Story.Has("FLAG_HIDE_BATTLE_TOWER_TEALA"));
    }
}
