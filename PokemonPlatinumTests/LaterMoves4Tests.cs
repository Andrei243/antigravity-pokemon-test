using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The later generations' moves, batch 3, its first half (plan 06 · R26a): Generation 8's, Sword and Shield's and
/// Legends: Arceus's, each by Pokémon Showdown's rule (data/moves.ts at the pinned commit). One rule to a test, under
/// the modern rules, the rolls fixed by kind (<see cref="CoreScenario.Calm"/>: no critical hit, the strongest roll,
/// nothing misses, no side effect).
/// </summary>
public class LaterMoves4Tests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static BattleCore Duel(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, BattleWeather sky = BattleWeather.None) =>
        Wild(mine, foe, rolls, Modern, sky);

    private static BattleCore Pair(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null) => Doubles(mine, theirs, rolls, Modern);

    /// <summary>What the foe lost to the player's move in one turn, the foe idling; the core made ready first by <paramref name="before"/>.</summary>
    private static int Took(Pokemon mine, Pokemon foe, Action<BattleCore>? before = null, BattleRandom? rolls = null)
    {
        var core = Duel(mine, Idling(foe), rolls);
        before?.Invoke(core);
        int hp = foe.CurrentHP;
        Turn(core, 0, foe.Moves.Count - 1);
        return hp - foe.CurrentHP;
    }

    private static List<BattleEvent> Shown(BattleCore core)
    {
        var log = core.TakeLog().ToList();
        return log.OfType<Said>().SelectMany(s => s.Shows.Concat(s.OnImpact)).Concat(log).ToList();
    }

    private static Pokemon Holding(Pokemon p, string item)
    {
        p.HeldItem = ItemDatabase.Get(item) ?? throw new ArgumentException(item);
        return p;
    }

    // ------------------------------------------------------------------ Sword and Shield: the aim and the turn's order

    [Fact]
    public void SnipeShotIsntDrawnAwayByFollowMe()
    {
        // Clefable's Follow Me (+2) would draw every move aimed at one foe; Snipe Shot goes where it was aimed
        var core = Pair(new[] { Mon("Golduck", 50, "Snipe Shot"), Idling(Mon("Snorlax", 50)) },
            new[] { Mon("Clefable", 50, "Follow Me"), Idling(Mon("Snorlax", 50)) });
        DoubleTurn(core, (Mine, 0, Foe2), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(core.At(Foe).Pokemon!.MaxHP, core.At(Foe).Pokemon!.CurrentHP);
        Assert.True(core.At(Foe2).Pokemon!.CurrentHP < core.At(Foe2).Pokemon!.MaxHP);
    }

    [Fact]
    public void GrassyGlideGoesFirstOnGrassyTerrain()
    {
        // A slow Abomasnow's Grassy Glide against a fast Jolteon's Tackle: on Grassy Terrain it is a step quicker
        var core = Duel(Mon("Abomasnow", 50, "Grassy Glide"), Mon("Jolteon", 50, "Tackle"));
        core.SetTerrain(FieldCondition.GrassyTerrain, null);
        core.TakeLog();
        var said = Turn(core, 0, 0);
        InOrder(said, "Abomasnow used Grassy Glide!", $"{core.At(Foe).Name} used Tackle!");

        var plain = Duel(Mon("Abomasnow", 50, "Grassy Glide"), Mon("Jolteon", 50, "Tackle"));
        InOrder(Turn(plain, 0, 0), $"{plain.At(Foe).Name} used Tackle!", "Abomasnow used Grassy Glide!");
    }

    [Fact]
    public void BoltBeakIsTwiceAsStrongAgainstATargetStillToMove()
    {
        // The same Dracozolt against the same Snorlax: moving first, the power is 170 rather than 85
        var fast = Mon("Dracozolt", 50, "Bolt Beak");
        fast.Speed = 999;
        var slow = Mon("Dracozolt", 50, "Bolt Beak");
        slow.Speed = 1;
        int first = Took(fast, Mon("Snorlax", 50));
        int second = Took(slow, Mon("Snorlax", 50));
        Assert.InRange(first, second * 2 - 4, second * 2 + 4);
    }

    [Fact]
    public void DragonDartsHitsTwiceInASingleBattle() =>
        Assert.Contains("Hit 2 times!", Turn(Duel(Mon("Dragapult", 50, "Dragon Darts"), Idling(Mon("Snorlax", 50))), 0, 0));

    [Fact]
    public void DragonDartsSendsADartAtEachFoeInADoubleBattle()
    {
        var core = Pair(new[] { Mon("Dragapult", 50, "Dragon Darts"), Idling(Mon("Snorlax", 50)) },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        int one = core.At(Foe).Pokemon!.MaxHP - core.At(Foe).Pokemon!.CurrentHP;
        int other = core.At(Foe2).Pokemon!.MaxHP - core.At(Foe2).Pokemon!.CurrentHP;
        // One dart each, at full strength (no spread's quarter off): the same damage on the two Snorlax
        Assert.True(one > 0);
        Assert.Equal(one, other);
    }

    [Fact]
    public void ExpandingForceHitsEveryFoeOnPsychicTerrain()
    {
        var core = Pair(new[] { Mon("Alakazam", 50, "Expanding Force"), Idling(Mon("Snorlax", 50)) },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        core.SetTerrain(FieldCondition.PsychicTerrain, null);
        DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < core.At(Foe).Pokemon!.MaxHP);
        Assert.True(core.At(Foe2).Pokemon!.CurrentHP < core.At(Foe2).Pokemon!.MaxHP);
    }

    // ------------------------------------------------------------------ what holds a Pokémon in

    [Fact]
    public void JawLockHoldsBothItsUserAndItsTarget()
    {
        var core = Duel(Mon("Drednaw", 50, "Jaw Lock"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("Neither Pokémon can run away!", Turn(core, 0, 0));
        Assert.False(core.CanSwitchOut(core.At(Mine)));
        Assert.False(core.CanSwitchOut(core.At(Foe)));
    }

    [Fact]
    public void NoRetreatRaisesEveryStatAndHoldsItsUserOnce()
    {
        var core = Duel(Mon("Falinks", 50, "No Retreat"), Idling(Mon("Snorlax", 50)));
        var said = Turn(core, 0, 0);
        Assert.Contains("Falinks can no longer escape because it used No Retreat!", said);
        var p = core.At(Mine).Pokemon!;
        foreach (var stat in new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed })
            Assert.Equal(1, p.StatStages.GetValueOrDefault(stat));
        Assert.False(core.CanSwitchOut(core.At(Mine)));
        // Used again while it holds, it fails
        Assert.Contains("But it failed!", Turn(core, 0, 0));
    }

    [Fact]
    public void OctolockHoldsItsTargetAndSqueezesItsGuardsAtEachTurnsEnd()
    {
        var core = Duel(Mon("Grapploct", 50, "Octolock"), Idling(Mon("Snorlax", 50)));
        var foe = core.At(Foe);
        Assert.Contains($"{foe.Name} can no longer escape because of Octolock!", Turn(core, 0, 0));
        Assert.False(core.CanSwitchOut(foe));
        // The turn's end: a stage of Defense and one of Sp. Def
        Assert.Equal(-1, foe.Pokemon!.StatStages.GetValueOrDefault(StatType.Defense));
        Assert.Equal(-1, foe.Pokemon.StatStages.GetValueOrDefault(StatType.SpDefense));
    }

    [Fact]
    public void AGhostTypeIsImmuneToOctolock()
    {
        var core = Duel(Mon("Grapploct", 50, "Octolock"), Idling(Mon("Gengar", 50)));
        Assert.Contains($"It doesn't affect {core.At(Foe).Name}...", Turn(core, 0, 0));
        Assert.Null(core.At(Foe).Volatile.OctolockedBy);
    }

    // ------------------------------------------------------------------ berries

    [Fact]
    public void StuffCheeksRaisesDefenseByTwoAndEatsTheBerry()
    {
        var core = Duel(Holding(Mon("Greedent", 50, "Stuff Cheeks"), "Oran Berry"), Idling(Mon("Snorlax", 50)));
        var said = Turn(core, 0, 0);
        Assert.Contains("Greedent ate its Oran Berry!", said);
        Assert.Equal(2, core.At(Mine).Pokemon!.StatStages.GetValueOrDefault(StatType.Defense));
        Assert.Null(core.At(Mine).Pokemon!.HeldItem);
        // Without a berry it fails
        Assert.Contains("But it failed!", Turn(core, 0, 0));
    }

    [Fact]
    public void TeatimeHasEveryoneEatTheirBerries()
    {
        var mine = Holding(Mon("Snorlax", 50, "Teatime"), "Sitrus Berry");
        var foe = Holding(Idling(Mon("Snorlax", 50)), "Sitrus Berry");
        var core = Duel(mine, foe);
        var said = Turn(core, 0, 0);
        Assert.Contains("It's teatime! Everyone dug in to their Berries!", said);
        Assert.Null(mine.HeldItem);
        Assert.Null(foe.HeldItem);
        Assert.Contains("But it failed!", Turn(core, 0, 0));
    }

    // ------------------------------------------------------------------ moves on another Pokémon

    [Fact]
    public void TarShotSlowsItsTargetAndMakesFireMovesTwiceAsStrong()
    {
        var core = Duel(Mon("Coalossal", 50, "Tar Shot"), Idling(Mon("Snorlax", 50)));
        var flamethrower = new Move(MoveDatabase.Get("Flamethrower"));
        Assert.Equal(1f, DamageCalculator.Effectiveness(core.At(Mine), core.At(Foe), flamethrower, Modern));
        Assert.Contains($"{core.At(Foe).Name} became weaker to fire!", Turn(core, 0, 1));
        Assert.Equal(-1, core.At(Foe).Pokemon!.StatStages.GetValueOrDefault(StatType.Speed));
        Assert.Equal(2f, DamageCalculator.Effectiveness(core.At(Mine), core.At(Foe), flamethrower, Modern));
    }

    [Fact]
    public void MagicPowderMakesItsTargetAPsychicType()
    {
        var core = Duel(Mon("Hatterene", 50, "Magic Powder"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.Equal(new[] { PokemonType.Psychic }, core.At(Foe).Types);
        // A powder: nothing to a Grass type
        var grass = Duel(Mon("Hatterene", 50, "Magic Powder"), Idling(Mon("Venusaur", 50)));
        Assert.Contains($"It doesn't affect {grass.At(Foe).Name}...", Turn(grass, 0, 0));
    }

    [Fact]
    public void PoltergeistFailsAgainstATargetThatHoldsNothing()
    {
        var empty = Duel(Mon("Gengar", 50, "Poltergeist"), Idling(Mon("Machamp", 50)));
        Assert.Contains("But it failed!", Turn(empty, 0, 0));
        var holding = Duel(Mon("Gengar", 50, "Poltergeist"), Holding(Idling(Mon("Machamp", 50)), "Leftovers"));
        Assert.Contains($"{holding.At(Foe).Name} is about to be attacked by its Leftovers!", Turn(holding, 0, 1));
    }

    [Fact]
    public void EerieSpellTakesThreePpFromTheTargetsLastMove()
    {
        // Snorlax is faster than nobody here: Alakazam's Eerie Spell comes after Snorlax's Tackle
        var core = Duel(Mon("Alakazam", 50, "Eerie Spell"), Mon("Snorlax", 50, "Tackle"));
        core.At(Mine).Pokemon!.Speed = 1;
        var tackle = core.At(Foe).Pokemon!.Moves[0];
        Turn(core, 0, 0);
        // One for its own use, three to the spell
        Assert.Equal(tackle.Data.MaxPP - 4, tackle.CurrentPP);
    }

    [Fact]
    public void BurningJealousyBurnsATargetWhoseStatsRoseThisTurn()
    {
        // Swords Dance first, then the jealous fire; a target that only idled is left alone
        var core = Duel(Mon("Typhlosion", 50, "Burning Jealousy"), Mon("Snorlax", 50, "Swords Dance", "Splash"));
        core.At(Mine).Pokemon!.Speed = 1;
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.Burn, core.At(Foe).Pokemon!.Status);

        var calm = Duel(Mon("Typhlosion", 50, "Burning Jealousy"), Idling(Mon("Snorlax", 50)));
        Turn(calm, 0, 1);
        Assert.Equal(StatusCondition.None, calm.At(Foe).Pokemon!.Status);
    }

    [Fact]
    public void LashOutIsTwiceAsStrongAfterItsUsersStatsFell()
    {
        // Snorlax's Growl comes first and takes a stage of Attack: the power is 150, the Attack at −1 two thirds
        var lowered = Duel(Mon("Grimmsnarl", 50, "Lash Out"), Mon("Snorlax", 50, "Growl"));
        lowered.At(Mine).Pokemon!.Speed = 1;
        int before = lowered.At(Foe).Pokemon!.CurrentHP;
        Turn(lowered, 0, 0);
        int growled = before - lowered.At(Foe).Pokemon!.CurrentHP;
        var plain = Duel(Mon("Grimmsnarl", 50, "Lash Out"), Idling(Mon("Snorlax", 50)));
        int hp = plain.At(Foe).Pokemon!.CurrentHP;
        Turn(plain, 0, 0);
        int calm = hp - plain.At(Foe).Pokemon!.CurrentHP;
        // ×2 for the power and ×2/3 for the stage: about four thirds of the plain hit
        Assert.InRange(growled * 3, calm * 4 - 12, calm * 4 + 12);
    }

    [Fact]
    public void DireClawPoisonsParalysesOrPutsToSleepOneTimeInTwo()
    {
        // The chance's roll is 0 (under 50), and the draw of the three is 0: poison
        // (Machamp: a Snorlax's Immunity would keep the poison off)
        var core = Duel(Mon("Sneasler", 50, "Dire Claw"), Idling(Mon("Machamp", 50)), Steady().Force(RollKind.SideEffect, 0));
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.Poison, core.At(Foe).Pokemon!.Status);
        var spared = Duel(Mon("Sneasler", 50, "Dire Claw"), Idling(Mon("Machamp", 50)));
        Turn(spared, 0, 0);
        Assert.Equal(StatusCondition.None, spared.At(Foe).Pokemon!.Status);
    }

    // ------------------------------------------------------------------ power by what holds

    [Fact]
    public void BodyPressHitsWithItsUsersDefenseAndItsStage()
    {
        // Steelix (Defense 205, Attack 90) against Blissey (Defense 15), level 50, power 80, Fighting on Normal ×2:
        // floor(floor(22 × 80 × 205 / 15) / 50) + 2 = floor(24053 / 50) + 2 = 483; ×2 = 966. By its Attack it
        // would be floor(10560 / 50) + 2 = 213, and 426. At +2 Defense (410): floor(48106 / 50) + 2 = 964; ×2 = 1928.
        var steelix = Mon("Steelix", 50, "Body Press");
        var blissey = Mon("Blissey", 50);
        Assert.Equal(966, Damage(steelix, blissey, "Body Press", Steady(), Modern));
        steelix.StatStages[StatType.Defense] = 2;
        Assert.Equal(1928, Damage(steelix, blissey, "Body Press", Steady(), Modern));
    }

    [Fact]
    public void SurgingStrikesHitsThreeTimesAndEveryHitIsCritical()
    {
        var said = Turn(Duel(Mon("Golduck", 50, "Surging Strikes"), Idling(Mon("Snorlax", 50))), 0, 0);
        Assert.Contains("Hit 3 times!", said);
        Assert.Contains("A critical hit!", said);
    }

    [Fact]
    public void GravAppleIsHalfAsStrongAgainUnderGravity()
    {
        int plain = Took(Mon("Flapple", 50, "Grav Apple"), Mon("Snorlax", 50));
        int heavy = Took(Mon("Flapple", 50, "Grav Apple"), Mon("Snorlax", 50), core => core.Field.GravityTurns = 5);
        Assert.InRange(heavy * 2, plain * 3 - 6, plain * 3 + 6);
    }

    [Fact]
    public void RisingVoltageIsTwiceAsStrongAgainstATargetOnElectricTerrain()
    {
        int plain = Took(Mon("Toxtricity", 50, "Rising Voltage"), Mon("Snorlax", 50));
        int charged = Took(Mon("Toxtricity", 50, "Rising Voltage"), Mon("Snorlax", 50), core => core.SetTerrain(FieldCondition.ElectricTerrain, null));
        // ×2 for the power and 5325/4096 for the terrain's own bonus to an Electric move
        Assert.True(charged > plain * 2);
        // Not against one in the air
        int flying = Took(Mon("Toxtricity", 50, "Rising Voltage"), Mon("Togekiss", 50), core => core.SetTerrain(FieldCondition.ElectricTerrain, null));
        int flyingPlain = Took(Mon("Toxtricity", 50, "Rising Voltage"), Mon("Togekiss", 50));
        Assert.True(flying < flyingPlain * 2);
    }

    [Fact]
    public void TerrainPulseTakesTheTerrainsType()
    {
        // On Electric Terrain it is an Electric move, which a Ground type doesn't feel
        var core = Duel(Mon("Indeedee", 50, "Terrain Pulse"), Idling(Mon("Golem", 50)));
        core.SetTerrain(FieldCondition.ElectricTerrain, null);
        core.TakeLog();
        Assert.Contains($"It doesn't affect {core.At(Foe).Name}...", Turn(core, 0, 1));
    }

    [Fact]
    public void BarbBarrageIsTwiceAsStrongAgainstAPoisonedTarget()
    {
        // Magic Guard keeps the poison's own bite out of the count
        int plain = Took(Mon("Qwilfish", 50, "Barb Barrage"), With(Mon("Machamp", 50), ability: "Magic Guard"));
        var poisoned = With(Mon("Machamp", 50), ability: "Magic Guard");
        poisoned.Status = StatusCondition.Poison;
        int doubled = Took(Mon("Qwilfish", 50, "Barb Barrage"), poisoned);
        Assert.InRange(doubled, plain * 2 - 2, plain * 2 + 2);
    }

    [Fact]
    public void InfernalParadeIsTwiceAsStrongAgainstATargetWithACondition()
    {
        int plain = Took(Mon("Typhlosion", 50, "Infernal Parade"), Mon("Machamp", 50));
        var paralysed = Mon("Machamp", 50);
        paralysed.Status = StatusCondition.Paralyze;
        int doubled = Took(Mon("Typhlosion", 50, "Infernal Parade"), paralysed);
        Assert.InRange(doubled, plain * 2 - 2, plain * 2 + 2);
    }

    [Fact]
    public void MistyExplosionCostsItsUserEverythingAndIsStrongerOnMistyTerrain()
    {
        // Clefable, on the ground (Weezing floats by Levitate, and the terrain would do nothing for it)
        var core = Duel(Mon("Clefable", 50, "Misty Explosion"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.Equal(0, core.At(Mine).Pokemon!.CurrentHP);

        int plain = Took(Mon("Clefable", 50, "Misty Explosion"), Mon("Snorlax", 50));
        int misty = Took(Mon("Clefable", 50, "Misty Explosion"), Mon("Snorlax", 50), c => c.SetTerrain(FieldCondition.MistyTerrain, null));
        Assert.InRange(misty * 2, plain * 3 - 6, plain * 3 + 6);
    }

    [Fact]
    public void AuraWheelIsMorpekosAloneAndDarkWhenItIsHangry()
    {
        var other = Duel(Mon("Pikachu", 50, "Aura Wheel"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("But it failed!", Turn(other, 0, 0));

        // Hangry, it is Dark: a Psychic type isn't immune and a Dark type resists it
        var morpeko = Mon("Morpeko", 50, "Aura Wheel");
        morpeko.ChangeForm("Morpeko-Hangry");
        var core = Duel(morpeko, Idling(Mon("Golem", 50)));
        Turn(core, 0, 1);
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < core.At(Foe).Pokemon!.MaxHP);
        Assert.Equal(1, morpeko.StatStages.GetValueOrDefault(StatType.Speed));
    }

    [Fact]
    public void ShellSideArmIsPhysicalAndTouchesWhenThatWouldHurtMore()
    {
        // Machamp's Attack against Garchomp's Defense beats its Sp. Atk against Sp. Def: physical, and Rough Skin answers
        var machamp = Mon("Machamp", 50, "Shell Side Arm");
        Turn(Duel(machamp, Idling(With(Mon("Garchomp", 50), ability: "Rough Skin"))), 0, 1);
        Assert.True(machamp.CurrentHP < machamp.MaxHP);
        // Alakazam's is special: no touch, no Rough Skin
        var alakazam = Mon("Alakazam", 50, "Shell Side Arm");
        Turn(Duel(alakazam, Idling(With(Mon("Garchomp", 50), ability: "Rough Skin"))), 0, 1);
        Assert.Equal(alakazam.MaxHP, alakazam.CurrentHP);
    }

    // ------------------------------------------------------------------ moves on its own side

    [Fact]
    public void ClangorousSoulRaisesEveryStatForAThirdOfItsHp()
    {
        // Kommo-o's 135 HP at level 50: 135 × 33 / 100 = 44 lost
        var core = Duel(Mon("Kommo-o", 50, "Clangorous Soul"), Idling(Mon("Snorlax", 50)));
        var p = core.At(Mine).Pokemon!;
        Turn(core, 0, 0);
        Assert.Equal(p.MaxHP - p.MaxHP * 33 / 100, p.CurrentHP);
        Assert.Equal(1, p.StatStages.GetValueOrDefault(StatType.SpDefense));
        // With a third of its HP or less left it fails
        p.CurrentHP = p.MaxHP * 33 / 100;
        Assert.Contains("But it failed!", Turn(core, 0, 0));
    }

    [Fact]
    public void ObstructStopsAHitAndTakesTwoStagesOfDefenseFromWhoeverTouchedIt()
    {
        var core = Duel(Mon("Obstagoon", 50, "Obstruct"), Mon("Snorlax", 50, "Tackle"));
        Turn(core, 0, 0);
        Assert.Equal(core.At(Mine).Pokemon!.MaxHP, core.At(Mine).Pokemon!.CurrentHP);
        Assert.Equal(-2, core.At(Foe).Pokemon!.StatStages.GetValueOrDefault(StatType.Defense));
        // A status move goes through, as through King's Shield
        var growl = Duel(Mon("Obstagoon", 50, "Obstruct"), Mon("Snorlax", 50, "Growl"));
        Turn(growl, 0, 0);
        Assert.Equal(-1, growl.At(Mine).Pokemon!.StatStages.GetValueOrDefault(StatType.Attack));
    }

    [Fact]
    public void LifeDewGivesAQuarterOfTheirHpRounded()
    {
        // Lapras's 190 HP: a quarter is 47.5, rounded to 48
        var core = Duel(Mon("Lapras", 50, "Life Dew"), Idling(Mon("Snorlax", 50)));
        var p = core.At(Mine).Pokemon!;
        p.CurrentHP = 100;
        Turn(core, 0, 0);
        Assert.Equal(148, p.CurrentHP);
    }

    [Fact]
    public void JungleHealingRestoresAQuarterAndCuresTheCondition()
    {
        // Rillaboom's 160 HP: this.modify(160, 0.25) = (160 × 1024 + 2047) / 4096 = 40
        var core = Duel(Mon("Rillaboom", 50, "Jungle Healing"), Idling(Mon("Snorlax", 50)));
        var p = core.At(Mine).Pokemon!;
        p.CurrentHP = 100;
        p.Status = StatusCondition.Burn;
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, p.Status);
        // Burn's sixteenth is gone with the burn: what is left is the quarter
        Assert.Equal(100 + (p.MaxHP * 1024 + 2047) / 4096, p.CurrentHP);
    }

    [Fact]
    public void TakeHeartRaisesItsSpecialStatsAndCuresItsCondition()
    {
        var core = Duel(Mon("Alakazam", 50, "Take Heart"), Idling(Mon("Snorlax", 50)));
        var p = core.At(Mine).Pokemon!;
        p.Status = StatusCondition.Burn;
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, p.Status);
        Assert.Equal(1, p.StatStages.GetValueOrDefault(StatType.SpAttack));
        Assert.Equal(1, p.StatStages.GetValueOrDefault(StatType.SpDefense));
    }

    [Fact]
    public void CoachingRaisesTheAllysAttackAndDefense()
    {
        var core = Pair(new[] { Mon("Machamp", 50, "Coaching"), Idling(Mon("Snorlax", 50)) },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        var ally = core.At(Mine2).Pokemon!;
        Assert.Equal(1, ally.StatStages.GetValueOrDefault(StatType.Attack));
        Assert.Equal(1, ally.StatStages.GetValueOrDefault(StatType.Defense));
        Assert.Equal(0, core.At(Mine).Pokemon!.StatStages.GetValueOrDefault(StatType.Attack));
    }

    [Fact]
    public void ScaleShotRaisesItsUsersSpeedAndLowersItsDefenseAfterItsHits()
    {
        var core = Duel(Mon("Dragapult", 50, "Scale Shot"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        var p = core.At(Mine).Pokemon!;
        Assert.Equal(1, p.StatStages.GetValueOrDefault(StatType.Speed));
        Assert.Equal(-1, p.StatStages.GetValueOrDefault(StatType.Defense));
    }

    [Fact]
    public void MeteorBeamRaisesSpAtkAsItCharges()
    {
        var core = Duel(Mon("Golem", 50, "Meteor Beam"), Idling(Mon("Snorlax", 50)));
        var said = Turn(core, 0, 0);
        Assert.Contains("Golem is overflowing with space power!", said);
        Assert.Equal(1, core.At(Mine).Pokemon!.StatStages.GetValueOrDefault(StatType.SpAttack));
        Assert.Equal(core.At(Foe).Pokemon!.MaxHP, core.At(Foe).Pokemon!.CurrentHP);
        Turn(core, 0, 0);
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < core.At(Foe).Pokemon!.MaxHP);
    }

    // ------------------------------------------------------------------ the field

    [Fact]
    public void SteelRollerFailsWithoutATerrainAndEndsOne()
    {
        var bare = Duel(Mon("Copperajah", 50, "Steel Roller"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("But it failed!", Turn(bare, 0, 0));

        var core = Duel(Mon("Copperajah", 50, "Steel Roller"), Idling(Mon("Snorlax", 50)));
        core.SetTerrain(FieldCondition.GrassyTerrain, null);
        core.TakeLog();
        Turn(core, 0, 1);
        Assert.Null(core.Field.Terrain);
    }

    [Fact]
    public void CourtChangeSwapsWhatLiesOnEachSide()
    {
        var core = Duel(Mon("Cinderace", 50, "Court Change"), Idling(Mon("Snorlax", 50)));
        core.Field.Side(BattleSide.Player).StealthRock = true;
        core.Field.Side(BattleSide.Enemy).ReflectTurns = 3;
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 1));
        var shown = Shown(core);
        Assert.False(core.Field.Side(BattleSide.Player).StealthRock);
        Assert.True(core.Field.Side(BattleSide.Enemy).StealthRock);
        Assert.True(core.Field.Side(BattleSide.Player).Reflect);
        Assert.False(core.Field.Side(BattleSide.Enemy).Reflect);
        // The line carries each change, for the field's look
        Assert.Contains(shown, e => e is SideConditionChanged { Side: BattleSide.Enemy, Condition: SideCondition.StealthRock, Layers: 1 });
        Assert.Contains(shown, e => e is SideConditionChanged { Side: BattleSide.Player, Condition: SideCondition.StealthRock, Layers: 0 });
        // With nothing on either side it fails
        var empty = Duel(Mon("Cinderace", 50, "Court Change"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("But it failed!", Turn(empty, 0, 1));
    }

    [Fact]
    public void StoneAxeLeavesStealthRockBehind()
    {
        var core = Duel(Mon("Kleavor", 50, "Stone Axe"), Idling(Mon("Snorlax", 50)));
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        Assert.Contains(Shown(core), e => e is SideConditionChanged { Side: BattleSide.Enemy, Condition: SideCondition.StealthRock, Layers: 1 });
        Assert.True(core.Field.Side(BattleSide.Enemy).StealthRock);
    }

    [Fact]
    public void CeaselessEdgeLaysALayerOfSpikes()
    {
        var core = Duel(Mon("Samurott", 50, "Ceaseless Edge"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Turn(core, 0, 0);
        Assert.Equal(2, core.Field.Side(BattleSide.Enemy).Spikes);
    }

    // ------------------------------------------------------------------ Legends: Arceus

    [Fact]
    public void ChloroblastCostsItsUserHalfItsHp()
    {
        // Venusaur's 140 HP: Math.round(140 / 2) = 70
        var core = Duel(Mon("Venusaur", 50, "Chloroblast"), Idling(Mon("Snorlax", 50)));
        var p = core.At(Mine).Pokemon!;
        Turn(core, 0, 0);
        Assert.Equal(p.MaxHP - (p.MaxHP + 1) / 2, p.CurrentHP);
    }

    [Fact]
    public void BleakwindStormIsSureToHitInTheRain()
    {
        // The accuracy roll is 99, which an 80 misses; under rain it isn't rolled
        var dry = Duel(Mon("Tornadus", 50, "Bleakwind Storm"), Idling(Mon("Snorlax", 50)), Calm().Force(RollKind.Accuracy, 99));
        Turn(dry, 0, 0);
        Assert.Equal(dry.At(Foe).Pokemon!.MaxHP, dry.At(Foe).Pokemon!.CurrentHP);
        var wet = Duel(Mon("Tornadus", 50, "Bleakwind Storm"), Idling(Mon("Snorlax", 50)), Calm().Force(RollKind.Accuracy, 99), BattleWeather.Rain);
        Turn(wet, 0, 0);
        Assert.True(wet.At(Foe).Pokemon!.CurrentHP < wet.At(Foe).Pokemon!.MaxHP);
    }

    [Fact]
    public void RagingFuryHoldsItsUserToItAsOutrageDoes()
    {
        var core = Duel(Mon("Arcanine", 50, "Raging Fury"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.True(core.At(Mine).Volatile.RampageTurns > 0);
        Assert.DoesNotContain(Mine, Assert.IsType<ActionRequest>(core.Request).Places);
    }

    [Fact]
    public void PowerShiftSwapsAttackAndDefense()
    {
        var core = Duel(Mon("Shuckle", 50, "Power Shift"), Idling(Mon("Snorlax", 50)));
        var p = core.At(Mine).Pokemon!;
        (int attack, int defense) = (p.Attack, p.Defense);
        Turn(core, 0, 0);
        Assert.Equal((defense, attack), (p.Attack, p.Defense));
    }

    [Fact]
    public void VictoryDanceRaisesAttackDefenseAndSpeed()
    {
        var core = Duel(Mon("Decidueye", 50, "Victory Dance"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        var p = core.At(Mine).Pokemon!;
        Assert.Equal((1, 1, 1), (p.StatStages.GetValueOrDefault(StatType.Attack), p.StatStages.GetValueOrDefault(StatType.Defense), p.StatStages.GetValueOrDefault(StatType.Speed)));
    }

    [Fact]
    public void WaveCrashCostsItsUserAThirdOfWhatItDealt()
    {
        var core = Duel(Mon("Basculegion", 50, "Wave Crash"), Idling(Mon("Snorlax", 50)));
        var mine = core.At(Mine).Pokemon!;
        var foe = core.At(Foe).Pokemon!;
        Turn(core, 0, 0);
        int dealt = foe.MaxHP - foe.CurrentHP;
        Assert.Equal(mine.MaxHP - Math.Max(1, dealt / 3), mine.CurrentHP);
    }

    [Fact]
    public void LegendsArceusMovesTakeTheirFlagsAndSideEffectsFromShowdown()
    {
        // PokeAPI has nothing of these but their numbers; the importer reads Showdown's table for the rest
        Assert.Equal(MoveFlags.Contact | MoveFlags.Protect | MoveFlags.Mirror, MoveDatabase.Get("Wave Crash").Flags);
        Assert.Equal(33, MoveDatabase.Get("Wave Crash").RecoilPercent);
        var arrows = MoveDatabase.Get("Triple Arrows");
        Assert.Equal((1, 30, StatType.Defense, 50), (arrows.CritStage, arrows.FlinchChancePercent, arrows.TargetStatChange!.Value, arrows.StatChangeChancePercent));
        var rush = MoveDatabase.Get("Headlong Rush");
        Assert.True(rush.StatChangeTargetSelf);
        Assert.Equal(new[] { StatType.SpDefense }, rush.AlsoChangesStats);
        Assert.Equal(MoveEffectSupport.Full, MoveDatabase.Get("Esper Wing").Support);
    }
}
