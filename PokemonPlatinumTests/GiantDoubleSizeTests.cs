using System;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The giant's size in a double battle (the user's ruling of 2026-10-31; style guide, "A giant's growth"): a single's
/// giant keeps ×1.25 (foe) and ×1.3 (own), one with a partner beside it grows to ×1.12 and ×1.15, and at that size its
/// silhouette keeps clear of its partner's across the overview.
/// </summary>
public class GiantDoubleSizeTests
{
    private readonly ITestOutputHelper output;

    public GiantDoubleSizeTests(ITestOutputHelper output) => this.output = output;

    private static Pokemon Mon(string species) => new(PokemonDatabase.Get(species)!, 50);

    private static BattleAnimator Grown(int slots, BattleSide side)
    {
        var anim = new BattleAnimator();
        if (slots > 1) anim.SetSlots(slots, slots);
        for (int slot = 0; slot < slots; slot++)
        {
            anim.Appear(BattleSide.Player, Mon("Blissey"), slot);
            anim.Appear(BattleSide.Enemy, Mon("Blissey"), slot);
        }
        anim.Dynamax(side, 0, gigantamax: false, played: false);
        return anim;
    }

    [Theory]
    [InlineData(BattleSide.Player), InlineData(BattleSide.Enemy)]
    public void AGiantWithAPartnerStandsSmallerThanOneAlone(BattleSide side)
    {
        bool player = side == BattleSide.Player;
        var single = Grown(1, side);
        var paired = Grown(2, side);
        Assert.False(single.Paired(side));
        Assert.True(paired.Paired(side));
        float alone = BattleAnimator.GiantOf(single[side, 0], player, single.Paired(side));
        float beside = BattleAnimator.GiantOf(paired[side, 0], player, paired.Paired(side));
        Assert.Equal(player ? BattleAnimator.DynamaxScaleOwn : BattleAnimator.DynamaxScaleFoe, alone);
        Assert.Equal(player ? BattleAnimator.DynamaxScaleOwnDouble : BattleAnimator.DynamaxScaleFoeDouble, beside);
        // About ×1.15 for the player's own, as ruled, and still a giant
        Assert.InRange(BattleAnimator.DynamaxScaleOwnDouble, 1.13f, 1.17f);
        Assert.InRange(BattleAnimator.DynamaxScaleFoeDouble, 1.08f, BattleAnimator.DynamaxScaleOwnDouble);
        Assert.True(beside < alone);
        Assert.True(beside > 1.05f);
    }

    [Fact]
    public void TheFallInADoubleEndsOnTheSmallerSize()
    {
        var anim = Grown(2, BattleSide.Player);
        var v = anim.Player;
        Assert.Equal(BattleAnimator.DynamaxScaleOwnDouble, BattleAnimator.GiantOf(v, true, true));
        Assert.Equal(BattleAnimator.DynamaxScaleOwnDouble, BattleAnimator.FallOf(0f, BattleAnimator.GiantScale(true, true)));
    }

    [Fact]
    public void AGiantOnEachSideOfADoubleKeepsTheWreathsUnderTheirCaps()
    {
        // The wreaths are sized by the place's height, which follows the smaller giant; their count does not change
        var anim = Grown(2, BattleSide.Player);
        anim.Dynamax(BattleSide.Enemy, 1, gigantamax: false, played: false);
        var places = new FxPlaces();
        for (int s = 0; s < 2; s++)
        {
            places.Set(BattleSide.Player, s, BattlePlaces.Spot(BattleSide.Player, s, 2), 1.6f * BattleAnimator.DynamaxScaleOwnDouble);
            places.Set(BattleSide.Enemy, s, BattlePlaces.Spot(BattleSide.Enemy, s, 2), 2.2f * BattleAnimator.DynamaxScaleFoeDouble);
        }
        for (float t = 0f; t < 2f; t += 0.05f)
        {
            anim.Update(0.05f, (_, _) => null);
            var fx = new FxList();
            MoveFx.Giants(anim, places, fx);
            Assert.InRange(fx.Count, 2, 2 * MoveFx.GiantCap);
            Assert.True(fx.Count <= MoveFx.MaxQuads);
        }
    }

    [Theory]
    [InlineData("Garchomp-Mega", "Blissey")]
    [InlineData("Snorlax", "Snorlax")]
    [InlineData("Garchomp", "Snorlax")]
    public void AtTheOverviewAGiantKeepsClearOfItsPartner(string giant, string partner)
    {
        // Not every pair can be held to it: a Charizard's wings reach over a Blissey beside it at its own size in a
        // double (1.283 across a gap of 1.2), so no giant's size could keep it clear; the pairs here are the leftover's
        // (a Mega Dynamax Garchomp shows the Mega's model) and two whose own sizes keep clear.
        // The two of a side stand on one row, half a platform's radius either side of its middle (BattlePlaces.Spot),
        // and the overview looks along the row's normal from behind, so across the picture they part by the gap between
        // their places less their two half-widths as the back view turns them. YD's leftovers found the single's ×1.3
        // put a winged giant's wing over its partner (gi92_7). The tolerance is 2% of the gap between the places: the
        // half-widths are the rest pose's, and the idle's sway and a wing's beat move a tip by about that much.
        var anim = Grown(2, BattleSide.Player);
        var giantModel = PokemonModels.Get(giant);
        var partnerModel = PokemonModels.Get(partner);
        float gap = MathF.Abs(BattlePlaces.Spot(BattleSide.Player, 0, 2).X - BattlePlaces.Spot(BattleSide.Player, 1, 2).X);
        float Half(PokeModel m) => PokemonSprites.Framing(m, SpriteView.Back, PokemonSprites.Size).HalfWidth * BattleRenderer.ModelScale(m, true, anim);
        float partnerHalf = Half(partnerModel);
        float single = Half(giantModel) * BattleAnimator.DynamaxScaleOwn + partnerHalf;
        float paired = Half(giantModel) * BattleAnimator.DynamaxScaleOwnDouble + partnerHalf;
        output.WriteLine($"{giant} beside {partner}: gap {gap:0.000}, reach at ×1.3 {single:0.000}, at ×1.15 {paired:0.000}");
        Assert.True(paired <= gap * 1.02f, $"{giant} at ×{BattleAnimator.DynamaxScaleOwnDouble} reaches {paired:0.000} across a gap of {gap:0.000}");
    }
}
