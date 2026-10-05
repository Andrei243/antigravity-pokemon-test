using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The move families of plan 06 · R4, on the battle's rules alone: moves that hit several times, fixed damage,
/// one-hit knockouts, power by the battle's numbers, the moves that cost their user everything, the counters,
/// Stockpile's three and the hits that only work in their turn. One rule to a test, the rolls it hangs on fixed
/// by kind, and the numbers worked out from the original's code in a comment (<c>battle_script.c</c>'s
/// <c>BtlCmd_*</c> and the effect scripts). The stats are as in <see cref="BattleFieldTests"/> (base + 5, HP
/// base + 60 at level 50); also used here:
/// <code>
///            HP   Atk  Def  SpA  SpD  Spe
/// Steelix    135   90  205   60   70   35   Steel / Ground, 400 kg
/// Pikachu     95   60   35   55   45   95   Electric, 6 kg
/// Arceus     180  125  125  125  125  125   Normal
/// Beedrill   125   85   45   50   85   80   Bug / Poison
/// </code>
/// </summary>
public class BattleFamilyTests
{
    private static List<MoveShown> Shown(BattleCore core) => core.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<MoveShown>().ToList();

    // ================================================================== several hits

    [Fact]
    public void AMoveOfSeveralHitsHitsTwoToFiveTimes()
    {
        // BtlCmd_SetMultiHit: a draw of 4 below 2 is the count less two; otherwise a second draw of 4, plus two.
        // Fury Attack, 15 power: 135 × 15 × 22 / 105 / 50 = 8, + 2 = 10 a hit
        var blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(Mon("Machamp", 50, "Fury Attack"), blastoise, Calm().Force(RollKind.HitCount, 2, 3)));
        InOrder(said, "Machamp used Fury Attack!", "Hit 5 times!");
        Assert.Equal(139 - 50, blastoise.CurrentHP);

        blastoise = Mon("Blastoise", 50);
        Assert.Contains("Hit 2 times!", Turn(Wild(Mon("Machamp", 50, "Fury Attack"), blastoise, Calm().Force(RollKind.HitCount, 0))));
        Assert.Equal(139 - 20, blastoise.CurrentHP);

        // Skill Link makes it five every time; the hits stop when the target is down
        Assert.Contains("Hit 5 times!", Turn(Wild(With(Mon("Machamp", 50, "Fury Attack"), ability: "Skill Link"), Mon("Blastoise", 50), Calm().Force(RollKind.HitCount, 0))));
        blastoise = Mon("Blastoise", 50);
        blastoise.CurrentHP = 15;
        InOrder(Turn(Wild(Mon("Machamp", 50, "Fury Attack"), blastoise, Calm().Force(RollKind.HitCount, 2, 3))), "Hit 2 times!", "Foe Blastoise fainted!");

