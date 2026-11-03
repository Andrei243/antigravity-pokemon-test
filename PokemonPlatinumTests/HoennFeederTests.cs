using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Hoenn's Safari Zone's Pokéblock feeders (plan 21 · H11, part d; Emerald's <c>src/safari_zone.c</c> and
/// <c>PickWildMonNature</c>): a Pokéblock set down on a feeder for a hundred steps, the range Emerald checks (its carried
/// difference kept), the nature it chooses four times in five, the script that sets one down, and the eleven feeders on
/// the zone's map.
/// </summary>
public class HoennFeederTests
{
    private static readonly Pokeblock Red = new(PokeblockColor.Red, 20, 0, 0, 0, 0, 10);

    [Fact]
    public void APokeblockSetDownLiesOnItsFeederForAHundredStepsOfTheGame()
    {
        var game = new SafariGame();
        game.Start();
        Assert.True(game.Feeders.Place(64, 70, Red));
        Assert.Equal(0, game.Feeders.At(64, 70));
        Assert.Equal(-1, game.Feeders.At(65, 70));
        for (int i = 0; i < PokeblockFeeders.Steps - 1; i++) game.Step();
        Assert.Equal(0, game.Feeders.At(64, 70));
        Assert.Equal(1, game.Feeders.Slots[0]!.StepsLeft);
        game.Step();
        Assert.Equal(-1, game.Feeders.At(64, 70));
    }

    [Fact]
    public void TenPlacesAndNoMoreAndTheGamesBeginningAndEndEmptyThem()
    {
        var game = new SafariGame();
        game.Start();
        for (int i = 0; i < PokeblockFeeders.Count; i++) Assert.True(game.Feeders.Place(i, 0, Red));
        Assert.False(game.Feeders.Place(20, 20, Red));
        game.End();
        Assert.All(game.Feeders.Slots, s => Assert.Null(s));
        game.Feeders.Place(4, 7, Red);
        game.Start();
        Assert.All(game.Feeders.Slots, s => Assert.Null(s));
        game.Feeders.Place(4, 7, Red);
        game.Resume(10, 100);
        Assert.All(game.Feeders.Slots, s => Assert.Null(s));
    }

    [Fact]
    public void APokeblockReachesFiveStepsAcrossAndDownInItsOwnPartAlone()
    {
        var feeders = new PokeblockFeeders();
        feeders.Place(64, 70, Red);   // the south part, its own (24, 30)
        Assert.Same(Red, feeders.InReach(64, 70));
        Assert.Same(Red, feeders.InReach(67, 72));   // 3 + 2
        Assert.Null(feeders.InReach(67, 73));        // 3 + 3
        Assert.Same(Red, feeders.InReach(64, 75));
        Assert.Null(feeders.InReach(64, 76));
        // The same tile of the part to the west is another place
        Assert.Null(feeders.InReach(24, 70));
        Assert.Equal(4, PokeblockFeeders.PartOf(64, 70));
        Assert.Equal((24, 30), PokeblockFeeders.Local(64, 70));
    }

    [Fact]
    public void EmeraldsRangeCarriesOneFeedersDifferenceIntoTheNext()
    {
        // Two Pokéblocks in one part: Emerald subtracts the second feeder's tile from what it made of the first, so a
        // player a step from the second is not in its reach (GetPokeblockFeederWithinRange)
        var feeders = new PokeblockFeeders();
        var blue = Red with { Color = PokeblockColor.Blue, Spicy = 0, Dry = 20 };
        feeders.Place(10, 10, Red);
        feeders.Place(30, 30, blue);
        Assert.Null(feeders.InReach(31, 30));
        // A lone feeder is found where it lies
        var alone = new PokeblockFeeders();
        alone.Place(30, 30, blue);
        Assert.Same(blue, alone.InReach(31, 30));
    }

    [Fact]
    public void FourWildPokemonInFiveTakeANatureThatLikesThePokeblock()
    {
        int chosen = 0;
        const int Tries = 2000;
        for (int seed = 0; seed < Tries; seed++)
        {
            var nature = PokeblockFeeders.NatureFor(Red, new Random(seed));
            if (nature is not { } n) continue;
            chosen++;
            Assert.True(Pokeblocks.Gain(n, Red) > 0, $"{n} doesn't like a spicy Pokéblock");
        }
        Assert.InRange(chosen, Tries * 3 / 4, Tries * 17 / 20);
        // Nothing chosen without a Pokéblock, nor by one no nature likes
        Assert.Null(PokeblockFeeders.NatureFor(null, new Random(1)));
        var plain = new Pokeblock(PokeblockColor.Black, 0, 0, 0, 0, 0, 0);
        Assert.All(Enumerable.Range(0, 50), s => Assert.Null(PokeblockFeeders.NatureFor(plain, new Random(s))));
    }

    [Fact]
    public void TheFeederInReachChoosesBeforeSynchronize()
    {
        // A Synchronize lead of a nature that dislikes spicy: whatever the feeder chooses likes it
        var lead = new WildLead("Synchronize", 30, Nature.Modest, Gender.Male, Feeder: Red);
        for (int seed = 0; seed < 200; seed++)
            if (WildEncounterRules.NatureFor(lead, new Random(seed)) is { } n && n != Nature.Modest)
                Assert.True(Pokeblocks.Gain(n, Red) > 0);
        // With no feeder the lead's nature comes one time in two, as before
        var plainLead = lead with { Feeder = null };
        Assert.Contains(Enumerable.Range(0, 20), s => WildEncounterRules.NatureFor(plainLead, new Random(s)) == Nature.Modest);
    }

    private static HeadlessScriptHost Feed(HeadlessScriptHost host, params int[] answers)
    {
        foreach (int a in answers) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("HoennFeeder", "hoenn_safari_zone_south")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheFeedersScriptSetsAPokeblockOfTheCaseDownAndThenSaysItIsStillThere()
    {
        var host = new HeadlessScriptHost { PlayerTile = (64, 71), PlayerFacing = Direction.Up };
        host.Safari.Start();
        host.PokeblockCase.Add(Red);
        Feed(host, 0, 0);
        Assert.Equal(0, host.Safari.Feeders.At(64, 70));
        Assert.Equal(0, host.PokeblockCase.Count);
        Assert.Contains(host.Transcript, l => l.Text.Contains(Red.Name));

        host.Transcript.Clear();
        Feed(host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("still lying"));

        // Turned down, or with no Pokéblock in the case, nothing is set down
        var other = new HeadlessScriptHost { PlayerTile = (64, 71), PlayerFacing = Direction.Up };
        other.Safari.Start();
        other.PokeblockCase.Add(Red);
        Feed(other, 1);
        Assert.Equal(-1, other.Safari.Feeders.At(64, 70));
        Assert.Equal(1, other.PokeblockCase.Count);
        var none = new HeadlessScriptHost { PlayerTile = (64, 71), PlayerFacing = Direction.Up };
        none.Safari.Start();
        Feed(none, 0);
        Assert.Equal(-1, none.Safari.Feeders.At(64, 70));
    }

    [Fact]
    public void TheZonesElevenFeedersAreReadOnTheirTiles()
    {
        var hoenn = World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn);
        var zone = hoenn.BuildMaps().Single(m => m.Name == PokeblockFeeders.MapName);
        var tiles = zone.TileScripts.Where(t => t.Value == "HoennFeeder").Select(t => t.Key).ToHashSet();
        // Emerald's MB_POKEBLOCK_FEEDER tiles, each part's own laid where the part lies on the map
        (int, int)[] emerald =
        {
            (4, 7), (33, 15), (31, 29), (72, 26), (45, 34), (18, 56), (64, 70), (85, 22), (109, 51), (105, 61), (99, 73)
        };
        Assert.Equal(emerald.ToHashSet(), tiles);
    }
}
