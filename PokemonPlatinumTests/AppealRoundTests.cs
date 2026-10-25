using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;

namespace PokemonPlatinumTests;

/// <summary>
/// Hoenn's contest (plan 21 · H11): Emerald's tables (<c>src/data/contest_moves.h</c>, <c>contest_opponents.h</c>) as the
/// data holds them, and the appeals' rules (<c>src/contest.c</c>, <c>src/contest_effect.c</c>, <c>src/contest_ai.c</c>):
/// the first round's order, a seeded contest giving the same scores twice, a move's hearts, a combo, a jam, a repeat, the
/// crowd's burst, a condition raised, the order bent. Each number is worked out from the original's code in a comment.
/// </summary>
public class AppealRoundTests
{
    // ---------------------------------------------------------------- helpers

    private static Pokemon Bidoof() => new(PokemonDatabase.Get("Bidoof")!, 20, Gender.Male, Nature.Hardy, false);

    private static Appealer Mon(string trainer, int cool, bool player, params string[] moves) => new()
    {
        Trainer = trainer,
        Pokemon = Bidoof(),
        IsPlayer = player,
        Stats = new[] { cool, 0, 0, 0, 0 },
        Sheen = 0,
        Moves = moves.Select(m => AppealMove.Of(MoveDatabase.Get(m))).ToList()
    };

    /// <summary>Four on a stage whose conditions are 40, 30, 20 and 10 in a Cool contest: the first goes first, the player (the last) fourth.</summary>
    private static AppealRound Stage(string[][] moves, ContestStat category = ContestStat.Cool, int seed = 1)
    {
        var mons = new List<Appealer>();
        for (int i = 0; i < 4; i++) mons.Add(Mon($"T{i}", 40 - i * 10, i == 3, moves[i]));
        return new AppealRound(mons, category, AppealRank.Normal, new Random(seed));
    }

    private static string[][] Everyone(params string[] moves) => Enumerable.Range(0, 4).Select(_ => moves).ToArray();

    private static List<AppealEvent> PlayRound(AppealRound round, params int[] picks)
    {
        round.ChooseMoves(picks);
        var events = new List<AppealEvent>();
        for (int turn = 0; turn < 4; turn++) events.AddRange(round.PlayTurn());
        return events;
    }

    // ---------------------------------------------------------------- Emerald's tables

    [Fact]
    public void TheTablesAreEmeralds()
    {
        Assert.Equal(48, HoennContestDatabase.Effects.Count);
        Assert.Equal(Enumerable.Range(0, 48), HoennContestDatabase.Effects.Select(e => e.Id));
        Assert.Equal(354, HoennContestDatabase.Moves.Count);
        Assert.Equal(62, HoennContestDatabase.ComboStarters.Count);
        Assert.Equal(96, HoennContestDatabase.Opponents.Count);

        // gContestEffects[HIGHLY_APPEALING], [STARTLE_FRONT_MON], [BADLY_STARTLE_FRONT_MON]: appeal and jam in tens of points
        Assert.Equal((40, 0), (HoennContestDatabase.Effect(0).Appeal, HoennContestDatabase.Effect(0).Jam));
        Assert.Equal((30, 20), (HoennContestDatabase.Effect(8).Appeal, HoennContestDatabase.Effect(8).Jam));
        Assert.Equal((10, 40), (HoennContestDatabase.Effect(12).Appeal, HoennContestDatabase.Effect(12).Jam));

        // Pound: a plain Tough appeal that starts the Pound combo; Double Slap follows it; Stomp (whose source line has no
        // field name) follows Leer
        var pound = MoveDatabase.Get("Pound").HoennContest;
        Assert.Equal((0, ContestStat.Tough, 60), (pound.Effect, pound.Category, pound.ComboStarter));
        Assert.Equal("Pound", HoennContestDatabase.ComboStarterName(60));
        Assert.True(HoennContestDatabase.AreCombo(pound, MoveDatabase.Get("Double Slap").HoennContest));
        Assert.False(HoennContestDatabase.AreCombo(MoveDatabase.Get("Double Slap").HoennContest, pound));
        Assert.True(HoennContestDatabase.AreCombo(MoveDatabase.Get("Leer").HoennContest, MoveDatabase.Get("Stomp").HoennContest));
    }

