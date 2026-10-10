using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using System.Text.Json;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Poffins and the contest condition (plan 06 · R14c): how a Poffin is made (<c>Poffin_MakePoffin</c>,
/// <c>src/poffin.c</c>), the pot it is stirred in (<c>src/overlay083/ov83_0223F7F4.c</c>), the case it is kept in, what
/// eating it does (<c>PoffinCase_UpdateMonContestStats</c>, <c>src/applications/poffin_case/main.c</c>), and the Poffin
/// House's and the fan club's people. Every number is worked out from the original's code in a comment beside it.
/// </summary>
[Collection("MapDatabase")]
public class PoffinTests
{
    private static Poffin Make(int spicy, int dry, int sweet, int bitter, int sour, int smooth = 25, bool foul = false) =>
        Poffins.Make(new[] { spicy, dry, sweet, bitter, sour }, smooth, foul, new Random(1));

    private static Pokemon Mon(string species, Nature nature, int level = 20) =>
        new(PokemonDatabase.Get(species)!, level, Gender.Male, nature, false);

    private static ItemData Berry(string name) => ItemDatabase.Get(name)!;

    // ---------------------------------------------------------------- making one

    [Fact]
    public void APoffinIsNamedForItsFlavours()
    {
        // One flavour: its own kind (flavor × 5 + flavor), its level that flavour
        var spicy = Make(20, 0, 0, 0, 0);
        Assert.Equal((PoffinType.Spicy, 20, "Spicy Poffin"), (spicy.Type, spicy.Level, spicy.Name));
        Assert.Equal(PoffinType.Sour, Make(0, 0, 0, 0, 12).Type);

        // Two: the stronger first (Dry 1 × 5 + Spicy 0 = Dry-Spicy), the level the flavour it is named for first
        var drySpicy = Make(10, 20, 0, 0, 0);
        Assert.Equal((PoffinType.DrySpicy, 20, "Dry-Spicy Poffin"), (drySpicy.Type, drySpicy.Level, drySpicy.Name));
        // A tie goes to the first of the five (flavors[a] >= flavors[b])
        Assert.Equal(PoffinType.SpicyDry, Make(15, 15, 0, 0, 0).Type);
        Assert.Equal(PoffinType.SweetBitter, Make(0, 0, 9, 3, 0).Type);

        // Three are rich, four or five overripe, and the level is then the strongest flavour
        var rich = Make(5, 30, 7, 0, 0);
        Assert.Equal((PoffinType.Rich, 30), (rich.Type, rich.Level));
        Assert.Equal(PoffinType.Overripe, Make(5, 6, 7, 8, 0).Type);
        Assert.Equal(PoffinType.Overripe, Make(5, 6, 7, 8, 9).Type);

        Assert.Equal("Rich Poffin", Poffins.NameOf(PoffinType.Rich));
        Assert.Equal("Sour-Bitter Poffin", Poffins.NameOf(PoffinType.SourBitter));
    }

    [Fact]
    public void AFlavourOfFiftyMakesItMildWhateverElseItIs()
    {
        // isMild once any flavour is 50 or more, over every other kind
        Assert.Equal(PoffinType.Mild, Make(50, 0, 0, 0, 0).Type);
        Assert.Equal(PoffinType.Spicy, Make(49, 0, 0, 0, 0).Type);
        var mild = Make(60, 30, 30, 30, 30, smooth: 40);
        Assert.Equal((PoffinType.Mild, 60, 40), (mild.Type, mild.Level, mild.Smoothness));
        // Poffin_CalcLevel never says more than 99
        Assert.Equal(99, Make(120, 0, 0, 0, 0).Level);
    }

    [Fact]
    public void NoFlavourOrAMistakeMakesAFoulPoffinWithThreeFlavoursOfTwo()
    {
        // Poffin_MakeFoul: three of the flavours not there already are set to 2; the smoothness is kept
        foreach (var foul in new[] { Make(0, 0, 0, 0, 0, smooth: 30), Make(9, 0, 0, 0, 0, smooth: 30, foul: true) })
        {
            Assert.Equal(PoffinType.Foul, foul.Type);
            int[] f = { foul.Spicy, foul.Dry, foul.Sweet, foul.Bitter, foul.Sour };
            Assert.Equal(3, f.Count(v => v == 2));
            Assert.Equal(2, f.Count(v => v == 0));
            Assert.Equal(30, foul.Smoothness);
            Assert.Equal(2, foul.Level);
        }
    }

    [Fact]
    public void FlavoursAreBytesAsTheOriginalKeepsThem()
    {
        // u8 poffinFlavors: 300 is kept as 44, which is under 50 and so not mild
        var wrapped = Make(300, 0, 0, 0, 0, smooth: 270);
        Assert.Equal((PoffinType.Spicy, 44, 14), (wrapped.Type, wrapped.Spicy, wrapped.Smoothness));
    }

    // ---------------------------------------------------------------- the pot

    [Fact]
    public void AStrokeCountsAsTheOriginalMeasuresIt()
    {
        // ApproximateArcLength(prev, cur) × 160: from (48, 0) to (48, 10) about the middle, the unit vector of (0, 48) is
        // (0, 4096), the dot product with (0, 10) is 40960, >> 12 is 10, the cross product 480 > 0 keeps it positive
        Assert.Equal(1600, PotMath.Swirl(176, 106, 176, 96));
        // The same stroke the other way round is negative
        Assert.Equal(-1600, PotMath.Swirl(176, 96, 176, 106));
        // Half way between the four points straight across, a stroke round the middle counts for nothing: at (34, 34)
        // the vector (34, 34) made a unit is (2896, 2896), and the stroke (-1, 1) along the circle gives 0
        Assert.Equal(0, PotMath.Swirl(161, 131, 162, 130));
        // CalcDistance2D rounds down; CalcRadialAngle over 427 pixels round
        Assert.Equal(5, PotMath.Distance(131, 100, 128, 96));
        Assert.Equal(5 * 0xFFFF / 427, PotMath.RadialAngle(5));
    }

    [Fact]
    public void AStrokePushesTheBatterWhichSlowsByItself()
    {
        var pot = new PoffinPot(new Random(3));
        // A swirl of 3200 at a good place (48 from the middle, inside the batter's 72): the push is
        // FX_Mul(3200 << 12, FX_Div(8 << 12, 204 << 12) = 161) >> 12 = 3200 × 161 >> 12 = 125, less the first stage's 64
        pot.Step(new[] { new PotTouch(176, 96, 3200, false) });
        Assert.Equal(61, pot.Speed);
        Assert.True(pot.Steady);
        // A swirl of 1600 pushes 1600 × 161 >> 12 = 62, less than the drag: it slows to 61 + 62 − 64 = 59
        pot.Step(new[] { new PotTouch(176, 96, 1600, false) });
        Assert.Equal(59, pot.Speed);
        // In the very middle a stroke counts half and the stir isn't steady; past the rim, nothing
        pot.Step(new[] { new PotTouch(130, 96, 3200, false) });
        Assert.False(pot.Steady);
        Assert.Equal(59 + (1600 * 161 >> 12) - 64, pot.Speed);
        // 80 from the middle is past the rim, which is 72 at this speed (FX_Mul(1.0, 64) + 8): it slows from 57 to nothing
        pot.Step(new[] { new PotTouch(128 + 80, 96, 3200, false) });
        Assert.Equal(0, pot.Speed);
    }

    [Fact]
    public void TooSlowWarnsThenBurnsAndTooFastSpillsOverButInTheLastStage()
    {
        // ov83_0223FAAC: ninety frames at 910 or under warn (2), each ninety more burn (1)
        var pot = new PoffinPot(new Random(5));
        var still = new[] { PotTouch.Still };
        for (int i = 0; i < 89; i++) pot.Step(still);
        Assert.Equal(0, pot.BurnSign);
        pot.Step(still);
        Assert.Equal(2, pot.BurnSign);
        Assert.Equal(0, pot.Burns);
        for (int i = 0; i < 90; i++) pot.Step(still);
        Assert.Equal((1, 1), (pot.BurnSign, pot.Burns));

        // Thirty frames at the most spill it over, over and over
        var fast = new PoffinPot(new Random(5));
        var hard = new[] { new PotTouch(176, 96, 65535, false) };
        int spills = 0;
        for (int i = 0; i < 300; i++)
        {
            fast.Step(hard);
            if (fast.Spilled) spills++;
        }
        Assert.Equal(PoffinPot.Top, fast.Speed);
        Assert.True(spills >= 8, $"{spills} spills");
        Assert.Equal(spills, fast.Spills);
        Assert.False(PoffinPot.Spilling(PoffinPot.Top, 2));
        Assert.True(PoffinPot.Spilling(-PoffinPot.Top, 1));
    }

    [Fact]
    public void AStageEndsAfterTwentySecondsAndTheCookingAfterThree()
    {
        // ov83_0223FC58: a stage is 600 frames; the 1801st frame finds the third over and the pot done
        var pot = new PoffinPot(new Random(7));
        var still = new[] { PotTouch.Still };
        for (int i = 0; i < 600; i++) Assert.False(pot.Step(still));
        Assert.Equal(0, pot.Phase);
        pot.Step(still);
        Assert.Equal((1, 1), (pot.Phase, pot.Stage));
        for (int i = 0; i < 1199; i++) pot.Step(still);
        Assert.Equal(2, pot.Phase);
        Assert.True(pot.Step(still));
        Assert.True(pot.Done);
        Assert.Equal(1800, pot.Frames);
        Assert.Equal((1, 0, 0), pot.Time);
        // The fade toward the next stage: 31 × frames left / 60 in the last two seconds; the 570th frame finds
        // 600 − 569 = 31 left: 31 × 31 / 60 = 16
        var fresh = new PoffinPot(new Random(7));
        for (int i = 0; i < 570; i++) fresh.Step(still);
        Assert.Equal(16, fresh.Fade);
    }

    [Fact]
    public void SixteenTurnsTheArrowsWayEndAStage()
    {
        var pot = new PoffinPot(new Random(11));
        int frames = 0;
        // Push hard the arrow's way, easing off before the batter spills over
        while (pot.Phase == 0 && frames < 600)
        {
            bool easy = Math.Abs(pot.Speed) > 3000;
            pot.Step(new[] { new PotTouch(176, 96, easy ? 0 : 6000, pot.Backward) });
            frames++;
        }
        Assert.Equal(1, pot.Phase);
        Assert.True(frames < 600, $"{frames} frames");
        Assert.Equal(0, pot.Spills);
    }

    [Fact]
    public void TheArrowIsDrawnAsTheCookingOpensAndAgainOnceItsTimeIsUp()
    {
        // ov83_0223FDB0 draws it at once; ov83_0223FBBC waits 150 frames and a draw of up to 59 in the first stage,
        // counting only while the batter goes its way
        var pot = new PoffinPot(new Random(13));
        int drawn = 0, first = -1;
        for (int i = 0; i < 600; i++)
        {
            pot.Step(new[] { new PotTouch(176, 96, Math.Abs(pot.Speed) > 2500 ? 0 : 4000, pot.Backward) });
            if (pot.ArrowDrawn && first < 0) first = i;
            if (pot.ArrowDrawn) drawn++;
            if (pot.Phase > 0) break;
        }
        Assert.InRange(first, 148, 150 + 59);
        Assert.True(drawn >= 1);
    }

    [Fact]
    public void ACheriBerryCookedInAMinuteMakesASpicyPoffinOfLevelNine()
    {
        // ov83_0223FFD4 with one Cheri Berry (spicy 10, smoothness 25): each flavour less the next round
        // (10 − 0, 0, 0, 0, 0 − 10), one below nought takes one off each (9, −1, −1, −1, −11); a minute (1,800 frames)
        // scales by 1,800,000 / 1,800 = 1000 → 100 in a hundred, so 9 × 100 / 100 = 9 and the rest nought. The
        // smoothness is 25 / 1 − 1 = 24.
        var cheri = new[] { Berry("Cheri Berry") };
        var poffin = PoffinPot.Result(cheri, 1800, 0, 0, 0, new Random(1));
        Assert.Equal((PoffinType.Spicy, 9, 0, 24), (poffin.Type, poffin.Spicy, poffin.Dry, poffin.Smoothness));

        // Half a minute scales by 1,800,000 / 900 = 2000 → 200: 18. Two burns and a spill take three off: 6
        Assert.Equal(18, PoffinPot.Result(cheri, 900, 0, 0, 0, new Random(1)).Spicy);
        Assert.Equal(6, PoffinPot.Result(cheri, 1800, 2, 1, 0, new Random(1)).Spicy);
        // 1,250 frames: 1,800,000 / 1,250 = 1440, whose last digit 0 is under 5: 144; 9 × 144 = 1296 → 13 (96 ≥ 50)
        Assert.Equal(13, PoffinPot.Result(cheri, 1250, 0, 0, 0, new Random(1)).Spicy);
        // Burned to nothing: no flavour left, which is foul
        Assert.Equal(PoffinType.Foul, PoffinPot.Result(cheri, 1800, 19, 0, 0, new Random(1)).Type);

        // A pot the cooking runs to its end gives the same
        var pot = new PoffinPot(new Random(1));
        while (!pot.Step(new[] { PotTouch.Still })) { }
        Assert.Equal(PoffinPot.Result(cheri, pot.Frames, pot.Burns, pot.Spills, 0, new Random(1)), pot.Finish(cheri, new Random(1)));
    }

    [Fact]
    public void AnOranBerryOfFourFlavoursComesOutDry()
    {
        // Oran: spicy, dry, bitter and sour 10, smoothness 20. Differences (0, 10, −10, 0, 0), one below nought:
        // (−1, 9, −11, −1, −1), a minute: a Dry Poffin of level 9, smoothness 20 − 1 = 19
        var poffin = PoffinPot.Result(new[] { Berry("Oran Berry") }, 1800, 0, 0, 0, new Random(1));
        Assert.Equal((PoffinType.Dry, 9, 19), (poffin.Type, poffin.Level, poffin.Smoothness));
    }

    [Fact]
    public void CooksTogetherShareTheirBerriesAndTheSameBerryTwiceIsFoul()
    {
        // Cheri and Pecha (sweet 10): sums (10, 0, 10, 0, 0), differences (10, −10, 10, 0, −10), two below nought:
        // (8, −12, 8, −2, −12); a minute keeps them: Spicy-Sweet (a tie, the first first) of level 8. Smoothness
        // (25 + 25) / 2 − 2 = 23, less nothing for stirring together
        var pair = new[] { Berry("Cheri Berry"), Berry("Pecha Berry") };
        var two = PoffinPot.Result(pair, 1800, 0, 0, 0, new Random(1));
        Assert.Equal((PoffinType.SpicySweet, 8, 8, 23), (two.Type, two.Spicy, two.Sweet, two.Smoothness));
        // 300 frames together: 300 / 6 × 1 / 10 = 5 off the smoothness for two; for four, × 10 / 10 = 50, held to 10
        Assert.Equal(18, PoffinPot.Result(pair, 1800, 0, 0, 300, new Random(1)).Smoothness);
        Assert.Equal(50, PoffinPot.TogetherBonusOf(300, 4));

        var same = PoffinPot.Result(new[] { Berry("Cheri Berry"), Berry("Cheri Berry") }, 1800, 0, 0, 0, new Random(1));
        Assert.Equal(PoffinType.Foul, same.Type);
    }

    [Fact]
    public void FourFlavoursBelowNoughtMakeItFoul()
    {
        // Sums (10, 20, 30, 40, 0): differences (−10, −10, −10, 40, −10), four below nought: foul
        var odd = new ItemData { Id = 90001, Name = "Odd Berry", Berry = new BerryData { Spiciness = 10, Dryness = 20, Sweetness = 30, Bitterness = 40, Smoothness = 30 } };
        Assert.Equal(PoffinType.Foul, PoffinPot.Result(new[] { odd }, 1800, 0, 0, 0, new Random(1)).Type);
        // The smoothness is never under 15
        var smooth = new ItemData { Id = 90002, Name = "Smooth Berry", Berry = new BerryData { Spiciness = 10, Smoothness = 10 } };
        Assert.Equal(15, PoffinPot.Result(new[] { smooth }, 1800, 0, 0, 0, new Random(1)).Smoothness);
    }

    [Fact]
    public void StirringTogetherCountsOnlyForMoreThanOneCookCloseTogether()
    {
        // ov83_0223FFA8: a sixth of the frames together, × 1, 5 or 10 for two, three or four cooks, ÷ 10
        var pot = new PoffinPot(new Random(17));
        Assert.Equal(0, pot.TogetherBonus(1));
        var hard = new PotTouch(176, 96, 6000, false);
        for (int i = 0; i < 200; i++)
        {
            bool easy = Math.Abs(pot.Speed) > 3000;
            var touch = hard with { Swirl = easy ? 700 : 6000, Backward = pot.Backward };
            pot.Step(new[] { touch, touch with { X = 170, Y = 100 } });
        }
        Assert.True(pot.TogetherFrames > 0);
        Assert.Equal(pot.TogetherFrames / 6 * 1 / 10, pot.TogetherBonus(2));
        Assert.Equal(pot.TogetherFrames / 6 * 10 / 10, pot.TogetherBonus(4));

        // Alone, never
        var alone = new PoffinPot(new Random(17));
        for (int i = 0; i < 200; i++) alone.Step(new[] { hard with { Backward = alone.Backward } });
        Assert.Equal(0, alone.TogetherFrames);
    }

    // ---------------------------------------------------------------- eating one

    [Fact]
    public void EatingAPoffinRaisesTheConditionAndTheSheen()
    {
        // A Hardy Pokémon takes the Poffin as it is: spicy to cool, dry to beauty, and so on, the smoothness to sheen
        var p = Mon("Turtwig", Nature.Hardy);
        p.Friendship = 70;
        var poffin = Make(20, 10, 0, 4, 0, smooth: 25);
        Assert.Equal(PoffinTaste.Neutral, Poffins.TasteFor(poffin, p.Nature));
        Poffins.Feed(poffin, p);
        Assert.Equal((20, 10, 0, 4, 0, 25), (p.Cool, p.Beauty, p.Cute, p.Smart, p.Tough, p.Sheen));
        // and likes its trainer one point more
        Assert.Equal(71, p.Friendship);
    }

    [Fact]
    public void ANatureLikesOneFlavourATenthMoreAndDislikesAnotherATenthLess()
    {
        // Adamant likes spicy and dislikes dry (sFlavorPreferences): (u8)(30 × 1.1f) = 33 and (u8)(25 × 0.9f) = 22
        var adamant = Mon("Turtwig", Nature.Adamant);
        var poffin = Make(30, 25, 0, 0, 0, smooth: 20);
        Assert.Equal(PoffinTaste.Liked, Poffins.TasteFor(poffin, Nature.Adamant));
        Poffins.Feed(poffin, adamant);
        Assert.Equal((33, 22, 20), (adamant.Cool, adamant.Beauty, adamant.Sheen));

        // Modest the other way round: it dislikes spicy and likes dry: (u8)(30 × 0.9f) = 27, (u8)(25 × 1.1f) = 27
        Assert.Equal(PoffinTaste.Disliked, Poffins.TasteFor(poffin, Nature.Modest));
        var modest = Mon("Turtwig", Nature.Modest);
        Poffins.Feed(poffin, modest);
        Assert.Equal((27, 27), (modest.Cool, modest.Beauty));

        // In single precision 7 × 1.1f is 7.7 and keeps 7; 10 × 0.9f is 9 (the product rounds to 9.0) and keeps 9
        var small = Make(7, 10, 0, 0, 0, smooth: 20);
        var again = Mon("Turtwig", Nature.Adamant);
        Poffins.Feed(small, again);
        Assert.Equal((7, 9), (again.Cool, again.Beauty));

        // Equal amounts of the liked and the disliked flavour, or a nature with no taste: neither
        Assert.Equal(PoffinTaste.Neutral, Poffins.TasteFor(Make(10, 10, 0, 0, 0), Nature.Adamant));
        Assert.Equal(PoffinTaste.Neutral, Poffins.TasteFor(Make(10, 0, 0, 0, 0), Nature.Serious));
        Assert.Equal((Flavor.Bitter, Flavor.Sour), Poffins.TasteOf(Nature.Gentle));
    }

    [Fact]
    public void TheConditionStopsAt255AndAFullSheenEatsNoMore()
    {
        var p = Mon("Turtwig", Nature.Hardy);
        p.Cool = 250;
        p.Sheen = 240;
        p.Friendship = 255;
        Assert.True(Poffins.WouldEat(p));
        Poffins.Feed(Make(20, 0, 0, 0, 0, smooth: 25), p);
        Assert.Equal((255, 255, 255), (p.Cool, p.Sheen, p.Friendship));
        // TryFeedPoffin: "won't eat any more" once the sheen is full, however much room the qualities have
        Assert.False(Poffins.WouldEat(p));
        p.Sheen = 254;
        Assert.True(Poffins.WouldEat(p));
    }

    [Fact]
    public void FeebasFedDryPoffinsGrowsBeautifulEnoughToEvolve()
    {
        // Feebas evolves on a level up with beauty of 170 or more; a Modest one likes dry: (u8)(40 × 1.1f) = 44 a Poffin
        var feebas = Mon("Feebas", Nature.Modest);
        var dry = Make(0, 40, 0, 0, 0, smooth: 20);
        var context = new EvolutionContext();
        for (int i = 0; i < 3; i++) Poffins.Feed(dry, feebas);
        Assert.Equal(132, feebas.Beauty);
        Assert.Null(Evolution.Find(feebas, EvolutionTrigger.LevelUp, context));
        Poffins.Feed(dry, feebas);
        Assert.Equal(176, feebas.Beauty);
        Assert.Equal("Milotic", Evolution.Find(feebas, EvolutionTrigger.LevelUp, context)!.TargetSpecies);
    }

    [Fact]
    public void TheSummaryShowsTwelveSparklesWhenTheSheenIsFull()
    {
        // PokemonSummaryScreen_InitSheenSprites: ((12 << 8) / 255 = 12) × sheen >> 8, nought for none, twelve when full
        Assert.Equal(0, Poffins.SparklesOf(0));
        Assert.Equal(0, Poffins.SparklesOf(21));
        Assert.Equal(1, Poffins.SparklesOf(22));
        Assert.Equal(6, Poffins.SparklesOf(128));
        Assert.Equal(11, Poffins.SparklesOf(254));
        Assert.Equal(12, Poffins.SparklesOf(255));
    }

    // ---------------------------------------------------------------- the case

    [Fact]
    public void TheCaseHoldsAHundredAndClosesUpWhenOneIsTakenOut()
    {
        var poffins = new PoffinCase();
        for (int i = 0; i < PoffinCase.Capacity; i++) Assert.True(poffins.Add(Make(i % 40 + 1, 0, 0, 0, 0)));
        Assert.True(poffins.IsFull);
        Assert.Equal(0, poffins.Free);
        Assert.False(poffins.Add(Make(5, 0, 0, 0, 0)));
        Assert.Equal(PoffinCase.Capacity, poffins.Count);

        // PoffinCase_ClearSlot and PoffinCase_Compact: the ones after it move up in order
        poffins.RemoveAt(2);
        Assert.Equal(99, poffins.Count);
        Assert.Equal(4, poffins.All[2].Spicy);
        Assert.True(poffins.Add(Make(50, 0, 0, 0, 0)));
        Assert.Equal(PoffinType.Mild, poffins.All[^1].Type);
    }

    [Fact]
    public void TheCaseListsAFlavoursPoffinsNewestFirst()
    {
        var poffins = new PoffinCase();
        poffins.Add(Make(10, 0, 0, 0, 0));
        poffins.Add(Make(0, 10, 0, 0, 0));
        poffins.Add(Make(5, 10, 0, 0, 0));
        Assert.Equal(new[] { 2, 1, 0 }, poffins.Listed(null));
        Assert.Equal(new[] { 2, 0 }, poffins.Listed(Flavor.Spicy));
        Assert.Equal(new[] { 2, 1 }, poffins.Listed(Flavor.Dry));
        Assert.Empty(poffins.Listed(Flavor.Sour));
    }

    [Fact]
    public void TheCasesListShowsAsManyRowsAsItsPanelHolds()
    {
        // The list sits under the tabs, as the bag's pocket does, and over its prompt
        const float height = ModernUi.ContentBottom - (ModernUi.ContentTop + 76 + 24) - ModernUi.CasePrompt - ModernUi.Gutter;
        Assert.Equal(PoffinCaseScreen.VisibleRows, ModernUi.RowsIn(height));
    }

    [Fact]
    public void FeedingFromTheCaseTakesThePoffinAndShowsTheChange()
    {
        var poffins = new PoffinCase();
        poffins.Add(Make(0, 0, 20, 0, 0, smooth: 30));
        var party = new Party();
        var p = Mon("Pachirisu", Nature.Timid);
        party.Add(p);
        var screen = new PoffinCaseScreen();
        screen.Open(poffins, party);
        screen.Confirm();
        Assert.Equal(CaseStep.Actions, screen.Step);
        screen.Confirm();
        Assert.Equal(CaseStep.Choose, screen.Step);
        screen.Confirm();
        Assert.Equal(CaseStep.Condition, screen.Step);
        screen.Confirm();
        Assert.Equal(CaseStep.Eating, screen.Step);
        // Timid likes sweet: (u8)(20 × 1.1f) = 22, and says so happily
        Assert.Equal((22, 30), (p.Cute, p.Sheen));
        Assert.Equal(PoffinTaste.Liked, screen.Taste);
        Assert.Contains("happily", screen.Prompt);
        Assert.Equal(new[] { 0, 0, 0, 0, 0, 0 }, screen.Before);
        Assert.Equal(0, poffins.Count);
        screen.Confirm();
        Assert.Equal(CaseStep.After, screen.Step);
        screen.Confirm();
        Assert.Equal(CaseStep.List, screen.Step);

        // A full sheen eats no more, and the Poffin stays in the case
        poffins.Add(Make(10, 0, 0, 0, 0));
        p.Sheen = 255;
        screen.Open(poffins, party);
        screen.Confirm(); screen.Confirm(); screen.Confirm(); screen.Confirm();
        Assert.Equal(CaseStep.Condition, screen.Step);
        Assert.Contains("won't eat", screen.Prompt);
        Assert.Equal(1, poffins.Count);

        // Thrown away
        screen.Cancel(); screen.Cancel();
        screen.Confirm();
        screen.MoveCursor(1);
        screen.Confirm();
        Assert.Equal(CaseStep.Trash, screen.Step);
        screen.MoveCursor(1);
        screen.Confirm();
        Assert.Equal(0, poffins.Count);
    }

    // ---------------------------------------------------------------- cooking at the Poffin House

    [Fact]
    public void ACookingGoesInStirsMakesAPoffinAndAsksForAnother()
    {
        var bag = new Inventory();
        bag.AddItem(Berry("Pecha Berry"), 2);
        var poffins = new PoffinCase();
        var screen = new PoffinCookingScreen();
        screen.Begin(Berry("Pecha Berry"), "Lucas", new Random(21), poffins, bag);
        Assert.Equal(CookingPhase.Pour, screen.Phase);
        Assert.Contains("Pecha Berry went in", screen.Line);
        screen.Advance(PoffinCookingScreen.PourTime);
        Assert.Equal(CookingPhase.Stir, screen.Phase);

        // A steady hand: the arrow's way, easing off before it spills over and stirring again before it burns
        for (int frame = 0; frame < 2000 && screen.Phase == CookingPhase.Stir; frame++)
        {
            var pot = screen.Pot!;
            int speed = Math.Abs(pot.Speed);
            screen.Turn(speed > 3000 ? 0 : pot.Backward ? -1 : 1);
            screen.Advance(1f / PoffinPot.FramesPerSecond);
        }
        Assert.Equal(CookingPhase.Finish, screen.Phase);
        Assert.Equal("Done!", screen.Line);
        Assert.True(screen.TakeCooked());
        Assert.False(screen.TakeCooked());
        var made = screen.Made!;
        Assert.Equal(PoffinType.Sweet, made.Type);
        Assert.True(screen.Pot!.Frames < 1800, $"{screen.Pot.Frames} frames");
        Assert.True(made.Level >= 9, $"level {made.Level}");
        Assert.Equal(0, screen.Pot.Spills);

        screen.Advance(PoffinCookingScreen.FinishTime);
        Assert.Equal(CookingPhase.Results, screen.Phase);
        Assert.Null(screen.Line);
        // Nothing goes on until the results have been shown
        screen.PressConfirm();
        Assert.Equal(CookingPhase.Results, screen.Phase);
        screen.Advance(PoffinCookingScreen.CardShown);
        screen.PressConfirm();
        Assert.Equal(CookingPhase.PutAway, screen.Phase);
        Assert.Equal(new[] { made }, poffins.All);
        Assert.Contains("put the Sweet Poffin away", screen.Line);
        screen.PressConfirm();
        Assert.Equal(CookingPhase.Again, screen.Phase);

        // Another: a berry is left, so the bag is wanted again
        screen.PressConfirm();
        Assert.Equal(CookingPhase.Done, screen.Phase);
        Assert.False(screen.IsActive);
        Assert.True(screen.WantsBerry);
    }

    [Fact]
    public void NoMoreCookingWithTheCaseFullOrNoBerryLeft()
    {
        var bag = new Inventory();
        var poffins = new PoffinCase();
        var screen = new PoffinCookingScreen();
        screen.Begin(Berry("Cheri Berry"), "Lucas", new Random(2), poffins, bag);
        Through(screen);
        screen.PressConfirm();
        Assert.Equal(CookingPhase.Notice, screen.Phase);
        Assert.Contains("no Berries", screen.Line);
        screen.Advance(PoffinCookingScreen.MessageWait);
        Assert.False(screen.IsActive);
        Assert.False(screen.WantsBerry);

        bag.AddItem(Berry("Cheri Berry"), 1);
        for (int i = poffins.Count; i < PoffinCase.Capacity - 1; i++) poffins.Add(Make(1, 0, 0, 0, 0));
        screen.Begin(Berry("Cheri Berry"), "Lucas", new Random(2), poffins, bag);
        Through(screen);
        Assert.True(poffins.IsFull);
        screen.PressConfirm();
        Assert.Contains("full", screen.Line);

        // No is no
        screen.Begin(Berry("Cheri Berry"), "Lucas", new Random(2), new PoffinCase(), bag);
        Through(screen);
        screen.MoveChoice(1);
        screen.PressConfirm();
        Assert.False(screen.IsActive);
        Assert.False(screen.WantsBerry);
    }

    // Lets a pot burn through its three stages and comes to "another?"
    private static void Through(PoffinCookingScreen screen)
    {
        for (int i = 0; i < 4000 && screen.Phase is CookingPhase.Pour or CookingPhase.Stir or CookingPhase.Finish; i++) screen.Advance(0.05f);
        Assert.Equal(CookingPhase.Results, screen.Phase);
        screen.Advance(PoffinCookingScreen.ResultsWait);
        Assert.Equal(CookingPhase.PutAway, screen.Phase);
        screen.Advance(PoffinCookingScreen.MessageWait);
        Assert.Equal(CookingPhase.Again, screen.Phase);
    }

    // ---------------------------------------------------------------- the save and the copy

    [Fact]
    public void TheConditionAndTheCaseAreSaved()
    {
        var p = Mon("Feebas", Nature.Mild);
        (p.Cool, p.Beauty, p.Cute, p.Smart, p.Tough, p.Sheen) = (1, 170, 3, 4, 5, 66);
        var back = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal((1, 170, 3, 4, 5, 66), (back.Cool, back.Beauty, back.Cute, back.Smart, back.Tough, back.Sheen));

        var copy = p.Clone();
        copy.Sheen = 0;
        copy.CopyStateFrom(p);
        Assert.Equal(66, copy.Sheen);

        var save = new SaveData { Poffins = new List<Poffin> { Make(60, 30, 30, 30, 30, smooth: 40), Make(0, 9, 0, 0, 3) } };
        var read = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        var poffins = new PoffinCase();
        poffins.Restore(read.Poffins);
        Assert.Equal(save.Poffins, poffins.All);
        poffins.Restore(null);
        Assert.Equal(0, poffins.Count);
    }

    [Fact]
    public void CharapsChatotComesWithTheTradeTablesCondition()
    {
        // charap_chatot.json: cool, beauty, cute, smart and tough 20; NPCTrade_CreateMon sets no sheen
        var chatot = NpcTrades.Make(NpcTrades.Get("charap")!, 20, new DateTime(2026, 10, 10));
        Assert.Equal((20, 20, 20, 20, 20, 0), (chatot.Cool, chatot.Beauty, chatot.Cute, chatot.Smart, chatot.Tough, chatot.Sheen));
    }

    // ---------------------------------------------------------------- the screens' doors into it

    [Fact]
    public void TheBagOpensThePoffinCase()
    {
        var item = Berry("Poffin Case");
        Assert.Equal(new[] { BagAction.Open, BagAction.Register, BagAction.Cancel }, BagScreen.ActionsFor(item));
        Assert.True(BagScreen.UsedInField(item));
    }

    [Fact]
    public void TheSummaryTurnsToTheConditionOnceTheContestHallIsVisited()
    {
        var party = new Party();
        party.Add(Mon("Turtwig", Nature.Hardy));
        var screen = new PartyScreen();
        screen.Open();
        screen.Confirm(party);
        screen.Confirm(party);
        Assert.True(screen.ShowSummary);
        screen.TurnSummaryPage(1);
        Assert.Equal(0, screen.SummaryPage);
        screen.ShowCondition = true;
        screen.TurnSummaryPage(1);
        Assert.Equal(1, screen.SummaryPage);
        screen.TurnSummaryPage(-1);
        Assert.Equal(0, screen.SummaryPage);
    }

    // ---------------------------------------------------------------- the scripts

    private static HeadlessScriptHost Run(string name, string place, HeadlessScriptHost host, NPC? subject = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, place)!, subject);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void ThePoffinCommandsAskTheCaseAndTheBag()
    {
        var library = ScriptLibrary.FromSources(("test", """
            script Check
              poffin check
            script Give
              poffin give 60 30 30 30 30 40
            script Room
              poffin room
            script Cook
              poffin cook
            """), ("common", ""));
        int Result(HeadlessScriptHost on, string script)
        {
            var runner = new ScriptRunner(library, on);
            runner.Start(library.All.First(s => s.Name == script), null);
            runner.RunToEnd();
            return runner.Result;
        }

        // CheckCanCookPoffin: no berry in the bag is 1; GivePoffin's RESULT is the Poffin's kind (Mild, 28); there is room
        var host = new HeadlessScriptHost();
        Assert.Equal(1, Result(host, "Check"));
        Assert.Equal((int)PoffinType.Mild, Result(host, "Give"));
        Assert.Equal(1, Result(host, "Room"));
        Assert.Equal(PoffinType.Mild, host.Poffins.All.Single().Type);
        Result(host, "Cook");
        Assert.Contains("open PoffinCooking", host.Log);

        // A berry and room: 0. A berry and a full case: 2, no room, and GivePoffin is POFFIN_NONE
        var full = new HeadlessScriptHost();
        full.Bag.AddItem(Berry("Oran Berry"), 1);
        Assert.Equal(0, Result(full, "Check"));
        for (int i = 0; i < PoffinCase.Capacity; i++) full.Poffins.Add(Make(1, 0, 0, 0, 0));
        Assert.Equal(2, Result(full, "Check"));
        Assert.Equal(0, Result(full, "Room"));
        Assert.Equal(0xFFFF, Result(full, "Give"));
    }

    [Fact]
    public void TheCookAsksForACaseAndABerryBeforeTheCookingOpens()
    {
        // No case
        var host = new HeadlessScriptHost();
        host.Answers.Enqueue(0);
        Run("Cook", "PoffinHouse", host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("need a Poffin Case"));
        Assert.DoesNotContain("open PoffinCooking", host.Log);

        // A case and no berry
        host = new HeadlessScriptHost();
        host.Bag.AddItem(Berry("Poffin Case"), 1);
        host.Answers.Enqueue(0);
        Run("Cook", "PoffinHouse", host);
        Assert.Contains(host.Transcript, l => l.Text.Contains("haven't a single Berry"));

        // Both: the cooking opens
        host = new HeadlessScriptHost();
        host.Bag.AddItem(Berry("Poffin Case"), 1);
        host.Bag.AddItem(Berry("Razz Berry"), 3);
        host.Answers.Enqueue(0);
        Run("Cook", "PoffinHouse", host);
        Assert.Contains("open PoffinCooking", host.Log);
    }

    [Fact]
    public void TheChairmanGivesThePoffinCaseOnce()
    {
        var host = Run("Chairman", "HearthomeFanClub", new HeadlessScriptHost());
        Assert.Equal(1, host.Bag.GetQuantity(Berry("Poffin Case")));
        Assert.True(host.Story.Has("FLAG_RECEIVED_HEARTHOME_CITY_POKEMON_FAN_CLUB_POFFIN_CASE"));
        var again = new HeadlessScriptHost();
        again.Story.Set("FLAG_RECEIVED_HEARTHOME_CITY_POKEMON_FAN_CLUB_POFFIN_CASE");
        Run("Chairman", "HearthomeFanClub", again);
        Assert.Equal(0, again.Bag.GetQuantity(Berry("Poffin Case")));
    }

    [Theory]
    [InlineData(255, "adores")]
    [InlineData(200, "very fond")]
    [InlineData(150, "quite friendly")]
    [InlineData(100, "warming")]
    [InlineData(50, "isn't quite used")]
    [InlineData(1, "wary")]
    [InlineData(0, "fed up")]
    public void TheFanClubsWomanReadsTheLeadsFriendship(int friendship, string said)
    {
        var host = new HeadlessScriptHost();
        var lead = Mon("Psyduck", Nature.Hardy);
        lead.Friendship = friendship;
        host.Party.Add(lead);
        Run("Beauty", "HearthomeFanClub", host);
        Assert.Contains(host.Transcript, l => l.Text.Contains(said));
    }

    [Fact]
    public void TheBoyInTheLobbyGivesAMildPoffinToAPlayerWithACase()
    {
        var map = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "ContestHallLobby.json")).ToMap();
        var boy = map.Everyone.Single(n => n.Key == "rich_boy");
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (16, 7), PlayerFacing = Direction.Up };
        host.Bag.AddItem(Berry("Poffin Case"), 1);
        Run("RichBoy", "ContestHallLobby", host, boy);
        var mild = host.Poffins.All.Single();
        // GivePoffin 60, 30, 30, 30, 30, 40: Mild, level 60
        Assert.Equal((PoffinType.Mild, 60, 40), (mild.Type, mild.Level, mild.Smoothness));
        Assert.True(host.Story.Has("FLAG_RECEIVED_CONTEST_HALL_LOBBY_MILD_POFFIN"));
        Assert.Equal((24, 6), (boy.GridX, boy.GridY));

        // Once only
        Run("RichBoy", "ContestHallLobby", host, boy);
        Assert.Single(host.Poffins.All);
        Assert.Contains(host.Transcript, l => l.Text.Contains("practice to win"));
    }

    [Fact]
    public void ABoyWhoSawNoCaseGivesThePoffinWhenThePlayerComesBackWithOne()
    {
        var map = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "ContestHallLobby.json")).ToMap();
        var boy = map.Everyone.Single(n => n.Key == "rich_boy");
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (16, 7), PlayerFacing = Direction.Up };
        Run("RichBoy", "ContestHallLobby", host, boy);
        Assert.Empty(host.Poffins.All);
        Assert.True(host.Story.Has("FLAG_TALKED_TO_CONTEST_HALL_LOBBY_RICH_BOY"));
        host.Bag.AddItem(Berry("Poffin Case"), 1);
        Run("RichBoy", "ContestHallLobby", host, boy);
        Assert.Equal(PoffinType.Mild, host.Poffins.All.Single().Type);
    }
}
