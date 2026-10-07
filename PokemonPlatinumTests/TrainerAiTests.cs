using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 06 · R9: Platinum's trainer AI (<c>src/battle/trainer_ai</c>) and the battles that play by rules of their
/// own. The scores are the original script's: every move starts at 100, each routine of the trainer's flags adds
/// or takes away, and the highest score is used.
/// </summary>
public class TrainerAiTests
{
    /// <summary>One look of the AI over its moves, as <c>TrainerAI_Init</c> and <c>TrainerAI_EvalMoves</c> make it.</summary>
    private static AiThinking Look(BattleCore core, AiFlags flags, Battler? attacker = null, Battler? defender = null)
    {
        core.AiMemory.CatchUp(core);
        var look = new AiThinking(core, core.AiMemory, attacker ?? core.EnemySlots[0], defender ?? core.PlayerSlots[0], flags);
        look.Think();
        return look;
    }

    private static BattleCore Special(BattleKind kind, Pokemon mine, Pokemon foe, BattleRandom? rolls = null, int balls = 0, bool cannotFlee = false)
    {
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Kind = kind, SpecialBalls = balls, CannotFlee = cannotFlee,
            Random = rolls ?? Calm(), Rules = Ruleset.Platinum, PlayerController = null, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    // ------------------------------------------------------------------ the routines

    [Fact]
    public void ABasicTrainerKeepsAwayFromAMoveItsTargetIsImmuneTo()
    {
        // Basic_CheckForImmunity: IfMoveEffectivenessEquals TYPE_MULTI_IMMUNE, ScoreMinus10
        var core = Against(new[] { Of(Gender.Male, "Gastly", 20, "Lick") }, new[] { Of(Gender.Male, "Bidoof", 20, "Tackle", "Ember") });
        var look = Look(core, AiFlags.Basic);
        Assert.Equal(90, look.Scores[0]);
        Assert.Equal(100, look.Scores[1]);

        var choice = TrainerAi.Instance.ChooseAction(core, core.EnemySlots[0]);
        Assert.Equal(ChoiceKind.Fight, choice.Kind);
        Assert.Equal(1, choice.Move);
    }

    [Fact]
    public void AnAbilityTheTrainerKnowsOfMarksAMoveDown()
    {
        // Basic_CheckWaterAbsorption: a Water move into Water Absorb gets -12. Vaporeon has no other ability, so the
        // AI's guess is sure
        var core = Against(new[] { Of(Gender.Male, "Vaporeon", 30, "Bite") }, new[] { Of(Gender.Male, "Buizel", 30, "Water Gun", "Quick Attack") });
        var look = Look(core, AiFlags.Basic);
        Assert.Equal(88, look.Scores[0]);
        Assert.Equal(100, look.Scores[1]);

        // Lanturn may have Volt Absorb or Illuminate: the trainer doesn't know of a Water Absorb nobody has seen
        var lanturn = With(Of(Gender.Male, "Lanturn", 30, "Spark"), ability: "Water Absorb");
        core = Against(new[] { lanturn }, new[] { Of(Gender.Male, "Buizel", 30, "Water Gun", "Quick Attack") });
        look = Look(core, AiFlags.Basic);
        Assert.Equal(100, look.Scores[0]);
    }

    [Fact]
    public void EvaluatingAttacksPrefersTheStrongestMoveAndOneThatKnocksOut()
    {
        // EvalAttack_Main: a move that isn't the strongest -1; one that knocks the target out at the strongest roll +4
        var bidoof = Of(Gender.Male, "Bidoof", 30, "Tackle");
        var core = Against(new[] { bidoof }, new[] { Of(Gender.Male, "Machop", 20, "Pound", "Mega Punch") });
        var look = Look(core, AiFlags.EvalAttack);
        Assert.Equal(99, look.Scores[0]);
        Assert.Equal(100, look.Scores[1]);

        bidoof.CurrentHP = 1;
        look = Look(core, AiFlags.EvalAttack);
        Assert.Equal(104, look.Scores[0]);
        Assert.Equal(104, look.Scores[1]);
    }

    [Fact]
    public void SettingUpOnTheFirstTurnIsMostlyWorthIt()
    {
        // SetupFirstTurn_Main: IfRandomLessThan 80, Terminate; AddToMoveScore 2. A roll of 200 isn't under 80.
        var core = Against(new[] { Of(Gender.Male, "Bidoof", 20, "Tackle") }, new[] { Of(Gender.Male, "Machop", 20, "Low Kick", "Bulk Up") },
            Calm().Force(RollKind.AiChoice, 200));
        var look = Look(core, AiFlags.SetupFirstTurn);
        Assert.Equal(100, look.Scores[0]);
        Assert.Equal(102, look.Scores[1]);
    }

    [Fact]
    public void AMoveWithNoPpLeftIsNeverChosen()
    {
        var machop = Of(Gender.Male, "Machop", 20, "Karate Chop", "Leer");
        machop.Moves[0].CurrentPP = 0;
        var core = Against(new[] { Of(Gender.Male, "Bidoof", 20, "Tackle") }, new[] { machop });
        var look = Look(core, AiFlags.Basic | AiFlags.EvalAttack | AiFlags.Expert);
        Assert.Equal(0, look.Scores[0]);
        Assert.Equal(1, TrainerAi.Instance.ChooseAction(core, core.EnemySlots[0]).Move);
    }

    [Fact]
    public void ATrainersAbilitiesComeFromItsData()
    {
        // Platinum's trainers think with the flags of their data: Roark with Basic, EvalAttack and Expert
        var roark = TrainerDatabase.Get("TRAINER_LEADER_ROARK")!;
        Assert.Equal(AiFlags.Basic | AiFlags.EvalAttack | AiFlags.Expert, AiFlagNames.Parse(roark.Ai));
        Assert.Equal(new[] { "Potion", "Potion" }, roark.Items);
        Assert.Equal(AiFlags.Basic, AiFlagNames.Parse(TrainerDatabase.Get("youngster_tristan")!.Ai));
    }

    // ------------------------------------------------------------------ switching and items

    [Fact]
    public void ATrainerHealsAPokemonInTheRedWithItsPotion()
    {
        // TrainerAI_ShouldUseItem: an HP item when the Pokémon is under a quarter of its HP, or would take all of it
        var geodude = Of(Gender.Male, "Geodude", 14, "Tackle");
        var party = new Party();
        party.Add(Of(Gender.Male, "Bidoof", 20, "Tackle"));
        var roark = new Trainer { Id = "roark", Name = "Roark", TrainerClass = "Leader", Ai = AiFlags.Basic, Items = new List<string> { "Potion" } };
        roark.Party.Add(geodude);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { roark }, Random = Calm(), Rules = Ruleset.Platinum, PlayerController = null, PlayerName = "Lucas"
        });
        core.Start();
        Assert.Equal(ChoiceKind.Fight, TrainerAi.Instance.ChooseAction(core, core.EnemySlots[0]).Kind);

