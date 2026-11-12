using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Max Raid dens (plan 33 · GL3; <c>docs/mechanics/rulings.md</c>, "Max Raid dens"): a raid fills the player's side's four
/// places against one boss Dynamaxed for the whole battle; its barrier goes up past half its HP and breaks by the hits
/// the rule counts; at four and five stars it acts twice; the raiders' fourth fall loses it; it ends on the catch the
/// den's script throws; it replays from the Vs. Recorder's file; and it is the single it began as under Platinum's rules.
/// What a den holds is drawn by the day and never the dice.
/// </summary>
public class GalarRaidTests
{
    private static readonly DateTime Day = new(2027, 4, 1);

    private static List<Trainer> Allies(int level) => Raids.Allies(Raids.Den("rolling_1")!, 7, 1, level);

    private static BattleSetup Setup(int stars, int level, uint seed, string boss = "Onix", string mine = "Rillaboom", int mineLevel = 60, Ruleset? rules = null, int? allyLevel = null)
    {
        var party = new Party();
        party.Add(Mon(mine, mineLevel, "Wood Hammer", "Drum Beating", "Knock Off", "U-turn"));
        return new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = { Mon(boss, level, "Rock Slide", "Iron Tail", "Bind", "Screech") },
            Random = new BattleRandom(seed), Rules = rules ?? Ruleset.Modern, Format = BattleFormat.Raid,
            Raid = new RaidTerms(stars, Allies(allyLevel ?? level)), PlayerName = "Victor"
        };
    }

    private static BattleEngine Fought(BattleSetup setup)
    {
        var battle = new BattleEngine(setup);
        // The player's first move every turn, the raiders' and the boss's by the trainer AI
        for (int i = 0; i < 6000 && !battle.IsBattleOver; i++)
        {
            if (battle.HUD.MenuState == BattleMenuState.Message) battle.ConfirmMessage();
            else battle.SelectMove(0);
            battle.Update(1f / 60f);
        }
        Assert.True(battle.IsBattleOver, $"turn {battle.Core.Turn}, falls {battle.Core.RaidFalls}, waiting for {battle.Core.Request}");
        return battle;
    }

    private static List<string> Lines(BattleCore core) => core.Log.OfType<Said>().Select(s => s.Text).ToList();

    [Fact]
    public void ARaidFillsFourPlacesAgainstOneBossDynamaxedForTheWholeBattle()
    {
        var battle = Fought(Setup(4, 40, 33001, boss: "Steelix", mine: "Dubwool", mineLevel: 36));
        var core = battle.Core;
        Assert.True(core.IsRaid);
        Assert.Equal(4, core.PlayerSlots.Count);
        Assert.Single(core.EnemySlots);
        Assert.Null(core.PlayerSlots[0].Trainer);
        Assert.All(core.PlayerSlots.Skip(1), s => Assert.NotNull(s.Trainer));
        Assert.True(battle.IsBattleOver);
        var lines = Lines(core);
        Assert.Contains(lines, l => l == Text.Of("battle.raidBegins", ("pokemon", Text.Of("battle.foe", ("pokemon", "Steelix")))) || l.Contains("grew to a giant"));
        // The boss's Dynamax never runs down: no line ends it, however many turns the raid lasts
        Assert.DoesNotContain(lines, l => l.StartsWith(Text.Of("battle.dynamaxEnded", ("pokemon", "")).Trim()) && l.Contains("Steelix"));
        Assert.True(core.Turn > 3, $"the raid lasted {core.Turn} turns");
        // Nobody switches in a raid, and nothing comes out of the bag
        Assert.Equal(BattleCore.RaidSwitchRefusal, Text.Of("battle.raidNoSwitch"));
    }

    [Fact]
    public void TheBarrierGoesUpPastHalfAndBreaksByTheHitsTheRuleCounts()
    {
        Assert.Equal(0, RaidRules.Shields(2));
        Assert.Equal(2, RaidRules.Shields(3));
        Assert.Equal(4, RaidRules.Shields(5));
        Assert.Equal((1, 2), (RaidRules.BreaksOf(false), RaidRules.BreaksOf(true)));
        var battle = Fought(Setup(4, 36, 33002));
        var barriers = battle.Core.Log.OfType<Said>().SelectMany(s => s.Shows.OfType<RaidBarrier>()).Select(b => b.Shields).ToList();
        Assert.NotEmpty(barriers);
        // Up with three shields at four stars, then down a shield (two for a Max Move) at each hit until none stand
        Assert.Equal(RaidRules.Shields(4), barriers[0]);
        for (int i = 1; i < barriers.Count; i++) Assert.Contains(barriers[i - 1] - barriers[i], new[] { 1, 2, 3 });
        // and the boss falls only once its barrier is down
        Assert.Equal(BattleResult.PlayerVictory, battle.Result);
        Assert.Equal(0, barriers.Last());
    }

    [Fact]
    public void AFiveStarBossActsTwiceAndTheRaidersFourthFallLosesTheRaid()
    {
        Assert.True(RaidRules.ActsTwice(5, 1));
        Assert.True(RaidRules.ActsTwice(4, 2));
        Assert.False(RaidRules.ActsTwice(4, 1));
        Assert.False(RaidRules.ActsTwice(3, 2));
        // A weak raid party against a five-star boss: it acts again every turn, and the fourth fall blows them out
        var battle = Fought(Setup(5, 60, 33003, boss: "Steelix", mine: "Wooloo", mineLevel: 8, allyLevel: 8));
        var lines = Lines(battle.Core);
        Assert.Contains(lines, l => l.Contains("still raging"));
        Assert.Equal(BattleResult.PlayerDefeat, battle.Result);
        Assert.Equal(RaidRules.MostFalls, battle.Core.RaidFalls);
        Assert.Contains(Text.Of("battle.raidBlownOut"), lines);
        Assert.Contains(lines, l => l.Contains("back on its feet"));
    }

    [Fact]
    public void ARaidReplaysFromItsFileAndIsASingleUnderPlatinumsRules()
    {
        var battle = Fought(Setup(3, 30, 33004));
        var file = battle.RecordFile(Day)!;
        Assert.Equal(3, file.RaidStars);
        Assert.Equal(3, file.RaidAllies!.Count);
        var read = BattleRecordFile.Read(file.Serialize());
        var again = read.File!.Replay();
        Assert.Equal(battle.Result, again.Result);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));
        // Platinum's rules have no raids: the single it began as
        var platinum = new BattleEngine(Setup(3, 30, 33004, rules: Ruleset.Platinum));
        Assert.False(platinum.Core.IsRaid);
        Assert.Single(platinum.Core.PlayerSlots);
    }

    [Fact]
    public void ADensDayIsDrawnByTheDayAndItsScriptEndsOnTheCatch()
    {
        var den = Raids.Den("rolling_1")!;
        // The same day, the same den; the badges hold the stars down
        Assert.Equal(Raids.Today(den, 12345, 1, 0), Raids.Today(den, 12345, 1, 0));
        for (uint day = 0; day < 60; day++) Assert.InRange(Raids.Today(den, day, 1, 0).Stars, 1, 2);
        Assert.Contains(Enumerable.Range(0, 200).Select(d => Raids.Today(den, (uint)d, 1, 8).Stars), s => s >= 4);
        Assert.Contains(Enumerable.Range(0, 60).Select(d => Raids.Today(den, (uint)d, 1, 0).Lit), lit => lit);
        Assert.True(Raids.Today(den, 3, 1, 0, wished: true).Lit);
        // Every den's rows are species of the data
        foreach (var d in Raids.Galar.Dens)
            foreach (var row in d.Rows)
                Assert.True(PokemonDatabase.Get(row.Species) != null || PokemonDatabase.SpeciesOfForm(row.Species) != null, row.Species);

        // The den's script on a lit day: Watts, the raid won, a Poké Ball thrown that takes it
        uint lit = (uint)Enumerable.Range(0, 60).First(d => Raids.Today(den, (uint)d, 1, 0).Lit);
        var story = new StoryState();
        story.SetVar("VAR_GALAR_VERSION", 1);
        var party = new Party();
        party.Add(Mon("Grookey", 12, "Scratch"));
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Poké Ball")!, 3);
        var host = new HeadlessScriptHost(story, party, bag) { Region = RegionDatabase.Galar, DayNumber = lit };
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        host.Rolls.Enqueue(0);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("DenRolling1", "wild_area_rolling_fields")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        var today = Raids.Today(den, lit, 1, 0);
        Assert.Equal(Raids.WattsOf(today.Stars), story.Var(ScriptRunner.WattsVariable));
        Assert.Contains(host.Log, l => l.StartsWith("raidbattle "));
        Assert.Equal(2, party.Members.Count);
        Assert.Equal(today.Species, party.Members[1].Form ?? party.Members[1].Species.Name);
        Assert.Equal(2, bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        // The den is quiet once felled, and its Watts were taken
        var again = new HeadlessScriptHost(story, party, bag) { Region = RegionDatabase.Galar, DayNumber = lit };
        var second = new ScriptRunner(ScriptLibrary.Default, again);
        second.Start(ScriptLibrary.Default.Find("DenRolling1", "wild_area_rolling_fields")!, null);
        second.RunToEnd();
        Assert.Contains(again.Transcript, t => t.Text.Contains("No light rises"));
        Assert.Equal(Raids.WattsOf(today.Stars), story.Var(ScriptRunner.WattsVariable));
    }
}
