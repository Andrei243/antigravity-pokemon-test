using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 14 · B3: hordes on the bare core (<c>docs/mechanics/rulings.md</c>, "Formats of the later games", "Hordes").
/// One of the player's Pokémon against up to five wild ones, under the rules that have hordes
/// (<see cref="Ruleset.Hordes"/>): the places, a spread hit on all five, a ball refused until one is left, the EXP of
/// each faint, the run against the fastest, what a horde refuses as a wild double battle does, a record and its replay,
/// and a hundred hordes played to their end by the trainer AI on both sides. The screen is plan 14 · B4's.
/// </summary>
public class HordeTests
{
    private static Pokemon[] Five(string species, int level) => Enumerable.Range(0, 5).Select(_ => Mon(species, level)).ToArray();

    [Fact]
    public void AHordeIsOneAgainstFiveAndSaysSoAsItComesOut()
    {
        var core = Horde(Mon("Golem", 40, "Rock Slide"), Five("Zubat", 20));
        Assert.Equal(BattleFormat.Horde, core.Format);
        Assert.True(core.IsHorde);
        Assert.False(core.IsSingle);
        Assert.False(core.IsDouble);
        Assert.Single(core.PlayerSlots);
        Assert.Equal(5, core.EnemySlots.Count);
        Assert.Equal(new[] { Foe, Foe2, Foe3, Foe4, Foe5 }, core.EnemySlots.Select(b => b.Place));
        Assert.All(core.EnemySlots, b => Assert.True(b.IsActive));
        // The format's own numbers: one place against five, and places numbered as the original counts them
        Assert.Equal(1, BattleFormat.Horde.PlacesOf(BattleSide.Player));
        Assert.Equal(5, BattleFormat.Horde.PlacesOf(BattleSide.Enemy));
        Assert.Equal(9, Foe5.Number);
        Assert.True(Foe5.Number < Place.MaxNumber);
        Assert.Equal(Place.MaxNumber, core.Field.Places.Length);

        var log = core.Log;
        // Five come out, none from a ball, and the horde is announced once
        Assert.Equal(5, log.OfType<Entered>().Count(e => e.Place.Side == BattleSide.Enemy && !e.FromBall));
        Assert.Contains(Text.Of("battle.aWildHorde", ("pokemon", "Zubat")), Lines(log));
        // Every place is asked for its choice: the player's and the five
        Assert.Equal(new[] { Mine, Foe, Foe2, Foe3, Foe4, Foe5 }, Assert.IsType<ActionRequest>(core.Request).Places.OrderBy(p => p.Number));
    }

    [Fact]
    public void AHordeOfSeveralKindsNamesNone()
    {
        var core = Horde(Mon("Golem", 40, "Rock Slide"), new[] { Mon("Zubat", 20), Mon("Geodude", 20), Mon("Zubat", 20) });
        Assert.Equal(3, core.EnemySlots.Count);
        Assert.Contains(Text.Of("battle.aMixedHorde"), Lines(core.Log));
    }

