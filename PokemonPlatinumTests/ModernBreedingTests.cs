using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The modern Day Care (plan 36 · TR6; <c>docs/mechanics/rulings.md</c>, "Breeding and what a personality decides"): the
/// mother's ability handed down (her hidden one 60 times in 100, a regular one's place 80 and the other 20, beside Ditto
/// the other parent's), her ball, egg moves from both parents and no machine's move, the Shiny Charm's two extra rolls for
/// an Egg, and Unova's Day Care. An Egg made under Platinum's rules is what it was.
/// </summary>
public class ModernBreedingTests
{
    private static PokemonSpecies S(string name) => PokemonDatabase.Get(name)!;

    private static Pokemon Mon(string species, Gender gender, int level = 20) =>
        new(S(species), level, new Random(1), gender, Nature.Hardy);

    /// <summary>A generator that answers with the numbers given, in order, and then with nought.</summary>
    private sealed class Script(params int[] values) : Random
    {
        private int next;
        public override int Next(int maxValue) => (next < values.Length ? values[next++] : 0) % maxValue;
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
    }

    private static readonly Ruleset Modern = Ruleset.Modern, Platinum = Ruleset.Platinum;

    private const uint Offspring = 0x00070005;

    /// <summary>Shinx: Rivalry or Intimidate, Guts hidden; a Shinx egg from a Shinx and a Luxray.</summary>
    private static Pokemon Hidden(Pokemon p)
    {
        Assert.Equal("Guts", p.HiddenAbilityName);
        p.AbilityName = p.HiddenAbilityName;
        return p;
    }

    private static Dictionary<string, int> Abilities(Pokemon mother, Pokemon father, int seeds, Ruleset rules)
    {
        var counts = new Dictionary<string, int>();
        for (int seed = 0; seed < seeds; seed++)
        {
            var egg = Breeding.MakeEgg(father, mother, Offspring, 0, new Random(seed), rules);
            counts[egg.AbilityName!] = counts.GetValueOrDefault(egg.AbilityName!) + 1;
        }
        return counts;
    }

    // ---------------------------------------------------------------- abilities

    [Fact]
    public void AMothersHiddenAbilityIsHandedDownSixtyTimesInAHundredAndTheRegularOnesShareTheRest()
    {
        var mother = Hidden(Mon("Luxray", Gender.Female));
        var father = Mon("Shinx", Gender.Male);
        var counts = Abilities(mother, father, 4000, Modern);
        Assert.InRange(counts["Guts"], 4000 * 0.60 - 130, 4000 * 0.60 + 130);
        Assert.InRange(counts["Rivalry"], 4000 * 0.20 - 110, 4000 * 0.20 + 110);
        Assert.InRange(counts["Intimidate"], 4000 * 0.20 - 110, 4000 * 0.20 + 110);
    }

    [Fact]
    public void ARegularAbilityKeepsItsPlaceEightyTimesInAHundredAndNeverBecomesTheHiddenOne()
    {
        var mother = Mon("Luxray", Gender.Female);
        mother.AbilityName = "Intimidate"; // the second place
        var counts = Abilities(mother, Mon("Shinx", Gender.Male), 4000, Modern);
        Assert.False(counts.ContainsKey("Guts"));
        Assert.InRange(counts["Intimidate"], 4000 * 0.80 - 130, 4000 * 0.80 + 130);
        Assert.InRange(counts["Rivalry"], 4000 * 0.20 - 110, 4000 * 0.20 + 110);

        mother.AbilityName = "Rivalry";
        counts = Abilities(mother, Mon("Shinx", Gender.Male), 4000, Modern);
        Assert.InRange(counts["Rivalry"], 4000 * 0.80 - 130, 4000 * 0.80 + 130);
    }

    [Fact]
    public void TheFathersAbilityPassesNothingWithoutDitto()
    {
        var mother = Mon("Luxray", Gender.Female);
        mother.AbilityName = "Rivalry";
        var father = Hidden(Mon("Shinx", Gender.Male));
        Assert.False(Abilities(mother, father, 500, Modern).ContainsKey("Guts"));
    }

    [Fact]
    public void BesideDittoTheOtherParentsAbilityPassesWhateverItsGender()
    {
        var ditto = Mon("Ditto", Gender.Genderless);
        foreach (var gender in new[] { Gender.Male, Gender.Female })
        {
            var other = Hidden(Mon("Shinx", gender));
            var counts = Abilities(ditto, other, 2000, Modern);
            Assert.InRange(counts["Guts"], 2000 * 0.60 - 100, 2000 * 0.60 + 100);
            // Ditto's own Limber and Imposter are never the Egg's
            Assert.Equal(new[] { "Guts", "Intimidate", "Rivalry" }, counts.Keys.OrderBy(k => k).ToArray());
        }
    }

