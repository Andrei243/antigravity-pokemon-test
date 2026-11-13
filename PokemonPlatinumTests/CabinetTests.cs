using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Cabinets;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The arcade's two cabinets (plan 42 · PS6; rulings, "The arcade"): the match-three's board, swaps, cascades and damage by
/// the chart, a stage the same for the same seed and swaps; the picture-logic puzzle's clues and line solver, a picture
/// offered only when line logic finishes it alone; the silhouettes made from a model; the records round-tripping. In the
/// shared collection, since the screens play sounds.
/// </summary>
[Collection("Shared clocks and dice")]
public class CabinetTests
{
    private static readonly string[] Four = { "Squirtle", "Bulbasaur", "Charmander", "Pikachu" };

    private static CabinetStage StageOf(string foe = "Geodude", int hp = 400, int swaps = 20, uint seed = 77) =>
        new() { Id = "test", Foe = foe, Hp = hp, Swaps = swaps, Seed = seed, Coins = 25, CoinsPerSwapLeft = 2 };

    [Fact]
    public void TheTableIsWholeAndEveryFoeIsASpeciesOfTheData()
    {
        Assert.NotEmpty(CabinetDatabase.Stages);
        Assert.Equal(CabinetDatabase.Stages.Count, CabinetDatabase.Stages.Select(s => s.Id).Distinct().Count());
        foreach (var stage in CabinetDatabase.Stages)
        {
            Assert.NotNull(PokemonDatabase.Get(stage.Foe));
            Assert.True(stage.Hp > 0 && stage.Swaps > 0 && stage.Coins > 0, stage.Id);
        }
        Assert.True(CabinetDatabase.Helpers.Count >= MatchThree.Pieces);
        Assert.All(CabinetDatabase.Helpers, h => Assert.NotNull(PokemonDatabase.Get(h)));
        Assert.Equal(PictureLogic.Sizes.OrderBy(s => s), CabinetDatabase.Pictures.Select(p => p.Size).OrderBy(s => s));
    }

    [Fact]
    public void NoBoardBeginsWithALineAndEveryOneHasASwap()
    {
        for (uint seed = 0; seed < 300; seed++)
        {
            var board = new MatchThree(StageOf(seed: seed), Four);
            Assert.Empty(board.Runs());
            Assert.True(board.HasSwap(), $"seed {seed}");
        }
    }

    [Fact]
    public void ASwapThatMakesNoLineIsRefusedAndSpendsNothing()
    {
        var board = new MatchThree(StageOf(), Four);
        string before = board.Rows();
        // Find a pair of neighbours whose swap makes no line
        for (int c = 0; c < MatchThree.Cells; c++)
        {
            int d = c + 1;
            if (!MatchThree.Neighbours(c, d)) continue;
            var probe = new MatchThree(StageOf(), Four);
            if (probe.Swap(c, d) != SwapResult.NoLine) continue;
            Assert.Equal(SwapResult.NoLine, board.Swap(c, d));
            Assert.Equal(before, board.Rows());
            Assert.Equal(StageOf().Swaps, board.SwapsLeft);
            Assert.Equal(SwapResult.NotNeighbours, board.Swap(0, 2));
            Assert.Equal(StageOf().Swaps, board.SwapsLeft);
            return;
        }
        Assert.Fail("every swap of the board made a line");
    }

    [Fact]
    public void EachRunDealsItsLengthsDamageByTheChartAndCascadesComeInOrder()
    {
        bool sawCascade = false;
        for (uint seed = 0; seed < 60; seed++)
        {
            var board = new MatchThree(StageOf(hp: 100000, swaps: 30, seed: seed), Four);
            while (!board.Over && board.FirstSwap() is { } swap)
            {
                Assert.Equal(SwapResult.Made, board.Swap(swap.A, swap.B));
                Assert.NotEmpty(board.LastClears);
                int last = 1;
                foreach (var clear in board.LastClears)
                {
                    Assert.True(clear.Cascade >= last);
                    last = clear.Cascade;
                    float effect = TypeChart.GetEffectiveness(PokemonDatabase.Get(Four[clear.Piece])!.PrimaryType, PokemonType.Rock, PokemonType.Ground, Ruleset.Modern);
                    Assert.Equal(effect, clear.Effect);
                    int expected = (int)MathF.Floor((MatchThree.Base + MatchThree.PerExtra * (clear.Length - 3)) * (1f + MatchThree.CascadeBonus * (clear.Cascade - 1)) * effect);
                    Assert.Equal(expected, clear.Damage);
                    if (clear.Cascade > 1) sawCascade = true;
                }
                Assert.Empty(board.Runs());
            }
        }
        Assert.True(sawCascade, "no swap in sixty boards fell into a cascade");
        // Water against Rock and Ground is four times; Fire against Rock alone half (Ground takes it plainly)
        var b = new MatchThree(StageOf(), Four);
        Assert.Equal(4f, b.EffectOf(0));
        Assert.Equal(0.5f, b.EffectOf(2));
    }

