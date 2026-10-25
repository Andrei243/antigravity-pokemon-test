using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>
/// Pokéblocks (plan 21 · H11): the blender's arithmetic as Emerald's <c>CalculatePokeblock</c> has it, a block's taste for
/// a nature, feeding, the case, and the blender's game as a function of its presses.
/// </summary>
public class PokeblockTests
{
    private static BlenderBerry Berry(string name, int spicy, int dry, int sweet, int bitter, int sour, int smooth) =>
        new(name, spicy, dry, sweet, bitter, sour, smooth);

    private static readonly BlenderBerry Cheri = Berry("Cheri Berry", 10, 0, 0, 0, 0, 25);
    private static readonly BlenderBerry Chesto = Berry("Chesto Berry", 0, 10, 0, 0, 0, 25);
    private static readonly BlenderBerry Pecha = Berry("Pecha Berry", 0, 0, 10, 0, 0, 25);

    [Fact]
    public void OneBerryAtRestMakesAPokeblockOfItsOwnFlavourLessOne()
    {
        // Cheri: spicy 10, smoothness 25. Each flavour is taken from the next one's, so sour is 0 - 10 = -10: one negative.
        // The flavour left (10) loses the negative (9); at 0 rpm the factor is 100: 9 * 100 / 10 = 90 tenths = 9.
        // The feel is 25 / 1 - 1 player = 24.
        var block = Pokeblocks.Blend(new[] { Cheri }, 0, new Random(1));
        Assert.Equal(new Pokeblock(PokeblockColor.Red, 9, 0, 0, 0, 0, 24), block);
    }

    [Fact]
    public void AFasterBlendMakesAStrongerBlock()
    {
        // At 33,300 rpm (hundredths) the factor is 33300 / 333 + 100 = 200: 9 * 200 / 10 = 180 tenths = 18
        var block = Pokeblocks.Blend(new[] { Cheri }, 33300, new Random(1));
        Assert.Equal(18, block.Spicy);
        Assert.Equal(PokeblockColor.Red, block.Color);
    }

    [Fact]
    public void ColoursFollowTheFlavoursThatStandOut()
    {
        Assert.Equal(PokeblockColor.Blue, Pokeblocks.Blend(new[] { Chesto }, 0, new Random(1)).Color);
        Assert.Equal(PokeblockColor.Pink, Pokeblocks.Blend(new[] { Pecha }, 0, new Random(1)).Color);
        // Two different berries: the spicy one's 10 and the dry one's 10 less each other's: spicy 10, dry 10 - 0 = 10, sour 0 - 10
        var two = Pokeblocks.Blend(new[] { Cheri, Chesto }, 0, new Random(1));
        Assert.NotEqual(PokeblockColor.Black, two.Color);
    }

    [Fact]
    public void TwoOfOneBerryMakeABlackOneWhoseFlavoursAreThreeOfTheFiveAtTwo()
    {
        var block = Pokeblocks.Blend(new[] { Cheri, Cheri }, 5000, new Random(4));
        Assert.Equal(PokeblockColor.Black, block.Color);
        int[] flavours = { block.Spicy, block.Dry, block.Sweet, block.Bitter, block.Sour };
        Assert.Equal(3, flavours.Count(f => f == 2));
        Assert.Equal(2, flavours.Count(f => f == 0));
    }

    [Fact]
    public void ABlendIsTheSameWithTheSameDice()
    {
        var a = Pokeblocks.Blend(new[] { Cheri, Cheri }, 5000, new Random(9));
        var b = Pokeblocks.Blend(new[] { Cheri, Cheri }, 5000, new Random(9));
        Assert.Equal(a, b);
        Assert.Throws<ArgumentException>(() => Pokeblocks.Blend(Array.Empty<BlenderBerry>(), 0, new Random(1)));
        Assert.Throws<ArgumentException>(() => Pokeblocks.Blend(Enumerable.Repeat(Cheri, 5).ToArray(), 0, new Random(1)));
    }

