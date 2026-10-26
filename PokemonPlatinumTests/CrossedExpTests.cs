using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 18 · W4b-3 (Design C, approved 2026-10-26): a region the game crossed into gives half as much EXP again, and a
/// soft ceiling at 110% of its next milestone's mapped level stops EXP (never a level) in every difficulty, the run's own
/// cap winning where it is lower. The numbers are <c>docs/plans/W4b-levelling-the-world.md</c>'s table under Design C.
/// </summary>
public class CrossedExpTests
{
    /// <summary>Each built region's ceiling by Badges held, 0 to 8, and after its Hall of Fame (the plan's table).</summary>
    public static IEnumerable<object[]> Ceilings() => new[]
    {
        new object[] { RegionDatabase.Kanto, new[] { 77, 82, 84, 89, 99, 99, 102, 104, 114 }, 132 },
        new object[] { RegionDatabase.Johto, new[] { 122, 125, 126, 130, 135, 137, 138, 143, 149 }, 171 },
        new object[] { RegionDatabase.Hoenn, new[] { 156, 158, 160, 163, 163, 165, 170, 171, 178 }, 201 },
        new object[] { RegionDatabase.Unova, new[] { 182, 184, 188, 190, 192, 194, 199, 200, 203 }, 223 },
    };

    [Theory]
    [MemberData(nameof(Ceilings))]
    public void TheCeilingIsATenthOverTheNextMilestoneAndThePostGamesAfterTheHallOfFame(string region, int[] byBadges, int afterHallOfFame)
    {
        var story = new StoryState();
        var badges = RegionDatabase.Get(region)!.BadgeNames;
        Assert.Equal(8, badges.Count);
        Assert.Equal(byBadges[0], LevelCaps.Ceiling(region, story));
        for (int i = 0; i < 8; i++)
        {
            story.GiveBadge(region, badges[i]);
            Assert.Equal(byBadges[i + 1], LevelCaps.Ceiling(region, story));
        }
        story.CompleteRegion(region);
        Assert.Equal(afterHallOfFame, LevelCaps.Ceiling(region, story));
        // Never past the game's highest level
        Assert.True(afterHallOfFame <= Ruleset.HighestLevel);
    }

    [Fact]
    public void SinnohAndARegionWithNoTableHaveNoCeiling()
    {
        var story = new StoryState();
        Assert.Null(LevelCaps.Ceiling(RegionDatabase.Sinnoh, story));
        Assert.Null(LevelCaps.Ceiling("Kalos", story));
    }

    [Fact]
    public void TheLowerCapWinsAndNoneLeavesTheOther()
    {
        Assert.Null(LevelCaps.Lower(null, null));
        Assert.Equal(77, LevelCaps.Lower(null, 77));
        Assert.Equal(70, LevelCaps.Lower(70, null));
        // A capped run in Kanto with no Badge: the run's 70 under the ceiling's 77
        var story = new StoryState();
        var run = LevelCaps.For(new RunRules { LevelCap = true }, RegionDatabase.Kanto, story, crossed: true);
        Assert.Equal(70, LevelCaps.Lower(run, LevelCaps.Ceiling(RegionDatabase.Kanto, story)));
        // After the Hall of Fame the run's cap falls away and the ceiling stays
        story.CompleteRegion(RegionDatabase.Kanto);
        run = LevelCaps.For(new RunRules { LevelCap = true }, RegionDatabase.Kanto, story, crossed: true);
        Assert.Equal(132, LevelCaps.Lower(run, LevelCaps.Ceiling(RegionDatabase.Kanto, story)));
    }

    // ---- the bonus in a battle