        // By the modern rules the count is drawn out of a hundred: 35, 35, 15 and 15
        Assert.Contains("Hit 4 times!", Turn(Wild(Mon("Machamp", 50, "Fury Attack"), Mon("Blastoise", 50), Calm().Force(RollKind.HitCount, 84), Ruleset.Modern)));
        Assert.Contains("Hit 5 times!", Turn(Wild(Mon("Machamp", 50, "Fury Attack"), Mon("Blastoise", 50), Calm().Force(RollKind.HitCount, 85), Ruleset.Modern)));
    }

    [Fact]
    public void EachStingOfTwineedleMayPoison()
    {
        // The side effect runs for each hit; what the type did is told once, after the last, and then the count
        var blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(Mon("Beedrill", 50, "Twineedle"), blastoise, Calm().Force(RollKind.SideEffect, 19)));
        InOrder(said, "Beedrill used Twineedle!", "Foe Blastoise was poisoned!", "Hit 2 times!");
        Assert.Equal(StatusCondition.Poison, blastoise.Status);
    }

    [Fact]
    public void TripleKickGrowsWithEachKickAndEachRollsItsOwnAccuracy()
    {
        // 10, 20 and 30 power: 135 × 10 × 22 / 105 / 50 = 5, + 2 = 7, × 15 / 10 = 10; 11, 13, 19; 16, 18, 27
        var blastoise = Mon("Blastoise", 50);
        Assert.Contains("Hit 3 times!", Turn(Wild(Mon("Machamp", 50, "Triple Kick"), blastoise)));
        Assert.Equal(139 - 56, blastoise.CurrentHP);

        // A kick that misses ends them, with no miss told
        blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(Mon("Machamp", 50, "Triple Kick"), blastoise, Calm().Force(RollKind.Accuracy, 0, 0, 95)));
        Assert.Contains("Hit 2 times!", said);
        Assert.DoesNotContain("Machamp's attack missed!", said);
        Assert.Equal(139 - 29, blastoise.CurrentHP);
    }

    [Fact]
    public void BeatUpIsOneHitForEachOfThePartyThatStandsHealthy()
    {
        // BtlCmd_BeatUp: base Attack × 10 × 22 / the target's base Defense / 50, + 2, with no type to it.
        // Machamp 130 × 10 × 22 / 100 / 50 = 5, + 2 = 7; Snorlax 110: 4, + 2 = 6; Alakazam is asleep and sits out
        var alakazam = Mon("Alakazam", 50);
        alakazam.Status = StatusCondition.Sleep;
        alakazam.SleepTurns = 5;
        var blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(Mon("Machamp", 50, "Beat Up"), blastoise, bench: new[] { Mon("Snorlax", 50), alakazam }));
        InOrder(said, "Machamp used Beat Up!", "Machamp's attack!", "Snorlax's attack!");
        Assert.DoesNotContain("Alakazam's attack!", said);
        Assert.DoesNotContain(said, s => s.StartsWith("Hit "));
        Assert.Equal(139 - 13, blastoise.CurrentHP);

        // No type to it: a Ghost takes it (130 × 10 × 22 / 60 / 50 = 9, + 2 = 11; 8, + 2 = 10)
        var gengar = Mon("Gengar", 50);
        Turn(Wild(Mon("Machamp", 50, "Beat Up"), gengar, bench: Mon("Snorlax", 50)));
        Assert.Equal(120 - 21, gengar.CurrentHP);
    }

    // ================================================================== a fixed amount

    [Fact]
    public void AFixedAmountIgnoresEverythingButAnImmunity()
    {
        var blastoise = Mon("Blastoise", 50);
        Turn(Wild(Mon("Alakazam", 50, "Sonic Boom"), blastoise));
        Assert.Equal(139 - 20, blastoise.CurrentHP);
        Turn(Wild(Mon("Alakazam", 50, "Dragon Rage"), blastoise = Mon("Blastoise", 50)));
        Assert.Equal(139 - 40, blastoise.CurrentHP);
        Turn(Wild(Mon("Machamp", 50, "Seismic Toss"), blastoise = Mon("Blastoise", 50)));
        Assert.Equal(139 - 50, blastoise.CurrentHP);

        // Behind Reflect all the same, and never a critical hit
        blastoise = Idling(Mon("Blastoise", 50, "Reflect"));
        Turn(Wild(blastoise, Mon("Machamp", 50, "Seismic Toss"), Calm().Force(RollKind.Critical, 0)));
        Assert.Equal(139 - 50, blastoise.CurrentHP);

        // The type still says whom it can't touch
        Assert.Contains("It doesn't affect Foe Gengar...", Turn(Wild(Mon("Machamp", 50, "Seismic Toss"), Mon("Gengar", 50))));
        Assert.Contains("It doesn't affect Foe Snorlax...", Turn(Wild(Mon("Gengar", 50, "Night Shade"), Mon("Snorlax", 50))));
    }

    [Fact]
    public void SuperFangEndeavorAndPsywaveTakeTheirOwnAmounts()
    {
        // Super Fang: Divide(139, 2) = 69. Endeavor: down to the user's own HP, nothing when the target is as low.
        // Psywave: the level times (Random 10, 5) tenths
        var blastoise = Mon("Blastoise", 50);
        Turn(Wild(Mon("Machamp", 50, "Super Fang"), blastoise));
        Assert.Equal(139 - 69, blastoise.CurrentHP);

        var pikachu = Mon("Pikachu", 50, "Endeavor");
        pikachu.CurrentHP = 10;
        blastoise = Mon("Blastoise", 50);
        Turn(Wild(pikachu, blastoise));
        Assert.Equal(10, blastoise.CurrentHP);
        Assert.Contains("But it failed!", Turn(Wild(pikachu, blastoise)));

        blastoise = Mon("Blastoise", 50);
        Turn(Wild(Mon("Alakazam", 50, "Psywave"), blastoise, Calm().Force(RollKind.Power, 10)));
        Assert.Equal(139 - 75, blastoise.CurrentHP);
        Turn(Wild(Mon("Alakazam", 50, "Psywave"), blastoise = Mon("Blastoise", 50), Calm().Force(RollKind.Power, 0)));
        Assert.Equal(139 - 25, blastoise.CurrentHP);
    }

    [Fact]
    public void AOneHitKnockoutGoesByLevelAndChance()
    {
        // BtlCmd_TryOHKOMove: a roll of 100 under the accuracy plus the levels' difference, and never against a
        // higher level; Sturdy stands against it; Lock-On makes it sure
        var blastoise = Mon("Blastoise", 50);
        InOrder(Turn(Wild(Mon("Machamp", 50, "Fissure"), blastoise, Calm().Force(RollKind.Accuracy, 29))), "Machamp used Fissure!", "It's a one-hit KO!", "Foe Blastoise fainted!");
        Assert.Contains("Machamp's attack missed!", Turn(Wild(Mon("Machamp", 50, "Fissure"), Mon("Blastoise", 50), Calm().Force(RollKind.Accuracy, 30))));
        Assert.Contains("Foe Blastoise is unaffected!", Turn(Wild(Mon("Machamp", 50, "Fissure"), Mon("Blastoise", 60))));
        Assert.Contains("Foe Steelix was protected by Sturdy!", Turn(Wild(Mon("Machamp", 50, "Fissure"), With(Mon("Steelix", 50), ability: "Sturdy"))));
        Assert.Contains("It doesn't affect Foe Skarmory...", Turn(Wild(Mon("Machamp", 50, "Fissure"), Mon("Skarmory", 50))));

        var core = Wild(Mon("Machamp", 50, "Lock-On", "Fissure"), Mon("Blastoise", 50), Calm().Force(RollKind.Accuracy, 99));
        Turn(core, 0);
        Assert.Contains("It's a one-hit KO!", Turn(core, 1));
    }

    // ================================================================== power by the numbers

    [Fact]
    public void FlailGrowsAsItsUserWeakens()
    {
        // sHPPixelsToFlailPower over a bar of 64: 150 HP of 150 is 64 pixels (20 power), 48 is 20 (80), 2 is 1 (200).
        // 135 × 20 × 22 / 105 / 50 = 11, + 2 = 13; × 80: 45, + 2 = 47; × 200: 113, + 2 = 115
        int Damage(int hp)
        {
            var machamp = Mon("Machamp", 50, "Flail");
            machamp.CurrentHP = hp;
            var blastoise = Mon("Blastoise", 50);
            Turn(Wild(machamp, blastoise));
            return 139 - blastoise.CurrentHP;
        }
        Assert.Equal(13, Damage(150));
        Assert.Equal(47, Damage(48));
        Assert.Equal(115, Damage(2));
    }

    [Fact]
    public void WringOutAndEruptionGoByHp()
    {
        // Wring Out: 1 + 120 × the target's HP over its most: 121 at full (140 × 121 × 22 / 110 / 50 = 67, + 2 = 69),
        // 60 at 69 of 139 (33, + 2 = 35). Eruption: 150 × the user's: 150 at full (84, + 2 = 86, half against Water: 43),
        // 30 at 23 of 115 (16, + 2 = 18, 9)
        var blastoise = Mon("Blastoise", 50);
        Turn(Wild(Mon("Alakazam", 50, "Wring Out"), blastoise));
        Assert.Equal(139 - 69, blastoise.CurrentHP);
        blastoise.CurrentHP = 69;
        Turn(Wild(Mon("Alakazam", 50, "Wring Out"), blastoise));
        Assert.Equal(69 - 35, blastoise.CurrentHP);

        var alakazam = Mon("Alakazam", 50, "Eruption");
        Turn(Wild(alakazam, blastoise = Mon("Blastoise", 50)));
        Assert.Equal(139 - 43, blastoise.CurrentHP);
        alakazam.CurrentHP = 23;
        Turn(Wild(alakazam, blastoise = Mon("Blastoise", 50)));
        Assert.Equal(139 - 9, blastoise.CurrentHP);
    }

    [Fact]
    public void LowKickGoesByWeightAndGyroBallBySpeed()
    {
        // sWeightToPower in tenths of a kilogram: Blastoise 855 is 80 power (135 × 80 × 22 / 105 / 50 = 45, + 2 = 47,
        // × 15 / 10 = 70), Pikachu 60 is 20 (135 × 20 × 22 / 35 / 50 = 33, + 2 = 35, 52)
        var blastoise = Mon("Blastoise", 50);
        Turn(Wild(Mon("Machamp", 50, "Low Kick"), blastoise));
        Assert.Equal(139 - 70, blastoise.CurrentHP);
        var pikachu = Mon("Pikachu", 50);
        Turn(Wild(Mon("Machamp", 50, "Low Kick"), pikachu));
        Assert.Equal(95 - 52, pikachu.CurrentHP);

        // Gyro Ball: 1 + 25 × 125 / 35 = 90 for Snorlax against Alakazam (115 × 90 × 22 / 50 / 50 = 91, + 2 = 93);
        // the other way 1 + 25 × 35 / 125 = 8 (55 × 8 × 22 / 70 / 50 = 2, + 2 = 4)
        var alakazam = Mon("Alakazam", 50);
        Turn(Wild(Mon("Snorlax", 50, "Gyro Ball"), alakazam));
        Assert.Equal(115 - 93, alakazam.CurrentHP);
        var snorlax = Mon("Snorlax", 50);
        Turn(Wild(Mon("Alakazam", 50, "Gyro Ball"), snorlax));
        Assert.Equal(220 - 4, snorlax.CurrentHP);
    }

    [Fact]
    public void ReturnAndFrustrationGoByFriendship()
    {
        // Friendship × 10 / 25: 255 is 102 power (135 × 102 × 22 / 105 / 50 = 57, + 2 = 59); Frustration the other way round
        int Damage(string move, int friendship)
        {
            var machamp = Mon("Machamp", 50, move);
            machamp.Friendship = friendship;
            var blastoise = Mon("Blastoise", 50);
            Turn(Wild(machamp, blastoise));
            return 139 - blastoise.CurrentHP;
        }
        Assert.Equal(59, Damage("Return", 255));
        Assert.Equal(59, Damage("Frustration", 0));
        Assert.Equal(2, Damage("Return", 0));
    }

    [Fact]
    public void PunishmentGrowsWithTheTargetsBoostsAndTrumpCardWithItsLastPp()
    {
        // Punishment: 60 + 20 a stage raised, up to 200: three stages are 120, against a Defense one stage up (157):
        // 135 × 120 × 22 / 157 / 50 = 45, + 2 = 47; none, 60 (135 × 60 × 22 / 105 / 50 = 33, + 2 = 35)
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(Mon("Machamp", 50, "Punishment"), blastoise);
        blastoise.StatStages[StatType.Attack] = 2;
        blastoise.StatStages[StatType.Defense] = 1;
        Turn(core);
        Assert.Equal(139 - 47, blastoise.CurrentHP);
        Turn(Wild(Mon("Machamp", 50, "Punishment"), blastoise = Mon("Blastoise", 50)));
        Assert.Equal(139 - 35, blastoise.CurrentHP);

        // Trump Card by the PP left once used (sCurrentPPScaledPower): 4 left is 40 power (140 × 40 × 22 / 110 / 50 = 22,
        // + 2 = 24), none left 200 (112, + 2 = 114)
        var alakazam = Mon("Alakazam", 50, "Trump Card");
        Turn(Wild(alakazam, blastoise = Mon("Blastoise", 50)));
        Assert.Equal(139 - 24, blastoise.CurrentHP);
        alakazam = Mon("Alakazam", 50, "Trump Card");
        alakazam.Moves[0].CurrentPP = 1;
        Turn(Wild(alakazam, blastoise = Mon("Blastoise", 50)));
        Assert.Equal(139 - 114, blastoise.CurrentHP);
    }

    [Fact]
    public void HiddenPowerTakesItsTypeAndPowerFromTheIvs()
    {
        // BtlCmd_CalcHiddenPowerParams: IVs of 31 everywhere make every bit, so 63 × 40 / 63 + 30 = 70 power and
        // type 63 × 15 / 63 + 1 = 16, past the original's "mystery" type: Dark. Such an Alakazam has 155 Sp. Atk:
        // 155 × 70 × 22 / 90 / 50 = 53, + 2 = 55, twice against Psychic = 110. IVs of 0: Fighting at 30 power
        // (140 × 30 × 22 / 90 / 50 = 20, + 2 = 22, half = 11). By the modern rules the power is 60 (45, + 2 = 47, 94)
        static Pokemon Gifted()
        {
            var p = Mon("Alakazam", 50, "Hidden Power");
            p.IvHP = p.IvAttack = p.IvDefense = p.IvSpAttack = p.IvSpDefense = p.IvSpeed = 31;
            p.CalculateStats();
            return p;
        }
        var target = Mon("Alakazam", 50);
        var core = Wild(Gifted(), target);
        Turn(core);
        Assert.Equal(115 - 110, target.CurrentHP);
        Assert.Contains(Shown(core), m => m.Move == "Hidden Power" && m.Type == PokemonType.Dark);

        core = Wild(Mon("Alakazam", 50, "Hidden Power"), target = Mon("Alakazam", 50));
        Turn(core);
        Assert.Equal(115 - 11, target.CurrentHP);
        Assert.Contains(Shown(core), m => m.Move == "Hidden Power" && m.Type == PokemonType.Fighting);

        Turn(Wild(Gifted(), target = Mon("Alakazam", 50), rules: Ruleset.Modern));
        Assert.Equal(115 - 94, target.CurrentHP);
    }

    [Fact]
    public void PresentIsAGiftOfThreeSizesOrAHeal()
    {
        // BtlCmd_Present on a draw of 256: under 102 it is 40 power (135 × 40 × 22 / 105 / 50 = 22, + 2 = 24), under
        // 178 80 (47), under 204 120 (69), and from 204 a quarter of the target's HP given back (Divide(139, 4) = 34)
        int Left(int roll, int hp = 139)
        {
            var blastoise = Mon("Blastoise", 50);
            blastoise.CurrentHP = hp;
            Turn(Wild(Mon("Machamp", 50, "Present"), blastoise, Calm().Force(RollKind.Power, roll)));
            return blastoise.CurrentHP;
        }
        Assert.Equal(139 - 24, Left(0));
        Assert.Equal(139 - 47, Left(102));
        Assert.Equal(139 - 69, Left(178));
        Assert.Equal(100 + 34, Left(204, hp: 100));
    }

    [Fact]
    public void JudgmentTakesTheTypeOfThePlate()
    {
        // Against a Venusaur of level 100 (270 HP, 205 Sp. Def): 125 × 100 × 22 / 205 / 50 = 26, + 2 = 28. With a
        // Flame Plate the move is Fire and so is Arceus (Multitype, plan 06 · R7): its own type's bonus makes 42,
        // twice as strong against Grass, 84. With no plate both are Normal: 42. Without Multitype the plate still
        // makes the move Fire, but not Arceus: 28 × 2 = 56
        var venusaur = Mon("Venusaur", 100);
        var arceus = With(Mon("Arceus", 50, "Judgment"), item: "Flame Plate");
        var said = Turn(Wild(arceus, venusaur));
        Assert.Contains("It's super effective!", said);
        Assert.Equal(270 - 84, venusaur.CurrentHP);
        Turn(Wild(Mon("Arceus", 50, "Judgment"), venusaur = Mon("Venusaur", 100)));
        Assert.Equal(270 - 42, venusaur.CurrentHP);
        Turn(Wild(With(Mon("Arceus", 50, "Judgment"), ability: "Pressure", item: "Flame Plate"), venusaur = Mon("Venusaur", 100)));
        Assert.Equal(270 - 56, venusaur.CurrentHP);
    }

    [Fact]
    public void BrinePaybackAssuranceAndRevengeDoubleInTheirMoment()
    {
        // Brine, 65 power, from Pikachu on Snorlax: 55 × 65 × 22 / 115 / 50 = 13, + 2 = 15; at half the target's HP
        // or less it is 130: 27, + 2 = 29
        var snorlax = Mon("Snorlax", 50);
        Turn(Wild(Mon("Pikachu", 50, "Brine"), snorlax));
        Assert.Equal(220 - 15, snorlax.CurrentHP);
        snorlax.CurrentHP = 110;
        Turn(Wild(Mon("Pikachu", 50, "Brine"), snorlax));
        Assert.Equal(110 - 29, snorlax.CurrentHP);

        // Payback, 50: 115 × 50 × 22 / 105 / 50 = 24, + 2 = 26 before the target's action, 100 power after it: 48, + 2 = 50.
        // Blastoise is the faster; Trick Room's priority puts it after Snorlax instead
        var blastoise = Mon("Blastoise", 50);
        Turn(Wild(Mon("Snorlax", 50, "Payback"), blastoise));
        Assert.Equal(139 - 50, blastoise.CurrentHP);
        Turn(Wild(Mon("Snorlax", 50, "Payback"), blastoise = Mon("Blastoise", 50, "Trick Room")));
        Assert.Equal(139 - 26, blastoise.CurrentHP);

        // Assurance, 50: twice once the target has lost HP this turn, to anything (Double-Edge's recoil)
        Turn(Wild(Mon("Snorlax", 50, "Assurance"), blastoise = Mon("Blastoise", 50)));
        Assert.Equal(139 - 26, blastoise.CurrentHP);
        blastoise = Mon("Blastoise", 50, "Double-Edge");
        Turn(Wild(Mon("Snorlax", 50, "Assurance"), blastoise));
        // 88 × 120 × 22 / 70 / 50 = 66, + 2 = 68 to Snorlax, 22 back; then 50
        Assert.Equal(139 - 22 - 50, blastoise.CurrentHP);

        // Revenge, 60: 115 × 60 × 22 / 105 / 50 = 28, + 2 = 30; twice against whoever hurt the user this turn: 57, + 2 = 59
        Turn(Wild(Mon("Snorlax", 50, "Revenge"), blastoise = Mon("Blastoise", 50)));
        Assert.Equal(139 - 30, blastoise.CurrentHP);
        Turn(Wild(Mon("Snorlax", 50, "Revenge"), blastoise = Mon("Blastoise", 50, "Tackle")));
        Assert.Equal(139 - 59, blastoise.CurrentHP);
    }

    [Fact]
    public void FuryCutterDoublesWithEachUseUntilItsUserIsStopped()
    {
        // 10, 20, 40, 80, 160 and 160 again against Steelix (135 × 10 × 22 / 205 / 50 = 2, + 2 = 4, half against Steel:
        // 2; then 3, 6, 12, 24, 24). A miss starts it over
        var steelix = Mon("Steelix", 50);
        var core = Wild(Mon("Machamp", 50, "Fury Cutter"), steelix, Calm().Force(RollKind.Accuracy, 0, 0, 0, 0, 0, 0, 95, 0));
        int[] expected = { 2, 3, 6, 12, 24, 24 };
        int hp = 135;
        foreach (int damage in expected)
        {
            Turn(core);
            Assert.Equal(hp - damage, steelix.CurrentHP);
            hp -= damage;
        }
        Assert.Contains("Machamp's attack missed!", Turn(core));
        Turn(core);
        Assert.Equal(hp - 2, steelix.CurrentHP);
    }

    [Fact]
    public void RolloutRollsForFiveTurnsDoublingEachTime()
    {
        // 30, 60, 120 and 240 power: 135 × 30 × 22 / 105 / 50 = 16, + 2 = 18; 33, + 2 = 35; 67, + 2 = 69; then 137, which
        // is the end of Blastoise. Its user isn't asked while it rolls
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(Mon("Machamp", 50, "Rollout"), blastoise);
        Turn(core);
        Assert.Equal(139 - 18, blastoise.CurrentHP);
        Assert.Equal(new[] { Foe }, Assert.IsType<ActionRequest>(core.Request).Places);
        Turn(core);
        Assert.Equal(139 - 18 - 35, blastoise.CurrentHP);
        Turn(core);
        Assert.Equal(139 - 18 - 35 - 69, blastoise.CurrentHP);
        Assert.Contains("Foe Blastoise fainted!", Turn(core));

        // Defense Curl before it doubles it all; a miss ends it, and its user chooses again
        blastoise = Mon("Blastoise", 50);
        core = Wild(Mon("Machamp", 50, "Defense Curl", "Rollout"), blastoise, Calm().Force(RollKind.Accuracy, 0, 95));
        Turn(core, 0);
        Turn(core, 1);
        Assert.Equal(139 - 35, blastoise.CurrentHP);
        Assert.Contains("Machamp's attack missed!", Turn(core));
        Assert.Equal(new[] { Mine, Foe }, Assert.IsType<ActionRequest>(core.Request).Places);
    }

    // ================================================================== what costs the user everything

    [Fact]
    public void ExplosionCostsItsUserEverythingWhateverComesOfIt()
    {
        // Self-Destruct, 200 power, against half of Steelix's 205 Defense: 115 × 200 × 22 / 102 / 50 = 99, + 2 = 101,
        // × 15 / 10 = 151, half against Steel = 75; the user goes down once the hit is told
        var snorlax = Mon("Snorlax", 50, "Self-Destruct");
        var steelix = Mon("Steelix", 50);
        var said = Turn(Wild(snorlax, steelix));
        InOrder(said, "Snorlax used Self-Destruct!", "It's not very effective...", "Snorlax fainted!");
        Assert.Equal(135 - 75, steelix.CurrentHP);

        // Damp anywhere on the field stops it, and its user keeps its HP; a Protect doesn't save the user
        snorlax = Mon("Snorlax", 50, "Self-Destruct");
        Assert.Contains("Foe Golduck's Damp prevents Snorlax from using Self-Destruct!", Turn(Wild(snorlax, With(Mon("Golduck", 50), ability: "Damp"))));
        Assert.Equal(220, snorlax.CurrentHP);
        InOrder(Turn(Wild(Mon("Snorlax", 50, "Explosion"), Mon("Alakazam", 50, "Protect"))), "Foe Alakazam protected itself!", "Snorlax fainted!");

        // By the modern rules the Defense stands whole: 115 × 200 × 22 / 205 / 50 = 49, + 2 = 51, 76, 38
        steelix = Mon("Steelix", 50);
        Turn(Wild(Mon("Snorlax", 50, "Self-Destruct"), steelix, rules: Ruleset.Modern));
        Assert.Equal(135 - 38, steelix.CurrentHP);
    }

    [Fact]
    public void MementoAndBellyDrumPayWithTheUsersOwnHp()
    {
        var gengar = Mon("Gengar", 50, "Memento");
        var blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(gengar, blastoise, bench: Mon("Snorlax", 50)));
        InOrder(said, "Foe Blastoise's Attack harshly fell!", "Foe Blastoise's Sp. Atk harshly fell!", "Gengar fainted!");
        Assert.Equal((-2, -2), (blastoise.StatStages[StatType.Attack], blastoise.StatStages[StatType.SpAttack]));

        // subscript_belly_drum: half its HP for Attack at the top; nothing with half or less
        var snorlax = Mon("Snorlax", 50, "Belly Drum");
        var core = Wild(snorlax, Mon("Blastoise", 50));
        Assert.Contains("Snorlax cut its own HP and maximized its Attack!", Turn(core));
        Assert.Equal((110, 6), (snorlax.CurrentHP, snorlax.StatStages[StatType.Attack]));
        Assert.Contains("But it failed!", Turn(core));
        Assert.Equal(110, snorlax.CurrentHP);
    }

    // ================================================================== hits that go by the turn

    [Fact]
    public void FalseSwipeLeavesOneAndTriAttackGivesOneOfThree()
    {
        var blastoise = Mon("Blastoise", 50);
        blastoise.CurrentHP = 5;
        Turn(Wild(Mon("Machamp", 50, "False Swipe"), blastoise));
        Assert.Equal(1, blastoise.CurrentHP);

        // Blastoise moves after Alakazam, so a freeze would be rolled against at once: that roll is held too
        StatusCondition After(int pick, int chance = 19)
        {
            var target = Mon("Blastoise", 50);
            Turn(Wild(Mon("Alakazam", 50, "Tri Attack"), target, Calm().Force(RollKind.SideEffect, chance).Force(RollKind.Pick, pick).Force(RollKind.Thaw, 1)));
            return target.Status;
        }
        Assert.Equal(StatusCondition.Burn, After(0));
        Assert.Equal(StatusCondition.Freeze, After(1));
        Assert.Equal(StatusCondition.Paralyze, After(2));
        Assert.Equal(StatusCondition.None, After(0, chance: 20));
    }

    [Fact]
    public void FakeOutWorksOnItsUsersFirstTurnOnly()
    {
        var core = Wild(Mon("Machamp", 50, "Fake Out"), Mon("Blastoise", 50, "Tackle"), bench: Mon("Snorlax", 50));
        Assert.Contains("Foe Blastoise flinched!", Turn(core));
        Assert.Contains("But it failed!", Turn(core));

        // Out and back in: the turn after it comes in is its first again
        SwitchTo(core, 1);
        SwitchTo(core, 0);
        Assert.Contains("Foe Blastoise flinched!", Turn(core));
        Assert.Contains("But it failed!", Turn(core));
    }

    [Fact]
    public void SuckerPunchOnlyBeatsAnAttackStillToCome()
    {
        // BtlCmd_TrySuckerPunch: the target must be about to use a damaging move. Snorlax is the slower, so with
        // Quick Attack's priority against Sucker Punch's the target has moved first
        var blastoise = Mon("Blastoise", 50, "Tackle");
        Turn(Wild(Mon("Snorlax", 50, "Sucker Punch"), blastoise));
        Assert.True(blastoise.CurrentHP < 139);
        Assert.Contains("But it failed!", Turn(Wild(Mon("Snorlax", 50, "Sucker Punch"), Mon("Blastoise", 50, "Growl"))));
        Assert.Contains("But it failed!", Turn(Wild(Mon("Snorlax", 50, "Sucker Punch"), Mon("Blastoise", 50, "Quick Attack"))));
    }

    [Fact]
    public void FocusPunchNeedsItsUserUnhurt()
    {
        // The focus is told first in the turn; the punch lands unless anything hurt its user meanwhile. A Growl
        // doesn't, though it leaves the Attack a stage down (76): 76 × 150 × 22 / 105 / 50 = 47, + 2 = 49
        var blastoise = Mon("Blastoise", 50, "Growl");
        var said = Turn(Wild(Mon("Snorlax", 50, "Focus Punch"), blastoise));
        InOrder(said, "Go! Snorlax!", "Snorlax is tightening its focus!", "Foe Blastoise used Growl!", "Snorlax used Focus Punch!");
        Assert.Equal(139 - 49, blastoise.CurrentHP);

        blastoise = Mon("Blastoise", 50, "Tackle");
        said = Turn(Wild(Mon("Snorlax", 50, "Focus Punch"), blastoise));
        InOrder(said, "Snorlax is tightening its focus!", "Foe Blastoise used Tackle!", "Snorlax lost its focus and couldn't move!");
        Assert.Equal(139, blastoise.CurrentHP);
    }

    [Fact]
    public void LastResortNeedsEveryOtherMoveUsed()
    {
        var core = Wild(Mon("Machamp", 50, "Last Resort", "Tackle", "Growl"), Mon("Blastoise", 50));
        Assert.Contains("But it failed!", Turn(core, 0));
        Turn(core, 1);
        Assert.Contains("But it failed!", Turn(core, 0));
        Turn(core, 2);
        Assert.DoesNotContain("But it failed!", Turn(core, 0));

        // Alone it never works
        Assert.Contains("But it failed!", Turn(Wild(Mon("Machamp", 50, "Last Resort"), Mon("Blastoise", 50))));
    }

    [Fact]
    public void PayDayScattersCoinsPickedUpWithTheWin()
    {
        // Five times the user's level a use (subscript_pay_day), given with the winnings (AddPayDayMoney).
        // 135 × 40 × 22 / 35 / 50 = 67, + 2 = 69: two uses see Pikachu off
        var core = Wild(Mon("Machamp", 50, "Pay Day"), Mon("Pikachu", 50));
        Assert.Contains("Coins scattered everywhere!", Turn(core));
        Assert.Equal(250, core.PayDayMoney);
        var said = Turn(core);
        InOrder(said, "Foe Pikachu fainted!", "Lucas picked up $500!");
        Assert.Equal(BattleResult.PlayerVictory, core.Result);

        // A foe's Pay Day scatters nothing for the player
        core = Wild(Mon("Machamp", 50), Mon("Pikachu", 50, "Pay Day"));
        Turn(core);
        Assert.Equal(0, core.PayDayMoney);
    }

    // ================================================================== the counters

    [Fact]
    public void CounterMirrorCoatAndMetalBurstGiveBackWhatWasTaken()
    {
        // Strength on Snorlax: 135 × 80 × 22 / 70 / 50 = 67, + 2 = 69; Counter gives twice that back
        var machamp = Mon("Machamp", 50, "Strength");
        var core = Wild(Mon("Snorlax", 50, "Counter"), machamp);
        InOrder(Turn(core), "Foe Machamp used Strength!", "Snorlax used Counter!");
        Assert.Equal(150 - 138, machamp.CurrentHP);

        // Only a physical hit; Mirror Coat only a special one (Psychic: 140 × 90 × 22 / 115 / 50 = 48, + 2 = 50, × 15 / 10 = 75, twice is 150)
        Assert.Contains("But it failed!", Turn(Wild(Mon("Snorlax", 50, "Counter"), Mon("Alakazam", 50, "Psychic"))));
        var alakazam = Mon("Alakazam", 50, "Psychic");
        Assert.Contains("Foe Alakazam fainted!", Turn(Wild(Mon("Snorlax", 50, "Mirror Coat"), alakazam)));
        Assert.Contains("But it failed!", Turn(Wild(Mon("Snorlax", 50, "Mirror Coat"), Mon("Machamp", 50, "Strength"))));

        // Metal Burst: half as much again, either way; nothing taken, nothing given
        machamp = Mon("Machamp", 50, "Strength");
        Turn(Wild(Mon("Snorlax", 50, "Metal Burst"), machamp));
        Assert.Equal(150 - 103, machamp.CurrentHP);
        Assert.Contains("But it failed!", Turn(Wild(Mon("Snorlax", 50, "Metal Burst"), Mon("Machamp", 50))));

        // Counter is a Fighting move still: a Ghost can't be touched by it
        var gengar = Mon("Gengar", 50, "Sucker Punch");
        Assert.Contains("It doesn't affect Foe Gengar...", Turn(Wild(Mon("Snorlax", 50, "Counter"), gengar)));
    }

    // ================================================================== Stockpile's three

    [Fact]
    public void StockpileSpitUpAndSwallow()
    {
        var snorlax = Mon("Snorlax", 50, "Stockpile", "Spit Up", "Swallow");
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(snorlax, blastoise, Calm().Force(RollKind.Damage, 15));
        InOrder(Turn(core, 0), "Snorlax stockpiled 1!", "Snorlax's Defense rose!", "Snorlax's Sp. Def rose!");
        Turn(core, 0);
        Assert.Equal((2, 2), (snorlax.StatStages[StatType.Defense], snorlax.StatStages[StatType.SpDefense]));

        // Spit Up at two: 200 power with no roll off it (70 × 200 × 22 / 110 / 50 = 56, + 2 = 58, × 15 / 10 = 87), and the stockpile is gone
        var said = Turn(core, 1);
        InOrder(said, "Snorlax used Spit Up!", "Snorlax's stockpiled effect wore off!");
        Assert.Equal(139 - 87, blastoise.CurrentHP);
        Assert.Equal((0, 0), (snorlax.StatStages[StatType.Defense], snorlax.StatStages[StatType.SpDefense]));
        Assert.Contains("But it failed to spit up a thing!", Turn(core, 1));

        // Three is the most; Swallow at one is a quarter back, at three all of it
        Turn(core, 0);
        Turn(core, 0);
        Turn(core, 0);
        Assert.Contains("But it failed!", Turn(core, 0));
        snorlax.CurrentHP = 10;
        Assert.Contains("Snorlax regained health!", Turn(core, 2));
        Assert.Equal(220, snorlax.CurrentHP);
        Turn(core, 0);
        snorlax.CurrentHP = 100;
        Turn(core, 2);
        Assert.Equal(155, snorlax.CurrentHP);
        Assert.Contains("But it failed to swallow a thing!", Turn(core, 2));
    }

    // ================================================================== the rest

    [Fact]
    public void PainSplitAcupressurePsychUpAndCaptivate()
    {
        var snorlax = Mon("Snorlax", 50, "Pain Split");
        var blastoise = Mon("Blastoise", 50);
        blastoise.CurrentHP = 20;
        Assert.Contains("The battlers shared their pain!", Turn(Wild(snorlax, blastoise)));
        Assert.Equal((120, 120), (snorlax.CurrentHP, blastoise.CurrentHP));

        snorlax = Mon("Snorlax", 50, "Acupressure");
        Assert.Contains("Snorlax's Attack sharply rose!", Turn(Wild(snorlax, Mon("Blastoise", 50), Calm().Force(RollKind.Pick, 0))));
        Assert.Equal(2, snorlax.StatStages[StatType.Attack]);

        snorlax = Mon("Snorlax", 50, "Psych Up");
        blastoise = Mon("Blastoise", 50);
        var core = Wild(snorlax, blastoise);
        blastoise.StatStages[StatType.Attack] = 2;
        blastoise.StatStages[StatType.Defense] = -1;
        Assert.Contains("Snorlax copied Foe Blastoise's stat changes!", Turn(core));
        Assert.Equal((2, -1), (snorlax.StatStages[StatType.Attack], snorlax.StatStages[StatType.Defense]));

        var machamp = Mon("Machamp", 50);
        Assert.Contains("Foe Machamp's Sp. Atk harshly fell!", Turn(Wild(Of(Gender.Female, "Blastoise", 50, "Captivate"), machamp)));
        Assert.Contains("It failed to affect Foe Machamp!", Turn(Wild(Mon("Blastoise", 50, "Captivate"), Mon("Machamp", 50))));
        Assert.Contains("Foe Machamp's Oblivious made Captivate ineffective!", Turn(Wild(Of(Gender.Female, "Blastoise", 50, "Captivate"), With(Mon("Machamp", 50), ability: "Oblivious"))));
    }

    [Fact]
    public void MagnitudeIsDrawnOnceAUse()
    {
        // BtlCmd_CalcMagnitudePower on a draw of 100: under 5 it is Magnitude 4 at 10 power, from 95 Magnitude 10 at
        // 150 (135 × 150 × 22 / 105 / 50 = 84, + 2 = 86); twice as hard on a target underground
        var blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(Mon("Machamp", 50, "Magnitude"), blastoise, Calm().Force(RollKind.Power, 99)));
        InOrder(said, "Machamp used Magnitude!", "Magnitude 10!");
        Assert.Equal(139 - 86, blastoise.CurrentHP);
        Assert.Contains("Magnitude 4!", Turn(Wild(Mon("Machamp", 50, "Magnitude"), Mon("Blastoise", 50), Calm().Force(RollKind.Power, 0))));

        // 300 power against a Pokémon under the ground: 169, + 2 = 171, more than Blastoise has
        blastoise = Mon("Blastoise", 50, "Dig");
        var core = Wild(blastoise, Mon("Machamp", 50, "Magnitude"), Calm().Force(RollKind.Power, 99));
        Assert.Contains("Blastoise fainted!", Turn(core));
    }
}
