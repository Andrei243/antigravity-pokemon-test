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
/// The moves of their own of plan 06 · R5, on the battle's rules alone: the moves that call or copy another, that
/// take, swap, throw or eat an item, that change a type or an ability, that swap stats or stat changes, and the
/// few left over. One rule to a test, the rolls it hangs on fixed by kind, and the numbers worked out from the
/// original's code in a comment. The stats are as in <see cref="BattleFieldTests"/> and <see cref="BattleFamilyTests"/>;
/// also used here, at level 50 with no IVs:
/// <code>
///            HP   Atk  Def  SpA  SpD  Spe
/// Ditto      108   53   53   53   53   53   Normal
/// Chatot     136   70   50   97   47   96   Normal / Flying
/// </code>
/// </summary>
public class BattleUniqueTests
{
    private static List<MoveShown> Shown(BattleCore core) => core.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<MoveShown>().ToList();

    // ================================================================== calling and copying moves

    [Fact]
    public void MetronomeCallsAMoveItsUserDoesntKnow()
    {
        // BtlCmd_Metronome draws a move of the 467 until one comes up that the user doesn't know and may be called: the
        // 33rd is Tackle (135 × 35 × 22 / 105 / 50 = 19, + 2 = 21), the 118th Metronome itself, which is passed over
        var blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(Mon("Machamp", 50, "Metronome"), blastoise, Calm().Force(RollKind.Pick, 117, 32)));
        InOrder(said, "Machamp used Metronome!", "Machamp used Tackle!");
        Assert.Equal(139 - 21, blastoise.CurrentHP);