    [Fact]
    public void WhatCantBeAHordeFallsBack()
    {
        // Platinum's rules have no hordes: a single against the first, with the line a single says
        var platinum = Horde(Mon("Golem", 40, "Rock Slide"), Five("Zubat", 20), rules: Ruleset.Platinum);
        Assert.Equal(BattleFormat.Single, platinum.Format);
        Assert.Single(platinum.EnemySlots);
        Assert.Contains(Text.Of("battle.aWildAppeared", ("pokemon", "Zubat")), Lines(platinum.Log));
        // Nor is one wild Pokémon a horde
        var alone = Horde(Mon("Golem", 40, "Rock Slide"), new[] { Mon("Zubat", 20) });
        Assert.Equal(BattleFormat.Single, alone.Format);
        // And neither the Great Marsh nor a trainer's battle
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Horde, 3, 0, false, 0, 0, 5, BattleKind.Safari, Ruleset.Modern, out _));
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Horde, 3, 0, false, 1, 5, 0, BattleKind.Normal, Ruleset.Modern, out _));
        // A partner along makes it the double it always was
        Assert.Equal(BattleFormat.Double, BattleFormats.FallBack(BattleFormat.Horde, 3, 2, true, 0, 0, 5, BattleKind.Normal, Ruleset.Modern, out bool stays));
        Assert.True(stays);
        // And what a single and a double asked for are what they were
        Assert.Equal(BattleFormat.Double, BattleFormats.FallBack(BattleFormat.Double, 2, 0, false, 0, 0, 2, BattleKind.Normal, Ruleset.Platinum, out _));
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Double, 1, 0, false, 0, 0, 2, BattleKind.Normal, Ruleset.Platinum, out _));
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Single, 3, 0, false, 0, 0, 2, BattleKind.Normal, Ruleset.Modern, out _));
    }

    [Fact]
    public void ASpreadMoveHitsAllFiveForThreeQuartersAsInADouble()
    {
        // Rock Slide hits every foe it faces; with more than one hit each takes the spread hit's 3072/4096 of the
        // modern formula (Formulas.Modify after the + 2), as in a double battle. Nothing is left to chance (Calm)
        var horde = Horde(Mon("Golem", 40, "Rock Slide"), Five("Snorlax", 50));
        HordeTurn(horde);
        var hurt = horde.EnemySlots.Select(b => b.Pokemon!.MaxHP - b.Pokemon.CurrentHP).ToList();
        Assert.All(hurt, d => Assert.True(d > 0, "every one of the five is hit"));
        Assert.Single(hurt.Distinct());

        var party = new Party();
        party.Add(Mon("Golem", 40, "Rock Slide"));
        party.Add(Mon("Golem", 40));
        var twoOfThem = Five("Snorlax", 50).Take(2).ToList();
        var dbl = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = twoOfThem, Format = BattleFormat.Double, Random = Calm(), Rules = Ruleset.Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        dbl.Start();
        Assert.Equal(BattleFormat.Double, dbl.Format);
        DoubleTurn(dbl, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(hurt[0], twoOfThem[0].MaxHP - twoOfThem[0].CurrentHP);

        var single = Wild(Mon("Golem", 40, "Rock Slide"), Mon("Snorlax", 50), rules: Ruleset.Modern);
        Turn(single);
        var alone = single.EnemySlots[0].Pokemon!;
        Assert.True(alone.MaxHP - alone.CurrentHP > hurt[0], "one target alone takes the whole hit");
    }

    [Fact]
    public void ABallIsRefusedUntilOneIsLeftAndEachFaintGivesItsExp()
    {
        var golem = Mon("Golem", 60, "Rock Slide");
        var foes = new[] { Mon("Magikarp", 5, "Splash"), Mon("Magikarp", 5, "Splash"), Mon("Magikarp", 5, "Splash"), Mon("Magikarp", 5, "Splash"), Mon("Snorlax", 70) };
        var core = Horde(golem, foes);
        var ball = BattleChoice.UseItem(Mine, "Poké Ball");
        Assert.Equal(Text.Of("battle.thereAreTooManyPokemon"), core.WhyNot(ball));

        core.TakeLog();
        HordeTurn(core);
        Assert.Equal(4, core.EnemySlots.Count(b => !b.IsActive));
        Assert.True(core.EnemySlots[4].IsActive);
        Assert.Null(core.Result == BattleResult.None ? core.WhyNot(ball) : "the battle ended");

        // Each Magikarp's EXP is given as it faints, before the next one's fall is told (Task_GiveExperience per faint)
        int fallen = 0, given = 0;
        foreach (var e in core.Log)
        {
            if (e is Said said && said.Shows.OfType<Fainted>().Any(f => f.Place.Side == BattleSide.Enemy))
            {
                Assert.Equal(fallen, given);
                fallen++;
            }
            if (e is ExpGained exp && exp.Pokemon == golem) given++;
        }
        Assert.Equal(4, fallen);
        Assert.Equal(4, given);
    }

    [Fact]
    public void TwoLeftOfAHordeSayTheyAreTwo()
    {
        var core = Horde(Mon("Golem", 40, "Rock Slide"), new[] { Mon("Zubat", 20), Mon("Zubat", 20) });
        Assert.Equal(BattleFormat.Horde, core.Format);
        Assert.Equal(2, core.EnemySlots.Count);
        Assert.Equal(Text.Of("battle.thereAreTwoPokemon"), core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
    }

    [Fact]
    public void ARunIsMeasuredAgainstTheFastestOfTheHorde()
    {
        // Bidoof outruns the Slowpoke in the first place, so a single's rule would let it go; the Jolteon in the
        // fifth is faster, and the roll that would decide (Formulas.Escapes, 255: none) fails it
        static List<string> RunFrom(params Pokemon[] foes)
        {
            var core = Horde(Mon("Bidoof", 30, "Tackle"), foes, Calm().Force(RollKind.Escape, 255));
            Assert.True(core.PlayerSlots[0].Pokemon!.Speed > foes[0].Speed);
            var asked = Assert.IsType<ActionRequest>(core.Request).Places;
            core.Submit(asked.Select(p => p.Side == BattleSide.Player ? BattleChoice.Run(p) : BattleChoice.Fight(p, 0)).ToList());
            return Lines(core.TakeLog());
        }

        var fastest = Mon("Jolteon", 30);
        Assert.True(fastest.Speed > Mon("Bidoof", 30).Speed);
        var caught = RunFrom(Mon("Slowpoke", 30), Mon("Slowpoke", 30), Mon("Slowpoke", 30), Mon("Slowpoke", 30), fastest);
        Assert.Contains(Text.Of("battle.cantEscape"), caught);

        var away = RunFrom(Mon("Slowpoke", 30), Mon("Slowpoke", 30), Mon("Slowpoke", 30), Mon("Slowpoke", 30), Mon("Slowpoke", 30));
        Assert.Contains(Text.Of("battle.gotAwaySafely"), away);
    }

    [Fact]
    public void WhatAWildDoubleBattleRefusesAHordeRefusesToo()
    {
        // Roar drives a wild Pokémon off and ends a single; with others on the field it fails, as in a double
        var core = Horde(Mon("Arcanine", 50, "Roar"), Five("Zubat", 10));
        HordeTurn(core, at: Foe3);
        Assert.Equal(BattleResult.None, core.Result);
        Assert.All(core.EnemySlots, b => Assert.True(b.IsActive));
        // And a miss is told of its target, as where there are several
        var missed = Horde(Mon("Golem", 40, "Tackle"), Five("Zubat", 10), Calm().Force(RollKind.Accuracy, 99));
        var said = HordeTurn(missed, at: Foe2, foes: 0);
        Assert.Contains(said, l => l == Text.Of("battle.avoidedTheAttack", ("target", missed.At(Foe2).Name)));
    }

    // ------------------------------------------------------------------ a record, and the fuzz

    private static (Party Mine, List<Pokemon> Horde) Teams()
    {
        var party = new Party();
        party.Add(Mon("Grotle", 26, "Razor Leaf", "Bite"));
        party.Add(Mon("Luxio", 24, "Spark", "Bite"));
        return (party, new List<Pokemon> { Mon("Zubat", 18, "Bite", "Supersonic"), Mon("Zubat", 19, "Bite"), Mon("Geodude", 20, "Rock Throw"), Mon("Zubat", 18, "Bite"), Mon("Machop", 20, "Low Kick") });
    }

    private static BattleCore Fresh(BattleFormat format)
    {
        var (party, horde) = Teams();
        return new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = horde, Format = format, Random = new BattleRandom(20261025), Rules = Ruleset.Modern,
            PlayerController = null, EnemyController = TrainerAi.Instance, PlayerName = "Lucas"
        });
    }

    [Fact]
    public void ARecordedHordeReplaysTheSameAndOnlyAsAHorde()
    {
        var core = Fresh(BattleFormat.Horde);
        core.Start();
        for (int step = 0, turn = 0; step < 200 && core.Request != null; step++)
        {
            switch (core.Request)
            {
                case ActionRequest:
                    // Each of the player's moves in turn, at the foe its turn's number points to
                    var at = core.EnemySlots.Where(b => b.IsActive).ToList();
                    core.Submit(BattleChoice.Fight(Mine, turn % 2, at[turn % at.Count].Place));
                    turn++;
                    break;
                case ReplacementRequest asked:
                    core.Submit(BattleChoice.Switch(asked.Place, Enumerable.Range(0, core.PlayerParty.Count).First(i => core.CanSendIn(i))));
                    break;
            }
        }
        Assert.Null(core.Request);
        var record = core.Record!;
        Assert.Equal(BattleFormat.Horde, record.Format);

        var again = Fresh(BattleFormat.Horde);
        again.Replay(record);
        Assert.Equal(Lines(core.Log), Lines(again.Log));
        Assert.Equal(core.Result, again.Result);

        // The same seed and answers in a single are another battle: refused
        Assert.Throws<ArgumentException>(() => Fresh(BattleFormat.Single).Replay(record));
        // And a record that names no format (one written before, or a Vs. Recorder file's) isn't held to one
        var unsaid = Fresh(BattleFormat.Horde);
        unsaid.Replay(record with { Format = null });
        Assert.Equal(Lines(core.Log), Lines(unsaid.Log));
    }

    [Fact]
    public void AHundredHordesAlwaysEndAndLeaveNothingOutOfRange()
    {
        // The fuzz of BattleCoreTests with hordes: three of the player's against five wild Pokémon of a species (every
        // fourth horde of two species), at any level, both sides chosen for by the trainer AI, under the modern rules
        var species = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        for (int battle = 0; battle < 100; battle++)
        {
            var dice = new Random(battle * 104729 + 11);
            var mine = new Party();
            for (int i = 0; i < 3; i++) mine.Add(new Pokemon(species[dice.Next(species.Count)], dice.Next(10, 80), dice));
            var kinds = new[] { species[dice.Next(species.Count)], species[dice.Next(species.Count)] };
            var horde = Enumerable.Range(0, 5).Select(i => new Pokemon(kinds[battle % 4 == 3 ? i % 2 : 0], dice.Next(5, 60), dice)).ToList();
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = mine, WildPokemon = horde, Random = new BattleRandom((uint)battle), Rules = Ruleset.Modern,
                Format = BattleFormat.Horde, PlayerController = TrainerAi.Instance
            });
            Assert.Equal(BattleFormat.Horde, core.Format);
            core.Start();

            Assert.Null(core.Request);
            Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.PlayerDefeat, $"horde {battle} ended as {core.Result}");
            Assert.True(core.Turn < 1000, $"horde {battle} took {core.Turn} turns");
            Assert.IsType<Ended>(core.TakeLog().Last());
            foreach (var p in mine.Members.Concat(horde))
            {
                Assert.InRange(p.CurrentHP, 0, p.MaxHP);
                Assert.All(p.Moves, m => Assert.InRange(m.CurrentPP, 0, m.MaxPP));
                Assert.All(p.StatStages.Values, stage => Assert.InRange(stage, -6, 6));
            }
            bool mineDown = mine.Members.All(p => p.IsFainted), theirsDown = horde.All(p => p.IsFainted);
            Assert.True(core.Result == BattleResult.PlayerDefeat ? mineDown : theirsDown, $"horde {battle}: {core.Result}");
        }
    }

    // ------------------------------------------------------------------ the world's tables

    [Fact]
    public void EveryPlacesTablesMeetOnePokemonAtATime()
    {
        // Plan 14, decision 1: Platinum's places keep Platinum's formats. No table of the world, of any region, meets a
        // horde: the grass's hordes (plan 14 · B4) are a table of their own, Data/hordes.json, met after a region's Hall
        // of Fame (HordeScreenTests)
        var maps = MapDatabase.Fresh();
        var hordes = new List<string>();
        void Check(string place, IEnumerable<WildEncounterEntry> table)
        {
            foreach (var e in table.Where(e => e.Count != 1)) hordes.Add($"{place}: {e.SpeciesName} ×{e.Count}");
        }
        foreach (string name in MapDatabase.MapNames)
        {
            var map = maps(name);
            Check(map.Name, map.WildEncounters);
            foreach (var area in map.Areas)
            {
                Check(area.Key, area.WildEncounters);
                Check(area.Key, area.WaterEncounters);
                foreach (var rod in area.RodEncounters) Check(area.Key, rod);
                Check(area.Key, area.RockSmashEncounters);
                Check(area.Key, area.DarkGrassEncounters);
                Check(area.Key, area.ShakingGrassEncounters);
                Check(area.Key, area.RipplingWaterEncounters);
                Check(area.Key, area.RipplingRodEncounters);
            }
        }
        Assert.True(hordes.Count == 0, "Hordes in a table:\n" + string.Join("\n", hordes));

        // And a table's slot of one is written as it always was: no count in the file
        var file = new MapFile { WildEncounters = { new WildEncounterEntry { SpeciesName = "Zubat" }, new WildEncounterEntry { SpeciesName = "Golbat", Count = 5 } } };
        string json = System.Text.Json.JsonSerializer.Serialize(file.WildEncounters, GameDataFiles.Json);
        Assert.Equal(1, json.Split("\"count\"").Length - 1);
        var back = System.Text.Json.JsonSerializer.Deserialize<List<WildEncounterEntry>>(json, GameDataFiles.Json)!;
        Assert.Equal(new[] { 1, 5 }, back.Select(e => e.Count));
    }

    [Fact]
    public void AHordeSlotMeetsItsOthersAtLevelsOfTheirOwn()
    {
        var table = new List<WildEncounterEntry> { new() { SpeciesName = "Zubat", MinLevel = 10, MaxLevel = 20, Weight = 100, Count = 5 } };
        var met = WildEncounterRules.Meet(table, water: false, lead: null, new Random(7), keptAway: false)!;
        Assert.Equal(5, met.Count);
        Assert.Equal(4, met.OthersLevels!.Length);
        Assert.All(met.OthersLevels, l => Assert.InRange(l, 10, 20));
        // One alone draws nothing more than it did
        var single = new List<WildEncounterEntry> { new() { SpeciesName = "Zubat", MinLevel = 10, MaxLevel = 20, Weight = 100 } };
        Assert.Null(WildEncounterRules.Meet(single, water: false, lead: null, new Random(7), keptAway: false)!.OthersLevels);
    }
}
