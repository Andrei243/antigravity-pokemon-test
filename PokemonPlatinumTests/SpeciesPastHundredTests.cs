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
/// Plan 03 · D14: the species of our own that evolve past level 100, batches 1 to 4 (Kanto, Johto, Hoenn and Sinnoh:
/// docs/plans/D14-proposals-kanto.md, -johto.md, -hoenn.md and -sinnoh.md, each approved by the user on 2026-10-25). The hand-written table (<c>tools/DataImporter/Overrides/species-past-hundred.json</c>)
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
        // Kanto's 46, Johto's 34, Hoenn's 51 and Sinnoh's 39
        Assert.True(Designs.Count >= 170, $"only {Designs.Count} designs");
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

            // The rise: +80, or what the ceiling of 600 leaves a species that isn't legendary; a region's legendary set
            // to 660 (+80 for the birds, the beasts and the Regis, +60 for Latias and Latios); no stat falls
            var was = SpeciesPastHundred.StatsOf(from);
            bool legend = from.Legendary || from.Mythical;
            int rise = Total(d.Stats) - Total(was);
            Assert.All(Stats, stat => Assert.True(d.Stats.Of(stat) >= was.Of(stat), $"{d.Name}'s {stat} falls"));
            if (legend)
            {
                Assert.Equal(660, Total(d.Stats));
                Assert.InRange(rise, 1, 90); // the Treasures of Ruin rise +90, from 570 (Paldea's question 2)
            }
            else
            {
                Assert.True(Total(d.Stats) <= 600, $"{d.Name} totals {Total(d.Stats)}");
                Assert.True(rise == 80 || (rise < 80 && Total(d.Stats) == 600), $"{d.Name} rises {rise}");
            }

            // Evolves by level within its own region's climb, at most 160, with what the data has
            var e = d.Evolution;
            var (low, high) = Climb(from);
            Assert.True(e.Level >= low && e.Level <= high, $"{d.Name} evolves at {e.Level}, outside {low} to {high}");
            Assert.Equal(EvolutionTrigger.LevelUp == Evolution.TriggerOf(e.Method) || e.Method == EvolutionMethod.UseItem, true);
            if (e.Item != null) Assert.True(ItemDatabase.Get(e.Item) is { } item && item.Pocket != ItemPocket.KeyItems, $"{d.Name}: {e.Item}");
            if (e.Move != null) Assert.Contains(from.Learnset.Concat(from.Modern?.Learnset ?? new()), l => l.MoveName == e.Move);
            if (e.Species != null) Assert.True(Species(e.Species) is { IsOurs: false }, $"{d.Name} asks for {e.Species}");
            if (e.Method == EvolutionMethod.LevelAfterDefeating) Assert.InRange(e.Value, 1, 10);
            Assert.True(d.Height > 0 && d.Weight > 0 && d.Category.Length > 0, d.Name);
        }
    }

    /// <summary>
    /// The levels a design may evolve at: within the climb of the region its forebear is from (Hoenn's question 1,
    /// accepted on 2026-10-25). Kanto and Johto from 101, at most 130 (Johto's are 104 to 120); Hoenn, met at 136 to
    /// 162, from 140 to 158; a legendary at most 160 everywhere. Sinnoh, whose own climb never passes 100, takes the band
    /// where the chain first does (its question 1, accepted on 2026-10-25): 101 to 120, its lake guardians at 150.
    /// </summary>
    private static (int Low, int High) Climb(PokemonSpecies from)
    {
        bool legend = from.Legendary || from.Mythical;
        return from.Generation switch
        {
            3 => (140, legend ? 160 : 158),
            4 => (101, legend ? 150 : 120),
            9 => (244, legend ? 258 : 256), // Paldea's climb (its question 1, 2026-10-26): 244 to 256, the Treasures of Ruin at 258
            _ => (101, legend ? 160 : 130)
        };
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
        var context = new EvolutionContext { IsNight = e.Method is EvolutionMethod.LevelDay or EvolutionMethod.FriendshipDay or EvolutionMethod.LevelHoldingItemDay };
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
            case EvolutionMethod.LevelHoldingItemDay: p.HeldItem = ItemDatabase.Get(e.Item!); context.IsNight = false; break;
            case EvolutionMethod.LevelHoldingItemNight: p.HeldItem = ItemDatabase.Get(e.Item!); context.IsNight = true; break;
            case EvolutionMethod.LevelWithSpeciesInParty: context.Party = PartyOf(p, Mon(e.Species!, 50)); break;
            case EvolutionMethod.LevelAfterDefeating: p.EvolutionProgress[Evolution.DefeatKey(e.Species!)] = e.Value; break;
            case EvolutionMethod.LevelKnowsMove: p.Moves.Add(new Move(MoveDatabase.Get(e.Move!))); break;
            case EvolutionMethod.UseItem: context.Item = ItemDatabase.Get(e.Item!); trigger = EvolutionTrigger.UseItem; break;
            case EvolutionMethod.LevelWithItemsInBag: context.Bag = new Inventory(); context.Bag.AddItem(ItemDatabase.Get(e.Item!)!, e.Value); break;
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
            if (d.Evolution.Method is EvolutionMethod.LevelHoldingItem or EvolutionMethod.LevelHoldingItemDay or EvolutionMethod.LevelHoldingItemNight)
                Assert.Null(ready.HeldItem);
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
            Assert.True(e.Method == EvolutionMethod.UseItem ? Evolution.IsUsedToEvolve(item) : e.Method == EvolutionMethod.LevelWithItemsInBag ? Evolution.IsGatheredForEvolution(item) : Evolution.IsHeldForEvolution(item), e.Item);
        }
    }

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var m in members) party.Add(m);
        return party;
    }

    private static string? LevelUp(Pokemon p, EvolutionContext context) => Evolution.Find(p, EvolutionTrigger.LevelUp, context)?.TargetSpecies;

    [Fact]
    public void EachOfAPairGoesFurtherOnlyWithTheOtherInTheParty()
    {
        // Batch 3's sets (Hoenn's questions 2 and 3): Plusle and Minun, Volbeat and Illumise, Latias and Latios, each
        // with the other; batch 2's Raysail with an Octillery. One of its own kind beside it is not its partner.
        foreach (var (who, partner, into, level) in new[]
        {
            ("Plusle", "Minun", "Anodion", 142), ("Minun", "Plusle", "Cathodion", 142),
            ("Volbeat", "Illumise", "Photurion", 145), ("Illumise", "Volbeat", "Lampyria", 145),
            ("Latias", "Latios", "Aurelias", 160), ("Latios", "Latias", "Aurelios", 160),
            ("Mantine", "Octillery", "Raysail", 112),
            ("Honchkrow", "Murkrow", "Corvicapo", 115)
        })
        {
            var p = Mon(who, level);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(who, level)) }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(partner, 5)) }));
            Assert.Null(LevelUp(Mon(who, level - 1), new EvolutionContext { Party = PartyOf(Mon(partner, 5)) }));
        }
    }

    [Fact]
    public void TheRegisAndTheBeastsGoFurtherBesideWhoMadeThem()
    {
        // The Regis with a Regigigas in the party (Hoenn's question 2), the beasts with a Ho-Oh (Johto's question 2):
        // another of the trio is not enough
        foreach (var (who, other, maker, into, level) in new[]
        {
            ("Regirock", "Regice", "Regigigas", "Regimonolith", 160), ("Regice", "Registeel", "Regigigas", "Regiglacier", 160),
            ("Registeel", "Regirock", "Regigigas", "Regiferrum", 160),
            ("Raikou", "Entei", "Ho-Oh", "Raitenko", 150), ("Entei", "Suicune", "Ho-Oh", "Enkaiou", 150),
            ("Suicune", "Raikou", "Ho-Oh", "Suiseiren", 150)
        })
        {
            var p = Mon(who, level);
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(other, level)) }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(other, level), Mon(maker, 70)) }));
        }

        // A legendary's evolution is banned from the Battle Frontier with the rest of them; Wobbulwark may enter, as
        // Wobbuffet may (Johto's question 5)
        Assert.All(new[] { "Regimonolith", "Aurelias", "Raitenko" }, name => Assert.False(FrontierRules.MayEnter(Mon(name, 160))));
        Assert.True(FrontierRules.MayEnter(Mon("Wobbulwark", 110)));
    }

    [Fact]
    public void ZangooseAndSeviperGrowByBeatingThreeOfTheOther()
    {
        // Hoenn's question 5: LevelAfterDefeating, Kingambit's condition, counted by the battle's knock-outs
        var zangoose = Mon("Zangoose", 148, "Crush Claw");
        for (int beaten = 0; beaten < 3; beaten++)
        {
            Assert.Null(LevelUp(zangoose, new EvolutionContext()));
            var battle = Battle(zangoose, Mon("Seviper", 5), Steady(), Ruleset.Modern);
            for (int turn = 0; turn < 10 && !battle.IsBattleOver; turn++) Turn(battle);
            Assert.True(battle.IsBattleOver);
            Assert.Equal(beaten + 1, Evolution.Progress(zangoose, Evolution.DefeatKey("Seviper")));
        }
        Assert.Equal("Ravagoose", LevelUp(zangoose, new EvolutionContext()));

        // Another species knocked out counts for nothing, and the count stops where the evolution asks
        var other = Mon("Zangoose", 148, "Crush Claw");
        var onAnother = Battle(other, Mon("Ekans", 5), Steady(), Ruleset.Modern);
        for (int turn = 0; turn < 10 && !onAnother.IsBattleOver; turn++) Turn(onAnother);
        Assert.Equal(0, Evolution.Progress(other, Evolution.DefeatKey("Seviper")));
        for (int i = 0; i < 5; i++) Evolution.CountDefeat(other, Species("Seviper"));
        Assert.Equal(3, Evolution.Progress(other, Evolution.DefeatKey("Seviper")));

        var seviper = Mon("Seviper", 148);
        Evolution.CountDefeat(seviper, Species("Seviper"));
        Assert.Null(LevelUp(seviper, new EvolutionContext()));
        for (int i = 0; i < 3; i++) Evolution.CountDefeat(seviper, Species("Zangoose"));
        Assert.Equal("Scimitail", LevelUp(seviper, new EvolutionContext()));
    }

    [Fact]
    public void EspeonAndUmbreonNeedTheirStoneAndTheirFriendship()
    {
        // Johto's question 1: as batch 1's three Eeveelutions, each at 120 holding its stone with friendship 220 or more;
        // Sinnoh's Leafeon and Glaceon the same
        foreach (var (who, stone, into) in new[]
        {
            ("Espeon", "Sun Stone", "Zenitheon"), ("Umbreon", "Moon Stone", "Eclipseon"),
            ("Leafeon", "Leaf Stone", "Dryadeon"), ("Glaceon", "Ice Stone", "Borealeon")
        })
        {
            var p = Mon(who, 120);
            p.HeldItem = ItemDatabase.Get(stone);
            p.Friendship = FriendshipRules.EvolveAt - 1;
            Assert.Null(LevelUp(p, new EvolutionContext()));
            p.Friendship = FriendshipRules.EvolveAt;
            var found = Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext());
            Assert.Equal(into, found?.TargetSpecies);
            Evolution.Evolve(p, found!, new EvolutionContext());
            Assert.Null(p.HeldItem);

            // The stone used on it from the bag does nothing
            var used = Mon(who, 120);
            used.Friendship = 255;
            Assert.Null(Evolution.Find(used, EvolutionTrigger.UseItem, new EvolutionContext { Item = ItemDatabase.Get(stone) }));
        }

        // Sunflora and Delcatty take the stone again from the bag, past their level and not before; Sinnoh's Cherrim,
        // Mismagius and Froslass too
        foreach (var (who, stone, into, level) in new[]
        {
            ("Sunflora", "Sun Stone", "Helianthra", 110), ("Delcatty", "Moon Stone", "Felinelle", 140),
            ("Cherrim", "Sun Stone", "Sakurabloom", 104), ("Mismagius", "Dusk Stone", "Hexmagistra", 115), ("Froslass", "Dawn Stone", "Frostbride", 112)
        })
        {
            var context = new EvolutionContext { Item = ItemDatabase.Get(stone) };
            Assert.Null(Evolution.Find(Mon(who, level - 1), EvolutionTrigger.UseItem, context));
            Assert.Equal(into, Evolution.Find(Mon(who, level), EvolutionTrigger.UseItem, context)?.TargetSpecies);
        }
    }

    [Fact]
    public void LunatoneAndSolrockHoldTheirStonesAtTheirHour()
    {
        var lunatone = Mon("Lunatone", 150);
        lunatone.HeldItem = ItemDatabase.Get("Moon Stone");
        Assert.Null(LevelUp(lunatone, new EvolutionContext { IsNight = false }));
        Assert.Equal("Selenolith", LevelUp(lunatone, new EvolutionContext { IsNight = true }));

        var solrock = Mon("Solrock", 150);
        solrock.HeldItem = ItemDatabase.Get("Sun Stone");
        Assert.Null(LevelUp(solrock, new EvolutionContext { IsNight = true }));
        Assert.Equal("Heliolith", LevelUp(solrock, new EvolutionContext { IsNight = false }));
    }

    [Fact]
    public void SleighwingIsMetAtTheIceRock()
    {
        // Johto's question 6: Delibird at Route 217's Ice Rock (Glaceon's), not anywhere cold
        var delibird = Mon("Delibird", 104);
        Assert.Null(LevelUp(delibird, new EvolutionContext()));
        Assert.Null(LevelUp(delibird, new EvolutionContext { Sites = new[] { "Moss Rock" } }));
        Assert.Equal("Sleighwing", LevelUp(delibird, new EvolutionContext { Sites = new[] { "Ice Rock" } }));
        Assert.Null(LevelUp(Mon("Delibird", 103), new EvolutionContext { Sites = new[] { "Ice Rock" } }));
    }

    [Fact]
    public void HoennsSpeciesEvolveWithinHoennsClimbAndNotAtTheirFirstLevelThere()
    {
        // Hoenn's question 1: met at 136 to 162 (regions/hoenn.json's levels), so none of its species evolves before 140
        var hoenn = Designs.Where(d => Species(d.From).Generation == 3).ToList();
        Assert.Equal(51, hoenn.Count);
        Assert.All(hoenn, d => Assert.InRange(d.Evolution.Level, 140, 160));
        Assert.All(hoenn.Where(d => !Species(d.From).Legendary), d => Assert.InRange(d.Evolution.Level, 140, 158));
        var johto = Designs.Where(d => Species(d.From).Generation == 2).ToList();
        Assert.Equal(34, johto.Count);
        Assert.All(johto.Where(d => !Species(d.From).Legendary), d => Assert.InRange(d.Evolution.Level, 104, 120));
    }

    [Fact]
    public void SinnohsSpeciesEvolveInTheBandWhereTheChainFirstPassesAHundred()
    {
        // Sinnoh's question 1: its own levels never pass 100 (Platinum's), so its 36 evolve at 101 to 120, Kanto's
        // band, and its legendary set at 150, as the birds and the beasts
        var sinnoh = Designs.Where(d => Species(d.From).Generation == 4).ToList();
        Assert.Equal(39, sinnoh.Count);
        Assert.All(sinnoh.Where(d => !Species(d.From).Legendary), d => Assert.InRange(d.Evolution.Level, 101, 120));
        Assert.Equal(new[] { "Uxinous", "Mespathos", "Azelthymos" }, sinnoh.Where(d => Species(d.From).Legendary).Select(d => d.Name));
        Assert.All(sinnoh.Where(d => Species(d.From).Legendary), d => Assert.Equal(150, d.Evolution.Level));
    }

    [Fact]
    public void TheLakeGuardiansCloseARing()
    {
        // Sinnoh's question 2: each at 150 with the next in the party (Uxie with a Mesprit, Mesprit with an Azelf, Azelf
        // with an Uxie); the one before it, or another of its own kind, is not enough
        foreach (var (who, next, before, into) in new[]
        {
            ("Uxie", "Mesprit", "Azelf", "Uxinous"), ("Mesprit", "Azelf", "Uxie", "Mespathos"), ("Azelf", "Uxie", "Mesprit", "Azelthymos")
        })
        {
            var p = Mon(who, 150);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(before, 50), Mon(who, 50)) }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(next, 50)) }));
            Assert.Null(LevelUp(Mon(who, 149), new EvolutionContext { Party = PartyOf(Mon(next, 50)) }));
            Assert.Equal(660, Total(SpeciesPastHundred.StatsOf(Species(into))));
            Assert.False(FrontierRules.MayEnter(Mon(into, 150)));
        }
    }

    [Fact]
    public void LucarioAndLopunnyGrowByFriendshipByDay()
    {
        // FriendshipDay, Riolu's own way, past a level: by day with friendship 220 or more, never at night
        foreach (var (who, into, level) in new[] { ("Lucario", "Aurasage", 120), ("Lopunny", "Lapinova", 110) })
        {
            var p = Mon(who, level);
            p.Friendship = FriendshipRules.EvolveAt;
            Assert.Equal(into, LevelUp(p, new EvolutionContext { IsNight = false }));
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = true }));
            p.Friendship = FriendshipRules.EvolveAt - 1;
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = false }));
            var early = Mon(who, level - 1);
            early.Friendship = 255;
            Assert.Null(LevelUp(early, new EvolutionContext { IsNight = false }));
        }
    }

    [Fact]
    public void SinnohsThreePlacesEachGrowTheirOwn()
    {
        // LevelAtLocation at Sinnoh's own places: Abomasnow at the Ice Rock, Tangrowth at the Moss Rock, Probopass at
        // the Magnetic Field, and none of them at another's
        string[] places = { "Ice Rock", "Moss Rock", "Magnetic Field" };
        foreach (var (who, place, into, level) in new[]
        {
            ("Abomasnow", "Ice Rock", "Glaciarbor", 115), ("Tangrowth", "Moss Rock", "Tanglewood", 120), ("Probopass", "Magnetic Field", "Lodestarch", 118)
        })
        {
            var p = Mon(who, level);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            foreach (var other in places.Where(o => o != place)) Assert.Null(LevelUp(p, new EvolutionContext { Sites = new[] { other } }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Sites = new[] { place } }));
            Assert.Null(LevelUp(Mon(who, level - 1), new EvolutionContext { Sites = new[] { place } }));
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
        foreach (var name in new[] { "Ratchisel", "Glacirune", "Mimestro", "Ironwyrm", "Aurelias", "Heartreef", "Aurasage", "Uxinous" })
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
        Assert.All(new[] { "kanto", Pokedex.Sinnoh, "original-johto", "updated-johto", "hoenn" }, dex => Assert.DoesNotContain(Pokedex.Entries(dex), e => e.Species.IsOurs));

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
