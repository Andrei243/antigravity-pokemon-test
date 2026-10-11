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
/// The Battle Hall (plan 06 · R18c). The numbers are the decompilation's (<c>src/overlay104/battle_hall.c</c>,
/// <c>battle_hall_helpers.c</c>, <c>frscrcmd_battle_hall.c</c>, <c>src/scrcmd_battle_hall.c</c>,
/// <c>src/battle_hall_save.c</c>, <c>src/battle_hall_win_records.c</c>, <c>scripts_battle_hall.s</c>,
/// <c>frontier_scripts_battle_hall.s</c> and the board, <c>src/applications/frontier/battle_hall/main.c</c>).
/// </summary>
[Collection("MapDatabase")]
public class BattleHallTests
{
    private const int Silver = 307, Gold = 308;

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static bool Said(HeadlessScriptHost host, string part) => host.Transcript.Any(l => l.Text.Contains(part));

    /// <summary>The draws a test hands over one after another, and then a generator's.</summary>
    private static Func<int> Draws(int seed, params int[] values)
    {
        int at = 0;
        var random = new Random(seed);
        return () => at < values.Length ? values[at++] : random.Next(0x10000);
    }

    private static Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level);

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    private static int Cell(PokemonType type) => Array.IndexOf(BattleHallChallenge.Board, type);

    // ------------------------------------------------------------------ the tables

    [Fact]
    public void TheHallsTablesAreTheOriginals()
    {
        var hall = FrontierDatabase.Hall;
        // sBattleHallPotentialOpponents: 477 Pokémon, with the archive's empty set before the first
        Assert.Equal(478, hall.Pokemon.Count);
        Assert.Equal(478, hall.Types.Count);
        Assert.Equal("", hall.Pokemon[0].Species);
        Assert.Equal(("Bulbasaur", "Pidgey", "Snover", "Ivysaur", "Raichu", "Blastoise", "Regigigas"),
            (hall.Pokemon[1].Species, hall.Pokemon[8].Species, hall.Pokemon[154].Species, hall.Pokemon[155].Species, hall.Pokemon[271].Species, hall.Pokemon[376].Species, hall.Pokemon[477].Species));
        Assert.Equal(new[] { PokemonType.Grass, PokemonType.Poison }, hall.Types[1]);
        Assert.Equal(new[] { PokemonType.Normal, PokemonType.Flying }, hall.Types[8]);
        // GROUP_1_START to GROUP_4_START
        Assert.Equal(new[] { 1, 155, 271, 376 }, hall.Groups);
        // The only Pokémon of the Frontier in forms: Wormadam's other cloaks, with the types of each
        Assert.Equal(("Wormadam-Sandy", "Wormadam-Trash"), (hall.Pokemon[269].Form, hall.Pokemon[270].Form));
        Assert.Equal(new[] { PokemonType.Bug, PokemonType.Ground }, hall.Types[269]);
        Assert.Equal(new[] { PokemonType.Bug, PokemonType.Steel }, hall.Types[270]);
        Assert.All(hall.Pokemon.Skip(1), set => Assert.True(PokemonDatabase.Get(set.Species) != null && set.Moves.Count is >= 1 and <= 4));
        // Every one of them counts as its own species' types (the sandy Wormadam as Bug and Ground)
        var sandy = FrontierTrainers.Build(hall.Pokemon[269], 1, 8, 40);
        Assert.Equal((PokemonType.Bug, PokemonType.Ground, 40), (sandy.PrimaryType, sandy.SecondaryType, sandy.Level));

        // sHallTrainerClassesByRank, sHallTrainerClassesBySelectedType and the class of every Frontier trainer
        Assert.Equal((10, 18, 315), (hall.ClassesByRank.Count, hall.ClassesByType.Count, hall.TrainerClasses.Count));
        Assert.Equal(new[] { "youngster", "lass", "pokefan_male", "pokefan_female", "collector", "beauty", "poke_kid", "ninja_boy" }, hall.ClassesByRank[0]);
        Assert.Equal(new[] { "ace_trainer_male", "ace_trainer_female", "breeder_male", "breeder_female", "ranger_male", "ranger_female", "pi", "veteran" }, hall.ClassesByRank[9]);
        Assert.Equal(new[] { "tuber_male", "tuber_female", "sailor", "fisherman" }, hall.ClassesByType[Cell(PokemonType.Water)]);
        Assert.Equal(new[] { "dragon_tamer", "dragon_tamer", "dragon_tamer", "dragon_tamer" }, hall.ClassesByType[Cell(PokemonType.Dragon)]);
        Assert.Equal(("youngster", "hall_matron"), (hall.TrainerClasses[0], hall.TrainerClasses[Silver]));
        // Every class the tables name has a trainer of the Frontier
        Assert.All(hall.ClassesByRank.Concat(hall.ClassesByType).SelectMany(row => row), c => Assert.Contains(c, hall.TrainerClasses));

        // sBattleHallAppGridOrder: the seventeen types as the board lays them out
        Assert.Equal(17, BattleHallChallenge.Board.Length);
        Assert.Equal((PokemonType.Normal, PokemonType.Fire, PokemonType.Fighting, PokemonType.Steel),
            (BattleHallChallenge.Board[0], BattleHallChallenge.Board[1], BattleHallChallenge.Board[6], BattleHallChallenge.Board[16]));
        Assert.Equal(17, BattleHallChallenge.Board.Distinct().Count());
        Assert.Equal((Silver, Gold), FrontierBrains.Trainers(FrontierFacility.Hall));
        Assert.Equal((50, 170), FrontierBrains.Battles(FrontierFacility.Hall));
    }

    // ------------------------------------------------------------------ who may go in

    [Fact]
    public void OnePokemonOfLevelThirtyGoesInOrTwoOfOneSpecies()
    {
        // CheckPartyIsBattleHallEligible: no Egg, level 30 or more, not on the banlist
        Assert.True(BattleHallChallenge.MayEnter(Mon("Bidoof", 30)));
        Assert.False(BattleHallChallenge.MayEnter(Mon("Bidoof", 29)));
        Assert.False(BattleHallChallenge.MayEnter(Mon("Dialga", 70)));
        var egg = Mon("Togepi", 30);
        egg.IsEgg = true;
        Assert.False(BattleHallChallenge.MayEnter(egg));

        var party = PartyOf(Mon("Bidoof", 29), Mon("Staraptor", 45), Mon("Bidoof", 40));
        Assert.True(BattleHallChallenge.EnoughToEnter(party, FrontierChallenge.Single));
        // A Double challenge wants two of one species, both allowed: the Bidoof of level 29 isn't
        Assert.False(BattleHallChallenge.EnoughToEnter(party, FrontierChallenge.Double));
        party.Add(Mon("Bidoof", 31));
        Assert.True(BattleHallChallenge.EnoughToEnter(party, FrontierChallenge.Double));
        Assert.True(BattleHallChallenge.ValidEntry(party, new[] { 2, 3 }, FrontierChallenge.Double));
        Assert.False(BattleHallChallenge.ValidEntry(party, new[] { 1, 2 }, FrontierChallenge.Double));
        Assert.False(BattleHallChallenge.ValidEntry(party, new[] { 0, 2 }, FrontierChallenge.Double));
        Assert.True(BattleHallChallenge.ValidEntry(party, new[] { 1 }, FrontierChallenge.Single));
        Assert.False(BattleHallChallenge.ValidEntry(party, new[] { 0 }, FrontierChallenge.Single));
        Assert.Throws<InvalidOperationException>(() => BattleHallChallenge.Begin(new FrontierRecords(), FrontierChallenge.Multi, new[] { 1 }));
    }

    // ------------------------------------------------------------------ the ranks

    [Fact]
    public void ATypesRankIsFourBitsOfTheStatItSharesWithItsNeighbour()
    {
        // BattleHall_SaveTypeRank: the types by their places on the board, two to a stat, the first in the low four bits
        var records = new FrontierRecords();
        BattleHallChallenge.SaveRank(records, FrontierChallenge.Single, 0, 3);
        BattleHallChallenge.SaveRank(records, FrontierChallenge.Single, 1, 7);
        BattleHallChallenge.SaveRank(records, FrontierChallenge.Single, 16, 10);
        Assert.Equal(0x73, records.Get(FrontierStat.HallCurrentRanksNormalFightingSingle));
        Assert.Equal(0x0A, records.Get(FrontierStat.HallCurrentRanksDragonDarkSingle));
        Assert.Equal((3, 7, 0, 10), (BattleHallChallenge.LoadRank(records, FrontierChallenge.Single, 0), BattleHallChallenge.LoadRank(records, FrontierChallenge.Single, 1),
            BattleHallChallenge.LoadRank(records, FrontierChallenge.Single, 2), BattleHallChallenge.LoadRank(records, FrontierChallenge.Single, 16)));
        // A Double challenge's are its own, twelve stats on
        BattleHallChallenge.SaveRank(records, FrontierChallenge.Double, 0, 5);
        Assert.Equal(5, records.Get(FrontierStat.HallCurrentRanksNormalFightingDouble));
        Assert.Equal(0x73, records.Get(FrontierStat.HallCurrentRanksNormalFightingSingle));
        Assert.Equal((FrontierStat.HallCurrentStreakDouble, FrontierStat.HallLatestStreakDouble, FrontierStat.HallLatestSpeciesDouble),
            (BattleHallChallenge.CurrentStreakStat(FrontierChallenge.Double), BattleHallChallenge.LatestStreakStat(FrontierChallenge.Double), BattleHallChallenge.LatestSpeciesStat(FrontierChallenge.Double)));

        // A streak still running takes its ranks up again; one that isn't starts every type afresh
        records.SetRunning(FrontierFacility.Hall, 0, true);
        records.Set(FrontierStat.HallLatestStreakSingle, 20);
        var running = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        Assert.Equal((20, 2, 3, 7, 10), (running.Streak, running.Round, running.Ranks[0], running.Ranks[1], running.Ranks[16]));
        Assert.Equal((4, 8, 10), (running.ShownRank(0), running.ShownRank(1), running.ShownRank(16)));
        Assert.True(running.Beaten(16) && !running.Beaten(1));
        records.SetRunning(FrontierFacility.Hall, 0, false);
        var fresh = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        Assert.Equal((0, 0), (fresh.Streak, fresh.Round));
        Assert.All(fresh.Ranks, r => Assert.Equal(0, r));
        Assert.Equal(0, records.Get(FrontierStat.HallCurrentRanksNormalFightingSingle));
    }

    [Fact]
    public void WhenEveryTypeIsBeatenTenTimesTheyAllGoBackToTheirTenthRank()
    {
        // BattleHall_CapTypeRanks
        var records = new FrontierRecords();
        var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        for (int cell = 0; cell < 16; cell++) challenge.Ranks[cell] = 10;
        challenge.CapRanks();
        Assert.Equal(10, challenge.Ranks[0]);
        challenge.Ranks[16] = 10;
        challenge.CapRanks();
        Assert.All(challenge.Ranks.Take(17), r => Assert.Equal(9, r));
    }

    // ------------------------------------------------------------------ the level

    [Fact]
    public void TheSquareRootIsTheConsolesInFixedPoint()
    {
        // FX_Sqrt(FX32_CONST(n)): the root with twelve bits after the point, rounded to the nearest
        Assert.Equal(40960, BattleHallChallenge.FixedSqrt(100));
        Assert.Equal(28672, BattleHallChallenge.FixedSqrt(49));
        // The root of 50 is 7.0710678, and 4096 times that 28963.09; of 30, 5.4772256 and 22434.72
        Assert.Equal(28963, BattleHallChallenge.FixedSqrt(50));
        Assert.Equal(22435, BattleHallChallenge.FixedSqrt(30));
        Assert.Equal(0, BattleHallChallenge.FixedSqrt(0));
    }

    [Theory]
    // BattleHall_CalcOpponentsLevel, by hand. At level 50 the root is 28963/4096 = 7.07104: the level less three roots
    // is 117911/4096 = 28.787, and a rank is worth 50 / (5 x 7.07104) = 1.41421 levels.
    // Rank 0 with no other type played: 28.787, rounded up
    [InlineData(50, 0, 0, 29)]
    // Rank 9: 28.787 + 12.728 = 41.515
    [InlineData(50, 9, 0, 42)]
    // Rank 0 with three other types won over already: half a level each, 30.287
    [InlineData(50, 0, 3, 31)]
    // Level 100: the root is 10 exactly, so 70 with nothing to round, and a rank is worth two levels
    [InlineData(100, 0, 0, 70)]
    [InlineData(100, 9, 0, 88)]
    [InlineData(100, 9, 16, 96)]
    // Level 30: 55575/4096 = 13.568, a rank worth 30 / 27.3865 = 1.09543: rank 9 comes to 23.427
    [InlineData(30, 9, 0, 24)]
    // ... and with all sixteen other types played, 31.427, which is held to the challenger's own level
    [InlineData(30, 9, 16, 30)]
    public void TheOpponentsLevelGoesByTheChallengersLevelTheRankAndTheTypesPlayed(int level, int rank, int others, int expected)
    {
        var challenge = BattleHallChallenge.Begin(new FrontierRecords(), FrontierChallenge.Single, new[] { 0 });
        var party = PartyOf(Mon("Staraptor", level));
        challenge.Ranks[0] = rank;
        for (int cell = 1; cell <= others; cell++) challenge.Ranks[cell] = 1;
        challenge.Choose(0, party, Draws(1));
        Assert.Equal(expected, challenge.OpponentsLevel);
        Assert.Equal(BattleHallChallenge.FixedSqrt(level), challenge.LevelSqrt);
    }

    // ------------------------------------------------------------------ the opponent

    [Fact]
    public void AClassATrainerAndAPokemonOfTheTypeAreDrawnForEachBattle()
    {
        var records = new FrontierRecords();
        var party = PartyOf(Mon("Staraptor", 50));
        var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        // The class: a roll of twelve, the first eight the rank's classes (0: the first of rank 1's, a Youngster).
        // The trainer: from the place drawn (0) onward, the first of that class: trainer 0, Youngster Jim.
        // The Pokémon: from the place drawn (0, which is number 1 of the first group) onward, the first of the
        // type: Bulbasaur and the six after it are of other types, and number 8 is Pidgey, Normal and Flying.
        challenge.Choose(Cell(PokemonType.Normal), party, Draws(1, 0, 0, 0));
        Assert.Equal((0, 8, 0), (challenge.Trainers[0], challenge.Met[0], challenge.Trainers[1]));
        Assert.Equal(AiFlags.None, challenge.Mind);

        // One time in three the class is one the type brings: a roll of 9 at Water is the second of its four, a Tuber
        // (the girl), and trainer 27 is the first of them
        var water = BattleHallChallenge.PickClasses(Cell(PokemonType.Water), 1, 0, Draws(1, 9));
        Assert.Equal("tuber_female", Assert.Single(water));
        Assert.Equal("ranger_male", Assert.Single(BattleHallChallenge.PickClasses(Cell(PokemonType.Water), 1, 9, Draws(1, 4))));
        Assert.Equal(27, FrontierDatabase.Hall.TrainerClasses.IndexOf("tuber_female"));

        // The Pokémon made for the battle: its set, the level worked out, the IVs of the rank (8 at the first)
        var foe = challenge.MakeOpponent(party, Draws(5));
        var pidgey = Assert.Single(foe.Pokemon);
        Assert.Equal(("Pidgey", 29, 8, 8, false), (pidgey.Species.Name, pidgey.Level, pidgey.IvHP, pidgey.IvSpeed, pidgey.IsShiny));
        Assert.Equal(FrontierDatabase.Hall.Pokemon[8].Moves, pidgey.Moves.Select(m => m.Name));
        Assert.Equal(FrontierDatabase.Hall.Pokemon[8].Nature, pidgey.Nature);
        Assert.Equal(0, foe.Number);
        Assert.Same(foe, challenge.Standing);

        // After a win the type's rank is up by one, and the second battle's trainer is not the first again: the
        // search from place 0 passes Jim by (and the round's unfilled second places hold a nought, which keeps him
        // out too) and takes the next Youngster, trainer 1. The Pokémon met is not met again: from Pidgey's place,
        // Rattata.
        Assert.False(challenge.Won(records));
        Assert.Equal((1, 1, 1, 1), (challenge.Battle, challenge.Streak, challenge.Ranks[0], records.VictoriesAt(FrontierFacility.Hall)));
        Assert.Null(challenge.Standing);
        challenge.Choose(Cell(PokemonType.Normal), party, Draws(1, 0, 0, 7));
        Assert.Equal((1, 9), (challenge.Trainers[2], challenge.Met[2]));
        Assert.Equal(10, challenge.MakeOpponent(party, Draws(6)).Pokemon[0].IvHP);
    }

    [Fact]
    public void AClassWithNoTrainerLeftIsDrawnAgainFromTheRanksOwn()
    {
        // BattleHall_PickOpponentTrainers: three Youngsters stand among the first hundred trainers and a few more
        // further on; once every one of them has stood in the round, the search comes round to where it began and
        // another of the rank's eight classes is drawn in its place
        var records = new FrontierRecords();
        var party = PartyOf(Mon("Staraptor", 50));
        var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        var youngsters = FrontierDatabase.Hall.TrainerClasses.Select((c, i) => (c, i)).Where(p => p.c == "youngster").Select(p => p.i).ToList();
        // Every Youngster but Jim stands in the places of the round already, and Jim is kept out by the nought of an unfilled place
        for (int i = 1; i < youngsters.Count && i * 2 < 18; i++) challenge.Trainers[i * 2 - 2] = youngsters[i];
        Assert.True(youngsters.Count * 2 - 2 <= 18, "more Youngsters than a round has places for");
        // Set the challenge at its last battle but one by winning it there
        for (int n = 0; n < 9; n++) challenge.Won(records);
        // The class roll is the Youngster again; the trainer's place 0; then a class roll of 0 (a Youngster once more,
        // refused) and of 1: a Lass; the first Lass from place 0 is trainer 3
        challenge.Choose(Cell(PokemonType.Fire), party, Draws(1, 0, 0, 0, 1));
        Assert.Equal(3, challenge.Trainers[18]);
        Assert.Equal("lass", FrontierDatabase.Hall.TrainerClasses[3]);
    }

    [Fact]
    public void ThePokemonComeFromTheGroupsOfTheRank()
    {
        // sPotentialMatchupRangesByRank and sMatchupGroupRanges
        Assert.Equal((1, 154), BattleHallChallenge.RangeOfRank(0));
        Assert.Equal((1, 154), BattleHallChallenge.RangeOfRank(1));
        Assert.Equal((1, 270), BattleHallChallenge.RangeOfRank(2));
        Assert.Equal((1, 270), BattleHallChallenge.RangeOfRank(4));
        Assert.Equal((155, 375), BattleHallChallenge.RangeOfRank(5));
        Assert.Equal((155, 375), BattleHallChallenge.RangeOfRank(7));
        Assert.Equal((271, 477), BattleHallChallenge.RangeOfRank(8));
        Assert.Equal((271, 477), BattleHallChallenge.RangeOfRank(10));
        Assert.Equal(new[] { (1, 154), (155, 270), (271, 375), (376, 477) }, Enumerable.Range(0, 4).Select(BattleHallChallenge.RangeOfGroup));
        // The group a species stands in; one the table doesn't hold counts as of the strongest
        Assert.Equal((0, 1, 2, 3, 3, 3), (BattleHallChallenge.GroupOf("Bulbasaur"), BattleHallChallenge.GroupOf("Ivysaur"), BattleHallChallenge.GroupOf("Raichu"),
            BattleHallChallenge.GroupOf("Garchomp"), BattleHallChallenge.GroupOf("Regigigas"), BattleHallChallenge.GroupOf("Arceus")));

        // Whatever is drawn, the Pokémon is of the type, in the rank's range, not the challenger's own species and
        // not one met in the round
        var records = new FrontierRecords();
        var party = PartyOf(Mon("Gastly", 60));
        var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        var draws = Draws(11);
        var met = new List<int>();
        for (int battle = 0; battle < 10; battle++)
        {
            int rank = challenge.Ranks[Cell(PokemonType.Ghost)];
            challenge.Choose(Cell(PokemonType.Ghost), party, draws);
            int number = challenge.Met[battle * 2];
            var (min, max) = BattleHallChallenge.RangeOfRank(rank);
            Assert.InRange(number, min, max);
            Assert.Contains(PokemonType.Ghost, FrontierDatabase.Hall.Types[number]);
            Assert.NotEqual("Gastly", FrontierDatabase.Hall.Pokemon[number].Species);
            Assert.DoesNotContain(number, met);
            met.Add(number);
            // The trainers think harder as the rank goes up: nothing for the first three, the basic routine to the seventh
            Assert.Equal(rank + 1 >= 8 ? FrontierTrainers.Mind : rank + 1 >= 4 ? AiFlags.Basic : AiFlags.None, challenge.Mind);
            Assert.Equal(BattleHallChallenge.IvsByRank[Math.Min(rank, 9)], challenge.MakeOpponent(party, draws).Pokemon[0].IvAttack);
            challenge.Won(records);
        }
        Assert.Equal(10, challenge.Ranks[Cell(PokemonType.Ghost)]);
        Assert.Equal(10, challenge.Trainers.Where((_, i) => i % 2 == 0).Distinct().Count());
        Assert.Equal(new[] { 8, 10, 12, 14, 16, 18, 20, 22, 24, 26 }, BattleHallChallenge.IvsByRank);
    }

    [Fact]
    public void TheWalkStepsOverTheLastNumberOfItsRange()
    {
        // BattleHall_PickNextOpponentPokemon: the place drawn may be the range's last (154, Snover, Grass and Ice), but
        // the walk onward wraps one short of it. Drawn as the start for a type it isn't, it is passed by, and number 1
        // comes next: Bulbasaur, the first Grass type
        var party = PartyOf(Mon("Staraptor", 50));
        var challenge = BattleHallChallenge.Begin(new FrontierRecords(), FrontierChallenge.Single, new[] { 0 });
        challenge.Choose(Cell(PokemonType.Ice), party, Draws(1, 0, 0, 153));
        Assert.Equal(154, challenge.Met[0]);
        challenge = BattleHallChallenge.Begin(new FrontierRecords(), FrontierChallenge.Single, new[] { 0 });
        challenge.Choose(Cell(PokemonType.Poison), party, Draws(1, 0, 0, 153));
        Assert.Equal(1, challenge.Met[0]);
        // From 152 the walk looks at 153 and then, stepping over 154, at 1 again
        challenge = BattleHallChallenge.Begin(new FrontierRecords(), FrontierChallenge.Single, new[] { 0 });
        Assert.DoesNotContain(PokemonType.Poison, FrontierDatabase.Hall.Types[153]);
        challenge.Choose(Cell(PokemonType.Poison), party, Draws(1, 0, 0, 152));
        Assert.Equal(1, challenge.Met[0]);
    }

    [Fact]
    public void InADoubleChallengeTheTrainerHasTheSamePokemonTwice()
    {
        var records = new FrontierRecords();
        var party = PartyOf(Mon("Bidoof", 30), Mon("Bidoof", 44));
        var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Double, new[] { 0, 1 });
        // Two trainers are drawn for a Double battle, though only the first stands: both classes first (a Youngster
        // and a Lass), then a place for each (0, and 3: the first Lass)
        challenge.Choose(Cell(PokemonType.Fire), party, Draws(3, 0, 1, 0, 3, 0));
        Assert.Equal((0, 3), (challenge.Trainers[0], challenge.Trainers[1]));
        Assert.Equal(0, challenge.Met[1]);
        // The level goes by the higher of the two entered
        Assert.Equal(44, challenge.EnteredLevel(party));
        var foe = challenge.MakeOpponent(party, Draws(8));
        Assert.Equal(2, foe.Pokemon.Count);
        Assert.Equal(foe.Pokemon[0].Species.Name, foe.Pokemon[1].Species.Name);
        Assert.Contains(PokemonType.Fire, FrontierDatabase.Hall.Types[challenge.Met[0]]);
        // BattleHall_SetMonsPersonality: the first is one step of 250,025 from what was drawn, the second back where
        // it was, so both have the set's nature
        uint first = foe.Personalities[0], second = foe.Personalities[1];
        Assert.Equal(250025u, first > second ? first - second : second - first);
        Assert.Equal(foe.Pokemon[0].Nature, foe.Pokemon[1].Nature);
        Assert.Equal(FrontierDatabase.Hall.Pokemon[challenge.Met[0]].Nature, foe.Pokemon[0].Nature);
        Assert.Equal(250026u, BattleHallChallenge.Step(1, 0x12345678));
        Assert.Equal(1u, BattleHallChallenge.Step(250026, 0x12345678));
        // The team is the two entered, whole and at their own levels
        party.Members[0].CurrentHP = 1;
        var team = challenge.TeamForBattle(party);
        Assert.Equal(new[] { 30, 44 }, team.Members.Select(p => p.Level));
        Assert.All(team.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
        Assert.Equal(1, party.Members[0].CurrentHP);
    }

    [Fact]
    public void TheHallMatronComesAtBattlesFiftyAndAHundredAndSeventy()
    {
        // A streak of 40 with a Bidoof (of the first group): the fifth round's tenth battle is the fiftieth
        var records = new FrontierRecords();
        records.Set(FrontierStat.HallLatestStreakSingle, 40);
        records.SetRunning(FrontierFacility.Hall, 0, true);
        var party = PartyOf(Mon("Bidoof", 62));
        var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        var draws = Draws(21);
        for (int battle = 0; battle < 9; battle++)
        {
            Assert.Equal(FrontierPrint.None, challenge.ArgentaNext);
            challenge.Choose(battle, party, draws);
            Assert.NotEqual(Silver, challenge.Trainers[battle * 2]);
            challenge.Won(records);
        }
        Assert.Equal((49, FrontierPrint.Silver), (challenge.Streak, challenge.ArgentaNext));
        // Whatever type is chosen, she comes, with a Pokémon of any type out of the challenger's own group, at the
        // challenger's level and with 31 in every stat, thinking with all three routines
        challenge.Choose(Cell(PokemonType.Dragon), party, draws);
        Assert.Equal(Silver, challenge.Trainers[18]);
        Assert.InRange(challenge.Met[18], 1, 154);
        Assert.NotEqual("Bidoof", FrontierDatabase.Hall.Pokemon[challenge.Met[18]].Species);
        Assert.Equal(FrontierTrainers.Mind, challenge.Mind);
        var foe = challenge.MakeOpponent(party, draws);
        Assert.Equal((62, 31, 31), (foe.Pokemon[0].Level, foe.Pokemon[0].IvHP, foe.Pokemon[0].IvSpDefense));
        Assert.Equal(("Hall Matron", "Argenta"), (foe.Record.Class, foe.Record.Name));
        // The win raises the rank of the type chosen all the same, and ends the round with twenty Battle Points
        Assert.True(challenge.Won(records));
        Assert.Equal(1, challenge.Ranks[Cell(PokemonType.Dragon)]);
        challenge.EndRound(records, party);
        Assert.Equal((50, 5, BattleHallChallenge.ArgentaPoints), (challenge.Streak, challenge.Round, challenge.PointsEarned));

        // At the hundred and seventieth her Pokémon is of the strongest group
        records = new FrontierRecords();
        records.Set(FrontierStat.HallLatestStreakSingle, 169);
        records.SetRunning(FrontierFacility.Hall, 0, true);
        challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        Assert.Equal((16, FrontierPrint.Gold), (challenge.Round, challenge.ArgentaNext));
        // Battle is nought here, so the place of the round isn't the tenth: the draw goes by the streak the round and
        // the battle make, 161, and she doesn't come. Nine wins on, she does
        Assert.Equal(0, challenge.Battle);
        records.Set(FrontierStat.HallLatestStreakSingle, 160);
        challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        for (int battle = 0; battle < 9; battle++)
        {
            challenge.Choose(battle, party, draws);
            challenge.Won(records);
        }
        challenge.Choose(9, party, draws);
        Assert.Equal(Gold, challenge.Trainers[18]);
        Assert.InRange(challenge.Met[18], 376, 477);
        // A Double challenge never meets her
        records = new FrontierRecords();
        records.Set(FrontierStat.HallLatestStreakDouble, 49);
        records.SetRunning(FrontierFacility.Hall, 1, true);
        Assert.Equal(FrontierPrint.None, BattleHallChallenge.Begin(records, FrontierChallenge.Double, new[] { 0, 1 }).ArgentaNext);
    }

    // ------------------------------------------------------------------ rounds, records and Battle Points

    private static void WinRound(BattleHallChallenge challenge, FrontierRecords records, Party party, int seed = 3)
    {
        var draws = Draws(seed);
        int cell = 0;
        while (true)
        {
            // The board's own order: once every type has been beaten ten times, all of them open again at their tenth rank
            challenge.CapRanks();
            while (challenge.Beaten(cell)) cell = (cell + 1) % 17;
            challenge.Choose(cell, party, draws);
            if (challenge.Won(records)) break;
            cell = (cell + 1) % 17;
        }
        challenge.EndRound(records, party);
        challenge.UpdateRecord(records);
    }

    [Fact]
    public void RoundsWonEarnBattlePointsAndTheSpeciesKeepsItsRecord()
    {
        var records = new FrontierRecords();
        var party = PartyOf(Mon("Staraptor", 55));
        // sBPPerRoundSolo, by the round the streak goes on to
        int[] expected = { 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 6, 6, 8, 8, 10, 10, 12, 12, 12 };
        for (int round = 0; round < expected.Length; round++)
        {
            var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Double, new[] { 0, 0 });
            Assert.Equal((round * 10, round), (challenge.Streak, challenge.Round));
            WinRound(challenge, records, party, seed: round);
            Assert.Equal(expected[round], challenge.PointsEarned);
            Assert.Equal((round + 1) * 10, records.Get(FrontierStat.HallLatestStreakDouble));
            Assert.Equal((round + 1) * 10, records.Get(FrontierStat.HallCurrentStreakDouble));
            Assert.True(records.IsRunning(FrontierFacility.Hall, 1));
            Assert.Equal((round + 1) * 10, records.Hall.RecordOf(FrontierChallenge.Double, "Staraptor"));
        }
        Assert.Equal(200, records.VictoriesAt(FrontierFacility.Hall));
        Assert.Equal(PokemonDatabase.Get("Staraptor")!.DexNumber, records.Get(FrontierStat.HallLatestSpeciesDouble));
        // The types' ranks carry from round to round: ten wins a round, one type after another
        Assert.Equal("Staraptor", BattleHallChallenge.StreakSpecies(records, FrontierChallenge.Double));
        Assert.Null(BattleHallChallenge.StreakSpecies(records, FrontierChallenge.Single));
        Assert.Equal(0, records.Hall.RecordOf(FrontierChallenge.Single, "Staraptor"));
        Assert.Equal(1, records.Hall.SpeciesWithRecords);
    }

    [Fact]
    public void ALossEndsTheStreakAndTheRanksButNotTheRecord()
    {
        var records = new FrontierRecords();
        var party = PartyOf(Mon("Staraptor", 55));
        var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        WinRound(challenge, records, party);
        challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        var draws = Draws(4);
        for (int battle = 0; battle < 3; battle++)
        {
            challenge.Choose(battle + 10, party, draws);
            challenge.Won(records);
        }
        challenge.Choose(13, party, draws);
        // BattleHall_SaveOnLoss: the streak stands at 13 for the record, every type is back at its first rank, and it runs no more
        challenge.EndLost(records, party);
        Assert.True(challenge.UpdateRecord(records));
        Assert.Equal((13, 13, false, 13), (records.Get(FrontierStat.HallLatestStreakSingle), records.Get(FrontierStat.HallCurrentStreakSingle),
            records.IsRunning(FrontierFacility.Hall, 0), records.Hall.RecordOf(FrontierChallenge.Single, "Staraptor")));
        Assert.All(Enumerable.Range(0, 18), cell => Assert.Equal(0, BattleHallChallenge.LoadRank(records, FrontierChallenge.Single, cell)));
        // The next challenge starts from nothing; a shorter streak leaves the record alone
        challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        Assert.Equal(0, challenge.Streak);
        challenge.Choose(0, party, draws);
        challenge.Won(records);
        challenge.EndLost(records, party);
        Assert.True(challenge.UpdateRecord(records));
        Assert.Equal((1, 13), (records.Get(FrontierStat.HallCurrentStreakSingle), records.Hall.RecordOf(FrontierChallenge.Single, "Staraptor")));

        // Another species has a record of its own, and letting a running streak go (BattleHall_ClearActiveStreak) ends it
        var other = PartyOf(Mon("Luxray", 50));
        challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        WinRound(challenge, records, other);
        Assert.Equal((10, 13, 2), (records.Hall.RecordOf(FrontierChallenge.Single, "Luxray"), records.Hall.RecordOf(FrontierChallenge.Single, "Staraptor"), records.Hall.SpeciesWithRecords));
        Assert.Equal(23, records.Hall.TotalSingles);
        Assert.Equal("Luxray", BattleHallChallenge.StreakSpecies(records, FrontierChallenge.Single));
        BattleHallChallenge.Forfeit(records, FrontierChallenge.Single);
        Assert.Null(BattleHallChallenge.StreakSpecies(records, FrontierChallenge.Single));
        Assert.Equal(0, records.Get(FrontierStat.HallLatestStreakSingle));
    }

    [Fact]
    public void AChallengePutDownIsTakenUpWhereItStoodByTheSamePokemon()
    {
        var records = new FrontierRecords();
        var party = PartyOf(Mon("Staraptor", 55), Mon("Luxray", 50));
        var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        var draws = Draws(9);
        for (int battle = 0; battle < 4; battle++)
        {
            challenge.Choose(battle % 2, party, draws);
            challenge.Won(records);
        }
        var trainers = challenge.Trainers.ToArray();
        var met = challenge.Met.ToArray();
        challenge.Pause(records, party);
        // BH_FUNC_QUICKSAVE: the latest streak and the ranks are written, the streak a record goes by and its running are not
        Assert.Equal((4, 0, false, 0x22), (records.Get(FrontierStat.HallLatestStreakSingle), records.Get(FrontierStat.HallCurrentStreakSingle),
            records.IsRunning(FrontierFacility.Hall, 0), records.Get(FrontierStat.HallCurrentRanksNormalFightingSingle)));
        Assert.True(BattleHallChallenge.StillValid(records, party));
        // It survives a save
        var saved = GameDataFiles.Deserialize<FrontierRecords>(GameDataFiles.Serialize(records));
        var resumed = BattleHallChallenge.Resume(saved)!;
        Assert.Equal((4, 4, 0, true, 2, 2), (resumed.Battle, resumed.Streak, resumed.Round, resumed.Resumed, resumed.Ranks[0], resumed.Ranks[1]));
        Assert.Equal(trainers, resumed.Trainers);
        Assert.Equal(met, resumed.Met);
        Assert.Null(saved.Hall.Paused);
        Assert.Null(BattleHallChallenge.Resume(saved));
        // The type is chosen again, and the challenge is no longer one just taken up
        resumed.Choose(2, party, draws);
        Assert.False(resumed.Resumed);
        Assert.DoesNotContain(resumed.Trainers[8], trainers.Where((_, i) => i % 2 == 0 && i < 8));

        // Only the Pokémon it was entered with takes it up
        (party.Members[0], party.Members[1]) = (party.Members[1], party.Members[0]);
        Assert.False(BattleHallChallenge.StillValid(records, party));
    }

    // ------------------------------------------------------------------ the lobby's people

    [Fact]
    public void TheRecordKeeperHandsOutBattlePointsAsTheTotalPassesItsMarks()
    {
        // sBattleHallRecordMilestones
        Assert.Equal(47, BattleHallChallenge.Milestones.Length);
        Assert.Equal(((10, 1), (30, 3), (500, 10), (100000, 1000)), (BattleHallChallenge.Milestones[0], BattleHallChallenge.Milestones[1], BattleHallChallenge.Milestones[11], BattleHallChallenge.Milestones[46]));
        var records = new FrontierRecords();
        int next = 0;
        // Nothing won yet
        Assert.Equal(HallKeeperAnswer.Nothing, BattleHallChallenge.AskKeeper(records, ref next).Answer);
        // Nine: the first mark is still to come
        records.Hall.Singles["Staraptor"] = 9;
        var told = BattleHallChallenge.AskKeeper(records, ref next);
        Assert.Equal((HallKeeperAnswer.Waiting, 9, 10, 0, 0), (told.Answer, told.Total, told.Coming, told.Points, next));
        // Ten: one Battle Point
        records.Hall.Singles["Staraptor"] = 10;
        told = BattleHallChallenge.AskKeeper(records, ref next);
        Assert.Equal((HallKeeperAnswer.Reward, 10, 10, 30, 1, 1), (told.Answer, told.Total, told.Passed, told.Coming, told.Points, next));
        Assert.Equal(1, records.BattlePoints);
        // A hundred between two species passes three marks at once: 3 + 5 + 5
        records.Hall.Singles["Luxray"] = 90;
        told = BattleHallChallenge.AskKeeper(records, ref next);
        Assert.Equal((HallKeeperAnswer.Reward, 100, 100, 150, 13, 4), (told.Answer, told.Total, told.Passed, told.Coming, told.Points, next));
        Assert.Equal((14, 14), (records.BattlePoints, records.PointsReceived));
        // Asked again, nothing more until the next mark; Double records don't count
        records.Hall.Doubles["Luxray"] = 500;
        told = BattleHallChallenge.AskKeeper(records, ref next);
        Assert.Equal((HallKeeperAnswer.Waiting, 100, 150, 0), (told.Answer, told.Total, told.Coming, told.Points));
        // Every mark passed
        next = BattleHallChallenge.Milestones.Length;
        Assert.Equal(HallKeeperAnswer.AllPassed, BattleHallChallenge.AskKeeper(records, ref next).Answer);
    }

    [Fact]
    public void TheFanComesNearerAndTheVisitorsComeWithTheRecords()
    {
        var records = new FrontierRecords();
        Assert.Equal((0, 0, 0), (BattleHallChallenge.FanStage(records), BattleHallChallenge.FanTalk(records), BattleHallChallenge.Visitors(records)));
        var species = PokemonDatabase.GetAll().Select(s => s.Name).OrderBy(n => n).ToList();
        void Have(int count)
        {
            records.Hall.Singles.Clear();
            foreach (string name in species.Take(count)) records.Hall.Singles[name] = 1;
        }
        // BattleHall_OnResume and the fan's ten things to say
        foreach (var (count, stage, talk) in new[] { (1, 0, 1), (10, 0, 1), (11, 1, 2), (31, 1, 3), (51, 1, 4), (100, 1, 4), (101, 2, 5), (151, 2, 6), (251, 2, 7), (351, 2, 8), (475, 2, 9) })
        {
            Have(count);
            Assert.Equal((stage, talk), (BattleHallChallenge.FanStage(records), BattleHallChallenge.FanTalk(records)));
        }
        // A species with only a Double record counts too
        records.Hall.Singles.Clear();
        records.Hall.Doubles["Bidoof"] = 3;
        Assert.Equal(1, records.Hall.SpeciesWithRecords);
        // BattleHall_PickMajorNPC: by the Single records added up
        foreach (var (total, visitors) in new[] { (499, 0), (500, 1), (999, 1), (1000, 2), (9999, 2), (10000, 4), (50000, 4) })
        {
            records.Hall.Singles.Clear();
            for (int left = total, n = 0; left > 0; left -= 9999, n++) records.Hall.Singles[species[n]] = Math.Min(left, 9999);
            Assert.Equal(visitors, BattleHallChallenge.Visitors(records));
        }
    }

    // ------------------------------------------------------------------ the board

    [Fact]
    public void TheBoardsCursorGoesRoundAndTheEnteredPokemonTakesTwoPlaces()
    {
        var records = new FrontierRecords();
        var party = PartyOf(Mon("Staraptor", 55));
        var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        var screen = new HallBoardScreen();
        screen.Open(challenge, party.Members);
        Assert.Equal((true, HallBoardStep.Pick, 0, false), (screen.IsActive, screen.Step, screen.Cursor, screen.MatronNext));
        // UpdateCursorPosition: off the left of a row comes round to its right, and off the top to the bottom
        screen.Move(-1, 0);
        Assert.Equal(3, screen.Cursor);
        screen.Move(1, 0);
        Assert.Equal(0, screen.Cursor);
        screen.Move(0, -1);
        Assert.Equal(16, screen.Cursor);
        // Right of Steel are the Pokémon's two places, which are one: right again is the Hall Matron's
        screen.Move(1, 0);
        Assert.True(HallBoardScreen.IsSummary(screen.Cursor));
        screen.Move(1, 0);
        Assert.Equal(19, screen.Cursor);
        screen.Move(-1, 0);
        Assert.True(HallBoardScreen.IsSummary(screen.Cursor));
        // Up from there goes back by the side it was come to from: from the right, to Dragon; from Steel, to Ghost
        screen.Move(0, -1);
        Assert.Equal(14, screen.Cursor);
        screen.Move(0, 1);
        Assert.Equal(18, screen.Cursor);
        screen.Move(-1, 0);
        Assert.Equal(16, screen.Cursor);
        screen.Move(1, 0);
        screen.Move(0, -1);
        Assert.Equal(13, screen.Cursor);
        // Down from the top row's second place is the Pokémon too, and down again the top row
        screen.Move(0, -1);
        screen.Move(0, -1);
        screen.Move(0, -1);
        Assert.Equal(1, screen.Cursor);
        screen.Move(0, -1);
        Assert.Equal(17, screen.Cursor);
        screen.Move(0, 1);
        Assert.Equal(1, screen.Cursor);

        // The Pokémon's place opens its summary; any button comes back
        screen.Move(0, -1);
        screen.Confirm();
        Assert.Equal(HallBoardStep.Summary, screen.Step);
        screen.Cancel();
        Assert.Equal(HallBoardStep.Pick, screen.Step);
        // The Hall Matron's place can't be chosen, nor a type beaten ten times; and there is no way out without a type
        screen.Move(1, 0);
        Assert.Equal(19, screen.Cursor);
        screen.Confirm();
        Assert.Equal(HallBoardStep.Pick, screen.Step);
        challenge.Ranks[19 - 16] = 10;
        screen.Move(0, 1);
        Assert.Equal(3, screen.Cursor);
        Assert.False(screen.CanChoose(3));
        screen.Confirm();
        screen.Cancel();
        Assert.Equal((true, HallBoardStep.Pick), (screen.IsActive, screen.Step));
        // A type: asked, refused, asked again and agreed to
        screen.Move(-1, 0);
        screen.Confirm();
        Assert.Equal((HallBoardStep.Confirm, "Battle a Water-type Pokémon?"), (screen.Step, screen.Prompt));
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal((HallBoardStep.Pick, -1), (screen.Step, screen.Outcome));
        screen.Confirm();
        screen.Confirm();
        Assert.Equal((false, 2), (screen.IsActive, screen.Outcome));
        // Opened again, the cursor is on the type last chosen
        challenge.Choose(2, party, Draws(2));
        screen.Open(challenge, party.Members);
        Assert.Equal(2, screen.Cursor);
    }

    [Fact]
    public void BeforeTheHallMatronEveryTypeGoesDarkAndOnlyHerPlaceIsLeft()
    {
        var records = new FrontierRecords();
        records.Set(FrontierStat.HallLatestStreakSingle, 49);
        records.SetRunning(FrontierFacility.Hall, 0, true);
        var party = PartyOf(Mon("Staraptor", 55));
        var challenge = BattleHallChallenge.Begin(records, FrontierChallenge.Single, new[] { 0 });
        var screen = new HallBoardScreen();
        screen.Open(challenge, party.Members);
        Assert.True(screen.MatronNext && !screen.MatronOpen);
        // A type is chosen and agreed to as ever
        screen.Move(1, 0);
        screen.Confirm();
        screen.Confirm();
        // SELECT_STATE_PRINT_MATRON_MSG: she is announced, and then only her place can be chosen
        Assert.Equal((HallBoardStep.MatronNotice, true, 1), (screen.Step, screen.IsActive, screen.Saved));
        screen.Confirm();
        Assert.Equal((HallBoardStep.Pick, true, 19), (screen.Step, screen.MatronOpen, screen.Cursor));
        Assert.False(screen.CanChoose(1));
        Assert.True(screen.CanChoose(19));
        screen.Move(-1, 0);
        screen.Move(-1, 0);
        Assert.Equal(16, screen.Cursor);
        screen.Confirm();
        Assert.Equal(HallBoardStep.Pick, screen.Step);
        screen.Move(1, 0);
        screen.Move(1, 0);
        screen.Confirm();
        Assert.Equal((HallBoardStep.Confirm, "Take on the Hall Matron?"), (screen.Step, screen.Prompt));
        screen.Confirm();
        // What comes back is the type chosen first: the one whose rank the win will raise
        Assert.Equal((false, 1), (screen.IsActive, screen.Outcome));
    }

    // ------------------------------------------------------------------ the rooms

    [Fact]
    public void TheHallsRoomsAreLaidOutAsTheOriginalHasThem()
    {
        // The lobby (events_battle_hall, land data 322): come into from the east, three tiles of mat leading back out
        // onto the grounds before the Hall's porch
        var lobby = Room("BattleHall");
        Assert.Equal((27, 14, true, InteriorStyle.GameHall), (lobby.Width, lobby.Height, lobby.IsIndoors, lobby.Interior));
        var out_ = lobby.Warps.Where(w => w.TargetMap == "BattleFrontier").ToList();
        Assert.Equal(new[] { (26, 8), (26, 9), (26, 10) }, out_.Select(w => (w.SourceX, w.SourceY)));
        Assert.All(out_, w => Assert.Equal((24, 34, Direction.Right), (w.TargetX, w.TargetY, w.TargetFacing)));
        // The original comes down three flights of stairs here; the floor is level in this game (the rulings)
        Assert.Equal(lobby.HeightAt(24, 9), lobby.HeightAt(2, 5));
        // The counter along the eighth row with the records' machine at its end and the keeper beside it; the two
        // attendants in the gap under it, and the way down behind them, which nobody walks to past them
        Assert.All(Enumerable.Range(1, 7), x => Assert.True(lobby.IsSolid(x, 8)));
        Assert.Equal("RecordsMachine", lobby.SignScripts[(7, 8)]);
        Assert.True(lobby.IsSolid(6, 11) && lobby.IsSolid(6, 12) && !lobby.IsSolid(6, 9) && !lobby.IsSolid(6, 10));
        var down = Assert.Single(lobby.Warps, w => w.TargetMap == "BattleHallCorridor");
        Assert.Equal((3, 13, 4, 3), (down.SourceX, down.SourceY, down.TargetX, down.TargetY));
        Assert.Equal(new[] { "MultiAttendant", "SingleAttendant" }, lobby.Everyone.Where(n => n.Key?.EndsWith("attendant") == true).Select(n => n.Script).OrderBy(s => s));
        // The original's fifteen people less the reporter, the visitor four times over, and the PC
        Assert.Equal(18, lobby.Everyone.Count());
        Assert.All(lobby.Everyone.Where(n => !n.IsPCTerminal), n => Assert.NotNull(ScriptLibrary.Default.Find(n.Script!, "BattleHall")));
        Assert.Equal(4, lobby.Everyone.Count(n => n.ShownBy != null && n.GridX == 4 && n.GridY == 6));
        // The seats are walked onto
        Assert.Equal(6, lobby.Props.Count(p => p.Type == PropType.Cushion));
        Assert.True(lobby.IsWalkable(5, 6));

        // The passage and the stage, laid out from the scene's own places
        var corridor = Room("BattleHallCorridor");
        var attendant = Assert.Single(corridor.Everyone);
        Assert.Equal((4, 5, Direction.Down), (attendant.GridX, attendant.GridY, attendant.Facing));
        Assert.Equal(new[] { "BattleHall", "BattleHallStage" }, corridor.Warps.Select(w => w.TargetMap).OrderBy(n => n));
        var stage = Room("BattleHallStage");
        var opponent = stage.Everyone.Single(n => n.Key == "opponent");
        Assert.Equal("FLAG_HIDE_BATTLE_HALL_STAGE_OPPONENT", opponent.HiddenBy);
        Assert.Equal((3, 10, Direction.Right), stage.Everyone.Where(n => n.Key == "announcer").Select(n => (n.GridX, n.GridY, n.Facing)).Single());
        // The challenger's and the trainer's marks, either end of the stage
        Assert.True(stage.IsWalkable(6, 10) && stage.IsWalkable(10, 10));
        // The crowd: four always, four more from the third round and four from the sixth
        var crowd = stage.Everyone.Where(n => n.Key?.StartsWith("spectator") == true).ToList();
        Assert.Equal((12, 4, 4, 4), (crowd.Count, crowd.Count(n => n.HiddenBy == null), crowd.Count(n => n.HiddenBy == "FLAG_HIDE_BATTLE_HALL_CROWD_2"),
            crowd.Count(n => n.HiddenBy == "FLAG_HIDE_BATTLE_HALL_CROWD_3")));
        Assert.All(RoomPlanTests.BattleHallAndCastle.Take(3), name => Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id));
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
        for (int i = 0; i < 40; i++)
        {
            string next = LastWarp(host);
            Play(host, runner, next, "OnEnter");
            if (next == "BattleHall") return;
        }
        throw new InvalidOperationException("The challenge never came back to the lobby");
    }

    private static HeadlessScriptHost Host(params Pokemon[] team)
    {
        var host = new HeadlessScriptHost();
        foreach (var p in team) host.Party.Add(p);
        return host;
    }

    /// <summary>The counter's talk: the challenge by its place in the menu, and yes to whatever is asked.</summary>
    private static void Take(HeadlessScriptHost host, ScriptRunner runner, int way = 0)
    {
        host.Answers.Enqueue(way);
        if (host.Map?.Name != "BattleHall") host.Map = Room("BattleHall");
        host.PlayerTile = (7, 9);
        Play(host, runner, "BattleHall", "SingleAttendant", "single_attendant");
    }

    [Fact]
    public void ASingleChallengeIsPlayedFromTheCounterToTheBattlePoints()
    {
        var host = Host(Mon("Bidoof", 20), Mon("Garchomp", 60));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        Take(host, runner);

        // The counter: the one Pokémon that may go in is entered, the challenge begun, the game saved, the way down
        var hall = host.Hall!;
        Assert.Equal((FrontierChallenge.Single, 0), (hall.Way, hall.Battle));
        Assert.Equal(new[] { 1 }, hall.Slots);
        Assert.Contains("frontier enterhall 1", host.Log);
        Assert.Contains("frontier save 0", host.Log);
        Assert.Contains($"frontier journal {(int)FrontierFacility.Hall}", host.Log);
        Assert.Equal((255, 0, 1), (host.Story.Var("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION"), host.Story.Var(BattleHallChallenge.WayVariable), host.Frontier.ChallengesStarted));
        Assert.True(host.Story.Has("FLAG_HIDE_BATTLE_HALL_CROWD_2") && host.Story.Has("FLAG_HIDE_BATTLE_HALL_CROWD_3"));
        Assert.Equal("BattleHallCorridor", LastWarp(host));

        PlayOn(host, runner);
        // Ten battles on the stage, one Pokémon against one, each against a Pokémon of the type the board gave:
        // the first not yet beaten, so Normal ten times, its rank up with every win
        Assert.Equal(10, host.FrontierBattles.Count);
        Assert.Equal(10, host.Log.Count(l => l == "frontier board 0"));
        Assert.All(host.FrontierBattles, b => Assert.Equal((1, 1, false, "Garchomp", 60), (b.Team.Count, b.Foes.Single().Party.Count, b.Doubles, b.Team.Members[0].Species.Name, b.Team.Members[0].Level)));
        Assert.All(host.FrontierBattles, b => Assert.True(b.Foes[0].Party.Members[0].HasType(PokemonType.Normal)));
        Assert.Equal(10, host.FrontierBattles.Select(b => b.Foes[0].Party.Members[0].Species.Name).Distinct().Count());
        Assert.Equal(10, host.FrontierBattles.Select(b => b.Foes[0].Id).Distinct().Count());
        // The levels climb with the rank and never pass the challenger's; the minds sharpen
        var levels = host.FrontierBattles.Select(b => b.Foes[0].Party.Members[0].Level).ToList();
        Assert.Equal(levels.OrderBy(l => l), levels);
        Assert.True(levels[0] < levels[9] && levels[9] <= 60);
        Assert.Equal(new[] { AiFlags.None, AiFlags.None, AiFlags.None, AiFlags.Basic, AiFlags.Basic, AiFlags.Basic, AiFlags.Basic, FrontierTrainers.Mind, FrontierTrainers.Mind, FrontierTrainers.Mind },
            host.FrontierBattles.Select(b => b.Foes[0].Ai));
        Assert.True(Said(host, "The Normal type, at rank 1!"));
        Assert.True(Said(host, "The Normal type, at rank 10!"));
        // Between battles: restored nine times
        Assert.Equal(9, host.Transcript.Count(l => l.Text.Contains("Garchomp has been restored")));
        // The records, the species' own, the score, the Battle Point and the lobby
        Assert.Equal((10, 10, true, 10), (host.Frontier.Get(FrontierStat.HallLatestStreakSingle), host.Frontier.Get(FrontierStat.HallCurrentStreakSingle),
            host.Frontier.IsRunning(FrontierFacility.Hall, 0), host.Frontier.Hall.RecordOf(FrontierChallenge.Single, "Garchomp")));
        Assert.Equal((1, 10, BattleHallChallenge.RoundScore), (host.Frontier.BattlePoints, host.Frontier.VictoriesAt(FrontierFacility.Hall), host.FrontierScore));
        Assert.True(Said(host, "received 1 BP"));
        Assert.True(Said(host, "all ten"));
        Assert.Null(host.Hall);
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION"));
        Assert.Equal(2, host.Log.Count(l => l == "frontier save 0"));
        // The player's own Pokémon is as it was
        Assert.Equal(60, host.Party.Members[1].Level);

        // The next challenge with it takes the streak up: the second round, Normal beaten, so the board gives Fire
        host.FrontierBattles.Clear();
        host.Transcript.Clear();
        Take(host, runner);
        Assert.Equal((10, 1, 10), (host.Hall!.Streak, host.Hall.Round, host.Hall.Ranks[0]));
        Assert.False(Said(host, "still running"));
        Play(host, runner, "BattleHallCorridor", "OnEnter");
        host.Decide = (asked, _) => asked.Contains("Are you ready?") ? 2 : 0;
        Play(host, runner, "BattleHallStage", "OnEnter");
        Assert.True(Said(host, "The Fire type, at rank 1!"));
        Assert.True(host.FrontierBattles[0].Foes[0].Party.Members[0].HasType(PokemonType.Fire));
        // Given up after one win: the record stands at eleven, and the streak is over
        Assert.Equal((11, 11, false), (host.Frontier.Hall.RecordOf(FrontierChallenge.Single, "Garchomp"), host.Frontier.Get(FrontierStat.HallLatestStreakSingle), host.Frontier.IsRunning(FrontierFacility.Hall, 0)));
    }

    [Fact]
    public void AnotherSpeciesIsAskedAboutBeforeItEndsTheStreak()
    {
        var host = Host(Mon("Garchomp", 60), Mon("Luxray", 50));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Frontier.Set(FrontierStat.HallLatestStreakSingle, 20);
        host.Frontier.Set(FrontierStat.HallLatestSpeciesSingle, PokemonDatabase.Get("Garchomp")!.DexNumber);
        host.Frontier.SetRunning(FrontierFacility.Hall, 0, true);
        // Luxray is entered, and the question is answered no
        host.Entering.Enqueue(new[] { 1 });
        host.Decide = (asked, _) => asked.Contains("still running") ? 1 : 0;
        Take(host, runner);
        Assert.Contains(host.Asked, a => a.Question.Contains("Your streak with Garchomp is still running, and entering Luxray will end it"));
        Assert.Null(host.Hall);
        Assert.True(host.Frontier.IsRunning(FrontierFacility.Hall, 0));
        // Answered yes, the streak is let go of and Luxray's begins at nothing
        host.Entering.Enqueue(new[] { 1 });
        host.Decide = null;
        Take(host, runner);
        Assert.Equal((0, 0), (host.Hall!.Streak, host.Hall.Round));
        Assert.Equal(new[] { 1 }, host.Hall.Slots);
        // Garchomp again finds its streak as it was only if nothing ended it: here it is gone
        Assert.Equal(0, host.Frontier.Get(FrontierStat.HallLatestStreakSingle));
    }

    [Fact]
    public void ADoubleChallengeWantsTwoOfOneSpecies()
    {
        var host = Host(Mon("Garchomp", 60), Mon("Bidoof", 30), Mon("Bidoof", 35));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // Two of different species are turned away
        host.Entering.Enqueue(new[] { 0, 1 });
        Take(host, runner, way: 1);
        Assert.True(Said(host, "must be of the same kind"));
        Assert.Null(host.Hall);
        // The two Bidoof go in, and every battle is two against the same Pokémon twice
        Take(host, runner, way: 1);
        Assert.Equal(FrontierChallenge.Double, host.Hall!.Way);
        Assert.Equal(new[] { 1, 2 }, host.Hall.Slots);
        Assert.Equal(1, host.Story.Var(BattleHallChallenge.WayVariable));
        PlayOn(host, runner);
        Assert.Equal(10, host.FrontierBattles.Count);
        Assert.All(host.FrontierBattles, b => Assert.Equal((2, 2, true), (b.Team.Count, b.Foes.Single().Party.Count, b.Doubles)));
        Assert.All(host.FrontierBattles, b => Assert.Equal(b.Foes[0].Party.Members[0].Species.Name, b.Foes[0].Party.Members[1].Species.Name));
        Assert.All(host.FrontierBattles, b => Assert.True(b.Foes[0].Party.Members[0].Level <= 35));
        Assert.Equal((10, 10, 0), (host.Frontier.Get(FrontierStat.HallLatestStreakDouble), host.Frontier.Hall.RecordOf(FrontierChallenge.Double, "Bidoof"), host.Frontier.Hall.RecordOf(FrontierChallenge.Single, "Bidoof")));
        // With nobody to enter, the counter says what is needed
        var alone = Host(Mon("Garchomp", 60));
        var second = new ScriptRunner(ScriptLibrary.Default, alone);
        Take(alone, second, way: 1);
        Assert.True(Said(alone, "two Pokémon of the same kind"));
        var small = Host(Mon("Bidoof", 12));
        Take(small, new ScriptRunner(ScriptLibrary.Default, small));
        Assert.True(Said(small, "level 30 or higher"));
    }

    [Fact]
    public void TheHallMatronIsMetAtTheFiftiethBattleAndLeavesHerPrint()
    {
        var host = Host(Mon("Garchomp", 60));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // Four rounds won already with it
        host.Frontier.Set(FrontierStat.HallLatestStreakSingle, 40);
        host.Frontier.Set(FrontierStat.HallLatestSpeciesSingle, PokemonDatabase.Get("Garchomp")!.DexNumber);
        host.Frontier.SetRunning(FrontierFacility.Hall, 0, true);
        Take(host, runner);
        // The crowd of a streak past its second round, and not yet past its fifth
        Assert.True(!host.Story.Has("FLAG_HIDE_BATTLE_HALL_CROWD_2") && host.Story.Has("FLAG_HIDE_BATTLE_HALL_CROWD_3"));
        PlayOn(host, runner);

        Assert.Equal(10, host.FrontierBattles.Count);
        var last = host.FrontierBattles[9].Foes[0];
        Assert.Equal(("frontier_hall_matron_argenta_silver", "Hall Matron", FrontierTrainers.Mind), (last.Id, last.TrainerClass, last.Ai));
        // Her Pokémon is of Garchomp's own group, the strongest, at its level, with 31 in every stat
        var hers = last.Party.Members.Single();
        Assert.Equal((60, 31), (hers.Level, hers.IvSpeed));
        Assert.InRange(FrontierDatabase.Hall.Pokemon.FindIndex(1, p => p.Species == hers.Species.Name), 376, 477);
        Assert.True(Said(host, "look who is coming down to the stage"));
        Assert.True(Said(host, "I am Argenta, the Hall Matron"));
        Assert.Equal(1, host.Frontier.BrainsMet);
        // Twenty Battle Points, the silver print handed over in the lobby, and a streak of 50 running
        Assert.True(Said(host, "received 20 BP"));
        Assert.True(Said(host, "Silver Print"));
        Assert.Equal((2, FrontierPrint.Silver, 20, 50, 50), (host.Story.Var("VAR_BATTLE_HALL_PRINT_STATE"), FrontierBrains.PrintOf(host.Story, FrontierFacility.Hall),
            host.Frontier.BattlePoints, host.Frontier.Get(FrontierStat.HallLatestStreakSingle), host.Frontier.Hall.RecordOf(FrontierChallenge.Single, "Garchomp")));
        Assert.True(FrontierBrains.AnyPrint(host.Story));
        var board = FrontierBoard.For(FrontierFacility.Hall, host.Frontier, host.Story);
        Assert.Equal(FrontierPrint.Silver, board.Print);
    }

    [Fact]
    public void AChallengeIsPutDownTakenUpAgainAndGivenUp()
    {
        var host = Host(Mon("Garchomp", 60));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // The stage's menu after the third battle: REST, and yes; the lobby's question: not now
        int menus = 0;
        host.Decide = (asked, _) => asked.Contains("Are you ready?") ? (++menus == 3 ? 1 : 0) : asked.Contains("go on with it now") ? 1 : 0;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.Null(host.Hall);
        var paused = host.Frontier.Hall.Paused!;
        Assert.Equal((3, "Garchomp", 3), (paused.Battle, paused.Species, host.Frontier.Get(FrontierStat.HallLatestStreakSingle)));
        Assert.Equal((2, 0, 0), (host.Story.Var("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION"), host.Frontier.Get(FrontierStat.HallCurrentStreakSingle), host.Frontier.Hall.RecordOf(FrontierChallenge.Single, "Garchomp")));
        Assert.True(Said(host, "Speak to me whenever you are ready"));

        // Spoken to again, she takes it up: the menu comes first; this time RETIRE
        host.Decide = (asked, _) => asked.Contains("Are you ready?") ? 2 : 0;
        host.Transcript.Clear();
        Play(host, runner, "BattleHall", "SingleAttendant", "single_attendant");
        Assert.Equal((true, 3, 3), (host.Hall!.Resumed, host.Hall.Battle, host.Hall.Ranks[0]));
        Assert.Equal("BattleHallCorridor", LastWarp(host));
        PlayOn(host, runner);
        Assert.Equal(3, host.FrontierBattles.Count);
        Assert.True(Said(host, "Thank you for taking the challenge"));
        Assert.Equal((3, 3, false, 0), (host.Frontier.Hall.RecordOf(FrontierChallenge.Single, "Garchomp"), host.Frontier.Get(FrontierStat.HallCurrentStreakSingle),
            host.Frontier.IsRunning(FrontierFacility.Hall, 0), host.Frontier.BattlePoints));
        Assert.Null(host.Hall);
        Assert.Null(host.Frontier.Hall.Paused);

        // A challenge put down whose Pokémon is no longer in its place can't be taken up
        host.Decide = (asked, _) => asked.Contains("Are you ready?") ? 1 : asked.Contains("go on with it now") ? 1 : 0;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.NotNull(host.Frontier.Hall.Paused);
        host.Party.Members[0] = Mon("Luxray", 50);
        host.Decide = null;
        host.Transcript.Clear();
        Play(host, runner, "BattleHall", "SingleAttendant", "single_attendant");
        Assert.True(Said(host, "can't be taken up again"));
        Assert.Null(host.Hall);
        Assert.Null(host.Frontier.Hall.Paused);
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION"));
    }

    [Fact]
    public void ABattleLostEndsTheChallengeAndAGameClosedInTheMiddleEndsTheStreak()
    {
        var host = Host(Mon("Garchomp", 60));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        int fought = 0;
        host.Fight = _ => ++fought == 5 ? BattleOutcome.Lost : BattleOutcome.Won;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.Equal(5, host.FrontierBattles.Count);
        Assert.Equal((4, 4, false, 4, 0), (host.Frontier.Get(FrontierStat.HallLatestStreakSingle), host.Frontier.Hall.RecordOf(FrontierChallenge.Single, "Garchomp"),
            host.Frontier.IsRunning(FrontierFacility.Hall, 0), host.Frontier.VictoriesAt(FrontierFacility.Hall), host.Frontier.BattlePoints));
        Assert.True(Said(host, "Thank you for taking the challenge"));
        Assert.Null(host.Hall);
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION"));

        // A challenge under way when the game was closed: the lobby finds its mark, and the streak is over
        host.Fight = null;
        Take(host, runner);
        PlayOn(host, runner);
        Assert.True(host.Frontier.IsRunning(FrontierFacility.Hall, 0));
        Take(host, runner);
        Assert.Equal(255, host.Story.Var("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION"));
        host.Hall = null;
        host.Transcript.Clear();
        Play(host, runner, "BattleHall", "OnEnter");
        Assert.True(Said(host, "left before it was over"));
        Assert.Equal((false, 0, 0), (host.Frontier.IsRunning(FrontierFacility.Hall, 0), host.Frontier.Get(FrontierStat.HallLatestStreakSingle), host.Story.Var("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION")));
        // The record it had made stays
        Assert.Equal(10, host.Frontier.Hall.RecordOf(FrontierChallenge.Single, "Garchomp"));
    }

    [Fact]
    public void TheLobbysPeopleFollowTheRecords()
    {
        var host = Host(Mon("Garchomp", 60));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // The record keeper: her explanation the first time, and the count after
        Play(host, runner, "BattleHall", "RecordKeeper", "record_keeper");
        Assert.True(Said(host, "I keep the Hall's records"));
        Assert.True(host.Story.Has("FLAG_SPOKEN_TO_BATTLE_HALL_RECORD_KEEPER"));
        host.Frontier.Hall.Singles["Garchomp"] = 34;
        host.Transcript.Clear();
        Play(host, runner, "BattleHall", "RecordKeeper", "record_keeper");
        Assert.True(Said(host, "your records add up to 34 now"));
        Assert.True(Said(host, "past the mark of 30"));
        Assert.True(Said(host, "received 4 BP"));
        Assert.Equal((4, 2), (host.Frontier.BattlePoints, host.Story.Var(BattleHallChallenge.MilestoneVariable)));
        host.Transcript.Clear();
        Play(host, runner, "BattleHall", "RecordKeeper", "record_keeper");
        Assert.True(Said(host, "When they reach 50"));
        Assert.Equal(4, host.Frontier.BattlePoints);

        // The machine, the fan and the fan's place
        Play(host, runner, "BattleHall", "RecordsMachine");
        Assert.Contains($"frontier records {(int)FrontierFacility.Hall}", host.Log);
        Play(host, runner, "BattleHall", "Fan", "fan");
        Assert.True(Said(host, "You've got a record now"));
        foreach (string name in PokemonDatabase.GetAll().Select(s => s.Name).Take(11)) host.Frontier.Hall.Singles[name] = 1;
        Play(host, runner, "BattleHall", "OnEnter");
        var fan = host.Map!.Everyone.First(n => n.Key == "fan");
        Assert.Equal((13, 11, Direction.Left), (fan.GridX, fan.GridY, fan.Facing));

        // The visitors: back from a round won with every species' record adding up to 500, a roll under 30 brings the
        // first of them, once a day; any other roll, or a lower total, nobody
        host.Frontier.Hall.Singles["Garchomp"] = 600;
        host.Story.SetVar("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION", 1);
        host.Rolls.Enqueue(29);
        Play(host, runner, "BattleHall", "OnEnter");
        Assert.True(host.Story.Has("FLAG_BATTLE_HALL_VISITOR_0") && host.Story.Has("FLAG_DAILY_SET_BATTLE_HALL_MAJOR_NPC") && !host.Story.Has("FLAG_HIDE_BATTLE_HALL_MAJOR_NPC"));
        Assert.Equal(0, host.Story.Var("VAR_BATTLE_HALL_MAJOR_NPC"));
        // The next round won that day: whoever was there has gone, and nobody else comes
        host.Story.SetVar("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION", 1);
        host.Rolls.Enqueue(5);
        Play(host, runner, "BattleHall", "OnEnter");
        Assert.True(!host.Story.Has("FLAG_BATTLE_HALL_VISITOR_0") && host.Story.Has("FLAG_HIDE_BATTLE_HALL_MAJOR_NPC"));
        // Another day, with ten thousand: one of the four, here the last
        host.Story.Unset("FLAG_DAILY_SET_BATTLE_HALL_MAJOR_NPC");
        foreach (string name in PokemonDatabase.GetAll().Select(s => s.Name).Take(2)) host.Frontier.Hall.Singles[name] = 9999;
        host.Story.SetVar("VAR_BATTLE_HALL_LOBBY_LOAD_ACTION", 1);
        host.Rolls.Enqueue(0);
        host.Rolls.Enqueue(3);
        Play(host, runner, "BattleHall", "OnEnter");
        Assert.True(host.Story.Has("FLAG_BATTLE_HALL_VISITOR_3") && !host.Story.Has("FLAG_BATTLE_HALL_VISITOR_0"));
        Assert.Equal(3, host.Story.Var("VAR_BATTLE_HALL_MAJOR_NPC"));
        Play(host, runner, "BattleHall", "Jasmine", "visitor_jasmine");
        Assert.True(Said(host, "Olivine City"));
        // Everyone else has something to say
        var lobby = Room("BattleHall");
        foreach (var person in lobby.Everyone.Where(n => n.Script != null && n.Key?.EndsWith("attendant") != true))
        {
            host.Transcript.Clear();
            Play(host, runner, "BattleHall", person.Script!, person.Key);
            Assert.NotEmpty(host.Transcript);
        }
        Play(host, runner, "BattleHall", "MultiAttendant", "multi_attendant");
        Assert.True(Said(host, "nobody can be linked up with"));
    }

    [Fact]
    public void TheBoardShowsTheSpeciesWithTheLongestStreaks()
    {
        var records = new FrontierRecords();
        var story = new StoryState();
        Assert.Empty(FrontierBoard.For(FrontierFacility.Hall, records, story).Species);
        records.Hall.Singles["Garchomp"] = 50;
        records.Hall.Singles["Bidoof"] = 3;
        records.Hall.Doubles["Bidoof"] = 12;
        records.Hall.Doubles["Luxray"] = 0;
        records.Set(FrontierStat.HallLatestStreakSingle, 7);
        records.SetRunning(FrontierFacility.Hall, 0, true);
        var board = FrontierBoard.For(FrontierFacility.Hall, records, story);
        // A row for each way: the best any species has done, and the streak last made
        Assert.Equal(new[] { ("Single Battle", 50, 7, true), ("Double Battle", 12, 0, false) }, board.Rows.Select(r => (r.Label, r.Record, r.Latest, r.Running)));
        // And the species themselves, the longest Single streak first
        Assert.Equal(new[] { ("Garchomp", 50, 0), ("Bidoof", 3, 12) }, board.Species.Select(s => (s.Species, s.Single, s.Double)));
        Assert.Equal("Battle Hall", board.Title);
    }

    [Fact]
    public void TheHallCommandsSayWhatIsWrong()
    {
        var host = Host(Mon("Garchomp", 60));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var library = ScriptLibrary.FromSources(("test", "script Battle\n  hall battle\n  end\nscript Begin\n  hall begin single\n  end\nscript Quiet\n  hall active\n  hall round\n  hall rank\n  hall beaten\n  end\n"));
        runner = new ScriptRunner(library, host);
        runner.Start(library.Find("Quiet", "test")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        runner.Start(library.Find("Battle", "test")!, null);
        Assert.Contains("needs a challenge under way", Assert.Throws<ScriptException>(() => runner.RunToEnd()).Message);
        runner = new ScriptRunner(library, host);
        runner.Start(library.Find("Begin", "test")!, null);
        Assert.Contains("hall choose", Assert.Throws<ScriptException>(() => runner.RunToEnd()).Message);
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script X\n  hall begin triple\n  end\n"));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script X\n  hall sing\n  end\n"));
    }
}
