using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Every way a Pokémon evolves (docs/mechanics/evolution.md): the rules of <see cref="Evolution"/>, friendship,
/// the scene, and the places that set evolutions off (battles, the bag, trades, a spin).
/// </summary>
[Collection("MapDatabase")]
public class EvolutionTests
{
    // ---------------------------------------------------------------- helpers

    /// <summary>A Pokémon with no IVs and a neutral nature, so its stats are the same every run.</summary>
    private static Pokemon Mon(string species, int level, Gender gender = Gender.Male) =>
        new(PokemonDatabase.Get(species)!, level, gender, Nature.Hardy, false);

    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    private static readonly EvolutionContext Day = new();
    private static readonly EvolutionContext Night = new() { IsNight = true };

    private static string? LevelUp(Pokemon p, EvolutionContext? context = null) =>
        Evolution.Find(p, EvolutionTrigger.LevelUp, context ?? Day)?.TargetSpecies;

    private static string? Use(string item, Pokemon p, EvolutionContext? context = null)
    {
        context ??= new EvolutionContext();
        context.Item = Item(item);
        return Evolution.Find(p, EvolutionTrigger.UseItem, context)?.TargetSpecies;
    }

    private static string? Traded(Pokemon p, string? forSpecies = null) =>
        Evolution.Find(p, EvolutionTrigger.Trade, new EvolutionContext { TradedFor = forSpecies != null ? PokemonDatabase.Get(forSpecies) : null })?.TargetSpecies;

    // ---------------------------------------------------------------- level-up methods

    [Fact]
    public void TestLevelEvolutionsWaitForTheirLevel()
    {
        Assert.Null(LevelUp(Mon("Turtwig", 17)));
        Assert.Equal("Grotle", LevelUp(Mon("Turtwig", 18)));
        // A level skipped (a Rare Candy at 40) still counts
        Assert.Equal("Grotle", LevelUp(Mon("Turtwig", 40)));
        Assert.Null(LevelUp(Mon("Torterra", 100)));
    }

    [Fact]
    public void TestFriendshipEvolutionsNeed220()
    {
        var golbat = Mon("Golbat", 30);
        golbat.Friendship = FriendshipRules.EvolveAt - 1;
        Assert.Null(LevelUp(golbat));
        golbat.Friendship = FriendshipRules.EvolveAt;
        Assert.Equal("Crobat", LevelUp(golbat));
        Assert.Equal("Crobat", LevelUp(golbat, Night));
    }

    [Fact]
    public void TestFriendshipEvolutionsByDayAndByNight()
    {
        var riolu = Mon("Riolu", 10);
        riolu.Friendship = 255;
        Assert.Equal("Lucario", LevelUp(riolu, Day));
        Assert.Null(LevelUp(riolu, Night));

        var chingling = Mon("Chingling", 10);
        chingling.Friendship = 255;
        Assert.Null(LevelUp(chingling, Day));
        Assert.Equal("Chimecho", LevelUp(chingling, Night));

        // Eevee: Espeon by day, Umbreon at night
        var eevee = Mon("Eevee", 10);
        eevee.Friendship = 255;
        Assert.Equal("Espeon", LevelUp(eevee, Day));
        Assert.Equal("Umbreon", LevelUp(eevee, Night));
    }

    [Fact]
    public void TestNightIsFromEightInTheEveningToFourInTheMorning()
    {
        var saved = GameClock.Fixed;
        try
        {
            var nights = Enum.GetValues<TimeOfDay>().Where(t => { GameClock.Fixed = t; return GameClock.IsNight; }).ToList();
            Assert.Equal(new[] { TimeOfDay.Night, TimeOfDay.LateNight }, nights);
        }
        finally { GameClock.Fixed = saved; }
        Assert.Equal(TimeOfDay.Night, GameClock.ForHour(20));
        Assert.Equal(TimeOfDay.LateNight, GameClock.ForHour(3));
        Assert.Equal(TimeOfDay.Morning, GameClock.ForHour(4));
    }

    [Fact]
    public void TestEeveeWithAFairyMoveBecomesSylveonBeforeEspeon()
    {
        var eevee = Mon("Eevee", 10);
        eevee.Friendship = 255;
        eevee.Moves.Clear();
        eevee.Moves.Add(MoveDatabase.Create("Disarming Voice"));
        Assert.Equal(PokemonType.Fairy, eevee.Moves[0].Type);
        Assert.Equal("Sylveon", LevelUp(eevee, Day));
        Assert.Equal("Sylveon", LevelUp(eevee, Night));

        // The move without the friendship isn't enough
        eevee.Friendship = 100;
        Assert.Null(LevelUp(eevee, Day));
    }

    [Fact]
    public void TestEvolutionsByGender()
    {
        Assert.Equal("Vespiquen", LevelUp(Mon("Combee", 21, Gender.Female)));
        Assert.Null(LevelUp(Mon("Combee", 50, Gender.Male)));
        Assert.Equal("Mothim", LevelUp(Mon("Burmy", 20, Gender.Male)));
        Assert.Equal("Wormadam", LevelUp(Mon("Burmy", 20, Gender.Female)));
    }

    [Fact]
    public void TestTyrogueEvolvesByItsAttackAndDefense()
    {
        var tyrogue = Mon("Tyrogue", 20);
        Assert.Equal(tyrogue.Attack, tyrogue.Defense);
        Assert.Equal("Hitmontop", LevelUp(tyrogue));

        tyrogue.IvAttack = 31;
        tyrogue.RecalculateStats();
        Assert.Equal("Hitmonlee", LevelUp(tyrogue));

        tyrogue.IvAttack = 0;
        tyrogue.IvDefense = 31;
        tyrogue.RecalculateStats();
        Assert.Equal("Hitmonchan", LevelUp(tyrogue));

        Assert.Null(LevelUp(Mon("Tyrogue", 19)));
    }

    [Fact]
    public void TestWurmpleEvolvesByItsPersonality()
    {
        // Platinum: the upper half of the personality value, modulo 10, below 5 is Silcoon
        var wurmple = Mon("Wurmple", 7);
        wurmple.Personality = 3u << 16;
        Assert.Equal("Silcoon", LevelUp(wurmple));
        wurmple.Personality = (7u << 16) | 0xFFFF;
        Assert.Equal("Cascoon", LevelUp(wurmple));

        // Both happen: new Pokémon get personalities of their own
        var rng = new Random(5);
        var targets = Enumerable.Range(0, 60).Select(_ => LevelUp(new Pokemon(PokemonDatabase.Get("Wurmple")!, 7, rng))).ToHashSet();
        Assert.Equal(new HashSet<string?> { "Silcoon", "Cascoon" }, targets);
    }

    [Fact]
    public void TestEvolutionsThatNeedAHeldItemAtACertainHour()
    {
        var happiny = Mon("Happiny", 5);
        Assert.Null(LevelUp(happiny, Day));
        happiny.HeldItem = Item("Oval Stone");
        Assert.Equal("Chansey", LevelUp(happiny, Day));
        Assert.Null(LevelUp(happiny, Night));

        var sneasel = Mon("Sneasel", 5);
        sneasel.HeldItem = Item("Razor Claw");
        Assert.Null(LevelUp(sneasel, Day));
        Assert.Equal("Weavile", LevelUp(sneasel, Night));

        // The item is used up
        Evolution.Evolve(sneasel, Evolution.Find(sneasel, EvolutionTrigger.LevelUp, Night)!, Night);
        Assert.Equal("Weavile", sneasel.Species.Name);
        Assert.Null(sneasel.HeldItem);
    }

    [Fact]
    public void TestEvolutionsThatNeedAMove()
    {
        var piloswine = Mon("Piloswine", 40);
        Assert.Null(LevelUp(piloswine));
        piloswine.ReplaceMove(0, "Ancient Power");
        Assert.Equal("Mamoswine", LevelUp(piloswine));
    }

