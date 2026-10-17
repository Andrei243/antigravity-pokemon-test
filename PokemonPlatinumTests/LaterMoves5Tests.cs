using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The later generations' moves, batch 3, its second half (plan 06 · R26b): Generation 9's, each by Pokémon
/// Showdown's rule (data/moves.ts at the pinned commit). One rule to a test, under the modern rules, the rolls fixed by
/// kind (<see cref="CoreScenario.Calm"/>: no critical hit, the strongest roll, nothing misses, no side effect but a
/// sure one). A foe made by <c>Mon</c> with no moves knows only Idle, so its move 0 sits the turn out.
/// </summary>
public class LaterMoves5Tests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static BattleCore Duel(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, BattleWeather sky = BattleWeather.None, params Pokemon[] bench) =>
        Wild(mine, foe, rolls, Modern, sky, BattleTerrain.Plain, bench);

    private static BattleCore Pair(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null) => Doubles(mine, theirs, rolls, Modern);

    private static List<BattleEvent> Shown(BattleCore core)
    {
        var log = core.TakeLog().ToList();
        return log.OfType<Said>().SelectMany(s => s.Shows.Concat(s.OnImpact)).Concat(log).ToList();
    }

    /// <summary>What the foe lost to the player's first move in one turn, the foe sitting it out; the core made ready first by <paramref name="before"/>.</summary>
    private static int Took(Pokemon mine, Pokemon foe, Action<BattleCore>? before = null, BattleWeather sky = BattleWeather.None)
    {
        var core = Duel(mine, foe, sky: sky);
        before?.Invoke(core);
        core.TakeLog();
        int hp = foe.CurrentHP;
        Turn(core, 0, 0);
        return hp - foe.CurrentHP;
    }

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

    // ------------------------------------------------------------------ by their data

    [Fact]
    public void JetPunchGoesFirst()
    {
        var core = Duel(Slow(Mon("Palafin", 50, "Jet Punch")), Fast(Mon("Jolteon", 50, "Tackle")));
        var said = Turn(core, 0, 0);
        InOrder(said, "Palafin used Jet Punch!", $"{core.At(Foe).Name} used Tackle!");
    }

    [Fact]
    public void FlowerTrickIsAlwaysACriticalHit() =>
        Assert.Contains("A critical hit!", Turn(Duel(Mon("Meowscarada", 50, "Flower Trick"), Mon("Snorlax", 50)), 0, 0));

    [Fact]
    public void MakeItRainLowersItsUsersSpAtkOnceForTwoTargets()
    {
        var core = Pair(new[] { Mon("Gholdengo", 50, "Make It Rain"), Mon("Snorlax", 50) }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) });
        DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(-1, Stage(core.At(Mine).Pokemon!, StatType.SpAttack));
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < core.At(Foe).Pokemon!.MaxHP);
        Assert.True(core.At(Foe2).Pokemon!.CurrentHP < core.At(Foe2).Pokemon!.MaxHP);
    }

    [Fact]
    public void HyperDrillGoesThroughProtect()
    {
        var core = Duel(Slow(Mon("Dudunsparce", 50, "Hyper Drill")), Fast(Mon("Snorlax", 50, "Protect")));
        Turn(core, 0, 0);
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < core.At(Foe).Pokemon!.MaxHP);
    }

    [Fact]
    public void MatchaGotchaThawsAFrozenTarget()
    {
        var foe = Mon("Snorlax", 50);
        foe.Status = StatusCondition.Freeze;
        var core = Duel(Mon("Sinistcha", 50, "Matcha Gotcha"), foe);
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, core.At(Foe).Pokemon!.Status);
    }

    // ------------------------------------------------------------------ several hits

    [Fact]
    public void TwinBeamHitsTwice() => Assert.Contains("Hit 2 times!", Turn(Duel(Mon("Farigiraf", 50, "Twin Beam"), Mon("Snorlax", 50)), 0, 0));

    [Fact]
    public void TachyonCutterHitsTwice() => Assert.Contains("Hit 2 times!", Turn(Duel(Mon("Archaludon", 50, "Tachyon Cutter"), Mon("Snorlax", 50)), 0, 0));

    [Fact]
    public void TripleDiveHitsThreeTimes() => Assert.Contains("Hit 3 times!", Turn(Duel(Mon("Palafin", 50, "Triple Dive"), Mon("Snorlax", 70)), 0, 0));

    [Fact]
    public void PopulationBombHitsTenTimesWhenNothingMisses() =>
        Assert.Contains("Hit 10 times!", Turn(Duel(Mon("Maushold", 50, "Population Bomb"), Mon("Snorlax", 100)), 0, 0));

    [Fact]
    public void PopulationBombStopsAtAMiss()
    {
        // Every roll after the first misses: one hit, then it stops
        var rolls = Calm().Force(RollKind.Accuracy, 0, 99);
        var said = Turn(Duel(Mon("Maushold", 50, "Population Bomb"), Mon("Snorlax", 100), rolls), 0, 0);
        Assert.Contains("Hit 1 time!", said);
    }

    // ------------------------------------------------------------------ shields

    [Fact]
    public void SilkTrapStopsAHitAndSlowsWhoeverTouchedIt()
    {
        var core = Duel(Mon("Spidops", 50, "Silk Trap"), Mon("Snorlax", 50, "Tackle"));
        var said = Turn(core, 0, 0);
        Assert.Contains("Spidops protected itself!", said);
        Assert.Equal(core.At(Mine).Pokemon!.MaxHP, core.At(Mine).Pokemon!.CurrentHP);
        Assert.Equal(-1, Stage(core.At(Foe).Pokemon!, StatType.Speed));
    }

    [Fact]
    public void SilkTrapLetsAStatusMoveThrough()
    {
        var core = Duel(Mon("Spidops", 50, "Silk Trap"), Mon("Snorlax", 50, "Growl"));
        Turn(core, 0, 0);
        Assert.Equal(-1, Stage(core.At(Mine).Pokemon!, StatType.Attack));
    }

    [Fact]
    public void BurningBulwarkBurnsWhoeverTouchedIt()
    {
        var core = Duel(Mon("Scovillain", 50, "Burning Bulwark"), Mon("Snorlax", 50, "Tackle"));
        Turn(core, 0, 0);
        Assert.Equal(core.At(Mine).Pokemon!.MaxHP, core.At(Mine).Pokemon!.CurrentHP);
        Assert.Equal(StatusCondition.Burn, core.At(Foe).Pokemon!.Status);
    }

    // ------------------------------------------------------------------ power and damage of their own

    [Fact]
    public void LastRespectsGrowsWithEveryFaintOfItsSide()
    {
        // A Magikarp falls to the foe's Tackle, then Houndstone's Last Respects is 100 strong instead of 50
        var core = Against(new[] { Mon("Magikarp", 1), Mon("Houndstone", 50, "Last Respects") }, new[] { Fast(Mon("Snorlax", 50, "Tackle")) });
        Turn(core, 0, 0);
        SendIn(core, 1);
        var foe = core.At(Foe).Pokemon!;
        int hp = foe.CurrentHP;
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        int once = hp - foe.CurrentHP;

        int fresh = Took(Mon("Houndstone", 50, "Last Respects"), Mon("Snorlax", 50));
        Assert.InRange(once, fresh * 2 - 4, fresh * 2 + 4);
    }

    [Fact]
    public void RageFistGrowsWithEveryHitTaken()
    {
        // Hit once by a faster foe's Tackle, Rage Fist is 100 strong
        var core = Duel(Slow(Mon("Annihilape", 50, "Rage Fist")), Fast(Mon("Snorlax", 50, "Tackle")));
        var foe = core.At(Foe).Pokemon!;
        int hp = foe.CurrentHP;
        Turn(core, 0, 0);
        int hit = hp - foe.CurrentHP;
        int fresh = Took(Mon("Annihilape", 50, "Rage Fist"), Mon("Snorlax", 50));
        Assert.InRange(hit, fresh * 2 - 4, fresh * 2 + 4);
    }

    [Fact]
    public void RuinationTakesHalfTheTargetsHp()
    {
        var foe = Mon("Snorlax", 50);
        int hp = foe.CurrentHP;
        Took(Mon("Chien-Pao", 50, "Ruination"), foe);
        Assert.Equal(hp - hp / 2, foe.CurrentHP);
    }

    [Fact]
    public void ComeuppanceGivesBackHalfAsMuchAgainAsItTook()
    {
        var core = Duel(Slow(Mon("Kingambit", 50, "Comeuppance")), Fast(Mon("Snorlax", 50, "Tackle")));
        var me = core.At(Mine).Pokemon!;
        var foe = core.At(Foe).Pokemon!;
        Turn(core, 0, 0);
        int taken = me.MaxHP - me.CurrentHP;
        Assert.Equal(taken * 15 / 10, foe.MaxHP - foe.CurrentHP);
    }

    [Fact]
    public void ComeuppanceFailsWhenNothingHitItThisTurn() =>
        Assert.Contains("But it failed!", Turn(Duel(Mon("Kingambit", 50, "Comeuppance"), Mon("Snorlax", 50)), 0, 0));

    [Fact]
    public void HardPressIsWeakerAgainstATargetWithLessHp()
    {
        int full = Took(Mon("Archaludon", 50, "Hard Press"), Mon("Snorlax", 50));
        var half = Mon("Snorlax", 50);
        half.CurrentHP = half.MaxHP / 2;
        int halved = Took(Mon("Archaludon", 50, "Hard Press"), half);
        Assert.InRange(halved, full / 2 - 3, full / 2 + 3);
    }

    [Fact]
    public void TemperFlareIsTwiceAsStrongAfterAFailure()
    {
        // Turn one: Temper Flare's user fails (a Comeuppance with nothing to give back); turn two, it is 150 strong
        var core = Duel(Mon("Ceruledge", 50, "Comeuppance", "Temper Flare"), Mon("Snorlax", 50));
        Turn(core, 0, 0);
        var foe = core.At(Foe).Pokemon!;
        int hp = foe.CurrentHP;
        Turn(core, 1, 0);
        int after = hp - foe.CurrentHP;
        int fresh = Took(Mon("Ceruledge", 50, "Temper Flare"), Mon("Snorlax", 50));
        Assert.InRange(after, fresh * 2 - 4, fresh * 2 + 4);
    }

    [Fact]
    public void PsybladeIsHalfAsStrongAgainOnElectricTerrain()
    {
        int plain = Took(Mon("Machamp", 50, "Psyblade"), Mon("Snorlax", 50));
        int charged = Took(Mon("Machamp", 50, "Psyblade"), Mon("Snorlax", 50), c => c.SetTerrain(FieldCondition.ElectricTerrain, null));
        Assert.InRange(charged, plain * 3 / 2 - 3, plain * 3 / 2 + 3);
    }

    [Fact]
    public void CollisionCourseIsStrongerWhereItDoesWell()
    {
        // Against a Normal type (twice as effective) its power is 133, Hammer Arm's 100: a third more
        int course = Took(Mon("Koraidon", 50, "Collision Course"), Mon("Snorlax", 100));
        int hammer = Took(Mon("Koraidon", 50, "Hammer Arm"), Mon("Snorlax", 100));
        Assert.InRange(course, hammer * 133 / 100 - 4, hammer * 133 / 100 + 4);
    }

    [Fact]
    public void FickleBeamGoesAllOutThreeTimesInTen()
    {
        var allOut = Turn(Duel(Mon("Hydrapple", 50, "Fickle Beam"), Mon("Snorlax", 50), Calm().Force(RollKind.Power, 0)), 0, 0);
        Assert.Contains("Hydrapple is going all out for this attack!", allOut);
        var plain = Turn(Duel(Mon("Hydrapple", 50, "Fickle Beam"), Mon("Snorlax", 50), Calm().Force(RollKind.Power, 3)), 0, 0);
        Assert.DoesNotContain("Hydrapple is going all out for this attack!", plain);
    }

    [Fact]
    public void HydroSteamIsStrongerInTheSun()
    {
        int clear = Took(Mon("Volcanion", 50, "Hydro Steam"), Mon("Snorlax", 50));
        int sunny = Took(Mon("Volcanion", 50, "Hydro Steam"), Mon("Snorlax", 50), sky: BattleWeather.Sun);
        Assert.InRange(sunny, clear * 3 / 2 - 3, clear * 3 / 2 + 3);
    }

    [Fact]
    public void DynamaxCannonIsTwiceAsStrongAgainstADynamaxedTarget()
    {
        // A Dynamaxed foe that sat the turn out would put up Max Guard: this one is slower and strikes
        int plain = Took(Fast(Mon("Eternatus", 50, "Dynamax Cannon")), Slow(Mon("Snorlax", 100, "Tackle")));
        int giant = Took(Fast(Mon("Eternatus", 50, "Dynamax Cannon")), Slow(Mon("Snorlax", 100, "Tackle")), c => c.At(Foe).Volatile.Dynamaxed = true);
        Assert.InRange(giant, plain * 2 - 4, plain * 2 + 4);
    }

    // ------------------------------------------------------------------ the type by its user

    [Fact]
    public void RagingBullTakesPaldeanTaurossTypeAndBreaksTheScreens()
    {
        var tauros = Mon("Tauros", 50, "Raging Bull");
        tauros.Form = "Tauros-Paldea-Blaze";
        var core = Duel(tauros, Mon("Snorlax", 50));
        core.Field.Side(BattleSide.Enemy).ReflectTurns = 5;
        core.TakeLog();
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var shown = Shown(core);
        Assert.Contains(shown, e => e is MoveShown { Move: "Raging Bull", Type: PokemonType.Fire });
        Assert.False(core.Field.Side(BattleSide.Enemy).Reflect);
    }

    [Fact]
    public void IvyCudgelTakesOgerponsMasksType()
    {
        var ogerpon = Mon("Ogerpon", 50, "Ivy Cudgel");
        ogerpon.Form = "Ogerpon-Wellspring";
        var core = Duel(ogerpon, Mon("Snorlax", 50));
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        Assert.Contains(Shown(core), e => e is MoveShown { Move: "Ivy Cudgel", Type: PokemonType.Water });
    }

    // ------------------------------------------------------------------ when it may be used

    [Fact]
    public void ThunderclapFailsAgainstATargetNotAttacking() =>
        Assert.Contains("But it failed!", Turn(Duel(Mon("Raging Bolt", 50, "Thunderclap"), Mon("Snorlax", 50)), 0, 0));

    [Fact]
    public void ThunderclapHitsFirstAgainstATargetAttacking()
    {
        var core = Duel(Slow(Mon("Raging Bolt", 50, "Thunderclap")), Fast(Mon("Snorlax", 50, "Tackle")));
        InOrder(Turn(core, 0, 0), "Raging Bolt used Thunderclap!", $"{core.At(Foe).Name} used Tackle!");
    }

    [Fact]
    public void UpperHandStrikesOnlyAMoveThatGoesFirst()
    {
        var quick = Duel(Mon("Annihilape", 50, "Upper Hand"), Mon("Snorlax", 50, "Quick Attack"));
        var said = Turn(quick, 0, 0);
        Assert.True(quick.At(Foe).Pokemon!.CurrentHP < quick.At(Foe).Pokemon!.MaxHP);
        Assert.Contains($"{quick.At(Foe).Name} flinched!", said);

        var plain = Duel(Mon("Annihilape", 50, "Upper Hand"), Mon("Snorlax", 50, "Tackle"));
        Assert.Contains("But it failed!", Turn(plain, 0, 0));
    }

    [Fact]
    public void DoubleShockLeavesItsUserNoLongerElectric()
    {
        var core = Duel(Mon("Pawmot", 50, "Double Shock"), Mon("Snorlax", 50));
        Assert.Contains("Pawmot used up all its electricity!", Turn(core, 0, 0));
        Assert.False(core.At(Mine).HasType(PokemonType.Electric));
        Assert.Contains("But it failed!", Turn(core, 0, 0));
    }

    [Fact]
    public void GigatonHammerCantBeUsedTwiceInARow()
    {
        var core = Duel(Mon("Tinkaton", 50, "Gigaton Hammer", "Tackle"), Mon("Snorlax", 100));
        var me = core.At(Mine);
        Assert.Null(core.WhyNotMove(me, me.Pokemon!.Moves[0]));
        Turn(core, 0, 0);
        Assert.Equal("Tinkaton can't use Gigaton Hammer twice in a row!", core.WhyNotMove(me, me.Pokemon!.Moves[0]));
        Turn(core, 1, 0);
        Assert.Null(core.WhyNotMove(me, me.Pokemon!.Moves[0]));
    }

    [Fact]
    public void ElectroShotChargesUnlessItRains()
    {
        var dry = Duel(Mon("Archaludon", 50, "Electro Shot"), Mon("Snorlax", 50));
        Assert.Contains("Archaludon absorbed electricity!", Turn(dry, 0, 0));
        Assert.Equal(1, Stage(dry.At(Mine).Pokemon!, StatType.SpAttack));
        Assert.Equal(dry.At(Foe).Pokemon!.MaxHP, dry.At(Foe).Pokemon!.CurrentHP);

        var wet = Duel(Mon("Archaludon", 50, "Electro Shot"), Mon("Snorlax", 50), sky: BattleWeather.Rain);
        Assert.Contains("Archaludon absorbed electricity!", Turn(wet, 0, 0));
        Assert.Equal(1, Stage(wet.At(Mine).Pokemon!, StatType.SpAttack));
        Assert.True(wet.At(Foe).Pokemon!.CurrentHP < wet.At(Foe).Pokemon!.MaxHP);
    }

    // ------------------------------------------------------------------ a price or a crash

    [Fact]
    public void AxeKickCrashesForHalfItsHpAgainstAGhost()
    {
        var core = Duel(Mon("Annihilape", 50, "Axe Kick"), Mon("Gengar", 50));
        var said = Turn(core, 0, 0);
        Assert.Contains("Annihilape kept going and crashed!", said);
        var me = core.At(Mine).Pokemon!;
        Assert.Equal(me.MaxHP - me.MaxHP / 2, me.CurrentHP);
    }

    [Fact]
    public void SupercellSlamCrashesAgainstAGroundType()
    {
        var core = Duel(Mon("Iron Hands", 50, "Supercell Slam"), Mon("Donphan", 50));
        Assert.Contains("Iron Hands kept going and crashed!", Turn(core, 0, 0));
    }

    [Fact]
    public void GlaiveRushLeavesItsUserOpenToTwiceTheHurt()
    {
        var core = Duel(Fast(Mon("Baxcalibur", 50, "Glaive Rush")), Mon("Snorlax", 50, "Tackle"));
        var me = core.At(Mine).Pokemon!;
        Turn(core, 0, 0);
        int rushed = me.MaxHP - me.CurrentHP;

        var plain = Duel(Fast(Mon("Baxcalibur", 50, "Tackle")), Mon("Snorlax", 50, "Tackle"));
        var mine = plain.At(Mine).Pokemon!;
        Turn(plain, 0, 0);
        int calm = mine.MaxHP - mine.CurrentHP;
        Assert.InRange(rushed, calm * 2 - 3, calm * 2 + 3);
    }

    [Fact]
    public void GlaiveRushsPriceIsGoneOnceItsUserMovesAgain()
    {
        var core = Duel(Fast(Mon("Baxcalibur", 50, "Glaive Rush", "Tackle")), Mon("Snorlax", 50, "Tackle"));
        Turn(core, 0, 0);
        Assert.True(core.At(Mine).Volatile.GlaiveRush);
        core.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Fight(Foe, 0));
        Assert.False(core.At(Mine).Volatile.GlaiveRush);
    }

    // ------------------------------------------------------------------ what clears the field

    [Fact]
    public void IceSpinnerEndsTheTerrain()
    {
        var core = Duel(Mon("Baxcalibur", 50, "Ice Spinner"), Mon("Snorlax", 50));
        core.SetTerrain(FieldCondition.GrassyTerrain, null);
        core.TakeLog();
        Turn(core, 0, 0);
        Assert.Null(core.Field.Terrain);
    }

    [Fact]
    public void MortalSpinPoisonsAndBlowsAwayWhatLiesOnItsSide()
    {
        var core = Duel(Mon("Glimmora", 50, "Mortal Spin"), Mon("Chansey", 50));
        core.Field.Side(BattleSide.Player).Spikes = 2;
        core.TakeLog();
        Turn(core, 0, 0);
        Assert.Equal(0, core.Field.Side(BattleSide.Player).Spikes);
        Assert.Equal(StatusCondition.Poison, core.At(Foe).Pokemon!.Status);
    }

    [Fact]
    public void TidyUpClearsBothSidesAndRaisesAttackAndSpeed()
    {
        var core = Duel(Mon("Maushold", 50, "Tidy Up"), Mon("Snorlax", 50));
        core.Field.Side(BattleSide.Player).StealthRock = true;
        core.Field.Side(BattleSide.Enemy).Spikes = 1;
        core.TakeLog();
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var shown = Shown(core);
        Assert.False(core.Field.Side(BattleSide.Player).StealthRock);
        Assert.Equal(0, core.Field.Side(BattleSide.Enemy).Spikes);
        Assert.Contains(shown, e => e is SideConditionChanged { Side: BattleSide.Player, Condition: SideCondition.StealthRock, Layers: 0 });
        Assert.Equal(1, Stage(core.At(Mine).Pokemon!, StatType.Attack));
        Assert.Equal(1, Stage(core.At(Mine).Pokemon!, StatType.Speed));
    }

    // ------------------------------------------------------------------ what lasts on the target

    [Fact]
    public void SaltCureWearsAtItsTargetAtEachTurnsEnd()
    {
        var core = Duel(Mon("Garganacl", 50, "Salt Cure"), Mon("Snorlax", 50));
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog().ToList();
        var said = Lines(log);
        var foe = core.At(Foe);
        Assert.Contains($"{foe.Name} is being salt cured!", said);
        Assert.Contains($"{foe.Name} is hurt by Salt Cure!", said);
        Assert.True(foe.Volatile.SaltCured);
        Assert.Contains(log.OfType<Said>().SelectMany(s => s.Shows), e => e is MarkChanged { Mark: MoveMark.SaltCure, On: true });
        // A turn on: another eighth
        int hp = foe.Pokemon!.CurrentHP;
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        Assert.True(foe.Pokemon!.CurrentHP < hp);
    }

    [Fact]
    public void SaltCureTakesAQuarterFromAWaterType()
    {
        var foe = Mon("Vaporeon", 50);
        var core = Duel(Mon("Garganacl", 50, "Salt Cure", "Splash"), foe);
        Turn(core, 0, 0);
        int hp = foe.CurrentHP;
        Turn(core, 1, 0);
        Assert.Equal(foe.MaxHP / 4, hp - foe.CurrentHP);
    }

    [Fact]
    public void SyrupBombSlowsItsTargetForThreeTurnsEnds()
    {
        var core = Duel(Mon("Dipplin", 50, "Syrup Bomb", "Splash"), Mon("Snorlax", 50));
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog().ToList();
        var foe = core.At(Foe).Pokemon!;
        Assert.Contains($"{core.At(Foe).Name} got covered in sticky candy syrup!", Lines(log));
        Assert.Contains(log.OfType<Said>().SelectMany(s => s.Shows), e => e is MarkChanged { Mark: MoveMark.SyrupBomb, On: true });
        Assert.Equal(-1, Stage(foe, StatType.Speed));
        Turn(core, 1, 0);
        Turn(core, 1, 0);
        Assert.Equal(-3, Stage(foe, StatType.Speed));
        core.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Fight(Foe, 0));
        log = core.TakeLog().ToList();
        Assert.Equal(-3, Stage(foe, StatType.Speed));
        Assert.Contains(log, e => e is MarkChanged { Mark: MoveMark.SyrupBomb, On: false });
        Assert.Null(core.At(Foe).Volatile.SyrupBombBy);
    }

    [Fact]
    public void PsychicNoiseKeepsItsTargetFromHealingForTwoTurns()
    {
        var core = Duel(Mon("Indeedee", 50, "Psychic Noise"), Mon("Snorlax", 50));
        Assert.Contains($"{core.At(Foe).Name} was prevented from healing!", Turn(core, 0, 0));
        Assert.True(core.At(Foe).Volatile.HealBlockTurns > 0);
    }

    [Fact]
    public void AlluringVoiceConfusesATargetWhoseStatsRose()
    {
        var core = Duel(Slow(Mon("Clefable", 50, "Alluring Voice")), Fast(Mon("Snorlax", 50, "Swords Dance")));
        Turn(core, 0, 0);
        Assert.True(core.At(Foe).IsConfused);

        var calm = Duel(Mon("Clefable", 50, "Alluring Voice"), Mon("Snorlax", 50));
        Turn(calm, 0, 0);
        Assert.False(calm.At(Foe).IsConfused);
    }

    // ------------------------------------------------------------------ status moves

    [Fact]
    public void SpicyExtractRaisesAttackAndLowersDefenseByTwo()
    {
        var core = Duel(Mon("Scovillain", 50, "Spicy Extract"), Mon("Snorlax", 50));
        Turn(core, 0, 0);
        var foe = core.At(Foe).Pokemon!;
        Assert.Equal(2, Stage(foe, StatType.Attack));
        Assert.Equal(-2, Stage(foe, StatType.Defense));
    }

    [Fact]
    public void FilletAwayTradesHalfItsHpForSixStages()
    {
        var core = Duel(Mon("Veluza", 50, "Fillet Away"), Mon("Snorlax", 50));
        Turn(core, 0, 0);
        var me = core.At(Mine).Pokemon!;
        Assert.Equal(me.MaxHP - me.MaxHP / 2, me.CurrentHP);
        foreach (var stat in new[] { StatType.Attack, StatType.SpAttack, StatType.Speed }) Assert.Equal(2, Stage(me, stat));
        // With half its HP or less left, it fails
        Assert.Contains("But it failed!", Turn(core, 0, 0));
    }

    [Fact]
    public void DoodleCopiesTheTargetsAbility()
    {
        var foe = With(Mon("Snorlax", 50), ability: "Thick Fat");
        var core = Duel(With(Mon("Smeargle", 50, "Doodle"), ability: "Own Tempo"), foe);
        Turn(core, 0, 0);
        Assert.Equal("Thick Fat", core.At(Mine).Pokemon!.AbilityName);
    }

    [Fact]
    public void DragonCheerPumpsUpTheAllyAndADragonMore()
    {
        var core = Pair(new[] { Mon("Tatsugiri", 50, "Dragon Cheer"), Mon("Garchomp", 50) }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) });
        DoubleTurn(core, (Mine, 0, Mine2), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(2, core.At(Mine2).Volatile.DragonCheer);

        // In a single battle there is nobody to cheer
        var single = Duel(Mon("Tatsugiri", 50, "Dragon Cheer"), Mon("Snorlax", 50));
        Assert.Contains("But there was no target...", Turn(single, 0, 0));
    }

    [Fact]
    public void SnowscapeBringsSnow()
    {
        var core = Duel(Mon("Cetitan", 50, "Snowscape"), Mon("Snorlax", 50));
        Turn(core, 0, 0);
        Assert.Equal(BattleWeather.Snow, core.Field.Weather);
    }

    [Fact]
    public void ChillyReceptionBringsSnowAndSendsItsUserBack()
    {
        var core = Duel(Mon("Slowking", 50, "Chilly Reception"), Mon("Snorlax", 50), bench: Mon("Snorlax", 50));
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleWeather.Snow, core.Field.Weather);
        Assert.IsType<ReplacementRequest>(core.Request);
    }

    [Fact]
    public void ShedTailHandsItsSubstituteToTheNextAndNothingElse()
    {
        var core = Duel(Mon("Cyclizar", 50, "Shed Tail", "Swords Dance"), Mon("Snorlax", 50), bench: Mon("Snorlax", 50));
        var cyclizar = core.At(Mine).Pokemon!;
        Turn(core, 1, 0);
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        Assert.Equal(cyclizar.MaxHP - (cyclizar.MaxHP + 1) / 2, cyclizar.CurrentHP);
        SendIn(core, 1);
        var newcomer = core.At(Mine);
        Assert.True(newcomer.HasSubstitute);
        Assert.Equal(cyclizar.MaxHP / 4, newcomer.Volatile.SubstituteHp);
        Assert.Equal(0, Stage(newcomer.Pokemon!, StatType.Attack));
    }

    [Fact]
    public void ShedTailFailsWithNobodyToComeIn() =>
        Assert.Contains("But it failed!", Turn(Duel(Mon("Cyclizar", 50, "Shed Tail"), Mon("Snorlax", 50)), 0, 0));

    [Fact]
    public void RevivalBlessingBringsAFaintedPokemonBackWithHalfItsHp()
    {
        var down = Mon("Snorlax", 50);
        down.CurrentHP = 0;
        down.Status = StatusCondition.Faint;
        var core = Duel(Mon("Pawmot", 50, "Revival Blessing"), Mon("Snorlax", 50), bench: down);
        var said = Turn(core, 0, 0);
        Assert.Contains("Snorlax was revived and is ready to fight again!", said);
        Assert.Equal(down.MaxHP / 2, down.CurrentHP);
        Assert.Equal(StatusCondition.None, down.Status);
    }

    // ------------------------------------------------------------------ the marks on screen (style guide, "A condition that lasts")

    /// <summary>The looks of a battle followed by its events alone, as the screen follows them, held to the rules after every turn.</summary>
    private static ConditionLooks Follow(BattleCore core, ConditionLooks looks, ref int read)
    {
        for (; read < core.Log.Count; read++)
        {
            looks.Tell(core.Log[read]);
            if (core.Log[read] is Said said)
                foreach (var e in said.Shows.Concat(said.OnImpact)) looks.Tell(e);
        }
        Assert.Null(looks.Difference(core));
        return looks;
    }

    [Fact]
    public void TheSaltAndTheSyrupShowWhileTheyLastByTheEventsAlone()
    {
        var core = Duel(Mon("Garganacl", 50, "Salt Cure", "Syrup Bomb", "Splash"), Mon("Snorlax", 100));
        var looks = new ConditionLooks();
        int read = 0;
        Follow(core, looks, ref read);
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        Assert.True(Follow(core, looks, ref read).Of(Foe).Salt.Up);
        core.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Fight(Foe, 0));
        Assert.True(Follow(core, looks, ref read).Of(Foe).Syrup.Up);
        // The syrup's three turns' ends, then it goes with no line
        for (int i = 0; i < 3; i++)
        {
            core.Submit(BattleChoice.Fight(Mine, 2), BattleChoice.Fight(Foe, 0));
            Follow(core, looks, ref read);
        }
        Assert.False(looks.Of(Foe).Syrup.Up);
        Assert.True(looks.Of(Foe).Salt.Up);
    }

    [Fact]
    public void TheSaltAndTheSyrupKeepToTheirCapsAndArePure()
    {
        var feet = new System.Numerics.Vector3(2f, 1f, -3f);
        for (int i = 0; i <= 300; i++)
        {
            float age = i / 30f;
            var salt = new FxList();
            MoveFx.SaltGrains(age, 1f, 5591, feet, 2f, 0.8f, salt);
            Assert.InRange(salt.Count, 1, MoveFx.SaltCap);
            var syrup = new FxList();
            MoveFx.SyrupDrops(age, 1f, 5591, feet, 2f, 0.8f, syrup);
            Assert.InRange(syrup.Count, 1, MoveFx.SyrupCap);
            var again = new FxList();
            MoveFx.SyrupDrops(age, 1f, 5591, feet, 2f, 0.8f, again);
            Assert.Equal(syrup.Quads.Select(q => q.Center), again.Quads.Select(q => q.Center));
        }
        // Nothing at all at no strength
        var none = new FxList();
        MoveFx.SaltGrains(1f, 0f, 5591, feet, 2f, 0.8f, none);
        MoveFx.SyrupDrops(1f, 0f, 5591, feet, 2f, 0.8f, none);
        Assert.Equal(0, none.Count);
    }
}