    [Fact]
    public void EveryContestantIsAPokemonOfTheGameWithMovesItKnows()
    {
        foreach (var o in HoennContestDatabase.Opponents)
        {
            Assert.NotNull(PokemonDatabase.Get(o.Species));
            Assert.InRange(o.Moves.Count, 1, 4);
            Assert.All(o.Moves, m => Assert.True(MoveDatabase.Has(m), $"{o.Trainer}'s {m}"));
            Assert.NotEmpty(o.Pools);
            Assert.InRange(o.Rank, 0, 3);
        }
        // Every contest of every rank has contestants to draw three from, before the game's end and after it
        foreach (ContestStat quality in Enum.GetValues<ContestStat>())
            foreach (var rank in Enum.GetValues<AppealRank>())
            {
                Assert.True(HoennContestRules.Candidates(quality, rank, afterTheGame: false).Count >= 3, $"{quality} {rank} before");
                Assert.True(HoennContestRules.Candidates(quality, rank, afterTheGame: true).Count >= 3, $"{quality} {rank} after");
            }
    }

    [Fact]
    public void AMoveEmeraldNeverKnewIsAPlainAppealInAQualityOfItsOwn()
    {
        var move = MoveDatabase.Get("Draco Meteor").HoennContest;
        Assert.Equal(0, move.Effect);
        Assert.Equal(0, move.ComboStarter);
        // Every move of the data has a quality and an effect that exists
        foreach (var m in MoveDatabase.GetAll())
            Assert.InRange(m.HoennContest.Effect, 0, 47);
    }

    // ---------------------------------------------------------------- the condition

    [Fact]
    public void TheConditionScoresTheQualityPlusHalfItsNeighboursAndTheSheen()
    {
        // CalculateContestantRound1Points: the quality + (its two neighbours + sheen) / 2; Cool's neighbours are Tough and
        // Beauty, Beauty's Cool and Cute, Cute's Beauty and Smart, Smart's Cute and Tough, Tough's Smart and Cool
        var a = new Appealer { Pokemon = Bidoof(), Stats = new[] { 100, 40, 10, 20, 60 }, Sheen = 30 };
        Assert.Equal(100 + (60 + 40 + 30) / 2, AppealRound.Round1Points(ContestStat.Cool, a));     // 165
        Assert.Equal(40 + (100 + 10 + 30) / 2, AppealRound.Round1Points(ContestStat.Beauty, a));   // 110
        Assert.Equal(10 + (40 + 20 + 30) / 2, AppealRound.Round1Points(ContestStat.Cute, a));      // 55
        Assert.Equal(20 + (10 + 60 + 30) / 2, AppealRound.Round1Points(ContestStat.Smart, a));     // 70
        Assert.Equal(60 + (20 + 100 + 30) / 2, AppealRound.Round1Points(ContestStat.Tough, a));    // 135
    }

    [Fact]
    public void TheBestConditionAppealsFirstAndEveryoneHasATurn()
    {
        var mons = new[] { Mon("a", 10, false, "Pound"), Mon("b", 40, false, "Pound"), Mon("c", 20, false, "Pound"), Mon("d", 30, true, "Pound") };
        var round = new AppealRound(mons, ContestStat.Cool, AppealRank.Normal, new Random(7));
        Assert.Equal(new[] { 3, 0, 2, 1 }, round.TurnOrder);
        Assert.Equal(new[] { 1, 3, 2, 0 }, Enumerable.Range(0, 4).Select(round.ContestantAtTurn));
    }

    // ---------------------------------------------------------------- the appeals

    [Fact]
    public void AMoveAppealsForTheHeartsItsEffectGives()
    {
        var round = Stage(Everyone("Pound", "Thunder Punch"));
        var events = PlayRound(round, 0, 0, 0, 0);
        // HIGHLY_APPEALING is 40 points, four hearts, with no condition and nothing to move the crowd (Tough in a Cool contest)
        Assert.All(events.Where(e => e.Kind == AppealEventKind.Appealed), e => Assert.Equal(40, e.Amount));
        Assert.Equal(4, events.Count(e => e.Kind == AppealEventKind.Appealed));
        Assert.All(round.Status, s => Assert.Equal(40, s.PointTotal));
    }

