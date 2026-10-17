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
/// The later generations' moves, batch 1 (plan 06 · R24): Generation 5's own effects, the moves that lay the field
/// of R19b and Photon Geyser, each by Pokémon Showdown's rule (data/moves.ts at the pinned commit). One rule to a
/// test, under the modern rules, the rolls fixed by kind (<see cref="CoreScenario.Calm"/>: no critical hit, the
/// strongest roll, nothing misses, no side effect).
/// </summary>
public class LaterMovesTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static BattleCore Duel(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, BattleWeather sky = BattleWeather.None) =>
        Wild(mine, foe, rolls, Modern, sky);

    /// <summary>What the foe lost to the player's move in one turn, the foe idling.</summary>
    private static int Took(Pokemon mine, Pokemon foe, int move = 0, Action<BattleCore>? before = null)
    {
        var core = Duel(mine, Idling(foe));
        before?.Invoke(core);
        int hp = foe.CurrentHP;
        Turn(core, move, foe.Moves.Count - 1);
        return hp - foe.CurrentHP;
    }

    private static int Damage(Pokemon attacker, Pokemon defender, string move) =>
        DamageCalculator.CalculateDamage(attacker, defender, attacker.Moves.First(m => m.Name == move), Steady(), Modern).Damage;

    // ------------------------------------------------------------------ other numbers in the formula

    [Fact]
    public void PsyshockHitsTheTargetsDefense()
    {
        // Alakazam (Sp. Atk 140) against Blissey (Defense 15, Sp. Def 140), level 50, power 80:
        // floor(floor(22 × 80 × 140 / 15) / 50) + 2 = floor(16426 / 50) + 2 = 330; × 1.5 for its own type = 495.
        // Against the Sp. Def it would be floor(1760 / 50) + 2 = 37, and 55.
        Assert.Equal(495, Damage(Mon("Alakazam", 50, "Psyshock"), Mon("Blissey", 50), "Psyshock"));
    }

    [Fact]
    public void FoulPlayTakesTheTargetsAttackAndItsStage()
    {
        // Sableye's Foul Play (95) with Machamp's Attack (135) against its Defense (85): floor(22 × 95 × 135 / 85)
        // = 3319, / 50 = 66, + 2 = 68; × 1.5 = 102; Fighting resists Dark: 51. At Machamp's +2, Attack 270:
        // floor(22 × 95 × 270 / 85) = 6638, / 50 = 132, + 2 = 134; × 1.5 = 201; halved, 100.
        var sableye = Mon("Sableye", 50, "Foul Play");
        var machamp = Mon("Machamp", 50);
        Assert.Equal(51, Damage(sableye, machamp, "Foul Play"));
        machamp.StatStages[StatType.Attack] = 2;
        Assert.Equal(100, Damage(sableye, machamp, "Foul Play"));
    }

    [Fact]
    public void ChipAwayTakesNoNoticeOfTheTargetsDefenseStage()
    {
        var user = Mon("Emboar", 50, "Chip Away");
        var target = Mon("Snorlax", 50);
        int plain = Damage(user, target, "Chip Away");
        target.StatStages[StatType.Defense] = 6;
        Assert.Equal(plain, Damage(user, target, "Chip Away"));
        // …nor of its evasion
        var core = Duel(Mon("Emboar", 50, "Chip Away"), Idling(Mon("Snorlax", 50)), Calm().Force(RollKind.Accuracy, 99));
        core.At(Foe).Pokemon!.StatStages[StatType.Evasion] = 6;
        Assert.DoesNotContain(Turn(core, 0, 1), l => l.Contains("missed") || l.Contains("avoided"));
    }

    [Fact]
    public void PhotonGeyserIsPhysicalWhenItsUsersAttackIsTheHigher()
    {
        // Necrozma: Attack 112 and Sp. Atk 132 at level 50; at +2 its Attack is 224
        var necrozma = Mon("Necrozma", 50, "Photon Geyser");
        var blissey = Mon("Blissey", 50);
        var user = Wild(necrozma, Idling(blissey), Calm(), Modern).At(Mine);
        Assert.False(DamageCalculator.PhysicalByStats(user, necrozma.Moves[0].Data));
        int special = Damage(necrozma, blissey, "Photon Geyser");
        necrozma.StatStages[StatType.Attack] = 2;
        Assert.True(DamageCalculator.PhysicalByStats(user, necrozma.Moves[0].Data));
        // Against Blissey's Defense of 15 the physical side hits far harder than its Sp. Atk did against Sp. Def 140
        Assert.True(Damage(necrozma, blissey, "Photon Geyser") > 5 * special);
    }

    [Fact]
    public void StormThrowIsAlwaysACriticalHit()
    {
        // Calm() forces every critical roll to miss: Storm Throw rolls none
        var core = Duel(Mon("Throh", 50, "Storm Throw"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("A critical hit!", Turn(core, 0, 1));
    }

    // ------------------------------------------------------------------ power by what holds

    [Fact]
    public void VenoshockIsTwiceAsStrongAgainstAPoisonedTarget()
    {
        int plain = Took(Mon("Seviper", 50, "Venoshock"), Mon("Snorlax", 50));
        var poisoned = Mon("Snorlax", 50);
        poisoned.Status = StatusCondition.Poison;
        // Less the eighth that poison takes at the turn's end (220 / 8 = 27)
        int doubled = Took(Mon("Seviper", 50, "Venoshock"), poisoned) - 27;
        Assert.InRange(doubled, 2 * plain - 4, 2 * plain + 1);
    }

    [Fact]
    public void HexIsTwiceAsStrongAgainstATargetWithACondition()
    {
        int plain = Took(Mon("Gengar", 50, "Hex"), Mon("Snorlax", 50));
        var paralysed = Mon("Snorlax", 50);
        paralysed.Status = StatusCondition.Paralyze;
        Assert.InRange(Took(Mon("Gengar", 50, "Hex"), paralysed), 2 * plain - 4, 2 * plain + 1);
    }

    [Fact]
    public void AcrobaticsIsTwiceAsStrongWithNothingHeld()
    {
        int bare = Took(Mon("Swellow", 50, "Acrobatics"), Mon("Snorlax", 50));
        int holding = Took(With(Mon("Swellow", 50, "Acrobatics"), item: "Potion"), Mon("Snorlax", 50));
        Assert.InRange(bare, 2 * holding - 4, 2 * holding + 1);
    }

    [Fact]
    public void StoredPowerGrowsByTwentyForEveryStageRaised()
    {
        // Raised Defense and Speed don't touch the damage but the power: 20 + 20 × 4 = 100, five times 20
        int plain = Took(Mon("Sigilyph", 50, "Stored Power"), Mon("Snorlax", 50));
        int raised = Took(Mon("Sigilyph", 50, "Stored Power"), Mon("Snorlax", 50), before: core =>
        {
            var p = core.At(Mine).Pokemon!;
            p.StatStages[StatType.Defense] = 2;
            p.StatStages[StatType.Speed] = 2;
        });
        Assert.InRange(raised, 5 * plain - 12, 5 * plain + 2);
    }

    [Fact]
    public void HeavySlamGoesByHowManyTimesHeavierItsUserIs()
    {
        Assert.Equal(120, BattleCore.HeavySlamPower(100f, 20f));
        Assert.Equal(100, BattleCore.HeavySlamPower(100f, 24f));
        Assert.Equal(80, BattleCore.HeavySlamPower(100f, 33f));
        Assert.Equal(60, BattleCore.HeavySlamPower(100f, 50f));
        Assert.Equal(40, BattleCore.HeavySlamPower(100f, 50.1f));
    }

    [Fact]
    public void EchoedVoiceGrowsWhenItIsUsedTurnAfterTurn()
    {
        var core = Duel(Mon("Exploud", 50, "Echoed Voice"), Idling(Mon("Snorlax", 50)));
        var foe = core.At(Foe).Pokemon!;
        int hp = foe.CurrentHP;
        Turn(core, 0, 1);
        int first = hp - foe.CurrentHP;
        hp = foe.CurrentHP;
        Turn(core, 0, 1);
        Assert.InRange(hp - foe.CurrentHP, 2 * first - 4, 2 * first + 1);
    }

    // ------------------------------------------------------------------ how it hits

    [Fact]
    public void HurricaneCantMissInTheRain()
    {
        var core = Duel(Mon("Tornadus", 50, "Hurricane"), Idling(Mon("Snorlax", 50)), Calm().Force(RollKind.Accuracy, 99), BattleWeather.Rain);
        var said = Turn(core, 0, 1);
        Assert.DoesNotContain(said, l => l.Contains("missed"));
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < core.At(Foe).Pokemon!.MaxHP);
    }

    [Fact]
    public void FinalGambitDealsWhatItsUserHasLeftAndItsUserFaints()
    {
        var user = Mon("Throh", 50, "Final Gambit");
        var core = Duel(user, Idling(Mon("Snorlax", 50)));
        int left = user.CurrentHP;
        var foe = core.At(Foe).Pokemon!;
        var said = Turn(core, 0, 1);
        Assert.Equal(foe.MaxHP - left, foe.CurrentHP);
        Assert.Equal(0, user.CurrentHP);
        Assert.Contains("Throh fainted!", said);
    }

    [Fact]
    public void SynchronoiseDoesntAffectOneThatSharesNoTypeWithItsUser()
    {
        var core = Duel(Mon("Musharna", 50, "Synchronoise"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("It doesn't affect Foe Snorlax...", Turn(core, 0, 1));
        Assert.Equal(core.At(Foe).Pokemon!.MaxHP, core.At(Foe).Pokemon!.CurrentHP);
    }

    [Fact]
    public void FreezeShockTakesATurnToGetReady()
    {
        var core = Duel(Mon("Kyurem", 50, "Freeze Shock"), Idling(Mon("Snorlax", 50)));
        var said = Turn(core, 0, 1);
        Assert.Contains("Kyurem became cloaked in a freezing light!", said);
        Assert.Equal(core.At(Foe).Pokemon!.MaxHP, core.At(Foe).Pokemon!.CurrentHP);
        Turn(core, 0, 1);
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < core.At(Foe).Pokemon!.MaxHP);
    }

    [Fact]
    public void DualChopHitsTwice()
    {
        var core = Duel(Mon("Haxorus", 50, "Dual Chop"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("Hit 2 times!", Turn(core, 0, 1));
    }

    // ------------------------------------------------------------------ what the hit does

    [Fact]
    public void ClearSmogTakesAwayTheTargetsStatChanges()
    {
        var core = Duel(Mon("Amoonguss", 50, "Clear Smog"), Idling(Mon("Snorlax", 50)));
        core.At(Foe).Pokemon!.StatStages[StatType.Attack] = 2;
        Assert.Contains("Foe Snorlax's stat changes were removed!", Turn(core, 0, 1));
        Assert.Equal(0, core.At(Foe).Pokemon!.StatStages.GetValueOrDefault(StatType.Attack));
    }

    [Fact]
    public void IncinerateBurnsTheTargetsBerryUp()
    {
        var core = Duel(Mon("Emboar", 50, "Incinerate"), Idling(With(Mon("Snorlax", 50), item: "Sitrus Berry")));
        Assert.Contains("Foe Snorlax's Sitrus Berry was burned up!", Turn(core, 0, 1));
        Assert.Null(core.At(Foe).Pokemon!.HeldItem);
    }

    [Fact]
    public void SmackDownBringsAFlyingTypeWithinAGroundMovesReach()
    {
        var core = Duel(Mon("Tyrantrum", 50, "Smack Down", "Earthquake"), Idling(Mon("Pidgeot", 50)));
        Assert.Contains("Foe Pidgeot fell straight down!", Turn(core, 0, 1));
        int hp = core.At(Foe).Pokemon!.CurrentHP;
        Turn(core, 1, 1);
        Assert.True(core.At(Foe).Pokemon!.CurrentHP < hp);
    }

    [Fact]
    public void CircleThrowDragsATrainersPokemonOut()
    {
        var core = Against(new[] { Mon("Throh", 50, "Circle Throw") }, new[] { Idling(Mon("Snorlax", 50)), Idling(Mon("Blissey", 50)) }, rules: Modern);
        Assert.Contains("Foe Blissey was dragged out!", Turn(core, 0, 1));
        Assert.Equal("Blissey", core.At(Foe).Pokemon!.Species.Name);
    }

    // ------------------------------------------------------------------ status moves

    [Fact]
    public void ShellSmashTradesItsGuardForTwoStagesOfAttackSpAtkAndSpeed()
    {
        var core = Duel(Mon("Cloyster", 50, "Shell Smash"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 1);
        var s = core.At(Mine).Pokemon!.StatStages;
        Assert.Equal((-1, -1, 2, 2, 2), (s[StatType.Defense], s[StatType.SpDefense], s[StatType.Attack], s[StatType.SpAttack], s[StatType.Speed]));
    }

    [Fact]
    public void ShiftGearRaisesAttackOnceAndSpeedTwice()
    {
        var core = Duel(Mon("Klinklang", 50, "Shift Gear"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 1);
        var s = core.At(Mine).Pokemon!.StatStages;
        Assert.Equal((1, 2), (s[StatType.Attack], s[StatType.Speed]));
    }

    [Fact]
    public void AutotomizeRaisesSpeedTwiceAndTakesAHundredKilogramsOff()
    {
        var core = Duel(Mon("Steelix", 50, "Autotomize"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("Steelix became nimble!", Turn(core, 0, 1));
        Assert.Equal(2, core.At(Mine).Pokemon!.StatStages[StatType.Speed]);
        Assert.Equal(400f - 100f, BattleCore.WeightOf(core.At(Mine)), 2);
    }

    [Fact]
    public void HealPulseGivesHalfTheTargetsHpBack()
    {
        var core = Duel(Mon("Audino", 50, "Heal Pulse"), Idling(Mon("Snorlax", 50)));
        var foe = core.At(Foe).Pokemon!;
        foe.CurrentHP = 10;
        Turn(core, 0, 1);
        // Half of 220, rounded up
        Assert.Equal(120, foe.CurrentHP);
    }

    [Fact]
    public void GuardSplitAndPowerSplitShareTheAverages()
    {
        var core = Duel(Mon("Shuckle", 50, "Guard Split", "Power Split"), Idling(Mon("Snorlax", 50)));
        var mine = core.At(Mine).Pokemon!;
        var foe = core.At(Foe).Pokemon!;
        int defense = (mine.Defense + foe.Defense) / 2, attack = (mine.Attack + foe.Attack) / 2;
        Assert.Contains("Shuckle shared its guard with the target!", Turn(core, 0, 1));
        Assert.Equal((defense, defense), (mine.Defense, foe.Defense));
        Turn(core, 1, 1);
        Assert.Equal((attack, attack), (mine.Attack, foe.Attack));
    }

    [Fact]
    public void SoakMakesTheTargetAWaterType()
    {
        var core = Duel(Mon("Wailord", 50, "Soak"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 1);
        Assert.Equal(new[] { PokemonType.Water }, core.At(Foe).Types);
    }

    [Fact]
    public void ReflectTypeGivesItsUserTheTargetsTypes()
    {
        var core = Duel(Mon("Starmie", 50, "Reflect Type"), Idling(Mon("Torterra", 50)));
        Assert.Contains("Starmie's type became the same as Foe Torterra's type!", Turn(core, 0, 1));
        Assert.Equal(new[] { PokemonType.Grass, PokemonType.Ground }, core.At(Mine).Types);
    }

    [Fact]
    public void SimpleBeamAndEntrainmentChangeTheTargetsAbility()
    {
        var core = Duel(With(Mon("Sigilyph", 50, "Simple Beam", "Entrainment"), ability: "Magic Guard"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 1);
        Assert.Equal("Simple", core.At(Foe).Pokemon!.AbilityName);
        Turn(core, 1, 1);
        Assert.Equal("Magic Guard", core.At(Foe).Pokemon!.AbilityName);
    }

    [Fact]
    public void BestowHandsTheUsersItemToATargetHoldingNone()
    {
        var core = Duel(With(Mon("Togekiss", 50, "Bestow"), item: "Leftovers"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("Foe Snorlax received Leftovers from Togekiss!", Turn(core, 0, 1));
        Assert.Null(core.At(Mine).Pokemon!.HeldItem);
        Assert.Equal("Leftovers", core.At(Foe).Pokemon!.HeldItem?.Name);
    }

    // ------------------------------------------------------------------ the field of the later games

    [Theory]
    [InlineData("Electric Terrain", FieldCondition.ElectricTerrain)]
    [InlineData("Grassy Terrain", FieldCondition.GrassyTerrain)]
    [InlineData("Misty Terrain", FieldCondition.MistyTerrain)]
    [InlineData("Psychic Terrain", FieldCondition.PsychicTerrain)]
    public void ATerrainMoveLaysItsTerrainAndFailsWhileItLies(string move, FieldCondition terrain)
    {
        var core = Duel(Mon("Tapu Koko", 50, move), Idling(Mon("Snorlax", 50)));
        var look = new FieldLook();
        Turn(core, 0, 1);
        foreach (var e in Told(core)) look.Tell(e);
        Assert.Equal(terrain, core.Field.Terrain);
        Assert.Equal(4, core.Field.TerrainTurns);
        Assert.True(look.Of(terrain).Up);
        Assert.Contains("But it failed!", Turn(core, 0, 1));
    }

    [Fact]
    public void StickyWebIsLaidOnTheOtherSide()
    {
        var core = Duel(Mon("Galvantula", 50, "Sticky Web"), Idling(Mon("Snorlax", 50)));
        Turn(core, 0, 1);
        Assert.True(core.Field.Side(BattleSide.Enemy).StickyWeb);
        Assert.Contains("But it failed!", Turn(core, 0, 1));
    }

    [Fact]
    public void AuroraVeilGoesUpOnlyInSnow()
    {
        var clear = Duel(Mon("Glalie", 50, "Aurora Veil"), Idling(Mon("Snorlax", 50)));
        Assert.Contains("But it failed!", Turn(clear, 0, 1));
        var snowy = Duel(Mon("Glalie", 50, "Aurora Veil"), Idling(Mon("Snorlax", 50)), sky: BattleWeather.Snow);
        Turn(snowy, 0, 1);
        Assert.True(snowy.Field.Side(BattleSide.Player).AuroraVeil);
    }

    private static IEnumerable<BattleEvent> Told(BattleCore core)
    {
        foreach (var e in core.Log)
        {
            if (e is Said said)
            {
                foreach (var shown in said.Shows) yield return shown;
                foreach (var landing in said.OnImpact) yield return landing;
            }
            else yield return e;
        }
    }
}
