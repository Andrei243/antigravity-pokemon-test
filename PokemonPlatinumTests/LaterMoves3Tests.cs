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
/// The later generations' moves, R25's leftovers (plan 06): Sky Drop, Ally Switch, the three Pledges and Sappy Seed,
/// Defog's and Feint's later reach, and the shortcuts R25 left (Round, Instruct, Water Shuriken, Gravity against
/// Telekinesis), each by Pokémon Showdown's rule (data/moves.ts at the pinned commit). One rule to a test, under the
/// modern rules, the rolls fixed by kind (<see cref="CoreScenario.Calm"/>).
/// </summary>
public class LaterMoves3Tests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static BattleCore Duel(Pokemon mine, Pokemon foe, BattleRandom? rolls = null) => Wild(mine, foe, rolls, Modern);

    private static BattleCore Pair(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null) => Doubles(mine, theirs, rolls, Modern);

    /// <summary>A turn of a double battle with every event the lines carried, and what was said.</summary>
    private static (List<string> Said, List<BattleEvent> Shown) DoubleTurnWithEvents(BattleCore core, params (Place Who, int Move, Place? At)[] actions)
    {
        var asked = Assert.IsType<ActionRequest>(core.Request).Places;
        core.Submit(actions.Where(a => asked.Contains(a.Who)).Select(a => BattleChoice.Fight(a.Who, a.Move, a.At)).ToList());
        var log = core.TakeLog().ToList();
        return (Lines(log), log.OfType<Said>().SelectMany(s => s.Shows.Concat(s.OnImpact)).Concat(log).ToList());
    }

    private static List<BattleEvent> Shown(BattleCore core)
    {
        var log = core.TakeLog().ToList();
        return log.OfType<Said>().SelectMany(s => s.Shows.Concat(s.OnImpact)).Concat(log).ToList();
    }

    // ------------------------------------------------------------------ Sky Drop

    [Fact]
    public void SkyDropCarriesItsTargetUpAndLetsItFallTheTurnAfter()
    {
        var core = Duel(Mon("Staraptor", 50, "Sky Drop"), Mon("Machamp", 50, "Tackle"));
        var foe = core.At(Foe);
        var said = Turn(core, 0, 0);
        Assert.Contains($"Staraptor took {foe.Name} into the sky!", said);
        // The one carried does nothing while it is up there
        Assert.DoesNotContain($"{foe.Name} used Tackle!", said);
        Assert.Equal(Elsewhere.InTheAir, core.At(Mine).Volatile.Elsewhere);
        Assert.Equal(Elsewhere.InTheAir, foe.Volatile.Elsewhere);
        Assert.Equal(foe.Pokemon!.MaxHP, foe.Pokemon.CurrentHP);

        // The second turn Staraptor is held to it, and the fall hurts
        Assert.DoesNotContain(Mine, Assert.IsType<ActionRequest>(core.Request).Places);
        Turn(core, 0, 0);
        Assert.True(foe.Pokemon.CurrentHP < foe.Pokemon.MaxHP);
        Assert.Equal(Elsewhere.No, core.At(Mine).Volatile.Elsewhere);
        Assert.Equal(Elsewhere.No, foe.Volatile.Elsewhere);
    }

    [Fact]
    public void WhoeverSkyDropCarriesCantBeCalledBack()
    {
        var core = Duel(Mon("Machamp", 50, "Tackle"), Mon("Staraptor", 50, "Sky Drop"));
        Turn(core, 0, 0);
        Assert.False(core.CanSwitchOut(core.At(Mine)));
    }

    [Fact]
    public void SkyDropCantLiftWhatWeighsTwoHundredKilogramsOrMore()
    {
        // Snorlax is 460 kg (Showdown's getWeight in hectograms, 2000 or more)
        var core = Duel(Mon("Staraptor", 50, "Sky Drop"), Idling(Mon("Snorlax", 50)));
        Assert.Contains($"{core.At(Foe).Name} is too heavy to be lifted!", Turn(core, 0, 0));
        Assert.Equal(Elsewhere.No, core.At(Mine).Volatile.Elsewhere);
    }

    [Fact]
    public void SkyDropsFallDoesntTouchAFlyingType()
    {
        var core = Duel(Mon("Staraptor", 50, "Sky Drop"), Idling(Mon("Pidgeot", 50)));
        Turn(core, 0, 0);
        Assert.Contains($"It doesn't affect {core.At(Foe).Name}...", Turn(core, 0, 0));
        Assert.Equal(core.At(Foe).Pokemon!.MaxHP, core.At(Foe).Pokemon!.CurrentHP);
        Assert.Equal(Elsewhere.No, core.At(Foe).Volatile.Elsewhere);
    }

    [Fact]
    public void GravityBringsSkyDropsTwoDown()
    {
        // Staraptor carries the foe's Machamp up; Clefable's Gravity, slower, sets both down and frees the carried
        var core = Pair(new[] { Mon("Staraptor", 50, "Sky Drop"), Mon("Clefable", 50, "Gravity") },
            new[] { Idling(Mon("Machamp", 50)), Idling(Mon("Machamp", 50)) });
        DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(Elsewhere.No, core.At(Foe).Volatile.Elsewhere);
        Assert.Equal(Elsewhere.No, core.At(Mine).Volatile.Elsewhere);
        Assert.Null(core.At(Foe).Volatile.LiftedBy);
        Assert.False(core.At(Mine).Volatile.Charging);
    }

    // ------------------------------------------------------------------ Ally Switch

    [Fact]
    public void AllySwitchChangesThePlacesAndAMoveFindsWhoeverStandsThereNow()
    {
        // Ally Switch goes first (+2); Machamp's Tackle at Alakazam's place then finds Snorlax standing there
        var alakazam = Mon("Alakazam", 50, "Ally Switch");
        var snorlax = Idling(Mon("Snorlax", 50));
        var core = Pair(new[] { alakazam, snorlax }, new[] { Mon("Machamp", 50, "Tackle"), Idling(Mon("Machamp", 50)) });
        var (said, shown) = DoubleTurnWithEvents(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, Mine), (Foe2, 0, null));
        Assert.Contains("Alakazam and Snorlax switched places!", said);
        Assert.Contains(shown, e => e is PlacesSwapped { First: var a, Second: var b } && a == Mine && b == Mine2);
        Assert.Same(snorlax, core.At(Mine).Pokemon);
        Assert.Same(alakazam, core.At(Mine2).Pokemon);
        Assert.Equal(alakazam.MaxHP, alakazam.CurrentHP);
        Assert.True(snorlax.CurrentHP < snorlax.MaxHP);
    }

    [Fact]
    public void AllySwitchFailsInASingleBattle()
    {
        var core = Duel(Mon("Alakazam", 50, "Ally Switch"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("But it failed!", Turn(core, 0, 0));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void AllySwitchUsedTheTurnAfterWorksOneTimeInThree(int roll, bool works)
    {
        var core = Pair(new[] { Idling(Mon("Alakazam", 50, "Ally Switch")), Idling(Mon("Snorlax", 50)) },
            new[] { Idling(Mon("Machamp", 50)), Idling(Mon("Machamp", 50)) }, Calm().Force(RollKind.Protect, roll));
        DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        // Alakazam now stands in the second place
        var said = DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(works, said.Contains("Alakazam and Snorlax switched places!"));
        Assert.Equal(!works, said.Contains("But it failed!"));
    }

    [Fact]
    public void WhatLastsOnAPokemonGoesWithItWhenThePlacesChange()
    {
        var looks = new ConditionLooks();
        looks.Tell(new StatusChanged(Mine, StatusCondition.Burn));
        looks.Tell(new VolatileChanged(Mine2, Confused: true, InLove: false));
        looks.Tell(new PlacesSwapped(Mine, Mine2));
        Assert.Equal(StatusCondition.Burn, looks.Of(Mine2).Status);
        Assert.True(looks.Of(Mine).Confused);
        Assert.Equal(StatusCondition.None, looks.Of(Mine).Status);
    }

    [Fact]
    public void AllySwitchKeepsTheLooksToTheRules()
    {
        // A burned Alakazam changes places with a confused Snorlax: what the screen shows follows the events alone
        var alakazam = Mon("Alakazam", 50, "Ally Switch");
        alakazam.Status = StatusCondition.Burn;
        var core = Pair(new[] { alakazam, Idling(Mon("Snorlax", 50)) }, new[] { Idling(Mon("Machamp", 50)), Idling(Mon("Machamp", 50)) });
        var looks = new ConditionLooks();
        foreach (var e in Shown(core)) looks.Tell(e);
        Assert.Equal(StatusCondition.Burn, looks.Of(Mine).Status);
        var (_, shown) = DoubleTurnWithEvents(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        foreach (var e in shown) looks.Tell(e);
        Assert.Null(looks.Difference(core));
        Assert.Equal(StatusCondition.Burn, looks.Of(Mine2).Status);
    }

    // ------------------------------------------------------------------ the Pledges

    [Fact]
    public void WaterAndFirePledgeCombineIntoARainbowOverTheUsersSide()
    {
        // Charizard (Speed 105) goes first and waits; Blastoise's Water Pledge goes at once as the two together
        var core = Pair(new[] { Mon("Charizard", 50, "Fire Pledge"), Mon("Blastoise", 50, "Water Pledge") },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        var (said, shown) = DoubleTurnWithEvents(core, (Mine, 0, Foe), (Mine2, 0, Foe), (Foe, 0, null), (Foe2, 0, null));
        InOrder(said, "Charizard used Fire Pledge!", "Charizard is waiting for Blastoise's move...", "Blastoise used Water Pledge!",
            "The two moves have become one! It's a combined move!", "A rainbow appeared in the sky on your team's side!");
        Assert.True(core.Field.Side(BattleSide.Player).Rainbow);
        Assert.Equal(3, core.Field.Side(BattleSide.Player).RainbowTurns); // four, one already run at the turn's end
        Assert.Contains(shown, e => e is SideConditionChanged { Side: BattleSide.Player, Condition: SideCondition.Rainbow, Layers: 1 });
        // Each moved once: Blastoise's turn was spent on the combined move
        Assert.Equal(1, said.Count(l => l == "Blastoise used Water Pledge!"));
    }

    [Fact]
    public void ACombinedPledgeIsOfTheirTypeAt150WithTheSameTypeBonusWhoeverUsesIt()
    {
        // Venusaur's Water Pledge alone: 80, no bonus for the type. After Charizard's Fire Pledge: Water, 150, × 1.5
        var alone = Pair(new[] { Idling(Mon("Charizard", 50, "Fire Pledge")), Mon("Venusaur", 50, "Water Pledge") },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        DoubleTurn(alone, (Mine, 1, null), (Mine2, 0, Foe), (Foe, 0, null), (Foe2, 0, null));
        int once = alone.At(Foe).Pokemon!.MaxHP - alone.At(Foe).Pokemon!.CurrentHP;

        var together = Pair(new[] { Mon("Charizard", 50, "Fire Pledge"), Mon("Venusaur", 50, "Water Pledge") },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        DoubleTurn(together, (Mine, 0, Foe2), (Mine2, 0, Foe), (Foe, 0, null), (Foe2, 0, null));
        int combined = together.At(Foe).Pokemon!.MaxHP - together.At(Foe).Pokemon!.CurrentHP;
        // 150 / 80 × 1.5 = 2.81 (rounding takes a little either way)
        Assert.InRange(combined, once * 26 / 10, once * 30 / 10);
        // Charizard's own Fire Pledge never landed: Foe2 is untouched
        Assert.Equal(together.At(Foe2).Pokemon!.MaxHP, together.At(Foe2).Pokemon!.CurrentHP);
    }

    [Fact]
    public void FireAndGrassPledgeLeaveASeaOfFireThatBurnsAnEighthAtEachTurnsEnd()
    {
        var core = Pair(new[] { Mon("Charizard", 50, "Fire Pledge"), Mon("Venusaur", 50, "Grass Pledge") },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        var (said, shown) = DoubleTurnWithEvents(core, (Mine, 0, Foe), (Mine2, 0, Foe), (Foe, 0, null), (Foe2, 0, null));
        Assert.Contains("A sea of fire enveloped the foe's team!", said);
        Assert.Contains(shown, e => e is SideConditionChanged { Side: BattleSide.Enemy, Condition: SideCondition.SeaOfFire, Layers: 1 });
        // The second Snorlax wasn't hit: an eighth of its 220 HP, 27, went to the sea of fire
        var other = core.At(Foe2).Pokemon!;
        Assert.Equal(other.MaxHP - other.MaxHP / 8, other.CurrentHP);
        Assert.Contains("Foe Snorlax was hurt by the sea of fire!", said);
    }

    [Fact]
    public void GrassAndWaterPledgeLeaveASwampThatQuartersSpeed()
    {
        var core = Pair(new[] { Mon("Venusaur", 50, "Grass Pledge"), Mon("Blastoise", 50, "Water Pledge") },
            new[] { Idling(Mon("Jolteon", 50)), Idling(Mon("Snorlax", 50)) });
        int before = core.EffectiveSpeed(core.At(Foe));
        var said = DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, Foe), (Foe, 0, null), (Foe2, 0, null));
        Assert.Contains("A swamp enveloped the foe's team!", said);
        Assert.True(core.Field.Side(BattleSide.Enemy).Swamp);
        // Jolteon's 135 Speed: × 1024 / 4096, rounded half down, is 34
        Assert.Equal(135, before);
        Assert.Equal(34, core.EffectiveSpeed(core.At(Foe)));
    }

    [Fact]
    public void WhatThePledgesLeaveLastsFourTurns()
    {
        var core = Pair(new[] { Idling(Mon("Charizard", 50, "Fire Pledge")), Idling(Mon("Blastoise", 50, "Water Pledge")) },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, Foe), (Foe, 0, null), (Foe2, 0, null));
        for (int turn = 2; turn <= 3; turn++) DoubleTurn(core, (Mine, 1, null), (Mine2, 1, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.True(core.Field.Side(BattleSide.Player).Rainbow);
        var (said, shown) = DoubleTurnWithEvents(core, (Mine, 1, null), (Mine2, 1, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Contains("The rainbow on your team's side disappeared!", said);
        Assert.Contains(shown, e => e is SideConditionChanged { Side: BattleSide.Player, Condition: SideCondition.Rainbow, Layers: 0 });
        Assert.False(core.Field.Side(BattleSide.Player).Rainbow);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARainbowMakesSideEffectsTwiceAsLikely(bool rainbow)
    {
        // Flamethrower burns one time in ten; the roll of 15 is under 20 and not under 10
        var core = Duel(Mon("Charizard", 50, "Flamethrower"), Idling(Mon("Snorlax", 50)), Calm().Force(RollKind.SideEffect, 15));
        if (rainbow) core.Field.Side(BattleSide.Player).RainbowTurns = 4;
        Turn(core, 0, 0);
        Assert.Equal(rainbow ? StatusCondition.Burn : StatusCondition.None, core.At(Foe).Pokemon!.Status);
    }

    [Fact]
    public void ASinglePledgeIsAPlainHit()
    {
        var core = Duel(Mon("Blastoise", 50, "Water Pledge"), Idling(Mon("Snorlax", 50)));
        var said = Turn(core, 0, 0);
        Assert.DoesNotContain("The two moves have become one! It's a combined move!", said);
        Assert.False(core.Field.Side(BattleSide.Player).Rainbow);
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < core.At(Foe).Pokemon!.MaxHP);
    }

    // ------------------------------------------------------------------ Sappy Seed

    [Fact]
    public void SappySeedSeedsWhatItHitsButAGrassType()
    {
        var core = Duel(Mon("Venusaur", 50, "Sappy Seed"), Idling(Mon("Snorlax", 50)));
        Assert.Equal("SappySeed", MoveDatabase.Get("Sappy Seed").Effect);
        Assert.Contains($"{core.At(Foe).Name} was seeded!", Turn(core, 0, 0));
        Assert.Equal(Mine, core.At(Foe).Volatile.SeededBy);

        var grass = Duel(Mon("Venusaur", 50, "Sappy Seed"), Idling(Mon("Tangela", 50)));
        Turn(grass, 0, 0);
        Assert.Null(grass.At(Foe).Volatile.SeededBy);
        Assert.True(grass.At(Foe).Pokemon!.CurrentHP < grass.At(Foe).Pokemon!.MaxHP);
    }

    // ------------------------------------------------------------------ Defog's and Feint's later reach

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefogClearsItsUsersOwnHazardsAndTheTerrainUnderTheLaterRules(bool modern)
    {
        var core = Wild(Mon("Togekiss", 50, "Defog"), Idling(Mon("Snorlax", 50)), rules: modern ? Modern : Ruleset.Platinum);
        var mine = core.Field.Side(BattleSide.Player);
        mine.Spikes = 2;
        mine.StealthRock = true;
        if (modern) core.SetTerrain(FieldCondition.GrassyTerrain, null);
        core.TakeLog();
        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var shown = Shown(core);
        Assert.Equal(modern ? 0 : 2, mine.Spikes);
        Assert.Equal(!modern, mine.StealthRock);
        if (!modern) return;
        Assert.Null(core.Field.Terrain);
        Assert.Contains(shown, e => e is SideConditionChanged { Side: BattleSide.Player, Condition: SideCondition.Spikes, Layers: 0 });
        Assert.Contains(shown, e => e is FieldConditionChanged { Condition: FieldCondition.GrassyTerrain, Up: false });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FeintHitsATargetThatIsntProtectingUnderTheLaterRules(bool modern)
    {
        var core = Wild(Mon("Weavile", 50, "Feint"), Idling(Mon("Snorlax", 50)), rules: modern ? Modern : Ruleset.Platinum);
        var said = Turn(core, 0, 0);
        var foe = core.At(Foe).Pokemon!;
        Assert.Equal(modern, foe.CurrentHP < foe.MaxHP);
        Assert.Equal(!modern, said.Contains("But it failed!"));
    }

    [Fact]
    public void FeintBreaksASidesWideGuard()
    {
        // Wide Guard (+3) goes up, Feint (+2) breaks it, and Golem's Rock Slide then reaches both foes
        var core = Pair(new[] { Mon("Weavile", 50, "Feint"), Mon("Golem", 50, "Rock Slide") },
            new[] { Mon("Hariyama", 50, "Wide Guard"), Idling(Mon("Snorlax", 50)) });
        var said = DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.Contains("Foe Hariyama fell for the feint!", said);
        Assert.DoesNotContain(said, l => l.StartsWith("Wide Guard protected"));
        Assert.True(core.At(Foe2).Pokemon!.CurrentHP < core.At(Foe2).Pokemon!.MaxHP);
    }

    // ------------------------------------------------------------------ R25's shortcuts, made exact

    [Fact]
    public void ARoundCalledAgainIsNotDoubled()
    {
        // Only a Round another Round brought forward is twice as strong (sourceEffect 'round'): Instruct's isn't
        var core = Pair(new[] { Mon("Jolteon", 50, "Round"), Mon("Oranguru", 50, "Instruct") },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        int full = core.At(Foe).Pokemon!.MaxHP;
        var (said, shown) = DoubleTurnWithEvents(core, (Mine, 0, Foe), (Mine2, 0, Mine), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(2, said.Count(l => l == "Jolteon used Round!"));
        var hits = shown.OfType<Struck>().Where(s => s.Place == Foe).Select(s => s.Hp).ToList();
        Assert.Equal(2, hits.Count);
        Assert.Equal(full - hits[0], hits[0] - hits[1]);
    }

    [Fact]
    public void InstructAimsTheMoveWhereItWasAimedBefore()
    {
        // Jolteon's Tackle at the second Snorlax: told again, it goes there again (lastMoveTargetLoc)
        var core = Pair(new[] { Mon("Jolteon", 50, "Tackle"), Mon("Oranguru", 50, "Instruct") },
            new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Snorlax", 50)) });
        DoubleTurn(core, (Mine, 0, Foe2), (Mine2, 0, Mine), (Foe, 0, null), (Foe2, 0, null));
        Assert.Equal(core.At(Foe).Pokemon!.MaxHP, core.At(Foe).Pokemon!.CurrentHP);
        Assert.True(core.At(Foe2).Pokemon!.CurrentHP < core.At(Foe2).Pokemon!.MaxHP);
    }

    [Fact]
    public void AshGreninjasWaterShurikenHitsThreeTimesAt20()
    {
        var greninja = With(Mon("Greninja", 50, "Water Shuriken"), ability: "Battle Bond");
        greninja.ChangeForm("Greninja-Ash");
        var core = Duel(greninja, Idling(Mon("Snorlax", 50)), Calm().Force(RollKind.HitCount, 0));
        Assert.Contains("Hit 3 times!", Turn(core, 0, 0));
    }

    [Fact]
    public void GravityEndsTelekinesis()
    {
        var core = Duel(Mon("Alakazam", 50, "Telekinesis", "Gravity"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 0);
        Assert.True(core.At(Foe).Volatile.TelekinesisTurns > 0);
        Turn(core, 1, 0);
        Assert.Equal(0, core.At(Foe).Volatile.TelekinesisTurns);
    }
}
