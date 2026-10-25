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
/// Plan 13 · V3: the level cap by Badge and trainers scaled to the lead. Both off under a Normal run, where nothing
/// moves: the cap's number is 0 in a battle's setup, the scaled flag leaves a trainer the very object it was.
/// </summary>
public class RunLevelCapTests
{
    private static readonly RunRules Capped = new() { LevelCap = true };
    private static readonly RunRules Scaled = new() { ScaleTrainers = true };

    private static Trainer Made(string id)
    {
        var record = TrainerDatabase.Get(id) ?? throw new System.ArgumentException(id);
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        return trainer;
    }

    private static string[] Levels(Trainer t) => t.Party.Members.Select(p => $"{p.Species.Name} {p.Level}").ToArray();

    // ---- the table

    [Fact]
    public void TheCapIsTheNextLeadersBestPokemonAndFallsAwayWithTheStory()
    {
        var story = new StoryState();
        Assert.Equal(14, LevelCaps.For(Capped, "Sinnoh", story));
        string[] badges = { "Coal", "Forest", "Cobble", "Fen", "Relic", "Mine", "Icicle", "Beacon" };
        int[] caps = { 22, 26, 32, 37, 41, 44, 50, 62 };
        for (int i = 0; i < badges.Length; i++)
        {
            story.GiveBadge("Sinnoh", badges[i]);
            Assert.Equal(caps[i], LevelCaps.For(Capped, "Sinnoh", story));
        }
        // The Hall of Fame ends it
        story.CompleteRegion("Sinnoh");
        Assert.Null(LevelCaps.For(Capped, "Sinnoh", story));
    }

    [Fact]
    public void TheTableIsHeldToTheDataAndToTheLeagueTop()
    {
        string[] leaders = { "leader_roark", "leader_gardenia", "leader_fantina", "leader_maylene", "leader_wake", "leader_byron", "leader_candice", "leader_volkner", "champion_cynthia" };
        var table = LevelCaps.TableOf("Sinnoh")!;
        Assert.Equal(leaders.Length, table.Count);
        for (int i = 0; i < leaders.Length; i++)
            Assert.Equal(TrainerDatabase.Get(leaders[i])!.Party.Max(m => m.Level), table[i]);
    }

    [Fact]
    public void NothingIsCappedWithoutTheRuleOrATableOrOnceTheStoryIsDone()
    {
        var story = new StoryState();
        Assert.Null(LevelCaps.For(RunRules.Normal, "Sinnoh", story));
        Assert.Null(LevelCaps.For(null, "Sinnoh", story));
        Assert.Null(LevelCaps.For(RunRules.NuzlockePreset, "Sinnoh", story));
        // A region with no table is uncapped until its own plan writes one
        Assert.Null(LevelCaps.For(Capped, "Kanto", story));
        Assert.True(RunRules.Hardcore.LevelCap);
        Assert.Equal(14, LevelCaps.For(RunRules.Hardcore, "Sinnoh", story));
    }

    // ---- EXP in a battle