    [Fact]
    public void AComboIsBeganWithAStarterAndFinishedWithADoubleOfTheFollowersBaseAppeal()
    {
        var round = Stage(Everyone("Pound", "Double Slap"));
        var first = PlayRound(round, 0, 0, 0, 0);
        // Pound is a starter (CalculateAppealMoveImpact: comboStarterId != 0, so the judge's eye is on it)
        Assert.Equal(4, first.Count(e => e.Kind == AppealEventKind.ComboStarted));
        Assert.All(round.Status, s => Assert.True(s.HasJudgesAttention));
        var second = PlayRound(round, 1, 1, 1, 1);
        // Double Slap follows Pound: the finished combo adds the move's base appeal again (STARTLE_MON_WITH_JUDGES_ATTENTION, 20)
        Assert.Equal(4, second.Count(e => e.Kind == AppealEventKind.ComboDone));
        Assert.All(second.Where(e => e.Kind == AppealEventKind.ComboDone), e => Assert.Equal(20, e.Amount));
        Assert.All(round.Status, s => Assert.False(s.HasJudgesAttention));
    }

    [Fact]
    public void AMoveThatStartlesTakesItsJamOffThePokemonBeforeIt()
    {
        var round = Stage(Everyone("Pound", "Stomp"));
        // The order is 0, 1, 2, 3: the second uses Stomp (BADLY_STARTLE_FRONT_MON: jam 40) on the first, who used Pound (40)
        var turn1 = new List<AppealEvent>();
        round.ChooseMoves(new[] { 0, 1, 0, 0 });
        turn1.AddRange(round.PlayTurn());
        Assert.Equal(40, round.Status[0].Appeal);
        var turn2 = round.PlayTurn();
        Assert.Equal(0, round.Status[0].Appeal);                       // 40 - 40
        Assert.Equal(40, round.Status[0].Jam);
        Assert.Contains(turn2, e => e is { Kind: AppealEventKind.Jammed, Who: 0, Amount: 40 });
        Assert.Contains(turn2, e => e is { Kind: AppealEventKind.Line, Line: AppealLine.LeaptUp });
        // Stomp's own appeal is 10
        Assert.Equal(10, turn2.First(e => e.Kind == AppealEventKind.Appealed).Amount);
        // Nobody before the first can be startled, so a startler who goes first only fails
        var alone = Stage(Everyone("Stomp", "Pound"));
        alone.ChooseMoves(new[] { 0, 1, 1, 1 });
        var firstTurn = alone.PlayTurn();
        Assert.Contains(firstTurn, e => e is { Kind: AppealEventKind.Line, Line: AppealLine.MessedUp });
    }

    [Fact]
    public void ARepeatedMoveCostsTwentyTheFirstTime()
    {
        var round = Stage(Everyone("Pound", "Thunder Punch"));
        PlayRound(round, 0, 0, 0, 0);
        var events = PlayRound(round, 0, 0, 0, 0);
        // moveRepeatCount is 1 as the move is repeated: repeatJam = (1 + 1) * 10
        Assert.All(events.Where(e => e.Kind == AppealEventKind.Repeated), e => Assert.Equal(20, e.Amount));
        Assert.Equal(4, events.Count(e => e.Kind == AppealEventKind.Repeated));
        Assert.All(round.Status, s => Assert.Equal(40 + 20, s.PointTotal));   // 40, then 40 - 20
        // A third time costs thirty
        var third = PlayRound(round, 0, 0, 0, 0);
        Assert.All(third.Where(e => e.Kind == AppealEventKind.Repeated), e => Assert.Equal(30, e.Amount));
    }

    [Fact]
    public void AMoveOfTheContestsQualityStirsTheCrowdAndAFifthTimeBurstsItForSixty()
    {
        string[] cool = { "Thunder Punch", "Vine Whip", "Mega Kick", "Horn Attack", "Peck" };
        foreach (string m in cool) Assert.Equal((0, ContestStat.Cool), (MoveDatabase.Get(m).HoennContest.Effect, MoveDatabase.Get(m).HoennContest.Category));
        // Each contestant has the five (so none repeats the move before it)
        var round = Stage(Everyone(cool));
        var first = PlayRound(round, 0, 1, 2, 3);
        // sContestExcitementTable[Cool][Cool] is +1: ten points to the move while the meter stays under the top
        var ups = first.Where(e => e.Kind == AppealEventKind.CrowdUp).ToList();
        Assert.Equal(4, ups.Count);
        Assert.All(ups, e => Assert.Equal(10, e.Amount));
        Assert.Equal(4, round.ApplauseLevel);
        // The next move that stirs it would fill a fifth light: the burst is sixty, and the meter starts again
        var second = PlayRound(round, 4, 4, 4, 4);
        var burst = second.First(e => e.Kind == AppealEventKind.CrowdUp);
        Assert.Equal(60, burst.Amount);
        Assert.Equal(5, burst.Other);
        Assert.InRange(round.ApplauseLevel, 0, 4);
    }

