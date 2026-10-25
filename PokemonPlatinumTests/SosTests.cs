using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 14 · B5: SOS calls (<c>docs/mechanics/rulings.md</c>, "SOS calls"). A wild Pokémon of an SOS battle may spend its
/// turn calling for help, and an ally that answers fills the empty place beside it: the call and its chance, the answer,
/// the ally's species and the chain's bonus, never a call at a full place, nor from a trainer's Pokémon, nor under
/// Platinum's rules; the ball refused while two stand, the EXP of each faint, the chain going on after the caller falls,
/// a record and its replay, a fuzz of SOS battles, and on the screen the ally's hop, the side opening out and the Pokédex.
/// </summary>
public class SosTests
{
    /// <summary>An SOS battle on the bare core, both sides answered for from outside, under the modern rules unless others are handed.</summary>
    private static BattleCore Sos(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, Ruleset? rules = null, IBattleController? enemy = null)
    {
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Format = BattleFormat.Sos, Random = rolls ?? Calm(), Rules = rules ?? Ruleset.Modern,
            PlayerController = null, EnemyController = enemy, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    /// <summary>A turn of an SOS battle: the player's move at the target given, every wild Pokémon asked using its move in <paramref name="foes"/>' place.</summary>
    private static List<string> SosTurn(BattleCore core, int mine = 0, Place? at = null, int foes = 0)
    {
        var asked = Assert.IsType<ActionRequest>(core.Request).Places;
        core.Submit(asked.Select(p => p.Side == BattleSide.Player ? BattleChoice.Fight(p, mine, at) : BattleChoice.Fight(p, foes)).ToList());
        return Lines(core.TakeLog());
    }

    private static Pokemon Hurt(Pokemon p, int share)
    {
        p.CurrentHP = Math.Max(1, p.MaxHP / share);
        return p;
    }

    // ------------------------------------------------------------------ the format

    [Fact]
    public void AnSosBattleIsOnlyAWildPokemonMetAloneUnderRulesThatHaveCalls()
    {
        BattleFormat Fall(int trainers, int wild, bool partner, BattleKind kind, Ruleset rules) =>
            BattleFormats.FallBack(BattleFormat.Sos, 3, partner ? 1 : 0, partner, trainers, trainers > 0 ? 3 : 0, wild, kind, rules, out _);
        Assert.Equal(BattleFormat.Sos, Fall(0, 1, false, BattleKind.Normal, Ruleset.Modern));
        // Platinum's rules have no calls: the single it began as, so battles-platinum.txt can't move for it
        Assert.Equal(BattleFormat.Single, Fall(0, 1, false, BattleKind.Normal, Ruleset.Platinum));
        // A trainer's Pokémon never calls; nor two wild ones met at once, nor beside a partner, nor in the Great Marsh or Pal Park, nor a roamer
        Assert.Equal(BattleFormat.Single, Fall(1, 0, false, BattleKind.Normal, Ruleset.Modern));
        Assert.Equal(BattleFormat.Single, Fall(0, 2, false, BattleKind.Normal, Ruleset.Modern));
        Assert.NotEqual(BattleFormat.Sos, Fall(0, 1, true, BattleKind.Normal, Ruleset.Modern));
        Assert.Equal(BattleFormat.Single, Fall(0, 1, false, BattleKind.Safari, Ruleset.Modern));
        Assert.Equal(BattleFormat.Single, Fall(0, 1, false, BattleKind.PalPark, Ruleset.Modern));
        Assert.Equal(BattleFormat.Single, Fall(0, 1, false, BattleKind.Roamer, Ruleset.Modern));
        Assert.Equal(1, BattleFormat.Sos.PlacesOf(BattleSide.Player));
        Assert.Equal(2, BattleFormat.Sos.PlacesOf(BattleSide.Enemy));
        Assert.True(Ruleset.Modern.SosCalls);
        Assert.False(Ruleset.Platinum.SosCalls);

        // On the core: one against one, the second place empty and asked nothing
        var core = Sos(Mon("Snorlax", 50), Mon("Pikipek", 10, "Peck"));
        Assert.True(core.IsSos);
        Assert.Equal(2, core.EnemySlots.Count);
        Assert.False(core.EnemySlots[1].IsActive);
        Assert.Equal(new[] { Mine, Foe }, Assert.IsType<ActionRequest>(core.Request).Places.ToArray());
        Assert.Contains(Text.Of("battle.aWildAppeared", ("pokemon", "Pikipek")), Lines(core.Log));
    }

    [Fact]
    public void TheChanceToCallRisesAsItIsHurtAndTheAnswerIsFourTimesTheRate()
    {
        // Sun and Moon's numbers: the rate, three times at half the HP or less, five at a fifth or less
        Assert.Equal(15, BattleCore.CallChance(15, 100, 100, false, false));
        Assert.Equal(15, BattleCore.CallChance(15, 51, 100, false, false));
        Assert.Equal(45, BattleCore.CallChance(15, 50, 100, false, false));
        Assert.Equal(45, BattleCore.CallChance(15, 21, 100, false, false));
        Assert.Equal(75, BattleCore.CallChance(15, 20, 100, false, false));
        // Twice when an ally fell, twice again by the player's Intimidate, Unnerve or Pressure, and never past a hundred
        Assert.Equal(18, BattleCore.CallChance(9, 100, 100, true, false));
        Assert.Equal(36, BattleCore.CallChance(9, 100, 100, true, true));
        Assert.Equal(100, BattleCore.CallChance(15, 1, 100, true, true));
        Assert.Equal(0, BattleCore.CallChance(0, 1, 100, true, true));
        Assert.Equal(60, BattleCore.AnswerChance(15));
        Assert.Equal(12, BattleCore.AnswerChance(3));
        Assert.Equal(new[] { "Intimidate", "Unnerve", "Pressure" }, BattleCore.CallingAbilities);
    }

    [Fact]
    public void TheRatesAndTheAlliesAreTheRulesAndTheFiles()
    {
        // The rule by catch rate, none for a legendary, a mythical or an Ultra Beast
        Assert.Equal(15, SosAllies.RateOf(PokemonDatabase.Get("Pikipek")!));
        Assert.Equal(15, SosAllies.RateOf(PokemonDatabase.Get("Rattata")!));
        Assert.Equal(9, SosAllies.RateOf(PokemonDatabase.Get("Graveler")!));
        Assert.Equal(6, SosAllies.RateOf(PokemonDatabase.Get("Toucannon")!));
        Assert.Equal(0, SosAllies.RateOf(PokemonDatabase.Get("Mewtwo")!));
        Assert.Equal(0, SosAllies.RateOf(PokemonDatabase.Get("Mew")!));
        Assert.Equal(0, SosAllies.RateOf(PokemonDatabase.Get("Nihilego")!));
        // Every species the file names is one of the data's
        Assert.All(SosAllies.File.Rates.Keys.Concat(SosAllies.File.Pairs.Keys).Concat(SosAllies.File.Pairs.Values.SelectMany(v => v)),
            name => Assert.NotNull(PokemonDatabase.Get(name)));

        static string[] Names(string caller, int level) => SosAllies.Candidates(PokemonDatabase.Get(caller)!, level).Select(c => c.Species.Name).ToArray();
        // Its own line as far as the ally's level reaches by level alone, the caller's own the likeliest
        Assert.Equal(new[] { "Pikipek" }, Names("Pikipek", 10));
        Assert.Equal(new[] { "Pikipek", "Trumbeak" }, Names("Pikipek", 14));
        Assert.Equal(new[] { "Trumbeak", "Pikipek" }, Names("Trumbeak", 20));
        Assert.Equal(SosAllies.OwnWeight, SosAllies.Candidates(PokemonDatabase.Get("Pikipek")!, 30)[0].Weight);
        // What only a stone or friendship brings never answers but as the caller's own: Pikachu calls Pichu, never Raichu
        Assert.Equal(new[] { "Pikachu", "Pichu" }, Names("Pikachu", 60));
        // And the file's pairs: Cubone calls Kangaskhan, Marowak only from level 28
        Assert.Equal(new[] { "Cubone", "Kangaskhan" }, Names("Cubone", 20));
        Assert.Equal(new[] { "Cubone", "Marowak", "Kangaskhan" }, Names("Cubone", 30));
    }

    [Fact]
    public void ThePlacesOpenOutAsTheAllyComesAndItsHopLands()
    {
        // Opened out all the way, a side stands where it always did, to the bit; gathered, both places are its middle
        for (int slot = 0; slot < 2; slot++)
        {
            Assert.Equal(BattlePlaces.Spot(BattleSide.Enemy, slot, 2), BattlePlaces.Spot(BattleSide.Enemy, slot, 2, 1f));
            Assert.Equal(BattleStage.EnemySpot, BattlePlaces.Spot(BattleSide.Enemy, slot, 2, 0f));
        }
        Assert.Equal(BattlePlaces.Fit(2), BattlePlaces.Fit(2, 1f));
        Assert.Equal(1f, BattlePlaces.Fit(2, 0f));
        Assert.InRange(BattlePlaces.Fit(2, 0.5f), BattlePlaces.Fit(2), 1f);

        var start = BattleAllies.Hop(0f);
        Assert.Equal(1f, start.Back);
        Assert.Equal(0f, start.Height);
        var top = BattleAllies.Hop(BattleAnimator.ArriveTime / 2f);
        Assert.Equal(1f, top.Height, 3);
        var landed = BattleAllies.Hop(BattleAnimator.ArriveTime);
        Assert.Equal(0f, landed.Back);
        Assert.Equal(0f, landed.Height);
        Assert.Equal(1f, landed.Grow);

        // The animator: an SOS battle's wild side gathered until the first ally's hop opens it out; every other side open
        var anim = new BattleAnimator();
        anim.SetSlots(1, 2);
        Assert.Equal(1f, anim.Spread(BattleSide.Enemy));
        anim.Gather(BattleSide.Enemy);
        Assert.Equal(0f, anim.Spread(BattleSide.Enemy));
        Assert.Equal(1f, anim.Spread(BattleSide.Player));
        anim.Arrive(BattleSide.Enemy, Mon("Pikipek", 10), 1);
        Assert.Equal(0f, anim[BattleSide.Enemy, 1].ArriveAge);
        anim.Update(BattleAnimator.ArriveTime / 2f, (_, _) => null);
        Assert.InRange(anim.Spread(BattleSide.Enemy), 0.4f, 0.6f);
        anim.Update(BattleAnimator.ArriveTime, (_, _) => null);
        Assert.Equal(1f, anim.Spread(BattleSide.Enemy));
        Assert.True(anim[BattleSide.Enemy, 1].ArriveAge < 0f);
        Assert.True(anim[BattleSide.Enemy, 1].Present);
    }

    // ------------------------------------------------------------------ the call

    [Fact]
    public void AHurtPokemonCallsAndItsAllyFillsTheEmptyPlace()
    {
        var pikipek = Hurt(Mon("Pikipek", 10, "Peck"), 4);
        // The call (under 45: it is at a quarter of its HP) and the answer (under 60), the ally's level 10 less nought
        var core = Sos(Mon("Snorlax", 50), pikipek, Calm().Force(RollKind.Call, 0, 0).Force(RollKind.Ally, 0));
        string caller = core.EnemySlots[0].Name;
        core.TakeLog();
        var said = SosTurn(core);

        InOrder(said, Text.Of("battle.calledForHelp", ("pokemon", caller)), Text.Of("battle.wildAppeared", ("pokemon", "Pikipek")));
        // The call was its action: it used no move
        Assert.DoesNotContain(said, s => s.Contains("Peck"));
        var ally = core.EnemySlots[1];
        Assert.True(ally.IsActive);
        Assert.Equal("Pikipek", ally.Pokemon!.Species.Name);
        Assert.Equal(10, ally.Pokemon.Level);
        Assert.Equal(ally.Pokemon.MaxHP, ally.Pokemon.CurrentHP);
        Assert.NotSame(pikipek, ally.Pokemon);
        Assert.Equal(1, core.Field.SosChain);

        // The log: the call on its line, the ally taken in, then its line with its entry out of no ball and the Pokédex told
        var log = core.Log;
        var call = log.OfType<Said>().Single(s => s.Shows.OfType<CalledForHelp>().Any());
        Assert.True(call.Shows.OfType<CalledForHelp>().Single().Answered);
        var joined = Assert.Single(log.OfType<AllyJoined>());
        Assert.Same(ally.Pokemon, joined.Pokemon);
        Assert.Equal(Foe2, joined.Place);
        Assert.Equal(1, joined.Chain);
        Assert.NotSame(joined.Pokemon, joined.AsItCame);
        var line = log.OfType<Said>().Single(s => s.Shows.OfType<Entered>().Any(e => e.Place == Foe2));
        Assert.False(line.Shows.OfType<Entered>().Single().FromBall);
        Assert.Contains(line.Shows.OfType<Seen>(), s => s.Species.Name == "Pikipek");
        Assert.True(log.ToList().IndexOf(joined) < log.ToList().IndexOf(line));

        // Next turn both are asked for, and with both places full nobody calls: they move
        Assert.Equal(new[] { Mine, Foe, Foe2 }, Assert.IsType<ActionRequest>(core.Request).Places.ToArray());
        said = SosTurn(core);
        Assert.DoesNotContain(said, s => s == Text.Of("battle.calledForHelp", ("pokemon", caller)));
        Assert.Equal(2, said.Count(s => s.Contains("Peck")));
        Assert.Equal(1, core.Field.SosChain);
    }

    [Fact]
    public void ACallUnderItsChanceIsMadeAndOneOverItIsNot()
    {
        // At full HP a Pikipek calls under 15 and moves at 15 and over
        var calls = Sos(Mon("Snorlax", 50), Mon("Pikipek", 10, "Peck"), Calm().Force(RollKind.Call, 14, 99));
        var said = SosTurn(calls);
        Assert.Contains(Text.Of("battle.itsHelpDidntAppear"), said);
        Assert.DoesNotContain(said, s => s.Contains("Peck"));
        // Unanswered: the place stays empty and the chain is none
        Assert.False(calls.EnemySlots[1].IsActive);
        Assert.Equal(0, calls.Field.SosChain);
        Assert.False(calls.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<CalledForHelp>().Single().Answered);

        var moves = Sos(Mon("Snorlax", 50), Mon("Pikipek", 10, "Peck"), Calm().Force(RollKind.Call, 15));
        said = SosTurn(moves);
        Assert.Contains(said, s => s.Contains("Peck"));
        Assert.DoesNotContain(said, s => s.Contains(Text.Of("battle.itsHelpDidntAppear")));

        // The player's Intimidate doubles it: 15 is now under 30
        var intimidated = Sos(With(Mon("Snorlax", 50), ability: "Intimidate"), Mon("Pikipek", 10, "Peck"), Calm().Force(RollKind.Call, 29, 99));
        Assert.Contains(Text.Of("battle.itsHelpDidntAppear"), SosTurn(intimidated));
    }

    [Fact]
    public void NobodyCallsWhereNothingCanAnswer()
    {
        // A legendary never calls, whatever the roll
        var mew = Sos(Mon("Snorlax", 50), Hurt(Mon("Mew", 30, "Pound"), 10), Calm().Force(RollKind.Call, 0));
        Assert.DoesNotContain(SosTurn(mew), s => s.Contains(Text.Of("battle.itsHelpDidntAppear")) || s.EndsWith("called for help!"));

        // Nor a Pokémon asleep
        var sleeper = Hurt(Mon("Pikipek", 10, "Peck"), 10);
        sleeper.Status = StatusCondition.Sleep;
        sleeper.SleepTurns = 5;
        var asleep = Sos(Mon("Snorlax", 50), sleeper, Calm().Force(RollKind.Call, 0));
        Assert.Empty(asleep.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<CalledForHelp>());
        SosTurn(asleep);
        Assert.Empty(asleep.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<CalledForHelp>());

        // Nor under Platinum's rules: the battle is a single and the Pikipek moves
        var platinum = Sos(Mon("Snorlax", 50), Hurt(Mon("Pikipek", 10, "Peck"), 10), Calm().Force(RollKind.Call, 0), Ruleset.Platinum);
        Assert.Equal(BattleFormat.Single, platinum.Format);
        Assert.Contains(SosTurn(platinum), s => s.Contains("Peck"));

        // Nor a trainer's Pokémon, asked for an SOS battle all the same
        var party = new Party();
        party.Add(Mon("Snorlax", 50));
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100 };
        trainer.Party.Add(Hurt(Mon("Pikipek", 10, "Peck"), 10));
        var against = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Format = BattleFormat.Sos, Random = Calm().Force(RollKind.Call, 0),
            Rules = Ruleset.Modern, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        against.Start();
        Assert.Equal(BattleFormat.Single, against.Format);
        Assert.Contains(Turn(against), s => s.Contains("Peck"));
    }

