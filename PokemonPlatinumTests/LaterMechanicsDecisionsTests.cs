using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 06 · R30, the user's decisions of 2026-10-31: when the trainer AI uses each of the four later mechanics
/// (<c>TrainerAi.WithTheLaterMechanics</c>: a Z-Move only where it is worth it, a Dynamax on the last or the ace, a
/// Terastallization that turns a weakness or on the last), and the run of "No later mechanics" that refuses all of them
/// to both sides (<see cref="RunTerms.NoLaterMechanics"/>). Under the modern rules, the rolls fixed (<see cref="Calm"/>);
/// the trainer AI chooses for the foe and the player is answered for from outside.
/// </summary>
public class LaterMechanicsDecisionsTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    /// <summary>A battle against a trainer with every one of the four items; the player's side has them too.</summary>
    private static BattleCore Against(Pokemon[] mine, Pokemon[] theirs, RunTerms? run = null, bool ai = true, string gear = "kzdt")
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer
        {
            Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100,
            KeyStone = gear.Contains('k'), ZRing = gear.Contains('z'), DynamaxBand = gear.Contains('d'), TeraOrb = gear.Contains('t')
        };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = Modern,
            PlayerController = null, EnemyController = ai ? TrainerAi.Instance : null, PlayerName = "Lucas",
            KeyStone = true, ZRing = true, DynamaxBand = true, TeraOrb = true, Restrictions = run
        });
        core.Start();
        return core;
    }

    private static Pokemon Holding(Pokemon p, string item)
    {
        p.HeldItem = ItemDatabase.Get(item) ?? throw new ArgumentException(item);
        return p;
    }

    private static Pokemon Dyna(Pokemon p)
    {
        p.DynamaxLevel = 10;
        return p;
    }

    private static Pokemon Tera(Pokemon p, PokemonType type)
    {
        p.TeraType = TeraType.Of(type);
        return p;
    }

    private static readonly RunTerms NoLater = new(NoLaterMechanics: true);

    // ---------------------------------------------------------------- Z-Moves

    [Fact]
    public void TheAiUsesAZMoveOnADamagingMoveAtLeastNeutral()
    {
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Holding(Mon("Bidoof", 50, "Tackle"), "Normalium Z") }));
        Assert.Contains("Foe Bidoof used Breakneck Blitz!", said);
    }

    [Fact]
    public void TheAiKeepsItsZPowerFromAResistedMove()
    {
        // Tackle on a Geodude is not very effective: the Z-Power waits
        var said = Turn(Against(new[] { Mon("Geodude", 50, "Splash") }, new[] { Holding(Mon("Bidoof", 50, "Tackle"), "Normalium Z") }));
        Assert.Contains("Foe Bidoof used Tackle!", said);
        Assert.DoesNotContain(said, l => l.Contains("Z-Power"));
    }

    [Fact]
    public void TheAiUsesAStatusZMoveWhoseBonusHelps()
    {
        // Z-Growl raises the user's Defense: worth it at +0
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Holding(Mon("Bidoof", 50, "Growl"), "Normalium Z") }));
        Assert.Contains(said, l => l.Contains("Z-Power"));
    }

    [Fact]
    public void TheAiKeepsAStatusZMoveWhoseBonusWouldBeWasted()
    {
        // At +2 Defense already, Z-Growl's bonus is not worth the battle's one Z-Move
        var bidoof = Holding(Mon("Bidoof", 50, "Growl"), "Normalium Z");
        var core = Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { bidoof });
        bidoof.StatStages[StatType.Defense] = 2;
        var said = Turn(core);
        Assert.DoesNotContain(said, l => l.Contains("Z-Power"));
    }

    // ---------------------------------------------------------------- Dynamax

    [Fact]
    public void TheAiDynamaxesItsAceAsItComesIn()
    {
        // The lead is the team's highest level: it grows on its first turn
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") },
            new[] { Dyna(Mon("Bidoof", 50, "Tackle")), Dyna(Mon("Bidoof", 40, "Tackle")) }));
        Assert.Contains("Foe Bidoof swelled to a giant size with Dynamax energy!", said);
    }

    [Fact]
    public void TheAiKeepsItsDynamaxFromALeadThatIsNeitherAceNorLast()
    {
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") },
            new[] { Dyna(Mon("Bidoof", 40, "Tackle")), Dyna(Mon("Bidoof", 50, "Tackle")) }));
        Assert.DoesNotContain(said, l => l.Contains("Dynamax"));
        Assert.Contains("Foe Bidoof used Tackle!", said);
    }

    [Fact]
    public void TheAiDynamaxesItsLastPokemon()
    {
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Dyna(Mon("Bidoof", 40, "Tackle")) }));
        Assert.Contains("Foe Bidoof swelled to a giant size with Dynamax energy!", said);
    }

    // ---------------------------------------------------------------- Terastallization

    [Fact]
    public void TheAiTerastallizesWhenItsTeraTypeTurnsAWeaknessToTheFoesLastMove()
    {
        // Turn 1: nothing seen yet, and a teammate stands behind, so no Tera. Turn 2: Karate Chop has been seen, super
        // effective on a Normal type and nothing at all on a Ghost one
        var core = Against(new[] { Mon("Machop", 20, "Karate Chop") },
            new[] { Tera(Mon("Bidoof", 50, "Growl"), PokemonType.Ghost), Mon("Starly", 30, "Growl") }, gear: "t");
        Assert.DoesNotContain(Turn(core), l => l.Contains("Terastallized"));
        Assert.Contains("Foe Bidoof Terastallized into the Ghost type!", Turn(core));
    }

    [Fact]
    public void TheAiKeepsItsTeraWhenTheTeraTypeLeavesTheWeakness()
    {
        var core = Against(new[] { Mon("Machop", 20, "Karate Chop") },
            new[] { Tera(Mon("Bidoof", 50, "Growl"), PokemonType.Normal), Mon("Starly", 30, "Growl") }, gear: "t");
        Turn(core);
        Assert.DoesNotContain(Turn(core), l => l.Contains("Terastallized"));
    }

    [Fact]
    public void TheAiTerastallizesItsLastPokemon()
    {
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Mon("Bidoof", 50, "Tackle") }, gear: "t"));
        Assert.Contains("Foe Bidoof Terastallized into the Normal type!", said);
    }

    // ---------------------------------------------------------------- Mega Evolution, unchanged

    [Fact]
    public void TheAiStillMegaEvolvesAtOnce()
    {
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") },
            new[] { Holding(Mon("Beedrill", 40, "Splash"), "Beedrillite"), Mon("Bidoof", 50, "Tackle") }));
        Assert.Contains(said, l => l.Contains("has Mega Evolved"));
    }

    // ---------------------------------------------------------------- No later mechanics (the run)

    [Fact]
    public void NoLaterMechanicsRefusesEachOfThemToThePlayer()
    {
        var core = Against(new[] { Dyna(Holding(Mon("Bidoof", 50, "Tackle"), "Normalium Z")) }, new[] { Mon("Snorlax", 50, "Splash") }, NoLater, ai: false);
        var mine = core.At(Mine);
        Assert.True(core.LaterMechanicsBarred);
        Assert.NotNull(core.WhyNotZMove(mine, 0));
        Assert.NotNull(core.WhyNotDynamax(mine));
        Assert.NotNull(core.WhyNotTera(mine));
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Mine, 0) with { Terastallize = true }));

        var mega = Against(new[] { Holding(Mon("Beedrill", 50, "Splash"), "Beedrillite") }, new[] { Mon("Snorlax", 50, "Splash") }, NoLater, ai: false);
        Assert.NotNull(mega.WhyNotMega(mega.At(Mine)));
        // And without the run, the same Beedrill may
        var free = Against(new[] { Holding(Mon("Beedrill", 50, "Splash"), "Beedrillite") }, new[] { Mon("Snorlax", 50, "Splash") }, ai: false);
        Assert.Null(free.WhyNotMega(free.At(Mine)));
    }

    [Fact]
    public void NoLaterMechanicsKeepsTheAiFromEveryOneOfThem()
    {
        var said = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") },
            new[] { Dyna(Holding(Mon("Bidoof", 50, "Tackle"), "Normalium Z")) }, NoLater));
        Assert.Contains("Foe Bidoof used Tackle!", said);
        Assert.DoesNotContain(said, l => l.Contains("Z-Power") || l.Contains("Dynamax") || l.Contains("Terastallized"));

        var mega = Turn(Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Holding(Mon("Beedrill", 50, "Splash"), "Beedrillite") }, NoLater));
        Assert.DoesNotContain(mega, l => l.Contains("Mega Evolved"));
    }

    [Fact]
    public void ABattleWithNoLaterMechanicsKeepsItInItsRecordAndItsFile()
    {
        var core = Against(new[] { Mon("Snorlax", 50, "Tackle") }, new[] { Mon("Bidoof", 50, "Tackle") }, NoLater);
        core.Submit(BattleChoice.Fight(Mine, 0));
        Assert.True(core.Record!.NoLaterMechanics);
        var plain = Against(new[] { Mon("Snorlax", 50, "Tackle") }, new[] { Mon("Bidoof", 50, "Tackle") });
        Assert.False(plain.Record!.NoLaterMechanics);

        var file = new BattleRecordFile { NoLaterMechanics = true };
        Assert.True(file.ToSetup().Restrictions?.NoLaterMechanics);
        Assert.True(file.Record.NoLaterMechanics);
        Assert.Null(new BattleRecordFile().ToSetup().Restrictions);
    }

    [Fact]
    public void TheRunCarriesItToItsBattlesAndAnOlderSaveHasItOff()
    {
        var run = RunRules.Normal with { NoLaterMechanics = true };
        Assert.True(run.BattleTerms()!.NoLaterMechanics);
        Assert.True(run.Has("nolatermechanics"));
        Assert.Null(RunRules.Normal.BattleTerms());
        // Saved and read back whole; a run written before the field reads it as off
        Assert.True(JsonSerializer.Deserialize<RunRules>(JsonSerializer.Serialize(run))!.NoLaterMechanics);
        Assert.False(JsonSerializer.Deserialize<RunRules>("{}")!.NoLaterMechanics);
    }

    [Fact]
    public void TheRunPanelHasARowForIt()
    {
        var panel = new RunPanel();
        panel.Open();
        while (panel.SelectedRow != RunRow.NoLaterMechanics) panel.Move(1);
        Assert.Equal("shared.off", panel.ValueKey(RunRow.NoLaterMechanics));
        panel.Change(1);
        Assert.True(panel.Rules.NoLaterMechanics);
        Assert.Equal("shared.on", panel.ValueKey(RunRow.NoLaterMechanics));
        Assert.Equal("custom", panel.Rules.PresetKey);
    }
}