    private static string Play(uint seed)
    {
        var board = new MatchThree(StageOf(hp: 900, swaps: 18, seed: seed), Four);
        var log = new List<string>();
        while (!board.Over && board.FirstSwap() is { } swap)
        {
            board.Swap(swap.A, swap.B);
            log.Add($"{swap.A}-{swap.B}:{board.Hp}:{board.Rows()}");
        }
        return string.Join("|", log) + $"|{board.Score}|{board.Shuffles}|{board.Cleared}";
    }

    [Fact]
    public void AStageIsTheSameForTheSameSeedAndSwaps()
    {
        Assert.Equal(Play(5), Play(5));
        Assert.NotEqual(Play(5), Play(6));
    }

    [Fact]
    public void AStageClearedPaysItsCoinsAndOneForEachSwapLeftAndAStageLostPaysNothing()
    {
        var board = new MatchThree(StageOf(hp: 40, swaps: 10), Four);
        while (!board.Over && board.FirstSwap() is { } swap) board.Swap(swap.A, swap.B);
        Assert.True(board.Cleared);
        Assert.Equal(25 + 2 * board.SwapsLeft, board.Payout);
        var lost = new MatchThree(StageOf(hp: 100000, swaps: 2), Four);
        while (!lost.Over && lost.FirstSwap() is { } swap) lost.Swap(swap.A, swap.B);
        Assert.False(lost.Cleared);
        Assert.Equal(0, lost.Payout);
        Assert.Equal(SwapResult.Over, lost.Swap(0, 1));
    }

    [Fact]
    public void TheBoardTakesTheTeamsFirstFourSpeciesAndTheHelpersForTheRest()
    {
        var team = new[] { new Pokemon(PokemonDatabase.Get("Turtwig")!, 5), new Pokemon(PokemonDatabase.Get("Turtwig")!, 6), new Pokemon(PokemonDatabase.Get("Starly")!, 5) };
        var four = CabinetDatabase.BoardSpecies(team);
        Assert.Equal(new[] { "Turtwig", "Starly", CabinetDatabase.Helpers[0], CabinetDatabase.Helpers[1] }, four);
    }

    [Fact]
    public void TheScreenPlaysAStageToItsCoins()
    {
        var screen = new MatchThreeScreen();
        screen.Open(StageOf(hp: 40, swaps: 10), Four);
        Assert.Equal(MatchPhase.Intro, screen.Phase);
        screen.Confirm();
        Assert.Equal(MatchPhase.Board, screen.Phase);
        while (screen.Phase == MatchPhase.Board)
        {
            var swap = screen.Board!.FirstSwap()!.Value;
            screen.PointAt(swap.A);
            screen.Confirm();
            Assert.Equal(swap.A, screen.Picked);
            screen.PointAt(swap.B);
            screen.Confirm();
        }
        Assert.Equal(MatchPhase.Result, screen.Phase);
        Assert.True(screen.Outcome > 0);
        screen.Confirm();
        Assert.False(screen.IsActive);
    }

    // ------------------------------------------------------------------ picture logic

    private static bool[] Parse(params string[] rows) => rows.SelectMany(r => r.Select(c => c == '#')).ToArray();

    [Fact]
    public void TheCluesAreEachLinesRunsAndTheLineSolverFindsWhatEveryPlacementShares()
    {
        var picture = Parse("##..#", ".....", "#####", "#.#.#", "..#..");
        var (rows, columns) = PictureLogic.Clues(5, picture);
        Assert.Equal(new[] { 2, 1 }, rows[0]);
        Assert.Empty(rows[1]);
        Assert.Equal(new[] { 5 }, rows[2]);
        Assert.Equal(new[] { 1, 1, 1 }, rows[3]);
        Assert.Equal(new[] { 1, 2 }, columns[0]);
        // A run of 4 in 5 cells: its middle three are filled whatever
        var line = PictureLogic.SolveLine(new bool?[5], new[] { 4 })!;
        Assert.Equal(new bool?[] { null, true, true, true, null }, line);
        // A full line and an empty one
        Assert.All(PictureLogic.SolveLine(new bool?[5], new[] { 5 })!, c => Assert.True(c));
        Assert.All(PictureLogic.SolveLine(new bool?[5], Array.Empty<int>())!, c => Assert.False(c));
        // A known cell decides: 1,1 in 3 cells is #.#
        Assert.Equal(new bool?[] { true, false, true }, PictureLogic.SolveLine(new bool?[3], new[] { 1, 1 }));
        // A contradiction is none
        Assert.Null(PictureLogic.SolveLine(new bool?[] { false, false, null }, new[] { 2 }));
    }

