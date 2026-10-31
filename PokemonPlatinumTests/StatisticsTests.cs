using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>Plan 13 · V8: the counters, their keys, their place in the save and the statistics page of the Trainer Card.</summary>
public class StatisticsTests
{
    [Fact]
    public void EveryKeyIsNamedOnceAndAGroupHasEach()
    {
        var keys = Statistics.All.Select(k => k.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        foreach (var group in Enum.GetValues<StatGroup>()) Assert.NotEmpty(Statistics.KeysOf(group));
    }

    [Fact]
    public void AKeyNobodyNamedIsAMistake()
    {
        var s = new Statistics();
        Assert.Throws<ArgumentException>(() => s.Count("stepsTeleport"));
        Assert.True(s.IsEmpty);
    }

    [Fact]
    public void EveryWayOfTravellingHasAKeyThatIsNamed()
    {
        var s = new Statistics();
        foreach (var mode in Enum.GetValues<TravelMode>()) s.Count(Statistics.StepKey(mode));
        Assert.Equal(1, s.Get(Statistics.StepsSurf));
        Assert.Equal(1, s.Get(Statistics.StepsCycle));
        Assert.Equal(1, s.Get(Statistics.StepsDive));
        Assert.True(s.Get(Statistics.StepsFoot) >= 1);
    }

    [Fact]
    public void ABattleIsCountedOnceByItsResult()
    {
        var s = new Statistics();
        s.CountBattle(BattleResult.PlayerVictory, trainer: true);
        s.CountBattle(BattleResult.PlayerVictory, trainer: false);
        s.CountBattle(BattleResult.PlayerRan, trainer: false);
        s.CountBattle(BattleResult.EnemyFled, trainer: false);
        s.CountBattle(BattleResult.EnemyCaught, trainer: false, caughtNewSpecies: true);
        s.CountBattle(BattleResult.EnemyCaught, trainer: false);
        s.CountBattle(BattleResult.PlayerDefeat, trainer: true);
        s.CountBattle(BattleResult.None, trainer: true);

        Assert.Equal(1, s.Get(Statistics.TrainerWon));
        Assert.Equal(1, s.Get(Statistics.WildWon));
        Assert.Equal(1, s.Get(Statistics.WildRan));
        Assert.Equal(1, s.Get(Statistics.WildFled));
        Assert.Equal(2, s.Get(Statistics.Catches));
        Assert.Equal(1, s.Get(Statistics.NewSpecies));
        Assert.Equal(1, s.Get(Statistics.TrainerLost));
        Assert.Equal(1, s.Get(Statistics.Whiteouts));
        Assert.Equal(0, s.Get(Statistics.WildLost));
    }

    [Fact]
    public void TheCountersSurviveARoundTripOfTheSave()
    {
        var s = new Statistics();
        s.Count(Statistics.StepsFoot, 1234);
        s.Count(Statistics.Catches, 3);
        string json = JsonSerializer.Serialize(new SaveData { Counters = s.ToSaved() });
        var back = Statistics.From(JsonSerializer.Deserialize<SaveData>(json)!.Counters);
        Assert.Equal(1234, back.Get(Statistics.StepsFoot));
        Assert.Equal(3, back.Get(Statistics.Catches));
        Assert.Equal(0, back.Get(Statistics.Evolutions));
    }

    [Fact]
    public void ANewGameAndAnOldSaveCountNothingAndWriteNothing()
    {
        Assert.Null(new Statistics().ToSaved());
        Assert.DoesNotContain("Counters", JsonSerializer.Serialize(new SaveData()));
        Assert.True(Statistics.From(JsonSerializer.Deserialize<SaveData>("""{ "PlayerName": "Lucas" }""")!.Counters).IsEmpty);
    }

    [Fact]
    public void AKeyOfALaterGameIsPassedOver()
    {
        var back = Statistics.From(new Dictionary<string, long> { ["stepsHover"] = 9, [Statistics.Catches] = 2 });
        Assert.Equal(2, back.Get(Statistics.Catches));
        Assert.Equal(2, back.ToSaved()!.Values.Sum());
    }

    [Fact]
    public void TheCardHasAStatisticsPageOnlyOnceSomethingIsCounted()
    {
        var card = new TrainerCardScreen();
        card.Open();
        Assert.False(card.HasStatsPage);
        Assert.False(card.GoTo(CardPage.Statistics));

        card.Open(regionPages: 2, hasRun: true, titles: 3, hasStats: true);
        Assert.True(card.GoTo(CardPage.Statistics));
        Assert.Equal(CardPage.Statistics, card.Page);
        // After the Run page, before the titles and the regions
        Assert.True(card.Turn(1));
        Assert.Equal(CardPage.Titles, card.Page);
        Assert.True(card.Turn(1));
        Assert.Equal(CardPage.Region, card.Page);
        Assert.True(card.GoTo(CardPage.Run));
        card.Turn(1);
        Assert.Equal(CardPage.Statistics, card.Page);
    }

    [Fact]
    public void UpAndDownChangeTheGroupOnTheStatisticsPageOnly()
    {
        var card = new TrainerCardScreen();
        card.Open(hasStats: true);
        Assert.False(card.ChangeStatTab(1));
        card.GoTo(CardPage.Statistics);
        Assert.True(card.ChangeStatTab(1));
        Assert.Equal(1, card.StatTab);
        Assert.True(card.ChangeStatTab(-1));
        Assert.True(card.ChangeStatTab(-1));
        Assert.Equal(Enum.GetValues<StatGroup>().Length - 1, card.StatTab);
    }
}