    [Fact]
    public void ANatureLikesTheFlavourItsStatsLikeAndDislikesTheOpposite()
    {
        // Lonely: +Attack -Defense, so spicy is liked and sour disliked (gPokeblockFlavorCompatibilityTable)
        var spicy = new Pokeblock(PokeblockColor.Red, 10, 0, 0, 0, 0, 20);
        var sour = new Pokeblock(PokeblockColor.Yellow, 0, 0, 0, 0, 10, 20);
        Assert.Equal(10, Pokeblocks.Gain(Nature.Lonely, spicy));
        Assert.Equal(-10, Pokeblocks.Gain(Nature.Lonely, sour));
        Assert.Equal(0, Pokeblocks.Gain(Nature.Hardy, spicy));     // a nature with no taste
        Assert.Equal(PoffinTaste.Liked, Pokeblocks.TasteFor(Nature.Lonely, spicy));
        Assert.Equal(PoffinTaste.Disliked, Pokeblocks.TasteFor(Nature.Lonely, sour));
        Assert.Equal(PoffinTaste.Neutral, Pokeblocks.TasteFor(Nature.Hardy, spicy));
    }

    [Fact]
    public void EatingRaisesTheQualityAndTheSheenAndStopsWhenTheSheenIsFull()
    {
        var p = new Pokemon(PokemonDatabase.Get("Bidoof")!, 10, Gender.Male, Nature.Lonely, false);
        var block = new Pokeblock(PokeblockColor.Red, 10, 0, 0, 0, 0, 30);
        Assert.True(Pokeblocks.WouldEat(p));
        Pokeblocks.Feed(block, p);
        // The flavour's 10 and the nature's liking of it (10) on Cool; the others get the gain alone (10, held to 0 and up)
        Assert.Equal(20, p.Cool);
        Assert.Equal(10, p.Beauty);
        Assert.Equal(30, p.Sheen);
        p.Sheen = Poffins.MaxCondition;
        Assert.False(Pokeblocks.WouldEat(p));
        p.Sheen = 0;
        p.IsEgg = true;
        Assert.False(Pokeblocks.WouldEat(p));
    }

    [Fact]
    public void TheShownFeelStopsAtNinetyNine()
    {
        var block = new Pokeblock(PokeblockColor.Gold, 60, 0, 0, 0, 0, 200);
        Assert.Equal(99, block.ShownFeel);
        Assert.Equal(60, block.Level);
        Assert.Equal(Flavor.Spicy, block.Strongest);
    }

    [Fact]
    public void TheCaseHoldsFortyAndKeepsItsHolesAsTheyAre()
    {
        var c = new PokeblockCase();
        var block = new Pokeblock(PokeblockColor.Red, 9, 0, 0, 0, 0, 24);
        for (int i = 0; i < PokeblockCase.Capacity; i++) Assert.True(c.Add(block));
        Assert.True(c.IsFull);
        Assert.False(c.Add(block));
        c.RemoveAt(5);
        Assert.False(c.IsFull);
        Assert.Equal(39, c.Count);
        Assert.Null(c.Slots[5]);
        Assert.True(c.Add(block));
        Assert.Equal(40, c.Count);

        var restored = new PokeblockCase();
        restored.Restore(c.ToSaved());
        Assert.Equal(40, restored.Count);
    }

    [Fact]
    public void AGameOfTheBlenderIsAFunctionOfItsPressesAndItsDice()
    {
        var a = BlenderGame.Played(3, new Random(2), pressing: true);
        var b = BlenderGame.Played(3, new Random(2), pressing: true);
        Assert.True(a.IsOver);
        Assert.Equal((a.MaxRpm, a.Bests, a.Goods, a.Misses), (b.MaxRpm, b.Bests, b.Goods, b.Misses));
        // Pressing at the right moments beats standing by
        var idle = BlenderGame.Played(3, new Random(2), pressing: false);
        Assert.True(a.MaxRpm >= idle.MaxRpm);
        Assert.InRange(a.MaxRpm, BlenderGame.MinRpm, BlenderGame.MaxSpeed);
        Assert.Equal("30.00", BlenderGame.RpmText(3000));
    }
}