    // ------------------------------------------------------------------ two of them

    [Fact]
    public void TheBallIsRefusedWhileTwoStandAndEachFaintGivesItsExpAndTheChainGoesOn()
    {
        // The Golem waits a turn (its second move does nothing) while the Pikipek calls and is answered
        var golem = Idling(Mon("Golem", 60, "Rock Slide"));
        var core = Sos(golem, Hurt(Mon("Pikipek", 10, "Peck"), 4), Calm().Force(RollKind.Call, 0, 0).Force(RollKind.Ally, 0));
        var ball = BattleChoice.UseItem(Mine, "Poké Ball");
        Assert.Null(core.WhyNot(ball));
        SosTurn(core, mine: 1);
        Assert.True(core.EnemySlots[1].IsActive, "the ally came");
        Assert.Equal(Text.Of("battle.thereAreTwoPokemon"), core.WhyNot(ball));

        // Rock Slide fells both; the EXP of each is given as it falls, before the next one's fall is told
        foreach (var b in core.EnemySlots) b.Pokemon!.CurrentHP = 1;
        int before = core.Log.Count;
        SosTurn(core, mine: 0);
        int fallen = 0, given = 0;
        foreach (var e in core.Log.Skip(before))
        {
            if (e is Said said && said.Shows.OfType<Fainted>().Any(f => f.Place.Side == BattleSide.Enemy))
            {
                Assert.Equal(fallen, given);
                fallen++;
            }
            if (e is ExpGained exp && exp.Pokemon == golem) given++;
        }
        Assert.Equal(2, fallen);
        Assert.Equal(2, given);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
    }

