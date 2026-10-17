using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
/// The later generations' moves, batch 2 (plan 06 · R25): the rest of Generations 5 to 7, each by Pokémon
/// Showdown's rule (data/moves.ts at the pinned commit). One rule to a test, under the modern rules, the rolls
/// fixed by kind (<see cref="CoreScenario.Calm"/>: no critical hit, the strongest roll, nothing misses, no side
/// effect). The Pokémon have no IVs, EVs or nature (<see cref="Scenario.Mon"/>): a stat at level 50 is
/// base + 5, and HP base + 60.
/// </summary>
public class LaterMoves2Tests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static BattleCore Duel(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, BattleWeather sky = BattleWeather.None) =>
        Wild(mine, foe, rolls, Modern, sky);

    private static BattleCore Pair(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null) => Doubles(mine, theirs, rolls, Modern);

    /// <summary>A turn of a single battle, with what was said and every event the lines carried.</summary>
    private static (List<string> Said, List<BattleEvent> Shown) TurnWithEvents(BattleCore core, int mine, int foe)
    {
        core.Submit(BattleChoice.Fight(Mine, mine), BattleChoice.Fight(Foe, foe));
        var log = core.TakeLog().ToList();
        return (Lines(log), log.OfType<Said>().SelectMany(s => s.Shows).Concat(log).ToList());
    }

    private static int Stage(BattleCore core, Place place, StatType stat) => core.At(place).Pokemon!.StatStages.GetValueOrDefault(stat);

    private static int IndexOf(List<string> said, string start) => said.FindIndex(l => l.StartsWith(start));

    // ------------------------------------------------------------------ the rooms

    [Fact]
    public void WonderRoomSwapsTheDefensesAMoveIsTakenOn()
    {
        // Alakazam's Psychic (90; Sp. Atk 140) against Blissey (Defense 15, Sp. Def 140), the strongest roll:
        // as it stands, floor(floor(22 × 90 × 140 / 140) / 50) + 2 = 41, × 1.5 for its own type = 61 (rounded half down);
        // in Wonder Room, against the Defense: floor(22 × 90 × 140 / 15) = 18480, / 50 = 369, + 2 = 371, × 1.5 = 556.
        var core = Duel(Mon("Alakazam", 50, "Psychic"), Idling(Mon("Blissey", 50)));
        var move = core.At(Mine).Pokemon!.Moves[0];
        Assert.Equal(61, DamageCalculator.Calculate(core.At(Mine), core.At(Foe), move, Steady(), false, rules: Modern).Damage);
        core.Field.WonderRoomTurns = 5;
        Assert.Equal(556, DamageCalculator.Calculate(core.At(Mine), core.At(Foe), move, Steady(), false, rules: Modern).Damage);
    }

    [Fact]
    public void WonderRoomLastsFiveTurnsAndCarriesItsEvents()
    {
        var core = Duel(Idling(Mon("Alakazam", 50, "Wonder Room")), Idling(Mon("Snorlax", 50)));
        var (said, shown) = TurnWithEvents(core, 0, 0);
        Assert.Contains("A strange room swapped Defense and Sp. Def!", said);
        Assert.Contains(shown, e => e is FieldConditionChanged { Condition: FieldCondition.WonderRoom, Up: true });
        Assert.True(core.Field.Has(FieldCondition.WonderRoom));
        for (int turn = 2; turn <= 4; turn++) Turn(core, 1, 0);
        Assert.True(core.Field.Has(FieldCondition.WonderRoom));
        (said, shown) = TurnWithEvents(core, 1, 0);
        Assert.Contains("The strange room faded, and Defense and Sp. Def went back!", said);
        Assert.Contains(shown, e => e is FieldConditionChanged { Condition: FieldCondition.WonderRoom, Up: false });
        Assert.False(core.Field.Has(FieldCondition.WonderRoom));
    }

    [Fact]
    public void ARoomUsedAgainWhileItStandsEnds()
    {
        var core = Duel(Mon("Alakazam", 50, "Magic Room"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.True(core.Field.Has(FieldCondition.MagicRoom));
        Assert.Contains("The strange room faded, and held items work again!", Turn(core, 0, 0));
        Assert.False(core.Field.Has(FieldCondition.MagicRoom));
    }

    [Fact]
    public void MagicRoomPutsEveryHeldItemOutOfUse()
    {
        var foe = With(Idling(Mon("Snorlax", 50)), item: "Leftovers");
        var core = Duel(Mon("Alakazam", 50, "Magic Room"), foe);
        Assert.NotNull(BattleEffects.ItemOf(core.At(Foe)));
        foe.CurrentHP = 100;
        Turn(core, 0, 0);
        // Leftovers would have given back 220 / 16 = 13
        Assert.Equal(100, foe.CurrentHP);
        Assert.Null(BattleEffects.ItemOf(core.At(Foe)));
        Assert.Null(BattleEffects.HoldEffectOf(core.At(Foe)));
    }

    // ------------------------------------------------------------------ guards and shields

    [Fact]
    public void WideGuardStopsAMoveAtMoreThanOneFoe()
    {
        var core = Pair(new[] { Mon("Hariyama", 50, "Wide Guard"), Idling(Mon("Snorlax", 50)) },
            new[] { Mon("Golem", 50, "Rock Slide"), Idling(Mon("Snorlax", 50)) });
        var said = DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Contains("Wide Guard protected Hariyama!", said);
        Assert.Contains("Wide Guard protected Snorlax!", said);
        Assert.Equal(core.At(Mine).Pokemon!.MaxHP, core.At(Mine).Pokemon!.CurrentHP);
        Assert.Equal(core.At(Mine2).Pokemon!.MaxHP, core.At(Mine2).Pokemon!.CurrentHP);
    }

    [Fact]
    public void QuickGuardStopsAMoveThatGoesFirst()
    {
        var core = Duel(Mon("Hariyama", 50, "Quick Guard"), Mon("Jolteon", 50, "Quick Attack"));
        var said = Turn(core, 0, 0);
        Assert.Contains("Quick Guard protected Hariyama!", said);
        Assert.Equal(core.At(Mine).Pokemon!.MaxHP, core.At(Mine).Pokemon!.CurrentHP);
    }

    [Fact]
    public void CraftyShieldStopsAStatusMoveAndLetsAHitThrough()
    {
        var core = Duel(Mon("Klefki", 50, "Crafty Shield"), Mon("Jolteon", 50, "Thunder Wave", "Tackle"));
        Assert.Contains("Crafty Shield protected Klefki!", Turn(core, 0, 0));
        Assert.Equal(StatusCondition.None, core.At(Mine).Pokemon!.Status);
        Turn(core, 0, 1);
        Assert.True(core.At(Mine).Pokemon!.CurrentHP < core.At(Mine).Pokemon!.MaxHP);
    }

    [Fact]
    public void MatBlockWorksOnItsUsersFirstTurnAlone()
    {
        var core = Duel(Mon("Hariyama", 50, "Mat Block"), Mon("Snorlax", 50, "Tackle"));
        Assert.Contains("Mat Block protected Hariyama!", Turn(core, 0, 0));
        Assert.Equal(core.At(Mine).Pokemon!.MaxHP, core.At(Mine).Pokemon!.CurrentHP);
        Assert.Contains("But it failed!", Turn(core, 0, 0));
    }

    [Fact]
    public void KingsShieldTakesAStageOfAttackFromWhoeverTouchesIt()
    {
        var core = Duel(Mon("Aegislash", 50, "King's Shield"), Mon("Machamp", 50, "Tackle"));
        Assert.Contains("Aegislash protected itself!", Turn(core, 0, 0));
        Assert.Equal(-1, Stage(core, Foe, StatType.Attack));
    }

    [Fact]
    public void KingsShieldLetsAStatusMoveThrough()
    {
        var core = Duel(Mon("Aegislash", 50, "King's Shield"), Mon("Machamp", 50, "Leer"));
        Turn(core, 0, 0);
        Assert.Equal(-1, Stage(core, Mine, StatType.Defense));
    }

    [Fact]
    public void SpikyShieldHurtsWhoeverTouchesItByAnEighth()
    {
        // Snorlax's 220 HP: 27 lost
        var core = Duel(Mon("Chesnaught", 50, "Spiky Shield"), Mon("Snorlax", 50, "Tackle"));
        Assert.Contains("Foe Snorlax was hurt by Chesnaught's spikes!", Turn(core, 0, 0));
        Assert.Equal(220 - 27, core.At(Foe).Pokemon!.CurrentHP);
    }

    [Fact]
    public void BanefulBunkerPoisonsWhoeverTouchesIt()
    {
        var core = Duel(Mon("Toxapex", 50, "Baneful Bunker"), Mon("Machamp", 50, "Tackle"));
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.Poison, core.At(Foe).Pokemon!.Status);
    }

    [Fact]
    public void HyperspaceHoleBreaksThroughProtect()
    {
        var core = Duel(Mon("Hoopa", 50, "Hyperspace Hole"), Mon("Snorlax", 50, "Protect"));
        var said = Turn(core, 0, 0);
        Assert.Contains("It broke through Foe Snorlax's protection!", said);
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < core.At(Foe).Pokemon!.MaxHP);
    }

    [Fact]
    public void HyperspaceFuryIsHoopaUnboundsAlone()
    {
        var core = Duel(Mon("Hoopa", 50, "Hyperspace Fury"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("But it failed!", Turn(core, 0, 0));
        Assert.Equal(core.At(Foe).Pokemon!.MaxHP, core.At(Foe).Pokemon!.CurrentHP);
    }

    // ------------------------------------------------------------------ the order of the turn

    [Fact]
    public void AfterYouLetsItsTargetGoNext()
    {
        // Jolteon (135), Foe Pikachu (95), Hariyama (55), Foe Snorlax (35): Hariyama goes before the foes
        var core = Pair(new[] { Mon("Jolteon", 50, "After You"), Idling(Mon("Hariyama", 50)) },
            new[] { Idling(Mon("Pikachu", 50)), Idling(Mon("Snorlax", 50)) });
        var said = DoubleTurn(core, (Mine, 0, Mine2), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        InOrder(said, "Jolteon used After You!", "Hariyama took the kind offer!", "Hariyama used Idle!", "Foe Pikachu used Idle!");
    }

    [Fact]
    public void AfterYouAndQuashFailInASingleBattle()
    {
        var core = Duel(Mon("Jolteon", 50, "After You", "Quash"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("But it failed!", Turn(core, 0, 0));
        Assert.Contains("But it failed!", Turn(core, 1, 0));
    }

    [Fact]
    public void QuashPutsItsTargetLast()
    {
        var core = Pair(new[] { Mon("Jolteon", 50, "Quash"), Idling(Mon("Hariyama", 50)) },
            new[] { Idling(Mon("Pikachu", 50)), Idling(Mon("Snorlax", 50)) });
        var said = DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        InOrder(said, "Jolteon used Quash!", "Foe Pikachu's move was put off to the last!", "Hariyama used Idle!", "Foe Snorlax used Idle!", "Foe Pikachu used Idle!");
    }

    [Fact]
    public void RoundBringsTheOtherRoundForwardAtTwiceThePower()
    {
        // Snorlax, the slowest, uses Round on one Rhydon; Jolteon idles the first turn and uses Round on the other
        // the second, so Snorlax's comes straight after it, before both foes, and twice as strong
        var core = Pair(new[] { Idling(Mon("Jolteon", 50, "Round")), Mon("Snorlax", 50, "Round") },
            new[] { Idling(Mon("Rhydon", 50)), Idling(Mon("Rhydon", 50)) });
        var target = core.At(Foe).Pokemon!;
        int before = target.CurrentHP;
        DoubleTurn(core, (Mine, 1, null), (Mine2, 0, Foe), (Foe, 0, null), (Foe2, 0, null));
        int once = before - target.CurrentHP;
        before = target.CurrentHP;
        var said = DoubleTurn(core, (Mine, 0, Foe2), (Mine2, 0, Foe), (Foe, 0, null), (Foe2, 0, null));
        int twice = before - target.CurrentHP;
        InOrder(said, "Jolteon used Round!", "Snorlax used Round!", "Foe Rhydon used Idle!");
        Assert.InRange(twice, 2 * once - 2, 2 * once + 1);
    }

    [Fact]
    public void InstructMakesItsTargetUseItsMoveAgain()
    {
        var core = Pair(new[] { Mon("Jolteon", 50, "Tackle"), Mon("Oranguru", 50, "Instruct") },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        var said = DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, Mine), (Foe, 0, null), (Foe2, 0, null));
        Assert.Contains("Jolteon followed Oranguru's instructions!", said);
        Assert.Equal(2, said.Count(l => l == "Jolteon used Tackle!"));
    }

    [Fact]
    public void SpotlightDrawsTheFoesMovesToItsTarget()
    {
        var core = Pair(new[] { Mon("Jolteon", 50, "Spotlight"), Idling(Mon("Hariyama", 50)) },
            new[] { Mon("Snorlax", 50, "Tackle"), Idling(Mon("Snorlax", 50)) });
        DoubleTurn(core, (Mine, 0, Mine2), (Mine2, 0, null), (Foe, 0, Mine), (Foe2, 0, null));
        Assert.Equal(core.At(Mine).Pokemon!.MaxHP, core.At(Mine).Pokemon!.CurrentHP);
        Assert.True(core.At(Mine2).Pokemon!.CurrentHP < core.At(Mine2).Pokemon!.MaxHP);
    }

    [Fact]
    public void BeakBlastHeatsUpFirstAndBurnsWhoeverTouchesIt()
    {
        var core = Duel(Mon("Toucannon", 50, "Beak Blast"), Mon("Snorlax", 50, "Tackle"));
        var said = Turn(core, 0, 0);
        InOrder(said, "Toucannon started heating up its beak!", "Foe Snorlax used Tackle!", "Toucannon used Beak Blast!");
        Assert.Equal(StatusCondition.Burn, core.At(Foe).Pokemon!.Status);
    }

    [Fact]
    public void ShellTrapGoesOffOnlyAfterAPhysicalHit()
    {
        var core = Duel(Mon("Turtonator", 50, "Shell Trap"), Mon("Machamp", 50, "Tackle", "Swift"));
        var said = Turn(core, 0, 0);
        InOrder(said, "Turtonator set a shell trap!", "Foe Machamp used Tackle!", "Turtonator used Shell Trap!");
        Assert.DoesNotContain("Turtonator's shell trap didn't go off!", said);
        Assert.Contains("Turtonator's shell trap didn't go off!", Turn(core, 0, 1));
    }

    [Fact]
    public void FairyLockHoldsEveryoneInForTheNextTurn()
    {
        var core = Duel(Mon("Klefki", 50, "Fairy Lock", "Tackle"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.False(core.CanSwitchOut(core.At(Mine)));
        Assert.False(core.CanSwitchOut(core.At(Foe)));
        Turn(core, 1, 0);
        Assert.True(core.CanSwitchOut(core.At(Foe)));
    }

    // ------------------------------------------------------------------ moves with a type chart of their own

    [Fact]
    public void FlyingPressIsAFlyingMoveTooAgainstEachType()
    {
        // Against Breloom (Grass and Fighting): Fighting is neutral to both, Flying twice as strong against both: 4
        var core = Duel(Mon("Hawlucha", 50, "Flying Press"), Idling(Mon("Breloom", 50)));
        Assert.Equal(4f, DamageCalculator.Effectiveness(core.At(Mine), core.At(Foe), core.At(Mine).Pokemon!.Moves[0], Modern));
    }

    [Fact]
    public void FreezeDryIsTwiceAsStrongAgainstWater()
    {
        // Against Gyarados (Water and Flying): Ice's 2 against Flying, and 2 against Water in place of ½
        var core = Duel(Mon("Lapras", 50, "Freeze-Dry"), Idling(Mon("Gyarados", 50)));
        Assert.Equal(4f, DamageCalculator.Effectiveness(core.At(Mine), core.At(Foe), core.At(Mine).Pokemon!.Moves[0], Modern));
    }

    [Fact]
    public void ThousandArrowsReachesAFlyingTypeAndBringsItDown()
    {
        // Against Skarmory (Steel and Flying): Ground's 2 against Steel, the Flying type neutral
        var core = Duel(Mon("Zygarde", 50, "Thousand Arrows"), Idling(Mon("Skarmory", 50)));
        Assert.Equal(2f, DamageCalculator.Effectiveness(core.At(Mine), core.At(Foe), core.At(Mine).Pokemon!.Moves[0], Modern));
        Turn(core, 0, 0);
        Assert.True(core.At(Foe).Volatile.SmackedDown);
        Assert.True(DamageCalculator.IsGrounded(core.At(Foe)));
    }

    [Fact]
    public void ThousandArrowsGoesThroughLevitate()
    {
        var core = Duel(Mon("Zygarde", 50, "Thousand Arrows"), Idling(Mon("Bronzong", 50)));
        Assert.Equal("Levitate", core.At(Foe).Pokemon!.AbilityName);
        var said = Turn(core, 0, 0);
        Assert.DoesNotContain(said, l => l.Contains("Levitate"));
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < core.At(Foe).Pokemon!.MaxHP);
    }

    [Fact]
    public void RevelationDanceTakesItsUsersFirstType()
    {
        // Oricorio's Baile Style is Fire first: against Venusaur its Normal dance is a Fire move, and super effective
        var core = Duel(Mon("Oricorio", 50, "Revelation Dance"), Idling(Mon("Venusaur", 50)));
        Assert.Contains("It's super effective!", Turn(core, 0, 0));
    }

    // ------------------------------------------------------------------ Normal moves made Electric

    [Fact]
    public void IonDelugeMakesNormalMovesElectricForTheTurn()
    {
        // It goes first (+1): the Tackle after it is Electric, which a Ground type can't be hit by
        var core = Duel(Mon("Snorlax", 50, "Tackle"), Mon("Golem", 50, "Ion Deluge"));
        Assert.Contains("It doesn't affect Foe Golem...", Turn(core, 0, 0));
    }

    [Fact]
    public void PlasmaFistsLeavesNormalMovesElectricForTheTurn()
    {
        var core = Duel(Mon("Dugtrio", 50, "Plasma Fists"), Mon("Snorlax", 50, "Tackle"));
        Assert.Contains("It doesn't affect Dugtrio...", Turn(core, 0, 0));
    }

    [Fact]
    public void ElectrifyMakesTheTargetsMoveElectric()
    {
        var core = Duel(Mon("Dugtrio", 50, "Electrify"), Mon("Snorlax", 50, "Tackle"));
        var said = Turn(core, 0, 0);
        Assert.Contains("Foe Snorlax's moves have been electrified!", said);
        Assert.Contains("It doesn't affect Dugtrio...", said);
    }

    [Fact]
    public void PowderMakesAFireMoveExplode()
    {
        // Charizard's 138 HP: a quarter, rounded, is 35; its Ember never leaves it
        var core = Duel(Mon("Charizard", 50, "Ember"), Mon("Vivillon", 50, "Powder"));
        var said = Turn(core, 0, 0);
        InOrder(said, "Charizard is covered in powder!", "Charizard used Ember!", "The powder on Charizard burst into flame!");
        Assert.Equal(138 - 35, core.At(Mine).Pokemon!.CurrentHP);
        Assert.Equal(core.At(Foe).Pokemon!.MaxHP, core.At(Foe).Pokemon!.CurrentHP);
    }

    // ------------------------------------------------------------------ what a hit does besides

    [Fact]
    public void FellStingerRaisesItsUsersAttackByThreeOnAKnockOut()
    {
        var faint = Idling(Mon("Snorlax", 50));
        faint.CurrentHP = 1;
        var core = Against(new[] { Mon("Vespiquen", 50, "Fell Stinger") }, new[] { faint, Idling(Mon("Snorlax", 50)) }, rules: Modern);
        Turn(core, 0, 0);
        Assert.Equal(3, Stage(core, Mine, StatType.Attack));
    }

    [Fact]
    public void ThroatChopKeepsSoundMovesBackForTwoTurns()
    {
        var core = Duel(Mon("Krookodile", 50, "Throat Chop"), Mon("Exploud", 50, "Hyper Voice", "Tackle"));
        Turn(core, 0, 1);
        var hyperVoice = core.At(Foe).Pokemon!.Moves[0];
        Assert.Equal("Foe Exploud can't use Hyper Voice because of Throat Chop!", core.WhyNotMove(core.At(Foe), hyperVoice));
    }

    [Fact]
    public void ThroatChopsHoldEndsTheTurnAfter()
    {
        var core = Duel(Mon("Krookodile", 50, "Throat Chop", "Leer"), Mon("Exploud", 50, "Hyper Voice", "Tackle"));
        Turn(core, 0, 1);
        Turn(core, 1, 1);
        Assert.Null(core.WhyNotMove(core.At(Foe), core.At(Foe).Pokemon!.Moves[0]));
    }

    [Fact]
    public void SpiritShackleKeepsItsTargetFromLeaving()
    {
        var core = Duel(Mon("Decidueye", 50, "Spirit Shackle"), Idling(Mon("Machamp", 50)));
        Assert.True(core.CanSwitchOut(core.At(Foe)));
        Assert.Contains("Foe Machamp can no longer escape!", Turn(core, 0, 0));
        Assert.False(core.CanSwitchOut(core.At(Foe)));
    }

    [Fact]
    public void ThousandWavesKeepsItsTargetFromLeaving()
    {
        var core = Duel(Mon("Zygarde", 50, "Thousand Waves"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.False(core.CanSwitchOut(core.At(Foe)));
    }

    [Fact]
    public void SparklingAriaCuresABurn()
    {
        var foe = Idling(Mon("Snorlax", 50));
        foe.Status = StatusCondition.Burn;
        var core = Duel(Mon("Primarina", 50, "Sparkling Aria"), foe);
        Assert.Contains("Foe Snorlax's burn was healed!", Turn(core, 0, 0));
        Assert.Equal(StatusCondition.None, foe.Status);
    }

    [Fact]
    public void CoreEnforcerSuppressesTheAbilityOfATargetThatHasMoved()
    {
        // Jolteon (135) moves before Zygarde (100); Snorlax (35) after it
        var first = Duel(Mon("Zygarde", 50, "Core Enforcer"), Idling(Mon("Jolteon", 50)));
        Turn(first, 0, 0);
        Assert.True(first.At(Foe).Volatile.AbilitySuppressed);
        var second = Duel(Mon("Zygarde", 50, "Core Enforcer"), Idling(Mon("Snorlax", 50)));
        Turn(second, 0, 0);
        Assert.False(second.At(Foe).Volatile.AbilitySuppressed);
    }

    [Fact]
    public void SpectralThiefStealsTheTargetsRaisedStages()
    {
        var core = Duel(Mon("Marshadow", 50, "Spectral Thief"), Idling(Mon("Snorlax", 50)));
        core.At(Foe).Pokemon!.StatStages[StatType.Attack] = 2;
        Assert.Contains("Marshadow stole the target's raised stats!", Turn(core, 0, 0));
        Assert.Equal(2, Stage(core, Mine, StatType.Attack));
        Assert.Equal(0, Stage(core, Foe, StatType.Attack));
    }

    [Fact]
    public void BurnUpIsAFireTypesAndTakesTheTypeAway()
    {
        var core = Duel(Mon("Arcanine", 50, "Burn Up"), Idling(Mon("Blissey", 50)));
        Assert.Contains("Arcanine burned itself out!", Turn(core, 0, 0));
        Assert.False(core.At(Mine).HasType(PokemonType.Fire));
        Assert.Contains("But it failed!", Turn(core, 0, 0));
    }

    [Fact]
    public void MindBlownCostsHalfItsUsersHpRoundedUp()
    {
        // Blacephalon's 113 HP: 57 lost
        var core = Duel(Mon("Blacephalon", 50, "Mind Blown"), Idling(Mon("Blissey", 50)));
        Assert.Contains("Blacephalon was hurt by its own blast!", Turn(core, 0, 0));
        Assert.Equal(113 - 57, core.At(Mine).Pokemon!.CurrentHP);
    }

    [Fact]
    public void FirstImpressionWorksOnItsUsersFirstTurnAlone()
    {
        var core = Duel(Mon("Golisopod", 50, "First Impression"), Idling(Mon("Snorlax", 50)));
        Assert.DoesNotContain("But it failed!", Turn(core, 0, 0));
        Assert.Contains("But it failed!", Turn(core, 0, 0));
    }

    [Fact]
    public void StompingTantrumIsTwiceAsStrongAfterAMoveThatFailed()
    {
        // A Mat Block on the second turn fails; the Stomping Tantrum after it is twice the first
        var foe = Idling(Mon("Snorlax", 50));
        var core = Duel(Mon("Hariyama", 20, "Stomping Tantrum", "Mat Block"), foe);
        int before = foe.CurrentHP;
        Turn(core, 0, 0);
        int once = before - foe.CurrentHP;
        Assert.Contains("But it failed!", Turn(core, 1, 0));
        before = foe.CurrentHP;
        Turn(core, 0, 0);
        int twice = before - foe.CurrentHP;
        Assert.True(twice >= 2 * once - 2 && twice <= 2 * once + 1, $"{once} and then {twice}");
    }

    [Fact]
    public void WaterShurikenHitsTwoToFiveTimes()
    {
        var core = Duel(Mon("Greninja", 50, "Water Shuriken"), Idling(Mon("Snorlax", 50)));
        Assert.Contains(Turn(core, 0, 0), l => Regex.IsMatch(l, "^Hit [2-5] times!$"));
    }

    [Fact]
    public void PollenPuffHealsAnAllyByHalf()
    {
        // Snorlax's 220 HP: half is 110
        var ally = Idling(Mon("Snorlax", 50));
        var core = Pair(new[] { Mon("Ribombee", 50, "Pollen Puff"), ally }, new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        ally.CurrentHP = 100;
        DoubleTurn(core, (Mine, 0, Mine2), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(210, ally.CurrentHP);
    }

    // ------------------------------------------------------------------ status moves on another

    [Fact]
    public void TrickOrTreatAddsTheGhostTypeAndForestsCurseTakesItsPlace()
    {
        var core = Duel(Mon("Gourgeist", 50, "Trick-or-Treat", "Forest's Curse"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("The Ghost type was added to Foe Snorlax!", Turn(core, 0, 0));
        Assert.Equal(new[] { PokemonType.Normal, PokemonType.Ghost }, core.At(Foe).Types);
        Turn(core, 1, 0);
        Assert.Equal(new[] { PokemonType.Normal, PokemonType.Grass }, core.At(Foe).Types);
    }

    [Fact]
    public void TopsyTurvyTurnsEveryStageRound()
    {
        var core = Duel(Mon("Malamar", 50, "Topsy-Turvy"), Idling(Mon("Snorlax", 50)));
        core.At(Foe).Pokemon!.StatStages[StatType.Attack] = 2;
        core.At(Foe).Pokemon!.StatStages[StatType.Defense] = -1;
        Assert.Contains("Foe Snorlax's stat changes were turned around!", Turn(core, 0, 0));
        Assert.Equal(-2, Stage(core, Foe, StatType.Attack));
        Assert.Equal(1, Stage(core, Foe, StatType.Defense));
    }

    [Fact]
    public void PartingShotLowersTheTargetAndSendsItsUserBack()
    {
        var core = Against(new[] { Mon("Incineroar", 50, "Parting Shot"), Idling(Mon("Snorlax", 50)) }, new[] { Idling(Mon("Machamp", 50)) }, rules: Modern);
        Turn(core, 0, 0);
        Assert.Equal(-1, Stage(core, Foe, StatType.Attack));
        Assert.Equal(-1, Stage(core, Foe, StatType.SpAttack));
        Assert.Equal(Mine, Assert.IsType<ReplacementRequest>(core.Request).Place);
    }

    [Fact]
    public void VenomDrenchLowersAPoisonedFoeAlone()
    {
        var poisoned = Idling(Mon("Snorlax", 50));
        poisoned.Status = StatusCondition.Poison;
        var core = Duel(Mon("Toxapex", 50, "Venom Drench"), poisoned);
        Turn(core, 0, 0);
        foreach (var stat in new[] { StatType.Attack, StatType.SpAttack, StatType.Speed }) Assert.Equal(-1, Stage(core, Foe, stat));
        var healthy = Duel(Mon("Toxapex", 50, "Venom Drench"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("But it failed!", Turn(healthy, 0, 0));
    }

    [Fact]
    public void TelekinesisMakesEveryMoveFindItsTargetAndNoGroundMove()
    {
        // Every accuracy roll is the worst: Focus Blast (70) would miss
        var core = Duel(Mon("Alakazam", 50, "Telekinesis", "Focus Blast", "Earthquake"), Idling(Mon("Snorlax", 50)), Calm().Force(RollKind.Accuracy, 99));
        Assert.Contains("Foe Snorlax was hurled into the air!", Turn(core, 0, 0));
        Assert.DoesNotContain(Turn(core, 1, 0), l => l.Contains("missed") || l.Contains("avoided"));
        Assert.Contains("It doesn't affect Foe Snorlax...", Turn(core, 2, 0));
    }

    [Fact]
    public void StrengthSapHealsByTheTargetsAttackAndLowersIt()
    {
        // Machamp's Attack is 135; at −1 it is 135 × 2 / 3 = 90
        var user = Mon("Blissey", 50, "Strength Sap");
        var core = Duel(user, Idling(Mon("Machamp", 50)));
        user.CurrentHP = 100;
        Turn(core, 0, 0);
        Assert.Equal(235, user.CurrentHP);
        Assert.Equal(-1, Stage(core, Foe, StatType.Attack));
        user.CurrentHP = 100;
        Turn(core, 0, 0);
        Assert.Equal(190, user.CurrentHP);
    }

    [Fact]
    public void FloralHealingHealsTheTargetByHalfRoundedUp()
    {
        // Blissey's 315 HP: 158
        var target = Idling(Mon("Blissey", 50));
        var core = Duel(Mon("Comfey", 50, "Floral Healing"), target);
        target.CurrentHP = 100;
        Turn(core, 0, 0);
        Assert.Equal(258, target.CurrentHP);
    }

    [Fact]
    public void PurifyCuresTheTargetAndHealsTheUser()
    {
        // Nihilego's 169 HP: 85
        var foe = Idling(Mon("Snorlax", 50));
        foe.Status = StatusCondition.Paralyze;
        var user = Mon("Nihilego", 50, "Purify");
        var core = Duel(user, foe);
        user.CurrentHP = 10;
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
        Assert.Equal(95, user.CurrentHP);
    }

    [Fact]
    public void SpeedSwapSwapsTheTwoSpeeds()
    {
        var core = Duel(Mon("Jolteon", 50, "Speed Swap"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.Equal(35, core.At(Mine).Pokemon!.Speed);
        Assert.Equal(135, core.At(Foe).Pokemon!.Speed);
    }

    [Fact]
    public void LaserFocusMakesTheNextHitCritical()
    {
        var core = Duel(Mon("Pangoro", 50, "Laser Focus", "Tackle"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("Pangoro is focusing hard!", Turn(core, 0, 0));
        Assert.Contains("A critical hit!", Turn(core, 1, 0));
    }

    [Fact]
    public void BelchCantBeChosenUntilABerryIsEaten()
    {
        // Swalot's 160 HP, one over half: a Tackle brings it to half and it eats its Sitrus Berry
        var swalot = With(Idling(Mon("Swalot", 50, "Belch")), item: "Sitrus Berry");
        var core = Duel(swalot, Mon("Snorlax", 50, "Tackle"));
        swalot.CurrentHP = 81;
        var belch = swalot.Moves[0];
        Assert.NotNull(core.WhyNotMove(core.At(Mine), belch));
        Turn(core, 1, 0);
        Assert.Null(swalot.HeldItem);
        Assert.Null(core.WhyNotMove(core.At(Mine), belch));
    }

    // ------------------------------------------------------------------ status moves on its own side

    [Fact]
    public void ShoreUpHealsTwoThirdsInASandstorm()
    {
        // Palossand's 145 HP: this.modify(145, 0.5) = 72 (half, rounded half down), and in a sandstorm
        // this.modify(145, 0.667) = (145 × 2732 + 2047) / 4096 = 97
        var calm = Mon("Palossand", 50, "Shore Up");
        var core = Duel(calm, Idling(Mon("Snorlax", 50)));
        calm.CurrentHP = 1;
        Turn(core, 0, 0);
        Assert.Equal(73, calm.CurrentHP);
        var sandy = Mon("Palossand", 50, "Shore Up");
        var storm = Duel(sandy, Idling(Mon("Tyranitar", 50)), sky: BattleWeather.Sandstorm);
        sandy.CurrentHP = 1;
        Turn(storm, 0, 0);
        Assert.Equal(98, sandy.CurrentHP);
    }

    [Fact]
    public void RototillerRaisesTheGrassTypesOnTheGround()
    {
        var core = Duel(Mon("Torterra", 50, "Rototiller"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(core, Mine, StatType.Attack));
        Assert.Equal(1, Stage(core, Mine, StatType.SpAttack));
        Assert.Equal(0, Stage(core, Foe, StatType.Attack));
    }

    [Fact]
    public void FlowerShieldRaisesEveryGrassTypesDefense()
    {
        var core = Duel(Mon("Florges", 50, "Flower Shield"), Idling(Mon("Venusaur", 50)));
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(core, Foe, StatType.Defense));
        Assert.Equal(0, Stage(core, Mine, StatType.Defense));
    }

    [Fact]
    public void GearUpRaisesThoseWithPlusOrMinus()
    {
        var core = Duel(Mon("Klinklang", 50, "Gear Up"), Idling(Mon("Snorlax", 50)));
        Assert.Equal("Plus", core.At(Mine).Pokemon!.AbilityName);
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(core, Mine, StatType.Attack));
        Assert.Equal(1, Stage(core, Mine, StatType.SpAttack));
        var plain = Duel(Mon("Snorlax", 50, "Gear Up"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("But it failed!", Turn(plain, 0, 0));
    }

    [Fact]
    public void MagneticFluxRaisesTheDefensesOfThoseWithPlusOrMinus()
    {
        var core = Duel(Mon("Minun", 50, "Magnetic Flux"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(core, Mine, StatType.Defense));
        Assert.Equal(1, Stage(core, Mine, StatType.SpDefense));
    }

    [Fact]
    public void AromaticMistRaisesAnAllysSpDef()
    {
        var core = Pair(new[] { Mon("Aromatisse", 50, "Aromatic Mist"), Idling(Mon("Snorlax", 50)) }, new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        DoubleTurn(core, (Mine, 0, Mine2), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(1, Stage(core, Mine2, StatType.SpDefense));
    }

    [Fact]
    public void GeomancyChargesAndThenRaisesThreeStatsByTwo()
    {
        var core = Duel(Mon("Xerneas", 50, "Geomancy"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("Xerneas is absorbing power!", Turn(core, 0, 0));
        Assert.Equal(0, Stage(core, Mine, StatType.SpAttack));
        Turn(core, 0, 0);
        foreach (var stat in new[] { StatType.SpAttack, StatType.SpDefense, StatType.Speed }) Assert.Equal(2, Stage(core, Mine, stat));
    }

    [Fact]
    public void HappyHourDoublesThePrize()
    {
        var core = Against(new[] { Mon("Snorlax", 50, "Happy Hour") }, new[] { Idling(Mon("Snorlax", 50)) }, rules: Modern);
        Assert.Equal(100, core.PrizeMoney);
        Assert.Contains("Everyone is swept up in a happy mood!", Turn(core, 0, 0));
        Assert.Equal(200, core.PrizeMoney);
    }

    [Fact]
    public void CelebrateCongratulatesItsTrainer()
    {
        var core = Duel(Mon("Pikachu", 50, "Celebrate"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("Congratulations, Lucas!", Turn(core, 0, 0));
    }

    [Fact]
    public void HoldHandsTakesAnAllysHand()
    {
        var core = Pair(new[] { Mon("Pikachu", 50, "Hold Hands"), Idling(Mon("Snorlax", 50)) }, new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        Assert.Contains("Pikachu and Snorlax held hands!", DoubleTurn(core, (Mine, 0, Mine2), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null)));
    }

    [Fact]
    public void NoneOfThisBatchDoesNothingAnyMore()
    {
        // Every move of the batch that has an effect of its own runs it (the six left are named in plan 06 · R25)
        string[] left = { "Sky Drop", "Ally Switch", "Water Pledge", "Fire Pledge", "Grass Pledge", "Sappy Seed" };
        foreach (var move in MoveDatabase.GetAll().Where(m => m.Generation is >= 5 and <= 7 && m.Kind == MoveKind.Standard && !left.Contains(m.Name)))
            Assert.True(move.Effect == null || BattleCore.HasMoveEffect(move.Effect) || move.Support == MoveEffectSupport.Full, move.Name);
    }
}
