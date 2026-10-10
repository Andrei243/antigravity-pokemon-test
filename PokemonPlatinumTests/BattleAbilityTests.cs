using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The abilities of plan 06 · R7, on the battle's rules alone and in the field: each bonus of the damage formula
/// in the original's own place (<c>BattleSystem_CalcMoveDamage</c>), what acts as a Pokémon comes in and in what
/// order (<c>BattleSystem_TriggerEffectOnSwitch</c>), the abilities that change the turn, a move's type or its
/// target, what answers a hit, the shapes the weather gives, what the party picks up after a battle, and what the
/// lead's ability does to the wild Pokémon met. One rule to a test, the rolls it hangs on fixed by kind, and the
/// numbers worked out from the original's code in a comment. A hit is
/// <c>attack × power × 22 / defense / 50 + 2</c> at level 50, each step rounded down. The stats, with no IVs:
/// <code>
///            HP   Atk  Def  SpA  SpD  Spe
/// Machamp    150  135   85   70   90   60   Fighting
/// Blastoise  139   88  105   90  110   83   Water
/// Snorlax    220  115   70   70  115   35   Normal
/// Charizard  138   89   83  114   90  105   Fire / Flying
/// Regigigas  170  165  115   85  115  105   Normal
/// Hitmonlee  110  125   58   40  115   92   Fighting
/// </code>
/// </summary>
public class BattleAbilityTests
{
    /// <summary>What one use of a move takes off a Blastoise, by whoever uses it.</summary>
    private static int OnBlastoise(Pokemon user, Pokemon? blastoise = null, BattleRandom? rolls = null, Ruleset? rules = null)
    {
        blastoise ??= Mon("Blastoise", 50);
        int before = blastoise.CurrentHP;
        Turn(Wild(user, blastoise, rolls, rules));
        return before - blastoise.CurrentHP;
    }

    // ================================================================== the damage formula, in the original's order

