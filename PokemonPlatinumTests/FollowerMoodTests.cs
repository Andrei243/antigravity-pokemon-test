using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 35 · BN4, the follower's mood and its words: a strong mood's line over the band's, a place's line by water and in
/// a cave, a type's liking, the bubbles of its mood and of its own, and a calm follower's lines chosen with no draw.
/// </summary>
public class FollowerMoodTests
{
    private static readonly LeadFollower.Surroundings Nowhere = default;

    private static Pokemon Lead(string species = "Starly", int id = 3)
    {
        var p = Mon(species, 12);
        p.Id = new Guid(id, 0, 0, new byte[8]);
        return p;
    }

    // The first step count at which the line's own hash lets the place speak (the even half)
    private static int SpeakingStep(Pokemon p, LeadFollower.Surroundings at)
    {
        for (int steps = 0; steps < 64; steps++)
            if (LeadFollower.LineFor(p, 0, 2, at, steps, 5, 5) != LeadFollower.LineKey(2)) return steps;
        throw new Exception("the place never speaks");
    }

    [Fact]
    public void AStrongMoodsLineComesBeforeTheBandsAndTheAnythingElse()
    {
        var p = Lead();
        var lake = Nowhere with { ByWater = true };
        Assert.Equal("followerTalk.mood.Glum", LeadFollower.LineFor(p, -80, 4, lake, 0, 1, 1));
        Assert.Equal("followerTalk.mood.Elated", LeadFollower.LineFor(p, 75, 0, lake, 0, 1, 1));
        Assert.Equal("followerTalk.mood.Low", LeadFollower.LineFor(p, -45, 2, Nowhere, 0, 1, 1));
        // A mood under the strong mark leaves the band's line where the place says nothing
        Assert.Equal(LeadFollower.LineKey(2), LeadFollower.LineFor(p, 25, 2, Nowhere, 0, 1, 1));
        foreach (var mood in new[] { CareMood.Glum, CareMood.Low, CareMood.Cheerful, CareMood.Elated })
            Assert.True(Text.Has(LeadFollower.MoodKey(mood)), mood.ToString());
    }

    [Fact]
    public void APlacesLineByWaterAndInACaveAndATypesLiking()
    {
        var starly = Lead("Starly");
        var lake = Nowhere with { ByWater = true };
        Assert.Equal("followerTalk.place.Water", LeadFollower.LineFor(starly, 0, 2, lake, SpeakingStep(starly, lake), 5, 5));
        var cave = Nowhere with { Cave = true };
        Assert.Equal("followerTalk.place.Cave", LeadFollower.LineFor(starly, 0, 2, cave, SpeakingStep(starly, cave), 5, 5));
        var piplup = Lead("Piplup", 4);
        Assert.Equal("followerTalk.likes.water", LeadFollower.LineFor(piplup, 0, 2, lake, SpeakingStep(piplup, lake), 5, 5));
        var chimchar = Lead("Chimchar", 5);
        var rain = Nowhere with { Rain = true };
        Assert.Equal("followerTalk.likes.fireInRain", LeadFollower.LineFor(chimchar, 0, 2, rain, SpeakingStep(chimchar, rain), 5, 5));
        // Every place and liking has its words
        foreach (var spot in Enum.GetValues<LeadFollower.Spot>().Where(s => s != LeadFollower.Spot.None))
            Assert.True(Text.Has($"followerTalk.place.{spot}"), spot.ToString());
        // The order: a Gym before a room, a cave before water
        Assert.Equal(LeadFollower.Spot.Gym, LeadFollower.PlaceOf(Nowhere with { Gym = true, Room = true }));
        Assert.Equal(LeadFollower.Spot.Cave, LeadFollower.PlaceOf(Nowhere with { Cave = true, ByWater = true }));
    }

    [Fact]
    public void ACalmFollowerSpeaksOfThePlaceAboutOneTalkInTwoWithNoDraw()
    {
        var p = Lead();
        var lake = Nowhere with { ByWater = true };
        int spoke = Enumerable.Range(0, 200).Count(steps => LeadFollower.LineFor(p, 0, 2, lake, steps, 7, 9) != LeadFollower.LineKey(2));
        Assert.InRange(spoke, 70, 130);
        // The same talk gives the same line every time
        Assert.Equal(LeadFollower.LineFor(p, 0, 2, lake, 17, 7, 9), LeadFollower.LineFor(p, 0, 2, lake, 17, 7, 9));
        // ...and with nothing to say of the place, always the band's
        Assert.All(Enumerable.Range(0, 50), steps => Assert.Equal(LeadFollower.LineKey(3), LeadFollower.LineFor(p, 0, 3, Nowhere, steps, 7, 9)));
    }

    [Fact]
    public void TheBubblesFollowTheMoodAndAGlumOneTurnsItsBack()
    {
        Assert.Equal(EmoteBubble.Dots, LeadFollower.BubbleOf(CareMood.Glum, 4));
        Assert.Equal(EmoteBubble.Heart, LeadFollower.BubbleOf(CareMood.Elated, 0));
        Assert.Equal(LeadFollower.BubbleOf(2), LeadFollower.BubbleOf(CareMood.Calm, 2));
        Assert.True(LeadFollower.TurnsItsBack(CareMood.Glum));
        Assert.False(LeadFollower.TurnsItsBack(CareMood.Low));
        Assert.Equal(EmoteBubble.Note, LeadFollower.BubbleFor(LeadFollower.Moment.Won, CareMood.Calm));
        Assert.Equal(EmoteBubble.Dots, LeadFollower.BubbleFor(LeadFollower.Moment.Lost, CareMood.Elated));
        Assert.Equal(EmoteBubble.Heart, LeadFollower.BubbleFor(LeadFollower.Moment.Levelled, CareMood.Calm));
        Assert.True(LeadFollower.OwnBubbleSteps > 0);
    }

    [Fact]
    public void TheFindsAreUnmovedForACalmFollower()
    {
        // F4's find draws as it did: nothing until 256 steps, then one roll in three
        Assert.Null(LeadFollower.Find(LeadFollower.FindSteps - 1, new Random(1)));
        var a = new Random(5);
        var b = new Random(5);
        Assert.Equal(LeadFollower.Find(400, a), LeadFollower.Find(400, b));
    }
}
