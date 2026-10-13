using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 14 · B1: inverse battles (<c>docs/mechanics/rulings.md</c>, "Formats of the later games"). The flag is one
/// of the battle's conditions (<see cref="BattleConditions.Inverse"/>), the chart is turned round in one place
/// (<see cref="TypeChart.GetEffectiveness"/>), and everything that asks how a type does against another follows:
/// the damage and the lines about it, the immunities of types, Stealth Rock, the berries against a type,
/// Anticipation, Wonder Guard, Thunder Wave, Conversion 2, the trainer AI and the move cards' hints. What an
/// ability or magnetism stops is still stopped. Without the flag nothing is different, which each test shows
/// beside what it shows of the flag. The numbers are worked out by hand from Platinum's formula
/// (<see cref="Formulas.BaseDamage"/>): at level 50 a stat is its base + 5 and HP its base + 60.
/// </summary>
public class InverseBattleTests
{
    private static BattleConditions Chart(bool inverse) => new() { Inverse = inverse };

    /// <summary>A wild battle on the rules alone, both sides answered for from outside, under the chart asked for.</summary>
    private static BattleCore Wild(bool inverse, Pokemon mine, Pokemon foe, BattleRandom? rolls = null, params Pokemon[] bench)
    {
        var party = new Party();
        party.Add(mine);
        foreach (var p in bench) party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = rolls ?? Calm(), Rules = Ruleset.Platinum,
            Conditions = Chart(inverse), PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    /// <summary>A battle against a trainer under the chart asked for, both sides answered for from outside.</summary>
    private static BattleCore Against(bool inverse, Pokemon mine, params Pokemon[] theirs)
    {
        var party = new Party();
        party.Add(mine);
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100 };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = Ruleset.Platinum,
            Conditions = Chart(inverse), PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    // ================================================================== the chart

    [Fact]
    public void TheChartIsTurnedRoundTypeByTypeAndOnlyWhenAsked()
    {
        var types = Enum.GetValues<PokemonType>();
        foreach (var rules in new[] { Ruleset.Platinum, Ruleset.Modern })
        {
            foreach (var attack in types)
            {
                foreach (var defend in types)
                {
                    float plain = TypeChart.GetEffectiveness(attack, defend, null, rules);
                    // Left out, the flag is off: every caller from before gets what it got
                    Assert.Equal(plain, TypeChart.GetEffectiveness(attack, defend, null, rules, inverse: false));
                    // Twice becomes half, half becomes twice, nothing becomes twice, and the rest stays
                    float turned = plain > 1f ? 0.5f : plain < 1f ? 2f : 1f;
                    Assert.Equal(turned, TypeChart.GetEffectiveness(attack, defend, null, rules, inverse: true));
                }
            }
        }

        // Two types: each is turned round before the two are multiplied. Ground against Steel and Flying is nothing
        // in Platinum (2 × 0) and even in an inverse battle (½ × 2), not the "twice" of nothing turned round whole
        Assert.Equal(0f, TypeChart.GetEffectiveness(PokemonType.Ground, PokemonType.Steel, PokemonType.Flying, Ruleset.Platinum));
        Assert.Equal(1f, TypeChart.GetEffectiveness(PokemonType.Ground, PokemonType.Steel, PokemonType.Flying, Ruleset.Platinum, inverse: true));
        // Rock against Fire and Flying: four times, and a quarter
        Assert.Equal(4f, TypeChart.GetEffectiveness(PokemonType.Rock, PokemonType.Fire, PokemonType.Flying, Ruleset.Platinum));
        Assert.Equal(0.25f, TypeChart.GetEffectiveness(PokemonType.Rock, PokemonType.Fire, PokemonType.Flying, Ruleset.Platinum, inverse: true));
        // Fighting against Ghost and Poison: nothing, and four times (the immunity and the resistance both turned)
        Assert.Equal(4f, TypeChart.GetEffectiveness(PokemonType.Fighting, PokemonType.Ghost, PokemonType.Poison, Ruleset.Platinum, inverse: true));
    }

    [Fact]
    public void TheFlagComesWithTheBattlesConditionsAndIsSaidAsItOpens()
    {
        var inverse = Wild(true, Mon("Blastoise", 50), Mon("Snorlax", 50));
        Assert.True(inverse.Inverse);
        Assert.True(inverse.Field.Inverse);
        // After the send-outs, before anything else
        InOrder(Lines(inverse.Log), "A wild Snorlax appeared!", "Go! Blastoise!", BattleCore.InverseLine);
        Assert.Single(Lines(inverse.Log), line => line == BattleCore.InverseLine);

        var plain = Wild(false, Mon("Blastoise", 50), Mon("Snorlax", 50));
        Assert.False(plain.Inverse);
        Assert.False(plain.Field.Inverse);
        Assert.DoesNotContain(BattleCore.InverseLine, Lines(plain.Log));
        // A battle given no conditions at all is a plain one
        Assert.False(CoreScenario.Wild(Mon("Blastoise", 50), Mon("Snorlax", 50)).Inverse);
        Assert.False(new BattleConditions().Inverse);
    }

    // ================================================================== the three cases of a hit

    [Fact]
    public void AGhostTakesANormalHitForDouble()
    {
        // Blastoise's Strength (Attack 88, power 80) on Dusknoir (Defense 140, HP 105):
        // 88 × 80 × 22 / 140 / 50 = 22, + 2 = 24; Normal against Ghost, turned round, is twice: 48
        var dusknoir = Mon("Dusknoir", 50);
        var said = Turn(Wild(true, Mon("Blastoise", 50, "Strength"), dusknoir));
        Assert.Contains("It's super effective!", said);
        Assert.Equal(105 - 48, dusknoir.CurrentHP);

        // Without the flag it does nothing, as ever
        dusknoir = Mon("Dusknoir", 50);
        said = Turn(Wild(false, Mon("Blastoise", 50, "Strength"), dusknoir));
        Assert.Contains("It doesn't affect Foe Dusknoir...", said);
        Assert.DoesNotContain("It's super effective!", said);
        Assert.Equal(105, dusknoir.CurrentHP);
    }

    [Fact]
    public void AResistedHitIsDoubled()
    {
        // Charizard's Flamethrower (Sp. Atk 114, power 95) on Blastoise (Sp. Def 110, HP 139):
        // 114 × 95 × 22 / 110 / 50 = 43, + 2 = 45; its own type, 45 × 150 / 100 = 67.
        // Fire against Water is half in Platinum, 67 × 5 / 10 = 33, and twice turned round, 134
        var blastoise = Mon("Blastoise", 50);
        var said = Turn(Wild(true, Mon("Charizard", 50, "Flamethrower"), blastoise));
        Assert.Contains("It's super effective!", said);
        Assert.Equal(139 - 134, blastoise.CurrentHP);

        blastoise = Mon("Blastoise", 50);
        said = Turn(Wild(false, Mon("Charizard", 50, "Flamethrower"), blastoise));
        Assert.Contains("It's not very effective...", said);
        Assert.Equal(139 - 33, blastoise.CurrentHP);
    }

    [Fact]
    public void AWeaknessIsHalved()
    {
        // Blastoise's Surf (Sp. Atk 90, power 95) on Charizard (Sp. Def 90, HP 138):
        // 90 × 95 × 22 / 90 / 50 = 41, + 2 = 43; its own type, 43 × 150 / 100 = 64.
        // Water against Fire is twice in Platinum, 128, and half turned round, 64 × 5 / 10 = 32; Flying is even both ways
        var charizard = Mon("Charizard", 50);
        var said = Turn(Wild(true, Mon("Blastoise", 50, "Surf"), charizard));
        Assert.Contains("It's not very effective...", said);
        Assert.Equal(138 - 32, charizard.CurrentHP);

        charizard = Mon("Charizard", 50);
        said = Turn(Wild(false, Mon("Blastoise", 50, "Surf"), charizard));
        Assert.Contains("It's super effective!", said);
        Assert.Equal(138 - 128, charizard.CurrentHP);
    }

    [Fact]
    public void TwoTypesAreEachTurnedRoundBeforeTheyAreMultiplied()
    {
        // Garchomp's Earthquake (Attack 135, power 100) on Skarmory (Defense 145, HP 125):
        // 135 × 100 × 22 / 145 / 50 = 40, + 2 = 42; its own type, 63. In Platinum the Flying type stops it. Turned
        // round, Steel resists it (63 × 5 / 10 = 31) and Flying is weak to it (31 × 20 / 10 = 62), each in the order
        // of Skarmory's types: an even hit, with nothing said of it
        var skarmory = Mon("Skarmory", 50);
        var said = Turn(Wild(true, Mon("Garchomp", 50, "Earthquake"), skarmory));
        Assert.DoesNotContain("It's super effective!", said);
        Assert.DoesNotContain("It's not very effective...", said);
        Assert.Equal(125 - 62, skarmory.CurrentHP);

        skarmory = Mon("Skarmory", 50);
        Assert.Contains("It doesn't affect Foe Skarmory...", Turn(Wild(false, Mon("Garchomp", 50, "Earthquake"), skarmory)));
        Assert.Equal(125, skarmory.CurrentHP);
    }

    // ================================================================== what a type stopped, and what an ability still stops

    [Fact]
    public void AFlyingTypeIsWeakToGroundButLevitateAndMagnetismStillKeepItOff()
    {
        // Blastoise's Earthquake (Attack 88) on Pidgeot (Defense 80, HP 143): 88 × 100 × 22 / 80 / 50 = 48, + 2 = 50;
        // Normal is even, and Flying, which the move couldn't touch, takes twice: 100
        var pidgeot = Mon("Pidgeot", 50);
        Assert.Contains("It's super effective!", Turn(Wild(true, Mon("Blastoise", 50, "Earthquake"), pidgeot)));
        Assert.Equal(143 - 100, pidgeot.CurrentHP);
        pidgeot = Mon("Pidgeot", 50);
        Assert.Contains("It doesn't affect Foe Pidgeot...", Turn(Wild(false, Mon("Blastoise", 50, "Earthquake"), pidgeot)));

        // Levitate is an ability's immunity, not a type's: it stays (Showdown's Inverse Mod leaves isGrounded alone)
        var gengar = Mon("Gengar", 50);
        Assert.Equal("Levitate", gengar.AbilityName);
        Assert.Contains("Foe Gengar makes Ground moves miss with Levitate!", Turn(Wild(true, Mon("Blastoise", 50, "Earthquake"), gengar)));
        Assert.Equal(gengar.MaxHP, gengar.CurrentHP);

        // So does Magnet Rise. On the ground Magnezone, weak to Ground twice over in Platinum, resists it twice over
        // (88 × 100 × 22 / 120 / 50 = 32, + 2 = 34; Electric, × 5 / 10 = 17; Steel, × 5 / 10 = 8); risen, it takes nothing
        var magnezone = Mon("Magnezone", 50);
        Assert.Contains("It's not very effective...", Turn(Wild(true, Mon("Blastoise", 50, "Earthquake"), magnezone)));
        Assert.Equal(130 - 8, magnezone.CurrentHP);

        magnezone = Idling(Mon("Magnezone", 50, "Magnet Rise"));
        var core = Wild(true, Idling(Mon("Blastoise", 50, "Earthquake")), magnezone);
        Turn(core, mine: 1, foe: 0);
        Assert.Contains("It doesn't affect Foe Magnezone...", Turn(core, mine: 0, foe: 1));
        Assert.Equal(130, magnezone.CurrentHP);
    }

    [Fact]
    public void GravityHasNoImmunityLeftToLiftButStillBringsLevitateDown()
    {
        // In Platinum Gravity lets Earthquake hit a Flying type evenly: Blastoise's on Pidgeot is the 50 above. In an
        // inverse battle the Flying type is weak to Ground with Gravity or without it: 100 either way
        var pidgeot = Mon("Pidgeot", 50);
        var core = Wild(false, Mon("Blastoise", 50, "Gravity", "Earthquake"), pidgeot);
        Turn(core, mine: 0);
        var said = Turn(core, mine: 1);
        Assert.DoesNotContain("It's super effective!", said);
        Assert.Equal(143 - 50, pidgeot.CurrentHP);

        pidgeot = Mon("Pidgeot", 50);
        core = Wild(true, Mon("Blastoise", 50, "Gravity", "Earthquake"), pidgeot);
        Turn(core, mine: 0);
        Assert.Contains("It's super effective!", Turn(core, mine: 1));
        Assert.Equal(143 - 100, pidgeot.CurrentHP);

        // What Gravity does to an ability it still does: Gengar's Levitate no longer holds it up, and the hit goes by
        // its types (88 × 100 × 22 / 65 / 50 = 59, + 2 = 61; Ghost is even, and Poison, weak to Ground in Platinum,
        // resists it: 61 × 5 / 10 = 30)
        var gengar = Mon("Gengar", 50);
        core = Wild(true, Mon("Blastoise", 50, "Gravity", "Earthquake"), gengar);
        Turn(core, mine: 0);
        said = Turn(core, mine: 1);
        Assert.DoesNotContain(said, line => line.Contains("Levitate"));
        Assert.Contains("It's not very effective...", said);
        Assert.Equal(120 - 30, gengar.CurrentHP);
    }

    [Fact]
    public void WonderGuardLetsThroughWhatIsSuperEffectiveByTheBattlesChart()
    {
        // Shedinja is Bug and Ghost. Fire, which got through in Platinum, is resisted here; Normal, which the Ghost
        // type stopped, is super effective
        var shedinja = Mon("Shedinja", 50);
        Assert.Contains("Foe Shedinja avoided damage with Wonder Guard!", Turn(Wild(true, Mon("Charizard", 50, "Flamethrower"), shedinja)));
        Assert.Equal(1, shedinja.CurrentHP);

        shedinja = Mon("Shedinja", 50);
        Turn(Wild(true, Mon("Blastoise", 50, "Strength"), shedinja));
        Assert.Equal(0, shedinja.CurrentHP);

        // And the other way round without the flag
        shedinja = Mon("Shedinja", 50);
        Turn(Wild(false, Mon("Charizard", 50, "Flamethrower"), shedinja));
        Assert.Equal(0, shedinja.CurrentHP);
        shedinja = Mon("Shedinja", 50);
        Assert.Contains("Foe Shedinja avoided damage with Wonder Guard!", Turn(Wild(false, Mon("Blastoise", 50, "Strength"), shedinja)));
    }

    [Fact]
    public void ThunderWaveReachesAGroundType()
    {
        // The one status move a type can be immune to: with no type stopping anything, it paralyzes
        var golem = Mon("Golem", 50);
        Turn(Wild(true, Mon("Raichu", 50, "Thunder Wave"), golem));
        Assert.Equal(StatusCondition.Paralyze, golem.Status);

        golem = Mon("Golem", 50);
        Assert.Contains("It doesn't affect Foe Golem...", Turn(Wild(false, Mon("Raichu", 50, "Thunder Wave"), golem)));
        Assert.Equal(StatusCondition.None, golem.Status);
    }

    // ================================================================== the callers beside the hit

    [Fact]
    public void StealthRockGoesByTheBattlesChart()
    {
        // The existing test's three, with the chart turned round: Charizard (Fire and Flying, both weak to Rock in
        // Platinum) takes a thirty-second, Machamp (Fighting resists Rock) a quarter, Blastoise an eighth as ever
        var charizard = Mon("Charizard", 50);
        var machamp = Mon("Machamp", 50);
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(true, Mon("Snorlax", 50), Mon("Garchomp", 50, "Stealth Rock"), null, charizard, machamp, blastoise);

        Assert.Contains("Pointed stones float in the air around your team!", Turn(core));
        Assert.Contains("Pointed stones dug into Charizard!", SwitchTo(core, 1));
        Assert.Equal(138 - 4, charizard.CurrentHP);        // 138 / 32
        SwitchTo(core, 2);
        Assert.Equal(150 - 37, machamp.CurrentHP);          // 150 / 4
        SwitchTo(core, 3);
        Assert.Equal(139 - 17, blastoise.CurrentHP);        // 139 / 8
    }

    [Fact]
    public void ABerryAgainstATypeIsEatenWhenTheHitIsSuperEffectiveByTheBattlesChart()
    {
        // Flamethrower on Blastoise is 134 in an inverse battle (above): super effective, so an Occa Berry halves it
        // as it lands, 67, and is eaten
        var blastoise = With(Mon("Blastoise", 50), item: "Occa Berry");
        var said = Turn(Wild(true, Mon("Charizard", 50, "Flamethrower"), blastoise));
        Assert.Contains("The Occa Berry weakened Flamethrower's power!", said);
        Assert.Equal(139 - 67, blastoise.CurrentHP);
        Assert.Null(blastoise.HeldItem);

        // In Platinum the hit is resisted (33) and the berry stays in hand
        blastoise = With(Mon("Blastoise", 50), item: "Occa Berry");
        Turn(Wild(false, Mon("Charizard", 50, "Flamethrower"), blastoise));
        Assert.Equal(139 - 33, blastoise.CurrentHP);
        Assert.NotNull(blastoise.HeldItem);
    }

    [Fact]
    public void AnticipationShuddersAtWhatWouldHurtByTheBattlesChart()
    {
        bool Shudders(bool inverse, Pokemon foe, string holder) =>
            Lines(Wild(inverse, foe, With(Mon(holder, 50), ability: "Anticipation")).Log).Contains($"Foe {holder} shuddered!");

        // Toxicroak is Poison and Fighting. Cross Chop is resisted in Platinum and super effective turned round;
        // Psychic is four times in Platinum and a quarter turned round
        Assert.False(Shudders(false, Mon("Machamp", 50, "Cross Chop"), "Toxicroak"));
        Assert.True(Shudders(true, Mon("Machamp", 50, "Cross Chop"), "Toxicroak"));
        Assert.True(Shudders(false, Mon("Alakazam", 50, "Psychic"), "Toxicroak"));
        Assert.False(Shudders(true, Mon("Alakazam", 50, "Psychic"), "Toxicroak"));
        // A move that wouldn't touch it at all is nothing to shudder at; turned round it is a weakness
        Assert.False(Shudders(false, Mon("Blastoise", 50, "Strength"), "Dusknoir"));
        Assert.True(Shudders(true, Mon("Blastoise", 50, "Strength"), "Dusknoir"));
    }

    [Fact]
    public void Conversion2TakesATypeThatResistsByTheBattlesChart()
    {
        // The existing test's battle: Pikachu is hit by Water Gun. In an inverse battle the types that resist Water
        // are the ones weak to it in Platinum, in the chart's order Fire, Ground and Rock, and the second is drawn
        var core = Wild(true, Mon("Pikachu", 50, "Conversion 2"), Idling(Mon("Blastoise", 50, "Water Gun")), Calm().Force(RollKind.Pick, 1));
        Assert.Contains("But it failed!", Turn(core, 0, 0));
        Assert.Contains("Pikachu transformed into the Ground type!", Turn(core, 0, 1));
        Assert.True(core.At(Mine).HasType(PokemonType.Ground));

        // Nothing resists a Normal move there (Normal is super effective against nothing in Platinum): it fails
        core = Wild(true, Mon("Pikachu", 50, "Conversion 2"), Idling(Mon("Blastoise", 50, "Tackle")));
        Turn(core, 0, 0);
        Assert.Contains("But it failed!", Turn(core, 0, 1));
        Assert.True(core.At(Mine).HasType(PokemonType.Electric));
    }

    // ================================================================== the trainer AI and the move cards

    [Fact]
    public void TheTrainerAiReadsTheBattlesChart()
    {
        AiThinking Look(BattleCore core)
        {
            core.AiMemory.CatchUp(core);
            var look = new AiThinking(core, core.AiMemory, core.EnemySlots[0], core.PlayerSlots[0], AiFlags.Basic);
            look.Think();
            return look;
        }
        Pokemon Bidoof() => Of(Gender.Male, "Bidoof", 20, "Tackle", "Earthquake");
        Pokemon Gastly() => Of(Gender.Male, "Gastly", 20, "Lick");

        // Basic_CheckForImmunity. In Platinum a Ghost is immune to Tackle and Gastly's Levitate to Earthquake: both −10
        var plain = Against(false, Gastly(), Bidoof());
        var look = Look(plain);
        Assert.Equal((90, 90), (look.Scores[0], look.Scores[1]));
        Assert.Equal(Eff.Immune, look.Matchup(plain.EnemySlots[0], plain.PlayerSlots[0], MoveDatabase.Get("Tackle")!));

        // In an inverse battle Tackle is super effective (its own type: 40 × 1.5 = 60, twice against Ghost, 120, which
        // the original maps back to "double"), and Levitate still stops Earthquake: the trainer uses Tackle
        var inverse = Against(true, Gastly(), Bidoof());
        look = Look(inverse);
        Assert.Equal((100, 90), (look.Scores[0], look.Scores[1]));
        Assert.Equal(Eff.Double, look.Matchup(inverse.EnemySlots[0], inverse.PlayerSlots[0], MoveDatabase.Get("Tackle")!));
        Assert.Equal(Eff.Immune, look.Matchup(inverse.EnemySlots[0], inverse.PlayerSlots[0], MoveDatabase.Get("Earthquake")!));
        var choice = TrainerAi.Instance.ChooseAction(inverse, inverse.EnemySlots[0]);
        Assert.Equal((ChoiceKind.Fight, 0), (choice.Kind, choice.Move));

        // And what was a weakness is a resistance to it: Machop's Karate Chop on a Normal type, 60 × 2 and 60 / 2
        Pokemon Machop() => Of(Gender.Male, "Machop", 20, "Karate Chop");
        var chop = MoveDatabase.Get("Karate Chop")!;
        plain = Against(false, Of(Gender.Male, "Bidoof", 20, "Tackle"), Machop());
        Assert.Equal(Eff.Double, Look(plain).Matchup(plain.EnemySlots[0], plain.PlayerSlots[0], chop));
        inverse = Against(true, Of(Gender.Male, "Bidoof", 20, "Tackle"), Machop());
        Assert.Equal(Eff.Half, Look(inverse).Matchup(inverse.EnemySlots[0], inverse.PlayerSlots[0], chop));
    }

    [Fact]
    public void TheTrainerSendsInWhoeverHitsBestByTheBattlesChart()
    {
        // BattleAI_PostKOSwitchIn: the party member whose types do best against the foe's (BattleSystem_TypeMatchupMultiplier,
        // 40 through the chart for each of its types, a single type counted twice), if it has a super-effective move.
        // Against Blastoise (Water): Venusaur's Grass and Poison are 80 + 40 in Platinum and 20 + 40 turned round;
        // Charizard's Fire and Flying are 20 + 40 in Platinum and 80 + 40 turned round
        Pokemon[] Team() => new[]
        {
            Of(Gender.Male, "Bidoof", 50, "Tackle"), Of(Gender.Male, "Charizard", 50, "Flamethrower"), Of(Gender.Male, "Venusaur", 50, "Razor Leaf")
        };
        var plain = Against(false, Of(Gender.Male, "Blastoise", 50, "Surf"), Team());
        Assert.Equal(2, TrainerAi.PostKoSwitchIn(plain, plain.EnemySlots[0]));

        var inverse = Against(true, Of(Gender.Male, "Blastoise", 50, "Surf"), Team());
        Assert.Equal(1, TrainerAi.PostKoSwitchIn(inverse, inverse.EnemySlots[0]));
    }

    [Fact]
    public void TwoPokemonOnNoFieldMeetByThePlainChart()
    {
        // The calculator outside a battle (a battler that stands on no field) knows nothing of any battle's chart:
        // Flamethrower on Blastoise is the resisted 33 of above
        Assert.Equal(33, Damage(Mon("Charizard", 50, "Flamethrower"), Mon("Blastoise", 50), "Flamethrower"));
    }

    [Theory]
    [InlineData(true, "Gastly", "Pound", MoveHint.SuperEffective)]
    [InlineData(false, "Gastly", "Pound", MoveHint.NoEffect)]
    [InlineData(true, "Geodude", "Bubble", MoveHint.NotVeryEffective)]
    [InlineData(false, "Geodude", "Bubble", MoveHint.SuperEffective)]
    [InlineData(true, "Bidoof", "Bubble", MoveHint.Neutral)]
    public void TheMoveCardsHintTellsTheTruthOfTheBattle(bool inverse, string foe, string move, MoveHint hint)
    {
        var party = new Party();
        party.Add(Mon("Piplup", 20, move));
        var wild = Mon(foe, 20);
        var dex = new Pokedex();
        dex.RegisterSeen(wild.Species.DexNumber);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = dex, WildPokemon = new List<Pokemon> { wild },
            Random = Steady(), Rules = Ruleset.Platinum, MoveHints = true, Conditions = Chart(inverse)
        });
        var opening = Settle(battle);
        Assert.Equal(inverse, opening.Contains(BattleCore.InverseLine));
        Assert.Equal(hint, battle.HintFor(battle.PlayerPokemon.Moves[0], battle.EnemySlots[0]));

