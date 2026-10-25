using DataImporter;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D14, batch 6's supplement (docs/plans/D14-proposals-kalos-supplement.md, approved by the user on
/// 2026-10-26 with every question at its default): Ecosentinel from Zygarde, 660 at level 205 knowing Core Enforcer, any
/// forme evolving. <see cref="SpeciesPastHundredTests"/> holds the design to the shared rules.
/// </summary>
public class SpeciesPastHundredKalosSupplementTests
{
    private static PokemonSpecies Species(string name) => PokemonDatabase.Get(name)!;

    private static string? LevelUp(Pokemon p) => Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext())?.TargetSpecies;

    private static Pokemon Zygarde(int level, string? form = null, params string[] moves)
    {
        var p = Mon("Zygarde", level, moves);
        p.ChangeForm(form);
        return p;
    }

    [Fact]
    public void EcosentinelIsTheGuardiansDesign()
    {
        var s = Species("Ecosentinel");
        Assert.True(s.IsOurs);
        Assert.Equal(2718, s.DexNumber);
        Assert.Equal((PokemonType.Dragon, (PokemonType?)PokemonType.Ground), (s.PrimaryType, s.SecondaryType));
        Assert.Equal(new[] { "Aura Break" }, s.Abilities);
        Assert.Equal((118, 110, 131, 81, 110, 110), (s.BaseHP, s.BaseAttack, s.BaseDefense, s.BaseSpAttack, s.BaseSpDefense, s.BaseSpeed));
        Assert.Equal(660, s.BaseHP + s.BaseAttack + s.BaseDefense + s.BaseSpAttack + s.BaseSpDefense + s.BaseSpeed);
        Assert.Equal((6.2f, 720.0f), ((float)s.Height, (float)s.Weight));
        Assert.Equal("Rare", DexEntries.HabitatOf("Ecosentinel").ToString());
    }

    [Fact]
    public void ZygardeBecomesItAt205KnowingCoreEnforcerAndNotBefore()
    {
        Assert.Equal("Ecosentinel", LevelUp(Zygarde(205, null, "Core Enforcer")));
        Assert.Null(LevelUp(Zygarde(204, null, "Core Enforcer")));
        Assert.Null(LevelUp(Zygarde(205, null, "Land's Wrath")));
    }

    [Fact]
    public void AnyFormeEvolvesWithNoFormLeftOverAndTakesAuraBreak()
    {
        foreach (var forme in new[] { null, "Zygarde-10", "Zygarde-10-Power-Construct", "Zygarde-50-Power-Construct" })
        {
            var p = Zygarde(205, forme, "Core Enforcer");
            var found = Evolution.Find(p, EvolutionTrigger.LevelUp, new EvolutionContext());
            Assert.NotNull(found);
            Evolution.Evolve(p, found!, new EvolutionContext());
            Assert.Equal(("Ecosentinel", (string?)null, "Ecosentinel", "Aura Break"), (p.Species.Name, p.Form, p.ModelName, p.AbilityName));
        }
    }

    [Fact]
    public void EcosentinelMayNotEnterTheBattleFrontier()
    {
        Assert.False(FrontierRules.MayEnter(Mon("Ecosentinel", 205)));
    }
}
