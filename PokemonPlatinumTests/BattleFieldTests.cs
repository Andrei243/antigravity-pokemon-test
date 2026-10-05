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
/// Conditions and the field (plan 06 · R3), on the battle's rules alone: both sides are answered for from outside,
/// so a test says exactly what each Pokémon does. One rule to a test, the rolls it hangs on fixed by kind, and the
/// numbers worked out from Platinum's own code (the decompilation's <c>battle_controller_player.c</c>,
/// <c>battle_script.c</c>, <c>battle_lib.c</c> and the scripts under <c>res/battle/scripts</c>).
/// <para>
/// At level 50 with no IVs, no EVs and a neutral nature a stat is its base + 5 and HP its base + 60:
/// </para>
/// <code>
///            HP   Atk  Def  SpA  SpD  Spe
/// Alakazam   115   55   50  140   90  125   Psychic
/// Gengar     120   70   65  135   80  115   Ghost / Poison
/// Garchomp   168  135  100   85   90  107   Dragon / Ground
/// Charizard  138   89   83  114   90  105   Fire / Flying
/// Venusaur   140   87   88  105  105   85   Grass / Poison
/// Blastoise  139   88  105   90  110   83   Water
/// Skarmory   125   85  145   45   75   75   Steel / Flying
/// Umbreon    155   70  115   65  135   70   Dark
/// Tyranitar  160  139  115  100  105   66   Rock / Dark
/// Machamp    150  135   85   70   90   60   Fighting
/// Snorlax    220  115   70   70  115   35   Normal
/// </code>
/// A hit is stat × power × 22 / defence / 50, then a burn's or a screen's halving, the weather, + 2, the critical
/// multiplier, the roll, the user's own type (× 15 / 10) and the target's types, each step rounded down. The
/// faster Pokémon moves first, so who is faster is part of every scenario.
/// </summary>
public class BattleFieldTests
{
    // ================================================================== conditions that stop a move

    [Fact]
    public void ASleepLastsOneToFourTurnsAndTheSleeperMovesOnTheTurnItWakes()
    {
        // subscript_fall_asleep: the counter is Random 3, 2 (2 to 5); it drops by one each time the sleeper's
        // turn comes (CheckStatusDisruption), and at nothing the Pokémon wakes and goes on with its move
        int TurnsAsleep(int roll)
        {
            var machamp = Mon("Machamp", 50, "Strength");
            var core = Wild(machamp, Mon("Blastoise", 50), Calm().Force(RollKind.SleepTurns, roll));
            Assert.True(core.TryInflictStatus(core.At(Mine), StatusCondition.Sleep, core.At(Foe)));
            Assert.Equal(2 + roll, machamp.SleepTurns);
            for (int turns = 0; turns < 10; turns++)
            {
                var said = Turn(core);
                if (!said.Contains("Machamp is fast asleep."))
                {
                    InOrder(said, "Machamp woke up!", "Machamp used Strength!");
                    return turns;
                }
            }
            return -1;
        }
        Assert.Equal(1, TurnsAsleep(0));
        Assert.Equal(4, TurnsAsleep(3));
    }

    [Fact]
    public void AnInfatuatedPokemonCantMoveHalfTheTime()
    {
        // Blastoise (83) is faster than Machamp (60). BtlCmd_TryAttract: the other gender, and not twice;
        // CheckStatusDisruption: rand & 1 lets it move
        var machamp = Mon("Machamp", 50, "Strength");
        var blastoise = Of(Gender.Female, "Blastoise", 50, "Attract");
        var core = Wild(machamp, blastoise, Calm().Force(RollKind.Infatuation, 0, 1));

        var said = Turn(core);
        InOrder(said, "Foe Blastoise used Attract!", "Machamp fell in love!", "Machamp is in love with Foe Blastoise!", "Machamp is immobilized by love!");
        Assert.Equal(139, blastoise.CurrentHP);

        said = Turn(core);
        InOrder(said, "But it failed!", "Machamp is in love with Foe Blastoise!", "Machamp used Strength!");
        Assert.Equal(139 - 47, blastoise.CurrentHP);

        // The same gender, or none: nothing
        Assert.Contains("But it failed!", Turn(Wild(Mon("Machamp", 50), Mon("Blastoise", 50, "Attract"))));
        Assert.Contains("But it failed!", Turn(Wild(Of(Gender.Genderless, "Mew", 50, "Attract"), Of(Gender.Female, "Blastoise", 50))));

        // Oblivious
        var oblivious = Wild(With(Mon("Machamp", 50), ability: "Oblivious"), Of(Gender.Female, "Blastoise", 50, "Attract"));
        Assert.Contains("Machamp's Oblivious prevents romance!", Turn(oblivious));
    }