        geodude.CurrentHP = geodude.MaxHP / 4 - 1;
        var choice = TrainerAi.Instance.ChooseAction(core, core.EnemySlots[0]);
        Assert.Equal(ChoiceKind.Item, choice.Kind);
        Assert.Equal("Potion", choice.Item);
        // The potion is used up: there is no second
        Assert.Equal(ChoiceKind.Fight, TrainerAi.Instance.ChooseAction(core, core.EnemySlots[0]).Kind);
    }

    [Fact]
    public void ATrainerUsesItsItemsInTheTurnAndThePotionHeals()
    {
        var geodude = Of(Gender.Male, "Geodude", 14, "Tackle");
        geodude.CurrentHP = 5;
        var party = new Party();
        party.Add(Idling(Of(Gender.Male, "Bidoof", 20)));
        var trainer = new Trainer { Id = "roark", Name = "Roark", TrainerClass = "Leader", Ai = AiFlags.Basic, Items = new List<string> { "Potion" } };
        trainer.Party.Add(geodude);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = Ruleset.Platinum, PlayerController = null, PlayerName = "Lucas"
        });
        core.Start();
        core.Submit(BattleChoice.Fight(Mine, 0));
        var said = Lines(core.TakeLog());
        Assert.Contains(said, s => s.Contains("Potion"));
        Assert.Equal(25, geodude.CurrentHP);
    }

    [Fact]
    public void ATrainerSwitchesOutAPokemonThatCanOnlyHitAWonderGuard()
    {
        // TrainerAI_ShouldSwitch, CannotDamageWonderGuard: nothing it knows is super effective on Shedinja, and a
        // Pokémon behind it has a move that is; it switches two times in three
        var shedinja = With(Of(Gender.Male, "Shedinja", 30, "Scratch"), ability: "Wonder Guard");
        var core = Against(new[] { shedinja }, new[] { Of(Gender.Male, "Bidoof", 30, "Tackle"), Of(Gender.Male, "Ponyta", 30, "Ember") },
            Calm().Force(RollKind.AiChoice, 0));
        var choice = TrainerAi.Instance.ChooseAction(core, core.EnemySlots[0]);
        Assert.Equal(ChoiceKind.Switch, choice.Kind);
        Assert.Equal(1, choice.SwitchTo);
    }

    [Fact]
    public void AfterAKnockOutTheTrainerSendsInWhatHitsHardest()
    {
        // BattleAI_PostKOSwitchIn: the bench member whose types do best against the player's Pokémon, then the one
        // whose move would hurt it most
        var mine = Of(Gender.Male, "Turtwig", 30, "Tackle");
        var core = Against(new[] { mine }, new[] { Of(Gender.Male, "Bidoof", 5, "Tackle"), Of(Gender.Male, "Geodude", 30, "Tackle"), Of(Gender.Male, "Ponyta", 30, "Ember") });
        int picked = TrainerAi.PostKoSwitchIn(core, core.EnemySlots[0]);
        Assert.Equal(2, picked);
    }

    // ------------------------------------------------------------------ battles of their own

    [Fact]
    public void ARoamerRunsUnlessSomethingHoldsIt()
    {
        // RoamingPokemon_Main: Escape, unless bound or held by Mean Look
        var core = Special(BattleKind.Roamer, Of(Gender.Male, "Bidoof", 30, "Tackle"), Of(Gender.Male, "Mesprit", 50, "Confusion"));
        Assert.Equal(ChoiceKind.Run, TrainerAi.Instance.ChooseAction(core, core.EnemySlots[0]).Kind);
        core.EnemySlots[0].Volatile.TrappedBy = Mine;
        Assert.Equal(ChoiceKind.Fight, TrainerAi.Instance.ChooseAction(core, core.EnemySlots[0]).Kind);
        core.EnemySlots[0].Volatile.TrappedBy = null;

        var said = Turn(core);
        Assert.Equal(BattleResult.EnemyFled, core.Result);
        Assert.Contains(said, s => s.EndsWith("ran away!"));
        // Its line takes it off the platform as it is read (BattleEngine.Show), where a Pokémon called back goes
        // with its recall
        var line = Assert.Single(core.Log.OfType<Said>(), s => s.Text.EndsWith("ran away!"));
        Assert.Contains(line.Shows, e => e is Left { Ran: true } left && left.Place == core.EnemySlots[0].Place);
    }

    [Fact]
    public void ABattleThatCantBeFledIsntFled()
    {
        var core = Special(BattleKind.Normal, Of(Gender.Male, "Bidoof", 30, "Tackle"), Of(Gender.Male, "Giratina", 47, "Shadow Claw"), cannotFlee: true);
        Assert.NotNull(core.WhyNot(BattleChoice.Run(Mine)));
    }

    [Fact]
    public void InTheCatchingLessonTheAssistantPlaysItThroughAndCatchesWhatTheyMet()
    {
        // FieldBattleDTO_NewCatchingTutorial: the assistant's own starter against a level-2 Bidoof; CatchTutorial_Main
        // throws a ball once the Bidoof is down to a fifth; BATTLE_TYPE_ALWAYS_CATCH; no critical hit, no miss
        var piplup = Of(Gender.Male, "Piplup", 5, "Pound");
        var bidoof = Of(Gender.Male, "Bidoof", 2, "Tackle");
        var party = new Party();
        party.Add(piplup);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { bidoof }, Kind = BattleKind.CatchingLesson,
            Random = new BattleRandom(7).Force(RollKind.Accuracy, 99).Force(RollKind.Critical, 0), Rules = Ruleset.Platinum,
            PlayerController = null, PlayerName = "Dawn"
        });
        core.Start();

        // Nobody was asked anything: the lesson played itself to its end
        Assert.Null(core.Request);
        Assert.Equal(BattleResult.EnemyCaught, core.Result);
        Assert.Contains(core.Log, e => e is Caught);
        Assert.DoesNotContain(core.Log.OfType<Said>(), s => s.Text.Contains("critical"));
        Assert.DoesNotContain(core.Log.OfType<Said>(), s => s.Text.Contains("missed"));
        // What was caught is the assistant's, not added to the party the lesson was fought with
        Assert.Single(party.Members);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheFirstBattleHasNoCriticalHits(bool first)
    {
        // BtlCmd_CalcCrit: criticalMul 1 under BATTLE_STATUS_FIRST_BATTLE, whatever the roll
        var party = new Party();
        party.Add(Of(Gender.Male, "Piplup", 5, "Pound"));
        var rival = new Trainer { Id = "rival", Name = "Barry", TrainerClass = "Rival" };
        rival.Party.Add(Of(Gender.Male, "Turtwig", 5, "Tackle"));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { rival }, FirstBattle = first, Random = Steady().Force(RollKind.Critical, 0),
            Rules = Ruleset.Platinum, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        var said = Turn(core);
        Assert.Equal(!first, said.Any(s => s.Contains("critical")));
    }

    [Fact]
    public void TheGreatMarshIsPlayedWithBaitMudAndSafariBalls()
    {
        // Bait: the catch stage up, and the escape stage up unless the roll of 10 is 0; mud the other way round
        var core = Special(BattleKind.Safari, Of(Gender.Male, "Bidoof", 30, "Tackle"), Of(Gender.Male, "Carnivine", 25, "Bind"),
            Calm().Force(RollKind.Safari, 3, 254, 3, 254), balls: 30);
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Mine, 0)));
        Assert.NotNull(core.WhyNot(BattleChoice.Switch(Mine, 0)));
        Assert.NotNull(core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));

        core.Submit(BattleChoice.Bait(Mine));
        Assert.Equal(7, core.SafariCatchStage);
        Assert.Equal(7, core.SafariEscapeCount);
        core.Submit(BattleChoice.Mud(Mine));
        Assert.Equal(6, core.SafariCatchStage);
        Assert.Equal(6, core.SafariEscapeCount);
        Assert.Equal(BattleResult.None, core.Result);

        core.Submit(BattleChoice.UseItem(Mine, "Safari Ball"));
        Assert.Equal(29, core.SpecialBalls);
    }

    [Fact]
    public void AGreatMarshPokemonRunsByItsFleeRate()
    {
        // Carnivine's flee rate is 60, at the middle stage a roll of 255 at or under it runs
        var core = Special(BattleKind.Safari, Of(Gender.Male, "Bidoof", 30, "Tackle"), Of(Gender.Male, "Carnivine", 25, "Bind"),
            Calm().Force(RollKind.Safari, 5, 60), balls: 30);
        core.Submit(BattleChoice.Bait(Mine));
        // The bait made it a stage likelier: 60 × 15/10 is 90, and 60 is under it
        Assert.Equal(BattleResult.EnemyFled, core.Result);
    }

    [Fact]
    public void ThrowingTheLastSafariBallEndsTheGame()
    {
        var core = Special(BattleKind.Safari, Of(Gender.Male, "Bidoof", 30, "Tackle"), Of(Gender.Male, "Kangaskhan", 30, "Bite"),
            Calm().Force(RollKind.CatchShake, 65535).Force(RollKind.Safari, 254), balls: 1);
        core.Submit(BattleChoice.UseItem(Mine, "Safari Ball"));
        Assert.Equal(0, core.SpecialBalls);
        Assert.Equal(BattleResult.PlayerRan, core.Result);
    }

    [Fact]
    public void PalParksBallNeverMisses()
    {
        var core = Special(BattleKind.PalPark, Of(Gender.Male, "Bidoof", 30, "Tackle"), Of(Gender.Male, "Mewtwo", 70, "Psychic"), balls: 6);
        core.Submit(BattleChoice.UseItem(Mine, "Safari Ball"));
        Assert.Equal(BattleResult.EnemyCaught, core.Result);
        Assert.Contains(core.Log.OfType<Said>(), s => s.Text.Contains("Park Ball"));
    }

    [Fact]
    public void InATagBattleThePartnerFightsBesideThePlayerAndThePlayerLosesWithTheirOwnTeam()
    {
        var party = new Party();
        party.Add(Of(Gender.Male, "Bidoof", 5, "Growl"));
        var cheryl = new Trainer { Id = "cheryl", Name = "Cheryl", TrainerClass = "Pokémon Trainer", Ai = AiFlags.Basic };
        cheryl.Party.Add(Of(Gender.Female, "Chansey", 60, "Pound"));
        var grunts = new List<Trainer>();
        foreach (string name in new[] { "A", "B" })
        {
            var grunt = new Trainer { Id = "grunt_" + name, Name = name, TrainerClass = "Galactic Grunt" };
            grunt.Party.Add(Of(Gender.Male, "Stunky", 50, "Slash"));
            grunts.Add(grunt);
        }
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = grunts, Partner = cheryl, Format = BattleFormat.Double, Random = Calm(), Rules = Ruleset.Platinum,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        Assert.True(core.IsDouble);
        Assert.Same(cheryl, core.PlayerSlots[1].Trainer);
        Assert.Equal("Chansey", core.PlayerSlots[1].Pokemon!.Species.Name);
        // Only the player's own place is asked: the partner chooses for itself
        var asked = Assert.IsType<ActionRequest>(core.Request).Places;
        Assert.Contains(Mine, asked);
        Assert.DoesNotContain(Mine2, asked);

        // The player's only Pokémon goes down: the battle is lost, whatever Cheryl has left
        party.Members[0].CurrentHP = 1;
        DoubleTurn(core, (Mine, 0, Foe), (Foe, 0, Mine), (Foe2, 0, Mine));
        Assert.Equal(BattleResult.PlayerDefeat, core.Result);
    }

    // ------------------------------------------------------------------ Platinum's trainers

    [Fact]
    public void ATrainersPokemonIsBuiltFromItsPersonality()
    {
        // TrainerData_BuildParty: IVs are the scale's share of 31, the nature the personality's remainder by 25,
        // the second ability its lowest bit
        var record = new TrainerPokemonRecord { Species = "Geodude", Level = 12, IvScale = 50, Personality = 7112584, Moves = new List<string> { "Stealth Rock", "Rock Throw" } };
        var geodude = TrainerDatabase.Build(record);
        Assert.Equal(50 * 31 / 255, geodude.IvAttack);
        Assert.Equal(50 * 31 / 255, geodude.IvSpeed);
        Assert.Equal((Nature)(7112584 % 25), geodude.Nature);
        Assert.Equal(geodude.Abilities[0], geodude.AbilityName);
        Assert.Equal(new[] { "Stealth Rock", "Rock Throw" }, geodude.Moves.Select(m => m.Data.Name));
        Assert.Equal(geodude.MaxHP, geodude.CurrentHP);
        Assert.False(geodude.IsShiny);

        // The gender: the personality's last byte against the species' share of females
        var starly = PokemonDatabase.Get("Starly")!;
        Assert.Equal(Gender.Female, TrainerDatabase.GenderOf(starly, 0x00));
        Assert.Equal(Gender.Male, TrainerDatabase.GenderOf(starly, 0xFF));
    }

    [Fact]
    public void EveryTrainerOfPlatinumCanBeBuilt()
    {
        foreach (var record in TrainerDatabase.All)
        {
            Assert.NotEmpty(record.Party);
            Assert.True(record.PrizeMoney >= 0, record.Id);
            var party = TrainerDatabase.PartyOf(record);
            Assert.Equal(record.Party.Count, party.Count);
        }
        Assert.True(TrainerDatabase.All.Count() > 900);
    }

    [Fact]
    public void AMapsTrainerTakesPlatinumsTeamAndMind()
    {
        // Youngster Tristan of Route 202: a level-5 Starly, the Basic flag
        var trainer = new Trainer { Id = "youngster_tristan", Name = "Tristan", TrainerClass = "Youngster" };
        TrainerDatabase.Fill(trainer, TrainerDatabase.Get("youngster_tristan")!);
        Assert.Equal(AiFlags.Basic, trainer.Ai);
        Assert.Equal("Starly", Assert.Single(trainer.Party.Members).Species.Name);
        Assert.Equal(80, trainer.PrizeMoney);
    }

    // ------------------------------------------------------------------ Pal Park's show

    [Fact]
    public void TheCatchingShowScoresItsCatches()
    {
        var six = new[] { "Bidoof", "Starly", "Wooper", "Skorupi", "Carnivine", "Tropius" }.Select(n => new Pokemon(PokemonDatabase.Get(n)!, 20)).ToList();
        var show = new CatchingShow(six, new System.Random(1));
        Assert.Equal(6, show.ParkBalls);
        Assert.Equal(30 + 30 + 50 + 50 + 70 + 70, show.CatchingPoints);

        // A Pokémon turns up in its own area within fourteen steps
        int met = -1;
        for (int step = 0; step < 200 && met < 0; step++) met = show.Step(CatchingShow.AreaOf(six[0].Species));
        Assert.True(met >= 0);
        Assert.Equal(CatchingShow.AreaOf(six[0].Species), CatchingShow.AreaOf(six[met].Species));
        show.CaughtCurrent();
        Assert.Equal(5, show.ParkBalls);
        // One catch: no bonus for a change of type, 50 for each of its types
        int types = six[met].SecondaryType is { } second && second != six[met].PrimaryType ? 2 : 1;
        Assert.Equal(types * 50, show.TypePoints);

        show.End(400);
        Assert.Equal(1200, show.TimePoints);
        Assert.Equal(show.CatchingPoints + show.TypePoints + 1200, show.Score);
    }
}
