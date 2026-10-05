using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The held items and berries of plan 06 · R8, and the items used from the bag in battle, on the battle's rules
/// alone: each bonus of the damage formula in the original's own place (<c>BattleSystem_CalcMoveDamage</c>), the
/// items of Speed and the turn's order (<c>BattleSystem_CompareBattlerSpeed</c>), accuracy's items, the berries
/// eaten in a pinch (<c>BattleSystem_TriggerHeldItem</c>), the berries against a type
/// (<c>subscript_type_resist_berry</c>), the items that answer a hit (<c>BattleSystem_TriggerHeldItemOnHit</c>),
/// and the bag (<c>BtlCmd_UseBagItem</c>). One rule to a test, the rolls it hangs on fixed by kind, and the
/// numbers worked out from the original's code in a comment. A hit is
/// <c>attack × power × 22 / defense / 50 + 2</c> at level 50, each step rounded down. The stats, with no IVs:
/// <code>
///            HP   Atk  Def  SpA  SpD  Spe
/// Machamp    150  135   85   70   90   60   Fighting
/// Blastoise  139   88  105   90  110   83   Water
/// Snorlax    220  115   70   70  115   35   Normal
/// Charizard  138   89   83  114   90  105   Fire / Flying
/// Pikachu     95   60   35   55   45   95   Electric
/// Ditto      108   53   53   53   53   53   Normal
/// </code>
/// </summary>
public class BattleItemTests
{
    /// <summary>What one use of a move takes off a Blastoise, by whoever uses it.</summary>
    private static int OnBlastoise(Pokemon user, Pokemon? blastoise = null, BattleRandom? rolls = null)
    {
        blastoise ??= Mon("Blastoise", 50);
        int before = blastoise.CurrentHP;
        Turn(Wild(user, blastoise, rolls));
        return before - blastoise.CurrentHP;
    }

    /// <summary>What one use of a move takes off a Snorlax.</summary>
    private static int OnSnorlax(Pokemon user, Pokemon? snorlax = null)
    {
        snorlax ??= Mon("Snorlax", 50);
        int before = snorlax.CurrentHP;
        Turn(Wild(user, snorlax));
        return before - snorlax.CurrentHP;
    }

