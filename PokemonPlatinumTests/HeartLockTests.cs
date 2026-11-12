using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Models.SuperContests;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Locked hearts (plan 44 · SD1; <see cref="HeartLock"/>, <c>BattleCore.Locks.cs</c>; docs/mechanics/rulings.md,
/// "Locked hearts"): the lock and its gauge, the reclaim in a trainer's battle, the held EXP, the sealed moves,
/// breaking loose and CALM, and every place that turns a locked Pokémon away.
/// </summary>
public class HeartLockTests
{
    private static Pokemon Locked(string species, int level, string id, params string[] moves)
    {
        var p = Of(Gender.Male, species, level, moves);
        p.Lock = HeartLock.Locked(id);
        return p;
    }

    /// <summary>A trainer's battle under the modern rules, both sides answered from outside, with or without the Reclaimer.</summary>
    private static BattleCore Lockwork(Pokemon[] mine, Pokemon[] theirs, bool reclaimer, BattleRandom? rolls = null, Ruleset? rules = null)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "lockwork_agent", Name = "Ivo", TrainerClass = "Ace Trainer", PrizeMoney = 600 };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = rolls ?? Calm(), Rules = rules ?? Ruleset.Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", Reclaimer = reclaimer
        });
        core.Start();
        return core;
    }

    private static BattleChoice Ball => BattleChoice.UseItem(Mine, "Poké Ball");

    /// <summary>Every event of a log, those a line carries with it too.</summary>
    private static List<BattleEvent> All(IEnumerable<BattleEvent> log) =>
        log.SelectMany(e => e is Said s ? new[] { e }.Concat(s.Shows).Concat(s.OnImpact) : new[] { e }).ToList();

    private static BattleRandom Catches() => Calm().Force(RollKind.CatchShake, 0).Force(RollKind.CriticalCapture, 255);

    [Fact]
    public void ABallIsRefusedWithoutTheReclaimerAndAtAPokemonWithNoLock()
    {
        var without = Lockwork(new[] { Of(Gender.Male, "Luxray", 40, "Spark") }, new[] { Locked("Gible", 20, "lock_test") }, reclaimer: false);
        Assert.Equal(Text.Of("battle.theTrainerBlockedThe"), without.WhyNot(Ball));

        var unlocked = Lockwork(new[] { Of(Gender.Male, "Luxray", 40, "Spark") }, new[] { Of(Gender.Male, "Gible", 20) }, reclaimer: true);
        Assert.Equal(Text.Of("battle.theTrainerBlockedThe"), unlocked.WhyNot(Ball));

        var with = Lockwork(new[] { Of(Gender.Male, "Luxray", 40, "Spark") }, new[] { Locked("Gible", 20, "lock_test") }, reclaimer: true);
        Assert.Null(with.WhyNot(Ball));

        // Platinum's rules know no locks: the ball is refused as ever
        var platinum = Lockwork(new[] { Of(Gender.Male, "Luxray", 40, "Spark") }, new[] { Locked("Gible", 20, "lock_test") }, reclaimer: true, rules: Ruleset.Platinum);
        Assert.Equal(Text.Of("battle.theTrainerBlockedThe"), platinum.WhyNot(Ball));
    }

    [Fact]
    public void AReclaimCatchesItAndTheTrainerSendsTheNext()
    {
        var gible = Locked("Gible", 20, "lock_test");
        var core = Lockwork(new[] { Of(Gender.Male, "Luxray", 40, "Spark") }, new[] { gible, Of(Gender.Male, "Shinx", 20, "Tackle") }, reclaimer: true, Catches());
        core.TakeLog();
        core.Submit(Ball, BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        Assert.Contains(All(log), e => e is Caught);
        Assert.Contains(Text.Of("battle.reclaimed", ("foe", "Gible")), Lines(log));
        Assert.Single(core.Reclaimed);
        Assert.Equal("Gible", core.Reclaimed[0].Species.Name);
        Assert.Equal(BattleResult.None, core.Result);
        // The trainer is asked for their next (here from outside, as the test answers for both sides)
        Assert.Equal(Foe, Assert.IsType<ReplacementRequest>(core.Request).Place);
        core.Submit(BattleChoice.Switch(Foe, 0));
        Assert.Equal("Shinx", core.At(Foe).Pokemon!.Species.Name);
    }

    [Fact]
    public void ReclaimingTheLastWinsTheBattleAndItsPrize()
    {
        var core = Lockwork(new[] { Of(Gender.Male, "Luxray", 40, "Spark") }, new[] { Locked("Gible", 20, "lock_test") }, reclaimer: true, Catches());
        core.Submit(Ball, BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Contains(All(core.Log), e => e is Won);
        Assert.Contains(Lines(core.Log), l => l == Text.Of("battle.receivedForWinning", ("player", "Lucas"), ("money", core.PrizeMoney)));
    }

    [Fact]
    public void AKnockedOutLockedPokemonIsMissedNotReclaimed()
    {
        var gible = Locked("Gible", 5, "lock_test");
        var core = Lockwork(new[] { Of(Gender.Male, "Luxray", 60, "Ice Fang") }, new[] { gible }, reclaimer: true);
        Turn(core);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Empty(core.Reclaimed);
        // The rules' own copy fell with its lock on: the game notes its id as missed
        var record = new HeartsRecord();
        record.Miss("lock_test");
        Assert.Equal(new[] { "lock_test" }, record.Missed);
        record.Reclaim("lock_test");
        Assert.Empty(record.Missed);
        Assert.Equal(new[] { "lock_test" }, record.Reclaimed);
    }

    [Fact]
    public void ALockedPokemonHoldsItsExpAndItIsGivenWhenItsHeartOpensUnderTheCap()
    {
        var gible = Locked("Gible", 10, "lock_test", "Tackle");
        int level = gible.Level;
        var core = Wild(gible, Of(Gender.Male, "Bidoof", 9), Calm().Force(RollKind.Lock, 99), Ruleset.Modern);
        for (int i = 0; i < 40 && core.Result == BattleResult.None; i++) core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        var mine = core.PlayerParty.Members[0];
        Assert.Contains(All(core.Log), e => e is ExpHeld);
        Assert.DoesNotContain(All(core.Log), e => e is ExpGained);
        Assert.Equal(level, mine.Level);
        Assert.True(mine.Lock!.HeldExp > 0);

        // The heart opens: its EXP given, no further than the cap, and its friendship its species' base
        mine.Lock = mine.Lock with { Gauge = 0, HeldExp = mine.Lock.HeldExp + 5000 };
        var opening = HeartLock.Open(mine, levelCap: 14);
        Assert.Null(mine.Lock);
        Assert.True(mine.Level > level);
        Assert.True(mine.Level <= 14);
        Assert.Equal(mine.Species.BaseFriendship, mine.Friendship);
        Assert.Equal(level, opening.LevelBefore);
    }

    [Fact]
    public void ASealedMoveIsRefusedUntilItsBarEmpties()
    {
        var gible = Locked("Gible", 20, "lock_test", "Tackle", "Sand Attack", "Dragon Rage");
        var core = Wild(gible, Of(Gender.Male, "Bidoof", 20), rules: Ruleset.Modern);
        Assert.Null(core.WhyNot(BattleChoice.Fight(Mine, 0)));
        Assert.Equal(Text.Of("battle.moveSealedByLock", ("pokemon", "Gible"), ("move", "Sand Attack")), core.WhyNot(BattleChoice.Fight(Mine, 1)));
        Assert.Equal(1, gible.Lock!.MovesBack);
        Assert.Equal(2, gible.Lock.Lower(20).MovesBack);
        Assert.Equal(4, gible.Lock.Lower(60).MovesBack);
        Assert.Equal(4, gible.Lock.Lower(99).MovesBack);
        Assert.Equal(5, gible.Lock.BarsLeft);
        Assert.Equal(1, gible.Lock.Lower(81).BarsLeft);
    }

    [Fact]
    public void ALockedPokemonBreaksLooseByItsRollAndCalmEndsIt()
    {
        var gible = Locked("Gible", 30, "lock_test", "Tackle");
        // The first roll breaks it loose, the second says it ignores the order
        var core = Wild(gible, Of(Gender.Male, "Bidoof", 30, "Growl"), Calm().Force(RollKind.Lock, 0, 1), Ruleset.Modern);
        core.TakeLog();
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        Assert.Contains(All(log), e => e is BrokeLoose);
        var said = Lines(log);
        Assert.Contains(Text.Of("battle.brokeLoose", ("user", "Gible")), said);
        Assert.Contains(Text.Of("battle.looseIgnores", ("user", "Gible")), said);
        // And it hurts itself at the turn's end
        Assert.Contains(Text.Of("battle.looseHurts", ("pokemon", "Gible")), said);

        int gauge = core.At(Mine).Pokemon!.Lock!.Gauge;
        Assert.Null(core.WhyNot(BattleChoice.Calm(Mine)));
        core.Submit(BattleChoice.Calm(Mine), BattleChoice.Fight(Foe, 0));
        var calmed = core.TakeLog();
        Assert.Contains(All(calmed), e => e is Calmed);
        Assert.Contains(Text.Of("battle.calmedDown", ("pokemon", "Gible")), Lines(calmed));
        Assert.Equal(gauge - HeartLock.Fall.Calm, core.At(Mine).Pokemon!.Lock!.Gauge);
        Assert.Equal(Text.Of("battle.nothingToCalm", ("pokemon", "Gible")), core.WhyNot(BattleChoice.Calm(Mine)));
    }

    [Fact]
    public void ALockShowsOnTheLineThatSendsItOut()
    {
        var core = Lockwork(new[] { Of(Gender.Male, "Luxray", 40, "Spark") }, new[] { Locked("Gible", 20, "lock_test") }, reclaimer: true);
        var shown = core.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<LockShown>().Single();
        Assert.Equal(Foe, shown.Place);
        Assert.Equal(5, shown.Bars);
    }

    [Fact]
    public void AReclaimBattleReplaysFromItsRecord()
    {
        var first = Lockwork(new[] { Of(Gender.Male, "Luxray", 40, "Spark") }, new[] { Locked("Gible", 20, "lock_test"), Of(Gender.Male, "Shinx", 15, "Tackle") },
            reclaimer: true, new BattleRandom(77));
        for (int i = 0; i < 30 && first.Result == BattleResult.None; i++)
        {
            if (first.Request is ReplacementRequest replace) { first.Submit(BattleChoice.Switch(replace.Place, 0)); continue; }
            var asked = Assert.IsType<ActionRequest>(first.Request).Places;
            var choices = new List<BattleChoice>();
            if (asked.Contains(Mine)) choices.Add(first.WhyNot(Ball) == null ? Ball : BattleChoice.Fight(Mine, 0));
            if (asked.Contains(Foe)) choices.Add(BattleChoice.Fight(Foe, 0));
            first.Submit(choices);
        }
        var record = first.Record!;
        Assert.True(record.Reclaimer);
        // A replay needs a battle not yet started: build one the same way, unstarted
        var replay = Unstarted(new[] { Of(Gender.Male, "Luxray", 40, "Spark") }, new[] { Locked("Gible", 20, "lock_test"), Of(Gender.Male, "Shinx", 15, "Tackle") }, new BattleRandom(77));
        replay.Replay(record);
        Assert.Equal(Lines(first.Log), Lines(replay.Log));
        Assert.Equal(first.Result, replay.Result);
    }

    private static BattleCore Unstarted(Pokemon[] mine, Pokemon[] theirs, BattleRandom rolls)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "lockwork_agent", Name = "Ivo", TrainerClass = "Ace Trainer", PrizeMoney = 600 };
        foreach (var p in theirs) trainer.Party.Add(p);
        return new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = rolls, Rules = Ruleset.Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", Reclaimer = true
        });
    }

    [Fact]
    public void ALockSurvivesASaveAndAnOlderSaveHasNone()
    {
        var p = Locked("Gible", 20, "lock_sinnoh_gible");
        p.Lock = p.Lock!.Lower(30).Hold(1234);
        var back = SavedPokemonData.FromPokemon(p).ToPokemon(Ruleset.Modern);
        Assert.Equal(p.Lock, back.Lock);

        var plain = SavedPokemonData.FromPokemon(Of(Gender.Male, "Gible", 20));
        Assert.Null(plain.Lock);
        Assert.DoesNotContain("\"lock\"", System.Text.Json.JsonSerializer.Serialize(plain, GameDataFiles.Json), System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EveryPlaceTurnsALockedPokemonAway()
    {
        var gible = Locked("Gible", 20, "lock_test", "Tackle", "Sand Attack");
        var party = new Party();
        party.Add(gible);
        party.Add(Of(Gender.Male, "Luxray", 40, "Spark"));

        Assert.Equal("locked", DayCare.WhyNot(party, 0));
        Assert.Equal("locked", ContestRules.WhyNot(gible, ContestStat.Cool, ContestRank.Normal));
        Assert.Equal(JobRefusal.Locked, new JobBoard().WhyNot(gible, 4));
        Assert.False(FrontierRules.MayEnter(gible));
        Assert.NotNull(HeartLock.WhyNot(gible, LockedUse.Athlon));
        Assert.False(MoveTeaching.CanLearn(gible, ItemDatabase.Get("TM26")!, Ruleset.Modern));
        Assert.Empty(MoveTutors.Teachable(gible, MoveTutors.TrainingHall));
        Assert.False(ItemUse.WouldHelp(ItemDatabase.Get("Rare Candy")!, gible));
        var ready = Locked("Gible", 30, "lock_test");
        Assert.Null(Evolution.Find(ready, EvolutionTrigger.LevelUp, new EvolutionContext()));
        Assert.False(gible.GainExp(100000, out _));
        int friendship = gible.Friendship;
        FriendshipRules.Apply(gible, FriendshipEvent.LevelUp);
        Assert.Equal(friendship, gible.Friendship);
        var saved = SavedPokemonData.FromPokemon(gible);
        saved.Id = System.Guid.NewGuid();
        Assert.Equal(HomeRefusal.Locked, HomeMoves.WhyNotLeave(new SaveData(), saved, trade: true));
        // An unlocked Pokémon is turned away by none of them
        Assert.Null(HeartLock.WhyNot(Of(Gender.Male, "Gible", 20), LockedUse.Trade));
    }

    [Fact]
    public void AGaugeEmptiesByStepsAloneWithinItsBound()
    {
        var p = Locked("Gible", 20, "lock_test");
        int steps = 0;
        while (p.Lock is { IsOpen: false })
        {
            steps += HeartLock.StepsPerFall;
            HeartLock.Lower(p, HeartLock.Fall.Steps);
            Assert.True(steps <= 8000, "a heart opens by steps alone within 8,000 steps");
        }
        Assert.Equal(6400, steps);
    }

    [Fact]
    public void ARelevelledTrainerKeepsItsLockedMemberAsItIs()
    {
        var trainer = new Trainer { Id = "lockwork_agent", Name = "Ivo", TrainerClass = "Ace Trainer" };
        trainer.Party.Add(Locked("Gible", 20, "lock_test"));
        var grown = trainer.Relevelled(level => level + 40, grow: true);
        Assert.Equal("Gible", grown.Party.Members[0].Species.Name);
        Assert.Equal(60, grown.Party.Members[0].Level);
        Assert.NotNull(grown.Party.Members[0].Lock);
        var mapped = trainer.WithTeamOf(p => Of(Gender.Male, "Bidoof", p.Level));
        Assert.Equal("lock_test", mapped.Party.Members[0].Lock!.Id);
    }

    /// <summary>
    /// The plan's "done when": a locked Pokémon is reclaimed from a trainer, goes into the team, fights with its EXP
    /// held, and opens its heart once its gauge empties, its held EXP given.
    /// </summary>
    [Fact]
    public void AReclaimedPokemonJoinsTheTeamAndOpensItsHeartWithItsExp()
    {
        var core = Lockwork(new[] { Of(Gender.Male, "Luxray", 40, "Spark") }, new[] { Locked("Gible", 12, "lock_sinnoh_gible", "Tackle") }, reclaimer: true, Catches());
        core.Submit(Ball, BattleChoice.Fight(Foe, 0));
        var gible = Assert.Single(core.Reclaimed);
        var team = new Party();
        team.Add(gible);
        Assert.Equal("lock_sinnoh_gible", gible.Lock!.Id);

        int level = gible.Level;
        var fight = Wild(gible, Of(Gender.Male, "Bidoof", 8), Calm().Force(RollKind.Lock, 99), Ruleset.Modern);
        for (int i = 0; i < 20 && fight.Result == BattleResult.None; i++) fight.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleResult.PlayerVictory, fight.Result);
        Assert.Equal(level, gible.Level);
        int held = gible.Lock!.HeldExp;
        Assert.True(held > 0);

        while (!gible.Lock!.IsOpen) HeartLock.Lower(gible, HeartLock.Fall.Battle);
        var opening = HeartLock.Open(gible);
        Assert.Null(gible.Lock);
        Assert.Equal("lock_sinnoh_gible", opening.LockId);
        Assert.True(gible.CurrentExp >= held);
    }
}
