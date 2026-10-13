using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Platinum's two battle options (plan 08 · P1; <c>docs/mechanics/rulings.md</c>, "Battle Scene and Battle Style"):
/// Battle Style Shift's offer before a trainer's next Pokémon, which is the rules', and the Battle Scene, which is
/// the face's alone.
/// </summary>
public class BattleOptionsTests
{
    private static Pokemon[] MyTeam() => new[] { Of(Gender.Male, "Garchomp", 80, "Earthquake"), Of(Gender.Male, "Luxray", 50, "Spark") };
    private static Pokemon[] TheirTeam() => new[] { Of(Gender.Male, "Bidoof", 5), Of(Gender.Male, "Starly", 5) };

    /// <summary>The log with each line's events after it, in the order they are shown.</summary>
    private static List<BattleEvent> Flat(IEnumerable<BattleEvent> log) =>
        log.SelectMany(e => e is Said said ? new[] { e }.Concat(said.Shows).Concat(said.OnImpact) : new[] { e }).ToList();

    /// <summary>
    /// A turn in which the Garchomp knocks the Bidoof out, and the trainer's next Pokémon picked: both sides are
    /// answered from outside, so the rules ask for it (the offer comes after it is picked).
    /// </summary>
    private static List<string> KnockOut(BattleCore core)
    {
        var said = Turn(core);
        if (core.Request is ReplacementRequest { Place: var place } && place == Foe)
        {
            core.Submit(BattleChoice.Switch(Foe, 1));
            said.AddRange(Lines(core.TakeLog()));
        }
        return said;
    }

    // ------------------------------------------------------------------ Battle Style

    [Fact]
    public void ShiftOffersASwitchBeforeTheTrainersNextPokemonComesOut()
    {
        var core = Against(MyTeam(), TheirTeam(), offersSwitch: true);
        var said = KnockOut(core);

        // subscript_replace_fainted: the trainer's next Pokémon is picked, the question asked, and nothing of it is
        // out yet
        var offer = Assert.IsType<SwitchOfferRequest>(core.Request);
        Assert.Equal(Mine, offer.Place);
        Assert.Equal("Starly", offer.Upcoming.Species.Name);
        Assert.Contains("Starly", said[^1]);
        Assert.Contains("Will you change", said[^1]);
        Assert.DoesNotContain(core.Log, e => e is Said s && s.Shows.OfType<Entered>().Any(entered => entered.Place == Foe && entered.Pokemon.Species.Name == "Starly"));
        Assert.True(core.EnemySlots[0].Pokemon is null or { IsFainted: true });
    }

    [Fact]
    public void SetOffersNothing()
    {
        var core = Against(MyTeam(), TheirTeam());
        var said = KnockOut(core);
        Assert.IsType<ActionRequest>(core.Request);
        Assert.DoesNotContain(said, line => line.Contains("Will you change"));
        Assert.Equal("Starly", core.EnemySlots[0].Pokemon!.Species.Name);
        Assert.Equal("Garchomp", core.PlayerSlots[0].Pokemon!.Species.Name);
    }

    [Fact]
    public void NobodyToSwitchToMeansNoOffer()
    {
        var core = Against(new[] { Of(Gender.Male, "Garchomp", 80, "Earthquake") }, TheirTeam(), offersSwitch: true);
        KnockOut(core);
        Assert.IsType<ActionRequest>(core.Request);
    }

    [Fact]
    public void StayingInSendsOutTheTrainersPokemonAndGoesOn()
    {
        var core = Against(MyTeam(), TheirTeam(), offersSwitch: true);
        KnockOut(core);
        core.Submit(BattleChoice.Stay(Mine));
        var events = Flat(core.TakeLog());
        Assert.Contains(events, e => e is Entered entered && entered.Place == Foe && entered.Pokemon.Species.Name == "Starly");
        Assert.DoesNotContain(events, e => e is Entered entered && entered.Place == Mine);
        Assert.IsType<ActionRequest>(core.Request);
        Assert.Equal("Garchomp", core.PlayerSlots[0].Pokemon!.Species.Name);
    }

    [Fact]
    public void ASwitchAtTheOfferComesInBeforeTheTrainersPokemon()
    {
        var core = Against(MyTeam(), TheirTeam(), offersSwitch: true);
        KnockOut(core);
        core.Submit(BattleChoice.Switch(Mine, 1));
        var events = Flat(core.TakeLog());
        // subscript_switch_pokemon: the recall, the newcomer, then the trainer's Pokémon
        int back = events.FindIndex(e => e is Recalled recalled && recalled.Place == Mine);
        int mineIn = events.FindIndex(e => e is Entered entered && entered.Place == Mine);
        int foeIn = events.FindIndex(e => e is Entered entered && entered.Place == Foe);
        Assert.True(back >= 0 && mineIn > back && foeIn > mineIn, $"recalled {back}, mine in {mineIn}, theirs in {foeIn}");
        Assert.Equal("Luxray", core.PlayerSlots[0].Pokemon!.Species.Name);
        Assert.Equal("Starly", core.EnemySlots[0].Pokemon!.Species.Name);
        Assert.IsType<ActionRequest>(core.Request);
    }

