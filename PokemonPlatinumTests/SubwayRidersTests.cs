using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 22 · U11's leftovers: the Battle Subway's own riders, its prints and its counter (rulings, "The Battle Subway's
/// riders, prints and counter"), held to their table (<c>Data/subway.json</c>).
/// </summary>
public class SubwayRidersTests
{
    [Fact]
    public void EveryRiderIsOneOfUnovasClassesWithALookAndNamesOfItsOwn()
    {
        Assert.NotEmpty(SubwayRiders.Riders);
        var unovaClasses = TrainerDatabase.OfRegion("unova").Select(t => t.Class).ToHashSet();
        foreach (var rider in SubwayRiders.Riders)
        {
            Assert.Contains(rider.Class, unovaClasses);
            // a class of the battle's table, drawn as the look the rider names
            Assert.Equal(rider.Look, CharacterStyles.Default.OfClass(rider.Class, rider.Look));
            Assert.True(Text.Has($"classes.{Trainer.KeyOf(rider.Class)}"), rider.Class);
            Assert.Equal(SubwayRiders.NamesEach, rider.Names.Count);
            Assert.Equal(rider.Names.Count, rider.Names.Distinct().Count());
        }
        // No two riders of one class share a name, so a rider is one person
        Assert.Equal(SubwayRiders.Riders.Sum(r => r.Names.Count),
            SubwayRiders.Riders.SelectMany(r => r.Names.Select(n => r.Class + "/" + n)).Distinct().Count());
    }

    [Fact]
    public void EveryFrontierTrainerTheSubwayDrawsRidesAsARiderAndAPairNeverSharesAName()
    {
        for (int number = 0; number < FrontierDatabase.Trainers.Count; number++)
        {
            var dress = SubwayRiders.DressOf(number);
            Assert.Contains(SubwayRiders.Riders, r => r.Class == dress.Class && r.Names.Contains(dress.Name) && r.Look == dress.Look);
            Assert.StartsWith("subway_rider_", dress.Id);
            Assert.NotEqual(dress.Name, SubwayRiders.DressOf(number, dress.Name).Name);
        }
        // The cars' pairs: drawn as ever (the same teams), worn by two different riders
        var rng = new Random(7);
        for (int battle = 1; battle <= 70; battle++)
        {
            var pair = BattleSubway.MakeOpponents(SubwayTrain.Multi, battle, () => rng.Next(0x10000));
            Assert.Equal(2, pair.Count);
            Assert.NotEqual(pair[0].ShownClass + pair[0].ShownName, pair[1].ShownClass + pair[1].ShownName);
            Assert.All(pair, p => Assert.StartsWith("subway_rider_", p.AsTrainer().Id));
        }
        // the staff partner is a Depot Agent
        Assert.Equal("Depot Agent", BattleSubway.MakePartner(12345).AsTrainer().TrainerClass);
    }

    [Fact]
    public void ADressedTrainerKeepsItsTeamAndTheFrontiersOwnAreUnchanged()
    {
        int n = 0;
        int Next() => (n = n * 1103515245 + 12345) & 0xFFFF;
        var plain = FrontierTrainers.ForTrainers(3, 10, 10, false, Next)[0];
        var dressed = plain.DressedAs(SubwayRiders.DressOf(10));
        Assert.Equal(plain.Pokemon, dressed.Pokemon);
        Assert.Equal("frontier_" + plain.Record.Id, plain.AsTrainer().Id);
        Assert.Equal(plain.Record.Class, plain.AsTrainer().TrainerClass);
        Assert.Equal(SubwayRiders.DressOf(10).Class, dressed.AsTrainer().TrainerClass);
    }

    [Fact]
    public void TheCountersPrizesAreItemsOfTheDataAtPricesOfOurs()
    {
        Assert.NotEmpty(SubwayRiders.Prizes);
        Assert.Equal(SubwayRiders.Prizes.Count, SubwayRiders.Prizes.Select(p => p.Item).Distinct().Count());
        Assert.All(SubwayRiders.Prizes, p =>
        {
            Assert.NotNull(ItemDatabase.Get(p.Item));
            Assert.InRange(p.Price, 1, 100);
            Assert.Equal(p.Price, SubwayRiders.PriceOf(p.Item));
        });
        Assert.Equal(0, SubwayRiders.PriceOf("Master Ball"));
    }

    [Fact]
    public void ThePrintsFollowTheFrontiersFrame()
    {
        var story = new StoryState();
        // Emmet first (a Super Double Train meets no Ingo): the silver waits first
        Assert.Equal(FrontierPrint.Silver, SubwayRiders.Beaten(story, 2));
        Assert.Equal(FrontierPrint.None, SubwayRiders.Beaten(story, 1));
        Assert.Equal(FrontierPrint.None, SubwayRiders.PrintOf(story));
        Assert.Equal(FrontierPrint.Silver, SubwayRiders.HandOver(story));
        Assert.Equal(FrontierPrint.None, SubwayRiders.HandOver(story));
        Assert.Equal(FrontierPrint.None, SubwayRiders.Beaten(story, 1));
        Assert.Equal(FrontierPrint.Gold, SubwayRiders.Beaten(story, 2));
        Assert.Equal(FrontierPrint.Gold, SubwayRiders.HandOver(story));
        Assert.Equal(4, story.Var(SubwayRiders.PrintVariable));
        Assert.Equal(FrontierPrint.None, SubwayRiders.Beaten(story, 2));
    }
}
