using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Staged battles (docs/mechanics/rulings.md, "Staged battles"): a foe whose level is drawn as ???, that nothing can
/// touch, that only watches, and a battle that ends by itself, each a term of its own; and with none of them nothing
/// changes.
/// </summary>
public class StagedBattleTests
{
    private static BattleCore Staged(Pokemon mine, Pokemon foe, bool hide = false, bool untouchable = false, bool passive = false, int endAfter = 0)
    {
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = Ruleset.Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas",
            HideLevel = hide, Untouchable = untouchable, Passive = passive, EndAfterTurns = endAfter
        });
        core.Start();
        return core;
    }

    private static Pokemon Foe() => Of(Gender.Male, "Zacian", 70, "Tackle");

    [Fact]
    public void AMoveAimedAtAnUntouchableFoeFailsAndDoesNothing()
    {
        var mine = Of(Gender.Male, "Garchomp", 60, "Double-Edge", "Thunder Wave", "Swords Dance");
        var foe = Foe();
        var core = Staged(mine, foe, untouchable: true);
        int before = foe.CurrentHP, mineBefore = mine.CurrentHP;

        // A damaging move with recoil: said, paid for, and no damage either way
        var said = Turn(core, mine: 0, foe: 0);
        Assert.Contains(Text.Of("battle.cantBeTouched"), said);
        var rulesFoe = core.EnemySlots[0].Pokemon!;
        Assert.Equal(before, rulesFoe.CurrentHP);
        Assert.Equal(mine.Moves[0].MaxPP - 1, core.PlayerSlots[0].Pokemon!.Moves[0].CurrentPP);
        int afterFirst = core.PlayerSlots[0].Pokemon!.CurrentHP;
        Assert.True(afterFirst < mineBefore, "the foe isn't passive here, so its Tackle lands");

        // A status move aimed at it gives nothing
        said = Turn(core, mine: 1, foe: 0);
        Assert.Contains(Text.Of("battle.cantBeTouched"), said);
        Assert.Equal(StatusCondition.None, rulesFoe.Status);

        // A move aimed at the user works as ever
        said = Turn(core, mine: 2, foe: 0);
        Assert.DoesNotContain(Text.Of("battle.cantBeTouched"), said);
        Assert.Equal(2, core.PlayerSlots[0].Pokemon!.StatStages.GetValueOrDefault(StatType.Attack));
    }

    [Fact]
    public void NothingReachesAnUntouchableFoeByAnotherWay()
    {
        // A sandstorm the player's Tyranitar raises, a Perish Song and a Future Sight: the foe loses nothing and is doomed to nothing
        var mine = With(Of(Gender.Male, "Tyranitar", 70, "Perish Song", "Future Sight", "Protect", "Swords Dance"), ability: "Sand Stream");
        var foe = Of(Gender.Male, "Zacian", 70);
        var core = Staged(mine, foe, untouchable: true, passive: true);
        int before = core.EnemySlots[0].Pokemon!.CurrentHP;
        Turn(core, mine: 0);
        Turn(core, mine: 1);
        for (int turn = 0; turn < 4 && core.Result == BattleResult.None; turn++) Turn(core, mine: turn % 2 == 0 ? 2 : 3);
        Assert.Equal(before, core.EnemySlots[0].Pokemon!.CurrentHP);
        Assert.False(core.EnemySlots[0].Pokemon!.IsFainted);
        Assert.NotEqual(BattleResult.PlayerVictory, core.Result);
    }

    [Fact]
    public void APassiveFoeChoosesNothingAndOnlyWatches()
    {
        var mine = Of(Gender.Male, "Bidoof", 10, "Growl");
        var core = Staged(mine, Foe(), passive: true);
        int hp = mine.CurrentHP;
        for (int turn = 0; turn < 3; turn++)
        {
            // Nobody asks the foe what it does
            Assert.Equal(new[] { Mine }, Assert.IsType<ActionRequest>(core.Request).Places);
            core.Submit(new[] { BattleChoice.Fight(Mine, 0) });
            var said = Lines(core.TakeLog());
            Assert.Single(said, l => l == Text.Of("battle.onlyWatches"));
        }
        Assert.Equal(hp, core.PlayerSlots[0].Pokemon!.CurrentHP);
        Assert.Equal(BattleResult.None, core.Result);
    }

    [Fact]
    public void AStagedBattleEndsByItselfAfterItsTurnsAsAFlight()
    {
        var mine = Of(Gender.Male, "Bidoof", 10, "Growl");
        var core = Staged(mine, Foe(), hide: true, untouchable: true, passive: true, endAfter: 3);
        for (int turn = 0; turn < 2; turn++)
        {
            core.Submit(new[] { BattleChoice.Fight(Mine, 0) });
            Assert.Equal(BattleResult.None, core.Result);
        }
        core.Submit(new[] { BattleChoice.Fight(Mine, 0) });
        Assert.Equal(BattleResult.EnemyFled, core.Result);
        Assert.Contains(Text.Of("battle.stagedLeaves", ("pokemon", core.EnemySlots[0].Name)), Lines(core.Log));
        Assert.Equal(3, core.Turn);
    }

    [Fact]
    public void AStagedFoeCantBeCaughtAndGivesNoExp()
    {
        var mine = Of(Gender.Male, "Garchomp", 80, "Earthquake");
        var foe = Of(Gender.Male, "Bidoof", 5, "Growl");
        var core = Staged(mine, foe, hide: true);
        Assert.True(core.Staged);
        Assert.Equal(BattleCore.StagedBallRefusal, core.WhyNot(BattleChoice.UseItem(Mine, "Master Ball")));
        int exp = core.PlayerSlots[0].Pokemon!.CurrentExp;
        Turn(core, mine: 0, foe: 0);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Equal(exp, core.PlayerSlots[0].Pokemon!.CurrentExp);
        // The rules fight at the real level: the level is the screen's alone
        Assert.Equal(5, core.EnemySlots[0].Pokemon!.Level);
    }

    [Fact]
    public void WithNoTermsNothingIsStaged()
    {
        Assert.False(new BattleTerms().Staged);
        Assert.False(new BattleTerms(Inverse: true, NoBattleItems: true).Staged);
        var mine = Of(Gender.Male, "Garchomp", 80, "Earthquake");
        var core = Staged(mine, Of(Gender.Male, "Bidoof", 5, "Growl"));
        Assert.False(core.Staged);
        Assert.Null(core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
        // The same battle with no terms and one made the old way say the same, line for line
        var plain = Wild(Of(Gender.Male, "Garchomp", 80, "Earthquake"), Of(Gender.Male, "Bidoof", 5, "Growl"), rules: Ruleset.Modern);
        Turn(core, 0, 0);
        Turn(plain, 0, 0);
        Assert.Equal(Golden.BattleText.Written(plain), Golden.BattleText.Written(core));
    }

    // The game's own face of a staged trainer's battle, with nothing fixed but the seed
    private static BattleEngine Face(bool hide, bool untouchable, bool passive, int endAfter, uint seed = 20261112)
    {
        var mine = new Party();
        mine.Add(Scenario.Mon("Monferno", 20, "Ember", "Mach Punch"));
        var trainer = new Trainer { Id = "staged_test", Name = "Mara", TrainerClass = "Ace Trainer", PrizeMoney = 64 };
        trainer.Party.Add(Scenario.Mon("Bidoof", 18, "Tackle"));
        var pokedex = new Pokedex();
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = new Inventory(), Pokedex = pokedex, Trainers = { trainer },
            Random = new BattleRandom(seed), Rules = Ruleset.Modern, PlayerName = "Lucas",
            HideLevel = hide, Untouchable = untouchable, Passive = passive, EndAfterTurns = endAfter
        });
        Scenario.Settle(battle);
        for (int turn = 0; turn < 20 && !battle.IsBattleOver; turn++)
        {
            battle.SelectMove(0);
            Scenario.Settle(battle);
        }
        return battle;
    }

    [Fact]
    public void AHiddenLevelIsTheFoesAloneOnEveryCardThatShowsIt()
    {
        var mine = new Party();
        mine.Add(Scenario.Mon("Monferno", 20, "Ember"));
        var foe = Scenario.Mon("Bidoof", 18, "Tackle");
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = { foe },
            Random = new BattleRandom(5), Rules = Ruleset.Modern, PlayerName = "Lucas", HideLevel = true
        });
        Scenario.Settle(battle);
        Assert.True(battle.HidesLevel(BattleSide.Enemy));
        Assert.False(battle.HidesLevel(BattleSide.Player));
        Assert.Null(battle.ShownLevel(battle.EnemySlots[0]));
        Assert.Equal(20, battle.ShownLevel(battle.PlayerSlots[0]));
        Assert.True(battle.Anim[BattleSide.Enemy, 0].LevelHidden);
        Assert.False(battle.Anim[BattleSide.Player, 0].LevelHidden);
        var info = battle.Info;
        Assert.All(info.Side(BattleSide.Enemy).Pokemon, card => Assert.True(card.LevelHidden));
        Assert.All(info.Side(BattleSide.Player).Pokemon, card => Assert.False(card.LevelHidden));
        // The Pokédex is told nothing of a staged foe
        Assert.False(battle.Pokedex.IsSeen(foe.Species.DexNumber));
        Assert.Equal("???", Text.Of("battle.hiddenLevel"));
    }

    [Fact]
    public void AStagedBattlesFileKeepsItsTermsAndReplaysTheSame()
    {
        var battle = Face(hide: true, untouchable: true, passive: true, endAfter: 2);
        Assert.Equal(BattleResult.EnemyFled, battle.Result);
        var file = battle.RecordFile(new DateTime(2026, 11, 12))!;
        Assert.True(file.HideLevel && file.Untouchable && file.Passive);
        Assert.Equal(2, file.EndAfterTurns);
        var text = file.Serialize();
        var back = BattleRecordFile.Read(text).File!;
        Assert.True(back.HideLevel && back.Untouchable && back.Passive);
        var again = back.Replay();
        Assert.Equal(battle.Result, again.Result);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));

        // A file of a battle with none of the terms says nothing of them
        var plain = Face(hide: false, untouchable: false, passive: false, endAfter: 0);
        string plainText = plain.RecordFile(new DateTime(2026, 11, 12))!.Serialize();
        foreach (var word in new[] { "hideLevel", "untouchable", "passive", "endAfterTurns" })
            Assert.DoesNotContain(word, plainText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AScriptsBattleLinesCarryTheTerms()
    {
        var wild = ScriptParser.Parse("test", "script S\n wildbattle \"Zacian\" 70 hidelevel untouchable passive endafter 3")[0].Everything().Single(i => i.Op == Op.WildBattle);
        Assert.Equal(new BattleTerms(HideLevel: true, Untouchable: true, Passive: true, EndAfterTurns: 3), wild.Staged);
        // A staged wild battle can't be run from unless the line says so
        Assert.True(wild.Option);
        var mayRun = ScriptParser.Parse("test", "script S\n wildbattle \"Zacian\" 260 hidelevel mayrun canlose")[0].Everything().Single(i => i.Op == Op.WildBattle);
        Assert.False(mayRun.Option);
        Assert.True(mayRun.CanLose);
        Assert.Equal(260, mayRun.Number);

        var trainer = ScriptParser.Parse("test", "script S\n battle self untouchable canlose")[0].Everything().Single(i => i.Op == Op.Battle);
        Assert.Equal(new BattleTerms(Untouchable: true), trainer.Staged);
        Assert.Null(ScriptParser.Parse("test", "script S\n battle self canlose")[0].Everything().Single(i => i.Op == Op.Battle).Staged);
        Assert.Null(ScriptParser.Parse("test", "script S\n wildbattle \"Starly\" 2")[0].Everything().Single(i => i.Op == Op.WildBattle).Staged);

        // What a line can't say
        Assert.ThrowsAny<Exception>(() => ScriptParser.Parse("test", "script S\n wildbattle \"Zacian\" 150"));
        Assert.ThrowsAny<Exception>(() => ScriptParser.Parse("test", "script S\n wildbattle \"Starly\" 2 mayrun"));
        Assert.ThrowsAny<Exception>(() => ScriptParser.Parse("test", "script S\n wildbattle \"Zacian\" 70 passive passive"));
        Assert.ThrowsAny<Exception>(() => ScriptParser.Parse("test", "script S\n wildbattle \"Zacian\" 70 endafter 0"));
    }

    [Fact]
    public void AHeadlessHostWritesTheTermsAndGoesOnFromALoss()
    {
        var library = ScriptLibrary.FromSources(("test", "script S\n wildbattle \"Zacian\" 70 hidelevel untouchable passive endafter 3 canlose\n setvar VAR_AFTER 1"), ("common", ""));
        var host = new HeadlessScriptHost { Fight = _ => BattleOutcome.Lost };
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"), null, null);
        runner.RunToEnd();
        Assert.Contains("wildbattle Zacian 70 nofleeing hidelevel untouchable passive endafter 3 canlose Lost", host.Log);
        Assert.False(runner.EndedInDefeat);
        Assert.Equal(1, host.Story.Var("VAR_AFTER"));
    }
}
