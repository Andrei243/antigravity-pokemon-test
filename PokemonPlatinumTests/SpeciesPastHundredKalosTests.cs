using System.Linq;
using DataImporter;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D14, batch 6 (Kalos: docs/plans/D14-proposals-kalos.md, all 24 approved by the user on 2026-10-26 with
/// every default but question 2, whose Zygarde waits in D14-proposals-kalos-supplement.md). What is new to this batch:
/// Kalos's own climb as the band (188 to 203), the species whose forms ours don't keep (a Gourgeist's size, a
/// Furfrou's trim, the female Meowstic), the fossils holding a Rare Bone at their hour, Pangoro with a Pancham, and
/// Sylveon's end with the other Eeveelutions. <see cref="SpeciesPastHundredTests"/> holds every design to the shared
/// rules.
/// </summary>
public class SpeciesPastHundredKalosTests
{
    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static string? LevelUp(Pokemon p, EvolutionContext context) => Evolution.Find(p, EvolutionTrigger.LevelUp, context)?.TargetSpecies;

    private static Pokemon Female(string species, int level) => new(Species(species), level, Gender.Female, Nature.Hardy, false);

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var m in members) party.Add(m);
        return party;
    }

    /// <summary>Evolves it the way the game does once the evolution is found.</summary>
    private static void Grow(Pokemon p, EvolutionContext context)
    {
        var found = Evolution.Find(p, EvolutionTrigger.LevelUp, context);
        Assert.NotNull(found);
        Evolution.Evolve(p, found!, context);
    }

    [Fact]
    public void KalossSpeciesEvolveWithinKalossOwnClimb()
    {
        // Question 1's default: Kalos is met at about 185 to 205, so its species evolve at 188 to 203, from its first
        // Gym Leader to its Elite Four; Sylveon's end at 120 with the seven Eeveelutions before it
        var designs = SpeciesPastHundred.Load(SpeciesPastHundred.PathIn(Repository.Root));
        var kalos = designs.Where(d => Species(d.From).Generation == 6 && d.Evolution.FromForm == null).ToList();
        Assert.Equal(24, kalos.Count);
        Assert.All(kalos.Where(d => d.From != "Sylveon"), d => Assert.InRange(d.Evolution.Level, 188, 203));
        Assert.Equal(120, kalos.Single(d => d.From == "Sylveon").Evolution.Level);
        Assert.All(kalos, d => Assert.Equal(2000 + Species(d.From).DexNumber, Species(d.Name).DexNumber));
        Assert.All(kalos, d => Assert.InRange(Species(d.Name).DexNumber, 2660, 2715));

        // No legendary or mythical of Kalos goes further (question 2's Zygarde is a design still to approve)
        Assert.All(new[] { "Xerneas", "Yveltal", "Zygarde", "Diancie", "Hoopa", "Volcanion" },
            name => Assert.DoesNotContain(Species(name).Evolutions ?? new(), e => Species(e.TargetSpecies).IsOurs));
    }

    [Fact]
    public void AGourgeistOfAnySizeAndAFurfrouOfAnyTrimEvolveWithNoFormLeftOver()
    {
        // Questions 3 and 4: one Lanternhusk whatever the size, one Majesthound whatever the trim
        foreach (var size in new[] { null, "Gourgeist-Small", "Gourgeist-Large", "Gourgeist-Super" })
        {
            var gourgeist = Mon("Gourgeist", 198);
            gourgeist.ChangeForm(size);
            Assert.Null(LevelUp(gourgeist, new EvolutionContext { IsNight = false }));
            Grow(gourgeist, new EvolutionContext { IsNight = true });
            Assert.Equal(("Lanternhusk", (string?)null, "Lanternhusk"), (gourgeist.Species.Name, gourgeist.Form, gourgeist.ModelName));
            Assert.Equal((90, 105, 122), (gourgeist.Species.BaseHP, gourgeist.Species.BaseAttack, gourgeist.Species.BaseDefense));
        }

        foreach (var trim in new[] { null, "Furfrou-Heart", "Furfrou-La-Reine", "Furfrou-Pharaoh" })
        {
            var furfrou = Mon("Furfrou", 190);
            furfrou.ChangeForm(trim);
            furfrou.Friendship = FriendshipRules.EvolveAt;
            Assert.Null(LevelUp(furfrou, new EvolutionContext { IsNight = true }));
            Grow(furfrou, new EvolutionContext { IsNight = false });
            Assert.Equal(("Majesthound", (string?)null), (furfrou.Species.Name, furfrou.Form));
            Assert.Equal("Fur Coat", furfrou.AbilityName);
        }

        var unfriendly = Mon("Furfrou", 203);
        unfriendly.Friendship = FriendshipRules.EvolveAt - 1;
        Assert.Null(LevelUp(unfriendly, new EvolutionContext { IsNight = false }));
    }

    [Fact]
    public void AFemaleMeowsticBecomesOcculynxInHerOwnLook()
    {
        // Question 5: one Occulynx; the female leaves her form, takes the species' abilities slot for slot (her hidden
        // Competitive becomes Prankster) and is drawn in her own female look
        var female = Female("Meowstic", 190);
        Assert.Equal("Meowstic-Female", female.Form);
        female.AbilityName = female.HiddenAbilityName;
        Assert.Equal("Competitive", female.AbilityName);
        Assert.Null(LevelUp(female, new EvolutionContext { IsNight = false }));
        Grow(female, new EvolutionContext { IsNight = true });
        Assert.Equal(("Occulynx", (string?)null), (female.Species.Name, female.Form));
        Assert.Equal("Prankster", female.AbilityName);
        Assert.True(Species("Occulynx").GenderDifferences);
        Assert.True(PokemonLook.HasFemale("Occulynx"));
        Assert.Equal("Occulynx+f", female.LookName);

        var male = Mon("Meowstic", 190);
        Grow(male, new EvolutionContext { IsNight = true });
        Assert.Equal("Occulynx", male.LookName);

        // Pyroar's female mane is a form of the look; Blazemonarch keeps the difference as a female look of its own
        var lioness = Female("Pyroar", 190);
        Assert.Equal("Pyroar-Female", lioness.Form);
        Assert.Null(LevelUp(lioness, new EvolutionContext { IsNight = true }));
        Grow(lioness, new EvolutionContext { IsNight = false });
        Assert.Equal(("Blazemonarch", (string?)null, "Blazemonarch+f"), (lioness.Species.Name, lioness.Form, lioness.LookName));
    }

    [Fact]
    public void TheHisuianAvaluggStaysHisuis()
    {
        // Question 7: Glacitadel comes from the Kalosian Avalugg alone
        var hisui = Mon("Avalugg", 200);
        hisui.ChangeForm("Avalugg-Hisui");
        hisui.HeldItem = ItemDatabase.Get("Never-Melt Ice");
        Assert.NotEqual("Glacitadel", LevelUp(hisui, new EvolutionContext()));

        var kalos = Mon("Avalugg", 200);
        kalos.HeldItem = ItemDatabase.Get("Never-Melt Ice");
        Assert.Equal("Glacitadel", LevelUp(kalos, new EvolutionContext()));
        Grow(kalos, new EvolutionContext());
        Assert.Null(kalos.HeldItem);
    }

    [Fact]
    public void TheFossilsHoldARareBoneAtTheirFirstStagesHour()
    {
        // Question 6: no Jaw Fossil or Sail Fossil in the data, so a Rare Bone, Tyrantrum by day and Aurorus at night
        foreach (var (who, into, night) in new[] { ("Tyrantrum", "Despotitan", false), ("Aurorus", "Borealosaur", true) })
        {
            var p = Mon(who, 196);
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = night }));
            p.HeldItem = ItemDatabase.Get("Rare Bone");
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = !night }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { IsNight = night }));
            var early = Mon(who, 195);
            early.HeldItem = ItemDatabase.Get("Rare Bone");
            Assert.Null(LevelUp(early, new EvolutionContext { IsNight = night }));
            Assert.Equal(600, Species(into).BaseHP + Species(into).BaseAttack + Species(into).BaseDefense
                + Species(into).BaseSpAttack + Species(into).BaseSpDefense + Species(into).BaseSpeed);
        }
    }

    [Fact]
    public void PangoroBecomesTheBossOnlyWithAPanchamBesideIt()
    {
        var pangoro = Mon("Pangoro", 192);
        Assert.Null(LevelUp(pangoro, new EvolutionContext()));
        Assert.Null(LevelUp(pangoro, new EvolutionContext { Party = PartyOf(pangoro, Mon("Pangoro", 50)) }));
        Assert.Equal("Bambrawler", LevelUp(pangoro, new EvolutionContext { Party = PartyOf(pangoro, Mon("Pancham", 5)) }));
        Assert.Null(LevelUp(Mon("Pangoro", 191), new EvolutionContext { Party = PartyOf(Mon("Pancham", 5)) }));
    }

    [Fact]
    public void SylveonsEndIsTheEighthEeveelutionsAndHeliolisksSunStoneComesAgain()
    {
        // Sylveon as the seven before it: level 120 holding a Shiny Stone with friendship 220 or more
        var sylveon = Mon("Sylveon", 120);
        sylveon.HeldItem = ItemDatabase.Get("Shiny Stone");
        sylveon.Friendship = FriendshipRules.EvolveAt - 1;
        Assert.Null(LevelUp(sylveon, new EvolutionContext()));
        sylveon.Friendship = FriendshipRules.EvolveAt;
        Assert.Equal("Faerieon", LevelUp(sylveon, new EvolutionContext()));
        var used = Mon("Sylveon", 120);
        used.Friendship = 255;
        Assert.Null(Evolution.Find(used, EvolutionTrigger.UseItem, new EvolutionContext { Item = ItemDatabase.Get("Shiny Stone") }));

        // Heliolisk takes the Sun Stone again from the bag, past 194 and not before; held, it does nothing
        var stone = new EvolutionContext { Item = ItemDatabase.Get("Sun Stone") };
        Assert.Null(Evolution.Find(Mon("Heliolisk", 193), EvolutionTrigger.UseItem, stone));
        Assert.Equal("Coronafrill", Evolution.Find(Mon("Heliolisk", 194), EvolutionTrigger.UseItem, stone)?.TargetSpecies);
        var holding = Mon("Heliolisk", 203);
        holding.HeldItem = ItemDatabase.Get("Sun Stone");
        Assert.Null(LevelUp(holding, new EvolutionContext()));

        // Dragalge in the rain, and Aromatisse and Slurpuff holding what made them
        Assert.Null(LevelUp(Mon("Dragalge", 196), new EvolutionContext()));
        Assert.Equal("Kelpdrake", LevelUp(Mon("Dragalge", 196), new EvolutionContext { IsRaining = true }));
        foreach (var (who, item, into) in new[] { ("Aromatisse", "Sachet", "Perfumelle"), ("Slurpuff", "Whipped Dream", "Soufflair") })
        {
            var p = Mon(who, 194);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            p.HeldItem = ItemDatabase.Get(item);
            Assert.Equal(into, LevelUp(p, new EvolutionContext()));
        }
    }
}