    /// <summary>A Pokémon of a nature, otherwise as <see cref="Scenario.Mon"/> makes it.</summary>
    private static Pokemon Natured(string species, Nature nature, params string[] moves)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, 50, Gender.Male, nature, false);
        p.Moves.Clear();
        foreach (var name in moves) p.Moves.Add(new Move(MoveDatabase.Get(name)));
        if (moves.Length == 0) p.Moves.Add(new Move(Idle));
        return p;
    }

    private static int IndexOf(List<string> said, string line) => said.FindIndex(s => s == line);

    // ================================================================== the damage formula, in the original's order

    [Fact]
    public void ATypesItemStrengthensItsMovesByAFifthAndSoDoesAPlate()
    {
        // Charizard's Flamethrower (95) on Snorlax: 114 × 95 × 22 / 115 / 50 = 41, + 2 = 43, half as much again
        // for its own type: 64. Charcoal makes the power 114: 114 × 114 × 22 / 115 / 50 = 49, + 2 = 51, so 76.
        // A Flame Plate does the same; a Water move gets nothing from either (Water Pulse's 60: 26 + 2 = 28)
        Assert.Equal(64, OnSnorlax(Mon("Charizard", 50, "Flamethrower")));
        Assert.Equal(76, OnSnorlax(With(Mon("Charizard", 50, "Flamethrower"), item: "Charcoal")));
        Assert.Equal(76, OnSnorlax(With(Mon("Charizard", 50, "Flamethrower"), item: "Flame Plate")));
        Assert.Equal(28, OnSnorlax(With(Mon("Charizard", 50, "Water Pulse"), item: "Charcoal")));
        Assert.Equal(28, OnSnorlax(Mon("Charizard", 50, "Water Pulse")));
    }

    [Fact]
    public void AChoiceBandAddsHalfToAttackAndHoldsItsHolderToTheMove()
    {
        // Machamp's Tackle (35) on Blastoise: 135 × 35 × 22 / 105 / 50 = 19, + 2 = 21. With a Choice Band the
        // Attack is 202: 202 × 35 × 22 / 105 / 50 = 29, + 2 = 31
        Assert.Equal(21, OnBlastoise(Mon("Machamp", 50, "Tackle")));
        var banded = With(Mon("Machamp", 50, "Tackle", "Karate Chop"), item: "Choice Band");
        var core = Wild(banded, Mon("Blastoise", 50));
        Turn(core);
        Assert.Equal(139 - 31, core.At(Foe).Pokemon!.CurrentHP);
        // Now only Tackle may be chosen
        Assert.Contains("can only use Tackle", core.WhyNotMove(core.At(Mine), banded.Moves[1]));
        Assert.Null(core.WhyNotMove(core.At(Mine), banded.Moves[0]));
        var refused = Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 1), BattleChoice.Fight(Foe, 0)));
        Assert.Contains("can only use Tackle", refused.Message);
    }

    [Fact]
    public void ChoiceSpecsWiseGlassesAndMuscleBandEachInTheirPlace()
    {
        // Charizard's Flamethrower on Snorlax (64 plain): Choice Specs make the Sp. Atk 171: 171 × 95 × 22 / 115 / 50
        // = 62, + 2 = 64, so 96 with its own type's bonus. Wise Glasses make the power 104: 114 × 104 × 22 / 115 / 50
        // = 45, + 2 = 47, so 70. A Muscle Band on Machamp's Tackle (21 plain): the power 38, 135 × 38 × 22 / 105 / 50
        // = 21, + 2 = 23; on Flamethrower it does nothing
        Assert.Equal(96, OnSnorlax(With(Mon("Charizard", 50, "Flamethrower"), item: "Choice Specs")));
        Assert.Equal(70, OnSnorlax(With(Mon("Charizard", 50, "Flamethrower"), item: "Wise Glasses")));
        Assert.Equal(64, OnSnorlax(With(Mon("Charizard", 50, "Flamethrower"), item: "Muscle Band")));
        Assert.Equal(23, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), item: "Muscle Band")));
        Assert.Equal(21, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), item: "Wise Glasses")));
    }

    [Fact]
    public void TheItemsOfOneSpeciesWorkForThatSpeciesAlone()
    {
        // A Light Ball doubles Pikachu's power: Thunderbolt (95) on Blastoise, 55 × 95 × 22 / 110 / 50 = 20, + 2 = 22,
        // × 1.5 for its own type = 33, × 2 against Water = 66; with the ball the power is 190: 41, + 2 = 43, 64, 128.
        // A Thick Club doubles Marowak's Attack (85 to 170): Bone Club (65) on Blastoise, 85 × 65 × 22 / 105 / 50 = 23,
        // + 2 = 25, 37 for its own type; doubled, 46, + 2 = 48, 72. Metal Powder doubles Ditto's Defense (53 to 106)
        // against Machamp's Tackle: 135 × 35 × 22 / 53 / 50 = 39, + 2 = 41 without, 19, + 2 = 21 with. A Deep Sea
        // Tooth doubles Clamperl's Sp. Atk (79 to 158): Water Gun (40) on Snorlax, 79 × 40 × 22 / 115 / 50 = 12, + 2
        // = 14, 21 for its own type; doubled, 24, + 2 = 26, 39. None of them does anything for Machamp
        Assert.Equal(66, OnBlastoise(Mon("Pikachu", 50, "Thunderbolt")));
        Assert.Equal(128, OnBlastoise(With(Mon("Pikachu", 50, "Thunderbolt"), item: "Light Ball")));
        Assert.Equal(37, OnBlastoise(Mon("Marowak", 50, "Bone Club")));
        Assert.Equal(72, OnBlastoise(With(Mon("Marowak", 50, "Bone Club"), item: "Thick Club")));
        var ditto = Mon("Ditto", 50);
        Turn(Wild(Mon("Machamp", 50, "Tackle"), ditto));
        Assert.Equal(108 - 41, ditto.CurrentHP);
        ditto = With(Mon("Ditto", 50), item: "Metal Powder");
        Turn(Wild(Mon("Machamp", 50, "Tackle"), ditto));
        Assert.Equal(108 - 21, ditto.CurrentHP);
        Assert.Equal(21, OnSnorlax(Mon("Clamperl", 50, "Water Gun")));
        Assert.Equal(39, OnSnorlax(With(Mon("Clamperl", 50, "Water Gun"), item: "Deep Sea Tooth")));
        Assert.Equal(21, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), item: "Light Ball")));
        Assert.Equal(21, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), item: "Thick Club")));
    }

    [Fact]
    public void ASoulDewAddsHalfToALatiosSpecialStats()
    {
        // Latios (Sp. Atk 135) uses Dragon Pulse (90) on Blastoise: 135 × 90 × 22 / 110 / 50 = 48, + 2 = 50, and
        // 75 for its own type. With a Soul Dew the Sp. Atk is 202: 202 × 90 × 22 / 110 / 50 = 72, + 2 = 74, so 111
        Assert.Equal(75, OnBlastoise(Mon("Latios", 50, "Dragon Pulse")));
        Assert.Equal(111, OnBlastoise(With(Mon("Latios", 50, "Dragon Pulse"), item: "Soul Dew")));
        // And it hardens a Latias's Sp. Def (135 to 202) against Charizard's Flamethrower (95), which Dragon resists: 114 × 95 × 22 / 135
        // / 50 = 35, + 2 = 37, 55 for its own type, 27 against Dragon; 114 × 95 × 22 / 202 / 50 = 23, + 2 = 25, 37, 18
        var latias = Mon("Latias", 50);
        Turn(Wild(Mon("Charizard", 50, "Flamethrower"), latias));
        Assert.Equal(latias.MaxHP - 27, latias.CurrentHP);
        latias = With(Mon("Latias", 50), item: "Soul Dew");
        Turn(Wild(Mon("Charizard", 50, "Flamethrower"), latias));
        Assert.Equal(latias.MaxHP - 18, latias.CurrentHP);
    }

    [Fact]
    public void AnExpertBeltAddsAFifthToASuperEffectiveHitAlone()
    {
        // Machamp's Karate Chop (50) on Snorlax: 135 × 50 × 22 / 70 / 50 = 42, + 2 = 44, 66 for its own type, 132
        // against Normal. The belt makes it 158 (132 × 120 / 100). On Blastoise, where Fighting is nothing special,
        // Tackle stays 21
        Assert.Equal(132, OnSnorlax(Mon("Machamp", 50, "Karate Chop")));
        Assert.Equal(158, OnSnorlax(With(Mon("Machamp", 50, "Karate Chop"), item: "Expert Belt")));
        Assert.Equal(21, OnBlastoise(With(Mon("Machamp", 50, "Tackle"), item: "Expert Belt")));
    }

    [Fact]
    public void ALifeOrbAddsThreeTenthsBeforeTheRollAndCostsATenthAfter()
    {
        // Machamp's Tackle on Blastoise, 21 plain: 21 × 130 / 100 = 27 before the roll. Then a tenth of 150, 15
        var machamp = With(Mon("Machamp", 50, "Tackle"), item: "Life Orb");
        var blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(machamp, blastoise));
        Assert.Equal(139 - 27, blastoise.CurrentHP);
        Assert.Equal(150 - 15, machamp.CurrentHP);
        Assert.Contains("Machamp lost some of its HP!", said);
        // Not with Magic Guard
        machamp = With(Mon("Machamp", 50, "Tackle"), ability: "Magic Guard", item: "Life Orb");
        Turn(Wild(machamp, Mon("Blastoise", 50)));
        Assert.Equal(150, machamp.CurrentHP);
    }

    [Fact]
    public void AMetronomeGrowsWithEveryRepeatAndStartsAgainAtAnotherMove()
    {
        // Tackle on Blastoise: 21 the first time, 21 × 11 / 10 = 23 the second, 21 × 12 / 10 = 25 the third. A
        // Growl between puts the count back: 21 again
        var machamp = With(Mon("Machamp", 50, "Tackle", "Growl"), item: "Metronome");
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(machamp, blastoise);
        Turn(core);
        Assert.Equal(139 - 21, blastoise.CurrentHP);
        Turn(core);
        Assert.Equal(139 - 21 - 23, blastoise.CurrentHP);
        Turn(core);
        Assert.Equal(139 - 21 - 23 - 25, blastoise.CurrentHP);
        Turn(core, mine: 1);
        Turn(core);
        Assert.Equal(139 - 21 - 23 - 25 - 21, blastoise.CurrentHP);
    }

    [Fact]
    public void AnItemsCriticalHitStagesGoByTheHolder()
    {
        static int Stages(Pokemon p) => DamageCalculator.ItemCritStages(new Battler(BattleSide.Player, 0) { Pokemon = p });
        Assert.Equal(1, Stages(With(Mon("Machamp", 50), item: "Scope Lens")));
        Assert.Equal(1, Stages(With(Mon("Machamp", 50), item: "Razor Claw")));
        Assert.Equal(2, Stages(With(Mon("Chansey", 50), item: "Lucky Punch")));
        Assert.Equal(0, Stages(With(Mon("Machamp", 50), item: "Lucky Punch")));
        Assert.Equal(2, Stages(With(Mon("Farfetch'd", 50), item: "Leek")));
        Assert.Equal(0, Stages(With(Mon("Chansey", 50), item: "Leek")));
        Assert.Equal(0, Stages(Mon("Machamp", 50)));
    }

    // ================================================================== Speed and the turn's order

    private static bool MineMovesFirst(Pokemon mine, Pokemon foe, BattleRandom? rolls = null)
    {
        var said = Turn(Wild(mine, foe, rolls));
        int me = said.FindIndex(s => s.StartsWith($"{mine.DisplayName} used"));
        int them = said.FindIndex(s => s.StartsWith($"Foe {foe.DisplayName} used"));
        Assert.True(me >= 0 && them >= 0, string.Join("\n", said));
        return me < them;
    }

    [Fact]
    public void AChoiceScarfAddsHalfToSpeedUnlessKlutz()
    {
        // Machamp (60) against Blastoise (83): a Choice Scarf makes it 90 and first; with Klutz the scarf is nothing to it
        Assert.False(MineMovesFirst(Mon("Machamp", 50, "Tackle"), Mon("Blastoise", 50, "Tackle")));
        Assert.True(MineMovesFirst(With(Mon("Machamp", 50, "Tackle"), item: "Choice Scarf"), Mon("Blastoise", 50, "Tackle")));
        Assert.False(MineMovesFirst(With(Mon("Machamp", 50, "Tackle"), ability: "Klutz", item: "Choice Scarf"), Mon("Blastoise", 50, "Tackle")));
    }

    [Fact]
    public void AMachoBraceHalvesSpeedWhateverTheAbilitySays()
    {
        // Blastoise (83) with a Macho Brace is 41, behind Machamp (60); Klutz doesn't undo it, as the original
        // reads the item itself; a Power item and an Iron Ball halve it the same way
        Assert.False(MineMovesFirst(Mon("Machamp", 50, "Tackle"), Mon("Blastoise", 50, "Tackle")));
        Assert.True(MineMovesFirst(Mon("Machamp", 50, "Tackle"), With(Mon("Blastoise", 50, "Tackle"), item: "Macho Brace")));
        Assert.True(MineMovesFirst(Mon("Machamp", 50, "Tackle"), With(Mon("Blastoise", 50, "Tackle"), ability: "Klutz", item: "Macho Brace")));
        Assert.True(MineMovesFirst(Mon("Machamp", 50, "Tackle"), With(Mon("Blastoise", 50, "Tackle"), item: "Power Anklet")));
        Assert.True(MineMovesFirst(Mon("Machamp", 50, "Tackle"), With(Mon("Blastoise", 50, "Tackle"), item: "Iron Ball")));
    }

    [Fact]
    public void QuickPowderDoublesADittosSpeedAndALaggingTailGoesLast()
    {
        // Ditto (53) against Charizard (105): a Quick Powder makes it 106 and first. Charizard with a Lagging Tail
        // goes after Machamp (60) for all its 105; two Lagging Tails: the faster goes after
        Assert.False(MineMovesFirst(Mon("Ditto", 50, "Tackle"), Mon("Charizard", 50, "Tackle")));
        Assert.True(MineMovesFirst(With(Mon("Ditto", 50, "Tackle"), item: "Quick Powder"), Mon("Charizard", 50, "Tackle")));
        Assert.False(MineMovesFirst(With(Mon("Machamp", 50, "Tackle"), item: "Quick Powder"), Mon("Charizard", 50, "Tackle")));
        Assert.True(MineMovesFirst(Mon("Machamp", 50, "Tackle"), With(Mon("Charizard", 50, "Tackle"), item: "Lagging Tail")));
        Assert.False(MineMovesFirst(With(Mon("Machamp", 50, "Tackle"), item: "Full Incense"), Mon("Charizard", 50, "Tackle")));
        Assert.True(MineMovesFirst(With(Mon("Machamp", 50, "Tackle"), item: "Lagging Tail"), With(Mon("Charizard", 50, "Tackle"), item: "Lagging Tail")));
    }

    [Fact]
    public void AQuickClawGoesOffWhenThePlacesNumberLeavesNothingOverFive()
    {
        // Snorlax (35) against Machamp (60): with the turn's number 0 the claw works and says nothing, as the
        // original's does; with 1 it doesn't
        var snorlax = With(Mon("Snorlax", 50, "Tackle"), item: "Quick Claw");
        Assert.True(MineMovesFirst(snorlax, Mon("Machamp", 50, "Tackle"), Calm().Force(RollKind.Speed, 0)));
        Assert.False(MineMovesFirst(With(Mon("Snorlax", 50, "Tackle"), item: "Quick Claw"), Mon("Machamp", 50, "Tackle"), Calm().Force(RollKind.Speed, 1)));
        Assert.NotNull(snorlax.HeldItem);
    }

    [Fact]
    public void ACustapBerryLetsItsHolderMoveFirstInAPinchAndIsEaten()
    {
        // Snorlax at 50 of 220 HP (a quarter is 55) with a Custap Berry moves before Machamp, says so, and the berry is gone
        var snorlax = With(Mon("Snorlax", 50, "Tackle"), item: "Custap Berry");
        snorlax.CurrentHP = 50;
        var said = Turn(Wild(snorlax, Mon("Machamp", 50, "Tackle")));
        InOrder(said, "Snorlax's Custap Berry let it move first!", "Snorlax used Tackle!", "Foe Machamp used Tackle!");
        Assert.Null(snorlax.HeldItem);
        // At 60 it stays where its Speed puts it
        snorlax = With(Mon("Snorlax", 50, "Tackle"), item: "Custap Berry");
        snorlax.CurrentHP = 60;
        Assert.False(MineMovesFirst(snorlax, Mon("Machamp", 50, "Tackle")));
        Assert.NotNull(snorlax.HeldItem);
    }

    // ================================================================== accuracy

    [Fact]
    public void BrightPowderTakesATenthOffAndAWideLensAddsATenth()
    {
        // Cross Chop's 80: with the target's Bright Powder 80 × 90 / 100 = 72, so a roll of 75 misses; with the
        // user's Wide Lens 80 × 110 / 100 = 88, so a roll of 85 hits
        var at75 = Calm().Force(RollKind.Accuracy, 75);
        Assert.Contains("Machamp's attack missed!", Turn(Wild(Mon("Machamp", 50, "Cross Chop"), With(Mon("Blastoise", 50), item: "Bright Powder"), at75)));
        Assert.DoesNotContain("Machamp's attack missed!", Turn(Wild(Mon("Machamp", 50, "Cross Chop"), Mon("Blastoise", 50), Calm().Force(RollKind.Accuracy, 75))));
        var at85 = Calm().Force(RollKind.Accuracy, 85);
        Assert.Contains("Machamp's attack missed!", Turn(Wild(Mon("Machamp", 50, "Cross Chop"), Mon("Blastoise", 50), at85)));
        Assert.DoesNotContain("Machamp's attack missed!", Turn(Wild(With(Mon("Machamp", 50, "Cross Chop"), item: "Wide Lens"), Mon("Blastoise", 50), Calm().Force(RollKind.Accuracy, 85))));
    }

    [Fact]
    public void AZoomLensWorksAgainstATargetThatHasAlreadyMoved()
    {
        // Rock Slide's 90 with a roll of 95 misses; a Zoom Lens makes it 90 × 120 / 100 = 108 for Snorlax (35),
        // which moves after Machamp (60), and nothing for Charizard (105), which moves before it
        var at95 = () => Calm().Force(RollKind.Accuracy, 95);
        Assert.Contains("Snorlax's attack missed!", Turn(Wild(Mon("Snorlax", 50, "Rock Slide"), Mon("Machamp", 50, "Tackle"), at95())));
        Assert.DoesNotContain("Snorlax's attack missed!", Turn(Wild(With(Mon("Snorlax", 50, "Rock Slide"), item: "Zoom Lens"), Mon("Machamp", 50, "Tackle"), at95())));
        Assert.Contains("Charizard's attack missed!", Turn(Wild(With(Mon("Charizard", 50, "Rock Slide"), item: "Zoom Lens"), Mon("Machamp", 50, "Tackle"), at95())));
    }

    [Fact]
    public void AMicleBerryIsEatenInAPinchAndIsGoodForOneMove()
    {
        // Snorlax at 50 of 220 HP eats it as it comes in; the next Rock Slide is 90 × 120 / 100 = 108 and a roll
        // of 95 hits; the one after is 90 again and misses
        var snorlax = With(Mon("Snorlax", 50, "Rock Slide"), item: "Micle Berry");
        snorlax.CurrentHP = 50;
        var core = Wild(snorlax, Mon("Blastoise", 50), Calm().Force(RollKind.Accuracy, 95));
        Assert.Contains("Snorlax boosted the accuracy of its next move using its Micle Berry!", Lines(core.TakeLog()));
        Assert.Null(snorlax.HeldItem);
        Assert.DoesNotContain("Snorlax's attack missed!", Turn(core));
        Assert.Contains("Snorlax's attack missed!", Turn(core));
    }

    // ================================================================== berries and herbs

    [Fact]
    public void ASitrusBerryGivesAQuarterBackAtHalfHpAndAnOranTen()
    {
        // Machamp's Cross Chop (100) on Blastoise: 135 × 100 × 22 / 105 / 50 = 56, + 2 = 58, 87 for its own type:
        // 139 - 87 = 52, under half (69). A Sitrus Berry gives 139 × 25 / 100 = 34 back, 86; an Oran Berry 10, 62
        var blastoise = With(Mon("Blastoise", 50), item: "Sitrus Berry");
        var said = Turn(Wild(Mon("Machamp", 50, "Cross Chop"), blastoise));
        Assert.Contains("Foe Blastoise restored its health using its Sitrus Berry!", said);
        Assert.Equal(86, blastoise.CurrentHP);
        Assert.Null(blastoise.HeldItem);
        blastoise = With(Mon("Blastoise", 50), item: "Oran Berry");
        Turn(Wild(Mon("Machamp", 50, "Cross Chop"), blastoise));
        Assert.Equal(62, blastoise.CurrentHP);
        // Tackle's 21 leaves it well over half: the berry stays
        blastoise = With(Mon("Blastoise", 50), item: "Sitrus Berry");
        Turn(Wild(Mon("Machamp", 50, "Tackle"), blastoise));
        Assert.NotNull(blastoise.HeldItem);
    }

    [Fact]
    public void AFlavourBerryHealsAnEighthAndConfusesANatureThatDislikesIt()
    {
        // The Figy Berry (spicy) at half HP: 139 / 8 = 17 back, 52 + 17 = 69. A Hardy Blastoise likes everything;
        // a Modest one (Attack lowered) dislikes spicy food and is confused by it
        var blastoise = With(Mon("Blastoise", 50), item: "Figy Berry");
        var said = Turn(Wild(Mon("Machamp", 50, "Cross Chop"), blastoise));
        Assert.Equal(69, blastoise.CurrentHP);
        Assert.DoesNotContain(said, s => s.Contains("was too spicy"));
        blastoise = With(Natured("Blastoise", Nature.Modest), item: "Figy Berry");
        var core = Wild(Mon("Machamp", 50, "Cross Chop"), blastoise);
        said = Turn(core);
        InOrder(said, "Foe Blastoise restored its health using its Figy Berry!", "For Foe Blastoise, the Figy Berry was too spicy!", "Foe Blastoise became confused!");
        Assert.True(core.At(Foe).IsConfused);
        // A Modest nature is fine with a dry berry (Wiki), which an Adamant one dislikes
        Assert.Equal(Flavor.Dry, HeldItemEffects.DislikedBy(Nature.Adamant));
        Assert.Null(HeldItemEffects.DislikedBy(Nature.Hardy));
    }

    [Fact]
    public void APinchBerryRaisesItsStatAtAQuarterOrAtHalfWithGluttony()
    {
        // Blastoise at 100 HP takes Cross Chop's 87: 13, under a quarter (34). The Liechi Berry raises its Attack.
        // From 139 it is left at 52: over a quarter, so the berry stays, unless Gluttony halves the parameter to
        // 2 and half (69) is enough
        var blastoise = With(Mon("Blastoise", 50), item: "Liechi Berry");
        blastoise.CurrentHP = 100;
        var said = Turn(Wild(Mon("Machamp", 50, "Cross Chop"), blastoise));
        InOrder(said, "Foe Blastoise ate its Liechi Berry!", "Foe Blastoise's Attack rose!");
        Assert.Equal(1, blastoise.StatStages[StatType.Attack]);
        Assert.Null(blastoise.HeldItem);

        blastoise = With(Mon("Blastoise", 50), item: "Liechi Berry");
        Turn(Wild(Mon("Machamp", 50, "Cross Chop"), blastoise));
        Assert.NotNull(blastoise.HeldItem);
        blastoise = With(Mon("Blastoise", 50), ability: "Gluttony", item: "Liechi Berry");
        Turn(Wild(Mon("Machamp", 50, "Cross Chop"), blastoise));
        Assert.Null(blastoise.HeldItem);
        Assert.Equal(1, blastoise.StatStages[StatType.Attack]);
    }

    [Fact]
    public void LansatGivesTheFocusAndStarfRaisesOneStatSharply()
    {
        var blastoise = With(Mon("Blastoise", 50), item: "Lansat Berry");
        blastoise.CurrentHP = 100;
        var core = Wild(Mon("Machamp", 50, "Cross Chop"), blastoise);
        Assert.Contains("Foe Blastoise used its Lansat Berry to get pumped!", Turn(core));
        Assert.True(core.At(Foe).Volatile.FocusEnergy);
        Assert.Null(blastoise.HeldItem);

        // The Starf Berry draws among the five stats that can still rise: the first of them with the die at 0
        blastoise = With(Mon("Blastoise", 50), item: "Starf Berry");
        blastoise.CurrentHP = 100;
        var said = Turn(Wild(Mon("Machamp", 50, "Cross Chop"), blastoise, Calm().Force(RollKind.Pick, 0)));
        InOrder(said, "Foe Blastoise ate its Starf Berry!", "Foe Blastoise's Attack sharply rose!");
        Assert.Equal(2, blastoise.StatStages[StatType.Attack]);
    }

    [Fact]
    public void AWhiteHerbPutsLoweredStatsBackAndAMentalHerbEndsALove()
    {
        var blastoise = With(Mon("Blastoise", 50), item: "White Herb");
        var said = Turn(Wild(Mon("Machamp", 50, "Growl"), blastoise));
        InOrder(said, "Foe Blastoise's Attack fell!", "Foe Blastoise returned its stats to normal using its White Herb!");
        Assert.Equal(0, blastoise.StatStages.GetValueOrDefault(StatType.Attack));
        Assert.Null(blastoise.HeldItem);

        var female = With(Of(Gender.Female, "Blastoise", 50), item: "Mental Herb");
        var core = Wild(Mon("Machamp", 50, "Attract"), female);
        said = Turn(core);
        InOrder(said, "Foe Blastoise fell in love!", "Foe Blastoise cured its infatuation using its Mental Herb!");
        Assert.Null(core.At(Foe).Volatile.InLoveWith);
        Assert.Null(female.HeldItem);
    }

    [Fact]
    public void ALeppaBerryGivesTenPpBackToAMoveThatHasNone()
    {
        var machamp = With(Mon("Machamp", 50, "Tackle"), item: "Leppa Berry");
        machamp.Moves[0].CurrentPP = 1;
        var said = Turn(Wild(machamp, Mon("Blastoise", 50)));
        Assert.Contains("Machamp's Leppa Berry restored Tackle's PP!", said);
        Assert.Equal(10, machamp.Moves[0].CurrentPP);
        Assert.Null(machamp.HeldItem);
    }

    // ================================================================== a berry against the type

    [Fact]
    public void AResistBerryHalvesASuperEffectiveHitOfItsTypeAndIsEaten()
    {
        // Machamp's Karate Chop on Snorlax is 132 (half as much again for its own type, twice against Normal); a
        // Chople Berry halves it as it lands, 66. The Chilan Berry halves any Normal move: Tackle's 21 on Blastoise
        // becomes 10. A Chople Berry against Tackle does nothing
        var snorlax = With(Mon("Snorlax", 50), item: "Chople Berry");
        var said = Turn(Wild(Mon("Machamp", 50, "Karate Chop"), snorlax));
        Assert.Contains("The Chople Berry weakened Karate Chop's power!", said);
        Assert.Equal(220 - 66, snorlax.CurrentHP);
        Assert.Null(snorlax.HeldItem);

        var blastoise = With(Mon("Blastoise", 50), item: "Chilan Berry");
        Turn(Wild(Mon("Machamp", 50, "Tackle"), blastoise));
        Assert.Equal(139 - 10, blastoise.CurrentHP);
        Assert.Null(blastoise.HeldItem);

        snorlax = With(Mon("Snorlax", 50), item: "Chople Berry");
        Turn(Wild(Mon("Machamp", 50, "Tackle"), snorlax));
        Assert.NotNull(snorlax.HeldItem);
        Assert.Equal(PokemonType.Fighting, HeldItemEffects.TypeWeakenedBy("WeakenSeFight"));
        Assert.Equal(PokemonType.Normal, HeldItemEffects.TypeWeakenedBy("WeakenNormal"));
        Assert.Null(HeldItemEffects.TypeWeakenedBy("HpRestore"));
    }

    // ================================================================== the items that answer a hit

    [Fact]
    public void JabocaAndRowapBerriesCostTheAttackerAnEighth()
    {
        // Machamp's Tackle on a Blastoise with a Jaboca Berry costs it 150 / 8 = 18; Charizard's Flamethrower on
        // a Snorlax with a Rowap Berry costs it 138 / 8 = 17. Not the other way round, and not past Magic Guard
        var machamp = Mon("Machamp", 50, "Tackle");
        var blastoise = With(Mon("Blastoise", 50), item: "Jaboca Berry");
        var said = Turn(Wild(machamp, blastoise));
        Assert.Contains("Machamp was hurt by Foe Blastoise's Jaboca Berry!", said);
        Assert.Equal(150 - 18, machamp.CurrentHP);
        Assert.Null(blastoise.HeldItem);

        var charizard = Mon("Charizard", 50, "Flamethrower");
        var snorlax = With(Mon("Snorlax", 50), item: "Rowap Berry");
        Turn(Wild(charizard, snorlax));
        Assert.Equal(138 - 17, charizard.CurrentHP);
        Assert.Null(snorlax.HeldItem);

        charizard = Mon("Charizard", 50, "Flamethrower");
        snorlax = With(Mon("Snorlax", 50), item: "Jaboca Berry");
        Turn(Wild(charizard, snorlax));
        Assert.Equal(138, charizard.CurrentHP);
        Assert.NotNull(snorlax.HeldItem);

        machamp = With(Mon("Machamp", 50, "Tackle"), ability: "Magic Guard");
        blastoise = With(Mon("Blastoise", 50), item: "Jaboca Berry");
        Turn(Wild(machamp, blastoise));
        Assert.Equal(150, machamp.CurrentHP);
        Assert.NotNull(blastoise.HeldItem);
    }

    [Fact]
    public void AnEnigmaBerryGivesAQuarterBackAfterASuperEffectiveHit()
    {
        // Pikachu's Thunderbolt takes 66 off Blastoise; the berry gives 139 / 4 = 34 back: 107
        var blastoise = With(Mon("Blastoise", 50), item: "Enigma Berry");
        var said = Turn(Wild(Mon("Pikachu", 50, "Thunderbolt"), blastoise));
        Assert.Contains("Foe Blastoise restored its health using its Enigma Berry!", said);
        Assert.Equal(139 - 66 + 34, blastoise.CurrentHP);
        Assert.Null(blastoise.HeldItem);
        blastoise = With(Mon("Blastoise", 50), item: "Enigma Berry");
        Turn(Wild(Mon("Machamp", 50, "Tackle"), blastoise));
        Assert.NotNull(blastoise.HeldItem);
    }

    [Fact]
    public void AStickyBarbMovesToWhoeverTouchesItsHolderAndHurtsAnEighthATurn()
    {
        var machamp = Mon("Machamp", 50, "Tackle");
        var blastoise = With(Mon("Blastoise", 50), item: "Sticky Barb");
        var said = Turn(Wild(machamp, blastoise));
        InOrder(said, "Foe Blastoise's Sticky Barb was transferred to Machamp!", "Machamp is hurt by its Sticky Barb!");
        Assert.Equal("Sticky Barb", machamp.HeldItem?.Name);
        Assert.Null(blastoise.HeldItem);
        Assert.Equal(150 - 18, machamp.CurrentHP);
        Assert.Equal(139 - 21, blastoise.CurrentHP);
        // It stays with a holder that already holds something, and a move that doesn't touch moves nothing
        var charizard = With(Mon("Charizard", 50, "Flamethrower"), item: "Charcoal");
        var snorlax = With(Mon("Snorlax", 50), item: "Sticky Barb");
        said = Turn(Wild(charizard, snorlax));
        Assert.Contains("Foe Snorlax is hurt by its Sticky Barb!", said);
        Assert.Equal("Sticky Barb", snorlax.HeldItem?.Name);
    }

    [Fact]
    public void AShellBellGivesAnEighthOfTheDamageBack()
    {
        // Tackle's 21 on Blastoise: 21 / 8 = 2 back
        var machamp = With(Mon("Machamp", 50, "Tackle"), item: "Shell Bell");
        machamp.CurrentHP = 100;
        var said = Turn(Wild(machamp, Mon("Blastoise", 50)));
        Assert.Contains("Machamp restored a little HP using its Shell Bell!", said);
        Assert.Equal(102, machamp.CurrentHP);
    }

    [Fact]
    public void AKingsRockFlinchesOneTimeInTenWithAMoveThatCanCarryIt()
    {
        // Machamp (60) moves before Snorlax (35), so Snorlax has a move left to lose: the die at 5 flinches it,
        // at 15 not; Rock Slide hasn't the table's flag and never does
        var at5 = Calm().Force(RollKind.ItemChance, 5);
        Assert.Contains("Foe Snorlax flinched!", Turn(Wild(With(Mon("Machamp", 50, "Tackle"), item: "King's Rock"), Mon("Snorlax", 50, "Tackle"), at5)));
        Assert.DoesNotContain("Foe Snorlax flinched!", Turn(Wild(With(Mon("Machamp", 50, "Tackle"), item: "King's Rock"), Mon("Snorlax", 50, "Tackle"), Calm().Force(RollKind.ItemChance, 15))));
        Assert.DoesNotContain("Foe Snorlax flinched!", Turn(Wild(With(Mon("Machamp", 50, "Rock Slide"), item: "Razor Fang"), Mon("Snorlax", 50, "Tackle"), Calm().Force(RollKind.ItemChance, 5))));
    }

    [Fact]
    public void ADestinyKnotMakesWhoeverInfatuatedItsHolderFallInLoveToo()
    {
        var female = With(Of(Gender.Female, "Blastoise", 50), item: "Destiny Knot");
        var core = Wild(Mon("Machamp", 50, "Attract"), female);
        var said = Turn(core);
        InOrder(said, "Foe Blastoise fell in love!", "Foe Blastoise's Destiny Knot made Machamp fall in love!");
        Assert.Equal(Foe, core.At(Mine).Volatile.InLoveWith);
        Assert.Equal(Mine, core.At(Foe).Volatile.InLoveWith);
        Assert.NotNull(female.HeldItem);
    }

    // ================================================================== the prize and the field

    [Fact]
    public void AnAmuletCoinDoublesThePrize()
    {
        var said = Lines(Beat(Mon("Machamp", 50, "Karate Chop")));
        Assert.Contains("Lucas received $100 for winning!", said);
        said = Lines(Beat(With(Mon("Machamp", 50, "Karate Chop"), item: "Amulet Coin")));
        Assert.Contains("Lucas received $200 for winning!", said);
        said = Lines(Beat(With(Mon("Machamp", 50, "Karate Chop"), item: "Luck Incense")));
        Assert.Contains("Lucas received $200 for winning!", said);

        static IEnumerable<BattleEvent> Beat(Pokemon mine)
        {
            var core = Against(new[] { mine }, new[] { Mon("Magikarp", 5) });
            core.TakeLog();
            core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
            Assert.Equal(100 * (mine.HeldItem == null ? 1 : 2), core.PrizeMoney);
            return core.TakeLog();
        }
    }

    [Fact]
    public void ACleanseTagOnTheLeadCutsTheEncounterRateToTwoThirds()
    {
        // ModifyEncounterRateWithHeldItem, after the ability's say: 30 × 2 / 3 = 20; nothing for a Pure Incense's cousin the Sea Incense
        Assert.Equal(30, WildEncounterRules.Rate(30, new WildLead(null, 5, Nature.Hardy, Gender.Male), FieldWeather.Clear));
        Assert.Equal(20, WildEncounterRules.Rate(30, new WildLead(null, 5, Nature.Hardy, Gender.Male, "EncountersDown"), FieldWeather.Clear));
        Assert.Equal(30, WildEncounterRules.Rate(30, new WildLead(null, 5, Nature.Hardy, Gender.Male, "StrengthenWater"), FieldWeather.Clear));
        var party = new Party();
        party.Add(With(Mon("Machamp", 50), item: "Pure Incense"));
        Assert.Equal("EncountersDown", WildLead.Of(party)!.Value.HoldEffect);
    }

    // ================================================================== the bag

    private static List<string> Use(BattleCore core, string item, int onPartyMember = -1, int onMove = -1)
    {
        core.Submit(BattleChoice.UseItem(Mine, item, onPartyMember, onMove), BattleChoice.Fight(Foe, 0));
        return Lines(core.TakeLog());
    }

    [Fact]
    public void MedicineFromTheBagRestoresHpAndCuresAConditionInBattle()
    {
        var machamp = Mon("Machamp", 50, "Tackle");
        machamp.CurrentHP = 100;
        var core = Wild(machamp, Mon("Blastoise", 50));
        var said = Use(core, "Potion");
        InOrder(said, "Lucas used the Potion!", "Machamp recovered 20 HP!", "Foe Blastoise used Idle!");
        Assert.Equal(120, machamp.CurrentHP);
        // A Hyper Potion's 200, which the table keeps as a signed byte
        machamp.CurrentHP = 10;
        Use(core, "Hyper Potion");
        Assert.Equal(150, machamp.CurrentHP);

        machamp.CurrentHP = 10;
        machamp.Status = StatusCondition.Paralyze;
        said = Use(core, "Full Restore");
        InOrder(said, "Lucas used the Full Restore!", "Machamp recovered 140 HP!", "Machamp was cured of paralysis!");
        Assert.Equal(150, machamp.CurrentHP);
        Assert.Equal(StatusCondition.None, machamp.Status);

        machamp.Status = StatusCondition.Burn;
        Assert.Equal("It won't have any effect!", core.WhyNot(BattleChoice.UseItem(Mine, "Antidote")));
        said = Use(core, "Burn Heal");
        Assert.Contains("Machamp was cured of burn!", said);
        Assert.Equal(StatusCondition.None, machamp.Status);
    }

    [Fact]
    public void ARevivesABenchedPokemonAndAnEtherRestoresAMovesPp()
    {
        var snorlax = Mon("Snorlax", 50);
        snorlax.CurrentHP = 0;
        snorlax.Status = StatusCondition.Faint;
        var machamp = Mon("Machamp", 50, "Tackle");
        machamp.Moves[0].CurrentPP = 0;
        var core = Wild(machamp, Mon("Blastoise", 50), bench: new[] { snorlax });
        var said = Use(core, "Revive", onPartyMember: 1);
        InOrder(said, "Lucas used the Revive!", "Snorlax was revived!");
        Assert.Equal(110, snorlax.CurrentHP);
        Assert.Equal(StatusCondition.None, snorlax.Status);

        said = Use(core, "Ether", onMove: 0);
        Assert.Contains("Machamp's Tackle had its PP restored!", said);
        Assert.Equal(10, machamp.Moves[0].CurrentPP);
        machamp.Moves[0].CurrentPP = 3;
        Use(core, "Max Elixir");
        Assert.Equal(35, machamp.Moves[0].CurrentPP);
    }

    [Fact]
    public void BattleItemsRaiseAStatGiveTheFocusOrPutUpAMist()
    {
        var core = Wild(Mon("Machamp", 50, "Tackle"), Mon("Blastoise", 50));
        Assert.Contains("Machamp's Attack rose!", Use(core, "X Attack"));
        Assert.Equal(1, core.At(Mine).Pokemon!.StatStages[StatType.Attack]);
        Assert.Contains("Machamp's Speed rose!", Use(core, "X Speed"));
        Assert.Contains("Machamp is getting pumped!", Use(core, "Dire Hit"));
        Assert.True(core.At(Mine).Volatile.FocusEnergy);
        Assert.Contains("Your team became shrouded in mist!", Use(core, "Guard Spec."));
        Assert.True(core.Field.Side(BattleSide.Player).Mist);
    }

    [Fact]
    public void APokeDollEndsAWildBattleAndTheBagRefusesWhatWouldDoNothing()
    {
        var core = Wild(Mon("Machamp", 50, "Tackle"), Mon("Blastoise", 50));
        Assert.Equal("It won't have any effect!", core.WhyNot(BattleChoice.UseItem(Mine, "Potion")));
        Assert.Equal("It won't have any effect!", core.WhyNot(BattleChoice.UseItem(Mine, "Antidote")));
        Assert.Equal("It won't have any effect!", core.WhyNot(BattleChoice.UseItem(Mine, "Revive")));
        Assert.Equal("There's a time and place for everything, but not now.", core.WhyNot(BattleChoice.UseItem(Mine, "Rare Candy")));
        Assert.Null(core.WhyNot(BattleChoice.UseItem(Mine, "X Attack")));
        Assert.Null(core.WhyNot(BattleChoice.UseItem(Mine, "Poké Doll")));
        var said = Use(core, "Poké Doll");
        InOrder(said, "Lucas used the Poké Doll!", "Got away safely!");
        Assert.Equal(BattleResult.PlayerRan, core.Result);

        var trainer = Against(new[] { Mon("Machamp", 50, "Tackle") }, new[] { Mon("Blastoise", 50) });
        Assert.Equal("It can't be used in a Trainer battle!", trainer.WhyNot(BattleChoice.UseItem(Mine, "Fluffy Tail")));
        Assert.Equal("The Trainer blocked the Ball! Don't be a thief!", trainer.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
    }

    // ================================================================== the table

    [Fact]
    public void EveryHoldEffectOfPlatinumsItemsHasItsCode()
    {
        var held = ItemDatabase.GetAll().Where(i => i.Id < 1000 && i.HoldEffect != null).ToList();
        Assert.Equal(158, held.Count);
        Assert.All(held, i => Assert.True(HeldItemEffects.IsHoldable(i), i.Name));
        Assert.True(BattleCore.CanUseInBattle(ItemDatabase.Get("Full Heal")!));
        Assert.True(BattleCore.CanUseInBattle(ItemDatabase.Get("Blue Flute")!));
        Assert.False(BattleCore.CanUseInBattle(ItemDatabase.Get("Protein")!));
        Assert.False(BattleCore.CanUseInBattle(ItemDatabase.Get("TM01")!));
        Assert.Equal(PokemonType.Fighting, HeldItemEffects.TypeBoostedBy("StrengthenFight"));
        Assert.Equal(PokemonType.Fighting, HeldItemEffects.TypeBoostedBy("ArceusFighting"));
        Assert.True(HeldItemEffects.HalvesSpeed("LvlupSpeedEvUp"));
        Assert.False(HeldItemEffects.HalvesSpeed("ChoiceSpeed"));
    }
}