    [Fact]
    public void AMoveOfAQualityThatClashesCoolsTheCrowd()
    {
        // sContestExcitementTable[Cool][Cute] is -1: a Cute move (Splash) in a Cool contest lowers the meter by one
        var round = Stage(Everyone("Thunder Punch", "Vine Whip", "Splash"));
        round.ChooseMoves(new[] { 0, 1, 0, 1 });
        round.PlayTurn();
        round.PlayTurn();
        Assert.Equal(2, round.ApplauseLevel);
        var down = Stage(Everyone("Thunder Punch", "Tail Whip"));
        down.ChooseMoves(new[] { 0, 0, 1, 0 });
        down.PlayTurn();
        down.PlayTurn();
        var turn3 = down.PlayTurn();
        Assert.Contains(turn3, e => e.Kind == AppealEventKind.CrowdDown);
        Assert.Equal(1, down.ApplauseLevel);
    }

    [Fact]
    public void ARaisedConditionLastsAndAddsToEveryAppealAfterIt()
    {
        var round = Stage(Everyone("Swords Dance", "Pound"));
        var events = PlayRound(round, 0, 1, 1, 1);
        // IMPROVE_CONDITION: ten points of condition, and this turn's appeal takes condition - 10 so it adds nothing yet
        Assert.Equal(10, round.Status[0].Condition);
        Assert.Contains(events, e => e is { Kind: AppealEventKind.ConditionUp, Who: 0, Amount: 10 });
        Assert.Equal(10, events.First(e => e.Kind == AppealEventKind.Appealed && e.Who == 0).Amount);   // the move's own 10
        var next = PlayRound(round, 1, 1, 1, 1);
        // The next round Pound's 40 has the condition's 10 added for the one with a star
        Assert.Equal(50, next.First(e => e.Kind == AppealEventKind.Appealed && e.Who == 0).Amount);
        Assert.Equal(40, next.First(e => e.Kind == AppealEventKind.Appealed && e.Who == 1).Amount);
    }

    [Fact]
    public void AMoveThatAsksToGoEarlierMakesItsUserFirstInTheNextRound()
    {
        var round = Stage(Everyone("Agility", "Pound"));
        // The player, last in the order, uses Agility (NEXT_APPEAL_EARLIER)
        var events = PlayRound(round, 1, 1, 1, 0);
        Assert.Contains(events, e => e is { Kind: AppealEventKind.TurnOrder, Who: 3, Amount: 1 });
        Assert.Equal(0, round.TurnOrder[3]);
        // and one that asks to go later (Curse: NEXT_APPEAL_LATER) is last
        var later = Stage(Everyone("Curse", "Pound"));
        PlayRound(later, 0, 1, 1, 1);
        Assert.Equal(3, later.TurnOrder[0]);
    }

    [Fact]
    public void AMoveThatGivesEverythingLeavesNoMoreTurns()
    {
        var round = Stage(Everyone("Self-Destruct", "Pound"));
        var m = MoveDatabase.Get("Self-Destruct").HoennContest;
        Assert.Equal(2, m.Effect);   // GREAT_APPEAL_BUT_NO_MORE_MOVES
        PlayRound(round, 0, 1, 1, 1);
        Assert.True(round.Status[0].NoMoreTurns);
        Assert.False(round.CanAppeal(0));
        var next = PlayRound(round, 1, 1, 1, 1);
        Assert.Contains(next, e => e is { Kind: AppealEventKind.Watching, Who: 0 });
    }

    // ---------------------------------------------------------------- the whole contest

