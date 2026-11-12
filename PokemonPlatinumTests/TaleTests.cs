using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The Tales (plan 46 · CP11; <c>docs/mechanics/rulings.md</c>, "The Tales"): a tale told once a battle with a move, its
/// rule over the whole field for five turns for both sides, answered once by the other side, its Tale Forms woken and put
/// back, the trainer AI's sense of when, the Vs. Recorder's file, and nothing at all with no Book.
/// </summary>
public class TaleTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;
    private static readonly string[] All = { "zmeu", "varcolac", "solomonar", "dochia", "ielele" };

    /// <summary>A battle against a trainer under the modern rules, each side's tales given (null: no Book), both sides answered from outside unless the AI chooses for the foe.</summary>
    private static BattleCore Tales(Pokemon[] mine, Pokemon[] theirs, string[]? myTales = null, string[]? theirTales = null, bool ai = false,
        TaleTable? table = null, RunTerms? run = null, Ruleset? rules = null, BattleRandom? rolls = null)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "teller", Name = "Ilinca", TrainerClass = "Ace Trainer", PrizeMoney = 100, Tales = theirTales?.ToList() };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = rolls ?? Calm(), Rules = rules ?? Modern,
            PlayerController = null, EnemyController = ai ? TrainerAi.Instance : null, PlayerName = "Lucas",
            Tales = myTales, TaleTable = table, Restrictions = run
        });
        core.Start();
        return core;
    }

    /// <summary>A turn: each side uses its first move (or the one given), telling the tale given with it.</summary>
    private static (List<string> Said, List<BattleEvent> Log) Tell(BattleCore core, string? mine = null, string? theirs = null, int myMove = 0, int foeMove = 0)
    {
        var asked = Assert.IsType<ActionRequest>(core.Request).Places;
        var choices = new List<BattleChoice>();
        if (asked.Contains(Mine)) choices.Add(BattleChoice.Fight(Mine, myMove) with { Tale = mine });
        if (asked.Contains(Foe)) choices.Add(BattleChoice.Fight(Foe, foeMove) with { Tale = theirs });
        core.Submit(choices);
        var log = core.TakeLog().ToList();
        return (Lines(log), log);
    }

    // ---------------------------------------------------------------- the file

    [Fact]
    public void TheTalesFileHoldsTheFiveTheStoryNeedsEachWithItsWords()
    {
        var table = TaleTable.Default;
        Assert.Equal(5, table.Turns);
        Assert.Equal(All, table.All.Select(t => t.Id));
        foreach (var id in All)
            foreach (var word in new[] { "name", "told", "rule", "story" })
                Assert.True(PokemonPlatinumEngine.Core.Text.Has($"basme.{id}.{word}"), $"basme.{id}.{word}");
        Assert.Equal(130, table.Get("zmeu")!.Boost!.Percent);
        Assert.Equal(80, table.Get("varcolac")!.Accuracy);
        Assert.Equal(BattleWeather.Rain, table.Get("solomonar")!.Weather);
        Assert.Equal(BattleWeather.Hail, table.Get("dochia")!.Weather);
        Assert.True(table.Get("ielele")!.Boost!.Favours(PokemonType.Normal, MoveFlags.Sound));
    }

    [Fact]
    public void TheTalesFileIsReadStrictly()
    {
        Assert.Throws<System.IO.InvalidDataException>(() =>
            GameDataFiles.ReadStrict<TalesFile>("""{ "turns": 5, "tales": [ { "id": "x", "boots": { "percent": 130 } } ] }""", "tales.json"));
        Assert.Throws<System.IO.InvalidDataException>(() =>
            GameDataFiles.ReadStrict<TalesFile>("""{ "tales": [] }""", "tales.json"));
    }

    // ---------------------------------------------------------------- telling

    [Fact]
    public void ATaleToldSaysSoAndHoldsOverTheField()
    {
        var core = Tales(new[] { Mon("Monferno", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All);
        var (said, log) = Tell(core, mine: "zmeu");
        InOrder(said, "Lucas told the tale of the Zmeu!", "The sun stands still over the field!");
        var told = Assert.Single(log.OfType<Said>(), s => s.Text.StartsWith("Lucas told")).Shows.OfType<TaleTold>().Single();
        Assert.Equal((BattleSide.Player, "zmeu", 5), (told.Teller, told.Tale, told.Turns));
        Assert.Equal(("zmeu", BattleSide.Player), (core.Field.Tale!.Id, core.Field.Tale.Teller));
    }

    [Fact]
    public void ATaleHoldsForFiveTurnEndsAndThenEnds()
    {
        var core = Tales(new[] { Mon("Monferno", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All);
        Tell(core, mine: "zmeu");
        for (int turn = 2; turn <= 4; turn++)
        {
            Assert.DoesNotContain(Tell(core).Said, l => l.Contains("came to its end"));
            Assert.NotNull(core.Field.Tale);
        }
        Assert.Equal(1, core.Field.Tale!.Turns);
        var (said, log) = Tell(core);
        Assert.Contains("The tale of the Zmeu came to its end.", said);
        Assert.Single(log.OfType<Said>().SelectMany(s => s.Shows).OfType<TaleEnded>());
        Assert.Null(core.Field.Tale);
    }

    [Fact]
    public void AFireMoveHitsHarderUnderTheZmeusTaleForBothSides()
    {
        int Lost(string? tale, bool foeTells)
        {
            var foe = Mon("Snorlax", 50, "Ember");
            var mine = Mon("Monferno", 30, "Ember");
            var core = Tales(new[] { mine }, new[] { foe }, myTales: All, theirTales: All);
            int before = foe.CurrentHP, mineBefore = mine.CurrentHP;
            Tell(core, mine: foeTells ? null : tale, theirs: foeTells ? tale : null);
            // The rule holds for both sides alike: the foe's Ember on the player's Monferno too
            return (before - foe.CurrentHP) * 1000 + (mineBefore - mine.CurrentHP);
        }
        int plain = Lost(null, false), mineTold = Lost("zmeu", false), theirsTold = Lost("zmeu", true);
        Assert.True(mineTold / 1000 > plain / 1000 && mineTold % 1000 > plain % 1000, $"{plain} {mineTold}");
        Assert.Equal(mineTold, theirsTold);
        Assert.InRange(mineTold / 1000 / (double)(plain / 1000), 1.2, 1.4);
    }

    [Fact]
    public void TheVarcolacsEclipseMakesAMoveThatWouldHitMiss()
    {
        // The accuracy roll is 85 of 100: a sure move hits under no tale, and misses at the eclipse's 80
        BattleRandom Rolls() => Calm().Force(RollKind.Accuracy, 85);
        var plain = Tales(new[] { Mon("Bidoof", 30, "Tackle") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All, rolls: Rolls());
        int hp = plain.At(Foe).Pokemon!.CurrentHP;
        Tell(plain);
        Assert.True(plain.At(Foe).Pokemon!.CurrentHP < hp);

        var dark = Tales(new[] { Mon("Bidoof", 30, "Tackle") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All, rolls: Rolls());
        hp = dark.At(Foe).Pokemon!.CurrentHP;
        var (said, _) = Tell(dark, mine: "varcolac");
        Assert.Contains(said, l => l.Contains("missed"));
        Assert.Equal(hp, dark.At(Foe).Pokemon!.CurrentHP);
    }

    [Fact]
    public void TheSolomonarsStormBringsRainThatGoesWithItsTale()
    {
        var core = Tales(new[] { Mon("Monferno", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All, theirTales: All);
        var (_, log) = Tell(core, mine: "solomonar");
        Assert.Equal(BattleWeather.Rain, core.Field.Weather);
        Assert.Contains(log.OfType<Said>().SelectMany(s => s.Shows), e => e is WeatherChanged { Weather: BattleWeather.Rain });
        Assert.True(core.Field.Tale!.BroughtWeather);
        // The foe answers with the Zmeu's: the storm ends with the tale it came with
        var (said, answered) = Tell(core, theirs: "zmeu");
        Assert.Contains("Ilinca answered the Solomonar with the tale of the Zmeu!", said.Select(l => l.Replace("Ace Trainer ", "")));
        Assert.Contains(answered.OfType<Said>().SelectMany(s => s.Shows), e => e is TaleAnswered { Tale: "zmeu", Ended: "solomonar", Teller: BattleSide.Enemy });
        Assert.Equal(BattleWeather.None, core.Field.Weather);
        Assert.Equal(("zmeu", BattleSide.Enemy), (core.Field.Tale!.Id, core.Field.Tale.Teller));
    }

    [Fact]
    public void BabaDochiasColdBringsTheSnowUnderTheModernRules()
    {
        var core = Tales(new[] { Mon("Monferno", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All);
        Tell(core, mine: "dochia");
        Assert.Equal(BattleWeather.Snow, core.Field.Weather);
    }

    [Fact]
    public void TheIelelesDanceStrengthensSoundMovesOfAnyType()
    {
        int Lost(string? tale)
        {
            var foe = Mon("Snorlax", 50, "Splash");
            var core = Tales(new[] { Mon("Exploud", 40, "Hyper Voice") }, new[] { foe }, myTales: All);
            int before = foe.CurrentHP;
            Tell(core, mine: tale);
            return before - foe.CurrentHP;
        }
        Assert.InRange(Lost("ielele") / (double)Lost(null), 1.2, 1.4);
    }

    // ---------------------------------------------------------------- once a battle, both sides

    [Fact]
    public void EachSideTellsOneTaleAndAnswersOne()
    {
        var core = Tales(new[] { Mon("Monferno", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All, theirTales: All);
        Tell(core, mine: "zmeu");
        // The player has told theirs: no second while theirs holds, nor a tale of the same side's
        Assert.NotNull(core.WhyNotTale(core.At(Mine), "varcolac"));
        // The foe answers
        Tell(core, theirs: "varcolac");
        // The player may answer that, once
        Assert.Null(core.WhyNotTale(core.At(Mine), "solomonar"));
        Tell(core, mine: "solomonar");
        Assert.Equal("solomonar", core.Field.Tale!.Id);
        // The foe has answered already: it can't answer again
        Assert.Equal(PokemonPlatinumEngine.Core.Text.Of("battle.taleOnlyOnce"), core.WhyNotTale(core.At(Foe), "dochia"));
        // Once the player's runs out the foe still has its telling, and tells
        while (core.Field.Tale != null) Tell(core);
        Assert.Null(core.WhyNotTale(core.At(Foe), "dochia"));
        Tell(core, theirs: "dochia");
        // And now the player has neither a telling nor an answer left, nor the foe
        Assert.Equal(PokemonPlatinumEngine.Core.Text.Of("battle.taleOnlyOnce"), core.WhyNotTale(core.At(Mine), "ielele"));
        while (core.Field.Tale != null) Tell(core);
        Assert.NotNull(core.WhyNotTale(core.At(Mine), "ielele"));
        Assert.NotNull(core.WhyNotTale(core.At(Foe), "ielele"));
    }

    [Fact]
    public void ATaleToldOnceInTheBattleCantBeToldAgainByEitherSide()
    {
        var core = Tales(new[] { Mon("Monferno", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All, theirTales: All);
        Tell(core, mine: "zmeu");
        Assert.Equal(PokemonPlatinumEngine.Core.Text.Of("battle.taleAlreadyTold", ("tale", TaleTable.NameOf("zmeu"))), core.WhyNotTale(core.At(Foe), "zmeu"));
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0) with { Tale = "zmeu" }));
    }

    [Fact]
    public void BothSidesTellingOnOneTurnMakeTheSlowerAnAnswer()
    {
        // Monferno is faster than Snorlax: its trainer tells first, and the foe's tale answers it
        var core = Tales(new[] { Mon("Monferno", 30, "Splash") }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: All, theirTales: All);
        var (_, log) = Tell(core, mine: "zmeu", theirs: "varcolac");
        var shows = log.OfType<Said>().SelectMany(s => s.Shows).ToList();
        Assert.Single(shows.OfType<TaleTold>(), t => t.Tale == "zmeu");
        Assert.Single(shows.OfType<TaleAnswered>(), t => t.Tale == "varcolac" && t.Ended == "zmeu");
        Assert.Equal("varcolac", core.Field.Tale!.Id);
    }

    // ---------------------------------------------------------------- nothing without it

    [Fact]
    public void WithNoBookOrUnderPlatinumsRulesOrWithNoLaterMechanicsNoTaleIsTold()
    {
        static Pokemon[] A() => new[] { Mon("Monferno", 30, "Splash") };
        static Pokemon[] B() => new[] { Mon("Snorlax", 50, "Splash") };
        foreach (var core in new[]
        {
            Tales(A(), B(), myTales: null),
            Tales(A(), B(), myTales: new[] { "varcolac" }),
            Tales(A(), B(), myTales: All, rules: Ruleset.Platinum),
            Tales(A(), B(), myTales: All, run: new RunTerms(NoLaterMechanics: true))
        })
        {
            Assert.NotNull(core.WhyNotTale(core.At(Mine), "zmeu"));
            Assert.Empty(core.TalesToTell(core.At(Mine)).Where(t => t == "zmeu"));
            Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0) with { Tale = "zmeu" }, BattleChoice.Fight(Foe, 0)));
        }
    }

    [Fact]
    public void ABattleWithNoTaleToldWritesTheSameLogAsOneWithNoBook()
    {
        // The Book and its tales in hand, never told: the battle is the one it would be without them, to the line
        string Fought(string[]? tales)
        {
            var core = Tales(new[] { Mon("Monferno", 30, "Ember") }, new[] { Mon("Snorlax", 50, "Tackle") }, myTales: tales, theirTales: tales, rolls: new BattleRandom(7));
            for (int i = 0; i < 6 && core.Request is ActionRequest; i++) Tell(core);
            return string.Join('\n', Golden.BattleText.Written(core));
        }
        Assert.Equal(Fought(null), Fought(All));
    }

    // ---------------------------------------------------------------- Tale Forms

    /// <summary>A table of the Zmeu's tale alone, binding Rotom to its Heat form: a test's own, so no species of the game's is touched.</summary>
    private static TaleTable RotomWakes() => new(new TalesFile
    {
        Turns = 5,
        Tales = { new TaleRule { Id = "zmeu", Boost = new TaleBoost { Types = { PokemonType.Fire }, Percent = 130 }, Forms = { new TaleBinding { Species = "Rotom", Form = "Rotom-Heat" } } } }
    });

    [Fact]
    public void APokemonBoundToTheTaleWakesIntoItsFormAndGoesBackAsItEnds()
    {
        var rotom = Mon("Rotom", 40, "Splash");
        var core = Tales(new[] { rotom }, new[] { Mon("Snorlax", 50, "Splash") }, myTales: new[] { "zmeu" }, table: RotomWakes());
        var (said, log) = Tell(core, mine: "zmeu");
        Assert.Contains("Rotom woke to the tale of the Zmeu!", said);
        Assert.Contains(log.OfType<Said>().SelectMany(s => s.Shows), e => e is TaleFormChanged { Form: "Rotom-Heat", Woke: true });
        Assert.Equal("Rotom-Heat", rotom.Form);
        for (int i = 0; i < 4; i++) Tell(core);
        Assert.Null(core.Field.Tale);
        Assert.Null(rotom.Form);
    }

    [Fact]
    public void OnlyTheTellersSidesBoundPokemonWakeAndTheBattlesEndPutsThemBack()
    {
        var mine = Mon("Rotom", 40, "Splash");
        var theirs = Mon("Rotom", 40, "Splash");
        var core = Tales(new[] { mine }, new[] { theirs }, theirTales: new[] { "zmeu" }, table: RotomWakes());
        Tell(core, theirs: "zmeu");
        Assert.Equal(("Rotom-Heat", (string?)null), (theirs.Form, mine.Form));
        // A Pokémon that runs out of HP ends the battle with the tale still told: its form is given back all the same
        theirs.CurrentHP = 1;
        mine.Moves[0] = new Move(MoveDatabase.Get("Thunder Shock")!);
        Tell(core);
        Assert.NotEqual(BattleResult.None, core.Result);
        Assert.Null(theirs.Form);
    }

    // ---------------------------------------------------------------- the trainer AI

    [Fact]
    public void TheAiTellsItsBestTaleWhenItHelpsItsSide()
    {
        // The foe's Ponyta has Ember and the player's Snorlax nothing the tales favour: the Zmeu's is worth the most
        var core = Tales(new[] { Mon("Snorlax", 50, "Splash") }, new[] { Mon("Ponyta", 30, "Ember") }, theirTales: new[] { "varcolac", "zmeu" }, ai: true);
        Assert.Equal("zmeu", TrainerAi.TaleNow(core, core.At(Foe)));
        core.Submit(BattleChoice.Fight(Mine, 0));
        Assert.Equal(("zmeu", BattleSide.Enemy), (core.Field.Tale!.Id, core.Field.Tale.Teller));
    }

    [Fact]
    public void TheAiKeepsATaleThatHelpsTheFoeMore()
    {
        // The player's Monferno breathes fire and the foe's Psyduck doesn't: the Zmeu's would help the player
        var core = Tales(new[] { Mon("Monferno", 30, "Ember") }, new[] { Mon("Psyduck", 30, "Scratch") }, theirTales: new[] { "zmeu" }, ai: true);
        Assert.Null(TrainerAi.TaleNow(core, core.At(Foe)));
    }

    [Fact]
    public void TheAiAnswersATaleThatHurtsIt()
    {
        var core = Tales(new[] { Mon("Monferno", 30, "Ember") }, new[] { Mon("Psyduck", 30, "Water Gun") }, myTales: All, theirTales: new[] { "solomonar" });
        Tell(core, mine: "zmeu");
        Assert.True(TrainerAi.TaleWorth(core, BattleSide.Enemy, core.Field.Tale!.Rule) < 0);
        Assert.Equal("solomonar", TrainerAi.TaleNow(core, core.At(Foe)));
        // A tale of its own side's that helps it is never answered
        var kind = Tales(new[] { Mon("Monferno", 30, "Splash") }, new[] { Mon("Psyduck", 30, "Water Gun") }, myTales: All, theirTales: new[] { "zmeu" });
        Tell(kind, mine: "solomonar");
        Assert.Null(TrainerAi.TaleNow(kind, kind.At(Foe)));
    }

    // ---------------------------------------------------------------- the record and the Vs. Recorder's file

    [Fact]
    public void ARecordedBattleWithTalesReplaysTheSameAndItsFileKeepsThem()
    {
        var mine = new Party();
        mine.Add(Mon("Monferno", 30, "Ember", "Mach Punch"));
        var trainer = new Trainer { Id = "tale_test", Name = "Ilinca", TrainerClass = "Ace Trainer", PrizeMoney = 64, Ai = AiFlags(), Tales = new List<string> { "solomonar" } };
        trainer.Party.Add(Mon("Psyduck", 28, "Water Gun", "Scratch"));
        trainer.Party.Add(Mon("Ponyta", 27, "Ember", "Tackle"));
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.BookOfTalesItem)!, 1);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = bag, Pokedex = new Pokedex(), Trainers = { trainer },
            Random = new BattleRandom(46), Rules = Modern, PlayerName = "Lucas", Tales = new[] { "zmeu", "varcolac" }
        });
        Settle(battle);
        Assert.True(battle.CanTellTaleNow);
        Assert.Equal("zmeu", battle.TaleOffered);
        Assert.True(battle.ToggleTale());
        Assert.Equal("zmeu", battle.TaleArmed);
        for (int turn = 0; turn < 40 && !battle.IsBattleOver; turn++)
        {
            if (battle.IsChoosingReplacement) battle.SelectSwitch(0);
            else battle.SelectMove(0);
            Settle(battle);
        }
        Assert.True(battle.IsBattleOver);
        Assert.Contains(battle.Core.Record!.Answers.SelectMany(a => a), c => c.Tale == "zmeu");

        var file = battle.RecordFile(new DateTime(2026, 11, 13))!;
        Assert.Equal(new[] { "zmeu", "varcolac" }, file.Tales);
        Assert.Equal(new[] { "solomonar" }, file.Trainers.Single().Tales);
        string text = file.Serialize();
        var read = BattleRecordFile.Read(text);
        Assert.Equal(ReplayRefusal.None, read.Refused);
        Assert.Equal(text, read.File!.Serialize());
        var again = read.File.Replay();
        Assert.Equal(battle.Result, again.Result);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));
    }

    private static PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags AiFlags() => PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic;

    [Fact]
    public void WithoutTheBookTheMenuOffersNoTale()
    {
        var mine = new Party();
        mine.Add(Mon("Monferno", 30, "Ember"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Mon("Bidoof", 5, "Tackle") },
            Random = Steady(), Rules = Modern, PlayerName = "Lucas", Tales = new[] { "zmeu" }
        });
        Settle(battle);
        Assert.Null(battle.Core.PlayerTales);
        Assert.False(battle.CanTellTaleNow);
        Assert.False(battle.ToggleTale());
    }
}
