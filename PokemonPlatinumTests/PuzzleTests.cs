using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V26: the battle puzzles. Every puzzle's solution, played on the bare core, meets its goal and its decoy
/// doesn't; the solution's record replays to the same log; each is fought by the rules it names, handed to its battle.
/// </summary>
public class PuzzleTests
{
    private readonly ITestOutputHelper output;
    public PuzzleTests(ITestOutputHelper output) => this.output = output;

    public static IEnumerable<object[]> Ids => Puzzles.All.Select(p => new object[] { p.Id });

    private static PuzzleFile Get(string id) => Puzzles.All.Single(p => p.Id == id);

    [Fact]
    public void TenPuzzlesShipAndNoneHasAProblem()
    {
        Assert.True(Puzzles.All.Count >= 10, $"{Puzzles.All.Count} puzzles");
        Assert.Equal(Puzzles.All.Count, Puzzles.All.Select(p => p.Id).Distinct().Count());
        var problems = Puzzles.All.SelectMany(Puzzles.Problems).ToList();
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void TheSolutionMeetsTheGoalAndTheDecoyDoesNot(string id)
    {
        var puzzle = Get(id);
        var solved = Puzzles.Play(puzzle, puzzle.Solution);
        var decoyed = Puzzles.Play(puzzle, puzzle.Decoy);
        output.WriteLine($"{id}: solution {solved.Result} after {solved.Turn}, decoy {decoyed.Result} after {decoyed.Turn}");
        foreach (var e in solved.Log.OfType<Said>()) output.WriteLine("  s: " + e.Text);
        foreach (var e in decoyed.Log.OfType<Said>()) output.WriteLine("  d: " + e.Text);
        Assert.True(Puzzles.Judge(puzzle, solved), $"{id}'s solution doesn't meet its goal: {solved.Result} after {solved.Turn} turns");
        Assert.False(Puzzles.Judge(puzzle, decoyed), $"{id}'s decoy meets its goal: {decoyed.Result} after {decoyed.Turn} turns");
    }

    [Theory]
    [MemberData(nameof(Ids))]
    public void TheSolutionsRecordReplaysToTheSameLog(string id)
    {
        var puzzle = Get(id);
        var played = Puzzles.Play(puzzle, puzzle.Solution);
        var again = new BattleCore(Puzzles.Setup(puzzle));
        again.Replay(played.Record!);
        Assert.Equal(played.Log.OfType<Said>().Select(s => s.Text), again.Log.OfType<Said>().Select(s => s.Text));
        Assert.Equal(played.Result, again.Result);
    }

    [Fact]
    public void APuzzleIsFoughtByTheRulesItNamesWithoutTouchingTheGamesRules()
    {
        var before = Ruleset.Current;
        var puzzle = Get("priority");
        Assert.Same(Ruleset.Modern, Puzzles.Setup(puzzle).Rules);
        var platinum = new PuzzleFile { Rules = "Platinum" };
        Assert.Same(Ruleset.Platinum, platinum.RulesSet);
        Puzzles.Play(puzzle, puzzle.Solution);
        Assert.Same(before, Ruleset.Current);
    }

    [Fact]
    public void AFileThatAsksWhatTheCoreWouldRefuseIsNamed()
    {
        var bad = new PuzzleFile
        {
            Id = "bad",
            Player = { new PuzzlePokemon { Species = "Pikachu", Level = 50, Moves = { "Thunderbolt" }, Hp = 9999 } },
            Foe = { new PuzzlePokemon { Species = "Pikachu", Level = 50, Moves = { "Fly Away Now" } } },
            Turns = 0
        };
        var problems = Puzzles.Problems(bad);
        Assert.Contains(problems, p => p.Contains("9999 HP"));
        Assert.Contains(problems, p => p.Contains("battleRoom.illegal.move"));
        Assert.Contains(problems, p => p.Contains("0 turns"));
        Assert.Contains(problems, p => p.Contains("no solution"));
    }

    [Fact]
    public void TheStarsRoundTrip()
    {
        var records = new BattleRoomRecords();
        Assert.True(records.Solve("sunshine"));
        Assert.False(records.Solve("sunshine"));
        Assert.True(records.Solve("priority"));
        var settings = new GameSettings { BattleRoom = records };
        var back = System.Text.Json.JsonSerializer.Deserialize<GameSettings>(System.Text.Json.JsonSerializer.Serialize(settings))!;
        Assert.Equal(new[] { "priority", "sunshine" }, back.BattleRoom.Puzzles);
        Assert.True(back.BattleRoom.Solved("sunshine"));
    }

    [Fact]
    public void TheGoalIsJudgedByTheTurnTheBattleEndedIn()
    {
        var win = new PuzzleFile { Goal = PuzzleGoal.Win, Turns = 1 };
        Assert.True(Puzzles.Judge(win, BattleResult.PlayerVictory, 0));
        Assert.False(Puzzles.Judge(win, BattleResult.PlayerVictory, 1));
        Assert.False(Puzzles.Judge(win, BattleResult.None, 0));
        var survive = new PuzzleFile { Goal = PuzzleGoal.Survive, Turns = 2 };
        Assert.False(Puzzles.Judge(survive, BattleResult.None, 1));
        Assert.True(Puzzles.Judge(survive, BattleResult.None, 2));
        Assert.False(Puzzles.Judge(survive, BattleResult.PlayerDefeat, 0));
        Assert.True(Puzzles.Decided(survive, BattleResult.None, 2));
        Assert.False(Puzzles.Decided(survive, BattleResult.None, 1));
    }
}