    [Fact]
    public void WhenTheCallerFallsItsAllyCallsInItsPlaceAndALoneOneMayBeCaught()
    {
        var golem = Idling(Mon("Golem", 60, "Rock Throw"));
        var rolls = Calm().Force(RollKind.Call, 0, 0).Force(RollKind.Ally, 0);
        var core = Sos(golem, Hurt(Mon("Pikipek", 12, "Peck"), 4), rolls);
        SosTurn(core, mine: 1);
        Assert.True(core.EnemySlots[1].IsActive);
        Assert.Equal(1, core.Field.SosChain);

        // The caller falls to the faster Golem's Rock Throw; the ally stands, the battle goes on, and the ally, whose
        // place beside it is empty now, calls a second into the fallen one's place: the chain is two
        var ally = core.EnemySlots[1].Pokemon!;
        core.EnemySlots[0].Pokemon!.CurrentHP = 1;
        var said = SosTurn(core, mine: 0, at: Foe);
        Assert.Contains(Text.Of("battle.fainted", ("pokemon", Text.Of("battle.foe", ("pokemon", "Pikipek")))), said);
        Assert.Equal(BattleResult.None, core.Result);
        Assert.True(core.EnemySlots[1].IsActive);
        Assert.Same(ally, core.EnemySlots[1].Pokemon);
        Assert.True(core.EnemySlots[0].IsActive, "the ally called a second into the caller's place");
        Assert.Equal(2, core.Field.SosChain);
        Assert.Equal(2, core.Log.OfType<AllyJoined>().Count());

        // With no more calls, once one is left a ball may be aimed at it
        rolls.Force(RollKind.Call, 99);
        core.EnemySlots[0].Pokemon!.CurrentHP = 1;
        SosTurn(core, mine: 0, at: Foe);
        Assert.Equal(BattleResult.None, core.Result);
        Assert.Equal(1, core.EnemySlots.Count(b => b.IsActive));
        Assert.Null(core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
    }

    [Fact]
    public void AChainOfTenGivesTheIvsTheTableSays()
    {
        Assert.Equal(new SosChain.Bonus(0, 0, 0), SosChain.Of(1));
        Assert.Equal(new SosChain.Bonus(0, 0, 0), SosChain.Of(4));
        Assert.Equal(new SosChain.Bonus(1, 0, 0), SosChain.Of(5));
        Assert.Equal(new SosChain.Bonus(2, 5, 0), SosChain.Of(10));
        Assert.Equal(new SosChain.Bonus(2, 5, 4), SosChain.Of(11));
        Assert.Equal(new SosChain.Bonus(3, 10, 4), SosChain.Of(20));
        Assert.Equal(new SosChain.Bonus(3, 10, 8), SosChain.Of(21));
        Assert.Equal(new SosChain.Bonus(4, 15, 8), SosChain.Of(30));
        Assert.Equal(new SosChain.Bonus(4, 15, 12), SosChain.Of(31));
        Assert.Equal(SosChain.Of(SosChain.Longest), SosChain.Of(9999));

        // The same rolls make the same ally; nine calls before it, it is the tenth and two of its IVs are whole
        Pokemon AllyAt(int chainBefore)
        {
            var core = Sos(Mon("Snorlax", 50), Hurt(Mon("Pikipek", 10, "Peck"), 4), Calm().Force(RollKind.Call, 0, 0).Force(RollKind.Ally, 0, 0, 12345, 678));
            core.Field.SosChain = chainBefore;
            SosTurn(core);
            Assert.Equal(chainBefore + 1, core.Field.SosChain);
            return core.EnemySlots[1].Pokemon!;
        }
        static int[] Ivs(Pokemon p) => new[] { p.IvHP, p.IvAttack, p.IvDefense, p.IvSpAttack, p.IvSpDefense, p.IvSpeed };
        var first = Ivs(AllyAt(0));
        var tenth = Ivs(AllyAt(9));
        Assert.True(tenth.Count(iv => iv == 31) >= 2, $"the tenth's IVs: {string.Join(", ", tenth)}");
        // What changed is only stats made whole, two at most
        for (int i = 0; i < 6; i++) Assert.True(tenth[i] == first[i] || tenth[i] == 31);
        Assert.True(Enumerable.Range(0, 6).Count(i => tenth[i] != first[i]) <= 2);
    }

    // ------------------------------------------------------------------ records and the fuzz

    [Fact]
    public void ARecordedSosBattleReplaysTheSameAndOnlyAsOne()
    {
        BattleCore Fresh(BattleFormat format)
        {
            var party = new Party();
            party.Add(With(Idling(Mon("Snorlax", 40, "Tackle")), ability: "Intimidate"));
            return new BattleCore(new CoreSetup
            {
                PlayerParty = party, WildPokemon = new List<Pokemon> { Hurt(Mon("Rattata", 12, "Tackle", "Quick Attack"), 3) }, Format = format,
                Random = new BattleRandom(20261025), Rules = Ruleset.Modern, PlayerController = null, EnemyController = TrainerAi.Instance, PlayerName = "Lucas"
            });
        }
        var core = Fresh(BattleFormat.Sos);
        core.Start();
        for (int step = 0, turn = 0; step < 60 && core.Request != null; step++)
        {
            // The Snorlax waits five turns for calls, then tackles whoever its turn's number points to
            var at = core.EnemySlots.Where(b => b.IsActive).ToList();
            core.Submit(BattleChoice.Fight(Mine, turn < 5 ? 1 : 0, at[turn % at.Count].Place));
            turn++;
        }
        Assert.Contains(core.Log, e => e is Said s && s.Shows.OfType<CalledForHelp>().Any());
        var record = core.Record!;
        Assert.Equal(BattleFormat.Sos, record.Format);

        var again = Fresh(BattleFormat.Sos);
        again.Replay(record);
        Assert.Equal(Lines(core.Log), Lines(again.Log));
        Assert.Equal(core.Field.SosChain, again.Field.SosChain);
        // The same seed and answers in a single are another battle: refused
        Assert.Throws<ArgumentException>(() => Fresh(BattleFormat.Single).Replay(record));
    }

    [Fact]
    public void SixtySosBattlesAlwaysEndWithNeverMoreThanTwoFoes()
    {
        // Both sides chosen for by the trainer AI, the wild Pokémon of any species that calls, hurt to make it call
        var species = PokemonDatabase.GetAll().Where(s => SosAllies.RateOf(s) > 0).OrderBy(s => s.DexNumber).ToList();
        int calls = 0, answered = 0;
        for (int battle = 0; battle < 60; battle++)
        {
            var dice = new Random(battle * 7919 + 5);
            var mine = new Party();
            for (int i = 0; i < 2; i++) mine.Add(new Pokemon(species[dice.Next(species.Count)], dice.Next(20, 70), dice));
            var wild = new Pokemon(species[dice.Next(species.Count)], dice.Next(5, 50), dice);
            wild.CurrentHP = Math.Max(1, wild.MaxHP / 3);
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = mine, WildPokemon = new List<Pokemon> { wild }, Random = new BattleRandom((uint)battle), Rules = Ruleset.Modern,
                Format = BattleFormat.Sos, PlayerController = TrainerAi.Instance
            });
            Assert.Equal(BattleFormat.Sos, core.Format);
            core.Start();

            Assert.Null(core.Request);
            Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.PlayerDefeat, $"SOS battle {battle} ended as {core.Result}");
            Assert.True(core.Turn < 1000, $"SOS battle {battle} took {core.Turn} turns");
            var log = core.TakeLog().ToList();
            Assert.IsType<Ended>(log.Last());
            Assert.Equal(2, core.EnemySlots.Count);
            var called = log.OfType<Said>().SelectMany(s => s.Shows).OfType<CalledForHelp>().ToList();
            calls += called.Count;
            answered += called.Count(c => c.Answered);
            Assert.Equal(called.Count(c => c.Answered), log.OfType<AllyJoined>().Count());
            Assert.Equal(log.OfType<AllyJoined>().Count(), core.Field.SosChain);
            var foes = log.OfType<AllyJoined>().Select(j => j.Pokemon).Prepend(wild).ToList();
            foreach (var p in mine.Members.Concat(foes))
            {
                Assert.InRange(p.CurrentHP, 0, p.MaxHP);
                Assert.All(p.Moves, m => Assert.InRange(m.CurrentPP, 0, m.MaxPP));
            }
            if (core.Result == BattleResult.PlayerVictory) Assert.All(core.EnemySlots, b => Assert.False(b.IsActive));
        }
        Assert.True(answered > 0 && calls > answered, $"{calls} calls, {answered} answered");
    }

    // ------------------------------------------------------------------ the screen

    [Fact]
    public void OnTheScreenTheAllyHopsInBesideTheCallerAndThePokedexIsTold()
    {
        var party = new Party();
        party.Add(Mon("Snorlax", 50));
        var dex = new Pokedex();
        // Cubone at a quarter of its HP calls; its answer is Kangaskhan, the last of its three (Cubone 3, Kangaskhan 1)
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = dex,
            WildPokemon = new List<Pokemon> { Hurt(Mon("Cubone", 20, "Bone Club"), 4) }, Format = BattleFormat.Sos,
            Random = Steady().Force(RollKind.Call, 0, 0).Force(RollKind.Ally, 0, 3), Rules = Ruleset.Modern
        });
        Settle(battle);
        Assert.True(battle.IsSos);
        Assert.Equal(2, battle.Anim.SlotsOf(BattleSide.Enemy));
        Assert.Equal(0f, battle.Anim.Spread(BattleSide.Enemy));
        Assert.True(battle.Anim[BattleSide.Enemy, 0].Present);
        Assert.False(battle.Anim[BattleSide.Enemy, 1].Present);
        int kangaskhan = PokemonDatabase.Get("Kangaskhan")!.DexNumber;
        Assert.False(dex.IsSeen(kangaskhan));

        battle.SelectMove(0);
        bool hopped = false;
        for (int i = 0; i < 600 && !battle.IsBattleOver && battle.HUD.MenuState is BattleMenuState.Message or BattleMenuState.LearnMove; i++)
        {
            if (battle.IsWaitingForConfirm) battle.ConfirmMessage();
            battle.Update(1f / 60f);
            hopped |= battle.Anim[BattleSide.Enemy, 1].ArriveAge >= 0f;
        }
        Assert.True(hopped, "the ally hopped onto its place");
        Assert.True(dex.IsSeen(kangaskhan));
        Assert.Equal(1, battle.SosChain);
        Assert.Equal("Kangaskhan", battle.EnemySlots[1].Pokemon!.Species.Name);
        Assert.Same(battle.EnemySlots[1].Pokemon, battle.Anim[BattleSide.Enemy, 1].Shown);
        Assert.True(battle.Anim[BattleSide.Enemy, 1].Present);
        for (int i = 0; i < 120; i++) battle.Update(1f / 60f);
        Assert.Equal(1f, battle.Anim.Spread(BattleSide.Enemy));
        Assert.True(BattleHUD.BoxSlide(battle.Anim, battle.Anim[BattleSide.Enemy, 1]) >= 0f);
        // A move aimed at one asks which of the two
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
    }
}