        // A move it knows is passed over too: the 98th is Quick Attack (135 × 40 × 22 / 105 / 50 = 22, + 2 = 24)
        blastoise = Mon("Blastoise", 50);
        Assert.Contains("Machamp used Quick Attack!", Turn(Wild(Mon("Machamp", 50, "Metronome", "Tackle"), blastoise, Calm().Force(RollKind.Pick, 32, 97))));
        Assert.Equal(139 - 24, blastoise.CurrentHP);
    }

    [Fact]
    public void ACalledMoveThatLeavesTheFieldStillDoes()
    {
        // The 369th move is U-turn (70 power: 135 × 70 × 22 / 105 / 50 = 39, + 2 = 41): called by Metronome, it still asks for a replacement
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(Mon("Machamp", 50, "Metronome"), blastoise, Calm().Force(RollKind.Pick, 368), bench: Mon("Snorlax", 50));
        InOrder(Turn(core), "Machamp used Metronome!", "Machamp used U-turn!");
        Assert.Equal(139 - 41, blastoise.CurrentHP);
        Assert.IsType<ReplacementRequest>(core.Request);
    }

    [Fact]
    public void MirrorMoveSendsBackTheLastMoveAimedAtItsUser()
    {
        // Blastoise is the faster: on the first turn nothing has been aimed at it yet; on the second it sends Tackle
        // back (88 × 35 × 22 / 85 / 50 = 15, + 2 = 17)
        var machamp = Mon("Machamp", 50, "Tackle");
        var core = Wild(machamp, Mon("Blastoise", 50, "Mirror Move"));
        InOrder(Turn(core), "Foe Blastoise used Mirror Move!", "But it failed!", "Machamp used Tackle!");
        var said = Turn(core);
        InOrder(said, "Foe Blastoise used Mirror Move!", "Foe Blastoise used Tackle!");
        Assert.Equal(150 - 17, machamp.CurrentHP);
    }

    [Fact]
    public void SleepTalkUsesOneOfItsUsersOwnMovesWhileItSleeps()
    {
        // Of Sleep Talk, Tackle, Fly and Focus Punch, only Tackle can be talked in one's sleep (115 × 35 × 22 / 105 / 50 = 16,
        // + 2 = 18, × 15 / 10 = 27)
        var snorlax = Mon("Snorlax", 50, "Sleep Talk", "Tackle", "Fly", "Focus Punch");
        snorlax.Status = StatusCondition.Sleep;
        snorlax.SleepTurns = 5;
        var blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(snorlax, blastoise));
        InOrder(said, "Snorlax is fast asleep.", "Snorlax used Sleep Talk!", "Snorlax used Tackle!");
        Assert.Equal(139 - 27, blastoise.CurrentHP);

        // Awake, it has nothing to say
        Assert.Contains("But it failed!", Turn(Wild(Mon("Snorlax", 50, "Sleep Talk", "Tackle"), Mon("Blastoise", 50))));
    }

    [Fact]
    public void CopycatRepeatsTheLastMoveAnyoneUsed()
    {
        // Machamp is the faster: nothing has been used on the first turn; on the second it copies Snorlax's Tackle
        // (135 × 35 × 22 / 70 / 50 = 29, + 2 = 31)
        var snorlax = Mon("Snorlax", 50, "Tackle");
        var core = Wild(Mon("Machamp", 50, "Copycat"), snorlax);
        InOrder(Turn(core), "Machamp used Copycat!", "But it failed!", "Foe Snorlax used Tackle!");
        int hp = snorlax.CurrentHP;
        InOrder(Turn(core), "Machamp used Copycat!", "Machamp used Tackle!");
        Assert.Equal(hp - 31, snorlax.CurrentHP);
    }

    [Fact]
    public void MeFirstStealsTheTargetsAttackAndHitsHarder()
    {
        // BtlCmd_TryMeFirst: the faster Machamp uses the Tackle Snorlax was about to, half as strong again before the
        // roll (135 × 35 × 22 / 70 / 50 = 29, + 2 = 31, × 15 / 10 = 46); Snorlax's own Tackle follows (115 × 35 × 22 / 85 / 50 = 20,
        // + 2 = 22, × 15 / 10 = 33)
        var machamp = Mon("Machamp", 50, "Me First");
        var snorlax = Mon("Snorlax", 50, "Tackle");
        var said = Turn(Wild(machamp, snorlax));
        InOrder(said, "Machamp used Me First!", "Machamp used Tackle!", "Foe Snorlax used Tackle!");
        Assert.Equal(220 - 46, snorlax.CurrentHP);
        Assert.Equal(150 - 33, machamp.CurrentHP);

        // Not a status move, and not a target that has already moved
        Assert.Contains("But it failed!", Turn(Wild(Mon("Machamp", 50, "Me First"), Mon("Snorlax", 50, "Growl"))));
        Assert.Contains("But it failed!", Turn(Wild(Mon("Snorlax", 50, "Me First"), Mon("Blastoise", 50, "Tackle"))));
    }

    [Fact]
    public void AssistCallsAMoveOfTheRestOfTheParty()
    {
        // Pikachu on the bench knows Thunderbolt and Quick Attack: the second is drawn (135 × 40 × 22 / 105 / 50 = 22, + 2 = 24)
        var blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(Mon("Machamp", 50, "Assist"), blastoise, Calm().Force(RollKind.Pick, 1), bench: Mon("Pikachu", 50, "Thunderbolt", "Quick Attack")));
        InOrder(said, "Machamp used Assist!", "Machamp used Quick Attack!");
        Assert.Equal(139 - 24, blastoise.CurrentHP);
        Assert.Contains("But it failed!", Turn(Wild(Mon("Machamp", 50, "Assist"), Mon("Blastoise", 50))));
    }

    [Fact]
    public void MimicTakesTheTargetsLastMoveUntilItsUserLeaves()
    {
        // Blastoise is the faster and has used Growl; Mimic's place holds it with 5 PP, and Mimic is back once Machamp has been out
        var machamp = Mon("Machamp", 50, "Mimic", "Tackle");
        var core = Wild(machamp, Mon("Blastoise", 50, "Growl"), bench: Mon("Snorlax", 50));
        Assert.Contains("Machamp learned Growl!", Turn(core));
        Assert.Equal(("Growl", 5), (machamp.Moves[0].Name, machamp.Moves[0].CurrentPP));
        SwitchTo(core, 1);
        SwitchTo(core, 0);
        Assert.Equal(("Mimic", 9), (machamp.Moves[0].Name, machamp.Moves[0].CurrentPP));

        // A target that hasn't used anything yet gives nothing to copy
        Assert.Contains("But it failed!", Turn(Wild(Mon("Machamp", 50, "Mimic"), Mon("Snorlax", 50, "Tackle"))));
    }

    [Fact]
    public void SketchLearnsTheTargetsLastMoveForGood()
    {
        var machamp = Mon("Machamp", 50, "Sketch", "Tackle");
        var core = Wild(machamp, Mon("Blastoise", 50, "Growl"), bench: Mon("Snorlax", 50));
        Assert.Contains("Machamp sketched Growl!", Turn(core));
        Assert.Equal(("Growl", 40), (machamp.Moves[0].Name, machamp.Moves[0].CurrentPP));
        SwitchTo(core, 1);
        SwitchTo(core, 0);
        Assert.Equal("Growl", machamp.Moves[0].Name);
    }

    [Fact]
    public void MagicCoatBouncesAStatusMoveBack()
    {
        // The faster Blastoise shrouds itself; Machamp's Thunder Wave comes back at Machamp
        var machamp = Mon("Machamp", 50, "Thunder Wave");
        var blastoise = Mon("Blastoise", 50, "Magic Coat");
        var said = Turn(Wild(machamp, blastoise));
        InOrder(said, "Foe Blastoise shrouded itself with Magic Coat!", "Machamp used Thunder Wave!", "Machamp's Thunder Wave was bounced back by Magic Coat!");
        Assert.Equal((StatusCondition.Paralyze, StatusCondition.None), (machamp.Status, blastoise.Status));

        // The last to act has nothing to shroud itself against: Magic Coat goes early (+4), so only another's comes before it
        Assert.Contains("But it failed!", Turn(Wild(Mon("Snorlax", 50, "Magic Coat"), Mon("Blastoise", 50, "Magic Coat"))));
    }

    [Fact]
    public void SnatchTakesAMoveItsUserWouldHaveUsedOnItself()
    {
        var machamp = Mon("Machamp", 50, "Bulk Up");
        var blastoise = Mon("Blastoise", 50, "Snatch");
        var said = Turn(Wild(machamp, blastoise));
        InOrder(said, "Foe Blastoise waits for a target to make a move!", "Machamp used Bulk Up!", "Foe Blastoise snatched Machamp's move!", "Foe Blastoise's Attack rose!");
        Assert.Equal((1, 1), (blastoise.StatStages[StatType.Attack], blastoise.StatStages[StatType.Defense]));
        Assert.Equal(0, machamp.StatStages[StatType.Attack]);
    }

    // ================================================================== items

    [Fact]
    public void ThiefTakesTheTargetsItemWhenItsUserHoldsNone()
    {
        // Thief, 40 power: 135 × 40 × 22 / 105 / 50 = 22, + 2 = 24
        var machamp = Mon("Machamp", 50, "Thief");
        var blastoise = With(Mon("Blastoise", 50), item: "Leftovers");
        Assert.Contains("Machamp stole Foe Blastoise's Leftovers!", Turn(Wild(machamp, blastoise)));
        Assert.Equal(139 - 24, blastoise.CurrentHP);
        Assert.Equal(("Leftovers", null), (machamp.HeldItem?.Name, blastoise.HeldItem?.Name));

        // Sticky Hold keeps it; a user with its hands full takes nothing
        Assert.Contains("Foe Blastoise's Sticky Hold made Thief ineffective!", Turn(Wild(Mon("Machamp", 50, "Thief"), With(Mon("Blastoise", 50), ability: "Sticky Hold", item: "Leftovers"))));
        blastoise = With(Mon("Blastoise", 50), item: "Leftovers");
        Turn(Wild(With(Mon("Machamp", 50, "Thief"), item: "Oran Berry"), blastoise));
        Assert.Equal("Leftovers", blastoise.HeldItem?.Name);

        // Under Platinum's rules the opponents' Pokémon can't take the player's items; by the modern ones they can
        machamp = With(Mon("Machamp", 50), item: "Leftovers");
        Turn(Wild(machamp, Mon("Blastoise", 50, "Thief")));
        Assert.Equal("Leftovers", machamp.HeldItem?.Name);
        machamp = With(Mon("Machamp", 50), item: "Leftovers");
        Turn(Wild(machamp, Mon("Blastoise", 50, "Thief"), rules: Ruleset.Modern));
        Assert.Null(machamp.HeldItem);
    }

    [Fact]
    public void KnockOffTakesTheItemForTheBattleOnly()
    {
        // Knock Off, 20 power: 135 × 20 × 22 / 105 / 50 = 11, + 2 = 13; the item is back with the battle's end
        var blastoise = With(Mon("Blastoise", 50), item: "Leftovers");
        var core = Wild(Mon("Machamp", 50, "Knock Off"), blastoise);
        Assert.Contains("Machamp knocked off Foe Blastoise's Leftovers!", Turn(core));
        Assert.Equal(139 - 13, blastoise.CurrentHP);
        Assert.Null(blastoise.HeldItem);
        blastoise.CurrentHP = 1;
        Turn(core);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Equal("Leftovers", blastoise.HeldItem?.Name);

        // By the modern rules it is half as strong again against an item it can knock off: 30 power (16, + 2 = 18)
        blastoise = With(Mon("Blastoise", 50), item: "Leftovers");
        Turn(Wild(Mon("Machamp", 50, "Knock Off"), blastoise, rules: Ruleset.Modern));
        Assert.Equal(139 - 18, blastoise.CurrentHP);
    }

    [Fact]
    public void TrickSwapsTheTwoItems()
    {
        var alakazam = With(Mon("Alakazam", 50, "Trick"), item: "Choice Specs");
        var blastoise = With(Mon("Blastoise", 50), item: "Leftovers");
        var said = Turn(Wild(alakazam, blastoise));
        InOrder(said, "Alakazam switched items with its target!", "Alakazam obtained one Leftovers.", "Foe Blastoise obtained one Choice Specs.");
        Assert.Equal(("Leftovers", "Choice Specs"), (alakazam.HeldItem?.Name, blastoise.HeldItem?.Name));

        // Nothing to swap, or mail: it fails; and the opponents' Pokémon can't under Platinum's rules
        Assert.Contains("But it failed!", Turn(Wild(Mon("Alakazam", 50, "Trick"), Mon("Blastoise", 50))));
        Assert.Contains("But it failed!", Turn(Wild(With(Mon("Alakazam", 50, "Trick"), item: "Air Mail"), With(Mon("Blastoise", 50), item: "Leftovers"))));
        var machamp = With(Mon("Machamp", 50), item: "Leftovers");
        Assert.Contains("But it failed!", Turn(Wild(machamp, Mon("Blastoise", 50, "Trick"))));
        Assert.Equal("Leftovers", machamp.HeldItem?.Name);
        Turn(Wild(machamp, Mon("Blastoise", 50, "Trick"), rules: Ruleset.Modern));
        Assert.Null(machamp.HeldItem);
    }

    [Fact]
    public void FlingThrowsTheItemAndRecycleBringsItBack()
    {
        // A Flame Orb flies at 30 power (115 × 30 × 22 / 105 / 50 = 14, + 2 = 16) and burns, which takes an eighth
        // (17) as the turn ends; the orb is gone, and Recycle finds it
        var snorlax = With(Mon("Snorlax", 50, "Fling", "Recycle"), item: "Flame Orb");
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(snorlax, blastoise);
        var said = Turn(core, 0);
        InOrder(said, "Snorlax used Fling!", "Snorlax flung its Flame Orb!");
        Assert.Equal(139 - 16 - 17, blastoise.CurrentHP);
        Assert.Equal(StatusCondition.Burn, blastoise.Status);
        Assert.Null(snorlax.HeldItem);
        Assert.Equal(StatusCondition.None, snorlax.Status);
        Assert.Contains("Snorlax found one Flame Orb!", Turn(core, 1));
        Assert.Equal("Flame Orb", snorlax.HeldItem?.Name);
        Assert.Contains("But it failed!", Turn(core, 1));

        // Nothing to throw
        Assert.Contains("But it failed!", Turn(Wild(Mon("Snorlax", 50, "Fling"), Mon("Blastoise", 50))));
    }

    [Fact]
    public void NaturalGiftTakesItsTypeAndPowerFromTheBerry()
    {
        // A Cheri Berry is a Fire move of 60 power: 115 × 60 × 22 / 85 / 50 = 35, + 2 = 37 on Machamp; the berry is gone
        var snorlax = With(Mon("Snorlax", 50, "Natural Gift"), item: "Cheri Berry");
        var machamp = Mon("Machamp", 50);
        var core = Wild(snorlax, machamp);
        Turn(core);
        Assert.Equal(150 - 37, machamp.CurrentHP);
        Assert.Contains(Shown(core), m => m.Move == "Natural Gift" && m.Type == PokemonType.Fire);
        Assert.Null(snorlax.HeldItem);
        Assert.Contains("But it failed!", Turn(core));
    }

    [Fact]
    public void PluckEatsTheTargetsBerry()
    {
        // Pluck, 60 power: 135 × 60 × 22 / 105 / 50 = 33, + 2 = 35; the Oran Berry gives its 10 HP to Machamp, and Blastoise can Recycle it
        var machamp = Mon("Machamp", 50, "Pluck");
        machamp.CurrentHP = 100;
        var blastoise = With(Mon("Blastoise", 50, "Recycle"), item: "Oran Berry");
        var core = Wild(machamp, blastoise);
        var said = Turn(core);
        InOrder(said, "Machamp stole and ate Foe Blastoise's Oran Berry!", "Machamp restored its health using its Oran Berry!");
        Assert.Equal(139 - 35, blastoise.CurrentHP);
        Assert.Equal(110, machamp.CurrentHP);
        Assert.Null(blastoise.HeldItem);
        Assert.Contains("Foe Blastoise found one Oran Berry!", Turn(core));
    }

    // ================================================================== types and abilities

    [Fact]
    public void ConversionTakesTheTypeOfOneOfItsUsersMoves()
    {
        // Of Thunderbolt and Surf only Surf's type is one Pikachu isn't
        var core = Wild(Mon("Pikachu", 50, "Conversion", "Thunderbolt", "Surf"), Mon("Blastoise", 50), Calm().Force(RollKind.Pick, 0));
        Assert.Contains("Pikachu transformed into the Water type!", Turn(core));
        Assert.True(core.At(Mine).HasType(PokemonType.Water));
        Assert.False(core.At(Mine).HasType(PokemonType.Electric));
        Assert.Contains("But it failed!", Turn(Wild(Mon("Pikachu", 50, "Conversion", "Thunderbolt"), Mon("Blastoise", 50))));
    }

    [Fact]
    public void Conversion2TakesATypeThatResistsTheLastHit()
    {
        // Pikachu is the faster: nothing has hit it on the first turn; after Water Gun, the types that resist Water in
        // the chart's order are Water, Grass and Dragon, and the second is drawn
        var core = Wild(Mon("Pikachu", 50, "Conversion 2"), Idling(Mon("Blastoise", 50, "Water Gun")), Calm().Force(RollKind.Pick, 1));
        Assert.Contains("But it failed!", Turn(core, 0, 0));
        Assert.Contains("Pikachu transformed into the Grass type!", Turn(core, 0, 1));
        Assert.True(core.At(Mine).HasType(PokemonType.Grass));
    }

    [Fact]
    public void TransformTakesTheTargetsShapeUntilItsUserLeaves()
    {
        // Ditto takes Machamp's species, stats, ability, types and moves (5 PP each) and keeps its own name and HP;
        // its Tackle is then Machamp's (135 × 35 × 22 / 85 / 50 = 24, + 2 = 26), and it is Ditto again once it has been out
        var ditto = Mon("Ditto", 50, "Transform");
        var machamp = Mon("Machamp", 50, "Tackle");
        var core = Wild(ditto, machamp, bench: Mon("Snorlax", 50));
        Assert.Contains("Ditto transformed into Foe Machamp!", Turn(core));
        Assert.Equal(("Machamp", "Ditto", 135, 85), (ditto.Species.Name, ditto.DisplayName, ditto.Attack, ditto.Defense));
        Assert.Equal(("Tackle", 5), (ditto.Moves.Single().Name, ditto.Moves.Single().CurrentPP));
        Assert.True(core.At(Mine).HasType(PokemonType.Fighting));
        Assert.Equal(108, ditto.MaxHP);
        Turn(core);
        Assert.Equal(150 - 26, machamp.CurrentHP);
        SwitchTo(core, 1);
        Assert.Equal(("Ditto", 53, "Transform", 9), (ditto.Species.Name, ditto.Attack, ditto.Moves.Single().Name, ditto.Moves.Single().CurrentPP));
    }

    [Fact]
    public void RolePlaySkillSwapAndWorrySeedChangeAbilitiesForTheBattle()
    {
        var machamp = With(Mon("Machamp", 50, "Role Play"), ability: "Guts");
        var core = Wild(machamp, With(Mon("Blastoise", 50), ability: "Torrent"), bench: Mon("Snorlax", 50));
        Assert.Contains("Machamp copied Foe Blastoise's Torrent!", Turn(core));
        Assert.Equal("Torrent", machamp.AbilityName);
        SwitchTo(core, 1);
        Assert.Equal("Guts", machamp.AbilityName);

        machamp = With(Mon("Machamp", 50, "Skill Swap"), ability: "Guts");
        var blastoise = With(Mon("Blastoise", 50), ability: "Torrent");
        Assert.Contains("Machamp swapped abilities with its target!", Turn(Wild(machamp, blastoise)));
        Assert.Equal(("Torrent", "Guts"), (machamp.AbilityName, blastoise.AbilityName));

        blastoise = With(Mon("Blastoise", 50), ability: "Torrent");
        Assert.Contains("Foe Blastoise acquired Insomnia!", Turn(Wild(Mon("Machamp", 50, "Worry Seed"), blastoise)));
        Assert.Equal("Insomnia", blastoise.AbilityName);
        Assert.Contains("But it failed!", Turn(Wild(Mon("Machamp", 50, "Worry Seed"), With(Mon("Blastoise", 50), ability: "Truant"))));
    }

    // ================================================================== stats and stat changes

    [Fact]
    public void PowerTrickSwapsAttackAndDefenseUntilItsUserLeaves()
    {
        // With its 70 Defense for an Attack, Snorlax's Tackle is 70 × 35 × 22 / 105 / 50 = 10, + 2 = 12, × 15 / 10 = 18
        var snorlax = Mon("Snorlax", 50, "Power Trick", "Tackle");
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(snorlax, blastoise, bench: Mon("Machamp", 50));
        Assert.Contains("Snorlax switched its Attack and Defense!", Turn(core, 0));
        Assert.Equal((70, 115), (snorlax.Attack, snorlax.Defense));
        Turn(core, 1);
        Assert.Equal(139 - 18, blastoise.CurrentHP);
        SwitchTo(core, 1);
        Assert.Equal((115, 70), (snorlax.Attack, snorlax.Defense));
    }

    [Fact]
    public void TheSwapsTradeStatChanges()
    {
        var machamp = Mon("Machamp", 50, "Power Swap", "Guard Swap", "Heart Swap");
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(machamp, blastoise);
        machamp.StatStages[StatType.Attack] = 2;
        blastoise.StatStages[StatType.SpAttack] = 1;
        Assert.Contains("Machamp switched all changes to its Attack and Sp. Atk with the target!", Turn(core, 0));
        Assert.Equal((0, 1, 2, 0), (machamp.StatStages[StatType.Attack], machamp.StatStages[StatType.SpAttack], blastoise.StatStages[StatType.Attack], blastoise.StatStages[StatType.SpAttack]));

        blastoise.StatStages[StatType.Defense] = -2;
        Assert.Contains("Machamp switched all changes to its Defense and Sp. Def with the target!", Turn(core, 1));
        Assert.Equal((-2, 0), (machamp.StatStages[StatType.Defense], blastoise.StatStages[StatType.Defense]));

        core.At(Foe).Volatile.FocusEnergy = true;
        Assert.Contains("Machamp switched stat changes with the target!", Turn(core, 2));
        Assert.Equal((2, 0, 0, -2), (machamp.StatStages[StatType.Attack], machamp.StatStages[StatType.Defense], blastoise.StatStages[StatType.Attack], blastoise.StatStages[StatType.Defense]));
        Assert.True(core.At(Mine).Volatile.FocusEnergy);
        Assert.False(core.At(Foe).Volatile.FocusEnergy);
    }

    // ================================================================== the rest

    [Fact]
    public void FollowMeDrawsTheFoesMovesInADoubleBattle()
    {
        // Snorlax draws Machamp's Tackle, aimed at Pikachu, to itself (135 × 35 × 22 / 70 / 50 = 29, + 2 = 31)
        var pikachu = Mon("Pikachu", 50);
        var snorlax = Mon("Snorlax", 50, "Follow Me");
        var core = Doubles(new[] { pikachu, snorlax }, new[] { Mon("Machamp", 50, "Tackle"), Mon("Blastoise", 50) });
        var said = DoubleTurn(core, (Mine, 0, null), (Mine2, 0, null), (Foe, 0, Mine), (Foe2, 0, null));
        InOrder(said, "Snorlax became the center of attention!", "Foe Machamp used Tackle!");
        Assert.Equal((95, 220 - 31), (pikachu.CurrentHP, snorlax.CurrentHP));
    }

    [Fact]
    public void HelpingHandMakesTheAllysMoveStronger()
    {
        // Machamp's Tackle on Blastoise is 21; with a Helping Hand its power is 52 (135 × 52 × 22 / 105 / 50 = 29, + 2 = 31)
        var blastoise = Mon("Blastoise", 50);
        var core = Doubles(new[] { Mon("Pikachu", 50, "Helping Hand"), Mon("Machamp", 50, "Tackle") }, new[] { blastoise, Mon("Snorlax", 50) });
        var said = DoubleTurn(core, (Mine, 0, Mine2), (Mine2, 0, Foe), (Foe, 0, null), (Foe2, 0, null));
        InOrder(said, "Pikachu is ready to help Machamp!", "Machamp used Tackle!");
        Assert.Equal(139 - 31, blastoise.CurrentHP);

        // Alone, there is nobody to help
        Assert.Contains("But there was no target...", Turn(Wild(Mon("Pikachu", 50, "Helping Hand"), Mon("Blastoise", 50))));
    }

    [Fact]
    public void SpiteTakesFourPpOffTheTargetsLastMove()
    {
        var blastoise = Mon("Blastoise", 50, "Tackle");
        var core = Wild(Mon("Snorlax", 50, "Spite"), blastoise);
        Assert.Contains("It reduced the PP of Foe Blastoise's Tackle by 4!", Turn(core));
        Assert.Equal(35 - 1 - 4, blastoise.Moves[0].CurrentPP);
        blastoise.Moves[0].CurrentPP = 3;
        Assert.Contains("It reduced the PP of Foe Blastoise's Tackle by 2!", Turn(core));
        Assert.Equal(0, blastoise.Moves[0].CurrentPP);

        // Nothing used yet
        Assert.Contains("But it failed!", Turn(Wild(Mon("Machamp", 50, "Spite"), Mon("Snorlax", 50, "Tackle"))));
    }

    [Fact]
    public void ChatterConfusesByTheRulesChance()
    {
        // Chatot's Chatter: 97 × 60 × 22 / 110 / 50 = 23, + 2 = 25, × 15 / 10 = 37; the original's roll of 100 confuses at or under
        // the chance (30 here, the loudest recording's); by the modern rules always. The confused Blastoise is kept from hurting itself
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(Mon("Chatot", 50, "Chatter"), blastoise, Calm().Force(RollKind.SideEffect, 30).Force(RollKind.ConfusionSelfHit, 1));
        Turn(core);
        Assert.Equal(139 - 37, blastoise.CurrentHP);
        Assert.True(core.At(Foe).IsConfused);
        core = Wild(Mon("Chatot", 50, "Chatter"), Mon("Blastoise", 50), Calm().Force(RollKind.SideEffect, 31));
        Turn(core);
        Assert.False(core.At(Foe).IsConfused);
        core = Wild(Mon("Chatot", 50, "Chatter"), Mon("Blastoise", 50), Calm().Force(RollKind.SideEffect, 99).Force(RollKind.ConfusionSelfHit, 1), Ruleset.Modern);
        Turn(core);
        Assert.True(core.At(Foe).IsConfused);
    }
}