        // And the hit then sounds as the card said
        int before = battle.Core.Log.Count;
        var said = Scenario.Turn(battle);
        Assert.Equal(hint == MoveHint.SuperEffective, said.Contains("It's super effective!"));
        Assert.Equal(hint == MoveHint.NotVeryEffective, said.Contains("It's not very effective..."));
        if (hint == MoveHint.NoEffect) return;
        var sounded = battle.Core.Log.Skip(before).SelectMany(e => e is Said line ? line.Shows.Concat(line.OnImpact).Prepend(e) : new[] { e })
            .OfType<HitSounded>().First();
        Assert.Equal(hint == MoveHint.SuperEffective, sounded.SuperEffective);
        Assert.Equal(hint == MoveHint.NotVeryEffective, sounded.NotVeryEffective);
    }

    // ================================================================== a record

    [Fact]
    public void ARecordedInverseBattleReplaysTheSameAndOnlyAsAnInverseBattle()
    {
        (Party Mine, Pokemon Foe) Teams()
        {
            var party = new Party();
            party.Add(Mon("Gengar", 40, "Shadow Ball", "Strength", "Thunderbolt"));
            party.Add(Mon("Skarmory", 40, "Steel Wing", "Aerial Ace"));
            return (party, Mon("Rhydon", 42, "Earthquake", "Rock Slide", "Hammer Arm"));
        }
        BattleCore Battle(uint seed, bool inverse)
        {
            var (mine, foe) = Teams();
            return new BattleCore(new CoreSetup
            {
                PlayerParty = mine, WildPokemon = new List<Pokemon> { foe }, Random = new BattleRandom(seed), Rules = Ruleset.Platinum,
                Conditions = Chart(inverse), PlayerController = null, EnemyController = null, PlayerName = "Lucas"
            });
        }

        // Played with its chance left free: moves in turn, and whoever is asked for after a faint
        var core = Battle(20261012, inverse: true);
        core.Start();
        for (int turn = 0; turn < 30 && core.Request != null; turn++)
        {
            switch (core.Request)
            {
                case ReplacementRequest asked:
                    core.Submit(BattleChoice.Switch(asked.Place, Enumerable.Range(0, core.PlayerParty.Count).First(i => core.CanSendIn(core.At(asked.Place), i))));
                    break;
                case ActionRequest asked:
                    core.Submit(asked.Places.Select(p => BattleChoice.Fight(p, turn % core.At(p).Pokemon!.Moves.Count)).ToList());
                    break;
            }
        }
        Assert.Null(core.Request);
        // The chart was at work in it: Gengar's Levitate apart, Rhydon's Ground moves found a Flying type weak
        Assert.Contains(BattleCore.InverseLine, Lines(core.Log));

        var record = core.Record!;
        Assert.True(record.Inverse);
        Assert.Equal(20261012u, record.Seed);

        var again = Battle(record.Seed, inverse: true);
        again.Replay(record);
        Assert.Equal(Golden.BattleText.Written(core), Golden.BattleText.Written(again));
        Assert.Equal(core.Result, again.Result);

        // Written out and read back it is still an inverse battle's
        string written = GameDataFiles.Serialize(record);
        var restored = System.Text.Json.JsonSerializer.Deserialize<BattleRecord>(written, GameDataFiles.Json)!;
        Assert.True(restored.Inverse);
        var third = Battle(restored.Seed, inverse: true);
        third.Replay(restored);
        Assert.Equal(Golden.BattleText.Written(core), Golden.BattleText.Written(third));

        // The same seed and answers under the plain chart are another battle: a replay refuses the wrong one, both ways
        Assert.Throws<ArgumentException>(() => Battle(record.Seed, inverse: false).Replay(record));
        var plain = Battle(record.Seed, inverse: false);
        plain.Start();
        plain.Submit(record.Answers[0]);
        Assert.False(plain.Record!.Inverse);
        Assert.Throws<ArgumentException>(() => Battle(record.Seed, inverse: true).Replay(plain.Record!));
        var first = Battle(record.Seed, inverse: true);
        first.Start();
        first.Submit(record.Answers[0]);
        Assert.NotEqual(Golden.BattleText.Written(plain), Golden.BattleText.Written(first));

        // A record written before the flag existed reads as a plain battle's
        var old = System.Text.Json.JsonSerializer.Deserialize<BattleRecord>("{\"seed\":7,\"answers\":[]}", GameDataFiles.Json)!;
        Assert.False(old.Inverse);
    }

    // ================================================================== the script's word

    [Fact]
    public void TheWordInverseOnABattleLineAsksForAnInverseBattle()
    {
        var read = ScriptParser.Parse("test", """
            script S
              battle self as "scientist_shaun_rematch_3" inverse canlose
              battle self inverse
              battle self canlose
            """)[0].Everything().Where(i => i.Op == Op.Battle).ToList();
        Assert.Equal(new[] { true, true, false }, read.Select(i => i.Inverse));
        // The other words keep their meaning beside it, in any order
        Assert.Equal(("scientist_shaun_rematch_3", true), (read[0].AsTrainer, read[0].Option));
        Assert.False(read[1].Option);

        // The host is told, and a battle that doesn't say the word is told nothing
        var library = ScriptLibrary.FromSources(("test", """
            script S
              battle self as "scientist_shaun_rematch_3" inverse canlose
              battle self
            """), ("common", ""));
        var host = new HeadlessScriptHost();
        var person = new NPC { Key = "scientist", Name = "Scientist", GridX = 3, GridY = 3 };
        var map = new Map(12, 12) { Name = "Room" };
        map.NPCs.Add(person);
        host.Map = map;
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.First(s => s.File == "test"), person);
        runner.RunToEnd();

        Assert.Empty(host.Problems);
        Assert.Equal(new[] { "battle scientist_shaun_rematch_3 inverse Won", "battle scientist_shaun_rematch_3 Won" }, host.Log.Where(l => l.StartsWith("battle ")));
        // The person fought with the team named, as that trainer
        Assert.Equal(("Shaun", "Scientist"), (person.TrainerData!.Name, person.TrainerData.TrainerClass));
        Assert.Equal(new[] { "Magneton", "Alakazam" }, person.TrainerData.Party.Members.Select(p => p.Species.Name));
    }

    // ================================================================== the Fight Area's scientist

    private const string Scientist = "inverse_scientist", Place = "fight_area";
    private const string CrowdGone = "FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS";
    private const string DaysBattle = "FLAG_DAILY_FIGHT_AREA_INVERSE_BATTLE", Explained = "FLAG_FIGHT_AREA_INVERSE_EXPLAINED";
    private const string Team = "scientist_ingram";

    /// <summary>A game after the Fight Area's tag battle, the player standing below the scientist and facing him.</summary>
    private static OpeningTests.Game AfterTheTagBattle(BattleOutcome fight = BattleOutcome.Won)
    {
        var game = new OpeningTests.Game(0, fight);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 62));
        game.Story.SetVar("VAR_FIGHT_AREA_STATE", 2);
        game.Story.Set(CrowdGone);
        game.Arrive("Sinnoh", 662, 433);
        game.Facing = Direction.Up;
        return game;
    }

    private static HeadlessScriptHost Ask(OpeningTests.Game game, params int[] answers)
    {
        foreach (int answer in answers) game.Answers.Enqueue(answer);
        var npc = game.Present(Scientist, Place) ?? throw new InvalidOperationException("the scientist isn't there");
        game.Tile = (npc.GridX, npc.GridY + 1);
        game.Facing = Direction.Up;
        return game.Play(ScriptLibrary.Default.Find(npc.Script!, Place)!, npc);
    }

    [Fact]
    public void TheScientistStandsClearOfTheFightAreasScenesOnceTheCrowdHasGone()
    {
        // Nobody is there before the tag battle before the gate is over
        var early = new OpeningTests.Game();
        var map = early.Maps["Sinnoh"];
        var scientist = map.Everyone.Single(n => n.Key == Scientist);
        Assert.Equal((662, 432, CrowdGone, "InverseBattle", "Scientist"), (scientist.GridX, scientist.GridY, scientist.ShownBy, scientist.Script, scientist.NpcType));
        Assert.Null(scientist.HiddenBy);
        Assert.DoesNotContain(scientist, map.NPCs);
        // He carries no team and is no trainer of the map: nothing is drawn for him as the world loads, and no eye sees the player
        Assert.Null(scientist.TrainerData);
        Assert.False(scientist.IsTrainer);
        Assert.Null(scientist.Movement);

        // His tile is open ground of the Fight Area, with room to speak to him from the south and the west
        Assert.Equal(Place, map.AreaAt(662, 432)?.Key);
        foreach (var (x, y) in new[] { (662, 432), (662, 433), (661, 432) }) Assert.False(map.IsSolid(x, y), $"({x}, {y}) is shut");
        // No warp, no trigger and nothing to read on it or beside it
        Assert.DoesNotContain(map.Warps, w => Math.Abs(w.SourceX - 662) <= 1 && Math.Abs(w.SourceY - 432) <= 1);
        Assert.DoesNotContain(map.Triggers, t => 662 >= t.X && 662 < t.X + Math.Max(1, t.Width) && 432 >= t.Y && 432 < t.Y + Math.Max(1, t.Depth));
        // Nobody of the Fight Area stands or wanders there, and the scenes' walks keep to the gate's road (x 654 to 657) and the pier
        foreach (var other in map.Everyone.Where(n => n != scientist && map.AreaAt(n.GridX, n.GridY)?.Key == Place))
        {
            Assert.NotEqual((662, 432), (other.GridX, other.GridY));
            if (other.Movement is { } moves)
                Assert.True(Math.Abs(662 - moves.HomeX) > moves.RangeX || Math.Abs(432 - moves.HomeY) > moves.RangeZ, $"{other.Key} wanders onto the scientist's tile");
        }

        // Once the crowd has gone he is there
        var game = AfterTheTagBattle();
        Assert.NotNull(game.Present(Scientist, Place));
        // The flag that brings him on is the one the tag battle's script sets as the crowd leaves
        Assert.Contains("setflag " + CrowdGone, System.IO.File.ReadAllText(System.IO.Path.Combine(GameDataFiles.PathOf(ScriptLibrary.Folder), "fight_area.txt")));
    }

    [Fact]
    public void TheScientistExplainsOnceAndARefusalLeavesTheOfferStanding()
    {
        var game = AfterTheTagBattle();
        var talk = Ask(game, 1);
        // What an inverse battle is, the offer, and the refusal
        Assert.Contains(talk.Transcript, l => l.Speaker == "Scientist" && l.Text.Contains("inverse battle"));
        Assert.Contains(talk.Transcript, l => l.Text.Contains("takes double"));
        Assert.Single(talk.Asked);
        Assert.Contains("unlearning", talk.Transcript[^1].Text);
        Assert.DoesNotContain(talk.Log, l => l.StartsWith("battle "));
        Assert.True(game.Story.Has(Explained));
        Assert.False(game.Story.Has(DaysBattle));

        // Asked again he goes straight to the offer
        talk = Ask(game, 1);
        Assert.DoesNotContain(talk.Transcript, l => l.Text.Contains("takes double"));
        Assert.Single(talk.Asked);
        Assert.False(game.Story.Has(DaysBattle));
    }

    [Fact]
    public void TheScientistBattlesOnceADayUnderTheInverseChart()
    {
        var game = AfterTheTagBattle();
        var talk = Ask(game, 0);
        Assert.Contains($"battle {Team} inverse Won", talk.Log);
        Assert.Contains("straight into my notes", string.Join(" ", talk.Transcript.Select(l => l.Text)));
        Assert.True(game.Story.Has(DaysBattle));
        // He fought as a trainer of our own (plan 14 · B14), with a name and a team of his own: the trainer on Route
        // 212 whose team he borrowed in B1 is no more beaten than he was, and no table of Platinum's holds him
        Assert.Contains(TrainerDatabase.Ours, t => t.Id == Team);
        Assert.DoesNotContain(TrainerDatabase.All, t => t.Id == Team);
        var ours = TrainerDatabase.Get(Team)!;
        Assert.Equal(("Ingram", "Scientist"), (ours.Name, ours.Class));
        Assert.False(game.Story.HasDefeated("scientist_shaun"));
        Assert.False(game.Story.HasDefeated("scientist_shaun_rematch_3"));
        // Its Magneton is the point of it: Electric and Steel
        var magneton = TrainerDatabase.PartyOf(TrainerDatabase.Get(Team)!).Members[0];
        Assert.Equal(("Magneton", PokemonType.Electric, PokemonType.Steel), (magneton.Species.Name, magneton.PrimaryType, magneton.SecondaryType));

        // The same day: no question, no battle
        talk = Ask(game);
        Assert.Empty(talk.Asked);
        Assert.DoesNotContain(talk.Log, l => l.StartsWith("battle "));
        Assert.Contains("Come back tomorrow", talk.Transcript.Single().Text);

        // A new day clears the day's flag with the others, and he offers again without the explanation
        game.Story.ClearDaily();
        Assert.False(game.Story.Has(DaysBattle));
        talk = Ask(game, 0);
        Assert.DoesNotContain(talk.Transcript, l => l.Text.Contains("takes double"));
        Assert.Contains($"battle {Team} inverse Won", talk.Log);
    }

    [Fact]
    public void ABattleLostToTheScientistIsNoWhiteOutAndIsStillTheDaysBattle()
    {
        var game = AfterTheTagBattle(BattleOutcome.Lost);
        var talk = Ask(game, 0);
        Assert.Contains($"battle {Team} inverse Lost", talk.Log);
        // The script went on (canlose) to his word about it, where the player stood
        Assert.Contains("Old habits", string.Join(" ", talk.Transcript.Select(l => l.Text)));
        Assert.DoesNotContain(talk.Log, l => l.StartsWith("warp "));
        Assert.Equal("Sinnoh", game.Map.Name);
        Assert.True(game.Story.Has(DaysBattle));
        Assert.False(game.Story.HasDefeated(Team));
        Assert.Contains("Come back tomorrow", Ask(game).Transcript.Single().Text);
    }
}