    [Fact]
    public void TestEvolutionsThatNeedCompanyInTheParty()
    {
        var mantyke = Mon("Mantyke", 10);
        Assert.Null(LevelUp(mantyke, new EvolutionContext { Party = PartyOf(mantyke, Mon("Turtwig", 5)) }));
        Assert.Equal("Mantine", LevelUp(mantyke, new EvolutionContext { Party = PartyOf(mantyke, Mon("Remoraid", 5)) }));

        // Pancham needs a Dark type beside it, and its level
        var pancham = Mon("Pancham", 32);
        Assert.Null(LevelUp(pancham, new EvolutionContext { Party = PartyOf(pancham) }));
        Assert.Equal("Pangoro", LevelUp(pancham, new EvolutionContext { Party = PartyOf(pancham, Mon("Murkrow", 5)) }));
        var young = Mon("Pancham", 31);
        Assert.Null(LevelUp(young, new EvolutionContext { Party = PartyOf(young, Mon("Murkrow", 5)) }));
    }

    [Fact]
    public void TestEvolutionsAtASpecialPlace()
    {
        var mossy = new EvolutionContext { Sites = new[] { "Moss Rock" } };
        var icy = new EvolutionContext { Sites = new[] { "Ice Rock" } };
        var magnetic = new EvolutionContext { Sites = new[] { "Magnetic Field" } };

        // The place comes before friendship: a friendly Eevee by the Moss Rock becomes Leafeon
        var eevee = Mon("Eevee", 10);
        eevee.Friendship = 255;
        Assert.Equal("Leafeon", LevelUp(eevee, mossy));
        Assert.Equal("Glaceon", LevelUp(eevee, icy));

        Assert.Null(LevelUp(Mon("Magneton", 40)));
        Assert.Equal("Magnezone", LevelUp(Mon("Magneton", 40), magnetic));
        Assert.Equal("Probopass", LevelUp(Mon("Nosepass", 20), magnetic));
    }

    [Fact]
    public void TestFeebasEvolvesOnBeauty()
    {
        var feebas = Mon("Feebas", 20);
        feebas.Beauty = 169;
        Assert.Null(LevelUp(feebas));
        feebas.Beauty = 170;
        Assert.Equal("Milotic", LevelUp(feebas));
    }

    [Fact]
    public void TestTheRulesOfLaterGames()
    {
        // Rain in the field, a full moon (night here), an upside-down console (a plain level here)
        Assert.Null(LevelUp(Mon("Sliggoo", 50)));
        Assert.Equal("Goodra", LevelUp(Mon("Sliggoo", 50), new EvolutionContext { IsRaining = true }));
        Assert.Null(LevelUp(Mon("Inkay", 29)));
        Assert.Equal("Malamar", LevelUp(Mon("Inkay", 30)));
        Assert.Null(Use("Peat Block", Mon("Ursaring", 40)));
        Assert.Equal("Ursaluna", Use("Peat Block", Mon("Ursaring", 40), new EvolutionContext { IsNight = true }));

        // The sun's legend by day, the moon's at night
        Assert.Equal("Solgaleo", LevelUp(Mon("Cosmoem", 53), Day));
        Assert.Equal("Lunala", LevelUp(Mon("Cosmoem", 53), Night));

        // Day and night for the same species
        Assert.Equal("Lycanroc", LevelUp(Mon("Rockruff", 25), Day));
        Assert.Equal("Aurorus", LevelUp(Mon("Amaura", 39), Night));
        Assert.Null(LevelUp(Mon("Amaura", 39), Day));
    }

    [Fact]
    public void TestEvolutionsThatCountSomething()
    {
        // A thousand steps at the head of the party
        var pawmo = Mon("Pawmo", 30);
        for (int i = 0; i < 999; i++) Evolution.CountStep(pawmo);
        Assert.Null(LevelUp(pawmo));
        Evolution.CountStep(pawmo);
        Assert.Equal("Pawmot", LevelUp(pawmo));
        Evolution.CountStep(pawmo);
        Assert.Equal(1000, Evolution.Progress(pawmo, Evolution.StepsKey));

        // Twenty uses of one move; other moves don't count
        var primeape = Mon("Primeape", 40);
        for (int i = 0; i < 19; i++) Evolution.CountMoveUse(primeape, "Rage Fist");
        for (int i = 0; i < 50; i++) Evolution.CountMoveUse(primeape, "Scratch");
        Assert.Null(LevelUp(primeape));
        Evolution.CountMoveUse(primeape, "Rage Fist");
        Assert.Equal("Annihilape", LevelUp(primeape));

        // Three of its own kind knocked out
        var bisharp = Mon("Bisharp", 60);
        var rival = PokemonDatabase.Get("Bisharp")!;
        Evolution.CountDefeat(bisharp, rival);
        Evolution.CountDefeat(bisharp, PokemonDatabase.Get("Bidoof")!);
        Evolution.CountDefeat(bisharp, rival);
        Assert.Null(LevelUp(bisharp));
        Evolution.CountDefeat(bisharp, rival);
        Assert.Equal("Kingambit", LevelUp(bisharp));

        // A Pokémon that evolves some other way counts nothing
        var turtwig = Mon("Turtwig", 5);
        Evolution.CountStep(turtwig);
        Evolution.CountMoveUse(turtwig, "Tackle");
        Assert.Empty(turtwig.EvolutionProgress);

        // What was counted is spent by the evolution
        Evolution.Evolve(pawmo, Evolution.Find(pawmo, EvolutionTrigger.LevelUp, Day)!, Day);
        Assert.Empty(pawmo.EvolutionProgress);
    }

    [Fact]
    public void TestGimmighoulSpendsItsCoins()
    {
        var bag = new Inventory();
        var context = new EvolutionContext { Bag = bag };
        var gimmighoul = Mon("Gimmighoul", 20);
        bag.AddItem(Item("Gimmighoul Coin"), 998);
        Assert.Null(LevelUp(gimmighoul, context));
        bag.AddItem(Item("Gimmighoul Coin"), 3);
        Evolution.Evolve(gimmighoul, Evolution.Find(gimmighoul, EvolutionTrigger.LevelUp, context)!, context);
        Assert.Equal("Gholdengo", gimmighoul.Species.Name);
        Assert.Equal(2, bag.GetQuantity(Item("Gimmighoul Coin")));
    }

    // ---------------------------------------------------------------- Nincada

    [Fact]
    public void TestNincadaLeavesAShedinjaBehind()
    {
        var nincada = Mon("Nincada", 20);
        nincada.IvSpeed = 21;
        nincada.Nature = Nature.Jolly;
        var party = PartyOf(nincada);
        var bag = new Inventory();
        bag.AddItem(Item("Poké Ball"), 2);
        var context = new EvolutionContext { Party = party, Bag = bag };
        var moves = nincada.Moves.Select(m => m.Name).ToList();

        var outcome = Evolution.Evolve(nincada, Evolution.Find(nincada, EvolutionTrigger.LevelUp, context)!, context);

        Assert.Equal("Ninjask", nincada.Species.Name);
        Assert.Equal(2, party.Count);
        var shedinja = party.Members[1];
        Assert.Same(shedinja, outcome.Shed);
        Assert.Equal("Shedinja", shedinja.Species.Name);
        Assert.Equal((20, 21, Nature.Jolly), (shedinja.Level, shedinja.IvSpeed, shedinja.Nature));
        Assert.Equal(moves, shedinja.Moves.Select(m => m.Name));
        Assert.Equal("Wonder Guard", shedinja.AbilityName);
        Assert.Equal((1, 1), (shedinja.MaxHP, shedinja.CurrentHP));
        Assert.Equal(1, bag.GetQuantity(Item("Poké Ball")));
    }