    private static (BattleCore Core, Pokemon Mine) Fight(string species, int level, bool crossed, Difficulty difficulty = Difficulty.Normal, int cap = 0, Ruleset? rules = null)
    {
        var mine = Mon(species, level, "Tackle");
        var foe = Mon("Onix", 40, "Harden");
        foe.CurrentHP = 1;
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = rules ?? Ruleset.Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", CrossedExp = crossed, Difficulty = difficulty, RunLevelCap = cap
        });
        core.Start();
        core.TakeLog();
        Turn(core);
        return (core, mine);
    }

    private static int Gained(BattleCore core) => core.Log.OfType<ExpGained>().Sum(e => e.Amount);

    [Fact]
    public void ACrossedRegionsFoesGiveHalfAsMuchAgainUnderEitherRules()
    {
        foreach (var rules in new[] { Ruleset.Platinum, Ruleset.Modern })
        {
            int plain = Gained(Fight("Turtwig", 30, crossed: false, rules: rules).Core);
            int crossed = Gained(Fight("Turtwig", 30, crossed: true, rules: rules).Core);
            Assert.True(plain > 0);
            Assert.Equal(plain * 3 / 2, crossed);
            Assert.Equal(crossed, LevelCaps.CrossedExp(plain));
        }
    }

    [Fact]
    public void TheBonusStacksWithEasysThreeHalves()
    {
        int plain = Gained(Fight("Turtwig", 30, crossed: false).Core);
        int easy = Gained(Fight("Turtwig", 30, crossed: true, difficulty: Difficulty.Easy).Core);
        Assert.Equal(DifficultyRules.ScaleExp(plain * 3 / 2, Difficulty.Easy), easy);
        Assert.True(easy > plain * 2);
    }

    [Fact]
    public void ThePokemonOverTheCeilingKeepsItsLevelAndGainsNothing()
    {
        // Arriving in Hoenn at 171 against a ceiling of 156: it fights at 171 and doesn't grow
        var (core, mine) = Fight("Turtwig", 171, crossed: true, cap: 156);
        Assert.Equal(171, mine.Level);
        Assert.Equal(0, Gained(core));
        Assert.True(LevelCaps.Holds(mine, 156));
    }

    [Fact]
    public void ARecordAndItsFileKeepTheBonusAndAReplayWithoutItIsRefused()
    {
        var (core, _) = Fight("Turtwig", 30, crossed: true);
        Assert.True(core.Record!.CrossedExp);

        var party = new Party();
        party.Add(Mon("Turtwig", 5, "Tackle"));
        var setup = new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Mon("Starly", 3) }, Random = new BattleRandom(1), Rules = Ruleset.Platinum,
            PlayerController = null, EnemyController = null, CrossedExp = true
        };
        var file = BattleRecordFile.Opening(setup);
        Assert.True(file.CrossedExp);
        Assert.True(file.Record.CrossedExp);
        Assert.True(file.ToSetup().CrossedExp);

        var plainParty = new Party();
        plainParty.Add(Mon("Turtwig", 5, "Tackle"));
        var plain = new BattleCore(new CoreSetup
        {
            PlayerParty = plainParty, WildPokemon = new List<Pokemon> { Mon("Starly", 3) }, Random = new BattleRandom(1), Rules = Ruleset.Platinum,
            PlayerController = null, EnemyController = null
        });
        var record = new BattleRecord(1, new List<IReadOnlyList<BattleChoice>>(), CrossedExp: true);
        Assert.Throws<ArgumentException>(() => plain.Replay(record));
    }

    // ---- the Day Care

    [Fact]
    public void TheDayCaresStepsStopAtTheCapAndItsFeeCountsOnlyTheLevelsGrownUnderIt()
    {
        var party = new Party();
        var bidoof = Mon("Bidoof", 5, "Tackle");
        party.Add(bidoof);
        party.Add(Mon("Shinx", 5, "Tackle"));
        party.Add(Mon("Shinx", 5, "Tackle"));
        var dayCare = new DayCare { LevelCap = 6 };
        Assert.True(dayCare.Leave(party, 0));
        var day = new DateTime(2026, 6, 3);
        // 300 steps would bring Bidoof (Medium Fast, 125 EXP) to 425, Lv. 7; held at 6 it stops at 216
        for (int i = 0; i < 300; i++) dayCare.Step(party, new Random(1), day, Ruleset.Platinum);
        Assert.Equal((6, 1, 200), (dayCare.LevelNow(0), dayCare.LevelsGained(0), dayCare.Fee(0)));
        dayCare.TakeBack(party, 0);
        Assert.Equal((6, Pokemon.GetExpForLevel(6, bidoof.Species.GrowthRate)), (bidoof.Level, bidoof.CurrentExp));
    }

    [Fact]
    public void ADayCareWithNoCapGivesEveryStep()
    {
        var party = new Party();
        var bidoof = Mon("Bidoof", 5, "Tackle");
        party.Add(bidoof);
        party.Add(Mon("Shinx", 5, "Tackle"));
        var dayCare = new DayCare();
        Assert.True(dayCare.Leave(party, 0));
        for (int i = 0; i < 300; i++) dayCare.Step(party, new Random(1), new DateTime(2026, 6, 3), Ruleset.Platinum);
        Assert.Equal(7, dayCare.LevelNow(0));
    }
}