    [Fact]
    public void ASpeciesWithNoHiddenAbilityAndOneWithASingleRegularAbilityKeepWhatTheyHave()
    {
        // Bidoof's Simple, Unaware and Moody hidden: an Egg whose mother has Simple has Simple or Unaware; Hidden Moody comes
        var bidoof = Mon("Bidoof", Gender.Female);
        var egg = Breeding.MakeEgg(Mon("Bidoof", Gender.Male), bidoof, Offspring, 0, new Random(1), Modern);
        Assert.Contains(egg.AbilityName, new[] { "Simple", "Unaware", "Moody" });
        // Magnemite-like genderless pairs with Ditto still give an ability of the Egg's own species
        var magnemite = Mon("Magnemite", Gender.Genderless);
        var other = Breeding.MakeEgg(Mon("Ditto", Gender.Genderless), magnemite, Offspring, 0, new Random(2), Modern);
        Assert.Contains(other.AbilityName, other.Abilities.Append(other.HiddenAbilityName!));
    }

    [Fact]
    public void AnEggMadeUnderPlatinumsRulesKeepsTheAbilityItsPersonalityGivesAndDrawsNothingMore()
    {
        var mother = Hidden(Mon("Luxray", Gender.Female));
        var father = Mon("Shinx", Gender.Male);
        mother.Ball = "Great Ball";
        var rng = new Random(5);
        var egg = Breeding.MakeEgg(father, mother, Offspring, 0, rng, Platinum, shinyCharm: true);
        // Bit 0 of the couple's personality says which regular ability, whatever the mother's hidden one
        Assert.Equal(Personality.AbilityOf(egg.Abilities, Offspring), egg.AbilityName);
        Assert.Equal((Breeding.EggBall, true), (egg.Ball, egg.IsEgg));

        // The draws: the Egg's own two for its IVs and the three picks and three coins of Platinum's inheritance, nothing else
        var expected = new Random(5);
        var plain = new Pokemon(S("Shinx"), 1, Offspring, expected);
        Breeding.InheritIvs(plain, father, mother, expected, Platinum);
        Assert.Equal(expected.Next(), rng.Next());
        Assert.Equal(Breeding.IvOrder.Select(s => Breeding.IvOf(plain, s)), Breeding.IvOrder.Select(s => Breeding.IvOf(egg, s)));
    }

    // ---------------------------------------------------------------- the ball

    [Fact]
    public void AnEggIsInItsMothersBallAndAMasterOrCherishBallIsAPokeBall()
    {
        var father = Mon("Shinx", Gender.Male);
        var mother = Mon("Luxray", Gender.Female);
        father.Ball = "Ultra Ball";
        mother.Ball = "Dusk Ball";
        Assert.Equal("Dusk Ball", Breeding.MakeEgg(father, mother, Offspring, 0, new Random(1), Modern).Ball);
        mother.Ball = "Master Ball";
        Assert.Equal("Poké Ball", Breeding.MakeEgg(father, mother, Offspring, 0, new Random(1), Modern).Ball);
        mother.Ball = "Cherish Ball";
        Assert.Equal("Poké Ball", Breeding.MakeEgg(father, mother, Offspring, 0, new Random(1), Modern).Ball);
        mother.Ball = null;
        Assert.Equal("Poké Ball", Breeding.MakeEgg(father, mother, Offspring, 0, new Random(1), Modern).Ball);
        // Under Platinum's rules, always a Poké Ball
        mother.Ball = "Dusk Ball";
        Assert.Equal("Poké Ball", Breeding.MakeEgg(father, mother, Offspring, 0, new Random(1), Platinum).Ball);
    }

    [Fact]
    public void TwoOfOneSpeciesGiveEitherOnesBallOnACoin()
    {
        var father = Mon("Shinx", Gender.Male);
        var mother = Mon("Shinx", Gender.Female);
        father.Ball = "Ultra Ball";
        mother.Ball = "Dusk Ball";
        var balls = Enumerable.Range(0, 200)
            .Select(seed => Breeding.MakeEgg(father, mother, Offspring, 0, new Random(seed), Modern).Ball).ToHashSet();
        Assert.Equal(new[] { "Dusk Ball", "Ultra Ball" }, balls.OrderBy(b => b).ToArray());
    }

    [Fact]
    public void BesideDittoTheOtherParentsBallIsTheEggs()
    {
        var ditto = Mon("Ditto", Gender.Genderless);
        ditto.Ball = "Luxury Ball";
        var shinx = Mon("Shinx", Gender.Male);
        shinx.Ball = "Heal Ball";
        for (int seed = 0; seed < 20; seed++)
            Assert.Equal("Heal Ball", Breeding.MakeEgg(ditto, shinx, Offspring, 0, new Random(seed), Modern).Ball);
    }