    [Fact]
    public void TestNoShedinjaWithoutABallOrAFreePlace()
    {
        // No Poké Ball
        var nincada = Mon("Nincada", 20);
        var context = new EvolutionContext { Party = PartyOf(nincada), Bag = new Inventory() };
        Assert.Null(Evolution.Evolve(nincada, Evolution.Find(nincada, EvolutionTrigger.LevelUp, context)!, context).Shed);
        Assert.Equal(1, context.Party!.Count);

        // A full party
        var second = Mon("Nincada", 20);
        var bag = new Inventory();
        bag.AddItem(Item("Poké Ball"), 1);
        var full = new EvolutionContext { Party = PartyOf(second, Mon("Bidoof", 5), Mon("Bidoof", 5), Mon("Bidoof", 5), Mon("Bidoof", 5), Mon("Bidoof", 5)), Bag = bag };
        Assert.Null(Evolution.Evolve(second, Evolution.Find(second, EvolutionTrigger.LevelUp, full)!, full).Shed);
        Assert.Equal(1, bag.GetQuantity(Item("Poké Ball")));

        // Shedinja is never something a Pokémon turns into
        Assert.DoesNotContain(PokemonDatabase.GetAll().SelectMany(s => s.Evolutions ?? new()),
            e => e.Method == EvolutionMethod.LevelShedinja && Evolution.TriggerOf(e.Method) != null);
    }

    // ---------------------------------------------------------------- items

    [Fact]
    public void TestStonesEvolveTheRightPokemon()
    {
        Assert.Equal("Flareon", Use("Fire Stone", Mon("Eevee", 5)));
        Assert.Equal("Vaporeon", Use("Water Stone", Mon("Eevee", 5)));
        Assert.Equal("Jolteon", Use("Thunder Stone", Mon("Eevee", 5)));
        Assert.Equal("Raichu", Use("Thunder Stone", Mon("Pikachu", 5)));
        Assert.Null(Use("Fire Stone", Mon("Pikachu", 5)));
        Assert.Null(Use("Fire Stone", Mon("Turtwig", 5)));

        // A stone is no level-up: nothing happens to Eevee on its own
        Assert.Null(LevelUp(Mon("Eevee", 50)));
    }

    [Fact]
    public void TestStonesThatAskForAGender()
    {
        Assert.Equal("Gallade", Use("Dawn Stone", Mon("Kirlia", 20, Gender.Male)));
        Assert.Null(Use("Dawn Stone", Mon("Kirlia", 20, Gender.Female)));
        Assert.Equal("Froslass", Use("Dawn Stone", Mon("Snorunt", 20, Gender.Female)));
        Assert.Null(Use("Dawn Stone", Mon("Snorunt", 20, Gender.Male)));
    }

    [Fact]
    public void TestTheEverstoneStopsEverythingButStones()
    {
        var everstone = Item("Everstone");
        Assert.Equal(Evolution.EverstoneEffect, everstone.HoldEffect);

        var turtwig = Mon("Turtwig", 18);
        turtwig.HeldItem = everstone;
        Assert.Null(LevelUp(turtwig));

        var haunter = Mon("Haunter", 30);
        haunter.HeldItem = everstone;
        Assert.Null(Traded(haunter));

        var eevee = Mon("Eevee", 5);
        eevee.HeldItem = everstone;
        Assert.Equal("Flareon", Use("Fire Stone", eevee));

        // Platinum's oddity: Kadabra evolves whatever it holds
        var kadabra = Mon("Kadabra", 30);
        kadabra.HeldItem = everstone;
        Assert.Equal("Alakazam", Traded(kadabra));
    }

    // ---------------------------------------------------------------- trades

    [Fact]
    public void TestTradeEvolutions()
    {
        foreach (var (from, into) in new[] { ("Kadabra", "Alakazam"), ("Machoke", "Machamp"), ("Graveler", "Golem"), ("Haunter", "Gengar") })
        {
            Assert.Equal(into, Traded(Mon(from, 30)));
            // Only a trade does it
            Assert.Null(LevelUp(Mon(from, 99)));
        }
        Assert.Null(Traded(Mon("Turtwig", 50)));
    }

    [Fact]
    public void TestTradeEvolutionsThatNeedAHeldItem()
    {
        var onix = Mon("Onix", 30);
        Assert.Null(Traded(onix));
        onix.HeldItem = Item("Metal Coat");
        var evolution = Evolution.Find(onix, EvolutionTrigger.Trade, new EvolutionContext())!;
        Assert.Equal("Steelix", evolution.TargetSpecies);
        Evolution.Evolve(onix, evolution, new EvolutionContext());
        Assert.Equal("Steelix", onix.Species.Name);
        Assert.Null(onix.HeldItem);

        // Poliwhirl and Slowpoke take a King's Rock; Clamperl's item picks its evolution
        var clamperl = Mon("Clamperl", 20);
        clamperl.HeldItem = Item("Deep Sea Tooth");
        Assert.Equal("Huntail", Traded(clamperl));
        clamperl.HeldItem = Item("Deep Sea Scale");
        Assert.Equal("Gorebyss", Traded(clamperl));

        var electabuzz = Mon("Electabuzz", 40);
        electabuzz.HeldItem = Item("Electirizer");
        Assert.Equal("Electivire", Traded(electabuzz));
    }

    [Fact]
    public void TestKarrablastAndShelmetEvolveWhenTradedForEachOther()
    {
        Assert.Null(Traded(Mon("Karrablast", 20)));
        Assert.Null(Traded(Mon("Karrablast", 20), "Bidoof"));
        Assert.Equal("Escavalier", Traded(Mon("Karrablast", 20), "Shelmet"));
        Assert.Equal("Accelgor", Traded(Mon("Shelmet", 20), "Karrablast"));
    }

    [Fact]
    public void TestALinkingCordCountsAsATrade()
    {
        Assert.True(Evolution.IsUsedToEvolve(Item("Linking Cord")));
        Assert.Equal("Gengar", Use("Linking Cord", Mon("Haunter", 30)));
        Assert.Null(Use("Linking Cord", Mon("Turtwig", 30)));

        // The held item is still needed, and the Everstone still stops it
        var scyther = Mon("Scyther", 30);
        Assert.Null(Use("Linking Cord", scyther));
        scyther.HeldItem = Item("Metal Coat");
        Assert.Equal("Scizor", Use("Linking Cord", scyther));
        var machoke = Mon("Machoke", 30);
        machoke.HeldItem = Item("Everstone");
        Assert.Null(Use("Linking Cord", machoke));

        // Karrablast needs a Shelmet with it
        var karrablast = Mon("Karrablast", 20);
        Assert.Null(Use("Linking Cord", karrablast, new EvolutionContext { Party = PartyOf(karrablast) }));
        Assert.Equal("Escavalier", Use("Linking Cord", karrablast, new EvolutionContext { Party = PartyOf(karrablast, Mon("Shelmet", 20)) }));
    }

    // ---------------------------------------------------------------- spinning

    [Fact]
    public void TestMilceryEvolvesWhenThePlayerSpinsWhileItHoldsASweet()
    {
        var milcery = Mon("Milcery", 10);
        Assert.Null(Evolution.Find(milcery, EvolutionTrigger.Spin, Day));
        Assert.Null(LevelUp(milcery));
        foreach (var sweet in new[] { "Strawberry Sweet", "Love Sweet", "Berry Sweet", "Clover Sweet", "Flower Sweet", "Star Sweet", "Ribbon Sweet" })
        {
            milcery.HeldItem = Item(sweet);
            Assert.Equal("Alcremie", Evolution.Find(milcery, EvolutionTrigger.Spin, Day)?.TargetSpecies);
        }
        // Holding it is not enough without the spin
        Assert.Null(LevelUp(milcery));
    }

    [Fact]
    public void TestSpinTrackerWantsFourQuarterTurnsTheSameWay()
    {
        const float dt = 1f / 60f;
        var clockwise = new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Left, Direction.Up };