    private static BattleCore Fight(Pokemon mine, int cap, Ruleset? rules = null)
    {
        var foe = Mon("Onix", 100, "Harden");
        foe.CurrentHP = 1;
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = rules ?? Ruleset.Platinum,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", RunLevelCap = cap
        });
        core.Start();
        core.TakeLog();
        return core;
    }

    [Fact]
    public void ExpStopsAtTheStartOfTheCapsLevelAndSaysSoOnce()
    {
        var turtwig = Mon("Turtwig", 13, "Tackle");
        var core = Fight(turtwig, 14);
        var lines = Turn(core);
        var growth = turtwig.Species.GrowthRate;

        Assert.Equal(14, turtwig.Level);
        Assert.Equal(Pokemon.GetExpForLevel(14, growth), turtwig.CurrentExp);
        Assert.Contains("Turtwig grew to Lv. 14!", lines);
        Assert.Single(lines, l => l.Contains("can't grow past Lv. 14 yet"));
        // The line comes after the level's
        Assert.True(lines.FindIndex(l => l.Contains("grew to Lv. 14")) < lines.FindIndex(l => l.Contains("can't grow past")));
    }

    [Fact]
    public void APokemonOnTheCapsLevelGainsNothingAndNoGainIsTold()
    {
        var turtwig = Mon("Turtwig", 14, "Tackle");
        var before = turtwig.CurrentExp;
        var core = Fight(turtwig, 14);
        var lines = Turn(core);

        Assert.Equal(before, turtwig.CurrentExp);
        Assert.Equal(14, turtwig.Level);
        Assert.DoesNotContain(lines, l => l.Contains("gained"));
        Assert.Empty(core.Log.OfType<ExpGained>());
        Assert.Single(lines, l => l.Contains("can't grow past Lv. 14 yet"));
    }

    [Fact]
    public void WithTheBadgeItGoesOnAndWithNoCapNothingIsSaid()
    {
        var capped = Mon("Turtwig", 14, "Tackle");
        Turn(Fight(capped, 14));
        var freed = Mon("Turtwig", 14, "Tackle");
        var lines = Turn(Fight(freed, 22));
        Assert.True(freed.Level > 14);
        Assert.Contains(lines, l => l.Contains("gained"));
        Assert.DoesNotContain(lines, l => l.Contains("can't grow past"));

        var none = Mon("Turtwig", 14, "Tackle");
        var uncapped = Turn(Fight(none, 0));
        Assert.Equal(freed.CurrentExp, none.CurrentExp);
        Assert.DoesNotContain(uncapped, l => l.Contains("can't grow past"));
    }

    [Fact]
    public void ACapAboveTheGainChangesNothingAndTheModernRulesCapToo()
    {
        foreach (var rules in new[] { Ruleset.Platinum, Ruleset.Modern })
        {
            var a = Mon("Turtwig", 13, "Tackle");
            var b = Mon("Turtwig", 13, "Tackle");
            Turn(Fight(a, 0, rules));
            Turn(Fight(b, 100, rules));
            Assert.Equal(a.CurrentExp, b.CurrentExp);
            Assert.Equal(a.Level, b.Level);

            var c = Mon("Turtwig", 13, "Tackle");
            var lines = Turn(Fight(c, 14, rules));
            Assert.Equal(14, c.Level);
            Assert.Equal(Pokemon.GetExpForLevel(14, c.Species.GrowthRate), c.CurrentExp);
            Assert.Single(lines, l => l.Contains("can't grow past Lv. 14 yet"));
        }
    }

    [Fact]
    public void ARecordKeepsTheCapAndAReplayUnderAnotherIsRefused()
    {
        var record = new BattleRecord(1, new List<IReadOnlyList<BattleChoice>>(), false, false, Difficulty.Normal, 14);
        var party = new Party();
        party.Add(Mon("Turtwig", 5, "Tackle"));
        var uncapped = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Mon("Starly", 3) }, Random = new BattleRandom(1), Rules = Ruleset.Platinum,
            PlayerController = null, EnemyController = null
        });
        Assert.Throws<System.ArgumentException>(() => uncapped.Replay(record));
    }

    // ---- the candy

    [Fact]
    public void ARareCandyRefusesAtTheCapAndSaysWhy()
    {
        var candy = ItemDatabase.Get("Rare Candy")!;
        var turtwig = Mon("Turtwig", 14, "Tackle");
        Assert.True(ItemUse.WouldHelp(candy, turtwig));
        Assert.False(ItemUse.WouldHelp(candy, turtwig, runCap: 14));
        var refused = ItemUse.Apply(candy, turtwig, runCap: 14);
        Assert.False(refused.Applied);
        Assert.Contains("14", refused.Message);
        Assert.Equal(14, turtwig.Level);

        var below = Mon("Turtwig", 13, "Tackle");
        Assert.True(ItemUse.WouldHelp(candy, below, runCap: 14));
        Assert.True(ItemUse.Apply(candy, below, runCap: 14).Applied);
        Assert.Equal(14, below.Level);
        // And it stops there
        Assert.False(ItemUse.Apply(candy, below, runCap: 14).Applied);
        // With no cap it goes on
        Assert.True(ItemUse.Apply(candy, below).Applied);
        Assert.Equal(15, below.Level);
    }

    // ---- trainers scaled to the lead

    [Fact]
    public void ScalingKeepsTheSpreadAndMeetsTheLeadAtItsBest()
    {
        var roark = Made("leader_roark");
        var scaled = DifficultyRules.Prepare(roark, Scaled, 30);
        Assert.NotSame(roark, scaled);
        // 12, 12 and 14 shifted by sixteen: the best is the lead's level, the spread (two levels) is kept
        Assert.Equal(new[] { "Geodude 28", "Onix 28", "Cranidos 30" }, Levels(scaled));
        Assert.Equal(new[] { "Geodude 12", "Onix 12", "Cranidos 14" }, Levels(roark));
        // Nothing else of him moves
        Assert.Equal(roark.PrizeMoney, scaled.PrizeMoney);
        Assert.Equal(roark.Ai, scaled.Ai);
        Assert.All(scaled.Party.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
    }

    [Fact]
    public void AScaledYoungsterMeetsALevelThirtyLeadAtThirty()
    {
        var youngster = Made("youngster_tristan");
        var scaled = DifficultyRules.Prepare(youngster, Scaled, 30);
        Assert.Equal(30, scaled.Party.Members.Max(p => p.Level));
        int spread = youngster.Party.Members.Max(p => p.Level) - youngster.Party.Members.Min(p => p.Level);
        Assert.Equal(spread, scaled.Party.Members.Max(p => p.Level) - scaled.Party.Members.Min(p => p.Level));
    }

    [Fact]
    public void ScalingGoesDownToo()
    {
        var roark = Made("leader_roark");
        var scaled = DifficultyRules.Prepare(roark, Scaled, 8);
        Assert.Equal(8, scaled.Party.Members.Max(p => p.Level));
        Assert.Equal(new[] { 6, 6, 8 }, scaled.Party.Members.Select(p => p.Level));
    }

    [Fact]
    public void ScalingIsOffWithoutTheRuleOrALeadAndTheDifficultyLiesOnTopOfIt()
    {
        var roark = Made("leader_roark");
        Assert.Same(roark, DifficultyRules.Prepare(roark, RunRules.Normal, 30));
        Assert.Same(roark, DifficultyRules.Prepare(roark, Scaled, 0));
        Assert.Same(roark, DifficultyRules.Prepare(roark, new RunRules { LevelCap = true }, 30));

        // Challenge on a scaled team: the lead's level and then the table's four
        var hard = DifficultyRules.Prepare(roark, new RunRules { ScaleTrainers = true, Difficulty = Difficulty.Challenge }, 30);
        Assert.Equal(34, hard.Party.Members.Max(p => p.Level));
        // Easy on a scaled team: the lead's level and then the table's four fewer
        var soft = DifficultyRules.Prepare(roark, new RunRules { ScaleTrainers = true, Difficulty = Difficulty.Easy }, 30);
        Assert.Equal(26, soft.Party.Members.Max(p => p.Level));
    }
}
