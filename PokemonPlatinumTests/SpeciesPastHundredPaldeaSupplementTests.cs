using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D14, batch 9's supplement (Paldea: docs/plans/D14-proposals-paldea-supplement.md, approved by the user on
/// 2026-10-26, all 18 designs, every question at its default). What is new beside <see cref="SpeciesPastHundredTests"/>
/// and <see cref="SpeciesPastHundredPaldeaTests"/>: the fourteen paradox Pokémon at 600 by the hour, the Loyal Three to
/// 600 under their master Pecharunt, and Zerofin.
/// </summary>
public class SpeciesPastHundredPaldeaSupplementTests
{
    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static int Total(PokemonSpecies s) => s.BaseHP + s.BaseAttack + s.BaseDefense + s.BaseSpAttack + s.BaseSpDefense + s.BaseSpeed;

    private static string? LevelUp(Pokemon p, EvolutionContext context) => Evolution.Find(p, EvolutionTrigger.LevelUp, context)?.TargetSpecies;

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var m in members) party.Add(m);
        return party;
    }

    private static readonly (string Who, string Into, int Level, bool Night)[] Paradox =
    {
        ("Great Tusk", "Tuskendon", 250, true), ("Scream Tail", "Shriekpuff", 250, true), ("Brute Bonnet", "Fungoliath", 250, true),
        ("Flutter Mane", "Banshemane", 250, true), ("Slither Wing", "Saurmoth", 250, true), ("Sandy Shocks", "Lodestatic", 250, true),
        ("Iron Treads", "Treadnought", 250, false), ("Iron Bundle", "Cryoparcel", 250, false), ("Iron Hands", "Gauntletron", 250, false),
        ("Iron Jugulis", "Hydrajet", 250, false), ("Iron Moth", "Fusionmoth", 250, false), ("Iron Thorns", "Thornframe", 250, false),
        ("Roaring Moon", "Lunaroar", 254, true), ("Iron Valiant", "Paladroid", 254, false)
    };

    [Fact]
    public void TheEighteenAreNumberedAfterTheirForebearsAndMeetNothingTaken()
    {
        var numbers = new[] { ("Zerofin", 2964), ("Tuskendon", 2984), ("Thornframe", 2995), ("Lunaroar", 3005), ("Paladroid", 3006),
            ("Okidaimyo", 3014), ("Munkiregent", 3015), ("Fezantessa", 3016) };
        foreach (var (name, number) in numbers)
        {
            Assert.Equal(number, Species(name).DexNumber);
            Assert.Same(Species(name), PokemonDatabase.GetByDex(number));
            Assert.True(Species(name).IsOurs);
        }
        Assert.Equal(373, PokemonDatabase.Ours().Count);
        Assert.Equal(18, PokemonDatabase.Ours().Count(s => s.DexNumber is 2964 or (>= 2984 and <= 2995) or 3005 or 3006 or (>= 3014 and <= 3016)));
    }

    [Fact]
    public void TheParadoxPokemonStandAtSixHundredAndEvolveAtTheirHour()
    {
        // The ancient ones (Protosynthesis) at night, the future ones (Quark Drive) by day, 250 and 254 for the two
        // of the 590s; none is flagged legendary, so a paradox Pokémon's evolution may enter the Frontier
        foreach (var (who, into, level, night) in Paradox)
        {
            var from = Species(who);
            Assert.False(from.Legendary || from.Mythical);
            Assert.Equal(600, Total(Species(into)));
            Assert.Equal(from.Abilities, Species(into).Abilities);
            Assert.Equal(night ? "Protosynthesis" : "Quark Drive", Species(into).Abilities.Single());

            var p = Mon(who, level);
            Assert.Equal(into, LevelUp(p, new EvolutionContext { IsNight = night }));
            Assert.Null(LevelUp(p, new EvolutionContext { IsNight = !night }));
            Assert.Null(LevelUp(Mon(who, level - 1), new EvolutionContext { IsNight = night }));
            Assert.Equal(45, Species(into).CatchRate);
            Assert.True(FrontierRules.MayEnter(Mon(into, level)));
        }
    }

    [Fact]
    public void TheLoyalThreeNeedAPecharunt_AndStopAtTheirMastersSixHundred()
    {
        // Question 1's default: +45 each to 600, Pecharunt's own total, the first legendary set of ours under 660
        Assert.Equal(600, Total(Species("Pecharunt")));
        foreach (var (who, into) in new[] { ("Okidogi", "Okidaimyo"), ("Munkidori", "Munkiregent"), ("Fezandipiti", "Fezantessa") })
        {
            Assert.Equal(600, Total(Species(into)));
            Assert.Equal(3, Species(into).CatchRate);
            Assert.Equal(Species(who).HiddenAbility, Species(into).HiddenAbility);
            Assert.Equal(Species(who).Abilities, Species(into).Abilities);

            var p = Mon(who, 258);
            Assert.Null(LevelUp(p, new EvolutionContext()));
            Assert.Null(LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon("Okidogi", 50), Mon("Munkidori", 50)) }));
            Assert.Equal(into, LevelUp(p, new EvolutionContext { Party = PartyOf(p, Mon("Pecharunt", 50)) }));
            Assert.Null(LevelUp(Mon(who, 257), new EvolutionContext { Party = PartyOf(Mon("Pecharunt", 50)) }));
            Assert.True(FrontierRules.IsBannedOfOurs(Species(into)));
            Assert.False(FrontierRules.MayEnter(Mon(into, 258)));
        }
    }

    [Fact]
    public void ZerofinSwimsFasterAndNeedsWaveCrash()
    {
        // Question 4's and 5's defaults: Swift Swim in Zero to Hero's place, level 248 knowing Wave Crash; it is a
        // species of ours with no form, so Zero to Hero's rule (by the species Palafin) never reaches it
        var zerofin = Species("Zerofin");
        Assert.Equal(537, Total(zerofin));
        Assert.Equal(new[] { "Swift Swim" }, zerofin.Abilities);
        Assert.Null(zerofin.Forms);
        Assert.Equal(115, zerofin.BaseSpeed);

        var p = Mon("Palafin", 248);
        Assert.Null(LevelUp(p, new EvolutionContext()));
        p.Moves.Add(new Move(MoveDatabase.Get("Wave Crash")));
        Assert.Equal("Zerofin", LevelUp(p, new EvolutionContext()));
        Assert.Null(LevelUp(Mon("Palafin", 247, "Wave Crash"), new EvolutionContext()));

        // Palafin's Hero Form (a form of its own) evolves the same, and Zero to Hero stays Palafin's
        var hero = Mon("Palafin", 248, "Wave Crash");
        hero.ChangeForm("Palafin-Hero");
        Assert.Equal("Zerofin", LevelUp(hero, new EvolutionContext()));
        Assert.True(FrontierRules.MayEnter(Mon("Zerofin", 248)));
    }
}