        var spin = new SpinTracker();
        var results = clockwise.Select(d => spin.Update(d, dt)).ToList();
        Assert.Equal(new[] { false, false, false, false, true }, results);

        // The other way round works too
        spin = new SpinTracker();
        Assert.True(clockwise.Reverse().Select(d => spin.Update(d, dt)).ToList().Last());

        // Turning back, or dawdling, starts the count again
        spin = new SpinTracker();
        var wobble = new[] { Direction.Up, Direction.Right, Direction.Down, Direction.Right, Direction.Down, Direction.Left };
        Assert.DoesNotContain(true, wobble.Select(d => spin.Update(d, dt)));

        spin = new SpinTracker();
        Assert.DoesNotContain(true, clockwise.Select(d => spin.Update(d, SpinTracker.MaxGap + 0.1f)));

        // Standing still between turns is fine as long as each turn comes soon enough
        spin = new SpinTracker();
        bool done = false;
        foreach (var d in clockwise)
            for (int i = 0; i < 20; i++) done |= spin.Update(d, dt);
        Assert.True(done);
    }

    // ---------------------------------------------------------------- what evolving does

    [Fact]
    public void TestEvolvingKeepsTheDamageTheNicknameAndTheAbilitySlot()
    {
        var shinx = Mon("Shinx", 15);
        shinx.AbilityName = "Intimidate";
        shinx.Nickname = "Sparky";
        shinx.CurrentHP = shinx.MaxHP - 7;
        Evolution.Evolve(shinx, Evolution.Find(shinx, EvolutionTrigger.LevelUp, Day)!, Day);
        Assert.Equal("Luxio", shinx.Species.Name);
        Assert.Equal("Sparky", shinx.DisplayName);
        Assert.Equal("Intimidate", shinx.AbilityName);
        Assert.Equal(shinx.MaxHP - 7, shinx.CurrentHP);

        // Without a nickname it takes the new species' name; a fainted Pokémon stays down
        var starly = Mon("Starly", 14);
        starly.CurrentHP = 0;
        starly.Status = StatusCondition.Faint;
        Evolution.Evolve(starly, Evolution.Find(starly, EvolutionTrigger.LevelUp, Day)!, Day);
        Assert.Equal("Staravia", starly.DisplayName);
        Assert.Equal(0, starly.CurrentHP);
    }

    [Fact]
    public void TestTheNewSpeciesOffersItsMoves()
    {
        // Platinum: Butterfree learns Confusion at level 10, the level Metapod evolves at
        var metapod = Mon("Metapod", 10);
        var outcome = Evolution.Evolve(metapod, Evolution.Find(metapod, EvolutionTrigger.LevelUp, Day)!, Day);
        Assert.Contains("Confusion", outcome.NewMoves);

        // Later games: moves learned on evolving, whatever the level
        var decidueye = PokemonDatabase.Get("Decidueye")!;
        var onEvolving = decidueye.Learnset.Where(m => m.Level == 0).Select(m => m.MoveName).ToList();
        Assert.NotEmpty(onEvolving);
        var dartrix = Mon("Dartrix", 50);
        outcome = Evolution.Evolve(dartrix, Evolution.Find(dartrix, EvolutionTrigger.LevelUp, Day)!, Day);
        Assert.All(onEvolving.Where(m => !dartrix.Knows(m)), m => Assert.Contains(m, outcome.NewMoves));

        // Nothing it already knows
        Assert.DoesNotContain(outcome.NewMoves, dartrix.Knows);
    }

    // ---------------------------------------------------------------- friendship

    [Fact]
    public void TestFriendshipGrowsMoreSlowlyTheHigherItIs()
    {
        var p = Mon("Golbat", 30);
        Assert.Equal(PokemonDatabase.Get("Golbat")!.BaseFriendship, p.Friendship);

        p.Friendship = 50;
        FriendshipRules.Apply(p, FriendshipEvent.LevelUp);
        Assert.Equal(55, p.Friendship);
        p.Friendship = 150;
        FriendshipRules.Apply(p, FriendshipEvent.LevelUp);
        Assert.Equal(153, p.Friendship);
        p.Friendship = 210;
        FriendshipRules.Apply(p, FriendshipEvent.LevelUp);
        Assert.Equal(212, p.Friendship);

        // Fainting costs one; to a foe thirty levels up, five, or ten once friendship is high
        FriendshipRules.Apply(p, FriendshipEvent.Faint);
        Assert.Equal(211, p.Friendship);
        FriendshipRules.Apply(p, FriendshipEvent.FaintToStronger);
        Assert.Equal(201, p.Friendship);

        // It stays within 0 and 255
        p.Friendship = 254;
        FriendshipRules.Apply(p, FriendshipEvent.LevelUp);
        Assert.Equal(255, p.Friendship);
        p.Friendship = 2;
        FriendshipRules.Apply(p, FriendshipEvent.FaintToStronger);
        Assert.Equal(0, p.Friendship);
    }

    [Fact]
    public void TestTheSootheBellAndTheLuxuryBallHelp()
    {
        var bell = Mon("Golbat", 30);
        bell.Friendship = 50;
        bell.HeldItem = Item("Soothe Bell");
        FriendshipRules.Apply(bell, FriendshipEvent.LevelUp);
        Assert.Equal(57, bell.Friendship); // 5 × 1.5, rounded down

        var luxury = Mon("Golbat", 30);
        luxury.Friendship = 50;
        luxury.Ball = "Luxury Ball";
        FriendshipRules.Apply(luxury, FriendshipEvent.LevelUp);
        Assert.Equal(56, luxury.Friendship);

        // Neither softens a loss
        bell.Friendship = luxury.Friendship = 50;
        bell.Ball = "Luxury Ball";
        FriendshipRules.Apply(bell, FriendshipEvent.Faint);
        Assert.Equal(49, bell.Friendship);
    }

    [Fact]
    public void TestWalkingAndLevellingUpRaiseFriendship()
    {
        // A walk cycle is a coin flip for each Pokémon
        var rng = new Random(3);
        var p = Mon("Golbat", 30);
        p.Friendship = 0;
        for (int i = 0; i < 200; i++) FriendshipRules.Apply(p, FriendshipEvent.WalkCycle, rng);
        Assert.InRange(p.Friendship, 70, 130);

        // Every level gained counts, from a battle or a Rare Candy
        var turtwig = Mon("Turtwig", 5);
        int before = turtwig.Friendship;
        turtwig.GainExp(turtwig.ExpForNextLevel - turtwig.CurrentExp, out _);
        Assert.Equal(before + 5, turtwig.Friendship);
    }

    // ---------------------------------------------------------------- battles

    private static BattleEngine Wild(Party party, Pokemon foe, int seed = 1)
    {
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { foe }, Random = new Random(seed * 7919 + 17)
        });
        Settle(battle);
        return battle;
    }

    private static List<string> Settle(BattleEngine battle)
    {
        var said = new List<string>();
        for (int i = 0; i < 400 && !battle.IsBattleOver; i++)
        {
            if (battle.HUD.MenuState != BattleMenuState.Message) break;
            if (battle.IsWaitingForConfirm) said.Add(battle.CurrentMessage);
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        return said;
    }

    private static readonly MoveData SureHit = new() { Name = "Sure Hit", Type = PokemonType.Normal, Category = MoveCategory.Physical, Power = 200, Accuracy = 0, MaxPP = 30 };
    private static readonly MoveData Idle = new() { Name = "Idle", Type = PokemonType.Normal, Category = MoveCategory.Status, MaxPP = 40, Target = MoveTarget.User };

    [Fact]
    public void TestNothingEvolvesWhileTheBattleIsOn()
    {
        // A Turtwig one battle away from level 18
        var turtwig = Mon("Turtwig", 17);
        turtwig.CurrentExp = turtwig.ExpForNextLevel - 1;
        turtwig.Moves.Clear();
        turtwig.Moves.Add(new Move(SureHit));
        var foe = Mon("Bidoof", 10);
        foe.Moves.Clear();
        foe.Moves.Add(new Move(Idle));

        var battle = Wild(PartyOf(turtwig), foe);
        battle.SelectMove(0);
        var said = Settle(battle);

        Assert.Equal(BattleResult.PlayerVictory, battle.Result);
        Assert.Equal(18, turtwig.Level);
        Assert.Equal("Turtwig", turtwig.Species.Name);
        Assert.DoesNotContain(said, m => m.Contains("evolv"));

        // The battle hands over who grew, and the rules take it from there
        Assert.Equal(new[] { turtwig }, battle.LeveledUp);
        Assert.Equal("Grotle", LevelUp(turtwig));
    }

    [Fact]
    public void TestBattlesCountMovesAndKnockOutsForEvolution()
    {
        var bisharp = Mon("Bisharp", 60);
        bisharp.Moves.Clear();
        bisharp.Moves.Add(new Move(SureHit));
        var foe = Mon("Bisharp", 5);
        foe.Moves.Clear();
        foe.Moves.Add(new Move(Idle));
        var battle = Wild(PartyOf(bisharp), foe);
        battle.SelectMove(0);
        Settle(battle);
        Assert.Equal(1, Evolution.Progress(bisharp, Evolution.DefeatKey("Bisharp")));

        var primeape = Mon("Primeape", 40);
        primeape.Moves.Clear();
        primeape.Moves.Add(MoveDatabase.Create("Rage Fist"));
        var wall = Mon("Bidoof", 100);
        wall.Moves.Clear();
        wall.Moves.Add(new Move(Idle));
        battle = Wild(PartyOf(primeape), wall);
        battle.SelectMove(0);
        Settle(battle);
        battle.SelectMove(0);
        Settle(battle);
        Assert.Equal(2, Evolution.Progress(primeape, Evolution.MoveKey("Rage Fist")));
    }

    [Fact]
    public void TestFaintingInBattleCostsFriendship()
    {
        var mine = Mon("Bidoof", 5);
        mine.Moves.Clear();
        mine.Moves.Add(new Move(Idle));
        int before = mine.Friendship;
        var foe = Mon("Starly", 20);
        foe.Moves.Clear();
        foe.Moves.Add(new Move(SureHit));
        var battle = Wild(PartyOf(mine), foe);
        battle.SelectMove(0);
        Settle(battle);
        Assert.True(mine.IsFainted);
        Assert.Equal(before - 1, mine.Friendship);

        // Thirty levels above it hurts more
        var small = Mon("Bidoof", 5);
        small.Moves.Clear();
        small.Moves.Add(new Move(Idle));
        var giant = Mon("Starly", 35);
        giant.Moves.Clear();
        giant.Moves.Add(new Move(SureHit));
        battle = Wild(PartyOf(small), giant);
        battle.SelectMove(0);
        Settle(battle);
        Assert.Equal(before - 5, small.Friendship);
    }

    // ---------------------------------------------------------------- the scene

    private static void Run(EvolutionScreen screen, float seconds)
    {
        for (int i = 0; i < (int)(seconds * 60f); i++) screen.Advance(1f / 60f);
    }

    private const float WholeScene = EvolutionScreen.NoticeTime + EvolutionScreen.GatherTime + EvolutionScreen.MorphTime +
                                     EvolutionScreen.BurstTime + EvolutionScreen.RevealTime + 0.5f;

    [Fact]
    public void TestTheSceneEvolvesThePokemon()
    {
        var turtwig = Mon("Turtwig", 18);
        var screen = new EvolutionScreen();
        screen.Begin(turtwig, Evolution.Find(turtwig, EvolutionTrigger.LevelUp, Day)!, Day, cancellable: true);
        Assert.True(screen.IsActive);
        Assert.Equal("What? Turtwig is evolving!", screen.Message);

        // Still a Turtwig while the light gathers
        Run(screen, EvolutionScreen.NoticeTime + EvolutionScreen.GatherTime + 1f);
        Assert.Equal(EvolutionPhase.Morph, screen.Phase);
        Assert.Equal("Turtwig", turtwig.Species.Name);
        Assert.Null(screen.Outcome);

        Run(screen, WholeScene);
        Assert.Equal(EvolutionPhase.Congratulate, screen.Phase);
        Assert.Equal("Grotle", turtwig.Species.Name);
        Assert.Equal("Congratulations! Your Turtwig evolved into Grotle!", screen.Message);
        Assert.Equal("Grotle", screen.Outcome!.Into.Name);

        // The scene waits for the player, then ends (or goes on to the moves)
        Run(screen, 5f);
        Assert.Equal(EvolutionPhase.Congratulate, screen.Phase);
        for (int i = 0; i < 6 && screen.IsActive; i++) screen.PressConfirm();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void TestBStopsALevelUpEvolutionButNotAStone()
    {
        var turtwig = Mon("Turtwig", 18);
        var screen = new EvolutionScreen();
        screen.Begin(turtwig, Evolution.Find(turtwig, EvolutionTrigger.LevelUp, Day)!, Day, cancellable: true);
        Run(screen, EvolutionScreen.NoticeTime + EvolutionScreen.GatherTime + 1f);
        screen.PressCancel();
        Assert.Equal(EvolutionPhase.Stopped, screen.Phase);
        Assert.Equal("Huh? Turtwig stopped evolving!", screen.Message);
        Run(screen, WholeScene);
        screen.PressConfirm();
        Assert.False(screen.IsActive);
        Assert.Equal("Turtwig", turtwig.Species.Name);
        Assert.Null(screen.Outcome);

        // It tries again at the next level
        Assert.Equal("Grotle", LevelUp(turtwig));

        // A stone's evolution goes through whatever is pressed
        var eevee = Mon("Eevee", 5);
        var context = new EvolutionContext { Item = Item("Fire Stone") };
        screen.Begin(eevee, Evolution.Find(eevee, EvolutionTrigger.UseItem, context)!, context, cancellable: false);
        for (int i = 0; i < (int)(WholeScene * 60f); i++)
        {
            if (screen.Phase is EvolutionPhase.Gather or EvolutionPhase.Morph) screen.PressCancel();
            screen.Advance(1f / 60f);
        }
        Assert.Equal("Flareon", eevee.Species.Name);
    }

    [Fact]
    public void TestTheSceneShowsOneShapeThenTheOther()
    {
        var turtwig = Mon("Turtwig", 18);
        var screen = new EvolutionScreen();
        screen.Begin(turtwig, Evolution.Find(turtwig, EvolutionTrigger.LevelUp, Day)!, Day, cancellable: true);

        var first = screen.Look();
        Assert.Equal((1f, 0f, 0f), (first.OldScale, first.NewScale, first.White));

        // During the morph both shapes are pure light and only one shows at a time
        while (screen.Phase != EvolutionPhase.Morph) screen.Advance(1f / 60f);
        bool sawOld = false, sawNew = false;
        while (screen.Phase == EvolutionPhase.Morph)
        {
            var look = screen.Look();
            Assert.Equal(1f, look.White);
            Assert.True(look.OldScale == 0f || look.NewScale == 0f);
            sawOld |= look.OldScale > 0.3f;
            sawNew |= look.NewScale > 0.3f;
            screen.Advance(1f / 60f);
        }
        Assert.True(sawOld && sawNew);

        // It ends on the new shape in its own colours
        Run(screen, EvolutionScreen.BurstTime + EvolutionScreen.RevealTime + 0.2f);
        var last = screen.Look();
        Assert.Equal((0f, 1f, 0f, 0f), (last.OldScale, last.NewScale, last.White, last.Flash));
    }

    [Fact]
    public void TestAFreshlyEvolvedPokemonLearnsItsNewMoves()
    {
        // With a free place it just learns the move
        var metapod = Mon("Metapod", 10);
        metapod.Moves.Clear();
        metapod.Moves.Add(MoveDatabase.Create("Harden"));
        var screen = new EvolutionScreen();
        screen.Begin(metapod, Evolution.Find(metapod, EvolutionTrigger.LevelUp, Day)!, Day, cancellable: true);
        Run(screen, WholeScene);
        screen.PressConfirm();
        Assert.Equal(EvolutionPhase.MoveNotice, screen.Phase);
        Assert.Equal("Butterfree learned Confusion!", screen.Message);
        Assert.True(metapod.Knows("Confusion"));
        screen.PressConfirm();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void TestWithFourMovesThePlayerChoosesWhichToForget()
    {
        Pokemon Full()
        {
            var p = Mon("Metapod", 10);
            p.Moves.Clear();
            foreach (var m in new[] { "Harden", "Tackle", "String Shot", "Bug Bite" }) p.Moves.Add(MoveDatabase.Create(m));
            return p;
        }

        EvolutionScreen ToTheChoice(Pokemon p)
        {
            var s = new EvolutionScreen();
            s.Begin(p, Evolution.Find(p, EvolutionTrigger.LevelUp, Day)!, Day, cancellable: true);
            Run(s, WholeScene);
            s.PressConfirm();
            Assert.Equal(EvolutionPhase.MoveChoice, s.Phase);
            Assert.Equal("Confusion", s.MoveToLearn);
            return s;
        }

        // Forget the second move
        var first = Full();
        var screen = ToTheChoice(first);
        screen.MoveCursor(1);
        screen.PressConfirm();
        Assert.Equal("Butterfree forgot Tackle and learned Confusion!", screen.Message);
        Assert.Equal(new[] { "Harden", "Confusion", "String Shot", "Bug Bite" }, first.Moves.Select(m => m.Name));
        screen.PressConfirm();
        Assert.False(screen.IsActive);

        // The last row, or B, keeps all four
        var second = Full();
        screen = ToTheChoice(second);
        screen.MoveCursor(-1);
        Assert.Equal(4, screen.Cursor);
        screen.PressConfirm();
        Assert.Equal("Butterfree did not learn Confusion.", screen.Message);
        Assert.False(second.Knows("Confusion"));

        var third = Full();
        screen = ToTheChoice(third);
        screen.PressCancel();
        Assert.False(third.Knows("Confusion"));
        screen.PressConfirm();
        Assert.False(screen.IsActive);
    }

    // ---------------------------------------------------------------- the bag

    [Fact]
    public void TestAStoneFromTheBagAsksWhichPokemon()
    {
        var turtwig = Mon("Turtwig", 5);
        var eevee = Mon("Eevee", 5);
        var party = PartyOf(turtwig, eevee);
        var bag = new Inventory();
        bag.AddItem(Item("Fire Stone"), 2);
        var notes = new List<string>();
        EvolutionContext Context() => new() { Party = party, Bag = bag };

        var screen = new BagScreen();
        screen.Open();
        Assert.True(BagScreen.NeedsTarget(Item("Fire Stone")));
        screen.BeginTargetChoice(Item("Fire Stone"));

        // Not on Turtwig: the stone stays in the bag and the choice stays open
        screen.UseOnTarget(bag, party, notes.Add, Context());
        Assert.Equal("It won't have any effect.", notes.Last());
        Assert.Equal(2, bag.GetQuantity(Item("Fire Stone")));
        Assert.NotNull(screen.ChoosingFor);
        Assert.Null(screen.TakeEvolution());

        // On Eevee: the stone is spent and the evolution is handed over, not to be stopped
        screen.MoveTarget(1, 0, party.Count);
        screen.UseOnTarget(bag, party, notes.Add, Context());
        Assert.Equal(1, bag.GetQuantity(Item("Fire Stone")));
        var request = screen.TakeEvolution()!;
        Assert.Equal((eevee, "Flareon", false), (request.Pokemon, request.Evolution.TargetSpecies, request.Cancellable));
        Assert.Null(screen.TakeEvolution());
        Assert.Null(screen.ChoosingFor);
    }

    [Fact]
    public void TestARareCandyCanSetOffAnEvolution()
    {
        var turtwig = Mon("Turtwig", 17);
        var party = PartyOf(Mon("Bidoof", 5), turtwig);
        var bag = new Inventory();
        bag.AddItem(Item("Rare Candy"), 3);
        var screen = new BagScreen();
        screen.Open();
        screen.BeginTargetChoice(Item("Rare Candy"));

        // The lead Bidoof just grows
        screen.UseOnTarget(bag, party, _ => { }, new EvolutionContext { Party = party, Bag = bag });
        Assert.Equal(6, party.Members[0].Level);
        Assert.Null(screen.TakeEvolution());

        // Turtwig reaches level 18: it evolves, and the player may stop it
        screen.BeginTargetChoice(Item("Rare Candy"));
        screen.MoveTarget(1, 0, party.Count);
        screen.UseOnTarget(bag, party, _ => { }, new EvolutionContext { Party = party, Bag = bag });
        var request = screen.TakeEvolution()!;
        Assert.Equal((turtwig, "Grotle", true), (request.Pokemon, request.Evolution.TargetSpecies, request.Cancellable));
        Assert.Equal("Turtwig", turtwig.Species.Name);
        Assert.Equal(1, bag.GetQuantity(Item("Rare Candy")));

        // A fainted Pokémon comes round
        var fainted = Mon("Bidoof", 5);
        fainted.CurrentHP = 0;
        fainted.Status = StatusCondition.Faint;
        var sick = PartyOf(fainted);
        screen.BeginTargetChoice(Item("Rare Candy"));
        screen.UseOnTarget(bag, sick, _ => { }, new EvolutionContext { Party = sick, Bag = bag });
        Assert.False(fainted.IsFainted);
        Assert.True(fainted.CurrentHP > 0);
    }

    [Fact]
    public void TestItemsPokemonHoldToEvolveCanBeGivenFromTheBag()
    {
        var holdMethods = new[]
        {
            EvolutionMethod.TradeHoldingItem, EvolutionMethod.LevelHoldingItem, EvolutionMethod.LevelHoldingItemDay,
            EvolutionMethod.LevelHoldingItemNight, EvolutionMethod.SpinHoldingItem
        };
        var held = PokemonDatabase.GetAll().SelectMany(s => s.Evolutions ?? new()).Where(e => holdMethods.Contains(e.Method)).Select(e => e.Item!).Distinct().ToList();
        Assert.Contains("Metal Coat", held);
        Assert.All(held.Append("Everstone"), name => Assert.True(BagScreen.NeedsTarget(Item(name)), name));

        // To the Pokémon the player picks, not just the first
        var onix = Mon("Onix", 20);
        var party = PartyOf(Mon("Bidoof", 5), onix);
        var bag = new Inventory();
        bag.AddItem(Item("Metal Coat"), 1);
        var screen = new BagScreen();
        screen.Open();
        screen.BeginTargetChoice(Item("Metal Coat"));
        screen.MoveTarget(1, 0, party.Count);
        screen.UseOnTarget(bag, party, _ => { }, new EvolutionContext { Party = party, Bag = bag });
        Assert.Equal("Metal Coat", onix.HeldItem?.Name);
        Assert.Equal(0, bag.GetQuantity(Item("Metal Coat")));
    }

    // ---------------------------------------------------------------- forms (plan 03 · D11)

    private static Pokemon InForm(string species, int level, string form, Gender gender = Gender.Male)
    {
        var p = Mon(species, level, gender);
        p.ChangeForm(form);
        return p;
    }

    [Fact]
    public void TestARegionalFormEvolvesOnlyByItsOwnEvolutions()
    {
        Assert.Equal("Persian", LevelUp(Mon("Meowth", 28)));
        var galar = InForm("Meowth", 28, "Meowth-Galar");
        Assert.Equal("Perrserker", LevelUp(galar));
        Evolution.Evolve(galar, Evolution.Find(galar, EvolutionTrigger.LevelUp, Day)!, Day);
        Assert.Equal(("Perrserker", (string?)null), (galar.Species.Name, galar.Form));

        // Alolan Meowth waits for friendship instead, and becomes an Alolan Persian
        var alola = InForm("Meowth", 28, "Meowth-Alola");
        Assert.Null(LevelUp(alola));
        alola.Friendship = FriendshipRules.EvolveAt;
        Evolution.Evolve(alola, Evolution.Find(alola, EvolutionTrigger.LevelUp, Day)!, Day);
        Assert.Equal(("Persian", "Persian-Alola", PokemonType.Dark), (alola.Species.Name, alola.Form, alola.PrimaryType));

        // Alolan Vulpix wants an Ice Stone, the other a Fire Stone
        Assert.Null(Use("Ice Stone", Mon("Vulpix", 10)));
        Assert.Null(Use("Fire Stone", InForm("Vulpix", 10, "Vulpix-Alola")));
        Assert.Equal("Ninetales", Use("Ice Stone", InForm("Vulpix", 10, "Vulpix-Alola")));

        // Hisuian Sneasel takes its Razor Claw by day, into a species of its own
        var hisui = InForm("Sneasel", 30, "Sneasel-Hisui");
        hisui.HeldItem = Item("Razor Claw");
        Assert.Equal("Sneasler", LevelUp(hisui));
        Assert.Null(LevelUp(hisui, Night));
        // A Galarian Linoone becomes Obstagoon at night, and a Linoone of Hoenn never does
        Assert.Equal("Obstagoon", LevelUp(InForm("Linoone", 35, "Linoone-Galar"), Night));
        Assert.Null(LevelUp(Mon("Linoone", 35), Night));
    }

    [Fact]
    public void TestALookCarriesOverAndOtherFormsKeepTheirSpeciesEvolutions()
    {
        // A female Burmy in a sandy cloak becomes a sandy Wormadam, part Ground type
        var sandy = InForm("Burmy", 20, "Burmy-Sandy", Gender.Female);
        var evolution = Evolution.Find(sandy, EvolutionTrigger.LevelUp, Day)!;
        Assert.Equal("Wormadam-Sandy", evolution.TargetForm);
        Assert.Equal("Wormadam-Sandy", Evolution.ModelAfter(sandy, evolution));
        Evolution.Evolve(sandy, evolution, Day);
        Assert.Equal(("Wormadam", "Wormadam-Sandy", (PokemonType?)PokemonType.Ground), (sandy.Species.Name, sandy.Form, sandy.SecondaryType));

        // A male one still becomes Mothim, which has no cloaks; a plant-cloaked female a plain Wormadam
        Assert.Equal("Mothim", LevelUp(InForm("Burmy", 20, "Burmy-Sandy")));
        var plant = Mon("Burmy", 20, Gender.Female);
        Evolution.Evolve(plant, Evolution.Find(plant, EvolutionTrigger.LevelUp, Day)!, Day);
        Assert.Equal(("Wormadam", (string?)null), (plant.Species.Name, plant.Form));
    }

    [Fact]
    public void TestAnEvolutionIntoARegionalFormHappensOnlyInItsRegion()
    {
        var context = new EvolutionContext { Item = Item("Thunder Stone"), Region = "Sinnoh" };
        var pikachu = Mon("Pikachu", 20);
        Assert.Null(Evolution.Find(pikachu, EvolutionTrigger.UseItem, context)!.TargetForm);
        context.Region = "Alola";
        var alolan = Evolution.Find(pikachu, EvolutionTrigger.UseItem, context)!;
        Assert.Equal("Raichu-Alola", alolan.TargetForm);
        Evolution.Evolve(pikachu, alolan, context);
        Assert.Equal(("Raichu", "Raichu-Alola", (PokemonType?)PokemonType.Psychic), (pikachu.Species.Name, pikachu.Form, pikachu.SecondaryType));

        // Koffing becomes a Galarian Weezing in Galar, and a Weezing anywhere else
        Assert.Null(Evolution.Find(Mon("Koffing", 35), EvolutionTrigger.LevelUp, Day)!.TargetForm);
        Assert.Equal("Weezing-Galar", Evolution.Find(Mon("Koffing", 35), EvolutionTrigger.LevelUp, new EvolutionContext { Region = "Galar" })!.TargetForm);
    }

    [Fact]
    public void TestOwnTempoRockruffEvolvesAtDusk()
    {
        var dusk = new EvolutionContext { IsDusk = true };
        // The ordinary Rockruff is a Midday Lycanroc by day, dusk included, and a Midnight one at night
        Assert.Null(Evolution.Find(Mon("Rockruff", 25), EvolutionTrigger.LevelUp, dusk)!.TargetForm);
        Assert.Equal("Lycanroc-Midnight", Evolution.Find(Mon("Rockruff", 25), EvolutionTrigger.LevelUp, Night)!.TargetForm);

        // One with Own Tempo evolves at dusk alone, into the Dusk Form
        var ownTempo = InForm("Rockruff", 25, "Rockruff-Own-Tempo");
        Assert.Null(LevelUp(ownTempo));
        Assert.Null(LevelUp(ownTempo, Night));
        Evolution.Evolve(ownTempo, Evolution.Find(ownTempo, EvolutionTrigger.LevelUp, dusk)!, dusk);
        Assert.Equal(("Lycanroc", "Lycanroc-Dusk"), (ownTempo.Species.Name, ownTempo.Form));
    }

    [Fact]
    public void TestSirfetchdRunerigusAndBasculegionWaitForWhatTheyCount()
    {
        // Three critical hits in one battle, and the battle over: not a level-up
        var farfetchd = InForm("Farfetch'd", 30, "Farfetch'd-Galar");
        Evolution.CountCriticalHit(farfetchd);
        Evolution.CountCriticalHit(farfetchd);
        Assert.Null(Evolution.Find(farfetchd, EvolutionTrigger.BattleEnd, Day));
        Evolution.CountCriticalHit(farfetchd);
        Assert.Null(LevelUp(farfetchd));
        Assert.Equal("Sirfetch'd", Evolution.Find(farfetchd, EvolutionTrigger.BattleEnd, Day)?.TargetSpecies);
        Assert.Equal(EvolutionTrigger.BattleEnd, Evolution.TriggerOf(EvolutionMethod.CriticalHits));
        // The next battle counts from nothing; a Farfetch'd of Kanto counts nothing at all
        Evolution.BeginBattle(farfetchd);
        Assert.Null(Evolution.Find(farfetchd, EvolutionTrigger.BattleEnd, Day));
        var kanto = Mon("Farfetch'd", 30);
        Evolution.CountCriticalHit(kanto);
        Assert.Empty(kanto.EvolutionProgress);

        // 49 HP lost to moves, then a level-up where the Stone Arch is
        var arch = new EvolutionContext { Sites = new[] { "Stone Arch" } };
        var yamask = InForm("Yamask", 30, "Yamask-Galar");
        Evolution.CountDamageTaken(yamask, 30);
        Evolution.CountDamageTaken(yamask, 18);
        Assert.Null(LevelUp(yamask, arch));
        Evolution.CountDamageTaken(yamask, 20);
        Assert.Equal(49, Evolution.Progress(yamask, Evolution.DamageKey));
        Assert.Null(LevelUp(yamask));
        Assert.Equal("Runerigus", LevelUp(yamask, arch));
        // Fainting loses it
        Evolution.CountFaint(yamask);
        Assert.Null(LevelUp(yamask, arch));
        // A Yamask of Unova becomes Cofagrigus as ever, and a Galarian one never does
        Assert.Equal("Cofagrigus", LevelUp(Mon("Yamask", 34)));
        Assert.Null(LevelUp(InForm("Yamask", 34, "Yamask-Galar")));

        // 294 HP lost to its own recoil; a female becomes the female Basculegion
        var basculin = InForm("Basculin", 30, "Basculin-White-Striped", Gender.Female);
        Evolution.CountRecoil(basculin, 293);
        Assert.Null(LevelUp(basculin));
        Evolution.CountRecoil(basculin, 1);
        var evolution = Evolution.Find(basculin, EvolutionTrigger.LevelUp, Day)!;
        Assert.Equal("Basculegion-Female", Evolution.ModelAfter(basculin, evolution));
        Evolution.Evolve(basculin, evolution, Day);
        Assert.Equal(("Basculegion", "Basculegion-Female"), (basculin.Species.Name, basculin.Form));
        Assert.Empty(basculin.EvolutionProgress);
        // A Red-Striped Basculin counts nothing
        var red = Mon("Basculin", 30);
        Evolution.CountRecoil(red, 300);
        Assert.Empty(red.EvolutionProgress);
    }

    [Fact]
    public void TestABattleCountsCriticalHitsDamageAndRecoil()
    {
        // Galarian Farfetch'd lands a critical hit each turn here, on a foe that can take three
        var farfetchd = Scenario.Mon("Farfetch'd", 50, "Tackle");
        farfetchd.ChangeForm("Farfetch'd-Galar");
        var battle = Scenario.Battle(farfetchd, Scenario.Mon("Blissey", 100),
            Scenario.Steady().Force(PokemonPlatinumEngine.Battle.Sim.RollKind.Critical, 0));
        for (int i = 0; i < 3; i++) Scenario.Turn(battle);
        Assert.False(battle.IsBattleOver);
        Assert.Equal(3, Evolution.Progress(farfetchd, Evolution.CriticalHitsKey));
        // The next battle starts the count again
        Scenario.Battle(farfetchd, Scenario.Mon("Blissey", 100));
        Assert.Equal(0, Evolution.Progress(farfetchd, Evolution.CriticalHitsKey));

        // White-Striped Basculin pays for Double-Edge in recoil
        var basculin = Scenario.Mon("Basculin", 50, "Double-Edge");
        basculin.ChangeForm("Basculin-White-Striped");
        Scenario.Turn(Scenario.Battle(basculin, Scenario.Mon("Blissey", 100)));
        Assert.True(basculin.CurrentHP < basculin.MaxHP);
        Assert.Equal(basculin.MaxHP - basculin.CurrentHP, Evolution.Progress(basculin, Evolution.RecoilKey));

        // Galarian Yamask counts the hits it takes, until it faints
        var yamask = Scenario.Mon("Yamask", 50);
        yamask.ChangeForm("Yamask-Galar");
        Scenario.Turn(Scenario.Battle(yamask, Scenario.Mon("Rattata", 30, "Bite")));
        Assert.True(yamask.CurrentHP < yamask.MaxHP);
        Assert.Equal(yamask.MaxHP - yamask.CurrentHP, Evolution.Progress(yamask, Evolution.DamageKey));
        Scenario.Turn(Scenario.Battle(yamask, Scenario.Mon("Tyranitar", 100, "Crunch")));
        Assert.True(yamask.IsFainted);
        Assert.Equal(0, Evolution.Progress(yamask, Evolution.DamageKey));
    }

    // ---------------------------------------------------------------- saves and data

    [Fact]
    public void TestSavesKeepWhatEvolutionNeeds()
    {
        var p = Mon("Pawmo", 30);
        p.Friendship = 187;
        p.Beauty = 42;
        p.Personality = 0xDEADBEEF;
        p.Ball = "Luxury Ball";
        for (int i = 0; i < 12; i++) Evolution.CountStep(p);

        var loaded = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal((187, 42, 0xDEADBEEF, "Luxury Ball"), (loaded.Friendship, loaded.Beauty, loaded.Personality, loaded.Ball));
        Assert.Equal(12, Evolution.Progress(loaded, Evolution.StepsKey));

        // A save from before these existed: base friendship, and a personality of its own
        var old = new SavedPokemonData { SpeciesName = "Golbat", Level = 30 }.ToPokemon();
        Assert.Equal(PokemonDatabase.Get("Golbat")!.BaseFriendship, old.Friendship);
        Assert.Empty(old.EvolutionProgress);
    }

    [Fact]
    public void TestEveryEvolutionInTheDataHasARule()
    {
        string[] sites = { "Moss Rock", "Ice Rock", "Magnetic Field", "Stone Arch" };
        foreach (var s in PokemonDatabase.GetAll())
        {
            foreach (var e in s.Evolutions ?? new())
            {
                string what = $"{s.Name} into {e.TargetSpecies} ({e.Method})";
                Assert.True(e.Method != EvolutionMethod.Other, what);
                Assert.True(Evolution.TriggerOf(e.Method) != null || e.Method == EvolutionMethod.LevelShedinja, what);

                // The fields its method reads are there
                switch (e.Method)
                {
                    case EvolutionMethod.Level or EvolutionMethod.LevelDay or EvolutionMethod.LevelNight or EvolutionMethod.LevelDusk
                        or EvolutionMethod.LevelMale or EvolutionMethod.LevelFemale:
                        Assert.True(e.Level > 0, what);
                        break;
                    case EvolutionMethod.LevelAtLocation:
                        Assert.Contains(e.Location, sites);
                        break;
                    case EvolutionMethod.LevelAfterDamage:
                        Assert.Contains(e.Location, sites);
                        Assert.True(e.Value > 0, what);
                        break;
                    case EvolutionMethod.LevelAfterSteps or EvolutionMethod.LevelAfterMoveUses or EvolutionMethod.LevelAfterDefeating
                        or EvolutionMethod.LevelWithItemsInBag or EvolutionMethod.Beauty or EvolutionMethod.CriticalHits or EvolutionMethod.LevelAfterRecoil:
                        Assert.True(e.Value > 0, what);
                        break;
                }
                if (Evolution.TriggerOf(e.Method) == EvolutionTrigger.UseItem) Assert.True(Evolution.IsUsedToEvolve(Item(e.Item!)), what);
                if (e.Method is EvolutionMethod.LevelAfterMoveUses or EvolutionMethod.LevelKnowsMove) Assert.NotNull(e.Move);
                if (e.Method is EvolutionMethod.LevelAfterDefeating or EvolutionMethod.LevelWithSpeciesInParty or EvolutionMethod.TradeWithSpecies)
                    Assert.NotNull(PokemonDatabase.Get(e.Species!));
            }
        }

        // No species has two evolutions that would always happen together: the first would hide the second. A form's
        // own and a region's own are apart from the rest (Alolan Diglett and Diglett both evolve at level 26)
        foreach (var s in PokemonDatabase.GetAll().Where(s => s.Evolutions != null))
            foreach (var same in s.Evolutions!.GroupBy(e => (e.FromForm, e.Region)))
                Assert.True(same.Count(e => e.Method == EvolutionMethod.Level) <= 1, s.Name);
    }

    [Fact]
    public void TestMapsCanNameTheirEvolutionSites()
    {
        // No map has one yet (Eterna Forest, Route 217 and Mt. Coronet come with plan 01), but the files can say so
        var file = MapFile.FromMap(Fixtures.Map("Route201"));
        Assert.Null(file.EvolutionSites);
        file.EvolutionSites = new List<string> { "Moss Rock" };
        var map = file.ToMap();
        Assert.Equal(new[] { "Moss Rock" }, map.EvolutionSites);
        Assert.Equal(new[] { "Moss Rock" }, MapFile.FromMap(map).EvolutionSites);

        string[] known = { "Moss Rock", "Ice Rock", "Magnetic Field", "Stone Arch" };
        foreach (var name in MapDatabase.MapNames)
        {
            var places = MapDatabase.Get(name);
            Assert.All(places.EvolutionSites.Concat(places.Areas.SelectMany(a => a.EvolutionSites)), site => Assert.Contains(site, known));
        }
    }
}
