using System;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>The summary's trainer memo and the characteristic it shows (plan 08 · P3).</summary>
public class TrainerMemoTests
{
    private static Pokemon WithIvs(uint personality, int hp, int atk, int def, int spe, int spa, int spd)
    {
        var p = Of(Gender.Male, "Bidoof", 10);
        p.Personality = personality;
        (p.IvHP, p.IvAttack, p.IvDefense, p.IvSpeed, p.IvSpAttack, p.IvSpDefense) = (hp, atk, def, spe, spa, spd);
        return p;
    }

    [Fact]
    public void TheCharacteristicIsTheStatOfTheHighestIv()
    {
        Assert.Equal((StatType.Attack, 2), Characteristics.Of(WithIvs(0, 10, 27, 3, 4, 5, 6)));
        Assert.Equal((StatType.Speed, 4), Characteristics.Of(WithIvs(0, 10, 11, 3, 29, 5, 6)));
        Assert.Equal((StatType.SpDefense, 0), Characteristics.Of(WithIvs(0, 1, 2, 3, 4, 5, 30)));
    }

    [Fact]
    public void TheLineIsTheIvsRemainderByFive()
    {
        for (int iv = 0; iv <= 31; iv++)
            Assert.Equal(iv % 5, Characteristics.Of(WithIvs(0, iv, 0, 0, 0, 0, 0)).Line);
    }

    [Fact]
    public void TiesGoToTheFirstStatFromThePersonalitysRemainderByThree()
    {
        // Order: HP, Attack, Defense, Speed, Sp. Atk, Sp. Def; the search starts at personality % 6
        var all31 = new[] { 31, 31, 31, 31, 31, 31 };
        for (uint personality = 0; personality < 12; personality++)
        {
            var p = WithIvs(personality, 31, 31, 31, 31, 31, 31);
            Assert.Equal(Characteristics.Order[(int)(personality % 6)], Characteristics.Of(p).Stat);
        }
        // Only the later two tie for the top: from Attack onward Sp. Atk comes first, from Sp. Def alone it is Sp. Def
        Assert.Equal(StatType.SpAttack, Characteristics.Of(WithIvs(1, 3, 3, 3, 3, 20, 20)).Stat);
        Assert.Equal(StatType.SpDefense, Characteristics.Of(WithIvs(5, 3, 3, 3, 3, 20, 20)).Stat);
        Assert.Equal(6, all31.Length);
    }

    [Fact]
    public void EveryLineHasItsWordsInEveryLanguageOffered()
    {
        foreach (string tag in new[] { "en", "ro" })
        {
            var keys = Text.KeysOf(tag);
            foreach (var stat in Characteristics.Order)
                for (int line = 0; line < 5; line++)
                    Assert.Contains($"summary.characteristic.{stat}.{line}", keys);
        }
    }

    [Fact]
    public void TheMemoPageFollowsTheFirstAndTheOthersStayInTheirOrder()
    {
        var screen = new PartyScreen();
        Assert.Equal(new[] { PartyScreen.StatsPage, PartyScreen.MemoPage, PartyScreen.SkillsPage, PartyScreen.RibbonsPage }, screen.SummaryPages.ToArray());
        screen.ShowCondition = true;
        Assert.Equal(new[] { PartyScreen.StatsPage, PartyScreen.MemoPage, PartyScreen.SkillsPage, PartyScreen.ConditionPage, PartyScreen.RibbonsPage }, screen.SummaryPages.ToArray());
    }

    [Fact]
    public void ACatchRecordsWhereWhenAndAtWhatLevel()
    {
        var p = Of(Gender.Male, "Bidoof", 7);
        p.Met("Route 202", new DateTime(2026, 6, 1));
        Assert.Equal(("Route 202", 7, new DateTime(2026, 6, 1)), (p.MetLocation, p.MetLevel, p.MetDate));
        var copy = Of(Gender.Male, "Bidoof", 1);
        copy.CopyStateFrom(p);
        Assert.Equal(("Route 202", 7, new DateTime(2026, 6, 1)), (copy.MetLocation, copy.MetLevel, copy.MetDate));
    }
}
