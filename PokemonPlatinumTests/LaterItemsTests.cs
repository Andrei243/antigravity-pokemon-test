using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The later games' held items, medicine and balls (plan 06 · R28a), each by Pokémon Showdown's rule (data/items.ts at
/// the pinned commit) or, for the balls, the newest games' own bonuses. One rule to a test, under the modern rules, the
/// rolls fixed by kind (<see cref="CoreScenario.Calm"/>). A bonus is held by the damage it makes beside the same hit
/// without the item, worked out from Showdown's 4096ths; a foe made by <c>Mon</c> with no moves knows only Idle.
/// </summary>
public class LaterItemsTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

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

    /// <summary>What the foe lost to the player's first move in one turn, the foe sitting it out.</summary>
    private static int Took(Pokemon mine, Pokemon foe, BattleWeather sky = BattleWeather.None)
    {
        var core = Duel(mine, foe, sky: sky);
        core.TakeLog();
        int hp = foe.CurrentHP;
        Turn(core, 0, 0);
        return hp - foe.CurrentHP;
    }

    private static void Near(double expected, int with, int without) =>
        Assert.InRange(with / (double)without, expected - 0.04, expected + 0.04);

    // ------------------------------------------------------------------ the data

    [Fact]
    public void TheImporterGivesTheLaterItemsTheirHoldEffects()
    {
        Assert.Equal("WeaknessPolicy", ItemDatabase.Get("Weakness Policy")!.HoldEffect);
        Assert.Equal(("RockyHelmet", 6), (ItemDatabase.Get("Rocky Helmet")!.HoldEffect, ItemDatabase.Get("Rocky Helmet")!.HoldParam));
        Assert.Equal("GemFire", ItemDatabase.Get("Fire Gem")!.HoldEffect);
        Assert.Equal("WeakenSeFairy", ItemDatabase.Get("Roseli Berry")!.HoldEffect);
        Assert.Equal(80, ItemDatabase.Get("Weakness Policy")!.FlingPower);   // Showdown's fling
        Assert.All(new[] { "Eviolite", "Air Balloon", "Eject Button", "Booster Energy", "Covert Cloak", "Kee Berry" },
            name => Assert.True(HeldItemEffects.IsHoldable(ItemDatabase.Get(name)!), name));
    }

    [Fact]
    public void JohtosBallsAreInTheData()
    {
        foreach (var name in new[] { "Fast Ball", "Level Ball", "Lure Ball", "Heavy Ball", "Love Ball", "Friend Ball", "Moon Ball", "Sport Ball", "Park Ball" })
            Assert.Equal(ItemPocket.PokeBalls, ItemDatabase.Get(name)?.Pocket);
    }

    // ------------------------------------------------------------------ the damage formula

    [Fact]
    public void AnEvioliteHardensAPokemonThatCanStillEvolve() // 6144/4096 on Defense
    {
        int with = Took(Mon("Machamp", 50, "Tackle"), With(Mon("Chansey", 50), item: "Eviolite"));
        int without = Took(Mon("Machamp", 50, "Tackle"), Mon("Chansey", 50));
        Near(1 / 1.5, with, without);
    }

    [Fact]
    public void AnEvioliteDoesNothingForAPokemonFullyEvolved()
    {
        int with = Took(Mon("Machamp", 50, "Tackle"), With(Mon("Blissey", 50), item: "Eviolite"));
        int without = Took(Mon("Machamp", 50, "Tackle"), Mon("Blissey", 50));
        Assert.Equal(without, with);
    }

    [Fact]
    public void AnAssaultVestHardensItsHolderAgainstSpecialMovesAlone()
    {
        Near(1 / 1.5, Took(Mon("Alakazam", 50, "Psychic"), With(Mon("Snorlax", 50), item: "Assault Vest")), Took(Mon("Alakazam", 50, "Psychic"), Mon("Snorlax", 50)));
        Assert.Equal(Took(Mon("Machamp", 50, "Tackle"), Mon("Snorlax", 50)), Took(Mon("Machamp", 50, "Tackle"), With(Mon("Snorlax", 50), item: "Assault Vest")));
    }

    [Fact]
    public void AGemMakesItsTypesFirstMoveStrongerAndIsUsedUp() // 5325/4096
    {
        var mine = With(Mon("Arcanine", 50, "Flamethrower"), item: "Fire Gem");
        int with = Took(mine, Mon("Snorlax", 50));
        int without = Took(Mon("Arcanine", 50, "Flamethrower"), Mon("Snorlax", 50));
        Near(1.3, with, without);
        Assert.Null(mine.HeldItem);
    }

    [Fact]
    public void AGemOfAnotherTypeStaysPut()
    {
        var mine = With(Mon("Arcanine", 50, "Tackle"), item: "Fire Gem");
        Took(mine, Mon("Snorlax", 50));
        Assert.Equal("Fire Gem", mine.HeldItem?.Name);
    }

    [Fact]
    public void APunchingGloveMakesAPunchATenthStronger() // 4506/4096
    {
        Near(1.1, Took(With(Mon("Machamp", 50, "Mach Punch"), item: "Punching Glove"), Mon("Snorlax", 50)), Took(Mon("Machamp", 50, "Mach Punch"), Mon("Snorlax", 50)));
    }

    [Fact]
    public void APixiePlateMakesAFairyMoveAFifthStronger()
    {
        Near(1.2, Took(With(Mon("Clefable", 50, "Moonblast"), item: "Pixie Plate"), Mon("Snorlax", 50)), Took(Mon("Clefable", 50, "Moonblast"), Mon("Snorlax", 50)));
    }

    [Fact]
    public void AUtilityUmbrellaKeepsTheRainOffTheHitsItsHolderTakes()
    {
        int dry = Took(Mon("Blastoise", 50, "Water Gun"), Mon("Snorlax", 50));
        int sheltered = Took(Mon("Blastoise", 50, "Water Gun"), With(Mon("Snorlax", 50), item: "Utility Umbrella"), BattleWeather.Rain);
        int wet = Took(Mon("Blastoise", 50, "Water Gun"), Mon("Snorlax", 50), BattleWeather.Rain);
        Assert.Equal(dry, sheltered);
        Assert.True(wet > dry);
    }

    [Fact]
    public void ARingTargetTakesAGhostsImmunityAway()
    {
        Assert.Equal(0, Took(Mon("Snorlax", 50, "Tackle"), Mon("Gengar", 50)));
        Assert.True(Took(Mon("Snorlax", 50, "Tackle"), With(Mon("Gengar", 50), item: "Ring Target")) > 0);
    }

    [Fact]
    public void ARoseliBerryHalvesASuperEffectiveFairyHitAndIsEaten()
    {
        var foe = With(Mon("Dragonite", 50), item: "Roseli Berry");
        int with = Took(Mon("Clefable", 50, "Moonblast"), foe);
        int without = Took(Mon("Clefable", 50, "Moonblast"), Mon("Dragonite", 50));
        Near(0.5, with, without);
        Assert.Null(foe.HeldItem);
    }

    [Fact]
    public void AFloatStoneHalvesItsHoldersWeight() // Gyarados's 235 kg is 117.5: Grass Knot's 120 becomes 100
    {
        Near(100 / 120.0, Took(Mon("Venusaur", 50, "Grass Knot"), With(Mon("Gyarados", 50), item: "Float Stone")), Took(Mon("Venusaur", 50, "Grass Knot"), Mon("Gyarados", 50)));
    }

    // ------------------------------------------------------------------ a hit answered

    [Fact]
    public void ARockyHelmetCostsWhoeverTouchesItsHolderASixth()
    {
        var mine = Mon("Machamp", 50, "Tackle");
        var core = Duel(mine, With(Mon("Snorlax", 50), item: "Rocky Helmet"));
        Turn(core, 0, 0);
        Assert.Equal(mine.MaxHP - mine.MaxHP / 6, mine.CurrentHP);
    }

    [Fact]
    public void ARockyHelmetLeavesAMoveThatDoesntTouchAlone()
    {
        var mine = Mon("Machamp", 50, "Rock Slide");
        var core = Duel(mine, With(Mon("Snorlax", 50), item: "Rocky Helmet"));
        Turn(core, 0, 0);
        Assert.Equal(mine.MaxHP, mine.CurrentHP);
    }

    [Fact]
    public void ProtectivePadsKeepARockyHelmetOff()
    {
        var mine = With(Mon("Machamp", 50, "Tackle"), item: "Protective Pads");
        var core = Duel(mine, With(Mon("Snorlax", 50), item: "Rocky Helmet"));
        Turn(core, 0, 0);
        Assert.Equal(mine.MaxHP, mine.CurrentHP);
    }

    [Fact]
    public void APunchingGloveTakesTheTouchOutOfAPunch()
    {
        var mine = With(Mon("Machamp", 50, "Mach Punch"), item: "Punching Glove");
        var core = Duel(mine, With(Mon("Snorlax", 50), item: "Rocky Helmet"));
        Turn(core, 0, 0);
        Assert.Equal(mine.MaxHP, mine.CurrentHP);
    }

    [Fact]
    public void AWeaknessPolicyRaisesAttackAndSpAttackTwoStagesAfterASuperEffectiveHit()
    {
        var foe = With(Mon("Snorlax", 50), item: "Weakness Policy");
        var core = Duel(Mon("Machamp", 50, "Karate Chop"), foe);
        var said = Turn(core, 0, 0);
        Assert.Equal((2, 2), (Stage(foe, StatType.Attack), Stage(foe, StatType.SpAttack)));
        Assert.Null(foe.HeldItem);
        Assert.Contains(said, l => l.Contains("used its Weakness Policy"));
    }

    [Fact]
    public void AWeaknessPolicyWaitsForASuperEffectiveHit()
    {
        var foe = With(Mon("Snorlax", 50), item: "Weakness Policy");
        Turn(Duel(Mon("Machamp", 50, "Tackle"), foe), 0, 0);
        Assert.Equal("Weakness Policy", foe.HeldItem?.Name);
    }

    [Theory]
    [InlineData("Absorb Bulb", "Water Gun", StatType.SpAttack)]
    [InlineData("Luminous Moss", "Water Gun", StatType.SpDefense)]
    [InlineData("Cell Battery", "Thunder Shock", StatType.Attack)]
    [InlineData("Snowball", "Powder Snow", StatType.Attack)]
    public void AnItemRaisesAStatWhenAHitOfItsTypeLands(string item, string move, StatType stat)
    {
        var foe = With(Mon("Snorlax", 50), item: item);
        Turn(Duel(Mon("Lapras", 50, move), foe), 0, 0);
        Assert.Equal(1, Stage(foe, stat));
        Assert.Null(foe.HeldItem);
    }

    [Fact]
    public void AKeeBerryIsEatenForAStageOfDefenseAfterAPhysicalHit()
    {
        var foe = With(Mon("Snorlax", 50), item: "Kee Berry");
        Turn(Duel(Mon("Machamp", 50, "Tackle"), foe), 0, 0);
        Assert.Equal(1, Stage(foe, StatType.Defense));
        Assert.Null(foe.HeldItem);
    }

    [Fact]
    public void AMarangaBerryWaitsForASpecialHit()
    {
        var foe = With(Mon("Snorlax", 50), item: "Maranga Berry");
        Turn(Duel(Mon("Machamp", 50, "Tackle"), foe), 0, 0);
        Assert.Equal("Maranga Berry", foe.HeldItem?.Name);
        Turn(Duel(Mon("Alakazam", 50, "Psychic"), foe), 0, 0);
        Assert.Equal(1, Stage(foe, StatType.SpDefense));
    }

    [Fact]
    public void AnAirBalloonKeepsGroundMovesOffUntilAHitPopsIt()
    {
        var foe = With(Mon("Snorlax", 50), item: "Air Balloon");
        var core = Duel(Mon("Rhydon", 50, "Earthquake", "Tackle"), foe);
        Assert.Contains(Lines(core.Log), l => l.Contains("floats in the air with its Air Balloon"));
        int hp = foe.CurrentHP;
        Turn(core, 0, 0);
        Assert.Equal(hp, foe.CurrentHP);
        Turn(core, 1, 0);
        Assert.Null(foe.HeldItem);
        Assert.True(foe.CurrentHP < hp);
    }

    // ------------------------------------------------------------------ its holder's own

    [Fact]
    public void CovertCloakKeepsASideEffectOff() // Nuzzle paralyses whoever it hits
    {
        var foe = With(Mon("Snorlax", 50), item: "Covert Cloak");
        Turn(Duel(Mon("Pikachu", 50, "Nuzzle"), foe), 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
    }

    [Fact]
    public void AClearAmuletKeepsAFoesDropOff()
    {
        var foe = With(Mon("Snorlax", 50), item: "Clear Amulet");
        Turn(Duel(Mon("Pikachu", 50, "Growl"), foe), 0, 0);
        Assert.Equal(0, Stage(foe, StatType.Attack));
    }

    [Fact]
    public void SafetyGogglesKeepAPowderOff()
    {
        var foe = With(Mon("Snorlax", 50), item: "Safety Goggles");
        Turn(Duel(Mon("Parasect", 50, "Spore"), foe), 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
    }

    [Fact]
    public void SafetyGogglesKeepASandstormOff()
    {
        var foe = With(Mon("Snorlax", 50), item: "Safety Goggles");
        var core = Duel(Mon("Tyranitar", 50, "Sandstorm"), foe);
        Turn(core, 0, 0);
        Assert.Equal(foe.MaxHP, foe.CurrentHP);
    }

    [Fact]
    public void LoadedDiceMakeAMoveOfSeveralHitsHitFourOrFiveTimes()
    {
        for (uint seed = 1; seed <= 6; seed++)
        {
            var core = Duel(With(Mon("Cloyster", 50, "Icicle Spear"), item: "Loaded Dice"), Mon("Snorlax", 50), Steady(seed).Force(RollKind.SideEffect, 99));
            var said = Turn(core, 0, 0);
            Assert.True(said.Any(l => l is "Hit 4 times!" or "Hit 5 times!"), string.Join(" | ", said));
        }
    }

    [Fact]
    public void ABindingBandMakesABindTakeASixth()
    {
        var foe = Mon("Snorlax", 50);
        var core = Duel(Fast(Idling(With(Mon("Arbok", 50, "Wrap"), item: "Binding Band"))), foe);
        Turn(core, 0, 0);
        int before = foe.CurrentHP;
        Turn(core, 1, 0);
        // A turn's end: a sixth (Showdown's boundDivisor), where the rules' own is an eighth
        Assert.Equal(foe.MaxHP / 6, before - foe.CurrentHP);
    }

    [Fact]
    public void ThroatSprayRaisesSpAttackAfterASoundMove()
    {
        var mine = With(Mon("Exploud", 50, "Growl"), item: "Throat Spray");
        Turn(Duel(mine, Mon("Snorlax", 50)), 0, 0);
        Assert.Equal(1, Stage(mine, StatType.SpAttack));
        Assert.Null(mine.HeldItem);
    }

    [Fact]
    public void ABlunderPolicyRaisesSpeedTwoStagesAfterAMiss()
    {
        var mine = With(Mon("Blastoise", 50, "Hydro Pump"), item: "Blunder Policy");
        Turn(Duel(mine, Mon("Snorlax", 50), Calm().Force(RollKind.Accuracy, 99)), 0, 0);
        Assert.Equal(2, Stage(mine, StatType.Speed));
        Assert.Null(mine.HeldItem);
    }

    [Fact]
    public void RoomServiceLowersSpeedOnceTrickRoomIsTwisted()
    {
        var foe = With(Mon("Snorlax", 50), item: "Room Service");
        Turn(Duel(Mon("Bronzong", 50, "Trick Room"), foe), 0, 0);
        Assert.Equal(-1, Stage(foe, StatType.Speed));
    }

    [Fact]
    public void AnAdrenalineOrbAnswersAnIntimidate()
    {
        var mine = With(Mon("Snorlax", 50, "Tackle"), item: "Adrenaline Orb");
        Duel(mine, With(Mon("Gyarados", 50), "Intimidate"));
        Assert.Equal((-1, 1), (Stage(mine, StatType.Attack), Stage(mine, StatType.Speed)));
        Assert.Null(mine.HeldItem);
    }

    [Fact]
    public void AnElectricSeedRaisesDefenseOnElectricTerrain()
    {
        var foe = With(Mon("Snorlax", 50), item: "Electric Seed");
        Turn(Duel(Mon("Pikachu", 50, "Electric Terrain"), foe), 0, 0);
        Assert.Equal(1, Stage(foe, StatType.Defense));
    }

    [Fact]
    public void BoosterEnergySetsQuarkDriveGoingWithNoTerrain()
    {
        var mine = With(Mon("Iron Bundle", 50, "Tackle"), "Quark Drive", "Booster Energy");
        var core = Duel(mine, Mon("Snorlax", 50));
        Assert.Null(mine.HeldItem);
        Assert.NotNull(LaterAbilities.ParadoxStat(core.PlayerSlots[0]));
    }

    [Fact]
    public void AMirrorHerbCopiesAFoesRaise()
    {
        var foe = With(Mon("Snorlax", 50), item: "Mirror Herb");
        Turn(Duel(Mon("Scizor", 50, "Swords Dance"), foe), 0, 0);
        Assert.Equal(2, Stage(foe, StatType.Attack));
        Assert.Null(foe.HeldItem);
    }

    [Fact]
    public void HeavyDutyBootsWalkOverStealthRock()
    {
        var bench = With(Mon("Charizard", 50, "Tackle"), item: "Heavy-Duty Boots");
        var core = Duel(Mon("Snorlax", 50, "Tackle"), Mon("Snorlax", 50), bench: bench);
        core.Field.Side(BattleSide.Player).StealthRock = true;
        SwitchTo(core, 1);
        Assert.Equal(bench.MaxHP, bench.CurrentHP);
    }

    [Fact]
    public void ADriveGivesTechnoBlastItsType()
    {
        var core = Duel(With(Mon("Genesect", 50, "Techno Blast"), item: "Douse Drive"), Mon("Arcanine", 50));
        var said = Turn(core, 0, 0);
        Assert.True(said.Contains("It's super effective!"), string.Join(" | ", said));
    }

    // ------------------------------------------------------------------ made to leave

    [Fact]
    public void AnEjectButtonSwitchesItsHolderOutAfterAHit()
    {
        var mine = Slow(With(Mon("Snorlax", 50, "Tackle"), item: "Eject Button"));
        var bench = Mon("Pikachu", 50, "Tackle");
        var core = Duel(mine, Fast(Mon("Machamp", 50, "Tackle")), bench: bench);
        var said = Turn(core, 0, 0);
        Assert.Contains(said, l => l.Contains("is switched out with the Eject Button"));
        said.AddRange(SendIn(core, 1));
        Assert.Equal(bench, core.PlayerSlots[0].Pokemon);
        Assert.Null(mine.HeldItem);
    }

    [Fact]
    public void ARedCardDragsItsAttackerOut()
    {
        var mine = Mon("Machamp", 50, "Tackle");
        var bench = Mon("Pikachu", 50, "Tackle");
        var core = Against(new[] { mine, bench }, new[] { With(Mon("Snorlax", 50), item: "Red Card") }, rules: Modern);
        var said = Turn(core, 0, 0);
        Assert.Contains(said, l => l.Contains("held up its Red Card"));
        Assert.Equal(bench, core.PlayerSlots[0].Pokemon);
    }

    [Fact]
    public void AnEjectPackSwitchesItsHolderOutAfterADrop()
    {
        var mine = Slow(With(Mon("Snorlax", 50, "Tackle"), item: "Eject Pack"));
        var bench = Mon("Pikachu", 50, "Tackle");
        var core = Duel(mine, Fast(Mon("Machamp", 50, "Growl")), bench: bench);
        var said = Turn(core, 0, 0);
        Assert.Contains(said, l => l.Contains("is switched out by the Eject Pack"));
        SendIn(core, 1);
        Assert.Equal(bench, core.PlayerSlots[0].Pokemon);
    }

    // ------------------------------------------------------------------ the bag

    [Fact]
    public void ASweetHeartRestoresTwentyHpInBattle()
    {
        var mine = Mon("Snorlax", 50, "Tackle");
        mine.CurrentHP = mine.MaxHP - 50;
        var core = Duel(mine, Mon("Pikachu", 50));
        core.Submit(BattleChoice.UseItem(Mine, "Sweet Heart"), BattleChoice.Fight(Foe, 0));
        Assert.Equal(mine.MaxHP - 30, mine.CurrentHP);
    }

    [Fact]
    public void MaxMushroomsRaiseEveryStatAStage()
    {
        var mine = Mon("Snorlax", 50, "Tackle");
        var core = Duel(mine, Mon("Pikachu", 50));
        core.Submit(BattleChoice.UseItem(Mine, "Max Mushrooms"), BattleChoice.Fight(Foe, 0));
        Assert.All(new[] { StatType.Attack, StatType.Defense, StatType.Speed, StatType.SpAttack, StatType.SpDefense }, s => Assert.Equal(1, Stage(mine, s)));
    }

    [Fact]
    public void TheRegionsSweetsCureAsAFullHeal()
    {
        Assert.True(BattleCore.CanUseInBattle(ItemDatabase.Get("Big Malasada")!));
        Assert.Equal(1, ItemDatabase.Get("Casteliacone")!.Use!["healParalysis"]);
        Assert.Equal((1, -1), (ItemDatabase.Get("Max Honey")!.Use!["revive"], ItemDatabase.Get("Max Honey")!.Use!["hpRestored"]));
    }

    // ------------------------------------------------------------------ the balls

    private static readonly BattleConditions Day = new();

    private static int Tenths(string ball, Pokemon target, int turn = 3, Pokemon? mine = null, BattleConditions? conditions = null) =>
        CatchCalculator.LaterBallTenths(ball, target, turn, conditions ?? Day, mine);

    [Fact]
    public void ABeastBallIsForUltraBeasts()
    {
        Assert.Equal(50, Tenths("Beast Ball", Mon("Nihilego", 50)));
        Assert.Equal(1, Tenths("Beast Ball", Mon("Pikachu", 50)));
        Assert.Equal(1, Tenths("Ultra Ball", Mon("Nihilego", 50)));
    }

    [Fact]
    public void PlatinumsBallsTakeTheNewestBonusesUnderTheLaterRules()
    {
        var pikachu = Mon("Pikachu", 50);
        Assert.Equal(50, Tenths("Quick Ball", pikachu, turn: 0));
        Assert.Equal(40, Formulas.BallTenths("Quick Ball", pikachu, 0, Day));    // Platinum's own
        Assert.Equal(35, Tenths("Net Ball", Mon("Gyarados", 50)));
        Assert.Equal(19, Tenths("Timer Ball", pikachu, turn: 3));
        Assert.Equal(31, Tenths("Nest Ball", Mon("Pikachu", 10)));
        Assert.Equal(30, Tenths("Dusk Ball", pikachu, conditions: new BattleConditions { Night = true }));
    }

    [Fact]
    public void JohtosBallsGoByWhatTheyAreFor()
    {
        Assert.Equal(40, Tenths("Fast Ball", Mon("Jolteon", 50)));
        Assert.Equal(10, Tenths("Fast Ball", Mon("Snorlax", 50)));
        Assert.Equal(80, Tenths("Level Ball", Mon("Pikachu", 10), mine: Mon("Snorlax", 50)));
        Assert.Equal(20, Tenths("Level Ball", Mon("Pikachu", 30), mine: Mon("Snorlax", 50)));
        Assert.Equal(40, Tenths("Moon Ball", Mon("Clefairy", 30)));
        Assert.Equal(40, Tenths("Lure Ball", Mon("Magikarp", 30), conditions: new BattleConditions { Fishing = true }));
        Assert.Equal(80, Tenths("Love Ball", Of(Gender.Female, "Pikachu", 30), mine: Of(Gender.Male, "Pikachu", 30)));
        Assert.Equal(10, Tenths("Love Ball", Of(Gender.Male, "Pikachu", 30), mine: Of(Gender.Male, "Pikachu", 30)));
        Assert.Equal(15, Tenths("Sport Ball", Mon("Pikachu", 30)));
    }

    [Fact]
    public void AHeavyBallGoesByWeight()
    {
        Assert.Equal(55, CatchCalculator.HeavyBallRate("Heavy Ball", Mon("Snorlax", 50), 25));            // 460 kg: 30 more
        Assert.Equal(170, CatchCalculator.HeavyBallRate("Heavy Ball", Mon("Pikachu", 50), 190));          // 6 kg: 20 less
    }

    [Fact]
    public void AHealBallMakesWhatItCaughtWholeAndAFriendBallItsFriend()
    {
        var caught = Mon("Pikachu", 30, "Thunder Shock");
        caught.CurrentHP = 1;
        caught.Status = StatusCondition.Paralyze;
        CatchCalculator.AfterTheCatch(caught, ItemDatabase.Get("Heal Ball")!);
        Assert.Equal((caught.MaxHP, StatusCondition.None), (caught.CurrentHP, caught.Status));
        CatchCalculator.AfterTheCatch(caught, ItemDatabase.Get("Friend Ball")!);
        Assert.Equal(200, caught.Friendship);
    }

    [Fact]
    public void AnOriginBallAlwaysHoldsUnderTheLaterRules()
    {
        var wild = Mon("Mewtwo", 70);
        Assert.Equal(4, CatchCalculator.Shakes(wild, ItemDatabase.Get("Origin Ball")!, new Random(1), 0, Day, rules: Modern));
    }
}