    [Fact]
    public void ASeededContestGivesTheSameScoresTwice()
    {
        (int[] total, int[] places, int[] condition) Play(int seed)
        {
            var player = new Pokemon(PokemonDatabase.Get("Milotic")!, 40) { Cool = 120, Beauty = 200, Cute = 90, Smart = 40, Tough = 30, Sheen = 80 };
            var stage = HoennContestRules.Stage(player, "Ann", ContestStat.Beauty, AppealRank.Hyper, afterTheGame: false, new Random(seed));
            var round = new AppealRound(stage, ContestStat.Beauty, AppealRank.Hyper, new Random(seed + 1));
            int moves = round.Appealers[round.PlayerIndex].Moves.Count;
            while (!round.IsOver)
            {
                round.ChooseMoves(round.AppealNumber % moves);
                for (int t = 0; t < 4; t++) round.PlayTurn();
            }
            return (round.TotalPoints.ToArray(), round.FinalStandings.ToArray(), round.Appealers.Select(a => a.Round1Points).ToArray());
        }

        var a = Play(11);
        var b = Play(11);
        Assert.Equal(a.total, b.total);
        Assert.Equal(a.places, b.places);
        Assert.Equal(a.condition, b.condition);
        // A contest ends with one winner and places 0 to 3
        Assert.Equal(new[] { 0, 1, 2, 3 }, a.places.OrderBy(p => p));
        // Every score is the condition's plus twice the hearts' points
        var player = new Pokemon(PokemonDatabase.Get("Milotic")!, 40);
        Assert.NotEqual(Play(11).total, Play(12).total);
    }

    [Fact]
    public void TheScoreIsTheConditionPlusTheAppealsTwiceOver()
    {
        var round = Stage(Everyone("Pound"));
        for (int r = 0; r < 5; r++)
        {
            // Five different moves keep the repeat's cost out of it
            round.ChooseMoves(new[] { 0, 0, 0, 0 });
            for (int t = 0; t < 4; t++) round.PlayTurn();
        }
        Assert.True(round.IsOver);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(round.Status[i].PointTotal * 2, round.Round2Points[i]);
            Assert.Equal(round.Appealers[i].Round1Points + round.Round2Points[i], round.TotalPoints[i]);
        }
    }

    [Fact]
    public void TheOpponentsThinkIsAMoveOfTheirsEveryRoundWithoutFail()
    {
        for (int seed = 0; seed < 40; seed++)
        {
            var player = new Pokemon(PokemonDatabase.Get("Castform")!, 30) { Cool = 60, Beauty = 20, Cute = 20, Smart = 90, Tough = 10, Sheen = 40 };
            var cat = (ContestStat)(seed % 5);
            var rank = (AppealRank)(seed % 4);
            var stage = HoennContestRules.Stage(player, "T", cat, rank, seed % 2 == 0, new Random(seed));
            var round = new AppealRound(stage, cat, rank, new Random(seed));
            while (!round.IsOver)
            {
                round.ChooseMoves(0);
                for (int t = 0; t < 4; t++) round.PlayTurn();
            }
            Assert.Equal(new[] { 0, 1, 2, 3 }, round.FinalStandings.OrderBy(p => p));
            Assert.Equal(AppealRound.Appeals, round.History.Count);
        }
    }

    [Fact]
    public void TheStageHasThreeOpponentsOfTheRankAndTheContestAndThePlayerLast()
    {
        var player = new Pokemon(PokemonDatabase.Get("Zigzagoon")!, 12);
        var stage = HoennContestRules.Stage(player, "Brendan", ContestStat.Tough, AppealRank.Super, afterTheGame: false, new Random(3));
        Assert.Equal(4, stage.Count);
        Assert.True(stage[3].IsPlayer);
        Assert.All(stage.Take(3), a =>
        {
            Assert.False(a.IsPlayer);
            Assert.Equal(1, a.Source!.Rank);
            Assert.Contains(ContestStat.Tough, a.Source.Pools);
            Assert.NotEqual(OpponentTime.OnlyAfterTheGame, a.Source.Time);
        });
        Assert.Equal(3, stage.Take(3).Select(a => a.Source).Distinct().Count());
        // After the game's end the late contestants join and the early ones are gone
        var late = HoennContestRules.Candidates(ContestStat.Cool, AppealRank.Master, afterTheGame: true);
        Assert.Contains(late, o => o.Time == OpponentTime.OnlyAfterTheGame);
        Assert.DoesNotContain(late, o => o.Time == OpponentTime.OnlyBeforeTheGame);
    }
}
