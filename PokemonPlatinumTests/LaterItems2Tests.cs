using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The later games' items R28a left (plan 06 · R28b): everything used on a Pokémon outside a battle (mints, Exp. Candies,
/// Ability Capsules and Patches, feathers, mochi, Legends: Arceus's candies, the Dynamax Candy, nectars, bottle caps, TRs,
/// the Prism Scale and the coins gathered for an evolution), and the held items still to run in a battle (the Ability
/// Shield, the Rusted Sword and Shield, Ogerpon's masks, the Legend Plate, Booster Energy as the sun goes, an Eject Pack
/// and a Mirror Herb after a Pokémon's coming in, Blunder Policy after a status move, an Air Balloon behind a
/// Substitute, Tough Claws under Protective Pads, the Strange Ball). One rule to a test, under the modern rules, the
/// rolls fixed by kind; docs/mechanics/rulings.md, "Items used outside a battle", has the rulings.
/// </summary>
public class LaterItems2Tests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static ItemData Item(string name) => ItemDatabase.Get(name) ?? throw new ArgumentException(name);

    private static BattleCore Duel(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, BattleWeather sky = BattleWeather.None, params Pokemon[] bench) =>
        Wild(mine, foe, rolls, Modern, sky, BattleTerrain.Plain, bench);

    private static Pokemon Fast(Pokemon p)
    {
        p.Speed = 999;
        return p;
    }

    private static Pokemon Slow(Pokemon p)
    {
        p.Speed = 1;
        return p;
    }

    private static int Stage(Pokemon p, StatType stat) => p.StatStages.GetValueOrDefault(stat);

    private static ItemUseResult Use(string item, Pokemon p) =>
        LaterItemUse.Handles(Item(item)) ? LaterItemUse.Apply(Item(item), p) : ItemUse.Apply(Item(item), p, 0, Modern);

    // ------------------------------------------------------------------ the data

    [Fact]
    public void TheImporterGivesTheItemsUsedOutsideABattleTheirUses()
    {
        Assert.Equal("Mint", Item("Adamant Mint").FieldUse);
        Assert.Equal(800, LaterItemUse.ExpOf(Item("Exp. Candy S")));
        Assert.Equal(1, Item("Health Feather").Use!["hpEVs"]);
        Assert.Equal(10, Item("Swift Mochi").Use!["speedEVs"]);
        Assert.Equal(25, Item("Smart Candy").Use!["spatkEVs"]);
        Assert.All(new[] { "Adamant Mint", "Exp. Candy XL", "Ability Capsule", "Ability Patch", "Dynamax Candy", "Red Nectar", "Clever Feather", "Fresh Start Mochi" },
            name => Assert.True(BagScreen.CanUse(Item(name)), name));
    }

    [Fact]
    public void TheTechnicalRecordsTeachTheirMovesToWhoeverLearnedThemInSwordAndShield()
    {
        var tr = Item("TR00");
        Assert.Equal(("Swords Dance", ItemPocket.TMsAndHMs), (tr.TeachesMove, tr.Pocket));
        Assert.True(MoveTeaching.IsMachine(tr));
        Assert.False(MoveTeaching.IsHm(tr)); // used up as it teaches, as Sword and Shield's are
        var learner = PokemonDatabase.GetAll().First(s => s.TmMoves?.Contains("TR00") == true);
        Assert.Equal(TeachAnswer.Able, MoveTeaching.Answer(Mon(learner.Name, 50, "Tackle"), tr));
        Assert.Equal(TeachAnswer.NotAble, MoveTeaching.Answer(Mon("Magikarp", 50, "Splash"), tr));
    }

    [Fact]
    public void TheBottleCapsAreInTheData()
    {
        Assert.True(HyperTraining.IsCap(Item("Bottle Cap")));
        Assert.True(HyperTraining.IsCap(Item("Gold Bottle Cap")));
    }

    // ------------------------------------------------------------------ mints

    [Fact]
    public void AMintMakesTheStatsGrowAsAnotherNaturesWould() // Adamant: a tenth more Attack, a tenth less Sp. Atk
    {
        var p = Mon("Machamp", 50);
        int attack = p.Attack, spAttack = p.SpAttack;
        Assert.True(Use("Adamant Mint", p).Applied);
        Assert.Equal((Nature.Hardy, Nature.Adamant), (p.Nature, p.MintNature));
        Assert.Equal((attack * 11 / 10, spAttack * 9 / 10), (p.Attack, p.SpAttack));
    }

    [Fact]
    public void AMintOfTheNatureItsStatsHaveAlreadyDoesNothing()
    {
        var p = Mon("Machamp", 50);
        Use("Adamant Mint", p);
        Assert.False(LaterItemUse.WouldHelp(Item("Adamant Mint"), p));
    }

    [Fact]
    public void AMintOfItsOwnNatureTakesTheLastMintAway()
    {
        var p = Mon("Machamp", 50);
        p.Nature = Nature.Modest;
        p.RecalculateStats();
        int attack = p.Attack;
        Use("Adamant Mint", p);
        Use("Modest Mint", p);
        Assert.Null(p.MintNature);
        Assert.Equal(attack, p.Attack);
    }

    // ------------------------------------------------------------------ Exp. Candies

    [Fact]
    public void AnExpCandyGivesItsPointsAndTheLevelsTheyReach() // Medium Slow: 135 points at level 5, 935 is level 11
    {
        var p = Mon("Bulbasaur", 5);
        var result = Use("Exp. Candy S", p);
        Assert.Equal((935, 11, (int?)11), (p.CurrentExp, p.Level, result.NewLevel));
    }

    [Fact]
    public void AnExpCandyDoesNothingAtLevelOneHundred()
    {
        Assert.False(LaterItemUse.WouldHelp(Item("Exp. Candy XS"), Mon("Bulbasaur", 100)));
    }

    // ------------------------------------------------------------------ abilities

    [Fact]
    public void AnAbilityCapsuleSwitchesBetweenItsTwoAbilities()
    {
        var p = Mon("Geodude", 30);
        Assert.Equal("Rock Head", p.AbilityName);
        Assert.True(Use("Ability Capsule", p).Applied);
        Assert.Equal("Sturdy", p.AbilityName);
    }

    [Fact]
    public void AnAbilityCapsuleDoesNothingForASpeciesWithOneAbility()
    {
        Assert.False(LaterItemUse.WouldHelp(Item("Ability Capsule"), Mon("Pikachu", 30)));
    }

    [Fact]
    public void AnAbilityCapsuleDoesNothingForAHiddenAbility()
    {
        var p = Mon("Geodude", 30);
        p.AbilityName = "Sand Veil";
        Assert.False(LaterItemUse.WouldHelp(Item("Ability Capsule"), p));
    }

    [Fact]
    public void AnAbilityPatchGivesTheHiddenAbilityAndTakesItBack() // Scarlet and Violet's way back
    {
        var p = Mon("Bulbasaur", 30);
        Use("Ability Patch", p);
        Assert.Equal("Chlorophyll", p.AbilityName);
        Use("Ability Patch", p);
        Assert.Equal("Overgrow", p.AbilityName);
    }

    [Fact]
    public void AHiddenAbilityStaysHiddenThroughAnEvolution()
    {
        var p = Mon("Bulbasaur", 30);
        p.AbilityName = "Chlorophyll";
        p.EvolveInto(PokemonDatabase.Get("Ivysaur")!);
        Assert.Equal("Chlorophyll", p.AbilityName);
    }

    // ------------------------------------------------------------------ effort

    [Fact]
    public void AFeatherGivesOnePointOfEffort()
    {
        var p = Mon("Machamp", 50);
        Assert.True(Use("Muscle Feather", p).Applied);
        Assert.Equal(1, p.EvAttack);
    }

    [Fact]
    public void AFeatherDoesNothingOnAStatAtTheMost() // 252 under the modern rules
    {
        var p = Mon("Machamp", 50);
        p.EvAttack = 252;
        Assert.False(ItemUse.WouldHelp(Item("Muscle Feather"), p, 0, Modern));
    }

    [Fact]
    public void AMochiGivesTenPointsOfEffort()
    {
        var p = Mon("Machamp", 50);
        Use("Swift Mochi", p);
        Assert.Equal(10, p.EvSpeed);
    }

    [Fact]
    public void ACandyOfLegendsArceusGivesItsEffortLevel() // our ruling: 25 points a level, 50 for an L and 75 for an XL
    {
        var p = Mon("Machamp", 50);
        Use("Smart Candy", p);
        Use("Courage Candy XL", p);
        Assert.Equal((25, 75), (p.EvSpAttack, p.EvSpDefense));
    }

    [Fact]
    public void AFreshStartMochiTakesAllTheEffortAway()
    {
        var p = Mon("Machamp", 50);
        p.EvHP = 100;
        p.EvSpeed = 50;
        p.RecalculateStats();
        var result = Use("Fresh Start Mochi", p);
        Assert.True(result.Applied);
        Assert.Equal(0, EffortRules.Total(p));
        Assert.Contains("lost all the effort", result.Message);
    }

    // ------------------------------------------------------------------ the Dynamax Candy and the nectars

    [Fact]
    public void ADynamaxCandyRaisesTheDynamaxLevelToTen()
    {
        var p = Mon("Pikachu", 30);
        Use("Dynamax Candy", p);
        Assert.Equal(1, p.DynamaxLevel);
        p.DynamaxLevel = 10;
        Assert.False(LaterItemUse.WouldHelp(Item("Dynamax Candy"), p));
        Assert.False(LaterItemUse.WouldHelp(Item("Dynamax Candy"), Mon("Zacian", 70)));
    }

    [Fact]
    public void ANectarChangesOricoriosStyle()
    {
        var p = Mon("Oricorio", 30);
        Use("Yellow Nectar", p);
        Assert.Equal("Oricorio-Pom-Pom", p.Form);
        Assert.False(LaterItemUse.WouldHelp(Item("Yellow Nectar"), p));
        Use("Red Nectar", p);
        Assert.Null(p.Form);
        Assert.False(LaterItemUse.WouldHelp(Item("Pink Nectar"), Mon("Pikachu", 30)));
    }

    // ------------------------------------------------------------------ hyper training

    [Fact]
    public void ABottleCapTrainsOneStatAsIfItsIvWere31() // (2 × 130 + 31) × 50 / 100 + 5 = 150; the IV stays 0
    {
        var p = Mon("Machamp", 50);
        Assert.True(HyperTraining.Train(Item("Bottle Cap"), p, StatType.Attack));
        Assert.Equal((150, 0), (p.Attack, p.IvAttack));
        Assert.False(HyperTraining.WouldHelp(Item("Bottle Cap"), p, StatType.Attack));
    }

    [Fact]
    public void HyperTrainingWaitsForLevelFifty() // Scarlet and Violet's
    {
        Assert.False(HyperTraining.WouldHelp(Item("Bottle Cap"), Mon("Machamp", 49), StatType.Attack));
    }

    [Fact]
    public void AGoldBottleCapTrainsEveryStat() // HP: (2 × 90 + 31) × 50 / 100 + 50 + 10 = 165
    {
        var p = Mon("Machamp", 50);
        HyperTraining.Train(Item("Gold Bottle Cap"), p);
        Assert.Equal(HyperTraining.All, p.HyperTrained);
        Assert.Equal(165, p.MaxHP);
    }

    [Fact]
    public void AMintAndHyperTrainingAreSaved()
    {
        var p = Mon("Machamp", 50);
        Use("Jolly Mint", p);
        HyperTraining.Train(Item("Bottle Cap"), p, StatType.Speed);
        var back = SavedPokemonData.FromPokemon(p).ToPokemon(Modern);
        Assert.Equal(((Nature?)Nature.Jolly, p.HyperTrained, p.Speed), (back.MintNature, back.HyperTrained, back.Speed));
    }

    // ------------------------------------------------------------------ evolutions

    [Fact]
    public void FeebasTradedHoldingAPrismScaleEvolves()
    {
        var p = With(Mon("Feebas", 30), item: "Prism Scale");
        Assert.Equal("Milotic", Evolution.Find(p, EvolutionTrigger.Trade, new EvolutionContext())?.TargetSpecies);
        Assert.True(Evolution.IsHeldForEvolution(Item("Prism Scale")));
    }

    [Fact]
    public void TheCoinsAndCandiesGatheredForAnEvolutionAreTold()
    {
        Assert.True(Evolution.IsGatheredForEvolution(Item("Gimmighoul Coin")));
        Assert.True(Evolution.IsGatheredForEvolution(Item("Meltan Candy")));
        Assert.False(Evolution.IsGatheredForEvolution(Item("Fire Stone")));
    }

    // ------------------------------------------------------------------ the Ability Shield

    [Fact]
    public void AnAbilityShieldKeepsWorrySeedOff()
    {
        var foe = With(Mon("Snorlax", 50), "Immunity", "Ability Shield");
        var said = Turn(Duel(Fast(Mon("Breloom", 50, "Worry Seed")), foe), 0, 0);
        Assert.Equal("Immunity", foe.AbilityName);
        Assert.Contains(said, l => l.Contains("is guarded by its Ability Shield"));
    }

    [Fact]
    public void AnAbilityShieldKeepsGastroAcidOff()
    {
        var foe = With(Mon("Snorlax", 50), "Immunity", "Ability Shield");
        var core = Duel(Fast(Mon("Muk", 50, "Gastro Acid")), foe);
        Turn(core, 0, 0);
        Assert.False(core.EnemySlots[0].Volatile.AbilitySuppressed);
    }

    [Fact]
    public void AnAbilityShieldKeepsMoldBreakerFromItsAbility()
    {
        var foe = With(Mon("Bronzong", 50), "Levitate", "Ability Shield");
        Turn(Duel(With(Mon("Excadrill", 50, "Earthquake"), "Mold Breaker"), foe), 0, 0);
        Assert.Equal(foe.MaxHP, foe.CurrentHP);
    }

    // ------------------------------------------------------------------ the Rusted Sword and Shield

    [Fact]
    public void ZacianWithTheRustedSwordIsCrownedAndItsIronHeadIsBehemothBlade()
    {
        var mine = With(Mon("Zacian", 70, "Iron Head"), "Intrepid Sword", "Rusted Sword");
        var core = Duel(mine, Mon("Snorlax", 50), bench: Mon("Pikachu", 50, "Tackle"));
        Assert.Equal(("Zacian-Crowned", "Behemoth Blade"), (mine.Form, mine.Moves[0].Name));
        SwitchTo(core, 1);
        Assert.Equal(((string?)null, "Iron Head"), (mine.Form, mine.Moves[0].Name));
    }

    [Fact]
    public void ZamazentaWithTheRustedShieldIsCrowned()
    {
        var mine = With(Mon("Zamazenta", 70, "Iron Head"), "Dauntless Shield", "Rusted Shield");
        Duel(mine, Mon("Snorlax", 50));
        Assert.Equal(("Zamazenta-Crowned", "Behemoth Bash"), (mine.Form, mine.Moves[0].Name));
    }

    // ------------------------------------------------------------------ Ogerpon's masks and the Legend Plate

    private static int Took(Pokemon mine, Pokemon foe)
    {
        var core = Duel(mine, foe);
        int hp = foe.CurrentHP;
        Turn(core, 0, 0);
        return hp - foe.CurrentHP;
    }

    [Fact]
    public void OgerponsMaskMakesItsMovesAFifthStronger() // 4915/4096
    {
        Pokemon Masked(string? item)
        {
            var p = Mon("Ogerpon", 50, "Tackle");
            p.ChangeForm("Ogerpon-Wellspring-Mask");
            return item == null ? p : With(p, item: item);
        }
        int with = Took(Masked("Wellspring Mask"), Mon("Snorlax", 50));
        int without = Took(Masked(null), Mon("Snorlax", 50));
        Assert.InRange(with / (double)without, 1.16, 1.24);
    }

    [Fact]
    public void OgerponWearsTheMaskItHolds()
    {
        var p = With(Mon("Ogerpon", 50), item: "Hearthflame Mask");
        Assert.True(FormRules.ByHeldItem(p));
        Assert.Equal("Ogerpon-Hearthflame-Mask", p.Form);
    }

    [Fact]
    public void TheLegendPlateGivesJudgmentTheTypeThatDoesTheMost() // Rock against Charizard, four times
    {
        var said = Turn(Duel(With(Mon("Arceus", 70, "Judgment"), item: "Legend Plate"), Mon("Charizard", 50)), 0, 0);
        Assert.Contains("It's super effective!", said);
    }

    // ------------------------------------------------------------------ Booster Energy, an Eject Pack and a Mirror Herb

    [Fact]
    public void BoosterEnergyGoesOffAsTheSunGoes()
    {
        var mine = With(Mon("Great Tusk", 50, "Tackle"), "Protosynthesis", "Booster Energy");
        var core = Duel(Slow(mine), Fast(Mon("Politoed", 50, "Rain Dance")), sky: BattleWeather.Sun);
        Assert.NotNull(mine.HeldItem);
        Turn(core, 0, 0);
        Assert.Null(mine.HeldItem);
        Assert.NotNull(LaterAbilities.ParadoxStat(core.PlayerSlots[0]));
    }

    [Fact]
    public void AnEjectPackSwitchesItsHolderOutAfterAnIntimidate()
    {
        var mine = With(Mon("Snorlax", 50, "Tackle"), item: "Eject Pack");
        var bench = Mon("Pikachu", 50, "Tackle");
        var core = Duel(mine, With(Mon("Gyarados", 50), "Intimidate"), bench: bench);
        Assert.Contains(Lines(core.TakeLog()), l => l.Contains("is switched out by the Eject Pack"));
        SendIn(core, 1);
        Assert.Equal(bench, core.PlayerSlots[0].Pokemon);
    }

    [Fact]
    public void AMirrorHerbCopiesAStageAFoeTookAsItCameIn() // Intrepid Sword's stage of Attack
    {
        var foe = With(Mon("Snorlax", 50), item: "Mirror Herb");
        Duel(With(Mon("Zacian", 70, "Tackle"), "Intrepid Sword"), foe);
        Assert.Equal(1, Stage(foe, StatType.Attack));
        Assert.Null(foe.HeldItem);
    }

    // ------------------------------------------------------------------ R28a's cut-downs made whole

    [Fact]
    public void ABlunderPolicyGoesOffAfterAStatusMoveMisses()
    {
        var mine = With(Mon("Gengar", 50, "Hypnosis"), item: "Blunder Policy");
        Turn(Duel(mine, Mon("Snorlax", 50), Calm().Force(RollKind.Accuracy, 99)), 0, 0);
        Assert.Equal(2, Stage(mine, StatType.Speed));
    }

    [Fact]
    public void AnAirBalloonPopsBehindASubstitute()
    {
        var foe = Fast(With(Mon("Snorlax", 50, "Substitute"), item: "Air Balloon"));
        var core = Duel(Slow(Mon("Machamp", 50, "Tackle")), foe);
        var said = Turn(core, 0, 0);
        Assert.True(core.EnemySlots[0].HasSubstitute);
        Assert.Null(foe.HeldItem);
        Assert.Contains(said, l => l.Contains("Air Balloon"));
    }

    [Fact]
    public void ToughClawsKeepsItsTouchUnderProtectivePads() // 5325/4096 either way
    {
        int padded = Took(With(Mon("Machamp", 50, "Tackle"), "Tough Claws", "Protective Pads"), Mon("Snorlax", 50));
        int bare = Took(With(Mon("Machamp", 50, "Tackle"), "Tough Claws"), Mon("Snorlax", 50));
        Assert.Equal(bare, padded);
    }

    [Fact]
    public void TheStrangeBallCatchesAsAPokeBall()
    {
        Assert.True(CatchCalculator.KnowsBall("Strange Ball"));
    }
}

