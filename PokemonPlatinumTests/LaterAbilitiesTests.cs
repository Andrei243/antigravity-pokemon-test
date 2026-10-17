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
/// The abilities of Generations 5 to 9 (plan 06 · R27), each by Pokémon Showdown's rule (data/abilities.ts at the pinned
/// commit). One rule to a test, under the modern rules, the rolls fixed by kind (<see cref="CoreScenario.Calm"/>: no
/// critical hit, the strongest roll, nothing misses, no side effect but a sure one). A bonus is held by the damage it
/// makes beside the same hit without the ability, worked out from Showdown's 4096ths; a foe made by <c>Mon</c> with no
/// moves knows only Idle, so its move 0 sits the turn out.
/// </summary>
public class LaterAbilitiesTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    private static BattleCore Duel(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, BattleWeather sky = BattleWeather.None, params Pokemon[] bench) =>
        Wild(mine, foe, rolls, Modern, sky, BattleTerrain.Plain, bench);

    private static BattleCore Pair(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null) => Doubles(mine, theirs, rolls, Modern);

    /// <summary>What the foe lost to the player's first move in one turn, the foe sitting it out.</summary>
    private static int Took(Pokemon mine, Pokemon foe, BattleWeather sky = BattleWeather.None, Action<BattleCore>? before = null)
    {
        var core = Duel(mine, foe, sky: sky);
        before?.Invoke(core);
        core.TakeLog();
        int hp = foe.CurrentHP;
        Turn(core, 0, 0);
        return hp - foe.CurrentHP;
    }

    /// <summary>The same hit with the attacker's ability and with one that does nothing to it.</summary>
    private static (int With, int Without) Compare(string species, string move, string ability, string foe, string? foeAbility = null, BattleWeather sky = BattleWeather.None)
    {
        Pokemon Foe() => foeAbility == null ? Mon(foe, 50) : With(Mon(foe, 50), foeAbility);
        int with = Took(With(Mon(species, 50, move), ability), Foe(), sky);
        int without = Took(With(Mon(species, 50, move), "Run Away"), Foe(), sky);
        return (with, without);
    }

    /// <summary>The same hit on the foe with its ability and with one that does nothing.</summary>
    private static (int With, int Without) Defended(string species, string move, string foe, string ability, Action<Pokemon>? ready = null, int level = 50)
    {
        var a = With(Mon(foe, 50), ability);
        var b = With(Mon(foe, 50), "Run Away");
        ready?.Invoke(a);
        ready?.Invoke(b);
        return (Took(Mon(species, level, move), a), Took(Mon(species, level, move), b));
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

    /// <summary>A ratio of two hits within a point or two of the rounding (a whole number of damage each).</summary>
    private static void Near(double expected, int with, int without) =>
        Assert.InRange(with / (double)without, expected - 0.04, expected + 0.04);

    // ------------------------------------------------------------------ the power

    [Fact]
    public void ToughClawsMakesAMoveThatTouchesFiveThirteenthsStronger() // 5325/4096
    {
        var (with, without) = Compare("Barbaracle", "Tackle", "Tough Claws", "Snorlax");
        Near(1.3, with, without);
    }

    [Fact]
    public void ToughClawsLeavesAMoveThatDoesntTouchAlone()
    {
        var (with, without) = Compare("Barbaracle", "Rock Slide", "Tough Claws", "Snorlax");
        Assert.Equal(without, with);
    }

    [Fact]
    public void StrongJawMakesABiteHalfAsStrongAgain()
    {
        var (with, without) = Compare("Tyrantrum", "Crunch", "Strong Jaw", "Snorlax");
        Near(1.5, with, without);
    }

    [Fact]
    public void MegaLauncherMakesAPulseHalfAsStrongAgain()
    {
        var (with, without) = Compare("Clawitzer", "Water Pulse", "Mega Launcher", "Snorlax");
        Near(1.5, with, without);
    }

    [Fact]
    public void SharpnessMakesASlicingMoveHalfAsStrongAgain()
    {
        var (with, without) = Compare("Gallade", "Psycho Cut", "Sharpness", "Snorlax");
        Near(1.5, with, without);
    }

    [Fact]
    public void SheerForceMakesAMoveWithASideEffectStrongerAndTakesTheEffectAway()
    {
        var (with, without) = Compare("Nidoking", "Fire Punch", "Sheer Force", "Snorlax");
        Near(1.3, with, without);
        var foe = Mon("Snorlax", 50);
        var core = Duel(With(Mon("Nidoking", 50, "Fire Punch"), "Sheer Force"), foe, Calm().Force(RollKind.SideEffect, 0));
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
    }

    [Fact]
    public void SheerForceLeavesAMoveWithNoSideEffectAlone()
    {
        var (with, without) = Compare("Nidoking", "Tackle", "Sheer Force", "Snorlax");
        Assert.Equal(without, with);
    }

    [Fact]
    public void PixilateMakesANormalMoveFairyAndAFifthStronger()
    {
        var core = Duel(With(Mon("Sylveon", 50, "Hyper Voice"), "Pixilate"), Mon("Hydreigon", 50));
        var said = Turn(core, 0, 0);
        // Fairy against a Dark and Dragon type: four times
        Assert.Contains("It's super effective!", said);
    }

    [Fact]
    public void AerilateLeavesAMoveOfItsOwnRuleAlone()
    {
        var core = Duel(With(Mon("Pinsir", 50, "Weather Ball"), "Aerilate"), Mon("Breloom", 50));
        var said = Turn(core, 0, 0);
        Assert.DoesNotContain("It's super effective!", said);
    }

    [Fact]
    public void LiquidVoiceMakesASoundMoveWater()
    {
        var core = Duel(With(Mon("Primarina", 50, "Hyper Voice"), "Liquid Voice"), Mon("Rhyperior", 50));
        Assert.Contains("It's super effective!", Turn(core, 0, 0));
    }

    [Fact]
    public void ProteanTurnsItsUserIntoTheMovesTypeOnceAStay()
    {
        var core = Duel(With(Mon("Greninja", 50, "Ice Beam", "Dark Pulse"), "Protean"), Mon("Snorlax", 50));
        var said = Turn(core, 0, 0);
        Assert.Contains($"{core.At(Mine).Name} transformed into the Ice type!", said);
        Assert.Equal(new[] { PokemonType.Ice }, core.At(Mine).Types);
        said = Turn(core, 1, 0);
        Assert.DoesNotContain(said, l => l.Contains("transformed into"));
    }

    [Fact]
    public void AnalyticHitsHarderAfterItsTargetHasMoved()
    {
        int with = Took(Slow(With(Mon("Magnezone", 50, "Flash Cannon"), "Analytic")), Fast(Mon("Snorlax", 50)));
        int without = Took(Slow(With(Mon("Magnezone", 50, "Flash Cannon"), "Run Away")), Fast(Mon("Snorlax", 50)));
        Near(1.3, with, without);
    }

    [Fact]
    public void SandForceStrengthensAGroundMoveInTheSand()
    {
        var (with, without) = Compare("Garchomp", "Earthquake", "Sand Force", "Steelix", sky: BattleWeather.Sandstorm);
        Near(1.3, with, without);
    }

    [Fact]
    public void ToxicBoostStrengthensAPoisonedUsersPhysicalMove()
    {
        var a = With(Mon("Zangoose", 50, "Slash"), "Toxic Boost");
        var b = With(Mon("Zangoose", 50, "Slash"), "Run Away");
        a.Status = b.Status = StatusCondition.Poison;
        Near(1.5, Took(a, Mon("Snorlax", 50)), Took(b, Mon("Snorlax", 50)));
    }

    [Fact]
    public void SupremeOverlordGrowsWithTheFallen()
    {
        var down = Mon("Pawniard", 50);
        down.CurrentHP = 0;
        down.Status = StatusCondition.Faint;
        var a = With(Mon("Kingambit", 50, "Iron Head"), "Supreme Overlord");
        int with = Took(a, Mon("Snorlax", 50), before: null);
        Assert.True(with > 0);
        var withFallen = Duel(With(Mon("Kingambit", 50, "Iron Head"), "Supreme Overlord"), Mon("Snorlax", 50), bench: down);
        withFallen.TakeLog();
        var foe = withFallen.At(Foe).Pokemon!;
        int hp = foe.CurrentHP;
        Turn(withFallen, 0, 0);
        Near(4506 / 4096.0, hp - foe.CurrentHP, with);
    }

    [Fact]
    public void ParentalBondHitsTwiceTheSecondAQuarterAsStrong()
    {
        var core = Duel(With(Mon("Kangaskhan", 50, "Body Slam"), "Parental Bond"), Mon("Snorlax", 80));
        Assert.Contains("Hit 2 times!", Turn(core, 0, 0));
    }

    [Fact]
    public void SteelySpiritStrengthensItsAllysSteelMove()
    {
        var core = Pair(new[] { Mon("Perrserker", 50, "Iron Head"), With(Mon("Snorlax", 50), "Steely Spirit") }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) });
        var plain = Pair(new[] { Mon("Perrserker", 50, "Iron Head"), With(Mon("Snorlax", 50), "Run Away") }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) });
        int Hit(BattleCore c)
        {
            var f = c.At(Foe).Pokemon!;
            int hp = f.CurrentHP;
            DoubleTurn(c, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
            return hp - f.CurrentHP;
        }
        Near(1.5, Hit(core), Hit(plain));
    }

    [Fact]
    public void DarkAuraStrengthensEveryDarkMove()
    {
        var (with, without) = Compare("Yveltal", "Dark Pulse", "Dark Aura", "Snorlax");
        Near(5448 / 4096.0, with, without);
    }

    [Fact]
    public void AuraBreakTurnsAnAuraRound()
    {
        int with = Took(With(Mon("Yveltal", 50, "Dark Pulse"), "Dark Aura"), With(Mon("Snorlax", 50), "Aura Break"));
        int without = Took(With(Mon("Yveltal", 50, "Dark Pulse"), "Run Away"), Mon("Snorlax", 50));
        Near(0.75, with, without);
    }

    // ------------------------------------------------------------------ the stats

    [Fact]
    public void DefeatistHalvesItsAttackAtHalfItsHp()
    {
        var a = With(Mon("Archeops", 50, "Rock Slide"), "Defeatist");
        var b = With(Mon("Archeops", 50, "Rock Slide"), "Run Away");
        a.CurrentHP = b.CurrentHP = a.MaxHP / 2;
        Near(0.5, Took(a, Mon("Snorlax", 50)), Took(b, Mon("Snorlax", 50)));
    }

    [Fact]
    public void FurCoatHalvesAPhysicalHit()
    {
        var (with, without) = Defended("Machamp", "Strength", "Furfrou", "Fur Coat");
        Near(0.5, with, without);
    }

    [Fact]
    public void FluffyHalvesATouchAndDoublesFire()
    {
        var (touch, plain) = Defended("Machamp", "Strength", "Bewear", "Fluffy");
        Near(0.5, touch, plain);
        var (fire, unburnt) = Defended("Charizard", "Flamethrower", "Bewear", "Fluffy", level: 15);
        Near(2.0, fire, unburnt);
    }

    [Fact]
    public void MultiscaleHalvesAHitAtFullHp()
    {
        var (with, without) = Defended("Mamoswine", "Ice Shard", "Dragonite", "Multiscale");
        Near(0.5, with, without);
    }

    [Fact]
    public void IceScalesHalvesASpecialHit()
    {
        var (with, without) = Defended("Charizard", "Flamethrower", "Frosmoth", "Ice Scales", level: 10);
        Near(0.5, with, without);
    }

    [Fact]
    public void WaterBubbleHalvesFireAndCantBeBurned()
    {
        var (with, without) = Defended("Charizard", "Flamethrower", "Araquanid", "Water Bubble");
        Near(0.5, with, without);
        var foe = With(Mon("Araquanid", 50), "Water Bubble");
        var core = Duel(Mon("Charizard", 50, "Will-O-Wisp"), foe);
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
    }

    [Fact]
    public void TabletsOfRuinWeakenTheAttackOfEveryoneElse()
    {
        int with = Took(Mon("Machamp", 50, "Strength"), With(Mon("Wo-Chien", 50), "Tablets of Ruin"));
        int without = Took(Mon("Machamp", 50, "Strength"), With(Mon("Wo-Chien", 50), "Run Away"));
        Near(0.75, with, without);
    }

    [Fact]
    public void ProtosynthesisRaisesTheBestStatInTheSun()
    {
        var mine = With(Mon("Great Tusk", 50, "Earthquake"), "Protosynthesis");
        var core = Duel(mine, Mon("Snorlax", 50), sky: BattleWeather.Sun);
        Assert.Contains(Lines(core.Log), l => l.Contains("raised its Attack in the sunlight"));
        var (with, without) = Compare("Great Tusk", "Earthquake", "Protosynthesis", "Snorlax", sky: BattleWeather.Sun);
        Near(5325 / 4096.0, with, without);
    }

    [Fact]
    public void StakeoutHitsANewcomerTwiceAsHard()
    {
        // The foe switches in this turn and the slow Stakeout user hits whoever came in
        var foes = new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) };
        var core = Against(new[] { Slow(With(Mon("Gumshoos", 50, "Crunch"), "Stakeout")) }, foes, rules: Modern);
        var plain = Against(new[] { Slow(With(Mon("Gumshoos", 50, "Crunch"), "Run Away")) }, new[] { Mon("Snorlax", 50), Mon("Snorlax", 50) }, rules: Modern);
        int Hit(BattleCore c)
        {
            c.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Switch(Foe, 1));
            var f = c.At(Foe).Pokemon!;
            return f.MaxHP - f.CurrentHP;
        }
        Near(2.0, Hit(core), Hit(plain));
    }

    // ------------------------------------------------------------------ taking a move in

    [Fact]
    public void SapSipperTakesAGrassMoveInAndRaisesItsAttack()
    {
        var foe = With(Mon("Azumarill", 50), "Sap Sipper");
        var core = Duel(Mon("Venusaur", 50, "Energy Ball"), foe);
        Turn(core, 0, 0);
        Assert.Equal(foe.MaxHP, foe.CurrentHP);
        Assert.Equal(1, Stage(foe, StatType.Attack));
    }

    [Fact]
    public void LightningRodTakesAnElectricMoveInByTheLaterRules()
    {
        var foe = With(Mon("Rhydon", 50), "Lightning Rod");
        var core = Duel(Mon("Pikachu", 50, "Thunder Wave"), foe);
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
        Assert.Equal(1, Stage(foe, StatType.SpAttack));
    }

    [Fact]
    public void LightningRodOnlyDrawsTheMoveUnderPlatinumsRules()
    {
        var foe = With(Mon("Raichu", 50), "Lightning Rod");
        var core = Wild(Mon("Pikachu", 50, "Thunder Shock"), foe, rules: Ruleset.Platinum);
        Turn(core, 0, 0);
        Assert.True(foe.CurrentHP < foe.MaxHP);
        Assert.Equal(0, Stage(foe, StatType.SpAttack));
    }

    [Fact]
    public void EarthEaterHealsOnAGroundMove()
    {
        var foe = With(Mon("Orthworm", 50), "Earth Eater");
        foe.CurrentHP = foe.MaxHP / 2;
        var core = Duel(Mon("Garchomp", 50, "Earthquake"), foe);
        Turn(core, 0, 0);
        Assert.True(foe.CurrentHP > foe.MaxHP / 2);
    }

    [Fact]
    public void WellBakedBodyTakesFireInAndRaisesItsDefenseTwo()
    {
        var foe = With(Mon("Dachsbun", 50), "Well-Baked Body");
        var core = Duel(Mon("Charizard", 50, "Flamethrower"), foe);
        Turn(core, 0, 0);
        Assert.Equal(foe.MaxHP, foe.CurrentHP);
        Assert.Equal(2, Stage(foe, StatType.Defense));
    }

    [Fact]
    public void BulletproofBlocksABomb()
    {
        var foe = With(Mon("Chesnaught", 50), "Bulletproof");
        var core = Duel(Mon("Gengar", 50, "Shadow Ball"), foe);
        Turn(core, 0, 0);
        Assert.Equal(foe.MaxHP, foe.CurrentHP);
    }

    [Fact]
    public void OvercoatBlocksAPowder()
    {
        var foe = With(Mon("Forretress", 50), "Overcoat");
        var core = Duel(Mon("Breloom", 50, "Spore"), foe);
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
    }

    [Fact]
    public void GoodAsGoldBlocksAnothersStatusMove()
    {
        var foe = With(Mon("Gholdengo", 50), "Good as Gold");
        var core = Duel(Mon("Jolteon", 50, "Thunder Wave"), foe);
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
    }

    [Fact]
    public void DazzlingStopsAMoveOfPriority()
    {
        var foe = With(Mon("Bruxish", 50), "Dazzling");
        var core = Duel(Mon("Lucario", 50, "Quick Attack"), foe);
        var said = Turn(core, 0, 0);
        Assert.Equal(foe.MaxHP, foe.CurrentHP);
        Assert.Contains(said, l => l.Contains("keeps") && l.Contains("Quick Attack"));
    }

    [Fact]
    public void PranksterMovesAStatusMoveFirst()
    {
        var core = Duel(Slow(With(Mon("Sableye", 50, "Will-O-Wisp"), "Prankster")), Fast(Mon("Machamp", 50, "Tackle")));
        var said = Turn(core, 0, 0);
        InOrder(said, $"{core.At(Mine).Name} used Will-O-Wisp!", $"{core.At(Foe).Name} used Tackle!");
    }

    [Fact]
    public void APranksterStatusMoveDoesntAffectADarkType()
    {
        var foe = Mon("Umbreon", 50);
        var core = Duel(With(Mon("Sableye", 50, "Will-O-Wisp"), "Prankster"), foe);
        Assert.Contains($"It doesn't affect {core.At(Foe).Name}...", Turn(core, 0, 0));
        Assert.Equal(StatusCondition.None, foe.Status);
    }

    [Fact]
    public void GaleWingsMovesAFlyingMoveFirstAtFullHp()
    {
        var core = Duel(Slow(With(Mon("Talonflame", 50, "Brave Bird"), "Gale Wings")), Fast(Mon("Snorlax", 50, "Tackle")));
        InOrder(Turn(core, 0, 0), $"{core.At(Mine).Name} used Brave Bird!", $"{core.At(Foe).Name} used Tackle!");
    }

    [Fact]
    public void TriageMovesAHealingMoveFirst()
    {
        var mine = Slow(With(Mon("Comfey", 50, "Draining Kiss"), "Triage"));
        var core = Duel(mine, Fast(Mon("Snorlax", 50, "Tackle")));
        InOrder(Turn(core, 0, 0), $"{core.At(Mine).Name} used Draining Kiss!", $"{core.At(Foe).Name} used Tackle!");
    }

    [Fact]
    public void MagicBounceSendsAStatusMoveBack()
    {
        var foe = With(Mon("Espeon", 50), "Magic Bounce");
        var mine = Mon("Raichu", 50, "Toxic");
        var core = Duel(mine, foe);
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
        Assert.Equal(StatusCondition.Toxic, mine.Status);
    }

    [Fact]
    public void UnseenFistTouchesPastProtect()
    {
        var foe = Fast(Mon("Snorlax", 50, "Protect"));
        var core = Duel(Slow(With(Mon("Urshifu", 50, "Close Combat"), "Unseen Fist")), foe);
        Turn(core, 0, 0);
        Assert.True(foe.CurrentHP < foe.MaxHP);
    }

    [Fact]
    public void InfiltratorHitsPastASubstitute()
    {
        var foe = Fast(Mon("Snorlax", 50, "Substitute"));
        var core = Duel(Slow(With(Mon("Dragapult", 50, "Dragon Pulse"), "Infiltrator")), foe);
        Turn(core, 0, 0);
        Assert.True(foe.CurrentHP < foe.MaxHP - foe.MaxHP / 4);
    }

    // ------------------------------------------------------------------ answering

    [Fact]
    public void DefiantRaisesItsAttackTwoWhenAFoeLowersAStat()
    {
        var mine = With(Mon("Bisharp", 50), "Defiant");
        var core = Duel(mine, Mon("Raichu", 50, "Growl"));
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(mine, StatType.Attack));
    }

    [Fact]
    public void CompetitiveAnswersIntimidate()
    {
        var foe = With(Mon("Milotic", 50), "Competitive");
        var core = Duel(With(Mon("Gyarados", 50), "Intimidate"), foe);
        Assert.Equal(-1, Stage(foe, StatType.Attack));
        Assert.Equal(2, Stage(foe, StatType.SpAttack));
    }

    [Fact]
    public void MirrorArmorTurnsAStatDropBack()
    {
        var foe = With(Mon("Corviknight", 50), "Mirror Armor");
        var mine = Mon("Raichu", 50, "Growl");
        var core = Duel(mine, foe);
        Turn(core, 0, 0);
        Assert.Equal(0, Stage(foe, StatType.Attack));
        Assert.Equal(-1, Stage(mine, StatType.Attack));
    }

    [Fact]
    public void ContraryTurnsAChangeRound()
    {
        var mine = With(Mon("Serperior", 50, "Leaf Storm"), "Contrary");
        var core = Duel(mine, Mon("Snorlax", 50));
        Turn(core, 0, 0);
        Assert.Equal(2, Stage(mine, StatType.SpAttack));
    }

    [Fact]
    public void JustifiedRaisesItsAttackWhenADarkMoveHitsIt()
    {
        var foe = With(Mon("Lucario", 50), "Justified");
        Turn(Duel(Mon("Umbreon", 50, "Bite"), foe), 0, 0);
        Assert.Equal(1, Stage(foe, StatType.Attack));
    }

    [Fact]
    public void WeakArmorTradesDefenseForSpeed()
    {
        var foe = With(Mon("Skarmory", 50), "Weak Armor");
        Turn(Duel(Mon("Snorlax", 50, "Tackle"), foe), 0, 0);
        Assert.Equal(-1, Stage(foe, StatType.Defense));
        Assert.Equal(2, Stage(foe, StatType.Speed));
    }

    [Fact]
    public void StaminaRaisesItsDefenseWhenHit()
    {
        var foe = With(Mon("Mudsdale", 50), "Stamina");
        Turn(Duel(Mon("Snorlax", 50, "Tackle"), foe), 0, 0);
        Assert.Equal(1, Stage(foe, StatType.Defense));
    }

    [Fact]
    public void BerserkRaisesItsSpAtkWhenAHitTakesItUnderHalf()
    {
        var foe = With(Mon("Drampa", 50), "Berserk");
        foe.CurrentHP = foe.MaxHP / 2 + 3;
        Turn(Duel(Mon("Snorlax", 20, "Tackle"), foe), 0, 0);
        Assert.True(foe.CurrentHP * 2 <= foe.MaxHP);
        Assert.Equal(1, Stage(foe, StatType.SpAttack));
    }

    [Fact]
    public void GooeySlowsWhoeverTouchesIt()
    {
        var mine = Mon("Snorlax", 50, "Tackle");
        Turn(Duel(mine, With(Mon("Goodra", 50), "Gooey")), 0, 0);
        Assert.Equal(-1, Stage(mine, StatType.Speed));
    }

    [Fact]
    public void IronBarbsHurtsWhoeverTouchesIt()
    {
        var mine = Mon("Snorlax", 50, "Tackle");
        Turn(Duel(mine, With(Mon("Ferrothorn", 50), "Iron Barbs")), 0, 0);
        Assert.Equal(mine.MaxHP - mine.MaxHP / 8, mine.CurrentHP);
    }

    [Fact]
    public void MummyPassesItselfOnToWhoeverTouchesIt()
    {
        var mine = With(Mon("Snorlax", 50, "Bite"), "Thick Fat");
        var core = Duel(mine, With(Mon("Cofagrigus", 50), "Mummy"));
        Turn(core, 0, 0);
        Assert.Equal("Mummy", mine.AbilityName);
    }

    [Fact]
    public void PickpocketTakesTheItemOfWhoeverTouchesIt()
    {
        var mine = With(Mon("Snorlax", 50, "Tackle"), item: "Leftovers");
        var foe = With(Mon("Weavile", 50), "Pickpocket");
        Turn(Duel(mine, foe), 0, 0);
        Assert.Null(mine.HeldItem);
        Assert.Equal("Leftovers", foe.HeldItem?.Name);
    }

    [Fact]
    public void CottonDownSlowsEveryoneElse()
    {
        var mine = Mon("Snorlax", 50, "Tackle");
        Turn(Duel(mine, With(Mon("Eldegoss", 50), "Cotton Down")), 0, 0);
        Assert.Equal(-1, Stage(mine, StatType.Speed));
    }

    [Fact]
    public void SeedSowerLaysGrassyTerrainWhenHit()
    {
        var core = Duel(Mon("Snorlax", 50, "Tackle"), With(Mon("Arboliva", 50), "Seed Sower"));
        Turn(core, 0, 0);
        Assert.True(core.Field.Has(FieldCondition.GrassyTerrain));
    }

    [Fact]
    public void ToxicDebrisScattersToxicSpikesAtTheAttackersFeet()
    {
        var core = Duel(Mon("Snorlax", 50, "Tackle"), With(Mon("Glimmora", 50), "Toxic Debris"));
        Turn(core, 0, 0);
        Assert.Equal(1, core.Field.Side(BattleSide.Player).ToxicSpikes);
    }

    [Fact]
    public void MoxieRaisesItsAttackForAKnockOut()
    {
        var mine = With(Mon("Gyarados", 80, "Waterfall"), "Moxie");
        var core = Against(new[] { mine }, new[] { Mon("Magikarp", 5), Mon("Magikarp", 5) }, rules: Modern);
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(mine, StatType.Attack));
    }

    [Fact]
    public void BeastBoostRaisesItsBestStatForAKnockOut()
    {
        var mine = With(Mon("Pheromosa", 80, "Lunge"), "Beast Boost");
        var core = Against(new[] { mine }, new[] { Mon("Magikarp", 5), Mon("Magikarp", 5) }, rules: Modern);
        Turn(core, 0, 0);
        Assert.Equal(LaterAbilitiesProbe.Best(mine), mine.StatStages.Single(s => s.Value == 1).Key);
    }

    [Fact]
    public void PoisonTouchPoisonsWhatItTouchesThreeTimesInTen()
    {
        var foe = Mon("Regirock", 50);
        var core = Duel(With(Mon("Grimer", 50, "Pound"), "Poison Touch"), foe, Calm().Force(RollKind.AbilityChance, 0));
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.Poison, foe.Status);
    }

    [Fact]
    public void CorrosionPoisonsASteelType()
    {
        var foe = Mon("Steelix", 50);
        Turn(Duel(With(Mon("Salazzle", 50, "Toxic"), "Corrosion"), foe), 0, 0);
        Assert.Equal(StatusCondition.Toxic, foe.Status);
    }

    // ------------------------------------------------------------------ the field and over time

    [Fact]
    public void ElectricSurgeLaysElectricTerrainOnEntry()
    {
        var core = Duel(With(Mon("Pincurchin", 50), "Electric Surge"), Mon("Snorlax", 50));
        Assert.True(core.Field.Has(FieldCondition.ElectricTerrain));
    }

    [Fact]
    public void PsychicSurgeLaysPsychicTerrainOnEntry()
    {
        var core = Duel(With(Mon("Indeedee", 50), "Psychic Surge"), Mon("Snorlax", 50));
        Assert.True(core.Field.Has(FieldCondition.PsychicTerrain));
    }

    [Fact]
    public void OrichalcumPulseBringsTheSun()
    {
        var core = Duel(With(Mon("Koraidon", 50), "Orichalcum Pulse"), Mon("Snorlax", 50));
        Assert.Equal(BattleWeather.Sun, core.Field.Weather);
    }

    [Fact]
    public void SurgeSurferDoublesItsSpeedOnElectricTerrain()
    {
        var mine = With(Mon("Raichu", 50), "Surge Surfer");
        var core = Duel(mine, With(Mon("Pincurchin", 50), "Electric Surge"));
        int plain = mine.Speed;
        Assert.Equal(plain * 2, core.EffectiveSpeed(core.At(Mine)));
    }

    [Fact]
    public void SandRushDoublesItsSpeedInTheSand()
    {
        var mine = With(Mon("Excadrill", 50), "Sand Rush");
        var core = Duel(mine, Mon("Snorlax", 50), sky: BattleWeather.Sandstorm);
        Assert.Equal(mine.Speed * 2, core.EffectiveSpeed(core.At(Mine)));
    }

    [Fact]
    public void ScreenCleanerTakesTheScreensAway()
    {
        var core = Duel(Mon("Snorlax", 50, "Reflect"), Mon("Mr. Mime", 50), bench: With(Mon("Mr. Rime", 50), "Screen Cleaner"));
        Turn(core, 0, 0);
        Assert.True(core.Field.Side(BattleSide.Player).Reflect);
        SwitchTo(core, 1);
        Assert.False(core.Field.Side(BattleSide.Player).Reflect);
    }

    [Fact]
    public void IntrepidSwordRaisesItsAttackOnceABattle()
    {
        var mine = With(Mon("Zacian", 50), "Intrepid Sword");
        var core = Duel(mine, Mon("Snorlax", 50), bench: Mon("Snorlax", 50));
        Assert.Equal(1, Stage(mine, StatType.Attack));
        SwitchTo(core, 1);
        SwitchTo(core, core.At(Mine).Roster!.Members.IndexOf(mine));
        Assert.Equal(0, Stage(mine, StatType.Attack));
    }

    [Fact]
    public void RegeneratorHealsAThirdOnTheWayOut()
    {
        var mine = With(Mon("Slowbro", 50), "Regenerator");
        mine.CurrentHP = 10;
        var core = Duel(mine, Mon("Snorlax", 50), bench: Mon("Snorlax", 50));
        SwitchTo(core, 1);
        Assert.Equal(10 + mine.MaxHP / 3, mine.CurrentHP);
    }

    [Fact]
    public void HarvestGrowsAnEatenBerryBackInTheSun()
    {
        var mine = With(Mon("Exeggutor", 50), "Harvest", "Sitrus Berry");
        var core = Duel(mine, Mon("Snorlax", 50, "Tackle"), sky: BattleWeather.Sun);
        mine.CurrentHP = mine.MaxHP / 2 + 5;
        Turn(core, 0, 0);
        Assert.Equal("Sitrus Berry", mine.HeldItem?.Name);
    }

    [Fact]
    public void MoodyRaisesOneStatTwoAndLowersAnotherOne()
    {
        var mine = With(Mon("Octillery", 50), "Moody");
        var core = Duel(mine, Mon("Snorlax", 50));
        Turn(core, 0, 0);
        Assert.Contains(mine.StatStages, s => s.Value == 2);
        Assert.Contains(mine.StatStages, s => s.Value == -1);
    }

    [Fact]
    public void HeavyMetalDoublesItsWeight()
    {
        var mine = With(Mon("Aggron", 50), "Heavy Metal");
        var core = Duel(mine, Mon("Snorlax", 50));
        Assert.Equal(mine.Weight * 2, LaterAbilitiesProbe.Weight(core.At(Mine)), 2);
    }

    [Fact]
    public void MercilessLandsACriticalHitOnAPoisonedTarget()
    {
        var foe = Mon("Snorlax", 50);
        foe.Status = StatusCondition.Poison;
        Assert.Contains("A critical hit!", Turn(Duel(With(Mon("Toxapex", 50, "Liquidation"), "Merciless"), foe), 0, 0));
    }

    [Fact]
    public void GorillaTacticsHoldsItToItsFirstMove()
    {
        var mine = With(Mon("Darmanitan", 50, "Icicle Crash", "Flare Blitz"), "Gorilla Tactics");
        var core = Duel(mine, Mon("Snorlax", 80));
        Turn(core, 0, 0);
        Assert.Equal("Icicle Crash", core.At(Mine).ChoiceLock?.Name);
    }

    [Fact]
    public void GuardDogTakesAnIntimidateAsAnInsult()
    {
        var foe = With(Mon("Mabosstiff", 50), "Guard Dog");
        Duel(With(Mon("Gyarados", 50), "Intimidate"), foe);
        Assert.Equal(1, Stage(foe, StatType.Attack));
    }

    [Fact]
    public void ComatoseKeepsEveryConditionOff()
    {
        var foe = With(Mon("Komala", 50), "Comatose");
        var core = Duel(Mon("Jolteon", 50, "Toxic"), foe);
        Assert.Contains(Lines(core.Log), l => l.EndsWith("is drowsing!"));
        Turn(core, 0, 0);
        Assert.Equal(StatusCondition.None, foe.Status);
    }

    [Fact]
    public void QuickDrawSometimesGoesFirst()
    {
        var core = Duel(Slow(With(Mon("Slowbro", 50, "Tackle"), "Quick Draw")), Fast(Mon("Jolteon", 50, "Tackle")), Calm().Force(RollKind.AbilityChance, 0));
        InOrder(Turn(core, 0, 0), $"{core.At(Mine).Name} used Tackle!", $"{core.At(Foe).Name} used Tackle!");
    }

    [Fact]
    public void MyceliumMightGoesLastWithAStatusMove()
    {
        var core = Duel(Fast(With(Mon("Toedscruel", 50, "Spore"), "Mycelium Might")), Slow(Mon("Snorlax", 50, "Tackle")));
        InOrder(Turn(core, 0, 0), $"{core.At(Foe).Name} used Tackle!", $"{core.At(Mine).Name} used Spore!");
    }

    [Fact]
    public void SoulHeartRaisesItsSpAtkForEveryFaint()
    {
        var mine = With(Mon("Magearna", 80, "Flash Cannon"), "Soul-Heart");
        var core = Against(new[] { mine }, new[] { Mon("Magikarp", 5), Mon("Magikarp", 5) }, rules: Modern);
        Turn(core, 0, 0);
        Assert.Equal(1, Stage(mine, StatType.SpAttack));
    }

    [Fact]
    public void PoisonPuppeteerConfusesWhoeverItPoisons()
    {
        var core = Duel(With(Mon("Pecharunt", 50, "Toxic"), "Poison Puppeteer"), Mon("Regirock", 50));
        Turn(core, 0, 0);
        Assert.True(core.At(Foe).IsConfused);
    }

    [Fact]
    public void StalwartIsNotDrawnByStormDrain()
    {
        var core = Pair(new[] { With(Mon("Duraludon", 50, "Water Pulse"), "Stalwart"), Mon("Snorlax", 50) },
            new[] { Mon("Snorlax", 50), With(Mon("Gastrodon", 50), "Storm Drain") });
        var target = core.At(Foe).Pokemon!;
        DoubleTurn(core, (Mine, 0, Foe), (Mine2, 0, null), (Foe, 0, null), (Foe2, 0, null));
        Assert.True(target.CurrentHP < target.MaxHP);
    }

    [Fact]
    public void ShapesAndTerastalAbilitiesHaveTheirEntries()
    {
        foreach (var name in new[] { "Zen Mode", "Stance Change", "Disguise", "Ice Face", "Tera Shift", "Embody Aspect" })
            Assert.Contains(name, AbilityEffectTable.Names);
    }
}

/// <summary>What the tests above read of the engine's own helpers.</summary>
internal static class LaterAbilitiesProbe
{
    public static StatType Best(Pokemon p) => PokemonPlatinumEngine.Battle.Effects.LaterAbilities.BestStat(p);

    public static float Weight(Battler b) => BattleCore.WeightOf(b);
}