    [Fact]
    public void TechnicianMakesAMoveOfSixtyOrLessHalfAsStrongAgain()
    {
        // Tackle's 35 becomes 52: 135 × 52 × 22 / 105 / 50 = 29, + 2 = 31 (21 for anyone else). Aerial Ace's 60 is
        // the strongest it helps: 90, so 135 × 90 × 22 / 105 / 50 = 50, + 2 = 52 (35 without). Take Down's 90 is
        // left alone, and also comes to 52
        Assert.Equal(31, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), ability: "Technician")));
        Assert.Equal(21, OnBlastoise(Mon("Machamp", 50, "Tackle")));
        Assert.Equal(52, OnBlastoise(With(Mon("Machamp", 50, "Aerial Ace"), ability: "Technician")));
        Assert.Equal(35, OnBlastoise(Mon("Machamp", 50, "Aerial Ace")));
        Assert.Equal(52, OnBlastoise(With(Mon("Machamp", 50, "Take Down"), ability: "Technician")));
    }

    [Fact]
    public void ThickFatHalvesTheMovesPowerAndNotItsDamage()
    {
        // Charizard's Flamethrower (95) on Snorlax: 114 × 95 × 22 / 115 / 50 = 41, + 2 = 43, and half as much again
        // for its own type: 64. Thick Fat halves the power, to 47: 114 × 47 × 22 / 115 / 50 = 20, + 2 = 22, so 33
        // (half of the damage would be 32). Blastoise's Ice Beam: 90 × 47 × 22 / 115 / 50 = 16, + 2 = 18, from 34
        int Hit(string attacker, string move, string? attackersAbility = null)
        {
            var snorlax = With(Mon("Snorlax", 50), ability: "Thick Fat");
            Turn(Wild(With(Mon(attacker, 50, move), ability: attackersAbility), snorlax));
            return 220 - snorlax.CurrentHP;
        }
        Assert.Equal(33, Hit("Charizard", "Flamethrower"));
        Assert.Equal(18, Hit("Blastoise", "Ice Beam"));
        // Mold Breaker's user finds no Thick Fat there
        Assert.Equal(64, Hit("Charizard", "Flamethrower", "Mold Breaker"));

        var plain = Mon("Snorlax", 50);
        Turn(Wild(Mon("Charizard", 50, "Flamethrower"), plain));
        Assert.Equal(220 - 64, plain.CurrentHP);
    }

    [Fact]
    public void TheUsersAttackIsRaisedByItsAbilityBeforeTheHitIsWorkedOut()
    {
        // Huge Power and Pure Power double the Attack: 270 × 35 × 22 / 105 / 50 = 39, + 2 = 41
        Assert.Equal(41, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), ability: "Huge Power")));
        Assert.Equal(41, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), ability: "Pure Power")));
        // Hustle: 135 × 150 / 100 = 202: 202 × 35 × 22 / 105 / 50 = 29, + 2 = 31
        Assert.Equal(31, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), ability: "Hustle")));
        // A special move is left alone: Machamp's Water Gun on Blastoise, 70 × 40 × 22 / 110 / 50 = 11, + 2 = 13, half for the type: 6
        Assert.Equal(6, OnBlastoise(With(Mon("Machamp", 50, "Water Gun"), ability: "Huge Power")));
    }

    [Fact]
    public void GutsTurnsAConditionIntoStrength()
    {
        // Burned, Guts has half its Attack again and the burn doesn't halve the hit: 31. Without Guts the burn
        // halves it before the 2 is added: 19 / 2 = 9, + 2 = 11
        var guts = Mon("Machamp", 50, "Tackle");
        guts.Status = StatusCondition.Burn;
        Assert.Equal(31, OnBlastoise(guts));

        var plain = With(Mon("Machamp", 50, "Tackle"), ability: "No Guard");
        plain.Status = StatusCondition.Burn;
        Assert.Equal(11, OnBlastoise(plain));

        // With no condition Guts adds nothing
        Assert.Equal(21, OnBlastoise(Mon("Machamp", 50, "Tackle")));
    }

    [Fact]
    public void MarvelScaleHardensAHolderWithACondition()
    {
        // Defense 105 × 150 / 100 = 157: 135 × 35 × 22 / 157 / 50 = 13, + 2 = 15; 21 with no condition
        var scaled = With(Mon("Blastoise", 50), ability: "Marvel Scale");
        scaled.Status = StatusCondition.Paralyze;
        Assert.Equal(15, OnBlastoise(Mon("Machamp", 50, "Tackle"), scaled));
        Assert.Equal(21, OnBlastoise(Mon("Machamp", 50, "Tackle"), With(Mon("Blastoise", 50), ability: "Marvel Scale")));
    }

    [Fact]
    public void PlusAndMinusStrengthenEachOtherOnTheSameSide()
    {
        // Plusle's Thunder Shock on Blastoise: 90 × 40 × 22 / 110 / 50 = 14, + 2 = 16, its own type 24, Water 48.
        // Beside a Minus its Sp. Atk is 135: 135 × 40 × 22 / 110 / 50 = 21, + 2 = 23 → 34 → 68
        int Hit(string partner)
        {
            var blastoise = Mon("Blastoise", 50);
            var core = Doubles(new[] { Mon("Plusle", 50, "Thunder Shock"), Mon(partner, 50) }, new[] { blastoise, Mon("Snorlax", 50) });
            DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
            return 139 - blastoise.CurrentHP;
        }
        Assert.Equal(68, Hit("Minun"));
        Assert.Equal(48, Hit("Snorlax"));
        // Two of a kind do nothing for each other
        Assert.Equal(48, Hit("Plusle"));
    }

    [Fact]
    public void AStartersAbilityHelpsItsTypeInAPinch()
    {
        // Blastoise's Water Gun on Machamp: 90 × 40 × 22 / 90 / 50 = 17, + 2 = 19 → 28. At a third of its HP or
        // less (46 of 139) Torrent makes the power 60: 90 × 60 × 22 / 90 / 50 = 26, + 2 = 28 → 42
        int Hit(int hp)
        {
            var blastoise = Mon("Blastoise", 50, "Water Gun");
            blastoise.CurrentHP = hp;
            var machamp = Mon("Machamp", 50);
            Turn(Wild(blastoise, machamp));
            return 150 - machamp.CurrentHP;
        }
        Assert.Equal(42, Hit(46));
        Assert.Equal(28, Hit(47));
    }

    [Fact]
    public void HeatproofHalvesAndDrySkinFeedsAFireMovesPower()
    {
        // Charizard's Flamethrower on Bronzong (Sp. Def 121): 114 × 95 × 22 / 121 / 50 = 39, + 2 = 41 → 61 → 122.
        // Heatproof: power 47: 114 × 47 × 22 / 121 / 50 = 19, + 2 = 21 → 31 → 62
        var bronzong = Mon("Bronzong", 50);
        Turn(Wild(Mon("Charizard", 50, "Flamethrower"), bronzong));
        Assert.Equal(127 - 122, bronzong.CurrentHP);
        bronzong = With(Mon("Bronzong", 50), ability: "Heatproof");
        Turn(Wild(Mon("Charizard", 50, "Flamethrower"), bronzong));
        Assert.Equal(127 - 62, bronzong.CurrentHP);

        // On Blastoise: 114 × 95 × 22 / 110 / 50 = 43, + 2 = 45 → 67 → 33. Dry Skin: power 95 × 125 / 100 = 118:
        // 114 × 118 × 22 / 110 / 50 = 53, + 2 = 55 → 82 → 41
        Assert.Equal(33, OnBlastoise(Mon("Charizard", 50, "Flamethrower")));
        Assert.Equal(41, OnBlastoise(Mon("Charizard", 50, "Flamethrower"), With(Mon("Blastoise", 50), ability: "Dry Skin")));
    }

    [Fact]
    public void SimplesStagesCountDoubleInPlatinumAndChangeDoubleInTheNewestGames()
    {
        // Withdraw raises Blastoise's Defense a stage. With Simple it counts as two: 105 × 4 / 2 = 210, so
        // 135 × 35 × 22 / 210 / 50 = 9, + 2 = 11 (15 for anyone else: 105 × 3 / 2 = 157)
        var blastoise = With(Mon("Blastoise", 50, "Withdraw"), ability: "Simple");
        var said = Turn(Wild(Mon("Machamp", 50, "Tackle"), blastoise));
        Assert.Contains("Foe Blastoise's Defense rose!", said);
        Assert.Equal(1, blastoise.StatStages[StatType.Defense]);
        Assert.Equal(139 - 11, blastoise.CurrentHP);

        blastoise = Mon("Blastoise", 50, "Withdraw");
        Turn(Wild(Mon("Machamp", 50, "Tackle"), blastoise));
        Assert.Equal(139 - 15, blastoise.CurrentHP);

        // By the modern rules the change itself is doubled, and counts once: the same 11
        blastoise = With(Mon("Blastoise", 50, "Withdraw"), ability: "Simple");
        said = Turn(Wild(Mon("Machamp", 50, "Tackle"), blastoise, rules: Ruleset.Modern));
        Assert.Contains("Foe Blastoise's Defense sharply rose!", said);
        Assert.Equal(2, blastoise.StatStages[StatType.Defense]);
        Assert.Equal(139 - 11, blastoise.CurrentHP);
    }

    [Fact]
    public void SimpleDoublesItsHoldersOwnAttackAndSpeedStagesToo()
    {
        // Swords Dance's two stages count as four: 135 × 6 / 2 = 405: 405 × 35 × 22 / 105 / 50 = 59, + 2 = 61 (41 for anyone else)
        var blastoise = Mon("Blastoise", 50);
        var machamp = With(Mon("Machamp", 50, "Swords Dance", "Tackle", "Agility"), ability: "Simple");
        var core = Wild(machamp, blastoise);
        Turn(core);
        Assert.Equal(2, machamp.StatStages[StatType.Attack]);
        Turn(core, mine: 1);
        Assert.Equal(139 - 61, blastoise.CurrentHP);

        // Agility's two stages of Speed count as four in the turn order: 60 × 6 / 2 = 180
        Turn(core, mine: 2);
        Assert.Equal(180, core.EffectiveSpeed(core.At(Mine)));
    }

    [Fact]
    public void UnawareTakesNoNoticeOfTheOthersStages()
    {
        // The target's: Machamp's Swords Dance counts for nothing against it: 21, not 41
        var blastoise = With(Mon("Blastoise", 50), ability: "Unaware");
        var core = Wild(Mon("Machamp", 50, "Swords Dance", "Tackle"), blastoise);
        Turn(core);
        Turn(core, mine: 1);
        Assert.Equal(139 - 21, blastoise.CurrentHP);

        // The user's: Blastoise's Withdraw counts for nothing against its Tackle: 21, not 15
        blastoise = Mon("Blastoise", 50, "Withdraw");
        Turn(Wild(With(Mon("Machamp", 50, "Tackle"), ability: "Unaware"), blastoise));
        Assert.Equal(139 - 21, blastoise.CurrentHP);
    }

    [Fact]
    public void RivalryGoesByTheTwoGenders()
    {
        // Against its own gender the power is 35 × 125 / 100 = 43: 135 × 43 × 22 / 105 / 50 = 24, + 2 = 26.
        // Against the other, 35 × 75 / 100 = 26: 135 × 26 × 22 / 105 / 50 = 14, + 2 = 16
        Assert.Equal(26, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), ability: "Rivalry"), Of(Gender.Male, "Blastoise", 50)));
        Assert.Equal(16, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), ability: "Rivalry"), Of(Gender.Female, "Blastoise", 50)));

        // A Pokémon with no gender is nobody's rival: Porygon2 (Defense 95): 135 × 35 × 22 / 95 / 50 = 21, + 2 = 23
        var porygon = With(Of(Gender.Genderless, "Porygon2", 50), ability: "Sturdy");
        Turn(Wild(With(Mon("Machamp", 50, "Tackle"), ability: "Rivalry"), porygon));
        Assert.Equal(145 - 23, porygon.CurrentHP);
    }

    [Fact]
    public void IronFistAndRecklessStrengthenTheirKindsOfMove()
    {
        // Mach Punch 40 → 48: 135 × 48 × 22 / 105 / 50 = 27, + 2 = 29 → 43 (36 without: 24 → 36)
        Assert.Equal(43, OnBlastoise(With(Mon("Machamp", 50, "Mach Punch"), ability: "Iron Fist")));
        Assert.Equal(36, OnBlastoise(Mon("Machamp", 50, "Mach Punch")));
        // A move that isn't a punch: Tackle stays 21
        Assert.Equal(21, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), ability: "Iron Fist")));

        // Reckless, a move with recoil: Take Down 90 → 108: 135 × 108 × 22 / 105 / 50 = 61, + 2 = 63 (52 without)
        Assert.Equal(63, OnBlastoise(With(Mon("Machamp", 50, "Take Down"), ability: "Reckless")));
        // and one that crashes when it misses: Hitmonlee's Jump Kick 85 → 102: 125 × 102 × 22 / 105 / 50 = 53, + 2 = 55 → 82 (69 without)
        Assert.Equal(82, OnBlastoise(With(Mon("Hitmonlee", 50, "Jump Kick"), ability: "Reckless")));
        Assert.Equal(69, OnBlastoise(Mon("Hitmonlee", 50, "Jump Kick")));
        Assert.Equal(21, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), ability: "Reckless")));
    }

    [Fact]
    public void AHelpingHandComesAfterRecklessInThePower()
    {
        // Jump Kick 85, Reckless × 12 / 10 = 102, the Helping Hand × 15 / 10 = 153: 125 × 153 × 22 / 105 / 50 = 80,
        // + 2 = 82 → 123. The other way round would be 127 × 12 / 10 = 152
        var blastoise = Mon("Blastoise", 50);
        var hitmonlee = With(Mon("Hitmonlee", 50, "Jump Kick"), ability: "Reckless");
        var core = Doubles(new[] { Mon("Pikachu", 50, "Helping Hand"), hitmonlee }, new[] { blastoise, Mon("Snorlax", 50) });
        DoubleTurn(core, (Mine, 0, Mine2), (Mine2, 0, Foe), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(139 - 123, blastoise.CurrentHP);
    }

    [Fact]
    public void SolarPowerBurnsBrightAndBurnsItsHolder()
    {
        // Sunflora's Energy Ball (80) on Machamp: 110 × 80 × 22 / 90 / 50 = 43, + 2 = 45 → 67. In the sun its
        // Sp. Atk is 165: 165 × 80 × 22 / 90 / 50 = 64, + 2 = 66 → 99; and it loses 135 / 8 = 16 as the turn ends
        var machamp = Mon("Machamp", 50);
        var sunflora = With(Mon("Sunflora", 50, "Energy Ball"), ability: "Solar Power");
        var said = Turn(Wild(sunflora, machamp, sky: BattleWeather.Sun));
        Assert.Equal(150 - 99, machamp.CurrentHP);
        Assert.Contains("Sunflora is hurt by its Solar Power!", said);
        Assert.Equal(135 - 16, sunflora.CurrentHP);

        machamp = Mon("Machamp", 50);
        sunflora = With(Mon("Sunflora", 50, "Energy Ball"), ability: "Solar Power");
        Turn(Wild(sunflora, machamp));
        Assert.Equal(150 - 67, machamp.CurrentHP);
        Assert.Equal(135, sunflora.CurrentHP);
    }

    [Fact]
    public void FlowerGiftLiftsItsWholeSideInTheSun()
    {
        // Cherrim blooms, and beside it Machamp's Attack is 202: Tackle on Blastoise 31 (21). Blastoise's Water
        // Gun at Machamp finds a Sp. Def of 135: 90 × 40 × 22 / 135 / 50 = 11, halved by the sun 5, + 2 = 7 → 10
        // (without the gift: 17 → 8, + 2 = 10 → 15)
        (int OnBlastoise, int OnMachamp, List<string> Opening, Pokemon Cherrim) Play(string? ability, BattleWeather sky)
        {
            var blastoise = Mon("Blastoise", 50, "Water Gun");
            var machamp = Mon("Machamp", 50, "Tackle");
            var cherrim = With(Mon("Cherrim", 50), ability: ability);
            var core = Doubles(new[] { cherrim, machamp }, new[] { blastoise, Mon("Snorlax", 50) }, sky: sky);
            var opening = Lines(core.Log);
            DoubleTurn(core, (Mine, 0, null), (Mine2, 0, Foe), (Foe, 0, Mine2), (Foe2, 0, null));
            return (139 - blastoise.CurrentHP, 150 - machamp.CurrentHP, opening, cherrim);
        }

        var gift = Play(null, BattleWeather.Sun);
        Assert.Contains("Cherrim transformed!", gift.Opening);
        Assert.Equal((31, 10), (gift.OnBlastoise, gift.OnMachamp));

        // A Cherrim with another ability still blooms (the original asks only the species) and gives nothing
        var none = Play("Chlorophyll", BattleWeather.Sun);
        Assert.Contains("Cherrim transformed!", none.Opening);
        Assert.Equal("Cherrim-Sunshine", none.Cherrim.Form);
        Assert.Equal((21, 15), (none.OnBlastoise, none.OnMachamp));

        // Under a plain sky the gift is nothing: Water Gun 17 + 2 = 19 → 28
        var dull = Play(null, BattleWeather.None);
        Assert.DoesNotContain("Cherrim transformed!", dull.Opening);
        Assert.Equal((21, 28), (dull.OnBlastoise, dull.OnMachamp));
    }

    [Fact]
    public void AConfusedPokemonsHitOnItselfIsWorkedOutAsStruggle()
    {
        // CALC_SELF_HIT(MOVE_STRUGGLE, 40): 135 × 40 × 22 / 85 / 50 = 27, + 2 = 29, whatever move was chosen.
        // Technician asks for a move that isn't Struggle and adds nothing; Huge Power doubles the Attack (57);
        // Rivalry finds its own gender across from it and makes the power 50: 135 × 50 × 22 / 85 / 50 = 34, + 2 = 36
        int SelfHit(string ability)
        {
            var machamp = With(Mon("Machamp", 50, "Mach Punch"), ability: ability);
            var core = Wild(machamp, Mon("Blastoise", 50), Calm().Force(RollKind.ConfusionSelfHit, 0));
            core.At(Mine).ConfusionTurns = 3;
            Assert.Contains("It hurt itself in its confusion!", Turn(core));
            return 150 - machamp.CurrentHP;
        }
        Assert.Equal(29, SelfHit("No Guard"));
        Assert.Equal(29, SelfHit("Technician"));
        Assert.Equal(29, SelfHit("Iron Fist"));
        Assert.Equal(57, SelfHit("Huge Power"));
        Assert.Equal(36, SelfHit("Rivalry"));
    }

    // ================================================================== the turn

    [Fact]
    public void SlowStartHalvesAttackAndSpeedForFiveTurns()
    {
        // Regigigas's Tackle on Snorlax (Defense 70): 165 × 35 × 22 / 70 / 50 = 36, + 2 = 38 → 57. For its first
        // five turns its Attack is 82: 82 × 35 × 22 / 70 / 50 = 18, + 2 = 20 → 30, and its Speed 52
        var snorlax = Mon("Snorlax", 50);
        var core = Wild(Mon("Regigigas", 50, "Tackle"), snorlax);
        Assert.Contains("Regigigas can't get going yet!", Lines(core.Log));
        Assert.Equal(52, core.EffectiveSpeed(core.At(Mine)));

        var said = new List<string>();
        for (int turn = 0; turn < 5; turn++)
        {
            Assert.DoesNotContain("Regigigas finally got going!", said);
            int before = snorlax.CurrentHP;
            said = Turn(core);
            Assert.Equal(30, before - snorlax.CurrentHP);
        }
        // It is said as the sixth turn begins, and said once
        Assert.Contains("Regigigas finally got going!", said);
        Assert.Equal(105, core.EffectiveSpeed(core.At(Mine)));
        int hp = snorlax.CurrentHP;
        Assert.DoesNotContain("Regigigas finally got going!", Turn(core));
        Assert.Equal(57, hp - snorlax.CurrentHP);
    }

    [Fact]
    public void TruantLoafsEveryOtherTurn()
    {
        var core = Wild(Mon("Slaking", 50, "Tackle"), Mon("Blastoise", 50));
        Assert.Contains("Slaking used Tackle!", Turn(core));
        var second = Turn(core);
        Assert.Contains("Slaking is loafing around!", second);
        Assert.DoesNotContain("Slaking used Tackle!", second);
        Assert.Contains("Slaking used Tackle!", Turn(core));
        Assert.Contains("Slaking is loafing around!", Turn(core));
    }

    [Fact]
    public void ATruantThatComesInActsOnItsFirstTurn()
    {
        // Battler_CheckTruant: its bit is the turn after the one it came in on, whichever turn that was
        var core = Wild(Mon("Machamp", 50), Mon("Blastoise", 50), bench: Mon("Slaking", 50, "Tackle"));
        Turn(core);
        SwitchTo(core, 1);
        Assert.Contains("Slaking used Tackle!", Turn(core));
        Assert.Contains("Slaking is loafing around!", Turn(core));
    }

    [Fact]
    public void ALoafingTruantNeitherTightensItsFocusNorPursues()
    {
        // Battler_CheckTruant is asked before Focus Punch's focus and before Pursuit's catch
        var core = Wild(Mon("Slaking", 50, "Focus Punch"), Mon("Blastoise", 50));
        Assert.Contains("Slaking is tightening its focus!", Turn(core));
        var second = Turn(core);
        Assert.DoesNotContain("Slaking is tightening its focus!", second);
        Assert.Contains("Slaking is loafing around!", second);

        // The player's Machamp leaves on the turn Slaking loafs: nothing catches it on its way out
        var machamp = Mon("Machamp", 50);
        core = Wild(machamp, Mon("Slaking", 50, "Pursuit"), bench: Mon("Snorlax", 50));
        Turn(core);
        int hp = machamp.CurrentHP;
        var said = SwitchTo(core, 1);
        Assert.DoesNotContain("Foe Slaking used Pursuit!", said);
        Assert.Contains("Foe Slaking is loafing around!", said);
        Assert.Equal(hp, machamp.CurrentHP);
    }

    [Fact]
    public void StallMovesAfterEveryoneOfItsPriority()
    {
        // Sableye (Speed 55) is faster than Snorlax (35) and still goes after it; a move of a higher priority goes first all the same
        var core = Wild(With(Mon("Sableye", 50, "Scratch", "Quick Attack"), ability: "Stall"), Mon("Snorlax", 50, "Tackle"));
        InOrder(Turn(core), "Foe Snorlax used Tackle!", "Sableye used Scratch!");
        InOrder(Turn(core, mine: 1), "Sableye used Quick Attack!", "Foe Snorlax used Tackle!");

        core = Wild(Mon("Sableye", 50, "Scratch"), Mon("Snorlax", 50, "Tackle"));
        InOrder(Turn(core), "Sableye used Scratch!", "Foe Snorlax used Tackle!");

        // Two with Stall: the faster goes after
        core = Wild(With(Mon("Sableye", 50, "Scratch"), ability: "Stall"), With(Mon("Snorlax", 50, "Tackle"), ability: "Stall"));
        InOrder(Turn(core), "Foe Snorlax used Tackle!", "Sableye used Scratch!");
    }

    [Fact]
    public void UnburdenDoublesSpeedOnceTheItemItCameInWithIsGone()
    {
        var drifblim = With(Mon("Drifblim", 50, "Fling"), ability: "Unburden", item: "Leftovers");
        var core = Wild(drifblim, Mon("Snorlax", 50));
        Assert.Equal(85, core.EffectiveSpeed(core.At(Mine)));
        Turn(core);
        Assert.Null(drifblim.HeldItem);
        Assert.Equal(170, core.EffectiveSpeed(core.At(Mine)));

        // One that came in with nothing has nothing to lose
        core = Wild(With(Mon("Drifblim", 50), ability: "Unburden"), Mon("Snorlax", 50));
        Assert.Equal(85, core.EffectiveSpeed(core.At(Mine)));
    }

    [Fact]
    public void QuickFeetRunsFasterWithAConditionEvenParalysis()
    {
        // 60 × 15 / 10 = 90; anyone else paralysed is at a quarter: 15
        var quick = With(Mon("Machamp", 50), ability: "Quick Feet");
        quick.Status = StatusCondition.Paralyze;
        var core = Wild(quick, Mon("Snorlax", 50));
        Assert.Equal(90, core.EffectiveSpeed(core.At(Mine)));

        var plain = Mon("Machamp", 50);
        plain.Status = StatusCondition.Paralyze;
        core = Wild(plain, Mon("Snorlax", 50));
        Assert.Equal(15, core.EffectiveSpeed(core.At(Mine)));
    }

    [Fact]
    public void KlutzHasNoUseForWhatItHolds()
    {
        // Leftovers gives nothing back, and Fling has nothing to throw
        var lopunny = With(Mon("Lopunny", 50, "Fling"), ability: "Klutz", item: "Leftovers");
        lopunny.CurrentHP = 100;
        var said = Turn(Wild(lopunny, Mon("Snorlax", 50)));
        Assert.Contains("But it failed!", said);
        Assert.Equal(100, lopunny.CurrentHP);
        Assert.Equal("Leftovers", lopunny.HeldItem?.Name);

        // Anyone else: 125 / 16 = 7 back
        var plain = With(Mon("Lopunny", 50), ability: "Cute Charm", item: "Leftovers");
        plain.CurrentHP = 100;
        Turn(Wild(plain, Mon("Snorlax", 50)));
        Assert.Equal(107, plain.CurrentHP);
    }

    [Fact]
    public void GluttonyEatsItsBerryAtHalfItsHp()
    {
        // A Liechi Berry is eaten at a quarter of the holder's HP (98 / 4 = 24); with Gluttony at half (49)
        Pokemon At(int hp, string ability)
        {
            var zigzagoon = With(Mon("Zigzagoon", 50), ability: ability, item: "Liechi Berry");
            zigzagoon.CurrentHP = hp;
            Wild(zigzagoon, Mon("Snorlax", 50));
            return zigzagoon;
        }
        Assert.Null(At(49, "Gluttony").HeldItem);
        Assert.NotNull(At(50, "Gluttony").HeldItem);
        Assert.NotNull(At(49, "Pickup").HeldItem);
        Assert.Null(At(24, "Pickup").HeldItem);
        Assert.Equal(1, At(49, "Gluttony").StatStages[StatType.Attack]);
    }

    [Fact]
    public void PoisonHealTurnsPoisonIntoMedicine()
    {
        // subscript_poison_damage: an eighth of its HP back (120 / 8 = 15) in place of the damage, and nothing at all at full HP
        var breloom = With(Mon("Breloom", 50), ability: "Poison Heal");
        breloom.Status = StatusCondition.Poison;
        breloom.CurrentHP = 100;
        var core = Wild(breloom, Mon("Snorlax", 50));
        var said = Turn(core);
        Assert.Contains("Breloom restored HP using its Poison Heal!", said);
        Assert.DoesNotContain("Breloom is hurt by poison!", said);
        Assert.Equal(115, breloom.CurrentHP);
        Turn(core);
        Assert.Equal(120, breloom.CurrentHP);
        said = Turn(core);
        Assert.DoesNotContain("Breloom restored HP using its Poison Heal!", said);
        Assert.DoesNotContain("Breloom is hurt by poison!", said);

        var plain = With(Mon("Breloom", 50), ability: "Effect Spore");
        plain.Status = StatusCondition.Poison;
        Turn(Wild(plain, Mon("Snorlax", 50)));
        Assert.Equal(105, plain.CurrentHP);
    }

    // ================================================================== a move's type and its target

    [Fact]
    public void NormalizeMakesEveryMoveNormal()
    {
        // Delcatty's Thunderbolt on Geodude, which the ground would have saved: Normal, with its own type's bonus,
        // against Rock: 60 × 95 × 22 / 35 / 50 = 71, + 2 = 73 → 109 → 54
        var geodude = Mon("Geodude", 50);
        var said = Turn(Wild(With(Mon("Delcatty", 50, "Thunderbolt"), ability: "Normalize"), geodude));
        Assert.Contains("It's not very effective...", said);
        Assert.Equal(100 - 54, geodude.CurrentHP);

        // A Ghost isn't touched by it
        var gengar = Mon("Gengar", 50);
        said = Turn(Wild(With(Mon("Delcatty", 50, "Thunderbolt"), ability: "Normalize"), gengar));
        Assert.Contains("It doesn't affect Foe Gengar...", said);
        Assert.Equal(120, gengar.CurrentHP);

        // Without it Thunderbolt is what it is
        geodude = Mon("Geodude", 50);
        Assert.Contains("It doesn't affect Foe Geodude...", Turn(Wild(With(Mon("Delcatty", 50, "Thunderbolt"), ability: "Cute Charm"), geodude)));
    }

    [Fact]
    public void NormalizeHoldsForAPursuitThatCatchesItsTarget()
    {
        // A Ghost that leaves is caught by a Pursuit that is Normal, and so isn't touched
        var gengar = Mon("Gengar", 50);
        var core = Wild(gengar, With(Mon("Delcatty", 50, "Pursuit"), ability: "Normalize"), bench: Mon("Snorlax", 50));
        var said = SwitchTo(core, 1);
        InOrder(said, "Foe Delcatty used Pursuit!", "It doesn't affect Gengar...");
        Assert.Equal(120, gengar.CurrentHP);
    }

    [Fact]
    public void KlutzGetsNothingFromABerryItPlucks()
    {
        // BattleSystem_PluckBerry: the berry is gone from its holder all the same
        var blastoise = With(Mon("Blastoise", 50), item: "Sitrus Berry");
        var lopunny = With(Mon("Lopunny", 50, "Pluck"), ability: "Klutz");
        lopunny.CurrentHP = 50;
        var said = Turn(Wild(lopunny, blastoise));
        Assert.Contains("Lopunny stole and ate Foe Blastoise's Sitrus Berry!", said);
        Assert.Null(blastoise.HeldItem);
        Assert.Equal(50, lopunny.CurrentHP);

        // Anyone else gets its quarter back: 125 / 4 = 31
        blastoise = With(Mon("Blastoise", 50), item: "Sitrus Berry");
        lopunny = With(Mon("Lopunny", 50, "Pluck"), ability: "Cute Charm");
        lopunny.CurrentHP = 50;
        Turn(Wild(lopunny, blastoise));
        Assert.Equal(81, lopunny.CurrentHP);
    }

    [Fact]
    public void LightningRodDrawsAnElectricMoveAimedAtItsPartner()
    {
        // BattleSystem_CheckRedirectionAbilities: the move goes to the holder, which takes it like anyone (Platinum's
        // Lightning Rod gives no immunity): Pikachu's Thunder Shock on Manectric, 55 × 40 × 22 / 65 / 50 = 14, + 2 = 16 → 24 → 12
        var manectric = With(Mon("Manectric", 50), ability: "Lightning Rod");
        var blastoise = Mon("Blastoise", 50);
        var core = Doubles(new[] { Mon("Pikachu", 50, "Thunder Shock"), Mon("Snorlax", 50) }, new[] { blastoise, manectric });
        var said = DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        InOrder(said, "Pikachu used Thunder Shock!", "Foe Manectric's Lightning Rod took the attack!");
        Assert.Equal(139, blastoise.CurrentHP);
        Assert.Equal(130 - 12, manectric.CurrentHP);

        // Aimed at the holder itself, nothing is said of it
        said = DoubleTurn(core, (Mine, 0, Foe2), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.DoesNotContain("Foe Manectric's Lightning Rod took the attack!", said);
        Assert.Equal(130 - 24, manectric.CurrentHP);
    }

    [Fact]
    public void MoldBreakerAndNormalizeArentDrawnAway()
    {
        // Thunder Shock on Blastoise: 55 × 40 × 22 / 110 / 50 = 8, + 2 = 10 → 15 → 30
        var manectric = With(Mon("Manectric", 50), ability: "Lightning Rod");
        var blastoise = Mon("Blastoise", 50);
        var core = Doubles(new[] { With(Mon("Pikachu", 50, "Thunder Shock"), ability: "Mold Breaker"), Mon("Snorlax", 50) }, new[] { blastoise, manectric });
        DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(139 - 30, blastoise.CurrentHP);
        Assert.Equal(130, manectric.CurrentHP);
    }

    [Fact]
    public void StormDrainDrawsAWaterMoveAimedAtItsPartner()
    {
        // Blastoise's Water Gun on Gastrodon (Sp. Def 87): 90 × 40 × 22 / 87 / 50 = 18, + 2 = 20 → 30
        var gastrodon = With(Mon("Gastrodon", 50), ability: "Storm Drain");
        var snorlax = Mon("Snorlax", 50);
        var core = Doubles(new[] { Mon("Blastoise", 50, "Water Gun", "Surf"), Mon("Machamp", 50) }, new[] { snorlax, gastrodon });
        var said = DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Contains("Foe Gastrodon's Storm Drain took the attack!", said);
        Assert.Equal(220, snorlax.CurrentHP);
        Assert.Equal(171 - 30, gastrodon.CurrentHP);

        // A move that hits everyone goes where it goes
        said = DoubleTurn(core, (Mine, 1, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.DoesNotContain("Foe Gastrodon's Storm Drain took the attack!", said);
        Assert.True(snorlax.CurrentHP < 220);
    }

    // ================================================================== answering a hit

    [Fact]
    public void ColorChangeTakesTheTypeOfTheMoveThatHit()
    {
        // Charizard's Ember on Kecleon (Sp. Def 125): 114 × 40 × 22 / 125 / 50 = 16, + 2 = 18 → 27; then, Kecleon a Fire type, half: 13
        var kecleon = Mon("Kecleon", 50);
        var core = Wild(Mon("Charizard", 50, "Ember"), kecleon);
        var said = Turn(core);
        Assert.Contains("Foe Kecleon transformed into the Fire type!", said);
        Assert.Equal(120 - 27, kecleon.CurrentHP);
        Assert.True(core.At(Foe).HasType(PokemonType.Fire));
        Assert.False(core.At(Foe).HasType(PokemonType.Normal));

        said = Turn(core);
        Assert.DoesNotContain("Foe Kecleon transformed into the Fire type!", said);
        Assert.Contains("It's not very effective...", said);
        Assert.Equal(120 - 27 - 13, kecleon.CurrentHP);
    }

    [Fact]
    public void AftermathTakesAQuarterFromWhoeverKnocksItOutByTouch()
    {
        (int Lost, List<string> Said) Finish(string move, string? attackersAbility = null)
        {
            var machamp = With(Mon("Machamp", 50, move), ability: attackersAbility);
            var stunky = With(Mon("Stunky", 50), ability: "Aftermath");
            stunky.CurrentHP = 1;
            var said = Turn(Wild(machamp, stunky));
            Assert.Equal(0, stunky.CurrentHP);
            return (150 - machamp.CurrentHP, said);
        }
        // 150 / 4 = 37
        var touched = Finish("Tackle");
        Assert.Contains("Machamp is hurt by Foe Stunky's Aftermath!", touched.Said);
        Assert.Equal(37, touched.Lost);
        // Not for a move that doesn't touch it, not while anyone has Damp, not through Magic Guard
        Assert.Equal(0, Finish("Water Gun").Lost);
        Assert.Equal(0, Finish("Tackle", "Damp").Lost);
        Assert.Equal(0, Finish("Tackle", "Magic Guard").Lost);
    }

    [Fact]
    public void CuteCharmMakesWhoeverTouchesItFallInLoveThreeTimesInTen()
    {
        List<string> Touch(Gender mine, Gender theirs, int roll)
        {
            var machamp = Of(mine, "Machamp", 50, "Tackle");
            var lopunny = With(Of(theirs, "Lopunny", 50), ability: "Cute Charm");
            return Turn(Wild(machamp, lopunny, Calm().Force(RollKind.AbilityChance, roll)));
        }
        Assert.Contains("Foe Lopunny's Cute Charm infatuated Machamp!", Touch(Gender.Male, Gender.Female, 29));
        Assert.DoesNotContain("Foe Lopunny's Cute Charm infatuated Machamp!", Touch(Gender.Male, Gender.Female, 30));
        Assert.DoesNotContain("Foe Lopunny's Cute Charm infatuated Machamp!", Touch(Gender.Female, Gender.Female, 0));
    }

    // ================================================================== coming in

    [Fact]
    public void TraceTakesAFoesAbilityWhichThenActs()
    {
        // The Trace phase comes before Intimidate's, so the Intimidate it took cuts too; in each phase the faster goes first (Gyarados 86, Gardevoir 85)
        var gardevoir = With(Mon("Gardevoir", 50), ability: "Trace");
        var gyarados = Mon("Gyarados", 50);
        var core = Wild(gardevoir, gyarados, bench: Mon("Snorlax", 50));
        InOrder(Lines(core.Log), "Gardevoir traced Foe Gyarados's Intimidate!", "Foe Gyarados's Intimidate cuts Gardevoir's Attack!", "Gardevoir's Intimidate cuts Foe Gyarados's Attack!");
        Assert.Equal("Intimidate", gardevoir.AbilityName);
        Assert.Equal(-1, gyarados.StatStages[StatType.Attack]);

        // It is Trace again once it has left
        SwitchTo(core, 1);
        Assert.Equal("Trace", gardevoir.AbilityName);
    }

    [Fact]
    public void TraceLeavesWhatItCantTake()
    {
        var gardevoir = With(Mon("Gardevoir", 50), ability: "Trace");
        var core = Wild(gardevoir, Mon("Castform", 50));
        Assert.DoesNotContain(Lines(core.Log), line => line.Contains("traced"));
        Assert.Equal("Trace", gardevoir.AbilityName);
    }

    [Fact]
    public void AnticipationShuddersAtAMoveThatWouldHurt()
    {
        bool Shudders(Pokemon foe, string holder = "Snorlax", int level = 50) =>
            Lines(Wild(foe, With(Mon(holder, level), ability: "Anticipation")).Log).Contains($"Foe {holder} shuddered!");

        // A Fighting move against a Normal type; not Tackle
        Assert.True(Shudders(Mon("Machamp", 50, "Tackle", "Mach Punch")));
        Assert.False(Shudders(Mon("Machamp", 50, "Tackle")));
        // The counters are left out (sMovesCannotTriggerAnticipation), and a move that does no damage has no flag to raise
        Assert.False(Shudders(Mon("Machamp", 50, "Counter")));
        Assert.False(Shudders(Mon("Machamp", 50, "Bulk Up")));
        // A one-hit knockout from a foe of its own level or more, unless the move can't touch it (Fissure on a Flying type)
        Assert.True(Shudders(Mon("Machamp", 50, "Fissure")));
        Assert.False(Shudders(Mon("Machamp", 49, "Fissure")));
        Assert.False(Shudders(Mon("Machamp", 50, "Fissure"), holder: "Charizard"));
    }

    [Fact]
    public void ForewarnNamesTheFoesStrongestMove()
    {
        string? Warned(params string[] moves)
        {
            var core = Wild(With(Mon("Snorlax", 50), ability: "Forewarn"), Mon("Charizard", 50, moves));
            const string start = "Snorlax's Forewarn alerted it to ";
            return Lines(core.Log).FirstOrDefault(l => l.StartsWith(start))?[start.Length..].TrimEnd('!');
        }
        Assert.Equal("Flamethrower", Warned("Ember", "Flamethrower", "Growl"));
        // A move of no fixed power counts as 80, a counter as 120 and a one-hit knockout as 150
        Assert.Equal("Dragon Rage", Warned("Ember", "Dragon Rage"));
        Assert.Equal("Counter", Warned("Flamethrower", "Counter"));
        Assert.Equal("Fissure", Warned("Counter", "Fissure", "Flamethrower"));
        // With nothing that hurts, any move of a foe
        Assert.Equal("Growl", Warned("Growl"));
    }

    [Fact]
    public void FriskFindsWhatTheFoeHolds()
    {
        var core = Wild(With(Mon("Banette", 50), ability: "Frisk"), With(Mon("Blastoise", 50), item: "Leftovers"));
        Assert.Contains("Banette frisked Foe Blastoise and found its Leftovers!", Lines(core.Log));

        core = Wild(With(Mon("Banette", 50), ability: "Frisk"), Mon("Blastoise", 50));
        Assert.DoesNotContain(Lines(core.Log), line => line.Contains("frisked"));
    }

    [Fact]
    public void MoldBreakerAndPressureAnnounceThemselves()
    {
        var core = Wild(With(Mon("Machamp", 50), ability: "Mold Breaker"), With(Mon("Snorlax", 50), ability: "Pressure"));
        InOrder(Lines(core.Log), "Machamp breaks the mold!", "Foe Snorlax is exerting its Pressure!");
    }

    [Fact]
    public void WhatActsOnEntryGoesPhaseByPhaseAndOnlyThenBySpeed()
    {
        // The weather abilities come before Intimidate, though Gyarados (86) is far faster than Snorlax (35)
        var core = Wild(Mon("Gyarados", 50), With(Mon("Snorlax", 50), ability: "Drizzle"));
        InOrder(Lines(core.Log), "Foe Snorlax's Drizzle brought the rain!", "Gyarados's Intimidate cuts Foe Snorlax's Attack!");

        // Nothing acts twice: a turn later the log has each line once
        Turn(core);
        Assert.Single(Lines(core.Log), line => line == "Gyarados's Intimidate cuts Foe Snorlax's Attack!");
        Assert.Single(Lines(core.Log), line => line == "Foe Snorlax's Drizzle brought the rain!");
    }

    [Fact]
    public void NothingActsTwiceWhenTheGamePlaysTheBattle()
    {
        // Through the battle's face, whose own battlers are copies of the rules': what has acted on entry is
        // carried over with the rest of a battler's state, turn after turn
        var gyarados = Mon("Gyarados", 50);
        var snorlax = With(Mon("Snorlax", 50), ability: "Pressure");
        var battle = Battle(gyarados, snorlax);
        var said = new List<string>();
        for (int turn = 0; turn < 3; turn++) said.AddRange(Turn(battle));
        Assert.DoesNotContain("Gyarados's Intimidate cuts Foe Snorlax's Attack!", said);
        Assert.DoesNotContain("Foe Snorlax is exerting its Pressure!", said);
        Assert.Equal(-1, battle.EnemyPokemon.StatStages[StatType.Attack]);
    }

    [Fact]
    public void AChoiceMadeWhileLinesAreStillShownDoesntUndoTheRulesState()
    {
        // A tool that picks a move before the opening lines have been read (the screen is then behind the rules)
        // must not hand the screen's stale state back to them: Intimidate would act a second time
        var party = new Party();
        party.Add(Mon("Gyarados", 50));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon("Snorlax", 50) }, Random = Steady(), Rules = Ruleset.Platinum
        });
        Assert.Equal(BattleMenuState.Message, battle.HUD.MenuState);
        battle.SelectMove(0);
        var said = Settle(battle);
        Assert.Single(said, line => line == "Gyarados's Intimidate cuts Foe Snorlax's Attack!");
        Assert.Equal(-1, battle.EnemyPokemon.StatStages[StatType.Attack]);
    }

    [Fact]
    public void AnAbilityTakenByTransformActsAsIfItHadJustComeIn()
    {
        // BtlCmd_Transform clears the flags of what has acted: Ditto, now with Intimidate, cuts the foe's Attack in the check after its move
        var gyarados = Mon("Gyarados", 50);
        var ditto = Mon("Ditto", 50, "Transform");
        var core = Wild(ditto, gyarados);
        var said = Turn(core);
        InOrder(said, "Ditto transformed into Foe Gyarados!", "Ditto's Intimidate cuts Foe Gyarados's Attack!");
        Assert.Equal(-1, gyarados.StatStages[StatType.Attack]);
    }

    // ================================================================== shapes

    [Fact]
    public void ForecastGivesCastformTheWeathersShapeAndType()
    {
        var castform = Mon("Castform", 50, "Sunny Day", "Hail");
        var core = Wild(castform, Mon("Snorlax", 50), sky: BattleWeather.Rain, bench: Mon("Machamp", 50));
        Assert.Contains("Castform transformed!", Lines(core.Log));
        Assert.Equal("Castform-Rainy", castform.Form);
        Assert.True(core.At(Mine).HasType(PokemonType.Water));

        // As soon as the weather has changed
        Assert.Contains("Castform transformed!", Turn(core));
        Assert.Equal("Castform-Sunny", castform.Form);
        Assert.True(core.At(Mine).HasType(PokemonType.Fire));
        Turn(core, mine: 1);
        Assert.Equal("Castform-Snowy", castform.Form);
        Assert.True(core.At(Mine).HasType(PokemonType.Ice));

        // It is itself again when it leaves the field
        SwitchTo(core, 1);
        Assert.Null(castform.Form);
    }

    [Fact]
    public void ForecastGoesBackWhenTheSkyClearsOrIsIgnored()
    {
        // Sunny Day lasts five turns; when it ends Castform is plain again
        var castform = Mon("Castform", 50, "Sunny Day");
        castform.Moves.Add(new Move(Idle));
        var core = Wild(castform, Mon("Snorlax", 50));
        Turn(core);
        Assert.Equal("Castform-Sunny", castform.Form);
        for (int turn = 0; turn < 4; turn++) Turn(core, mine: 1);
        Assert.Null(castform.Form);
        Assert.True(core.At(Mine).HasType(PokemonType.Normal));

        // With Cloud Nine across the field the weather is nothing to it
        castform = Mon("Castform", 50);
        core = Wild(castform, With(Mon("Snorlax", 50), ability: "Cloud Nine"), sky: BattleWeather.Rain);
        Assert.Null(castform.Form);
    }

    [Fact]
    public void MultitypeMakesArceusItsPlatesType()
    {
        var arceus = With(Mon("Arceus", 50), item: "Flame Plate");
        var core = Wild(arceus, Mon("Snorlax", 50));
        Assert.Equal("Arceus-Fire", arceus.Form);
        Assert.True(core.At(Mine).HasType(PokemonType.Fire));
        Assert.False(core.At(Mine).HasType(PokemonType.Normal));

        arceus = Mon("Arceus", 50);
        core = Wild(arceus, Mon("Snorlax", 50));
        Assert.Null(arceus.Form);
        Assert.True(core.At(Mine).HasType(PokemonType.Normal));
    }

    [Fact]
    public void ArceussTypeIsReadFromThePlateItself()
    {
        // BattleMon_Get: a Ditto in the shape of an Arceus that holds a Flame Plate has Multitype and no plate, and so is Normal
        var ditto = Mon("Ditto", 50, "Transform");
        var core = Wild(ditto, With(Mon("Arceus", 50), item: "Flame Plate"));
        Assert.True(core.At(Foe).HasType(PokemonType.Fire));
        Turn(core);
        Assert.Equal("Multitype", ditto.AbilityName);
        Assert.True(core.At(Mine).HasType(PokemonType.Normal));
        Assert.False(core.At(Mine).HasType(PokemonType.Fire));
        // And it keeps its own HP through it
        Assert.Equal(108, ditto.MaxHP);
    }

    [Fact]
    public void AnEmbargoDoesntTakeHoldOfArceus()
    {
        // subscript_embargo_start
        var core = Wild(Mon("Machamp", 50, "Embargo"), With(Mon("Arceus", 50), item: "Flame Plate"));
        Assert.Contains("But it failed!", Turn(core));
        Assert.Equal(0, core.At(Foe).Volatile.EmbargoTurns);
    }

    // ================================================================== after a battle won

    private static Pokemon? PickedUp(string ability, int level, params int[] rolls)
    {
        var finder = With(Mon(ability == "Honey Gather" ? "Combee" : "Zigzagoon", level), ability: ability);
        var foe = Mon("Bidoof", 5);
        foe.CurrentHP = 1;
        var core = Wild(Mon("Machamp", 50, "Tackle"), foe, Calm().Force(RollKind.Pickup, rolls), bench: finder);
        Turn(core);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        return finder;
    }

    [Theory]
    // BtlCmd_GenerateEndOfBattleItem: one time in ten; then a draw of 100 on the row of its level (a row every ten
    // levels): 30, then 10 each to 90, then 4 and 4, and 1 for each of the row's two rare finds
    [InlineData(50, 0, "Repel")]
    [InlineData(50, 29, "Repel")]
    [InlineData(50, 30, "Escape Rope")]
    [InlineData(50, 93, "Dusk Stone")]
    [InlineData(50, 94, "Shiny Stone")]
    [InlineData(50, 97, "Shiny Stone")]
    [InlineData(50, 98, "White Herb")]
    [InlineData(50, 99, "Ether")]
    [InlineData(1, 0, "Potion")]
    [InlineData(10, 99, "Hyper Potion")]
    [InlineData(11, 0, "Antidote")]
    [InlineData(100, 0, "Revive")]
    [InlineData(100, 97, "Max Elixir")]
    [InlineData(100, 98, "TM26")]
    [InlineData(100, 99, "Leftovers")]
    public void PickupFindsAnItemByItsHoldersLevel(int level, int draw, string item) =>
        Assert.Equal(item, PickedUp("Pickup", level, 0, draw)!.HeldItem?.Name);

    [Fact]
    public void PickupFindsNothingNineTimesInTenAndNothingWithItsHandsFull()
    {
        Assert.Null(PickedUp("Pickup", 50, 1, 0)!.HeldItem);

        var holding = With(With(Mon("Zigzagoon", 50), ability: "Pickup"), item: "Leftovers");
        var foe = Mon("Bidoof", 5);
        foe.CurrentHP = 1;
        Turn(Wild(Mon("Machamp", 50, "Tackle"), foe, Calm().Force(RollKind.Pickup, 0, 0), bench: holding));
        Assert.Equal("Leftovers", holding.HeldItem?.Name);
    }

    [Fact]
    public void HoneyGatherFindsHoneyTheMoreOftenTheHigherItsLevel()
    {
        // sHoneyGatherRate: five in a hundred for every ten levels: 25 at level 50, 5 at level 10, 50 at level 100
        Assert.Equal("Honey", PickedUp("Honey Gather", 50, 24)!.HeldItem?.Name);
        Assert.Null(PickedUp("Honey Gather", 50, 25)!.HeldItem);
        Assert.Equal("Honey", PickedUp("Honey Gather", 10, 4)!.HeldItem?.Name);
        Assert.Null(PickedUp("Honey Gather", 10, 5)!.HeldItem);
        Assert.Equal("Honey", PickedUp("Honey Gather", 51, 29)!.HeldItem?.Name);
        Assert.Equal("Honey", PickedUp("Honey Gather", 100, 49)!.HeldItem?.Name);
    }

    [Fact]
    public void NothingIsPickedUpFromABattleLost()
    {
        var finder = With(Mon("Zigzagoon", 50), ability: "Pickup");
        finder.CurrentHP = 0;
        var mine = Mon("Bidoof", 5, "Tackle");
        mine.CurrentHP = 1;
        var core = Wild(mine, Mon("Machamp", 50, "Tackle"), Calm().Force(RollKind.Pickup, 0, 0), bench: finder);
        Turn(core);
        Assert.Equal(BattleResult.PlayerDefeat, core.Result);
        Assert.Null(finder.HeldItem);
    }

    // ================================================================== in the field: the wild Pokémon met

    /// <summary>A generator that gives the numbers it was handed, in turn, and then nothing but zeroes.</summary>
    private sealed class Scripted(params int[] rolls) : Random
    {
        private readonly Queue<int> left = new(rolls);
        public override int Next(int maxValue) => left.Count > 0 ? Math.Min(left.Dequeue(), Math.Max(0, maxValue - 1)) : 0;
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
    }

    private static WildLead Lead(string ability, int level = 30, Nature nature = Nature.Hardy, Gender gender = Gender.Male) => new(ability, level, nature, gender);

    private static WildEncounterEntry Row(string species, int level, int weight = 10, int? to = null) =>
        new() { SpeciesName = species, MinLevel = level, MaxLevel = to ?? level, Weight = weight };

    [Fact]
    public void TheLeadsAbilityChangesHowOftenAWildPokemonIsMet()
    {
        // ModifyEncounterRateWithFieldParams
        Assert.Equal(30, WildEncounterRules.Rate(30, null, FieldWeather.Clear));
        Assert.Equal(30, WildEncounterRules.Rate(30, Lead("Overgrow"), FieldWeather.Clear));
        foreach (string ability in new[] { "Arena Trap", "No Guard", "Illuminate" })
            Assert.Equal(60, WildEncounterRules.Rate(30, Lead(ability), FieldWeather.Clear));
        Assert.Equal(100, WildEncounterRules.Rate(70, Lead("Illuminate"), FieldWeather.Clear));
        foreach (string ability in new[] { "White Smoke", "Quick Feet", "Stench" })
            Assert.Equal(15, WildEncounterRules.Rate(30, Lead(ability), FieldWeather.Clear));

        Assert.Equal(15, WildEncounterRules.Rate(30, Lead("Sand Veil"), FieldWeather.Sandstorm));
        Assert.Equal(30, WildEncounterRules.Rate(30, Lead("Sand Veil"), FieldWeather.Clear));
        foreach (var snow in new[] { FieldWeather.Snow, FieldWeather.HeavySnow, FieldWeather.Blizzard })
            Assert.Equal(15, WildEncounterRules.Rate(30, Lead("Snow Cloak"), snow));
        Assert.Equal(30, WildEncounterRules.Rate(30, Lead("Snow Cloak"), FieldWeather.Hail));
    }

    [Fact]
    public void MagnetPullAndStaticDrawTheirTypesOutOfTheTable()
    {
        var table = new[] { Row("Bidoof", 3, 20), Row("Starly", 3, 20), Row("Magnemite", 5), Row("Pikachu", 4), Row("Bronzor", 6) };

        // TryGetSlotForTypeMatchAbility: one time in two, any of the rows of the type, each as likely
        Assert.Equal("Magnemite", WildEncounterRules.Slot(table, false, Lead("Magnet Pull"), new Scripted(0, 0)).SpeciesName);
        Assert.Equal("Bronzor", WildEncounterRules.Slot(table, false, Lead("Magnet Pull"), new Scripted(0, 1)).SpeciesName);
        Assert.Equal("Pikachu", WildEncounterRules.Slot(table, false, Lead("Static"), new Scripted(0, 1)).SpeciesName);
        // The other time, the table by its weights as for anyone: 0 to 19 of 60 is the first row, 20 to 39 the second
        Assert.Equal("Bidoof", WildEncounterRules.Slot(table, false, Lead("Magnet Pull"), new Scripted(1, 19)).SpeciesName);
        Assert.Equal("Starly", WildEncounterRules.Slot(table, false, Lead("Magnet Pull"), new Scripted(1, 20)).SpeciesName);
        Assert.Equal("Starly", WildEncounterRules.Slot(table, false, null, new Scripted(20)).SpeciesName);

        // On the water Magnet Pull finds nothing (the original overwrites what it found), Static still does
        Assert.Equal("Bidoof", WildEncounterRules.Slot(table, true, Lead("Magnet Pull"), new Scripted(0, 0)).SpeciesName);
        Assert.Equal("Magnemite", WildEncounterRules.Slot(table, true, Lead("Static"), new Scripted(0, 0)).SpeciesName);

        // A table with none of the type, or of nothing else, is drawn from as usual
        var steel = new[] { Row("Magnemite", 5), Row("Bronzor", 6) };
        Assert.Equal("Bronzor", WildEncounterRules.Slot(steel, false, Lead("Magnet Pull"), new Scripted(0, 10)).SpeciesName);
        var none = new[] { Row("Bidoof", 3), Row("Starly", 4) };
        Assert.Equal("Starly", WildEncounterRules.Slot(none, false, Lead("Static"), new Scripted(0, 10)).SpeciesName);
    }

    [Fact]
    public void HustleVitalSpiritAndPressureMeetTheHighestLevels()
    {
        // The land's rows have a level each: one time in two, the highest row of the same species (TryFindHigherLevelSlot)
        var land = new[] { Row("Bidoof", 3), Row("Starly", 4), Row("Bidoof", 5), Row("Bidoof", 4) };
        foreach (string ability in new[] { "Hustle", "Vital Spirit", "Pressure" })
        {
            Assert.Equal(5, WildEncounterRules.Level(land, land[0], false, Lead(ability), new Scripted(1)));
            Assert.Equal(3, WildEncounterRules.Level(land, land[0], false, Lead(ability), new Scripted(0)));
        }
        Assert.Equal(3, WildEncounterRules.Level(land, land[0], false, Lead("Overgrow"), new Scripted(1)));

        // The water's rows have a range: one of it, or one time in two its top (GetWildMonLevel)
        var water = new[] { Row("Psyduck", 20, 60, to: 30) };
        Assert.Equal(23, WildEncounterRules.Level(water, water[0], true, null, new Scripted(3)));
        Assert.Equal(23, WildEncounterRules.Level(water, water[0], true, Lead("Hustle"), new Scripted(3, 0)));
        Assert.Equal(30, WildEncounterRules.Level(water, water[0], true, Lead("Hustle"), new Scripted(3, 1)));
    }

    [Fact]
    public void KeenEyeAndIntimidateKeepWeakWildPokemonAway()
    {
        // FirstMonAbilityPreventsEncounter: a lead above level 5, a wild Pokémon five levels or more below it, one time in two
        foreach (string ability in new[] { "Keen Eye", "Intimidate" })
        {
            Assert.True(WildEncounterRules.ScaredOff(Lead(ability, 20), 15, new Scripted(0)));
            Assert.False(WildEncounterRules.ScaredOff(Lead(ability, 20), 15, new Scripted(1)));
            Assert.False(WildEncounterRules.ScaredOff(Lead(ability, 20), 16, new Scripted(0)));
            Assert.False(WildEncounterRules.ScaredOff(Lead(ability, 5), 1, new Scripted(0)));
        }
        Assert.False(WildEncounterRules.ScaredOff(Lead("Overgrow", 50), 2, new Scripted(0)));
        Assert.False(WildEncounterRules.ScaredOff(null, 2, new Scripted(0)));

        // A step that would have met it meets nothing
        var table = new[] { Row("Bidoof", 3) };
        Assert.Null(WildEncounterRules.Meet(table, false, Lead("Keen Eye", 20), new Scripted(0, 0)));
        Assert.Equal("Bidoof", WildEncounterRules.Meet(table, false, Lead("Keen Eye", 20), new Scripted(0, 1))!.SpeciesName);
    }

    [Fact]
    public void SynchronizeAndCuteCharmChooseTheWildPokemonsNatureAndGender()
    {
        // GetNatureForWildMon: the lead's own nature one time in two
        Assert.Equal(Nature.Adamant, WildEncounterRules.NatureFor(Lead("Synchronize", nature: Nature.Adamant), new Scripted(0)));
        Assert.Null(WildEncounterRules.NatureFor(Lead("Synchronize", nature: Nature.Adamant), new Scripted(1)));
        Assert.Null(WildEncounterRules.NatureFor(Lead("Overgrow", nature: Nature.Adamant), new Scripted(0)));

        // CreateWildMon: the gender the lead isn't, two times in three, for a species that has both
        var bidoof = PokemonDatabase.Get("Bidoof")!;
        Assert.Equal(Gender.Male, WildEncounterRules.GenderFor(bidoof, Lead("Cute Charm", gender: Gender.Female), new Scripted(1)));
        Assert.Equal(Gender.Female, WildEncounterRules.GenderFor(bidoof, Lead("Cute Charm", gender: Gender.Male), new Scripted(2)));
        Assert.Null(WildEncounterRules.GenderFor(bidoof, Lead("Cute Charm", gender: Gender.Female), new Scripted(0)));
        Assert.Null(WildEncounterRules.GenderFor(PokemonDatabase.Get("Hitmonlee"), Lead("Cute Charm", gender: Gender.Female), new Scripted(1)));
        Assert.Null(WildEncounterRules.GenderFor(PokemonDatabase.Get("Magnemite"), Lead("Cute Charm", gender: Gender.Female), new Scripted(1)));
        Assert.Null(WildEncounterRules.GenderFor(bidoof, Lead("Overgrow", gender: Gender.Female), new Scripted(1)));
    }

    [Fact]
    public void AWildPokemonMetIsMadeWithWhatTheLeadChose()
    {
        // The row met carries the level decided and the choices; the Pokémon made from it has them, as its personality
        // gives them (plan 06 · R15): Cute Charm's built personality (sub_02074088) with Synchronize's nature, so its
        // ability too is the personality's
        var table = new[] { Row("Bidoof", 3, 10, to: 7) };
        var met = WildEncounterRules.Meet(table, false, Lead("Synchronize", nature: Nature.Timid), new Scripted(0, 2, 0))!;
        Assert.Equal((5, 5, Nature.Timid, null), (met.MinLevel, met.MaxLevel, met.Nature, met.Gender));
        Assert.NotSame(table[0], met);

        var species = PokemonDatabase.Get("Bidoof")!;
        var chosen = new Pokemon(species, 5, new Random(7), gender: Gender.Female, nature: Nature.Timid);
        Assert.Equal((Gender.Female, Nature.Timid), (chosen.Gender, chosen.Nature));
        Assert.Equal((Gender.Female, Nature.Timid), (Personality.GenderOf(species, chosen.Personality), Personality.NatureOf(chosen.Personality)));
        Assert.Equal(Personality.AbilityOf(species.Abilities, chosen.Personality), chosen.AbilityName);
    }

    [Fact]
    public void WhatTheLeadIsComesFromTheHeadOfTheParty()
    {
        var party = new Party();
        Assert.Null(WildLead.Of(party));
        var first = With(Of(Gender.Female, "Gardevoir", 42), ability: "Synchronize");
        first.CurrentHP = 0;
        party.Add(first);
        party.Add(Mon("Machamp", 50));
        // Fainted or not: the original asks only that it isn't an egg
        Assert.Equal(new WildLead("Synchronize", 42, Nature.Hardy, Gender.Female), WildLead.Of(party));
    }
}