    [Fact]
    public void OnlyStayOrASwitchAnswersTheOffer()
    {
        var core = Against(MyTeam(), TheirTeam(), offersSwitch: true);
        // Nothing to stay in for at a turn's choice
        Assert.NotNull(core.WhyNot(BattleChoice.Stay(Mine)));
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Stay(Mine), BattleChoice.Fight(Foe, 0)));

        KnockOut(core);
        Assert.IsType<SwitchOfferRequest>(core.Request);
        Assert.Null(core.WhyNot(BattleChoice.Stay(Mine)));
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0)));
        // The Pokémon already out can't be sent in for itself
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Switch(Mine, 0)));
        Assert.IsType<SwitchOfferRequest>(core.Request);
    }

    [Fact]
    public void TheOffersAnswerIsInTheRecordAndReplaysTheSame()
    {
        BattleCore Fresh()
        {
            var party = new Party();
            foreach (var p in MyTeam()) party.Add(p);
            var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100 };
            foreach (var p in TheirTeam()) trainer.Party.Add(p);
            return new BattleCore(new CoreSetup
            {
                PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = Ruleset.Platinum,
                PlayerController = null, EnemyController = null, PlayerName = "Lucas", OffersSwitch = true
            });
        }
        var played = Fresh();
        played.Start();
        KnockOut(played);
        played.Submit(BattleChoice.Switch(Mine, 1));
        Turn(played);

        var record = played.Record!;
        Assert.True(record.OffersSwitch);
        Assert.Contains(record.Answers, a => a.Count == 1 && a[0].Kind == ChoiceKind.Switch && a[0].Who == Mine && a[0].SwitchTo == 1);

        var again = Fresh();
        again.Replay(record);
        Assert.Equal(Golden.BattleText.Written(played), Golden.BattleText.Written(again));

        // A battle that offers nothing can't replay one that did
        var plain = Against(MyTeam(), TheirTeam());
        Assert.Throws<ArgumentException>(() => new BattleCore(new CoreSetup
        {
            PlayerParty = plain.PlayerParty, WildPokemon = new List<Pokemon> { Of(Gender.Male, "Bidoof", 5) }, Random = Calm(), Rules = Ruleset.Platinum,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        }).Replay(record));
    }

    // ------------------------------------------------------------------ on screen

    private static BattleEngine Screen(bool offers, bool scene = true)
    {
        var party = new Party();
        foreach (var p in MyTeam()) party.Add(p);
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100 };
        foreach (var p in TheirTeam()) trainer.Party.Add(p);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { trainer },
            WildPokemon = new List<Pokemon>(), Random = Calm(), Rules = Ruleset.Platinum, OffersSwitch = offers, Scene = scene
        });
        Settle(battle);
        return battle;
    }

    [Fact]
    public void TheOfferIsAQuestionWhoseNoAConfirmAnswers()
    {
        var battle = Screen(offers: true);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        battle.SelectMove(0);
        for (int i = 0; i < 50 && !battle.IsOfferingSwitch && battle.HUD.MenuState == BattleMenuState.Message; i++)
        {
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        // The line is the question, Yes and No beside it with the cursor on Yes, as the original's
        Assert.True(battle.IsOfferingSwitch);
        Assert.Equal(BattleMenuState.LearnMove, battle.HUD.MenuState);
        Assert.Equal(LearnStep.Shift, battle.LearnStep);
        Assert.Equal(0, battle.LearnCursor);
        Assert.Equal("Starly", battle.OfferedFoe!.Species.Name);
        Assert.Contains("Starly", battle.CurrentMessage);
        Assert.True(battle.IsLearningMove);

        // Whatever reads a battle on as messages stays in
        Settle(battle);
        Assert.False(battle.IsOfferingSwitch);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Equal("Garchomp", battle.PlayerPokemon.Species.Name);
        Assert.Equal("Starly", battle.EnemyPokemon.Species.Name);
        Assert.Contains(battle.Core.Record!.Answers, a => a.Count == 1 && a[0].Kind == ChoiceKind.Stay);
    }

    [Fact]
    public void TheOffersYesOpensTheTeamAndASwitchThereComesInFirst()
    {
        var battle = Screen(offers: true);
        battle.SelectMove(0);
        for (int i = 0; i < 50 && !battle.IsOfferingSwitch && battle.HUD.MenuState == BattleMenuState.Message; i++)
        {
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        battle.ChooseLearn(0);
        Assert.Equal(BattleMenuState.SwitchPokemon, battle.HUD.MenuState);
        Assert.True(battle.IsOfferingSwitch);
        // Not a question while the team is open: a confirm there answers nothing
        Assert.False(battle.IsLearningMove);

        battle.SelectSwitch(1);
        var said = Settle(battle);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Equal("Luxray", battle.PlayerPokemon.Species.Name);
        Assert.Equal("Starly", battle.EnemyPokemon.Species.Name);
        int mine = said.FindIndex(line => line.Contains("Luxray"));
        int theirs = said.FindIndex(line => line.Contains("Starly"));
        Assert.True(mine >= 0 && theirs > mine, string.Join(" / ", said));
    }

    [Fact]
    public void BackingOutOfTheTeamStaysIn()
    {
        var battle = Screen(offers: true);
        battle.SelectMove(0);
        for (int i = 0; i < 50 && !battle.IsOfferingSwitch && battle.HUD.MenuState == BattleMenuState.Message; i++)
        {
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        battle.ChooseLearn(0);
        battle.StayIn();
        Settle(battle);
        Assert.False(battle.IsOfferingSwitch);
        Assert.Equal("Garchomp", battle.PlayerPokemon.Species.Name);
        Assert.Equal("Starly", battle.EnemyPokemon.Species.Name);
    }

    [Fact]
    public void ABattleSetUpWithoutTheStyleOffersNothing()
    {
        // What a tool or a test makes for itself is Set unless it asks (BattleSetup.OffersSwitch is false)
        var battle = Screen(offers: false);
        battle.SelectMove(0);
        for (int i = 0; i < 50 && battle.HUD.MenuState == BattleMenuState.Message; i++)
        {
            Assert.False(battle.IsOfferingSwitch);
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Equal("Starly", battle.EnemyPokemon.Species.Name);
    }

    // ------------------------------------------------------------------ Battle Scene

    private static BattleEngine Sparring(bool scene)
    {
        var party = new Party();
        party.Add(Of(Gender.Male, "Garchomp", 50, "Dragon Claw", "Swords Dance"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Of(Gender.Male, "Snorlax", 50) }, Random = Calm(), Rules = Ruleset.Platinum, Scene = scene
        });
        Settle(battle);
        return battle;
    }

    [Fact]
    public void WithTheSceneOffNoMovePlaysAndItsHitLandsWithItsLine()
    {
        var on = Sparring(scene: true);
        var off = Sparring(scene: false);
        Assert.False(off.Scene);
        int full = off.EnemyPokemon.MaxHP;

        on.SelectMove(0);
        off.SelectMove(0);
        // The move's line is up: with the scene, its effect plays and its hit is still to land; without, neither
        Assert.Contains(on.Anim.Cues, c => c.Kind == CueKind.Move);
        Assert.DoesNotContain(off.Anim.Cues, c => c.Kind == CueKind.Move);
        Assert.Equal(on.CurrentMessage, off.CurrentMessage);
        on.Update(1f / 60f);
        off.Update(1f / 60f);
        Assert.Equal(full, on.EnemyPokemon.CurrentHP);
        Assert.True(off.EnemyPokemon.CurrentHP < full);

        // A stat raised: no cue for it either
        Settle(on);
        Settle(off);
        off.SelectMove(1);
        Assert.DoesNotContain(off.Anim.Cues, c => c.Kind is CueKind.StatUp or CueKind.StatDown or CueKind.Move);
    }

    [Fact]
    public void TheSceneChangesNothingTheRulesDoOrSay()
    {
        var on = Sparring(scene: true);
        var off = Sparring(scene: false);
        var saidOn = new List<string>();
        var saidOff = new List<string>();
        foreach (int move in new[] { 1, 0, 0, 0 })
        {
            if (on.IsBattleOver || off.IsBattleOver) break;
            saidOn.AddRange(Turn(on, move));
            saidOff.AddRange(Turn(off, move));
            Assert.Equal((on.PlayerPokemon.CurrentHP, on.EnemyPokemon.CurrentHP), (off.PlayerPokemon.CurrentHP, off.EnemyPokemon.CurrentHP));
            Assert.Equal(on.PlayerPokemon.StatStages, off.PlayerPokemon.StatStages);
        }
        Assert.Equal(saidOn, saidOff);
        Assert.Equal(Golden.BattleText.Written(on.Core), Golden.BattleText.Written(off.Core));
    }
}