    [Fact]
    public void AHatchedPokemonStaysInItsEggsBall()
    {
        var mother = Mon("Luxray", Gender.Female);
        mother.Ball = "Net Ball";
        var egg = Breeding.MakeEgg(Mon("Shinx", Gender.Male), mother, Offspring, 0, new Random(1), Modern);
        Breeding.Hatch(egg, "Route 201", new DateTime(2026, 3, 10));
        Assert.Equal(("Net Ball", false), (egg.Ball, egg.IsEgg));
        var plain = Breeding.GiftEgg(S("Togepi"), 0, new Random(1), Platinum);
        Breeding.Hatch(plain, null, new DateTime(2026, 3, 10));
        Assert.Equal("Poké Ball", plain.Ball);
    }

    // ---------------------------------------------------------------- moves

    private static Pokemon Knowing(string species, Gender gender, params string[] moves)
    {
        var p = Mon(species, gender, 30);
        p.Moves.Clear();
        foreach (string move in moves) p.TryLearn(move);
        return p;
    }

    [Fact]
    public void AnEggTakesEggMovesFromBothParentsAndNoMachinesMove()
    {
        // Shinx's egg moves include Ice Fang; Thunderbolt is TM24, which Shinx learns: the father's by Platinum's rules
        var father = Knowing("Luxio", Gender.Male, "Thunderbolt", "Leer");
        var mother = Knowing("Shinx", Gender.Female, "Ice Fang", "Tackle");
        Assert.Contains("Ice Fang", S("Shinx").EggMoves!);

        var modern = Breeding.MakeEgg(father, mother, Offspring, 0, new Random(1), Modern);
        Assert.Contains(modern.Moves, m => m.Name == "Ice Fang");
        Assert.DoesNotContain(modern.Moves, m => m.Name == "Thunderbolt");

        var platinum = Breeding.MakeEgg(father, mother, Offspring, 0, new Random(1), Platinum);
        Assert.Contains(platinum.Moves, m => m.Name == "Thunderbolt");
        Assert.DoesNotContain(platinum.Moves, m => m.Name == "Ice Fang"); // only the father's egg moves

        // The father's still pass
        var fathers = Breeding.MakeEgg(Knowing("Luxio", Gender.Male, "Ice Fang"), Knowing("Shinx", Gender.Female, "Tackle"),
            Offspring, 0, new Random(1), Modern);
        Assert.Contains(fathers.Moves, m => m.Name == "Ice Fang");
    }

    [Fact]
    public void WhatBothParentsKnowByLevelStillPassesDown()
    {
        var father = Knowing("Luxio", Gender.Male, "Bite");
        var mother = Knowing("Shinx", Gender.Female, "Bite");
        var egg = Breeding.MakeEgg(father, mother, Offspring, 0, new Random(1), Modern);
        Assert.Contains(egg.Moves, m => m.Name == "Bite");
    }

    // ---------------------------------------------------------------- the Shiny Charm

    /// <summary>The draws a modern Egg takes before the charm's: its IVs' two and the six of the later inheritance.</summary>
    private static int[] Before() => new int[8];

    [Fact]
    public void TheShinyCharmGivesAPlainEggTwoMoreRollsAtBeingShiny()
    {
        // A trainer whose number makes personality 0x12345678 shiny and the couple's 0x00070005 not (the values' exclusive
        // or is under 16 for the one, over for the other, Personality.IsShiny)
        const uint candidate = 0x12345678;
        var father = Mon("Shinx", Gender.Male);
        var mother = Mon("Luxray", Gender.Female);
        Assert.False(Personality.IsShiny(candidate, Offspring, Modern.ShinyOdds));
        Assert.True(Personality.IsShiny(candidate, candidate, Modern.ShinyOdds));

        // The first roll is another plain one (0x00090003), the second is the shiny's
        var draws = Before().Concat(new[] { 3, 9, 0x5678, 0x1234 }).ToArray();
        var with = Breeding.MakeEgg(father, mother, Offspring, candidate, new Script(draws), Modern, shinyCharm: true);
        Assert.Equal((true, candidate, true), (with.IsShiny, with.Personality, with.IsEgg));
        Assert.Equal("Shinx", with.Species.Name);
        Assert.Equal(Personality.NatureOf(candidate), with.Nature);

        // Without the charm the same draws make the plain Egg, and a charm under Platinum's rules does nothing
        var without = Breeding.MakeEgg(father, mother, Offspring, candidate, new Script(draws), Modern);
        Assert.Equal((false, Offspring), (without.IsShiny, without.Personality));
        var platinum = Breeding.MakeEgg(father, mother, Offspring, candidate, new Script(draws), Platinum, shinyCharm: true);
        Assert.Equal((false, Offspring), (platinum.IsShiny, platinum.Personality));
    }

