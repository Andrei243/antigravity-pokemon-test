using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 21 · part q (Hoenn's pictures finished): the Seaside Cycling Road a deck over Route 110's sea, the Abandoned Ship's
/// deck of planks, the S.S. Tidal's cabins walled by bulkheads cut low, and the roulette's line for a one-coin table.
/// </summary>
public class HoennPartQTests
{
    private static readonly Lazy<List<Map>> Maps = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn).BuildMaps().ToList());

    private static Map MapNamed(string name) => Maps.Value.Single(m => m.Name == name);

    [Theory]
    // The road's long stretch north and south over the sea, its wide platform, and its bend west
    [InlineData(217, 222)]
    [InlineData(216, 215)]
    [InlineData(212, 200)]
    [InlineData(220, 190)]
    public void TheSeasideCyclingRoadIsADeckOverTheSea(int x, int y)
    {
        var hoenn = MapNamed("Hoenn");
        Assert.True(hoenn.DeckAt(x, y) is { } deck && deck > hoenn.HeightAt(x, y) + 1f,
            $"({x}, {y}): deck {hoenn.DeckAt(x, y)?.ToString() ?? "none"} over ground {hoenn.HeightAt(x, y)}");
    }

    [Fact]
    public void TheCyclistsOnTheRoadStandOnItsDeck()
    {
        var hoenn = MapNamed("Hoenn");
        var riders = hoenn.Everyone.Where(n => n.GridX is >= 205 and <= 222 && n.GridY is >= 195 and <= 240 && hoenn.DeckAt(n.GridX, n.GridY) != null).ToList();
        Assert.NotEmpty(riders);
        Assert.All(riders, n => Assert.True(n.Level is > 1f, $"{n.Key} at ({n.GridX}, {n.GridY}) stands at {n.Level?.ToString() ?? "the ground"}"));
    }

    [Fact]
    public void TheAbandonedShipsDeckIsPlanksNotStone()
    {
        var deck = MapNamed("AbandonedShip_Deck");
        // The walk from the gangway to the cabins: the deck's planks, wherever the player stands on it
        int open = 0, planks = 0;
        for (int y = 0; y < deck.Height; y++)
            for (int x = 0; x < deck.Width; x++)
            {
                if (!deck.IsWalkable(x, y) || deck.GetGroundTile(x, y) is TileType.Water) continue;
                open++;
                if (deck.GetGroundTile(x, y) == TileType.Planks) planks++;
            }
        Assert.True(open > 0);
        Assert.True(planks * 10 >= open * 9, $"{planks} of the deck's {open} walked tiles are planks");
        Assert.DoesNotContain(Enumerable.Range(0, deck.Height).SelectMany(y => Enumerable.Range(0, deck.Width).Select(x => (x, y))),
            t => deck.GetGroundTile(t.x, t.y) == TileType.Grass && !deck.IsSolid(t.x, t.y));
    }

    [Theory]
    [InlineData("SSTidalCorridor")]
    [InlineData("SSTidalRooms")]
    [InlineData("SSTidalLowerDeck")]
    public void TheSSTidalsCabinsAreWalledByBulkheads(string room)
    {
        Assert.Equal(PartitionLook.Bulkhead, WorldModels.PartitionsOf(RegionDatabase.Hoenn, room));
        Assert.Equal(PartitionLook.Bulkhead, MapNamed(room).Partitions);
    }

    [Fact]
    public void AnotherHoennRoomKeepsItsWallAndJohtosAreAsTheyWere()
    {
        Assert.Equal(PartitionLook.Wall, WorldModels.PartitionsOf(RegionDatabase.Hoenn, "LilycoveCity_PokemonCenter_1F"));
        Assert.Equal(PartitionLook.Bulkhead, WorldModels.PartitionsOf("johto", "SSAqua_1F"));
        Assert.Equal(PartitionLook.Wall, WorldModels.PartitionsOf("kanto", "SSTidalRooms"));
    }

    [Fact]
    public void AOneCoinTableSaysCoinNotCoins()
    {
        Assert.Equal("A BALL: 1 COIN", Text.Of("rouletteTable.wagerOne"));
        Assert.Equal("A BALL: 3 COINS", Text.Of("rouletteTable.wager", ("coins", 3)));
    }
}
