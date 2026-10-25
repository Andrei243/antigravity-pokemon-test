using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DataImporter;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D14: the species of our own that evolve past level 100, batch 1 (Kanto: docs/plans/D14-proposals-kanto.md,
/// approved by the user on 2026-10-25). The hand-written table (<c>tools/DataImporter/Overrides/species-past-hundred.json</c>)
/// held to the proposals' rules, the species the importer made of it, each evolution firing at its level under the
/// engine's own rules and not before, their moves by R31's rule, a battle, a save, and how the rest of the game counts
/// them (outside the 1,025: the National Pokédex lists them last and counts them toward nothing).
/// </summary>
public class SpeciesPastHundredTests
{
    private static readonly Lazy<List<SpeciesDesign>> designs = new(() => SpeciesPastHundred.Load(SpeciesPastHundred.PathIn(Repository.Root)));
    private static List<SpeciesDesign> Designs => designs.Value;

    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static int Total(StatSpread s) => s.HP + s.Attack + s.Defense + s.SpAttack + s.SpDefense + s.Speed;

    private static readonly StatType[] Stats = { StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };

    // ------------------------------------------------------------------ the table

    [Fact]
    public void TheTableHoldsTheApprovedDesignsByTheProposalsRules()
    {
        Assert.True(Designs.Count >= 46, $"only {Designs.Count} designs");
        var games = PokemonDatabase.GetAll().SelectMany(s => new[] { s.Name }.Concat((s.Forms ?? new()).Select(f => f.Name)))
            .Select(Plain).ToHashSet();
        Assert.Equal(Designs.Count, Designs.Select(d => Plain(d.Name)).Distinct().Count());
        foreach (var d in Designs)
        {
            var from = Species(d.From);
            Assert.True(from != null && !from.IsOurs, $"{d.Name} evolves from {d.From}, no species of the games");

            // A name of our own: one word of letters, at most 12, no species' or form's of the games
            Assert.Matches("^[A-Z][a-z]{2,11}$", d.Name);
            Assert.DoesNotContain(Plain(d.Name), games);

            // The forebear is the last of its line or alone: nothing it evolves into but ours
            Assert.Empty((from.Evolutions ?? new()).Where(e => e.TargetSpecies != d.Name && e.FromForm == null));

            Assert.InRange(d.Types.Count, 1, 2);
            Assert.Equal(d.Types.Count, d.Types.Distinct().Count());

            // Abilities that exist, as many slots as the forebear's, a hidden one where it had one
            Assert.All(d.Abilities.Append(d.HiddenAbility).OfType<string>(), a => Assert.True(AbilityDatabase.Get(a) != null, $"{d.Name}: {a}"));
            Assert.Equal(from.Abilities.Count, d.Abilities.Count);
            Assert.Equal(from.HiddenAbility != null, d.HiddenAbility != null);

            // The rise: +80, or what the ceiling of 600 leaves a species that isn't legendary; no stat falls
            var was = SpeciesPastHundred.StatsOf(from);
            bool legend = from.Legendary || from.Mythical;
            int rise = Total(d.Stats) - Total(was);
            Assert.All(Stats, stat => Assert.True(d.Stats.Of(stat) >= was.Of(stat), $"{d.Name}'s {stat} falls"));
            if (legend) Assert.Equal(80, rise);
            else
            {
                Assert.True(Total(d.Stats) <= 600, $"{d.Name} totals {Total(d.Stats)}");
                Assert.True(rise == 80 || (rise < 80 && Total(d.Stats) == 600), $"{d.Name} rises {rise}");
            }

            // Evolves by level, 101 to 160 (130 for one that isn't legendary), with what the data has
            var e = d.Evolution;
            Assert.InRange(e.Level, 101, legend ? 160 : 130);
            Assert.Equal(EvolutionTrigger.LevelUp == Evolution.TriggerOf(e.Method) || e.Method == EvolutionMethod.UseItem, true);
            if (e.Item != null) Assert.True(ItemDatabase.Get(e.Item) is { } item && item.Pocket != ItemPocket.KeyItems, $"{d.Name}: {e.Item}");
            if (e.Move != null) Assert.Contains(from.Learnset.Concat(from.Modern?.Learnset ?? new()), l => l.MoveName == e.Move);
            Assert.True(d.Height > 0 && d.Weight > 0 && d.Category.Length > 0, d.Name);
        }
    }

    private static string Plain(string name) => new(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    [Fact]
    public void EachDesignIsASpeciesOfTheDataNumberedAfterItsForebear()
    {
        var ours = PokemonDatabase.Ours();
        Assert.Equal(Designs.Count, ours.Count);
        Assert.Equal(ours.OrderBy(s => s.DexNumber), ours);
        foreach (var d in Designs)
        {
            var s = Species(d.Name);
            var from = Species(d.From);
            Assert.True(s.IsOurs, d.Name);
            Assert.Equal(PokemonDatabase.OursFrom + from.DexNumber, s.DexNumber);
            Assert.Same(s, PokemonDatabase.GetByDex(s.DexNumber));
            Assert.Equal(0, s.Generation);
            Assert.Empty(s.DexNumbers);
            Assert.Null(s.SinnohNumber);
            Assert.Null(s.Forms);
            Assert.Equal((d.Types[0], d.Types.Count > 1 ? d.Types[1] : (PokemonType?)null), (s.PrimaryType, s.SecondaryType));
            Assert.Equal((d.Stats.HP, d.Stats.Attack, d.Stats.Defense, d.Stats.SpAttack, d.Stats.SpDefense, d.Stats.Speed),
                (s.BaseHP, s.BaseAttack, s.BaseDefense, s.BaseSpAttack, s.BaseSpDefense, s.BaseSpeed));
            Assert.Equal(d.Abilities, s.Abilities);
            Assert.Equal(d.HiddenAbility, s.HiddenAbility);
            Assert.Equal((d.Category, d.Height, d.Weight), (s.Category, s.Height, s.Weight));

            // What it has of its forebear's, by the proposal's rules
            Assert.Equal((from.GrowthRate, from.GenderRatio, from.HatchCycles, from.Color, from.Shape, from.Legendary, from.Mythical),
                (s.GrowthRate, s.GenderRatio, s.HatchCycles, s.Color, s.Shape, s.Legendary, s.Mythical));
            Assert.Equal(from.EggGroups, s.EggGroups);
            Assert.Equal(from.Offspring ?? from.Name, s.Offspring);
            Assert.Equal(from.Legendary || from.Mythical ? 3 : 45, s.CatchRate);
            Assert.Equal((int)Math.Round(from.BaseExpYield * 1.2, MidpointRounding.AwayFromZero), s.BaseExpYield);
            Assert.Equal(Total(from.EvYield!) + 1, Total(s.EvYield!));
            Assert.Equal(from.TmMoves, s.TmMoves);
            Assert.Equal(from.TutorMoves, s.TutorMoves);

            // Its evolution is the last of its forebear's, as the table writes it
            var row = from.Evolutions!.Last();
            Assert.Equal((d.Name, d.Evolution.Method, d.Evolution.Level, d.Evolution.Item, d.Evolution.Move, d.Evolution.Location, d.Evolution.NeedsFriendship),
                (row.TargetSpecies, row.Method, row.Level, row.Item, row.Move, row.Location, row.NeedsFriendship));
            Assert.Null(s.Evolutions);

            // The Pokédex: two entries of our own words and a habitat, and the composed sentence names the forebear
            Assert.Equal(2, DexEntries.EntriesOf(s.Name)!.Count);
            Assert.Contains(from.DisplayName, s.DexEntry);
            Assert.Contains(s.Name, from.DexEntry);
        }
    }

    // ------------------------------------------------------------------ evolving

    /// <summary>The forebear at a level, meeting everything its evolution asks (or, with <paramref name="meets"/> false, nothing but the level).</summary>
    private static (Pokemon Pokemon, EvolutionContext Context, EvolutionTrigger Trigger) Ready(SpeciesDesign d, int level, bool meets = true)
    {
        var e = d.Evolution;
        var p = Mon(d.From, level, "Tackle");
        p.Friendship = 0;
        var context = new EvolutionContext { IsNight = e.Method is EvolutionMethod.LevelDay or EvolutionMethod.FriendshipDay };
        var trigger = EvolutionTrigger.LevelUp;
        if (!meets) return (p, context, trigger);

        if (e.NeedsFriendship || e.Method is EvolutionMethod.Friendship or EvolutionMethod.FriendshipDay or EvolutionMethod.FriendshipNight)
            p.Friendship = 255;
        switch (e.Method)
        {
            case EvolutionMethod.LevelNight or EvolutionMethod.FriendshipNight: context.IsNight = true; break;
            case EvolutionMethod.LevelDay or EvolutionMethod.FriendshipDay: context.IsNight = false; break;
            case EvolutionMethod.LevelInRain: context.IsRaining = true; break;
            case EvolutionMethod.LevelAtLocation: context.Sites = new[] { e.Location! }; break;
            case EvolutionMethod.LevelHoldingItem: p.HeldItem = ItemDatabase.Get(e.Item!); break;
            case EvolutionMethod.LevelKnowsMove: p.Moves.Add(new Move(MoveDatabase.Get(e.Move!))); break;
            case EvolutionMethod.UseItem: context.Item = ItemDatabase.Get(e.Item!); trigger = EvolutionTrigger.UseItem; break;
        }
        return (p, context, trigger);
    }

    [Fact]
    public void EachEvolutionFiresAtItsLevelWithItsConditionAndNotBefore()
    {
        foreach (var d in Designs)
        {
            int level = d.Evolution.Level;
            var (ready, context, trigger) = Ready(d, level);
            var found = Evolution.Find(ready, trigger, context);
            Assert.True(found?.TargetSpecies == d.Name, $"{d.From} at {level} becomes {found?.TargetSpecies ?? "nothing"}, not {d.Name}");

            var (early, earlyContext, earlyTrigger) = Ready(d, level - 1);
            Assert.True(Evolution.Find(early, earlyTrigger, earlyContext)?.TargetSpecies != d.Name, $"{d.From} becomes {d.Name} at {level - 1}");

            // A condition besides the level is asked for: the level alone isn't enough
            if (d.Evolution.Method != EvolutionMethod.Level)
            {
                var (bare, bareContext, bareTrigger) = Ready(d, 160, meets: false);
                Assert.True(Evolution.Find(bare, bareTrigger, bareContext)?.TargetSpecies != d.Name, $"{d.From} becomes {d.Name} with nothing but its level");
            }

            // Carried out: the species changes, a held item is used up, the moves are kept
            var outcome = Evolution.Evolve(ready, found!, context);
            Assert.Equal((d.From, d.Name), (outcome.From.Name, outcome.Into.Name));
            Assert.Equal(d.Name, ready.Species.Name);
            if (d.Evolution.Method == EvolutionMethod.LevelHoldingItem) Assert.Null(ready.HeldItem);
            Assert.True(ready.Knows("Tackle"));
        }
    }

    [Fact]
    public void ARegionalFormOfAForebearDoesNotBecomeOneOfOurs()
    {
        // Question 10's default: the engine refuses a regional form every evolution that isn't its form's own
        var galar = new Pokemon(Species("Farfetch'd"), 120, Gender.Male, Nature.Hardy, false) { HeldItem = ItemDatabase.Get("Leek") };
        galar.ChangeForm("Farfetch'd-Galar");
        Assert.NotEqual("Porrigard", Evolution.Find(galar, EvolutionTrigger.LevelUp, new EvolutionContext())?.TargetSpecies);
        var kanto = new Pokemon(Species("Farfetch'd"), 120, Gender.Male, Nature.Hardy, false) { HeldItem = ItemDatabase.Get("Leek") };
        Assert.Equal("Porrigard", Evolution.Find(kanto, EvolutionTrigger.LevelUp, new EvolutionContext())?.TargetSpecies);
    }

    [Fact]
    public void TheirHeldItemsAndStonesAreKnownAsEvolutionItems()
    {
        foreach (var e in Designs.Select(d => d.Evolution).Where(e => e.Item != null))
        {
            var item = ItemDatabase.Get(e.Item!)!;
            Assert.True(e.Method == EvolutionMethod.UseItem ? Evolution.IsUsedToEvolve(item) : Evolution.IsHeldForEvolution(item), e.Item);
        }
    }

    // ------------------------------------------------------------------ moves

    [Fact]
    public void TheyLearnTheirForebearsMovesToAHundredAndR31sPastIt()
    {
        var all = MoveDatabase.GetAll().ToList();
        foreach (var d in Designs)
        {
            var s = Species(d.Name);
            var from = Species(d.From);
            foreach (var (mine, theirs) in new[] { (s.Learnset, from.Learnset), (s.Modern?.Learnset, from.Modern?.Learnset) })
            {
                Assert.Equal(theirs == null, mine == null);
                if (mine == null || theirs == null) continue;
                Assert.Equal(theirs.Where(l => l.Level is >= 1 and <= 100).Select(l => (l.Level, l.MoveName)), mine.Where(l => l.Level <= 100).Select(l => (l.Level, l.MoveName)));
                Assert.DoesNotContain(mine, l => l.Level == 0);

                // Past 100, R31's rule by its own types and stats
                var expected = MovesPastHundred.For(d.Types, d.Stats, all);
                var past = mine.Where(l => l.Level > 100).ToList();
                Assert.Equal(expected.Select((m, i) => (MovesPastHundred.LevelOf(i, expected.Count), m.Name)), past.Select(l => (l.Level, l.MoveName)));
                Assert.InRange(past.Count, 4, 8);
            }
        }
    }

    // ------------------------------------------------------------------ in the game

    [Fact]
    public void ARaticateLevelledPastItsLevelInABattleBecomesRatchiselAndFightsAsOne()
    {
        var raticate = Mon("Raticate", 104, "Tackle");
        raticate.CurrentExp = Pokemon.GetExpForLevel(105, raticate.Species.GrowthRate) - 1;
        var battle = Battle(raticate, Mon("Rattata", 50), Steady(), Ruleset.Modern);
        for (int turn = 0; turn < 10 && !battle.IsBattleOver; turn++) Turn(battle);
        Assert.True(battle.IsBattleOver);
        Assert.Equal(105, raticate.Level);
        Assert.Contains(raticate, battle.LeveledUp);

        // What the game asks once the battle is over (GameEngine.EndBattle)
        var into = Evolution.Find(raticate, EvolutionTrigger.LevelUp, new EvolutionContext());
        Assert.Equal("Ratchisel", into?.TargetSpecies);
        Evolution.Evolve(raticate, into!, new EvolutionContext());
        Assert.True(raticate.HasType(PokemonType.Steel));
        Assert.Equal(Species("Ratchisel").Abilities[0], raticate.Ability?.Name);

        // And in its next battle it is a Ratchisel: a Steel move with its own type's bonus, Steel's resistances
        var ratchisel = Mon("Ratchisel", 105, "Iron Head");
        var next = Battle(ratchisel, Mon("Rattata", 50), Steady(), Ruleset.Modern);
        Turn(next);
        Assert.True(next.IsBattleOver);
        var normal = Mon("Raticate", 105, "Iron Head");
        var foe = Mon("Snorlax", 105);
        Assert.True(Damage(ratchisel, foe, "Iron Head", rules: Ruleset.Modern) > Damage(normal, foe, "Iron Head", rules: Ruleset.Modern) * 1.4);
    }

    [Fact]
    public void OneOfOursIsSavedAndReadBackAndFoundByItsNumber()
    {
        foreach (var name in new[] { "Ratchisel", "Glacirune", "Mimestro" })
        {
            var p = new Pokemon(Species(name), 130, new Random(3));
            var saved = SavedPokemonData.FromPokemon(p);
            Assert.Equal(Species(name).DexNumber, saved.DexNumber);
            var read = JsonSerializer.Deserialize<SavedPokemonData>(JsonSerializer.Serialize(saved, GameDataFiles.Json), GameDataFiles.Json)!.ToPokemon();
            Assert.Equal((name, 130, p.Personality, p.Nature), (read.Species.Name, read.Level, read.Personality, read.Nature));
            Assert.Equal(p.Moves.Select(m => m.Name), read.Moves.Select(m => m.Name));

            // A save whose name the data no longer has is found by the number, as any species'
            saved.SpeciesName = "Missingno";
            Assert.Equal(name, saved.ToPokemon().Species.Name);
        }
    }

    [Fact]
    public void TheNationalPokedexListsThemLastAndCountsThemTowardNothing()
    {
        var national = Pokedex.Entries(Pokedex.National);
        Assert.Equal(PokemonDatabase.Ours(), national.Skip(1025).Select(e => e.Species));
        Assert.All(national.Skip(1025), e => Assert.Equal(e.Species.DexNumber, e.Number));
        Assert.All(new[] { "kanto", Pokedex.Sinnoh, "original-johto" }, dex => Assert.DoesNotContain(Pokedex.Entries(dex), e => e.Species.IsOurs));

        var (needed, _) = Pokedex.Goal(Pokedex.National);
        Assert.DoesNotContain(needed, s => s.IsOurs);

        var dex = new Pokedex();
        dex.RegisterCaught(new Pokemon(Species("Ferrisle"), 125, new Random(1)));
        dex.RegisterCaught(new Pokemon(Species("Lapras"), 40, new Random(1)));
        Assert.True(dex.IsCaught(Species("Ferrisle").DexNumber));
        Assert.Equal((1, 1), (dex.SeenCount, dex.CaughtCount));
        Assert.Equal((1, 1), (dex.SeenIn(Pokedex.National), dex.CaughtIn(Pokedex.National)));
    }

    [Fact]
    public void WhatHoldsToTheGamesSpeciesLeavesThemOut()
    {
        // GetAll is the games' 1,025: the battle corpus, the masters, the Pokéathlon's and Pal Park's draws go by it
        Assert.Equal(Enumerable.Range(1, 1025), PokemonDatabase.GetAll().Select(s => s.DexNumber));
        Assert.Equal(PokemonDatabase.GetAll().Count() + PokemonDatabase.Ours().Count, PokemonDatabase.Everyone().Count());
        Assert.All(PokemonDatabase.Ours(), s => Assert.Null(TrainerDatabase.Get(TrainerDatabase.MasterId(s.Name))));

        // The Battle Frontier: a legendary's evolution is banned, the rest may enter
        Assert.False(FrontierRules.MayEnter(Mon("Glacirune", 150)));
        Assert.False(FrontierRules.MayEnter(Mon("Pyrevant", 150)));
        Assert.True(FrontierRules.MayEnter(Mon("Ratchisel", 105)));

        // The way out: no other game has them, and a .pk4 holds nothing past 493
        Assert.Null(PokemonPlatinumEngine.Foreign.DsPokemon.Write(Mon("Ratchisel", 105), 1, "Lucas", false));
    }

    [Fact]
    public void EachIsGeneratedFromItsDataUntilItIsSculpted()
    {
        foreach (var s in PokemonDatabase.Ours())
        {
            var genome = PokemonGenomes.For(s.Name);
            Assert.True(genome != null, $"{s.Name} has no genome");
            var built = PokemonGenerator.Build(genome!);
            Assert.Equal(s.Name, built.Model.Species);
            Assert.True(built.Model.Decals.Count(d => d.IsEye) >= 1, $"{s.Name} has no eyes");
        }
    }
}