    [Fact]
    public void TheCharmsRollsAreOnlyTwoAndAShinyEggDrawsNone()
    {
        const uint candidate = 0x12345678;
        var father = Mon("Shinx", Gender.Male);
        var mother = Mon("Luxray", Gender.Female);
        // Three plain rolls: only two are made, so the third's shiny personality is never reached
        var draws = Before().Concat(new[] { 3, 9, 4, 9, 0x5678, 0x1234 }).ToArray();
        var egg = Breeding.MakeEgg(father, mother, Offspring, candidate, new Script(draws), Modern, shinyCharm: true);
        Assert.Equal((false, Offspring), (egg.IsShiny, egg.Personality));

        // An Egg already shiny for its trainer (the couple's personality) takes no extra roll
        var rng = new Script(Before());
        var shiny = Breeding.MakeEgg(father, mother, Offspring, Offspring, rng, Modern, shinyCharm: true);
        Assert.True(shiny.IsShiny);
        Assert.Equal(Offspring, shiny.Personality);
    }

    [Fact]
    public void TheCharmedEggKeepsAnEverstonesNature()
    {
        var father = Mon("Shinx", Gender.Male);
        var mother = Mon("Luxray", Gender.Female);
        mother.HeldItem = ItemDatabase.Get("Everstone");
        // The couple's 0x00070005 has the nature 7 (by 25); the first candidate, 0x12345678, has 21 and is drawn past, and
        // 0x12345683 has 7 and is shiny for the trainer who holds that number
        const uint couple = 0x00070005, rejected = 0x12345678, right = 0x12345683;
        Assert.Equal(Personality.NatureOf(couple), Personality.NatureOf(right));
        Assert.NotEqual(Personality.NatureOf(couple), Personality.NatureOf(rejected));
        var draws = Before().Concat(new[] { 0x5678, 0x1234, 0x5683, 0x1234 }).ToArray();
        var egg = Breeding.MakeEgg(father, mother, couple, right, new Script(draws), Modern, shinyCharm: true);
        Assert.Equal((true, right, Personality.NatureOf(couple)), (egg.IsShiny, egg.Personality, egg.Nature));
    }

    // ---------------------------------------------------------------- Unova's Day Care

    private static readonly Lazy<World> UnovaWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Unova"));
    private static readonly Lazy<Dictionary<string, Map>> UnovaMaps = new(() => UnovaWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Map> HandMade = new();
    private static Map MapNamed(string name) => UnovaMaps.Value.TryGetValue(name, out var map) ? map : HandMade.GetOrAdd(name, MapDatabase.Get);

    [Fact]
    public void RouteThreesDayCarePeopleRunTheCommonScriptsAsEveryFinishedRegionsDo()
    {
        var room = UnovaMaps.Value["Route3_DayCare"];
        Assert.Contains(room.Everyone, n => n.Script == "DayCareMan");
        Assert.Contains(room.Everyone, n => n.Script == "DayCareLady");
        Assert.NotNull(ScriptLibrary.Default.Find("DayCareMan", "unova_route_3_3"));
        Assert.NotNull(ScriptLibrary.Default.Find("DayCareLady", "unova_route_3_3"));
        var walked = WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Unova)!.Start!);
        Assert.Contains(walked.Keys, m => m.Name == "Route3_DayCare");
    }

    private static void Run(string name, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, "unova_route_3_3")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
    }

    [Fact]
    public void APokemonLeftWithRouteThreesLadyIsTakenInBredAndFoundAsAnEggByTheMan()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Bidoof", Gender.Male));
        host.Party.Add(Mon("Shinx", Gender.Female));
        host.Party.Add(Mon("Luxio", Gender.Male));
        host.Party.Add(Mon("Starly", Gender.Male));
        host.Party.Add(Mon("Staravia", Gender.Male));
        // The Lady: yes to raising one, the Shinx chosen, yes to another, and the Luxio (now second in the team) chosen
        host.Answers.Enqueue(0);
        host.PokemonChoice = 1;
        host.Answers.Enqueue(0);
        Run("DayCareLady", host);
        Assert.Equal(("Shinx", "Luxio", DayCareState.TwoPokemon), (host.DayCare[0]!.Species.Name, host.DayCare[1]!.Species.Name, host.DayCare.State));

        // A walk along the route, and the couple find an Egg (a draw of nought finds one; the personality the next two)
        for (int i = 0; i < 256; i++) host.DayCare.Step(host.Party, new Script(0, 5, 7, 0, 5, 7), new DateTime(2026, 3, 10), Modern);
        Assert.True(host.DayCare.HasEgg);

        host.Answers.Enqueue(0);
        Run("DayCareMan", host);
        var egg = host.Party.Members.Single(p => p.IsEgg);
        Assert.Equal(("Shinx", Breeding.DayCareCouple), (egg.Species.Name, egg.MetLocation));
        Assert.False(host.DayCare.HasEgg);
    }
}