    [Fact]
    public void APictureWithTwoAnswersIsNotSolvedByLineLogicAndOneWithOneIs()
    {
        // The two diagonals of a square give the same clues
        Assert.False(PictureLogic.SolvesAlone(2, Parse("#.", ".#")));
        Assert.True(PictureLogic.SolvesAlone(5, Parse("..#..", ".###.", "#####", ".###.", "..#..")));
    }

    [Fact]
    public void FillingAPicturesCellsSolvesItAndAMistakeCrossesTheCellAndCounts()
    {
        var picture = Parse("##", "#.");
        var puzzle = new PictureLogic("Bidoof", 2, picture);
        Assert.False(puzzle.Fill(1, 1));
        Assert.Equal(1, puzzle.Mistakes);
        Assert.Equal(CellMark.Crossed, puzzle[1, 1]);
        Assert.True(puzzle.Fill(0, 0));
        Assert.True(puzzle.Fill(1, 0));
        Assert.False(puzzle.Solved);
        Assert.True(puzzle.Fill(0, 1));
        Assert.True(puzzle.Solved);
        Assert.Equal(CabinetDatabase.PayFor(10, 0), CabinetDatabase.Pictures.First(p => p.Size == 10).Coins);
        Assert.True(CabinetDatabase.PayFor(10, 100) >= CabinetDatabase.Pictures.First(p => p.Size == 10).Least);
    }

    public static IEnumerable<object[]> Plans => new[] { BodyPlan.Biped, BodyPlan.Quadruped, BodyPlan.Bird, BodyPlan.Serpent, BodyPlan.Fish, BodyPlan.Floating }
        .Select(p => new object[] { p.ToString() });

    [Theory]
    [MemberData(nameof(Plans))]
    public void ASilhouetteIsTheSameTwiceAndOneOfferedSolvesByLineLogicAlone(string planName)
    {
        var model = PokemonModels.Sample(Enum.Parse<BodyPlan>(planName));
        foreach (int size in PictureLogic.Sizes)
            Assert.Equal(Silhouettes.Make(model, size), Silhouettes.Make(model, size));
        var offered = Silhouettes.Offer(model);
        if (offered == null) return;
        Assert.True(PictureLogic.SolvesAlone(offered.Size, offered.Cells));
        Assert.True(offered.Cells.Count(c => c) >= Silhouettes.LeastShare * offered.Size * offered.Size);
    }

    [Fact]
    public void SomeOfTheSixBodyPlansAreOffered()
    {
        int offered = new[] { BodyPlan.Biped, BodyPlan.Quadruped, BodyPlan.Bird, BodyPlan.Serpent, BodyPlan.Fish, BodyPlan.Floating }
            .Count(p => Silhouettes.Offer(PokemonModels.Sample(p)) != null);
        Assert.True(offered >= 3, $"only {offered} of six body plans give a picture line logic solves");
    }

    [Fact]
    public void TheRecordsRoundTripAndAreNoneWhileEmpty()
    {
        var records = new CabinetRecords();
        Assert.Null(records.ToSave());
        Assert.Equal(CabinetDatabase.Stages[0].Id, records.NextStage()!.Id);
        records.RecordStage(CabinetDatabase.Stages[0].Id, 420, won: true);
        records.RecordStage(CabinetDatabase.Stages[1].Id, 120, won: false);
        records.RecordPicture("Bidoof", 15);
        var back = new CabinetRecords();
        back.Load(records.ToSave());
        Assert.True(back.HasCleared(CabinetDatabase.Stages[0].Id));
        Assert.False(back.HasCleared(CabinetDatabase.Stages[1].Id));
        Assert.Equal(120, back.BestOf(CabinetDatabase.Stages[1].Id));
        Assert.Equal(15, back.Pictures["Bidoof"]);
        Assert.Equal(CabinetDatabase.Stages[1].Id, back.NextStage()!.Id);
    }
}