    [Fact]
    public void ATauntedPokemonCanOnlyAttackForThreeToFiveTurns()
    {
        // subscript_taunt_start: Random 2, 3 (3 to 5), counted down at each turn's end; a move of no power can't
        // be chosen, and one already chosen is lost. Alakazam (125) moves before Blastoise (83)
        var alakazam = Idling(Mon("Alakazam", 50, "Taunt"));
        var blastoise = Mon("Blastoise", 50, "Growl", "Tackle");
        var core = Wild(alakazam, blastoise, Calm().Force(RollKind.Duration, 0));

        var said = Turn(core);
        InOrder(said, "Alakazam used Taunt!", "Foe Blastoise fell for the taunt!", "Foe Blastoise can't use Growl after the taunt!");
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Foe, 0)));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Foe, 1)));
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Fight(Foe, 0)));

        // A second Taunt on a taunted Pokémon fails
        Assert.Contains("But it failed!", Turn(core, 0, 1));

        // Three turn ends from the one it was used in
        Assert.Contains("Foe Blastoise's taunt wore off!", Turn(core, 1, 1));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Foe, 0)));
    }

    [Fact]
    public void AnEncoreHoldsAPokemonToTheMoveItLastUsed()
    {
        // BtlCmd_TryEncore: the last move used, for rand % 5 + 3 turn ends and the one after
        var alakazam = Idling(Mon("Alakazam", 50, "Encore"));
        var blastoise = Mon("Blastoise", 50, "Growl", "Tackle");
        var core = Wild(alakazam, blastoise, Calm().Force(RollKind.Duration, 0));

        // Nothing to encore before it has moved
        Assert.Contains("But it failed!", Turn(core, 0, 0));

        // Now Growl is its last move: whatever it chose this turn comes out as Growl
        var said = Turn(core, 0, 1);
        InOrder(said, "Foe Blastoise received an encore!", "Foe Blastoise used Growl!");
        Assert.DoesNotContain("Foe Blastoise used Tackle!", said);
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Foe, 1)));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Foe, 0)));

        // Counter 3: it runs down over three turn ends and ends on the fourth
        Assert.DoesNotContain("Foe Blastoise's encore ended!", Turn(core, 1, 0));
        Assert.DoesNotContain("Foe Blastoise's encore ended!", Turn(core, 1, 0));
        Assert.Contains("Foe Blastoise's encore ended!", Turn(core, 1, 0));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Foe, 1)));
    }

    [Fact]
    public void DisableTakesAwayTheLastMoveUsed()
    {
        // BtlCmd_TryDisable: rand % 4 + 3, and one more turn end
        var alakazam = Idling(Mon("Alakazam", 50, "Disable"));
        var blastoise = Mon("Blastoise", 50, "Tackle", "Growl");
        var core = Wild(alakazam, blastoise, Calm().Force(RollKind.Duration, 0));

        Turn(core, 1, 0);
        var said = Turn(core, 0, 0);
        InOrder(said, "Foe Blastoise's Tackle was disabled!", "Foe Blastoise's Tackle is disabled!");
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Foe, 0)));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Foe, 1)));

        Assert.DoesNotContain("Foe Blastoise is disabled no more!", Turn(core, 1, 1));
        Assert.DoesNotContain("Foe Blastoise is disabled no more!", Turn(core, 1, 1));
        Assert.Contains("Foe Blastoise is disabled no more!", Turn(core, 1, 1));
    }

    [Fact]
    public void ATormentedPokemonCantUseTheSameMoveTwiceRunning()
    {
        var core = Wild(Idling(Mon("Alakazam", 50, "Torment")), Mon("Snorlax", 50, "Tackle", "Growl"));
        Assert.Contains("Foe Snorlax was subjected to torment!", Turn(core, 0, 0));
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Foe, 0)));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Foe, 1)));
        Turn(core, 1, 1);
        Assert.Null(core.WhyNot(BattleChoice.Fight(Foe, 0)));
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Foe, 1)));
    }

    [Fact]
    public void ImprisonSealsTheMovesItsUserKnows()
    {
        var core = Wild(Mon("Blastoise", 50, "Imprison", "Tackle"), Mon("Snorlax", 50, "Tackle", "Growl"));
        Assert.Contains("Blastoise sealed the moves it shares with its foe!", Turn(core, 0, 1));
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Foe, 0)));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Foe, 1)));

        // With no move in common there is nothing to seal
        Assert.Contains("But it failed!", Turn(Wild(Mon("Blastoise", 50, "Imprison"), Mon("Snorlax", 50, "Tackle"))));
    }

    [Fact]
    public void HealBlockAndEmbargoLastFiveTurns()
    {
        // Heal Block stops the moves on the original's list and what Leech Seed or a draining move gives; Embargo
        // stops the held item. Blastoise (83) acts before Snorlax (35)
        var snorlax = With(Mon("Snorlax", 50, "Recover", "Tackle"), item: "Leftovers");
        snorlax.CurrentHP = 100;
        var core = Wild(Idling(Mon("Blastoise", 50, "Heal Block", "Embargo")), snorlax);

        var said = Turn(core, 0, 0);
        InOrder(said, "Foe Snorlax was prevented from healing!", "Foe Snorlax can't use Recover while it is kept from healing!");
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Foe, 0)));
        // Leftovers are no healing move: they go on (Heal Block's list is of moves, in Platinum)
        Assert.Contains("Foe Snorlax restored a little HP using its Leftovers!", said);
        Assert.Equal(100 + 13, snorlax.CurrentHP);

        said = Turn(core, 1, 1);
        Assert.Contains("Foe Snorlax can't use items anymore!", said);
        Assert.DoesNotContain("Foe Snorlax restored a little HP using its Leftovers!", said);

        Turn(core, 2, 1);
        Turn(core, 2, 1);
        Assert.Contains("Foe Snorlax can heal again!", Turn(core, 2, 1));       // the fifth turn end
        Assert.Contains("Foe Snorlax can use items again!", Turn(core, 2, 1));  // and Embargo's fifth
    }

    // ================================================================== protection and the Substitute

    [Fact]
    public void ProtectWorksLessOftenEachTimeInARow()
    {
        // BtlCmd_TryProtection: the rates are 0xFFFF, 0x7FFF, 0x3FFF, 0x1FFF by how many times in a row it has
        // worked, and it works when the rate is no less than a number out of 65,536
        var alakazam = Mon("Alakazam", 50, "Protect");
        var machamp = Mon("Machamp", 50, "Strength");
        var core = Wild(alakazam, machamp, Calm().Force(RollKind.Protect, 0xFFFF, 0x7FFF, 0x4000, 0));

        var said = Turn(core);
        InOrder(said, "Alakazam used Protect!", "Alakazam protected itself!", "Foe Machamp used Strength!", "Alakazam protected itself!");
        Assert.Equal(115, alakazam.CurrentHP);

        Assert.Contains("Alakazam protected itself!", Turn(core));      // 0x7FFF against 0x7FFF
        Assert.Equal(115, alakazam.CurrentHP);

        said = Turn(core);                                              // 0x3FFF against 0x4000
        InOrder(said, "Alakazam used Protect!", "But it failed!", "Foe Machamp used Strength!");
        // 135 × 80 × 22 / 50 / 50 + 2 = 97
        Assert.Equal(115 - 97, alakazam.CurrentHP);

        // A failure starts the count again: the next one is sure
        Assert.Contains("Alakazam protected itself!", Turn(core));
        Assert.Equal(18, alakazam.CurrentHP);
    }

    [Fact]
    public void ByTheModernRulesProtectIsAThirdAsLikelyEachTime()
    {
        var alakazam = Mon("Alakazam", 50, "Protect");
        var core = Wild(alakazam, Mon("Machamp", 50, "Tackle"), Calm().Force(RollKind.Protect, 0, 21845, 21846), Ruleset.Modern);
        Assert.Contains("Alakazam protected itself!", Turn(core));
        Assert.Contains("Alakazam protected itself!", Turn(core));
        Assert.Contains("But it failed!", Turn(core));      // a ninth now: 7281 against 21846
    }

    [Fact]
    public void TheLastToActInATurnCantProtectItself()
    {
        // Both moves have priority +3; Alakazam (125) is the faster, so Snorlax's Protect is the turn's last move
        var core = Wild(Mon("Snorlax", 50, "Protect"), Mon("Alakazam", 50, "Detect"));
        InOrder(Turn(core), "Foe Alakazam used Detect!", "Foe Alakazam protected itself!", "Snorlax used Protect!", "But it failed!");
    }

    [Fact]
    public void EndureLeavesOneHp()
    {
        var alakazam = Mon("Alakazam", 50, "Endure");
        alakazam.CurrentHP = 10;
        var core = Wild(alakazam, Mon("Machamp", 50, "Strength"));
        InOrder(Turn(core), "Alakazam braced itself!", "Foe Machamp used Strength!", "Alakazam endured the hit!");
        Assert.Equal(1, alakazam.CurrentHP);
    }

    [Fact]
    public void FeintOnlyHitsAProtectedPokemonAndLiftsItsProtection()
    {
        // Feint has priority +2: after Protect (+3), before everything else
        var alakazam = Idling(Mon("Alakazam", 50, "Protect"));
        var core = Wild(alakazam, Mon("Machamp", 50, "Feint"));
        InOrder(Turn(core, 0, 0), "Alakazam protected itself!", "Foe Machamp used Feint!", "Alakazam fell for the feint!");
        Assert.True(alakazam.CurrentHP < 115);
        Assert.Contains("But it failed!", Turn(core, 1, 0));
    }

    [Fact]
    public void ASubstituteCostsAQuarterAndTakesTheHits()
    {
        // BtlCmd_TrySubstitute: Divide(139, 4) = 34 HP for a Substitute of 34. Machamp's Strength does 47: the
        // Substitute takes it all and is gone, and Blastoise loses nothing more
        var blastoise = Mon("Blastoise", 50, "Substitute");
        var core = Wild(blastoise, Mon("Machamp", 50, "Strength"));

        var said = Turn(core);
        InOrder(said, "Blastoise made a substitute!", "Foe Machamp used Strength!", "The substitute took damage for Blastoise!", "Blastoise's substitute faded!");
        Assert.Equal(139 - 34, blastoise.CurrentHP);
        Assert.False(core.At(Mine).HasSubstitute);

        // A move that would give a condition fails against it, and so does a stat drop from the other side
        blastoise = Mon("Blastoise", 50, "Substitute");
        core = Wild(blastoise, Mon("Snorlax", 50, "Sleep Powder", "Growl"));
        InOrder(Turn(core), "Blastoise made a substitute!", "Foe Snorlax used Sleep Powder!", "But it failed!");
        Assert.Equal(StatusCondition.None, blastoise.Status);
        said = Turn(core, 0, 1);
        InOrder(said, "Blastoise already has a substitute!", "Foe Snorlax used Growl!", "But it failed!");
        Assert.Equal(0, blastoise.StatStages.GetValueOrDefault(StatType.Attack));
        Assert.Equal(139 - 34, blastoise.CurrentHP);

        // With a quarter of its HP or less it can't make one
        var weak = Mon("Blastoise", 50, "Substitute");
        weak.CurrentHP = 34;
        Assert.Contains("It was too weak to make a substitute!", Turn(Wild(weak, Mon("Machamp", 50))));
        Assert.Equal(34, weak.CurrentHP);
    }

    // ================================================================== the weather

    [Fact]
    public void RainLastsFiveTurnsAndFeedsWaterMoves()
    {
        // Surf, 95 power: 90 × 95 × 22 / 90 / 50 = 41. In the rain × 15 / 10 = 61, + 2 = 63, Blastoise's own type
        // 94; under a clear sky 43 and 64
        var machamp = Mon("Machamp", 50);
        var core = Wild(Idling(Mon("Blastoise", 50, "Rain Dance", "Surf")), machamp);

        InOrder(Turn(core, 0), "Blastoise used Rain Dance!", "It started to rain!", "Rain continues to fall.");
        Assert.Equal(BattleWeather.Rain, core.Field.Weather);
        Assert.Contains("But it failed!", Turn(core, 0));
        Turn(core, 1);
        Assert.Equal(150 - 94, machamp.CurrentHP);
        Assert.Contains("Rain continues to fall.", Turn(core, 2));
        Assert.Contains("The rain stopped.", Turn(core, 2));        // the fifth turn end
        Assert.Equal(BattleWeather.None, core.Field.Weather);

        var dry = Mon("Machamp", 50);
        Turn(Wild(Mon("Blastoise", 50, "Surf"), dry));
        Assert.Equal(150 - 64, dry.CurrentHP);

        // Fire is halved: Flamethrower 114 × 95 × 22 / 90 / 50 = 52; / 2 = 26, + 2 = 28, Charizard's own type 42.
        // The rain of the place the battle is fought in doesn't end
        var soaked = Mon("Machamp", 50);
        core = Wild(Mon("Charizard", 50, "Flamethrower"), soaked, sky: BattleWeather.Rain);
        Turn(core);
        Assert.Equal(150 - 42, soaked.CurrentHP);
        Assert.True(core.Field.WeatherLasts);
    }

    [Fact]
    public void ASandstormWearsAtAllButRockSteelAndGroundAndHardensRock()
    {
        // Sand Stream brings a sandstorm that stays. Surf against Tyranitar, whose Sp. Def of 105 is 157 in the
        // sand: 90 × 95 × 22 / 157 / 50 = 23, + 2 = 25, × 15 / 10 = 37, × 2 against Rock = 74. Out of it:
        // / 105 / 50 = 35, 37, 55, 110
        var blastoise = Mon("Blastoise", 50, "Surf");
        var tyranitar = Mon("Tyranitar", 50);
        var core = Wild(blastoise, tyranitar);
        Assert.Equal(BattleWeather.Sandstorm, core.Field.Weather);
        Assert.True(core.Field.WeatherLasts);

        var said = Turn(core);
        InOrder(said, "Foe Tyranitar's Sand Stream whipped up a sandstorm!", "Blastoise used Surf!", "The sandstorm rages.", "Blastoise is buffeted by the sandstorm!");
        Assert.DoesNotContain("Foe Tyranitar is buffeted by the sandstorm!", said);
        Assert.Equal(160 - 74, tyranitar.CurrentHP);
        Assert.Equal(139 - 8, blastoise.CurrentHP);     // Divide(139, 16)

        var plain = With(Mon("Tyranitar", 50), ability: "Sturdy");
        Turn(Wild(Mon("Blastoise", 50, "Surf"), plain));
        Assert.Equal(160 - 110, plain.CurrentHP);

        // Steel and Ground types aren't worn at either
        said = Turn(Wild(Mon("Skarmory", 50), Mon("Garchomp", 50), sky: BattleWeather.Sandstorm));
        Assert.DoesNotContain(said, s => s.Contains("buffeted"));
    }

    [Fact]
    public void AnAbilitysWeatherStaysInPlatinumAndLastsFiveTurnsByTheModernRules()
    {
        var core = Wild(Mon("Kyogre", 50), Mon("Machamp", 50));
        for (int i = 0; i < 7; i++) Turn(core);
        Assert.Equal(BattleWeather.Rain, core.Field.Weather);

        core = Wild(Mon("Kyogre", 50), Mon("Machamp", 50), rules: Ruleset.Modern);
        for (int i = 0; i < 4; i++) Assert.Contains("Rain continues to fall.", Turn(core));
        Assert.Contains("The rain stopped.", Turn(core));

        // With the weather's rock, eight
        core = Wild(With(Mon("Kyogre", 50), item: "Damp Rock"), Mon("Machamp", 50), rules: Ruleset.Modern);
        for (int i = 0; i < 7; i++) Assert.Contains("Rain continues to fall.", Turn(core));
        Assert.Contains("The rain stopped.", Turn(core));
    }

    [Fact]
    public void HailWearsAtAllButIceAndBlizzardCantMissInIt()
    {
        // Blizzard is 70% accurate: a roll of 99 misses, except in hail
        var rolls = Calm().Force(RollKind.Accuracy, 99);
        Assert.Contains("Blastoise's attack missed!", Turn(Wild(Mon("Blastoise", 50, "Blizzard"), Mon("Snorlax", 50), rolls)));

        var blastoise = Mon("Blastoise", 50, "Blizzard");
        var snorlax = Mon("Snorlax", 50);
        var core = Wild(blastoise, snorlax, Calm().Force(RollKind.Accuracy, 99), sky: BattleWeather.Hail);
        var said = Turn(core);
        InOrder(said, "It started to hail!", "Blastoise used Blizzard!", "Hail continues to fall.", "Blastoise is pelted by the hail!", "Foe Snorlax is pelted by the hail!");
        Assert.DoesNotContain("Blastoise's attack missed!", said);
        // Blizzard, 120 power: 90 × 120 × 22 / 115 / 50 = 41, + 2 = 43; then a sixteenth of 220
        Assert.Equal(220 - 43 - 13, snorlax.CurrentHP);
        Assert.Equal(139 - 8, blastoise.CurrentHP);

        // An Ice type is at home in it, and Ice Body heals a sixteenth where it would have hurt
        var glaceon = With(Mon("Glaceon", 50), ability: "Ice Body");
        glaceon.CurrentHP = 100;
        said = Turn(Wild(glaceon, Mon("Abomasnow", 50)));
        Assert.DoesNotContain(said, s => s.Contains("pelted"));
        Assert.Equal(100 + 125 / 16, glaceon.CurrentHP);
    }

    [Fact]
    public void TheSunChargesSolarBeamAtOnceHalvesWaterAndKeepsIceAway()
    {
        // Solar Beam, 120 power, against Snorlax: 105 × 120 × 22 / 115 / 50 = 48, + 2 = 50, Venusaur's own type 75.
        // Snorlax's Surf back: 70 × 95 × 22 / 105 / 50 = 27; / 2 in the sun = 13, + 2 = 15, half against Grass = 7
        var venusaur = Mon("Venusaur", 50, "Solar Beam");
        var snorlax = Mon("Snorlax", 50, "Surf");
        var core = Wild(venusaur, snorlax, sky: BattleWeather.Sun);
        var said = Turn(core);
        InOrder(said, "The sunlight turned harsh!", "Venusaur used Solar Beam!", "Foe Snorlax used Surf!", "The sunlight is strong.");
        Assert.DoesNotContain("Venusaur took in sunlight!", said);
        Assert.Equal(220 - 75, snorlax.CurrentHP);
        Assert.Equal(140 - 7, venusaur.CurrentHP);
        Assert.False(core.TryInflictStatus(core.At(Foe), StatusCondition.Freeze, core.At(Mine)));

        // Under a clear sky it takes a turn to charge, and the Pokémon isn't asked what to do on the second
        venusaur = Mon("Venusaur", 50, "Solar Beam");
        snorlax = Mon("Snorlax", 50);
        core = Wild(venusaur, snorlax);
        Assert.Contains("Venusaur took in sunlight!", Turn(core));
        Assert.Equal(220, snorlax.CurrentHP);
        Assert.Equal(new[] { Foe }, Assert.IsType<ActionRequest>(core.Request).Places);
        Assert.Contains("Venusaur used Solar Beam!", Turn(core));
        Assert.Equal(220 - 75, snorlax.CurrentHP);
        Assert.Equal(9, venusaur.Moves[0].CurrentPP);       // spent once, on the first turn

        // In the rain the beam is half as strong: 48 / 2 = 24, + 2 = 26, 39
        snorlax = Mon("Snorlax", 50);
        core = Wild(Mon("Venusaur", 50, "Solar Beam"), snorlax, sky: BattleWeather.Rain);
        Turn(core);
        Turn(core);
        Assert.Equal(220 - 39, snorlax.CurrentHP);
    }

    [Fact]
    public void ThunderIsSureInTheRainAndAnEvenChanceInTheSun()
    {
        string miss = "Alakazam's attack missed!";
        Assert.DoesNotContain(miss, Turn(Wild(Mon("Alakazam", 50, "Thunder"), Mon("Snorlax", 50), Calm().Force(RollKind.Accuracy, 99), sky: BattleWeather.Rain)));
        Assert.Contains(miss, Turn(Wild(Mon("Alakazam", 50, "Thunder"), Mon("Snorlax", 50), Calm().Force(RollKind.Accuracy, 70))));
        Assert.DoesNotContain(miss, Turn(Wild(Mon("Alakazam", 50, "Thunder"), Mon("Snorlax", 50), Calm().Force(RollKind.Accuracy, 69))));
        Assert.Contains(miss, Turn(Wild(Mon("Alakazam", 50, "Thunder"), Mon("Snorlax", 50), Calm().Force(RollKind.Accuracy, 50), sky: BattleWeather.Sun)));
        Assert.DoesNotContain(miss, Turn(Wild(Mon("Alakazam", 50, "Thunder"), Mon("Snorlax", 50), Calm().Force(RollKind.Accuracy, 49), sky: BattleWeather.Sun)));
    }

    [Fact]
    public void CloudNineLeavesTheWeatherWithoutEffect()
    {
        // Golduck's Surf, with no rain to help it: 100 × 95 × 22 / 90 / 50 = 46, + 2 = 48, its own type 72
        var machamp = Mon("Machamp", 50);
        var core = Wild(With(Mon("Golduck", 50, "Surf"), ability: "Cloud Nine"), machamp, sky: BattleWeather.Rain);
        Turn(core);
        Assert.Equal(150 - 72, machamp.CurrentHP);
        Assert.Equal(BattleWeather.Rain, core.Field.Weather);
        Assert.Equal(BattleWeather.None, core.Field.WeatherInEffect);

        var said = Turn(Wild(With(Mon("Golduck", 50), ability: "Cloud Nine"), Mon("Machamp", 50), sky: BattleWeather.Sandstorm));
        Assert.DoesNotContain(said, s => s.Contains("buffeted"));
    }

    [Fact]
    public void TheWeathersAbilitiesFollowIt()
    {
        // Swift Swim and Chlorophyll double Speed
        var core = Wild(Mon("Kingdra", 50), Mon("Machamp", 50));
        Assert.Equal(90, core.EffectiveSpeed(core.At(Mine)));
        core = Wild(Mon("Kingdra", 50), Mon("Machamp", 50), sky: BattleWeather.Rain);
        Assert.Equal(180, core.EffectiveSpeed(core.At(Mine)));
        core = Wild(Mon("Sunflora", 50), Mon("Machamp", 50), sky: BattleWeather.Sun);
        Assert.Equal(70, core.EffectiveSpeed(core.At(Mine)));

        // Rain Dish gives a sixteenth back, Dry Skin an eighth; the sun takes an eighth from Dry Skin and Solar Power
        var ludicolo = With(Mon("Ludicolo", 50), ability: "Rain Dish");
        ludicolo.CurrentHP = 50;
        Turn(Wild(ludicolo, Mon("Machamp", 50), sky: BattleWeather.Rain));
        Assert.Equal(50 + 140 / 16, ludicolo.CurrentHP);

        var toxicroak = With(Mon("Toxicroak", 50), ability: "Dry Skin");
        toxicroak.CurrentHP = 50;
        Turn(Wild(toxicroak, Mon("Machamp", 50), sky: BattleWeather.Rain));
        Assert.Equal(50 + 143 / 8, toxicroak.CurrentHP);
        toxicroak.CurrentHP = 50;
        Turn(Wild(toxicroak, Mon("Machamp", 50), sky: BattleWeather.Sun));
        Assert.Equal(50 - 143 / 8, toxicroak.CurrentHP);

        // Hydration washes a condition away, and Leaf Guard keeps one from taking hold in the sun
        var vaporeon = With(Mon("Vaporeon", 50), ability: "Hydration");
        vaporeon.Status = StatusCondition.Burn;
        Assert.Contains("Vaporeon's Hydration cured its burn!", Turn(Wild(vaporeon, Mon("Machamp", 50), sky: BattleWeather.Rain)));
        core = Wild(Mon("Leafeon", 50), Mon("Machamp", 50), sky: BattleWeather.Sun);
        Assert.False(core.TryInflictStatus(core.At(Mine), StatusCondition.Burn, core.At(Foe)));

        // Sand Veil: a fifth harder to hit in a sandstorm. Strength, 100% accurate, is 80 there
        string miss = "Foe Machamp's attack missed!";
        Assert.Contains(miss, Turn(Wild(Mon("Garchomp", 50), Mon("Machamp", 50, "Strength"), Calm().Force(RollKind.Accuracy, 80), sky: BattleWeather.Sandstorm)));
        Assert.DoesNotContain(miss, Turn(Wild(Mon("Garchomp", 50), Mon("Machamp", 50, "Strength"), Calm().Force(RollKind.Accuracy, 79), sky: BattleWeather.Sandstorm)));
        Assert.DoesNotContain(miss, Turn(Wild(Mon("Garchomp", 50), Mon("Machamp", 50, "Strength"), Calm().Force(RollKind.Accuracy, 99))));
    }

    [Fact]
    public void WeatherBallTakesTheSkysTypeAndTwiceThePower()
    {
        // Alakazam, under a clear sky: Normal, 50 power: 140 × 50 × 22 / 90 / 50 = 34, + 2 = 36. In the rain it is
        // Water and 100 power: 68, × 15 / 10 = 102, + 2 = 104 (not Alakazam's own type)
        var machamp = Mon("Machamp", 50);
        Turn(Wild(Mon("Alakazam", 50, "Weather Ball"), machamp));
        Assert.Equal(150 - 36, machamp.CurrentHP);

        machamp = Mon("Machamp", 50);
        Turn(Wild(Mon("Alakazam", 50, "Weather Ball"), machamp, sky: BattleWeather.Rain));
        Assert.Equal(150 - 104, machamp.CurrentHP);

        // In hail it is Ice: a Water Absorb that would drink the rain's doesn't stop it
        var vaporeon = Mon("Vaporeon", 50);
        var said = Turn(Wild(Mon("Alakazam", 50, "Weather Ball"), vaporeon, sky: BattleWeather.Hail));
        Assert.Contains("It's not very effective...", said);
    }

    [Fact]
    public void MorningSunHealsByTheSky()
    {
        // BtlCmd_WeatherHPRecovery: half under a clear sky, Divide(max × 20, 30) in the sun, a quarter otherwise
        int Healed(BattleWeather sky)
        {
            var snorlax = Mon("Snorlax", 50, "Morning Sun");
            snorlax.CurrentHP = 1;
            Turn(Wild(snorlax, Mon("Machamp", 50), sky: sky));
            return snorlax.CurrentHP - 1;
        }
        Assert.Equal(110, Healed(BattleWeather.None));
        Assert.Equal(146, Healed(BattleWeather.Sun));
        Assert.Equal(55, Healed(BattleWeather.Rain));
    }

    // ================================================================== what a side puts up

    [Fact]
    public void ReflectHalvesPhysicalDamageForFiveTurnsButNotACriticalHit()
    {
        // Strength: 135 × 80 × 22 / 105 / 50 = 45; behind Reflect 22, + 2 = 24 (47 without)
        var blastoise = Idling(Mon("Blastoise", 50, "Reflect"));
        var core = Wild(blastoise, Mon("Machamp", 50, "Strength"));
        InOrder(Turn(core), "Blastoise used Reflect!", "Reflect raised your team's Defense!", "Foe Machamp used Strength!");
        Assert.Equal(139 - 24, blastoise.CurrentHP);
        Assert.Contains("But it failed!", Turn(core));

        for (int turn = 3; turn <= 4; turn++) Assert.DoesNotContain("Your team's Reflect wore off!", Turn(core, 1));
        Assert.Contains("Your team's Reflect wore off!", Turn(core, 1));
        Assert.Equal(139 - 24 * 5, blastoise.CurrentHP);

        // A critical hit goes through it: (45 + 2) × 2
        blastoise = Mon("Blastoise", 50, "Reflect");
        Turn(Wild(blastoise, Mon("Machamp", 50, "Strength"), Calm().Force(RollKind.Critical, 0)));
        Assert.Equal(139 - 94, blastoise.CurrentHP);

        // Light Clay: eight turns (seven left after the first turn's end)
        core = Wild(With(Mon("Blastoise", 50, "Reflect"), item: "Light Clay"), Mon("Machamp", 50));
        Turn(core);
        Assert.Equal(7, core.Field.Side(BattleSide.Player).ReflectTurns);
    }

    [Fact]
    public void LightScreenHalvesSpecialDamage()
    {
        // Psychic against Blastoise: 140 × 90 × 22 / 110 / 50 = 50; behind the screen 25, + 2 = 27, Alakazam's
        // own type 40. Blastoise is slower, so the first hit lands before the screen is up: 52 × 15 / 10 = 78
        var blastoise = Idling(Mon("Blastoise", 50, "Light Screen"));
        var core = Wild(blastoise, Mon("Alakazam", 50, "Psychic"));
        Turn(core);
        Assert.Equal(139 - 78, blastoise.CurrentHP);
        Turn(core, 1);
        Assert.Equal(139 - 78 - 40, blastoise.CurrentHP);
    }

    [Fact]
    public void BrickBreakBreaksTheScreensOfATargetItReaches()
    {
        // Brick Break, 75 power, takes no notice of the screen it breaks: 135 × 75 × 22 / 105 / 50 = 42, + 2 = 44,
        // Machamp's own type 66
        var blastoise = Mon("Blastoise", 50, "Reflect");
        var core = Wild(blastoise, Mon("Machamp", 50, "Brick Break"));
        InOrder(Turn(core), "Reflect raised your team's Defense!", "Foe Machamp used Brick Break!", "It shattered the barrier!");
        Assert.Equal(139 - 66, blastoise.CurrentHP);
        Assert.False(core.Field.Side(BattleSide.Player).Reflect);

        // Even a Ghost's, which it can't hurt (BattleSystem_TriggerPrimaryEffect)
        core = Wild(Mon("Gengar", 50, "Reflect"), Mon("Machamp", 50, "Brick Break"));
        InOrder(Turn(core), "Foe Machamp used Brick Break!", "It shattered the barrier!", "It doesn't affect Gengar...");
        Assert.False(core.Field.Side(BattleSide.Player).Reflect);
    }

    [Fact]
    public void MistSafeguardAndLuckyChantGuardASide()
    {
        var blastoise = Mon("Blastoise", 50, "Mist");
        Assert.Contains("Blastoise is protected by the mist!", Turn(Wild(blastoise, Mon("Snorlax", 50, "Growl"))));
        Assert.Equal(0, blastoise.StatStages.GetValueOrDefault(StatType.Attack));

        blastoise = Mon("Blastoise", 50, "Safeguard");
        Assert.Contains("Blastoise is protected by Safeguard!", Turn(Wild(blastoise, Mon("Snorlax", 50, "Thunder Wave"))));
        Assert.Equal(StatusCondition.None, blastoise.Status);

        // A hit that would have been critical isn't, under a Lucky Chant
        blastoise = Mon("Blastoise", 50, "Lucky Chant");
        Turn(Wild(blastoise, Mon("Machamp", 50, "Strength"), Calm().Force(RollKind.Critical, 0)));
        Assert.Equal(139 - 47, blastoise.CurrentHP);
    }

    // ================================================================== what lies in wait

    [Fact]
    public void SpikesHurtWhoeverComesInOnTheGroundAnEighthASixthOrAQuarter()
    {
        // subscript_hazards_check: Divide(max HP, (5 − layers) × 2). Garchomp lays one a turn; a fourth fails
        var snorlax = Mon("Snorlax", 50);
        var blastoise = Mon("Blastoise", 50);
        var skarmory = Mon("Skarmory", 50);
        var core = Wild(snorlax, Mon("Garchomp", 50, "Spikes"), bench: new[] { blastoise, skarmory });

        Assert.Contains("Spikes were scattered all around your team's feet!", Turn(core));
        InOrder(SwitchTo(core, 1), "Come back, Snorlax!", "Go! Blastoise!", "Blastoise is hurt by the spikes!");
        Assert.Equal(139 - 17, blastoise.CurrentHP);                // one layer: an eighth
        SwitchTo(core, 0);
        Assert.Equal(220 - 36, snorlax.CurrentHP);                  // two: a sixth
        var said = SwitchTo(core, 2);
        Assert.DoesNotContain("Skarmory is hurt by the spikes!", said);   // a Flying type isn't on the ground
        Assert.Equal(125, skarmory.CurrentHP);
        Assert.Contains("But it failed!", SwitchTo(core, 1));        // a fourth layer
        Assert.Equal(139 - 17 - 34, blastoise.CurrentHP);            // three: a quarter
    }

    [Fact]
    public void StealthRockHurtsByHowRockDoesAgainstTheNewcomer()
    {
        var charizard = Mon("Charizard", 50);
        var machamp = Mon("Machamp", 50);
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(Mon("Snorlax", 50), Mon("Garchomp", 50, "Stealth Rock"), bench: new[] { charizard, machamp, blastoise });

        Assert.Contains("Pointed stones float in the air around your team!", Turn(core));
        Assert.Contains("Pointed stones dug into Charizard!", SwitchTo(core, 1));
        Assert.Equal(138 - 69, charizard.CurrentHP);        // four times as strong against Fire and Flying: a half
        SwitchTo(core, 2);
        Assert.Equal(150 - 9, machamp.CurrentHP);           // Fighting resists Rock: a sixteenth
        SwitchTo(core, 3);
        Assert.Equal(139 - 17, blastoise.CurrentHP);        // an eighth
    }

    [Fact]
    public void ToxicSpikesPoisonAndAPoisonTypeTakesThemAway()
    {
        var machamp = Mon("Machamp", 50);
        var blastoise = Mon("Blastoise", 50);
        var skarmory = Mon("Skarmory", 50);
        var venusaur = Mon("Venusaur", 50);
        var core = Wild(machamp, Idling(Mon("Garchomp", 50, "Toxic Spikes")), bench: new[] { blastoise, skarmory, venusaur });

        Turn(core);
        Assert.Contains("Blastoise was poisoned!", SwitchTo(core, 1));          // one layer
        SwitchTo(core, 2);                                                      // two; Skarmory flies over them
        Assert.Equal(StatusCondition.None, skarmory.Status);
        var said = SwitchTo(core, 0);
        Assert.Contains("Machamp was badly poisoned!", said);
        Assert.Contains("But it failed!", said);                                // a third layer
        Assert.Contains("The poison spikes disappeared from around your team's feet!", SwitchTo(core, 3, foe: 1));
        Assert.Equal(StatusCondition.None, venusaur.Status);
        Assert.Equal(0, core.Field.Side(BattleSide.Player).ToxicSpikes);
    }

    [Fact]
    public void RapidSpinClearsItsUsersSideAndDefogItsTargets()
    {
        // Garchomp (107) is faster than Blastoise (83)
        var blastoise = Idling(Mon("Blastoise", 50, "Rapid Spin"));
        var core = Wild(blastoise, Mon("Garchomp", 50, "Spikes", "Stealth Rock", "Leech Seed"));
        Turn(core, 1, 0);
        Turn(core, 1, 1);
        var said = Turn(core, 0, 2);
        InOrder(said, "Blastoise was seeded!", "Blastoise used Rapid Spin!", "Blastoise blew away Leech Seed!", "Blastoise blew away Spikes!", "Blastoise blew away Stealth Rock!");
        var side = core.Field.Side(BattleSide.Player);
        Assert.Equal((0, false), (side.Spikes, side.StealthRock));

        // Defog: the target's evasion falls, its side loses its screens, and the fog lifts
        var garchomp = Idling(Mon("Garchomp", 50, "Reflect"));
        core = Wild(Idling(Mon("Blastoise", 50, "Defog")), garchomp, sky: BattleWeather.Fog);
        Turn(core, 1, 0);
        said = Turn(core, 0, 1);
        InOrder(said, "Foe Garchomp's evasiveness fell!", "The foe's team's Reflect was blown away!", "Blastoise blew away the deep fog!");
        Assert.Equal(-1, garchomp.StatStages.GetValueOrDefault(StatType.Evasion));
        Assert.Equal(BattleWeather.None, core.Field.Weather);
    }

    [Fact]
    public void FogMakesEveryMoveLessSure()
    {
        // × 6 / 10: a 100% move misses on a roll of 60
        Assert.Contains("Blastoise's attack missed!", Turn(Wild(Mon("Blastoise", 50, "Strength"), Mon("Snorlax", 50), Calm().Force(RollKind.Accuracy, 60), sky: BattleWeather.Fog)));
        Assert.DoesNotContain("Blastoise's attack missed!", Turn(Wild(Mon("Blastoise", 50, "Strength"), Mon("Snorlax", 50), Calm().Force(RollKind.Accuracy, 59), sky: BattleWeather.Fog)));
    }

    // ================================================================== the rooms and the wind

    [Fact]
    public void TrickRoomLetsTheSlowerGoFirst()
    {
        // Priority −7, so it is the turn's last move; from the next turn Snorlax (35) is ahead of Alakazam (125)
        var core = Wild(Mon("Snorlax", 50, "Trick Room", "Growl"), Mon("Alakazam", 50));
        InOrder(Turn(core, 0), "Foe Alakazam used Idle!", "Snorlax used Trick Room!", "Snorlax twisted the dimensions!");
        InOrder(Turn(core, 1), "Snorlax used Growl!", "Foe Alakazam used Idle!");
        Turn(core, 1);
        Turn(core, 1);
        Assert.Contains("The twisted dimensions returned to normal!", Turn(core, 1));   // the fifth turn end
        InOrder(Turn(core, 1), "Foe Alakazam used Idle!", "Snorlax used Growl!");

        // Used again while it is up, it takes it down
        core = Wild(Mon("Snorlax", 50, "Trick Room"), Mon("Alakazam", 50));
        Turn(core);
        Assert.Contains("Snorlax restored the twisted dimensions!", Turn(core));
        Assert.False(core.Field.TrickRoom);
    }

    [Fact]
    public void ATailwindDoublesSpeedForThreeTurns()
    {
        var core = Wild(Idling(Mon("Blastoise", 50, "Tailwind")), Mon("Machamp", 50));
        Assert.Contains("The tailwind blew from behind your team!", Turn(core));
        Assert.Equal(166, core.EffectiveSpeed(core.At(Mine)));
        Turn(core, 1);
        Assert.Contains("Your team's tailwind petered out!", Turn(core, 1));
        Assert.Equal(83, core.EffectiveSpeed(core.At(Mine)));

        // Four by the modern rules
        core = Wild(Idling(Mon("Blastoise", 50, "Tailwind")), Mon("Machamp", 50), rules: Ruleset.Modern);
        Turn(core);
        Assert.Equal(3, core.Field.Side(BattleSide.Player).TailwindTurns);
    }

    [Fact]
    public void GravityBringsEveryoneDownAndMakesMovesSurer()
    {
        // Earthquake against Skarmory once its Flying type no longer keeps it clear: 135 × 100 × 22 / 145 / 50 = 40,
        // + 2 = 42, twice against Steel = 84. Hypnosis, 60% accurate, × 10 / 6 = 100: it lands on a roll of 99
        var skarmory = Mon("Skarmory", 50, "Fly", "Tackle");
        var core = Wild(Mon("Machamp", 50, "Gravity", "Earthquake", "Hypnosis"), skarmory, Calm().Force(RollKind.Accuracy, 99));

        InOrder(Turn(core, 0, 1), "Gravity intensified!", "Foe Skarmory couldn't stay airborne because of gravity!");
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Foe, 0)));
        Turn(core, 1, 1);
        Assert.Equal(125 - 84, skarmory.CurrentHP);
        Assert.Contains("Foe Skarmory fell asleep!", Turn(core, 2, 1));
        Turn(core, 2, 1);
        Assert.Contains("Gravity returned to normal!", Turn(core, 2, 1));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Foe, 0)));
    }

    [Fact]
    public void MudSportHalvesElectricMoves()
    {
        // Thunderbolt: 140 × 95 × 22 / 115 / 50 = 50, + 2 = 52. With its power halved to 47: 25, 27
        var snorlax = Mon("Snorlax", 50, "Mud Sport");
        var core = Wild(snorlax, Mon("Alakazam", 50, "Thunderbolt"));
        Assert.Contains("Electricity's power was weakened!", Turn(core));
        Assert.Equal(220 - 52, snorlax.CurrentHP);
        Turn(core);
        Assert.Equal(220 - 52 - 27, snorlax.CurrentHP);
    }

    // ================================================================== moves that take more than a turn

    [Fact]
    public void FlyTakesItsUserOutOfReachForATurn()
    {
        // Fly, 90 power: 89 × 90 × 22 / 85 / 50 = 41, + 2 = 43, Charizard's own type 64, twice against Fighting = 128
        var charizard = Mon("Charizard", 50, "Fly");
        var machamp = Mon("Machamp", 50, "Strength", "Thunder");
        var core = Wild(charizard, machamp);

        InOrder(Turn(core), "Charizard flew up high!", "Foe Machamp used Strength!", "Foe Machamp's attack missed!");
        Assert.Equal(138, charizard.CurrentHP);
        Assert.True(core.At(Mine).IsElsewhere);
        Assert.Contains(core.Log, e => e is Said { Text: "Charizard flew up high!" } said && said.Shows.Contains(new Vanished(Mine)));
        Assert.Equal(new[] { Foe }, Assert.IsType<ActionRequest>(core.Request).Places);

        InOrder(Turn(core), "Charizard used Fly!", "It's super effective!", "Foe Machamp used Strength!");
        Assert.Equal(150 - 128, machamp.CurrentHP);
        Assert.Equal(14, charizard.Moves[0].CurrentPP);
        Assert.False(core.At(Mine).IsElsewhere);
        Assert.Contains(core.Log, e => e is Said { Text: "Charizard used Fly!" } said && said.Shows.Contains(new Reappeared(Mine)));
        // And it is back where Strength can reach it: 135 × 80 × 22 / 83 / 50 = 57, + 2 = 59
        Assert.Equal(138 - 59, charizard.CurrentHP);

        // Thunder follows it up there: 70 × 120 × 22 / 90 / 50 = 41, + 2 = 43, twice against Flying = 86
        charizard = Mon("Charizard", 50, "Fly");
        core = Wild(charizard, Mon("Machamp", 50, "Thunder"));
        Assert.DoesNotContain("Foe Machamp's attack missed!", Turn(core));
        Assert.Equal(138 - 86, charizard.CurrentHP);
    }

    [Fact]
    public void EarthquakeReachesADiggerAtTwiceThePower()
    {
        // 135 × 200 × 22 / 105 / 50 = 113, + 2 = 115 (58 above ground)
        var blastoise = Mon("Blastoise", 50, "Dig");
        var core = Wild(blastoise, Mon("Machamp", 50, "Earthquake"));
        InOrder(Turn(core), "Blastoise burrowed its way under the ground!", "Foe Machamp used Earthquake!");
        Assert.Equal(139 - 115, blastoise.CurrentHP);

        // Under the ground the sandstorm doesn't reach it
        blastoise = Mon("Blastoise", 50, "Dig");
        var said = Turn(Wild(blastoise, Mon("Garchomp", 50), sky: BattleWeather.Sandstorm));
        Assert.DoesNotContain("Blastoise is buffeted by the sandstorm!", said);
    }

    [Fact]
    public void SkullBashRaisesDefenseAsItGetsReadyAndAPowerHerbSkipsTheWait()
    {
        var blastoise = Mon("Blastoise", 50, "Skull Bash");
        var snorlax = Mon("Snorlax", 50);
        var core = Wild(blastoise, snorlax);
        InOrder(Turn(core), "Blastoise lowered its head!", "Blastoise's Defense rose!");
        Assert.Equal(220, snorlax.CurrentHP);
        // 88 × 100 × 22 / 70 / 50 = 55, + 2 = 57
        Assert.Contains("Blastoise used Skull Bash!", Turn(core));
        Assert.Equal(220 - 57, snorlax.CurrentHP);

        blastoise = With(Mon("Blastoise", 50, "Skull Bash"), item: "Power Herb");
        snorlax = Mon("Snorlax", 50);
        InOrder(Turn(Wild(blastoise, snorlax)), "Blastoise lowered its head!", "Blastoise became fully charged due to its Power Herb!", "Blastoise used Skull Bash!");
        Assert.Equal(220 - 57, snorlax.CurrentHP);
        Assert.Null(blastoise.HeldItem);
        Assert.Equal(1, blastoise.StatStages.GetValueOrDefault(StatType.Defense));
    }

    [Fact]
    public void HyperBeamCostsTheNextTurnIfItHit()
    {
        var core = Wild(Mon("Snorlax", 50, "Hyper Beam"), Mon("Blastoise", 50));
        Turn(core);
        Assert.Equal(new[] { Foe }, Assert.IsType<ActionRequest>(core.Request).Places);
        Assert.Contains("Snorlax must recharge!", Turn(core));
        Assert.Equal(new[] { Mine, Foe }, Assert.IsType<ActionRequest>(core.Request).Places);

        // A miss costs nothing
        core = Wild(Mon("Snorlax", 50, "Hyper Beam"), Mon("Blastoise", 50), Calm().Force(RollKind.Accuracy, 99));
        Assert.Contains("Snorlax's attack missed!", Turn(core));
        Assert.Equal(new[] { Mine, Foe }, Assert.IsType<ActionRequest>(core.Request).Places);
    }

    [Fact]
    public void ARampageLastsTwoOrThreeTurnsAndEndsInConfusion()
    {
        // subscript_thrash: Random 1, 2. Thrash, 90 power: 115 × 90 × 22 / 105 / 50 = 43, + 2 = 45, Snorlax's own type 67
        var snorlax = Mon("Snorlax", 50, "Thrash");
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(snorlax, blastoise, Calm().Force(RollKind.Duration, 0));

        Turn(core);
        Assert.Equal(new[] { Foe }, Assert.IsType<ActionRequest>(core.Request).Places);
        var said = Turn(core);
        InOrder(said, "Snorlax used Thrash!", "Snorlax became confused due to fatigue!");
        Assert.Equal(139 - 67 * 2, blastoise.CurrentHP);
        Assert.Equal(snorlax.Moves[0].MaxPP - 1, snorlax.Moves[0].CurrentPP);     // spent once
        Assert.True(core.At(Mine).IsConfused);
        Assert.Equal(new[] { Mine, Foe }, Assert.IsType<ActionRequest>(core.Request).Places);

        // Three with the other roll
        core = Wild(Mon("Snorlax", 50, "Thrash"), Mon("Skarmory", 50), Calm().Force(RollKind.Duration, 1));
        Turn(core);
        Assert.DoesNotContain("Snorlax became confused due to fatigue!", Turn(core));
        Assert.Contains("Snorlax became confused due to fatigue!", Turn(core));

        // Cut short by a move that can't touch its target, it ends without confusion
        core = Wild(Mon("Snorlax", 50, "Thrash"), Mon("Gengar", 50), Calm().Force(RollKind.Duration, 1));
        Assert.Contains("It doesn't affect Foe Gengar...", Turn(core));
        Assert.False(core.At(Mine).IsConfused);
        Assert.Equal(new[] { Mine, Foe }, Assert.IsType<ActionRequest>(core.Request).Places);
    }

    [Fact]
    public void BideGivesBackTwiceWhatItTook()
    {
        // Strength against Snorlax: 135 × 80 × 22 / 70 / 50 = 67, + 2 = 69. Two turns of it, then 276 back
        var snorlax = Mon("Snorlax", 50, "Bide");
        var machamp = Idling(Mon("Machamp", 50, "Strength"));
        var core = Wild(snorlax, machamp);

        InOrder(Turn(core, 0, 0), "Snorlax used Bide!", "Foe Machamp used Strength!");
        Assert.Equal(new[] { Foe }, Assert.IsType<ActionRequest>(core.Request).Places);
        InOrder(Turn(core, 0, 1), "Snorlax is storing energy!", "Foe Machamp used Idle!");
        var said = Turn(core, 0, 1);
        InOrder(said, "Snorlax unleashed energy!", "Foe Machamp took the energy!");
        Assert.Equal(150 - 138, machamp.CurrentHP);
        Assert.Equal(new[] { Mine, Foe }, Assert.IsType<ActionRequest>(core.Request).Places);

        // With nothing taken there is nothing to give back
        core = Wild(Mon("Snorlax", 50, "Bide"), Mon("Machamp", 50));
        Turn(core);
        Turn(core);
        InOrder(Turn(core), "Snorlax unleashed energy!", "But it failed!");
    }

    [Fact]
    public void AnUproarKeepsEveryoneAwake()
    {
        // subscript_uproar: Random 3, 3 turn ends. Uproar, 50 power: 140 × 50 × 22 / 110 / 50 = 28, + 2 = 30
        var blastoise = Mon("Blastoise", 50, "Tackle");
        blastoise.Status = StatusCondition.Sleep;
        blastoise.SleepTurns = 5;
        var core = Wild(Mon("Alakazam", 50, "Uproar"), blastoise, Calm().Force(RollKind.Duration, 0));

        var said = Turn(core);
        InOrder(said, "Alakazam caused an uproar!", "The uproar woke up Foe Blastoise!", "Foe Blastoise used Tackle!", "Alakazam is making an uproar!");
        Assert.False(core.TryInflictStatus(core.At(Foe), StatusCondition.Sleep, core.At(Mine)));
        Assert.Equal(new[] { Foe }, Assert.IsType<ActionRequest>(core.Request).Places);
        Assert.Contains("Alakazam is making an uproar!", Turn(core));
        Assert.Contains("Alakazam calmed down.", Turn(core));
        Assert.Equal(139 - 30 * 3, blastoise.CurrentHP);
        Assert.True(core.TryInflictStatus(core.At(Foe), StatusCondition.Sleep, core.At(Mine)));
    }

    // ================================================================== leaving the field

    [Fact]
    public void PursuitCatchesAPokemonOnItsWayOutAtTwiceThePower()
    {
        // Pursuit, 40 power: 135 × 40 × 22 / 105 / 50 = 22, + 2 = 24; at 80 against one leaving: 45, 47
        var blastoise = Mon("Blastoise", 50);
        var machamp = Mon("Machamp", 50, "Pursuit");
        var core = Wild(blastoise, machamp, bench: Mon("Snorlax", 50));
        Turn(core);
        Assert.Equal(139 - 24, blastoise.CurrentHP);

        var said = SwitchTo(core, 1);
        InOrder(said, "Come back, Blastoise!", "Foe Machamp used Pursuit!", "Go! Snorlax!");
        Assert.Equal(1, said.Count(s => s == "Foe Machamp used Pursuit!"));
        Assert.Equal(139 - 24 - 47, blastoise.CurrentHP);
        Assert.Equal(18, machamp.Moves[0].CurrentPP);
        Assert.Equal("Snorlax", core.At(Mine).Pokemon!.Species.Name);
    }

    [Fact]
    public void UTurnHitsAndBringsItsUserBack()
    {
        // U-turn, 70 power, Bug: 88 × 70 × 22 / 85 / 50 = 31, + 2 = 33, half against Fighting = 16. The replacement
        // is asked for in the middle of the turn, and it is the one Machamp's Strength then lands on
        var snorlax = Mon("Snorlax", 50);
        var machamp = Mon("Machamp", 50, "Strength");
        var core = Wild(Mon("Blastoise", 50, "U-turn"), machamp, bench: snorlax);

        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        InOrder(Lines(core.TakeLog()), "Blastoise used U-turn!", "It's not very effective...", "Blastoise went back to Lucas!");
        Assert.Equal(150 - 16, machamp.CurrentHP);
        InOrder(SendIn(core, 1), "Go! Snorlax!", "Foe Machamp used Strength!");
        Assert.Equal(220 - 69, snorlax.CurrentHP);

        // With nobody to come in it is a hit and nothing more
        core = Wild(Mon("Blastoise", 50, "U-turn"), Mon("Machamp", 50));
        Turn(core);
        Assert.IsType<ActionRequest>(core.Request);
    }

    [Fact]
    public void BatonPassHandsOnStatStagesAndASubstitute()
    {
        var snorlax = Mon("Snorlax", 50);
        var core = Wild(Mon("Blastoise", 50, "Swords Dance", "Substitute", "Baton Pass"), Mon("Machamp", 50), bench: snorlax);
        Turn(core, 0);
        Turn(core, 1);
        core.Submit(BattleChoice.Fight(Mine, 2), BattleChoice.Fight(Foe, 0));
        SendIn(core, 1);
        Assert.Same(snorlax, core.At(Mine).Pokemon);
        Assert.Equal(2, snorlax.StatStages.GetValueOrDefault(StatType.Attack));
        Assert.Equal(34, core.At(Mine).Volatile.SubstituteHp);

        // A plain switch hands on nothing
        core = Wild(Mon("Blastoise", 50, "Swords Dance"), Mon("Machamp", 50), bench: snorlax = Mon("Snorlax", 50));
        Turn(core);
        SwitchTo(core, 1);
        Assert.Equal(0, snorlax.StatStages.GetValueOrDefault(StatType.Attack));

        // And with nobody to pass to, the move fails
        Assert.Contains("But it failed!", Turn(Wild(Mon("Blastoise", 50, "Baton Pass"), Mon("Machamp", 50))));
    }

    [Fact]
    public void RoarDragsOutAnotherOfTheParty()
    {
        // BtlCmd_TryWhirlwind: someone else of the party, by chance; what lies in wait meets it
        var machamp = Mon("Machamp", 50);
        var core = Against(new[] { Mon("Blastoise", 50), Mon("Snorlax", 50), machamp }, new[] { Mon("Garchomp", 50, "Roar") }, Calm().Force(RollKind.DraggedOut, 1));
        InOrder(Turn(core), "Blastoise used Idle!", "Foe Garchomp used Roar!", "Machamp was dragged out!");
        Assert.Same(machamp, core.At(Mine).Pokemon);

        // A wild Pokémon's ends the meeting
        core = Wild(Mon("Blastoise", 50), Mon("Garchomp", 50, "Roar"));
        Turn(core);
        Assert.Equal(BattleResult.PlayerRan, core.Result);

        // Suction Cups and Soundproof hold against it
        core = Against(new[] { With(Mon("Octillery", 50), ability: "Suction Cups"), Mon("Snorlax", 50) }, new[] { Mon("Garchomp", 50, "Roar") });
        Assert.Contains("Octillery anchors itself with Suction Cups!", Turn(core));
        core = Against(new[] { Mon("Exploud", 50), Mon("Snorlax", 50) }, new[] { Mon("Garchomp", 50, "Roar") });
        Assert.Contains("Exploud's Soundproof blocks Roar!", Turn(core));

        // BattleSystem_CanWhirlwind: against a higher level it works when ((rand & 255) × (10 + 50) >> 8) + 1 is
        // more than 50 / 4
        core = Against(new[] { Mon("Blastoise", 50), Mon("Snorlax", 50) }, new[] { Mon("Garchomp", 10, "Roar") }, Calm().Force(RollKind.Whirlwind, 49));
        Assert.Contains("But it failed!", Turn(core));     // (49 × 60 >> 8) + 1 = 12
        core = Against(new[] { Mon("Blastoise", 50), Mon("Snorlax", 50) }, new[] { Mon("Garchomp", 10, "Roar") }, Calm().Force(RollKind.Whirlwind, 52));
        Assert.Contains("Snorlax was dragged out!", Turn(core));   // (52 × 60 >> 8) + 1 = 13
    }

    [Fact]
    public void TeleportEndsAWildBattleAndNoOther()
    {
        var core = Wild(Mon("Alakazam", 50, "Teleport"), Mon("Snorlax", 50));
        Assert.Contains("Alakazam fled from battle!", Turn(core));
        Assert.Equal(BattleResult.PlayerRan, core.Result);

        core = Against(new[] { Mon("Alakazam", 50, "Teleport") }, new[] { Mon("Snorlax", 50) });
        Assert.Contains("But it failed!", Turn(core));
        Assert.Equal(BattleResult.None, core.Result);
    }

    [Fact]
    public void AHealingWishMakesTheNextPokemonWhole()
    {
        var snorlax = Mon("Snorlax", 50);
        snorlax.CurrentHP = 30;
        snorlax.Status = StatusCondition.Paralyze;
        var core = Wild(Mon("Blastoise", 50, "Healing Wish"), Mon("Machamp", 50), bench: snorlax);

        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        Assert.Contains("Blastoise fainted!", Lines(core.TakeLog()));
        InOrder(SendIn(core, 1), "Go! Snorlax!", "The healing wish came true for Snorlax!");
        Assert.Equal((220, StatusCondition.None), (snorlax.CurrentHP, snorlax.Status));

        // It needs someone to come in
        Assert.Contains("But it failed!", Turn(Wild(Mon("Blastoise", 50, "Healing Wish"), Mon("Machamp", 50))));
    }

    // ================================================================== what holds a Pokémon on the field

    [Fact]
    public void ShadowTagArenaTrapAndMagnetPullHoldTheirFoesIn()
    {
        string? CantSwitch(Pokemon mine, Pokemon foe, Ruleset? rules = null) => Wild(mine, foe, rules: rules, bench: Mon("Snorlax", 50)).WhyNot(BattleChoice.Switch(Mine, 1));
        string? CantRun(Pokemon mine, Pokemon foe) => Wild(mine, foe, bench: Mon("Snorlax", 50)).WhyNot(BattleChoice.Run(Mine));

        Assert.Equal("Foe Wobbuffet prevents escape with Shadow Tag!", CantSwitch(Mon("Blastoise", 50), Mon("Wobbuffet", 50)));
        Assert.NotNull(CantRun(Mon("Blastoise", 50), Mon("Wobbuffet", 50)));
        Assert.Null(CantSwitch(Mon("Wobbuffet", 50), Mon("Wobbuffet", 50)));        // another Shadow Tag is free

        // A Shed Shell lets its holder be switched, a Smoke Ball lets it run
        Assert.Null(CantSwitch(With(Mon("Blastoise", 50), item: "Shed Shell"), Mon("Wobbuffet", 50)));
        Assert.NotNull(CantRun(With(Mon("Blastoise", 50), item: "Shed Shell"), Mon("Wobbuffet", 50)));
        Assert.Null(CantRun(With(Mon("Blastoise", 50), item: "Smoke Ball"), Mon("Wobbuffet", 50)));
        Assert.NotNull(CantSwitch(With(Mon("Blastoise", 50), item: "Smoke Ball"), Mon("Wobbuffet", 50)));

        // Arena Trap holds whoever is on the ground; Magnet Pull holds Steel
        var dugtrio = With(Mon("Dugtrio", 50), ability: "Arena Trap");
        Assert.NotNull(CantSwitch(Mon("Blastoise", 50), dugtrio));
        Assert.Null(CantSwitch(Mon("Skarmory", 50), dugtrio));
        Assert.Null(CantSwitch(Mon("Gengar", 50), dugtrio));                         // Levitate
        Assert.NotNull(CantSwitch(With(Mon("Skarmory", 50), item: "Iron Ball"), dugtrio));
        Assert.NotNull(CantSwitch(Mon("Skarmory", 50), Mon("Magnezone", 50)));
        Assert.Null(CantSwitch(Mon("Blastoise", 50), Mon("Magnezone", 50)));

        // By the modern rules nothing holds a Ghost
        Assert.NotNull(CantSwitch(Mon("Gengar", 50), Mon("Wobbuffet", 50)));
        Assert.Null(CantSwitch(Mon("Gengar", 50), Mon("Wobbuffet", 50), Ruleset.Modern));
    }

    [Fact]
    public void MeanLookHoldsItsTargetWhileItsUserStays()
    {
        var core = Against(new[] { Mon("Blastoise", 50), Mon("Snorlax", 50) }, new[] { Mon("Garchomp", 50, "Mean Look", "Roar"), Mon("Machamp", 50) });
        Assert.Contains("Blastoise can no longer escape!", Turn(core));
        Assert.Equal("Blastoise can't be called back!", core.WhyNot(BattleChoice.Switch(Mine, 1)));
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Switch(Mine, 1), BattleChoice.Fight(Foe, 0)));
    }

    [Fact]
    public void ABindingMoveHoldsAndHurtsForTwoToFiveTurns()
    {
        // subscript_bind_start: Random 3, 3; each turn end the count drops, hurting a sixteenth until the one
        // that frees. Wrap itself: 135 × 15 × 22 / 105 / 50 = 8, + 2 = 10
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(blastoise, Idling(Mon("Machamp", 50, "Wrap")), Calm().Force(RollKind.Duration, 0), bench: Mon("Snorlax", 50));

        InOrder(Turn(core, 0, 0), "Blastoise was caught in Foe Machamp's Wrap!", "Blastoise is hurt by Wrap!");
        Assert.Equal(139 - 10 - 8, blastoise.CurrentHP);
        Assert.Equal("Blastoise can't be called back!", core.WhyNot(BattleChoice.Switch(Mine, 1)));
        Assert.Contains("Blastoise is hurt by Wrap!", Turn(core, 0, 1));
        Assert.Contains("Blastoise was freed from Wrap!", Turn(core, 0, 1));
        Assert.Equal(139 - 10 - 16, blastoise.CurrentHP);
        Assert.Null(core.WhyNot(BattleChoice.Switch(Mine, 1)));

        // A Grip Claw makes it the longest it can be: five turns of hurt. By the modern rules an eighth each
        blastoise = Mon("Blastoise", 50);
        core = Wild(blastoise, Idling(With(Mon("Machamp", 50, "Wrap"), item: "Grip Claw")));
        Turn(core, 0, 0);
        Assert.Equal(5, core.At(Mine).Volatile.BindTurns);

        blastoise = Mon("Blastoise", 50);
        Turn(Wild(blastoise, Mon("Machamp", 50, "Wrap"), rules: Ruleset.Modern));
        Assert.Equal(139 - 10 - 17, blastoise.CurrentHP);
    }

    // ================================================================== conditions that work over time

    [Fact]
    public void LeechSeedTakesAnEighthEachTurnForTheSeeder()
    {
        var venusaur = Mon("Venusaur", 50, "Leech Seed");
        venusaur.CurrentHP = 100;
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(venusaur, blastoise);
        InOrder(Turn(core), "Foe Blastoise was seeded!", "Foe Blastoise's health is sapped by Leech Seed!");
        Assert.Equal(139 - 17, blastoise.CurrentHP);
        Assert.Equal(100 + 17, venusaur.CurrentHP);
        Assert.Contains("Foe Blastoise evaded the attack!", Turn(core));

        // A Grass type can't be seeded; a Big Root draws three tenths more
        Assert.Contains("It doesn't affect Foe Venusaur...", Turn(Wild(Mon("Venusaur", 50, "Leech Seed"), Mon("Venusaur", 50))));
        venusaur = With(Mon("Venusaur", 50, "Leech Seed"), item: "Big Root");
        venusaur.CurrentHP = 100;
        Turn(Wild(venusaur, Mon("Blastoise", 50)));
        Assert.Equal(100 + 17 * 130 / 100, venusaur.CurrentHP);

        // Liquid Ooze turns it on the seeder
        venusaur = Mon("Venusaur", 50, "Leech Seed");
        Assert.Contains("Venusaur sucked up the liquid ooze!", Turn(Wild(venusaur, With(Mon("Tentacruel", 50), ability: "Liquid Ooze"))));
        Assert.Equal(140 - 17, venusaur.CurrentHP);     // Tentacruel has 140 HP as well: an eighth is 17
    }

    [Fact]
    public void AGhostsCurseCostsHalfItsHpAndTakesAQuarterEachTurn()
    {
        var gengar = Mon("Gengar", 50, "Curse");
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(gengar, blastoise);
        InOrder(Turn(core), "Gengar cut its own HP and laid a curse on Foe Blastoise!", "Foe Blastoise is afflicted by the curse!");
        Assert.Equal(60, gengar.CurrentHP);
        Assert.Equal(139 - 34, blastoise.CurrentHP);
        Assert.Contains("But it failed!", Turn(core));

        // From anyone else it trades Speed for Attack and Defense
        var snorlax = Mon("Snorlax", 50, "Curse");
        Turn(Wild(snorlax, Mon("Blastoise", 50)));
        Assert.Equal((-1, 1, 1), (snorlax.StatStages[StatType.Speed], snorlax.StatStages[StatType.Attack], snorlax.StatStages[StatType.Defense]));
    }

    [Fact]
    public void APerishSongCountsDownFromThree()
    {
        var blastoise = Mon("Blastoise", 50, "Perish Song");
        var core = Wild(Idling(blastoise), Mon("Exploud", 50), bench: Mon("Snorlax", 50));
        InOrder(Turn(core), "All Pokémon hearing the song will faint in three turns!", "Foe Exploud's Soundproof blocks Perish Song!", "Blastoise's perish count fell to 3!");
        Assert.Contains("Blastoise's perish count fell to 2!", Turn(core, 1));
        Assert.Contains("Blastoise's perish count fell to 1!", Turn(core, 1));
        InOrder(Turn(core, 1), "Blastoise's perish count fell to 0!", "Blastoise fainted!");
        Assert.IsType<ReplacementRequest>(core.Request);

        // Leaving the field leaves the count behind
        core = Wild(Mon("Blastoise", 50, "Perish Song"), Mon("Machamp", 50), bench: Mon("Snorlax", 50));
        Turn(core);
        var said = SwitchTo(core, 1);
        Assert.DoesNotContain(said, s => s.StartsWith("Snorlax's perish count"));
        Assert.Contains("Foe Machamp's perish count fell to 2!", said);
    }

    [Fact]
    public void DestinyBondTakesTheAttackerAlongAndAGrudgeItsMove()
    {
        // subscript_faint_check_destiny_bond: the bond is told and the attacker goes down first
        var alakazam = Mon("Alakazam", 50, "Destiny Bond");
        alakazam.CurrentHP = 1;
        var core = Wild(alakazam, Mon("Machamp", 50, "Strength"), bench: Mon("Snorlax", 50));
        InOrder(Turn(core), "Alakazam is trying to take its foe down with it!", "Alakazam took Foe Machamp down with it!", "Foe Machamp fainted!", "Alakazam fainted!");

        alakazam = Mon("Alakazam", 50, "Grudge");
        alakazam.CurrentHP = 1;
        var machamp = Mon("Machamp", 50, "Strength");
        core = Wild(alakazam, machamp, bench: Mon("Snorlax", 50));
        InOrder(Turn(core), "Alakazam wants its foe to bear a grudge!", "Foe Machamp's Strength lost all its PP to the grudge!", "Alakazam fainted!");
        Assert.Equal(0, machamp.Moves[0].CurrentPP);

        // The bond holds only until its user moves again
        alakazam = Idling(Mon("Alakazam", 50, "Destiny Bond"));
        machamp = Idling(Mon("Machamp", 50, "Strength"));
        core = Wild(alakazam, machamp, bench: Mon("Snorlax", 50));
        Turn(core, 0, 1);
        alakazam.CurrentHP = 1;
        var said = Turn(core, 1, 0);
        Assert.DoesNotContain("Alakazam took Foe Machamp down with it!", said);
        Assert.Contains("Alakazam fainted!", said);
    }

    [Fact]
    public void AWishComesTrueATurnLater()
    {
        var blastoise = Idling(Mon("Blastoise", 50, "Wish"));
        blastoise.CurrentHP = 40;
        var core = Wild(blastoise, Mon("Machamp", 50));
        var said = Turn(core);
        Assert.Contains("Blastoise made a wish!", said);
        Assert.DoesNotContain("Blastoise's wish came true!", said);
        // Wishing again while one is on its way fails; the first comes true as that turn ends
        var second = Turn(core);
        Assert.Contains("But it failed!", second);
        Assert.Contains("Blastoise's wish came true!", second);
        Assert.Equal(40 + 69, blastoise.CurrentHP);     // Divide(139, 2)
    }

    [Fact]
    public void FutureSightLandsTwoTurnsLaterWithNoType()
    {
        // BtlCmd_TryFutureSight: worked out as it is used, with no type and no critical hit, against Umbreon, a
        // Dark type Psychic moves can't touch: 140 × 80 × 22 / 135 / 50 = 36, + 2 = 38
        var umbreon = Mon("Umbreon", 50);
        var core = Wild(Idling(Mon("Alakazam", 50, "Future Sight")), umbreon, Calm().Force(RollKind.Critical, 0));
        Assert.Contains("Alakazam foresaw an attack!", Turn(core));
        Assert.Contains("But it failed!", Turn(core));
        Assert.Equal(155, umbreon.CurrentHP);
        Assert.Contains("Foe Umbreon took the Future Sight attack!", Turn(core, 1));
        Assert.Equal(155 - 38, umbreon.CurrentHP);
    }

    [Fact]
    public void NightmareAndBadDreamsWearAtASleeper()
    {
        var snorlax = Mon("Snorlax", 50);
        snorlax.Status = StatusCondition.Sleep;
        snorlax.SleepTurns = 5;
        var core = Wild(Mon("Gengar", 50, "Nightmare"), snorlax);
        InOrder(Turn(core), "Foe Snorlax began having a nightmare!", "Foe Snorlax is locked in a nightmare!");
        Assert.Equal(220 - 55, snorlax.CurrentHP);

        snorlax = Mon("Snorlax", 50);
        Assert.Contains("But it failed!", Turn(Wild(Mon("Gengar", 50, "Nightmare"), snorlax)));     // awake

        snorlax = Mon("Snorlax", 50);
        snorlax.Status = StatusCondition.Sleep;
        snorlax.SleepTurns = 5;
        Assert.Contains("Foe Snorlax is tormented!", Turn(Wild(With(Mon("Gengar", 50), ability: "Bad Dreams"), snorlax)));
        Assert.Equal(220 - 27, snorlax.CurrentHP);
    }

    [Fact]
    public void AYawnBringsSleepAtTheEndOfTheNextTurn()
    {
        var machamp = Mon("Machamp", 50);
        var core = Wild(Idling(Mon("Blastoise", 50, "Yawn")), machamp);
        var said = Turn(core);
        Assert.Contains("Blastoise made Foe Machamp drowsy!", said);
        Assert.DoesNotContain("Foe Machamp fell asleep!", said);
        Assert.Contains("Foe Machamp fell asleep!", Turn(core, 1));
        Assert.Equal(StatusCondition.Sleep, machamp.Status);

        // Insomnia never grows drowsy
        Assert.Contains("Foe Machamp's Insomnia made it ineffective!", Turn(Wild(Mon("Blastoise", 50, "Yawn"), With(Mon("Machamp", 50), ability: "Insomnia"))));
    }

    [Fact]
    public void RestHealsEverythingForTwoTurnsAsleep()
    {
        // subscript_rest: the counter is 3, so it sleeps through two of its turns and moves on the third
        var snorlax = Mon("Snorlax", 50, "Rest");
        snorlax.CurrentHP = 50;
        snorlax.Status = StatusCondition.Paralyze;
        var core = Wild(snorlax, Mon("Blastoise", 50), Calm().Force(RollKind.FullParalysis, 1));
        Assert.Contains("Snorlax slept and became healthy!", Turn(core));
        Assert.Equal((220, StatusCondition.Sleep, 3), (snorlax.CurrentHP, snorlax.Status, snorlax.SleepTurns));
        Assert.Contains("Snorlax is fast asleep.", Turn(core));
        Assert.Contains("Snorlax is fast asleep.", Turn(core));
        InOrder(Turn(core), "Snorlax woke up!", "Snorlax used Rest!", "Snorlax's HP is full!");

        // Insomnia keeps it awake; Leaf Guard, even in the sun, doesn't (Platinum's own way)
        snorlax = With(Mon("Snorlax", 50, "Rest"), ability: "Insomnia");
        snorlax.CurrentHP = 50;
        Assert.Contains("Snorlax stayed awake because of its Insomnia!", Turn(Wild(snorlax, Mon("Blastoise", 50))));
        var leafeon = Mon("Leafeon", 50, "Rest");
        leafeon.CurrentHP = 50;
        Assert.Contains("Leafeon went to sleep!", Turn(Wild(leafeon, Mon("Blastoise", 50), sky: BattleWeather.Sun)));
    }

    [Fact]
    public void HealBellRefreshAndPsychoShiftMoveConditionsAbout()
    {
        var snorlax = Mon("Snorlax", 50);
        snorlax.Status = StatusCondition.Burn;
        var blastoise = Mon("Blastoise", 50, "Heal Bell", "Refresh", "Psycho Shift");
        blastoise.Status = StatusCondition.Paralyze;
        var core = Wild(blastoise, Mon("Machamp", 50), Calm().Force(RollKind.FullParalysis, 1), bench: snorlax);
        Turn(core, 0);
        Assert.Equal((StatusCondition.None, StatusCondition.None), (blastoise.Status, snorlax.Status));

        // A Soundproof Pokémon on the field doesn't hear the bell
        var exploud = Mon("Exploud", 50, "Heal Bell");
        exploud.Status = StatusCondition.Burn;
        Assert.Contains("Exploud's Soundproof blocks Heal Bell!", Turn(Wild(exploud, Mon("Machamp", 50))));
        Assert.Equal(StatusCondition.Burn, exploud.Status);

        blastoise.Status = StatusCondition.Burn;
        Assert.Contains("Blastoise's status returned to normal!", Turn(core, 1));
        Assert.Contains("But it failed!", Turn(core, 1));

        // Psycho Shift hands the user's condition to the target
        blastoise.Status = StatusCondition.Burn;
        var said = Turn(core, 2);
        InOrder(said, "Foe Machamp was burned!", "Blastoise moved its condition onto Foe Machamp!");
        Assert.Equal(StatusCondition.None, blastoise.Status);
    }

    [Fact]
    public void TheEndOfATurnGoesInPlatinumsOrder()
    {
        // CheckFieldConditions, then CheckMonConditions for each Pokémon from the fastest: the weather passes over
        // everyone; then Blastoise's Leftovers (+8), the seed (−17), its poison (−17) and the curse (−34)
        var blastoise = With(Mon("Blastoise", 50), item: "Leftovers");
        blastoise.CurrentHP = 100;
        blastoise.Status = StatusCondition.Poison;
        var snorlax = Mon("Snorlax", 50);
        var core = Wild(blastoise, snorlax, sky: BattleWeather.Sandstorm);
        core.At(Mine).Volatile.SeededBy = Foe;
        core.At(Mine).Volatile.Cursed = true;

        InOrder(Turn(core),
            "The sandstorm rages.", "Blastoise is buffeted by the sandstorm!", "Foe Snorlax is buffeted by the sandstorm!",
            "Blastoise restored a little HP using its Leftovers!", "Blastoise's health is sapped by Leech Seed!",
            "Foe Snorlax took in the sapped health!", "Blastoise is hurt by poison!", "Blastoise is afflicted by the curse!");
        Assert.Equal(100 - 8 + 8 - 17 - 17 - 34, blastoise.CurrentHP);
        Assert.Equal(220 - 13 + 13, snorlax.CurrentHP);     // the seed's 17 can only fill what the sand took
    }

    [Fact]
    public void AnOrbsHarmStartsTheTurnAfter()
    {
        // The orb is the last thing of its holder's turn end, after the burn's own step
        var machamp = With(Mon("Machamp", 50), item: "Flame Orb");
        var core = Wild(machamp, Mon("Blastoise", 50));
        var said = Turn(core);
        Assert.Contains("Machamp was burned!", said);
        Assert.DoesNotContain("Machamp is hurt by its burn!", said);
        Assert.Equal(150, machamp.CurrentHP);
        Assert.Contains("Machamp is hurt by its burn!", Turn(core));
    }

    // ================================================================== what the ground can reach

    [Fact]
    public void MagnetRiseRoostGastroAcidAndAnIronBallChangeWhatGroundMovesReach()
    {
        // Magnet Rise lifts its user clear for five turns
        var core = Wild(Mon("Machamp", 50, "Earthquake"), Mon("Alakazam", 50, "Magnet Rise"));
        InOrder(Turn(core), "Foe Alakazam levitated on electromagnetism!", "It doesn't affect Foe Alakazam...");

        // Roost takes the Flying type away until the turn ends: Earthquake is twice as strong against the Steel
        // that is left, 84 (see the Gravity test)
        var skarmory = Mon("Skarmory", 50, "Roost");
        skarmory.CurrentHP = 60;
        Turn(Wild(Mon("Machamp", 50, "Earthquake"), skarmory));
        Assert.Equal(60 + 62 - 84, skarmory.CurrentHP);

        // Levitate keeps Gengar clear until Gastro Acid takes its ability away
        Assert.Contains("Foe Gengar makes Ground moves miss with Levitate!", Turn(Wild(Mon("Machamp", 50, "Earthquake"), Mon("Gengar", 50))));
        core = Wild(Mon("Machamp", 50, "Gastro Acid", "Earthquake"), Mon("Gengar", 50));
        Assert.Contains("Foe Gengar's ability was suppressed!", Turn(core, 0));
        InOrder(Turn(core, 1), "Machamp used Earthquake!", "It's super effective!", "Foe Gengar fainted!");

        // An Iron Ball brings a Flying type down, and halves its Speed
        var weighted = With(Mon("Skarmory", 50), item: "Iron Ball");
        core = Wild(Mon("Machamp", 50, "Earthquake"), weighted);
        Assert.Equal(37, core.EffectiveSpeed(core.At(Foe)));
        Turn(core);
        Assert.Equal(125 - 84, weighted.CurrentHP);
    }

    [Fact]
    public void AMissedJumpKickCrashes()
    {
        // subscript_crash_on_miss: half the damage it would have done, and no more than half the target's HP.
        // High Jump Kick, 100 power, against Blastoise: 135 × 100 × 22 / 105 / 50 = 56, + 2 = 58, Machamp's own
        // type 87: it crashes for 43
        var machamp = Mon("Machamp", 50, "High Jump Kick");
        InOrder(Turn(Wild(machamp, Mon("Blastoise", 50), Calm().Force(RollKind.Accuracy, 99))), "Machamp's attack missed!", "Machamp kept going and crashed!");
        Assert.Equal(150 - 43, machamp.CurrentHP);

        // Against Snorlax it would have done 258: the crash stops at half of Snorlax's 220
        machamp = Mon("Machamp", 50, "High Jump Kick");
        Turn(Wild(machamp, Mon("Snorlax", 50), Calm().Force(RollKind.Accuracy, 99)));
        Assert.Equal(150 - 110, machamp.CurrentHP);

        // A Protect is a miss like another
        machamp = Mon("Machamp", 50, "High Jump Kick");
        Assert.Contains("Machamp kept going and crashed!", Turn(Wild(machamp, Mon("Alakazam", 50, "Protect"))));
        Assert.True(machamp.CurrentHP < 150);

        // Against a Ghost the damage is nothing, and so is the fall
        machamp = Mon("Machamp", 50, "High Jump Kick");
        InOrder(Turn(Wild(machamp, Mon("Gengar", 50))), "It doesn't affect Foe Gengar...", "Machamp kept going and crashed!");
        Assert.Equal(150, machamp.CurrentHP);
    }

    // ================================================================== taking aim

    [Fact]
    public void LockOnMakesTheNextMoveSureAndForesightLetsNormalMovesHitAGhost()
    {
        var snorlax = Mon("Snorlax", 50);
        var core = Wild(Mon("Blastoise", 50, "Lock-On", "Hydro Pump"), snorlax, Calm().Force(RollKind.Accuracy, 99));
        Assert.Contains("Blastoise took aim at Foe Snorlax!", Turn(core, 0));
        Assert.DoesNotContain("Blastoise's attack missed!", Turn(core, 1));
        Assert.True(snorlax.CurrentHP < 220);
        Assert.Contains("Blastoise's attack missed!", Turn(core, 1));    // the aim was for one turn

        // Strength against Gengar: 115 × 80 × 22 / 65 / 50 = 62, + 2 = 64, Snorlax's own type 96
        var gengar = Mon("Gengar", 50);
        core = Wild(Mon("Snorlax", 50, "Foresight", "Strength"), gengar);
        Assert.Contains("Snorlax identified Foe Gengar!", Turn(core, 0));
        Turn(core, 1);
        Assert.Equal(120 - 96, gengar.CurrentHP);
    }

    [Fact]
    public void MinimizeIsOneStageInPlatinumAndStompFlattensWhoeverUsedIt()
    {
        var blastoise = Mon("Blastoise", 50, "Minimize");
        Turn(Wild(blastoise, Mon("Machamp", 50)));
        Assert.Equal(1, blastoise.StatStages[StatType.Evasion]);
        blastoise = Mon("Blastoise", 50, "Minimize");
        Turn(Wild(blastoise, Mon("Machamp", 50), rules: Ruleset.Modern));
        Assert.Equal(2, blastoise.StatStages[StatType.Evasion]);

        // Stomp, 65 power: 135 × 65 × 22 / 105 / 50 = 36, + 2 = 38; at 130 against a minimized target 73, 75
        blastoise = Mon("Blastoise", 50, "Minimize");
        Turn(Wild(blastoise, Mon("Machamp", 50, "Stomp")));
        Assert.Equal(139 - 75, blastoise.CurrentHP);
    }

    // ================================================================== moves that go by a condition

    [Fact]
    public void SmellingSaltsWakeUpSlapAndFacadeGoByAStatusCondition()
    {
        // Smelling Salts at 120 against a paralysed Snorlax: 135 × 120 × 22 / 70 / 50 = 101, + 2 = 103, and the
        // paralysis is gone
        var snorlax = Mon("Snorlax", 50);
        snorlax.Status = StatusCondition.Paralyze;
        var said = Turn(Wild(Mon("Machamp", 50, "Smelling Salts"), snorlax, Calm().Force(RollKind.FullParalysis, 1)));
        Assert.Contains("Foe Snorlax was healed of paralysis!", said);
        Assert.Equal((220 - 103, StatusCondition.None), (snorlax.CurrentHP, snorlax.Status));

        // Wake-Up Slap at 120, Machamp's own type and twice as strong against Normal: 103 × 15 / 10 = 154, 308
        snorlax = Mon("Snorlax", 50);
        snorlax.Status = StatusCondition.Sleep;
        snorlax.SleepTurns = 5;
        said = Turn(Wild(Mon("Machamp", 50, "Wake-Up Slap"), snorlax));
        Assert.Contains("Foe Snorlax fainted!", said);

        // Facade at 140 from a burned Snorlax, the burn halving it all the same: 115 × 140 × 22 / 105 / 50 = 67,
        // / 2 = 33, + 2 = 35, its own type 52
        var blastoise = Mon("Blastoise", 50);
        var burned = Mon("Snorlax", 50, "Facade");
        burned.Status = StatusCondition.Burn;
        Turn(Wild(burned, blastoise));
        Assert.Equal(139 - 52, blastoise.CurrentHP);
    }

    [Fact]
    public void SnoreAndDreamEaterNeedASleeper()
    {
        Assert.Contains("But it failed!", Turn(Wild(Mon("Snorlax", 50, "Snore"), Mon("Blastoise", 50))));
        var asleep = Mon("Snorlax", 50, "Snore");
        asleep.Status = StatusCondition.Sleep;
        asleep.SleepTurns = 5;
        var blastoise = Mon("Blastoise", 50);
        InOrder(Turn(Wild(asleep, blastoise)), "Snorlax is fast asleep.", "Snorlax used Snore!");
        Assert.True(blastoise.CurrentHP < 139);

        // Dream Eater: 135 × 100 × 22 / 115 / 50 = 51, + 2 = 53; Gengar takes back half, 26
        var gengar = Mon("Gengar", 50, "Dream Eater");
        gengar.CurrentHP = 50;
        var snorlax = Mon("Snorlax", 50);
        Assert.Contains("Foe Snorlax wasn't affected!", Turn(Wild(gengar, snorlax)));
        snorlax.Status = StatusCondition.Sleep;
        snorlax.SleepTurns = 5;
        Assert.Contains("Foe Snorlax's dream was eaten!", Turn(Wild(gengar, snorlax)));
        Assert.Equal((220 - 53, 50 + 26), (snorlax.CurrentHP, gengar.CurrentHP));
    }

    [Fact]
    public void HazeClearsEveryStatStageOnTheField()
    {
        var blastoise = Mon("Blastoise", 50, "Swords Dance", "Haze");
        var machamp = Mon("Machamp", 50, "Growl");
        var core = Wild(blastoise, machamp);
        Turn(core, 0);
        Assert.Equal(1, blastoise.StatStages[StatType.Attack]);     // + 2 from the dance, − 1 from the growl
        Assert.Contains("All stat changes were eliminated!", Turn(core, 1));
        Assert.Equal(-1, blastoise.StatStages.GetValueOrDefault(StatType.Attack));  // Machamp growls after the haze
    }

    [Fact]
    public void RageBuildsWithEveryHitTaken()
    {
        var snorlax = Idling(Mon("Snorlax", 50, "Rage"));
        var core = Wild(snorlax, Mon("Machamp", 50, "Tackle"));
        Turn(core, 0);          // Machamp is the faster: the first Tackle lands before the rage is on
        Assert.Equal(0, snorlax.StatStages.GetValueOrDefault(StatType.Attack));
        Assert.Contains("Snorlax's rage is building!", Turn(core, 0));
        Assert.Equal(1, snorlax.StatStages[StatType.Attack]);
        // Once it uses anything else the rage is over
        Turn(core, 1);
        Assert.DoesNotContain("Snorlax's rage is building!", Turn(core, 1));
    }

    // ================================================================== the modern rules, and the screen

    [Fact]
    public void ByTheModernRulesGrassShrugsOffPowderAndAPoisonTypesToxicIsSure()
    {
        var venusaur = Mon("Venusaur", 50);
        Turn(Wild(Mon("Blastoise", 50, "Sleep Powder"), venusaur));
        Assert.Equal(StatusCondition.Sleep, venusaur.Status);
        venusaur = Mon("Venusaur", 50);
        Assert.Contains("It doesn't affect Foe Venusaur...", Turn(Wild(Mon("Blastoise", 50, "Sleep Powder"), venusaur, rules: Ruleset.Modern)));
        Assert.Equal(StatusCondition.None, venusaur.Status);

        // Toxic is 85% accurate: a roll of 99 misses, unless a Poison type uses it by the modern rules
        Assert.Contains("Gengar's attack missed!", Turn(Wild(Mon("Gengar", 50, "Toxic"), Mon("Snorlax", 50, "Tackle"), Calm().Force(RollKind.Accuracy, 99))));
        var blastoise = Mon("Blastoise", 50);
        Turn(Wild(Mon("Gengar", 50, "Toxic"), blastoise, Calm().Force(RollKind.Accuracy, 99), Ruleset.Modern));
        Assert.Equal(StatusCondition.Toxic, blastoise.Status);
    }

    [Fact]
    public void TheWeatherOfThePlaceComesIntoTheBattle()
    {
        // BattleSystem_TriggerEffectOnSwitch's field weather: rain of any strength is rain, snow of any is hail
        Assert.Equal(BattleWeather.Rain, PokemonPlatinumEngine.Overworld.Weathers.InBattle(PokemonPlatinumEngine.Overworld.FieldWeather.Thunderstorm));
        Assert.Equal(BattleWeather.Hail, PokemonPlatinumEngine.Overworld.Weathers.InBattle(PokemonPlatinumEngine.Overworld.FieldWeather.HeavySnow));
        Assert.Equal(BattleWeather.Sandstorm, PokemonPlatinumEngine.Overworld.Weathers.InBattle(PokemonPlatinumEngine.Overworld.FieldWeather.Sandstorm));
        Assert.Equal(BattleWeather.Fog, PokemonPlatinumEngine.Overworld.Weathers.InBattle(PokemonPlatinumEngine.Overworld.FieldWeather.Fog));
        Assert.Equal(BattleWeather.None, PokemonPlatinumEngine.Overworld.Weathers.InBattle(PokemonPlatinumEngine.Overworld.FieldWeather.Cloudy));

        // And a place that bends the order of things opens with Trick Room
        var party = new Party();
        party.Add(Mon("Snorlax", 50, "Growl"));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Mon("Alakazam", 50) }, Random = Calm(), Rules = Ruleset.Platinum,
            Conditions = new BattleConditions { TrickRoom = true }, EnemyController = null
        });
        core.Start();
        InOrder(Turn(core), "The dimensions became distorted!", "Snorlax used Growl!", "Foe Alakazam used Idle!");
    }

    [Fact]
    public void TheMenusRefuseWhatTheRulesRefuse()
    {
        // A taunted Pokémon's status move: said, and back to the moves
        var battle = Battle(Mon("Blastoise", 50, "Growl", "Tackle"), Mon("Alakazam", 50, "Taunt"));
        Scenario.Turn(battle, 1);
        battle.SelectMove(0);
        Assert.Equal("Blastoise can't use Growl after the taunt!", battle.CurrentMessage);
        Settle(battle);
        Assert.Equal(BattleMenuState.Moves, battle.HUD.MenuState);

        // Held on the field: neither switched out nor away, and the turn isn't spent
        var party = new Party();
        party.Add(Mon("Blastoise", 50));
        party.Add(Mon("Snorlax", 50));
        battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon("Wobbuffet", 50) }, Random = Steady(), Rules = Ruleset.Platinum
        });
        Settle(battle);
        battle.SelectSwitch(1);
        Assert.Equal("Foe Wobbuffet prevents escape with Shadow Tag!", battle.CurrentMessage);
        Settle(battle);
        Assert.Equal(BattleMenuState.SwitchPokemon, battle.HUD.MenuState);
        battle.SelectMainMenuOption(3);
        Assert.Equal("Foe Wobbuffet prevents escape with Shadow Tag!", battle.CurrentMessage);
        Settle(battle);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Equal(0, battle.Core.Turn);
    }

    [Fact]
    public void ATwoTurnMoveIsPlayedThroughAndThePokemonIsOffItsPlatformMeanwhile()
    {
        // The second turn asks nothing of anyone (the foe is chosen for from inside), so both are shown in one go
        var charizard = Mon("Charizard", 50, "Fly");
        var battle = Battle(charizard, Mon("Snorlax", 50));
        battle.SelectMove(0);

        var said = new List<string>();
        bool awayInTheAir = false, backForTheStrike = false;
        for (int i = 0; i < 400 && battle.HUD.MenuState == BattleMenuState.Message; i++)
        {
            if (battle.IsWaitingForConfirm)
            {
                said.Add(battle.CurrentMessage);
                if (battle.CurrentMessage == "Foe Snorlax used Idle!" && !said.Contains("Charizard used Fly!")) awayInTheAir = battle.Anim.Player.Away;
                if (battle.CurrentMessage == "Charizard used Fly!") backForTheStrike = !battle.Anim.Player.Away;
            }
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }

        InOrder(said, "Charizard flew up high!", "Foe Snorlax used Idle!", "Charizard used Fly!");
        Assert.True(awayInTheAir);
        Assert.True(backForTheStrike);
        Assert.False(battle.Anim.Player.Away);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Equal(2, battle.Core.Turn);
        Assert.Equal(14, charizard.Moves[0].CurrentPP);
    }

    [Fact]
    public void TheScreenFollowsTheWeather()
    {
        var battle = Battle(Mon("Kyogre", 50), Mon("Machamp", 50));
        Assert.Equal(BattleWeather.Rain, battle.Weather);
        Assert.Equal(BattleWeather.Rain, battle.Core.Field.Weather);
    }

    [Fact]
    public void BattlesFullOfTheseMovesAlwaysEndAndReplayTheSame()
    {
        // Teams of anything from Platinum, each with three of the moves this session gave their effects and one
        // plain attack; single and double, under every sky, by both sets of rules, both sides chosen for
        var effects = MoveDatabase.GetAll().Where(m => m.Kind == MoveKind.Standard && m.Effect != null && BattleCore.HasMoveEffect(m.Effect)).OrderBy(m => m.Id).ToList();
        var attacks = MoveDatabase.GetAll().Where(m => m.Kind == MoveKind.Standard && m.Effect == null && m.Category != MoveCategory.Status && m.Power >= 40).OrderBy(m => m.Id).ToList();
        var species = PokemonDatabase.GetAll().Where(s => s.Generation <= 4).OrderBy(s => s.DexNumber).ToList();
        Assert.True(effects.Count > 120, $"{effects.Count} moves have an effect of their own in the engine");

        List<string> Play(int battle)
        {
            var dice = new Random(battle * 104729 + 11);
            Party Team()
            {
                var party = new Party();
                for (int i = 0; i < 3; i++)
                {
                    var p = new Pokemon(species[dice.Next(species.Count)], dice.Next(30, 60), dice);
                    p.Moves.Clear();
                    while (p.Moves.Count < 3)
                    {
                        var move = effects[dice.Next(effects.Count)];
                        if (p.Moves.All(m => m.Data != move)) p.Moves.Add(new Move(move));
                    }
                    p.Moves.Add(new Move(attacks[dice.Next(attacks.Count)]));
                    party.Add(p);
                }
                return party;
            }
            var mine = Team();
            var rival = new Trainer { Name = "Tester", TrainerClass = "Ace Trainer", Party = Team() };
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = mine, Trainers = new List<Trainer> { rival }, Random = new BattleRandom((uint)battle + 1000),
                Rules = battle % 5 == 0 ? Ruleset.Modern : Ruleset.Platinum,
                Format = battle % 3 == 0 ? BattleFormat.Double : BattleFormat.Single,
                Conditions = new BattleConditions { Weather = (BattleWeather)(battle % 6), TrickRoom = battle % 11 == 0 },
                PlayerController = TrainerAi.Instance
            });
            core.Start();

            Assert.Null(core.Request);
            Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.PlayerDefeat, $"battle {battle} ended as {core.Result}");
            Assert.True(core.Turn < 2000, $"battle {battle} took {core.Turn} turns");
            Assert.IsType<Ended>(core.Log[^1]);
            foreach (var p in mine.Members.Concat(rival.Party.Members))
            {
                Assert.InRange(p.CurrentHP, 0, p.MaxHP);
                Assert.All(p.Moves, m => Assert.InRange(m.CurrentPP, 0, m.MaxPP));
                Assert.All(p.StatStages.Values, stage => Assert.InRange(stage, -6, 6));
            }
            foreach (var b in core.AllBattlers) Assert.True(b.Volatile.SubstituteHp >= 0);
            bool mineDown = mine.Members.All(p => p.IsFainted), theirsDown = rival.Party.Members.All(p => p.IsFainted);
            Assert.True(core.Result == BattleResult.PlayerDefeat ? mineDown : theirsDown, $"battle {battle}: {core.Result}");
            return core.Log.Select(e => e is Said s ? $"{s.Text} | {string.Join("; ", s.Shows)} | {string.Join("; ", s.OnImpact)}" : e.ToString()!).ToList();
        }

        for (int battle = 0; battle < 240; battle++) Play(battle);
        foreach (int battle in new[] { 3, 10, 55, 121 }) Assert.Equal(Play(battle), Play(battle));
    }
}
