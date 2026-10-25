using System;
using System.Linq;
using System.Text.Json;
using DataImporter;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D14, batch 5 (Unova: docs/plans/D14-proposals-unova.md, approved by the user with every default and all
/// 52 designs). What is Unova's own beside <see cref="SpeciesPastHundredTests"/>, which holds every batch to the
/// proposals' rules: the band within Unova's climb (question 1), the Swords of Justice beside Keldeo (question 2), the
/// pairs, the stones again past a level, Darmanitan's hidden ability (question 3), and the forms of Basculin, Sawsbuck
/// and Jellicent that evolve into one species of ours (questions 4 to 6), while the regional forms evolve into none.
/// </summary>
public class SpeciesPastHundredUnovaTests
{
    private static readonly Lazy<System.Collections.Generic.List<SpeciesDesign>> designs =
        new(() => SpeciesPastHundred.Load(SpeciesPastHundred.PathIn(Repository.Root)));

    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var m in members) party.Add(m);
        return party;
    }

    private static string? LevelUp(Pokemon p, EvolutionContext context) => Evolution.Find(p, EvolutionTrigger.LevelUp, context)?.TargetSpecies;

    private static int Total(PokemonSpecies s) => s.BaseHP + s.BaseAttack + s.BaseDefense + s.BaseSpAttack + s.BaseSpDefense + s.BaseSpeed;

    [Fact]
    public void UnovasSpeciesEvolveWithinUnovasClimbBeforeItsChampion()
    {
        // Question 1: Unova is met at 162 to 185 (regions/unova.json's levels), so its 49 evolve at 165 to 183, and the
        // Swords of Justice at 190, after Iris
        var unova = designs.Value.Where(d => Species(d.From).Generation == 5 && d.Evolution.FromForm == null).ToList();
        Assert.Equal(52, unova.Count);
        Assert.All(unova.Where(d => !Species(d.From).Legendary), d => Assert.InRange(d.Evolution.Level, 165, 183));
        Assert.Equal(new[] { "Cobaltarch", "Terrakhan", "Virizenith" }, unova.Where(d => Species(d.From).Legendary).Select(d => d.Name));
        Assert.All(unova.Where(d => Species(d.From).Legendary), d => Assert.Equal(190, d.Evolution.Level));
        Assert.All(unova, d => Assert.Equal(2000 + Species(d.From).DexNumber, Species(d.Name).DexNumber));
    }

    [Fact]
    public void EachNeedsMoreThanItsLevelWhereItsRowAsksMore()
    {
        // The shared test tries the bare level at 160, under Unova's band: here each forebear at its own level with
        // nothing else (no move, no item, no partner, no friendship, by day and dry) stays as it is but Sawsbuck
        foreach (var d in designs.Value.Where(d => Species(d.From).Generation == 5 && d.Evolution.FromForm == null))
        {
            var p = Mon(d.From, d.Evolution.Level, "Tackle");
            p.Friendship = 0;
            var context = new EvolutionContext { IsNight = d.Evolution.Method is EvolutionMethod.LevelDay or EvolutionMethod.FriendshipDay };
            var found = LevelUp(p, context);
            if (d.Evolution.Method == EvolutionMethod.Level) Assert.Equal(d.Name, found);
            else Assert.NotEqual(d.Name, found);
        }
    }

    [Fact]
    public void TheSwordsOfJusticeGoFurtherBesideKeldeo()
    {
        // Question 2: each at 190 with a Keldeo in the party (in either form); the other two of the trio are not enough
        foreach (var (who, others, into) in new[]
        {
            ("Cobalion", new[] { "Terrakion", "Virizion" }, "Cobaltarch"),
            ("Terrakion", new[] { "Cobalion", "Virizion" }, "Terrakhan"),
            ("Virizion", new[] { "Cobalion", "Terrakion" }, "Virizenith")
        })
        {
            var p = Mon(who, 190);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon(others[0], 70), Mon(others[1], 70)) }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon("Keldeo", 70)) }));
            var resolute = Mon("Keldeo", 70);
            resolute.ChangeForm("Keldeo-Resolute");
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, resolute) }));
            Assert.Null(LevelUp(Mon(who, 189), new EvolutionContext { Party = PartyOf(Mon("Keldeo", 70)) }));

            Assert.Equal(660, Total(Species(into)));
            Assert.Equal(3, Species(into).CatchRate);
            Assert.False(FrontierRules.MayEnter(Mon(into, 190)));
        }

        // The rest of Unova's may be entered, as their forebears may
        Assert.True(FrontierRules.MayEnter(Mon("Helioptera", 183)));
    }

    [Fact]
    public void EachOfAPairGoesFurtherOnlyWithTheOtherInTheParty()
    {
        // Question 8's Throh and Sawk, and Escavalier and Accelgor, each made by trading for the other
        foreach (var (who, partner, into, level) in new[]
        {
            ("Throh", "Sawk", "Throjutsu", 172), ("Sawk", "Throh", "Sawkendo", 172),
            ("Escavalier", "Accelgor", "Lancelier", 176), ("Accelgor", "Escavalier", "Velocigor", 176)
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
    public void HeatmorAndDurantGrowByBeatingThreeOfTheOther()
    {
        // Batch 3's Zangoose and Seviper again: LevelAfterDefeating, counted only for the other's species
        foreach (var (who, prey, into) in new[] { ("Heatmor", "Durant", "Pyrovore"), ("Durant", "Heatmor", "Formicarch") })
        {
            var p = Mon(who, 176);
            Evolution.CountDefeat(p, Species(who));
            for (int i = 0; i < 2; i++) Evolution.CountDefeat(p, Species(prey));
            Assert.Null(LevelUp(p, new EvolutionContext()));
            Evolution.CountDefeat(p, Species(prey));
            Assert.Equal(3, Evolution.Progress(p, Evolution.DefeatKey(prey)));
            Assert.Equal(into, LevelUp(p, new EvolutionContext()));

            var early = Mon(who, 175);
            for (int i = 0; i < 3; i++) Evolution.CountDefeat(early, Species(prey));
            Assert.Null(LevelUp(early, new EvolutionContext()));
        }
    }

    [Fact]
    public void TheStonesThatMadeThemMakeThemAgainPastALevel()
    {
        // The three monkeys, Musharna, Whimsicott and Lilligant (a pair: each by its Sun Stone) and Cinccino: the stone
        // used on it from the bag at its level or above, never held at a level-up and never before
        foreach (var (who, stone, into, level) in new[]
        {
            ("Simisage", "Leaf Stone", "Simisylvan", 170), ("Simisear", "Fire Stone", "Simiscorch", 170), ("Simipour", "Water Stone", "Simitorrent", 170),
            ("Musharna", "Moon Stone", "Somnarna", 170), ("Whimsicott", "Sun Stone", "Cottonimbus", 172), ("Lilligant", "Sun Stone", "Ballerilly", 172),
            ("Cinccino", "Shiny Stone", "Chinchillux", 170)
        })
        {
            var context = new EvolutionContext { Item = ItemDatabase.Get(stone) };
            Assert.Null(Evolution.Find(Mon(who, level - 1), EvolutionTrigger.UseItem, context));
            Assert.Equal(into, Evolution.Find(Mon(who, level), EvolutionTrigger.UseItem, context)?.TargetSpecies);
            Assert.Null(Evolution.Find(Mon(who, 183), EvolutionTrigger.UseItem, new EvolutionContext { Item = ItemDatabase.Get("Thunder Stone") }));

            var holding = Mon(who, level);
            holding.HeldItem = ItemDatabase.Get(stone);
            Assert.Null(LevelUp(holding, new EvolutionContext()));
        }
    }

    [Fact]
    public void SwoobatAndAudinoGrowByFriendshipAtTheirHour()
    {
        // FriendshipNight (Woobat's own way) and FriendshipDay, past a level
        foreach (var (who, into, level, night) in new[] { ("Swoobat", "Amorabat", 166, true), ("Audino", "Remedino", 168, false) })
        {
            var p = Mon(who, level);
            p.Friendship = FriendshipRules.EvolveAt;
            Assert.Equal(into, LevelUp(p, new EvolutionContext { IsNight = night }));
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = !night }));
            p.Friendship = FriendshipRules.EvolveAt - 1;
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = night }));
        }
    }

    [Fact]
    public void CryogonalAtTheIceRockAndTheRainsTwo()
    {
        // LevelAtLocation at Route 217's Ice Rock, and LevelInRain for Amoonguss and Stunfisk
        var cryogonal = Mon("Cryogonal", 178);
        Assert.Null(LevelUp(cryogonal, new EvolutionContext()));
        Assert.Null(LevelUp(cryogonal, new EvolutionContext { Sites = new[] { "Moss Rock" } }));
        Assert.Equal("Cryohedron", LevelUp(cryogonal, new EvolutionContext { Sites = new[] { "Ice Rock" } }));

        foreach (var (who, into, level) in new[] { ("Amoonguss", "Sporemonarch", 174), ("Stunfisk", "Fluxflounder", 172) })
        {
            Assert.Null(LevelUp(Mon(who, level), new EvolutionContext { IsRaining = false }));
            Assert.Equal(into, LevelUp(Mon(who, level), new EvolutionContext { IsRaining = true }));
            Assert.Null(LevelUp(Mon(who, level - 1), new EvolutionContext { IsRaining = true }));
        }
    }

    [Fact]
    public void TheFossilsEachHoldARareBone()
    {
        // Question 7: the Cover and Plume Fossils are no items of the data, so both hold the Rare Bone, used up
        foreach (var (who, into) in new[] { ("Carracosta", "Tortugarch"), ("Archeops", "Primavian") })
        {
            var p = Mon(who, 178);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            p.HeldItem = ItemDatabase.Get("Rare Bone");
            var found = Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext());
            Assert.Equal(into, found?.TargetSpecies);
            Evolution.Evolve(p, found!, new EvolutionContext());
            Assert.Null(p.HeldItem);
        }
        Assert.Equal(600, Total(Species("Primavian")));
        Assert.Equal(new[] { "Defeatist" }, Species("Primavian").Abilities);
    }

    [Fact]
    public void DarumandalaMeditatesWithInnerFocusInPlaceOfZenMode()
    {
        // Question 3: Zen Mode changes Darmanitan's own shape and nothing else's, so ours has Inner Focus hidden
        Assert.Equal("Zen Mode", Species("Darmanitan").HiddenAbility);
        Assert.Equal("Inner Focus", Species("Darumandala").HiddenAbility);
        Assert.Equal(Species("Darmanitan").Abilities, Species("Darumandala").Abilities);
    }

    [Fact]
    public void BasculinOfEveryStripeBecomesBascurageAndTheWhiteOneKeepsBasculegion()
    {
        // Question 5: any stripe evolves by level knowing Head Smash; the white-striped one still goes to Basculegion by
        // its recoil, which is its row's and comes first
        foreach (var form in new string?[] { null, "Basculin-Blue-Striped", "Basculin-White-Striped" })
        {
            var p = Mon("Basculin", 172, "Head Smash");
            if (form != null) p.ChangeForm(form);
            Assert.Equal("Bascurage", LevelUp(p, new EvolutionContext()));
            Assert.Null(LevelUp(Mon("Basculin", 172, "Tackle"), new EvolutionContext()));

            var found = Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext())!;
            Evolution.Evolve(p, found, new EvolutionContext());
            Assert.Equal(("Bascurage", (string?)null), (p.Species.Name, p.Form));
        }

        var white = Mon("Basculin", 172, "Head Smash");
        white.ChangeForm("Basculin-White-Striped");
        Evolution.CountRecoil(white, 300);
        Assert.Equal("Basculegion", LevelUp(white, new EvolutionContext()));
        var red = Mon("Basculin", 172, "Head Smash");
        Evolution.CountRecoil(red, 300);
        Assert.Equal("Bascurage", LevelUp(red, new EvolutionContext()));
    }

    [Fact]
    public void EveryCoatOfSawsbuckAndEitherJellicentBecomeOneSpeciesOfOurs()
    {
        // Question 4: Seasonstag carries the four seasons at once, so every coat evolves into it and keeps no form
        foreach (var form in new string?[] { null, "Sawsbuck-Summer", "Sawsbuck-Autumn", "Sawsbuck-Winter" })
        {
            var p = Mon("Sawsbuck", 174);
            if (form != null) p.ChangeForm(form);
            var found = Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext());
            Assert.Equal("Seasonstag", found?.TargetSpecies);
            Evolution.Evolve(p, found!, new EvolutionContext());
            Assert.Null(p.Form);
        }

        // Question 6: the female Jellicent, a form of the data, evolves into the one Jellicrown, whose female look is
        // its own (the species keeps the flag that its females look different)
        var female = new Pokemon(Species("Jellicent"), 176, Gender.Female, Nature.Hardy, false);
        female.ChangeForm("Jellicent-Female");
        var into = Evolution.Find(female, EvolutionTrigger.LevelUp, new EvolutionContext { IsNight = true });
        Assert.Equal("Jellicrown", into?.TargetSpecies);
        Assert.Null(LevelUp(female, new EvolutionContext { IsNight = false }));
        Evolution.Evolve(female, into!, new EvolutionContext { IsNight = true });
        Assert.Equal(("Jellicrown", Gender.Female, (string?)null), (female.Species.Name, female.Gender, female.Form));
        Assert.True(Species("Jellicrown").GenderDifferences);
        Assert.Null(Species("Jellicrown").Forms);
    }

    [Fact]
    public void TheRegionalFormsOfUnovasForebearsBecomeNoneOfOurs()
    {
        // The settled rule: an evolution that starts from a regional form is its form's own line
        foreach (var (who, form, into, level) in new[]
        {
            ("Lilligant", "Lilligant-Hisui", "Ballerilly", 172), ("Zoroark", "Zoroark-Hisui", "Zoromirage", 178),
            ("Braviary", "Braviary-Hisui", "Imperavia", 182), ("Stunfisk", "Stunfisk-Galar", "Fluxflounder", 172),
            ("Darmanitan", "Darmanitan-Galar-Standard", "Darumandala", 174)
        })
        {
            var p = Mon(who, level);
            p.ChangeForm(form);
            var context = new EvolutionContext { IsNight = into == "Zoromirage", IsRaining = true, Item = ItemDatabase.Get("Sun Stone") };
            Assert.NotEqual(into, LevelUp(p, context));
            Assert.NotEqual(into, Evolution.Find(p, EvolutionTrigger.UseItem, context)?.TargetSpecies);
        }
    }

    [Fact]
    public void OneOfUnovasIsSavedAndReadBack()
    {
        foreach (var name in new[] { "Beaconwatch", "Seasonstag", "Jellicrown", "Cobaltarch" })
        {
            var p = new Pokemon(Species(name), 190, new Random(5));
            var saved = SavedPokemonData.FromPokemon(p);
            Assert.Equal(Species(name).DexNumber, saved.DexNumber);
            var read = JsonSerializer.Deserialize<SavedPokemonData>(JsonSerializer.Serialize(saved, GameDataFiles.Json), GameDataFiles.Json)!.ToPokemon();
            Assert.Equal((name, 190, p.Personality, p.Gender), (read.Species.Name, read.Level, read.Personality, read.Gender));
        }
    }
}
