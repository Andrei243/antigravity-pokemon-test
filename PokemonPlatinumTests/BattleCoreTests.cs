using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The battle's rules by themselves (plan 06 · R2): a <see cref="BattleCore"/> with no screen round it, asked and
/// answered in plain data, and the log it writes. Then the two halves together: what the screen's Pokémon are
/// while a turn is being shown.
/// </summary>
public class BattleCoreTests
{
    private static readonly Place Mine = new(BattleSide.Player, 0), Foe = new(BattleSide.Enemy, 0);

    /// <summary>A wild battle with no screen; both sides answer from outside unless a chooser is given.</summary>
    private static BattleCore Wild(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, IBattleController? foeChooser = null,
        IBattleController? myChooser = null, params Pokemon[] bench)
    {
        var party = new Party();
        party.Add(mine);
        foreach (var p in bench) party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = rolls ?? Steady(), Rules = Ruleset.Platinum,
            PlayerController = myChooser, EnemyController = foeChooser, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    private static List<string> Lines(IEnumerable<BattleEvent> log) => log.OfType<Said>().Select(s => s.Text).ToList();

    /// <summary>A log as text, to compare two of them (a line's attachments are lists, which records compare by identity).</summary>
    private static List<string> Written(IEnumerable<BattleEvent> log) => log.Select(e => e is Said s
        ? $"{s.Text} | {string.Join("; ", s.Shows)} | {string.Join("; ", s.OnImpact)}"
        : e.ToString()!).ToList();

    // ------------------------------------------------------------------ asked and answered

    [Fact]
    public void ABattleRunsWithNoScreenAndWritesDownWhatHappened()
    {
        var machamp = Mon("Machamp", 50, "Strength");
        var blastoise = Mon("Blastoise", 50);
        var core = Wild(machamp, blastoise);

        // The opening, up to the first thing it has to ask: a choice for each side's Pokémon
        Assert.Equal(new[] { "A wild Blastoise appeared!", "Go! Machamp!" }, Lines(core.TakeLog()));
        Assert.Equal(new[] { Mine, Foe }, Assert.IsType<ActionRequest>(core.Request).Places);
        Assert.Equal(BattleResult.None, core.Result);

        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));

        // The rules have taken the hit at once: 139 − 47 (see BattleScenarioTests)
        Assert.Equal(92, blastoise.CurrentHP);
        Assert.Equal(14, machamp.Moves[0].CurrentPP);
        Assert.Equal(1, core.Turn);

        // And the log says when it is to be seen: the lunge and the move's effect as the line appears, the damage
        // and its sound at the moment of impact
        var log = core.TakeLog();
        var used = log.OfType<Said>().Single(s => s.Text == "Machamp used Strength!");
        Assert.Contains(new Lunged(Mine, MoveCategory.Physical), used.Shows);
        Assert.Contains(used.Shows, e => e is MoveShown { Move: "Strength", Missed: false, Critical: false } shown && shown.From == Mine && shown.To == Foe);
        Assert.Equal(new BattleEvent[] { new Struck(Foe, 92, Hard: false), new HitSounded(false) }, used.OnImpact);
        Assert.IsType<ActionRequest>(core.Request);
    }

    [Fact]
    public void WhatIsAskedMustBeAnswered()
    {
        var core = Wild(Mon("Machamp", 50, "Strength"), Mon("Blastoise", 50));
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0)));
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Mine, 0)));
        Assert.Throws<InvalidOperationException>(() => core.Start());

        // Chosen for from inside, the opponent isn't asked about
        var alone = Wild(Mon("Machamp", 50, "Strength"), Mon("Blastoise", 50), foeChooser: TrainerAi.Instance);
        Assert.Equal(new[] { Mine }, Assert.IsType<ActionRequest>(alone.Request).Places);
        alone.Submit(BattleChoice.Fight(Mine, 0));
        Assert.Contains("Foe Blastoise used Idle!", Lines(alone.TakeLog()));
    }

    [Fact]
    public void TheSameSeedAndChoicesGiveTheSameLog()
    {
        // Both sides chosen for from inside: the whole battle is played by the time it has started
        List<string> Play(uint seed)
        {
            var core = Wild(Mon("Bibarel", 30, "Headbutt", "Water Gun", "Growl"), Mon("Staravia", 30, "Wing Attack", "Quick Attack", "Growl"),
                new BattleRandom(seed), TrainerAi.Instance, TrainerAi.Instance);
            Assert.Null(core.Request);
            Assert.NotEqual(BattleResult.None, core.Result);
            return Written(core.TakeLog());
        }

        var first = Play(20261005);
        Assert.Equal(first, Play(20261005));
        Assert.True(first.Count > 10);
        Assert.Contains(Enumerable.Range(1, 8), seed => !Play((uint)seed).SequenceEqual(first));
    }

    [Fact]
    public void ARecordedBattleReplaysTheSame()
    {
        // The game's own battle, played by its menus with nothing fixed but the seed: moves, a switch, a Potion,
        // a ball, a try at running, a U-turn (whose replacement is asked for in the middle of the turn), and
        // whoever faints replaced
        static (Party Mine, Pokemon Wild) Teams()
        {
            var party = new Party();
            party.Add(Mon("Bidoof", 2, "Tackle"));
            party.Add(Mon("Grotle", 24, "Razor Leaf", "Tackle", "U-turn"));
            party.Add(Mon("Luxio", 23, "Spark", "Bite", "U-turn"));
            return (party, Mon("Metang", 25, "Metal Claw", "Take Down"));
        }

        var (party, wild) = Teams();
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Potion")!, 3);
        bag.AddItem(ItemDatabase.Get("Poké Ball")!, 5);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(), WildPokemon = { wild },
            Random = new BattleRandom(20261005), Rules = Ruleset.Platinum
        });
        Settle(battle);

        int Answered() => battle.Core.Record!.Answers.Count;
        for (int step = 0, turn = 0; step < 80 && !battle.IsBattleOver; step++)
        {
            int before = Answered();
            int bench = Enumerable.Range(0, party.Count).FirstOrDefault(i => !party.Members[i].IsFainted && battle.PlayerSlots.All(b => b.Pokemon != party.Members[i]), -1);
            if (battle.IsChoosingReplacement) battle.SelectSwitch(bench);
            else switch (turn++ % 7)
            {
                case 1 when bench >= 0: battle.SelectSwitch(bench); break;
                case 2: battle.UseBagItem("Potion"); break;
                case 3: battle.SelectMove(2); break;
                case 4: battle.UseBagItem("Poké Ball"); break;
                case 5: battle.SelectMove(1); break;
                case 6: battle.SelectMainMenuOption(3); break;
                default: battle.SelectMove(0); break;
            }
            Settle(battle);

            // What a menu refused (a Potion at full HP) never reached the rules: a move instead
            if (Answered() == before && !battle.IsBattleOver)
            {
                battle.SelectMove(0);
                Settle(battle);
            }
        }
        Assert.True(battle.IsBattleOver);

        var record = battle.Core.Record!;
        Assert.Equal(20261005u, record.Seed);
        var answered = record.Answers.SelectMany(a => a).ToList();
        Assert.Superset(new HashSet<ChoiceKind> { ChoiceKind.Fight, ChoiceKind.Switch, ChoiceKind.Item }, answered.Select(c => c.Kind).ToHashSet());
        Assert.Contains(answered, c => c.Item == "Potion");
        // A request in the middle of a turn is an answer of the record like any other
        Assert.True(Lines(battle.Core.Log).Any(line => line.Contains("went back to")), string.Join(" / ", Lines(battle.Core.Log)));
        Assert.True(record.Answers.Count >= 6, $"{record.Answers.Count} answers");

        // Again with no screen at all: the same teams, a generator of the record's seed, the same answers
        BattleCore Again(BattleRecord from)
        {
            var (mine, foe) = Teams();
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = mine, WildPokemon = new List<Pokemon> { foe }, Random = new BattleRandom(from.Seed), Rules = Ruleset.Platinum,
                PlayerName = PokemonPlatinumEngine.Core.PlayerIdentity.Name
            });
            core.Replay(from);
            return core;
        }
        var again = Again(record);
        Assert.Equal(Written(battle.Core.Log), Written(again.Log));
        Assert.Equal(battle.Result, again.Result);
        Assert.Null(again.Request);

        // And the game's own Pokémon ended as the rules' did
        foreach (var (shown, replayed) in party.Members.Zip(again.PlayerParty.Members))
        {
            Assert.Equal((replayed.CurrentHP, replayed.Level, replayed.CurrentExp, replayed.Status), (shown.CurrentHP, shown.Level, shown.CurrentExp, shown.Status));
            Assert.Equal(replayed.Moves.Select(m => (m.Name, m.CurrentPP)), shown.Moves.Select(m => (m.Name, m.CurrentPP)));
        }
        Assert.Equal(again.WildPokemon[0].CurrentHP, wild.CurrentHP);

        // A record is plain data: written out and read back it is the same battle
        var restored = System.Text.Json.JsonSerializer.Deserialize<BattleRecord>(GameDataFiles.Serialize(record), GameDataFiles.Json)!;
        Assert.Equal(record.Seed, restored.Seed);
        Assert.Equal(record.Answers.Select(a => a.Count), restored.Answers.Select(a => a.Count));
        Assert.Equal(record.Answers.SelectMany(a => a), restored.Answers.SelectMany(a => a));
        Assert.Equal(Written(again.Log), Written(Again(restored).Log));

        // A replay is of its own seed, and of a battle that hasn't begun
        var (otherParty, otherWild) = Teams();
        var other = new BattleCore(new CoreSetup { PlayerParty = otherParty, WildPokemon = new List<Pokemon> { otherWild }, Random = new BattleRandom(7), Rules = Ruleset.Platinum });
        Assert.Throws<ArgumentException>(() => other.Replay(record));
        Assert.Throws<InvalidOperationException>(() => again.Replay(record));
    }

    [Fact]
    public void AFaintedPokemonsPlaceIsAskedFor()
    {
        var weak = Mon("Bidoof", 5, "Tackle");
        var strong = Mon("Machamp", 50, "Strength");
        var foe = Mon("Blastoise", 50, "Surf");
        var core = Wild(weak, foe, bench: strong);
        core.TakeLog();

        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var lines = Lines(core.TakeLog());
        Assert.Contains("Bidoof fainted!", lines);
        Assert.Equal(Mine, Assert.IsType<ReplacementRequest>(core.Request).Place);

        // Only a Pokémon that can fight and isn't out will do
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Switch(Mine, 0)));
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0)));

        core.Submit(BattleChoice.Switch(Mine, 1));
        var log = core.TakeLog();
        var go = log.OfType<Said>().Single(s => s.Text == "Go! Machamp!");
        Assert.Contains(go.Shows, e => e is Entered { FromBall: true } entered && entered.Place == Mine && entered.Pokemon == strong);
        Assert.Same(strong, core.PlayerSlots[0].Pokemon);
        Assert.IsType<ActionRequest>(core.Request);
    }

    [Fact]
    public void RandomBattlesAlwaysEndAndLeaveNothingOutOfRange()
    {
        // A small fuzz: teams of anything at any level, both sides chosen for by the same chooser
        var species = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        for (int battle = 0; battle < 150; battle++)
        {
            var dice = new Random(battle * 7919 + 3);
            Party Team()
            {
                var party = new Party();
                for (int i = 0; i < 3; i++) party.Add(new Pokemon(species[dice.Next(species.Count)], dice.Next(5, 80), dice));
                return party;
            }
            var mine = Team();
            var rival = new Trainer { Name = "Tester", TrainerClass = "Ace Trainer", Party = Team() };
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = mine, Trainers = new List<Trainer> { rival }, Random = new BattleRandom((uint)battle), Rules = Ruleset.Platinum,
                Format = battle % 3 == 0 ? BattleFormat.Double : BattleFormat.Single, PlayerController = TrainerAi.Instance
            });
            core.Start();

            Assert.Null(core.Request);
            Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.PlayerDefeat, $"battle {battle} ended as {core.Result}");
            Assert.True(core.Turn < 1000, $"battle {battle} took {core.Turn} turns");
            Assert.IsType<Ended>(core.TakeLog().Last());
            foreach (var p in mine.Members.Concat(rival.Party.Members))
            {
                Assert.InRange(p.CurrentHP, 0, p.MaxHP);
                Assert.All(p.Moves, m => Assert.InRange(m.CurrentPP, 0, m.MaxPP));
                Assert.All(p.StatStages.Values, stage => Assert.InRange(stage, -6, 6));
            }
            // One side has nobody left, and it is the one the result says
            bool mineDown = mine.Members.All(p => p.IsFainted), theirsDown = rival.Party.Members.All(p => p.IsFainted);
            Assert.True(core.Result == BattleResult.PlayerDefeat ? mineDown : theirsDown, $"battle {battle}: {core.Result}");
        }
    }

    // ------------------------------------------------------------------ who goes first

    [Fact]
    public void TheTurnsOrderIsPriorityThenQuickClawThenSpeedThenACoin()
    {
        List<string> Order(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, int myMove = 0)
        {
            var core = Wild(mine, foe, rolls);
            core.TakeLog();
            core.Submit(BattleChoice.Fight(Mine, myMove), BattleChoice.Fight(Foe, 0));
            return Lines(core.TakeLog()).Where(l => l.Contains(" used ")).ToList();
        }

        // Blastoise (Speed 83) before Machamp (60)
        Assert.Equal(new[] { "Foe Blastoise used Tackle!", "Machamp used Strength!" }, Order(Mon("Machamp", 50, "Strength", "Mach Punch"), Mon("Blastoise", 50, "Tackle")));
        // A move of higher priority goes first whatever the Speed
        Assert.Equal(new[] { "Machamp used Mach Punch!", "Foe Blastoise used Tackle!" }, Order(Mon("Machamp", 50, "Strength", "Mach Punch"), Mon("Blastoise", 50, "Tackle"), myMove: 1));

        // A Quick Claw works when the number its place drew for the turn leaves nothing over five
        Pokemon Clawed()
        {
            var p = Mon("Machamp", 50, "Strength");
            p.HeldItem = ItemDatabase.Get("Quick Claw");
            return p;
        }
        Assert.Equal("Machamp used Strength!", Order(Clawed(), Mon("Blastoise", 50, "Tackle"), Steady().Force(RollKind.Speed, 10))[0]);
        Assert.Equal("Foe Blastoise used Tackle!", Order(Clawed(), Mon("Blastoise", 50, "Tackle"), Steady().Force(RollKind.Speed, 11))[0]);

        // Paralysis quarters Speed: 83 / 4 is slower than 60
        var slowed = Mon("Blastoise", 50, "Tackle");
        slowed.Status = StatusCondition.Paralyze;
        Assert.Equal("Machamp used Strength!", Order(Mon("Machamp", 50, "Strength"), slowed, Steady().Force(RollKind.FullParalysis, 1))[0]);

        // Equal Speed: a coin, and on a 1 the second of the two goes first
        Assert.Equal("Bidoof used Tackle!", Order(Mon("Bidoof", 20, "Tackle"), Mon("Bidoof", 20, "Tackle"), Steady().Force(RollKind.SpeedTie, 0))[0]);
        Assert.Equal("Foe Bidoof used Tackle!", Order(Mon("Bidoof", 20, "Tackle"), Mon("Bidoof", 20, "Tackle"), Steady().Force(RollKind.SpeedTie, 1))[0]);
    }

    [Fact]
    public void SwitchesAndItemsComeBeforeMoves()
    {
        var core = Wild(Mon("Bidoof", 20, "Tackle"), Mon("Blastoise", 50, "Tackle"), bench: Mon("Machamp", 50, "Strength"));
        core.TakeLog();
        core.Submit(BattleChoice.Switch(Mine, 1), BattleChoice.Fight(Foe, 0));
        var lines = Lines(core.TakeLog());
        Assert.Equal(new[] { "Come back, Bidoof!", "Go! Machamp!", "Foe Blastoise used Tackle!" }, lines.Take(3));
        Assert.Equal("Machamp", core.PlayerSlots[0].Pokemon!.Species.Name);
    }

    // ------------------------------------------------------------------ running

    [Fact]
    public void GettingAwayGoesBySpeedAndEachFailedTryHelpsTheNext()
    {
        // Machamp's Speed is 60 and Blastoise's 83: 60 × 128 / 83 = 92, so a roll of 256 below 92 gets away
        BattleCore Running(int roll, Action<Pokemon>? prepare = null)
        {
            var machamp = Mon("Machamp", 50, "Strength");
            prepare?.Invoke(machamp);
            var core = Wild(machamp, Mon("Blastoise", 50, "Tackle"), Steady().Force(RollKind.Escape, roll));
            core.TakeLog();
            core.Submit(BattleChoice.Run(Mine), BattleChoice.Fight(Foe, 0));
            return core;
        }

        var away = Running(91);
        Assert.Equal(BattleResult.PlayerRan, away.Result);
        Assert.Equal(new[] { "Got away safely!" }, Lines(away.TakeLog()));
        Assert.Null(away.Request);

        // A try that fails costs the turn: the foe still moves
        var held = Running(92);
        Assert.Equal(BattleResult.None, held.Result);
        Assert.Equal(new[] { "Can't escape!", "Foe Blastoise used Tackle!" }, Lines(held.TakeLog()).Take(2));

        // The second try is worth 30 more (122): the roll that held it the first time lets it go
        held.Submit(BattleChoice.Run(Mine), BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleResult.PlayerRan, held.Result);

        // ...and a roll of 122 holds the second try too, but not the third (152)
        var stubborn = Wild(Mon("Machamp", 50, "Strength"), Mon("Blastoise", 50, "Tackle"), Steady().Force(RollKind.Escape, 255, 122));
        stubborn.Submit(BattleChoice.Run(Mine), BattleChoice.Fight(Foe, 0));
        stubborn.Submit(BattleChoice.Run(Mine), BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleResult.None, stubborn.Result);
        stubborn.Submit(BattleChoice.Run(Mine), BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleResult.PlayerRan, stubborn.Result);

        // A Pokémon as fast as the foe, one with Run Away and one holding a Smoke Ball always get away
        var fast = Wild(Mon("Alakazam", 50), Mon("Blastoise", 50), Steady().Force(RollKind.Escape, 255));
        fast.Submit(BattleChoice.Run(Mine), BattleChoice.Fight(Foe, 0));
        Assert.Equal(BattleResult.PlayerRan, fast.Result);
        Assert.Equal(BattleResult.PlayerRan, Running(255, p => p.AbilityName = "Run Away").Result);
        Assert.Equal(BattleResult.PlayerRan, Running(255, p => p.HeldItem = ItemDatabase.Get("Smoke Ball")).Result);
    }

    // ------------------------------------------------------------------ EXP and catching, through a battle

    [Fact]
    public void ExpGoesToThoseWhoFoughtAndToAnExpShare()
    {
        var fighter = Mon("Machamp", 50, "Strength");
        var holder = Mon("Bidoof", 5);
        holder.HeldItem = ItemDatabase.Get("Exp. Share");
        var idle = Mon("Starly", 5);
        var foe = Mon("Bidoof", 40);
        var core = Wild(fighter, foe, bench: new[] { holder, idle });
        core.TakeLog();

        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));

        // Bidoof's base EXP times its level over 7, halved between the one who fought and the one holding the Exp. Share
        var (fought, shared) = Formulas.ExpShares(foe.Species.BaseExpYield, 40, 1, 1);
        Assert.Equal(foe.Species.BaseExpYield * 40 / 7 / 2, fought);
        var log = core.TakeLog();
        var lines = Lines(log);
        Assert.Contains($"Machamp gained {fought} EXP. Points!", lines);
        Assert.Contains($"Bidoof gained {shared} EXP. Points!", lines);
        Assert.DoesNotContain(lines, l => l.StartsWith("Starly gained"));
        Assert.Equal(BattleResult.PlayerVictory, core.Result);

        // The one that rose a level is told of, and remembered for what may follow the battle
        Assert.Contains(log, e => e is LevelRose rose && rose.Pokemon == holder);
        Assert.Contains(holder, core.LeveledUp);
        Assert.True(holder.Level > 5);
    }

    [Fact]
    public void AThrownBallHoldsOrDoesntByItsFourRolls()
    {
        BattleCore Throw(params int[] rolls)
        {
            var wild = Mon("Bidoof", 10);
            var core = Wild(Mon("Machamp", 50, "Strength"), wild, Steady().Force(RollKind.CatchShake, rolls));
            core.TakeLog();
            core.Submit(BattleChoice.UseItem(Mine, "Poké Ball"), BattleChoice.Fight(Foe, 0));
            return core;
        }

        // Every roll below the shake rate: caught, and the Pokémon is the player's
        var caught = Throw(0);
        Assert.Equal(BattleResult.EnemyCaught, caught.Result);
        var log = caught.TakeLog();
        Assert.Contains(log, e => e is BallThrown { Ball: "Poké Ball", Shakes: 4 });
        Assert.Contains(log, e => e is Caught { ToBox: false });
        Assert.Equal(new[] { "Lucas used one Poké Ball!", "Gotcha! Bidoof was caught!" }, Lines(log));
        Assert.Equal(2, caught.PlayerParty.Count);
        Assert.Equal("Poké Ball", caught.PlayerParty.Members[1].Ball);

        // The third roll too high: two shakes, and the turn goes on
        var free = Throw(0, 0, 65535);
        Assert.Equal(BattleResult.None, free.Result);
        var freeLog = free.TakeLog();
        Assert.Contains(freeLog, e => e is BallThrown { Shakes: 2 });
        Assert.Equal(new[] { "Lucas used one Poké Ball!", "Aargh! Almost had it!", "Foe Bidoof used Idle!" }, Lines(freeLog).Take(3));
    }

    // ------------------------------------------------------------------ the rules' copies and the screen's Pokémon

    [Fact]
    public void ACopyTakesOnEverythingThatCanChange()
    {
        var p = new Pokemon(PokemonDatabase.Get("Turtwig")!, 17, new Random(5));
        var copy = p.Clone();
        AssertSame(p, copy);

        // The copy's moves, stages and counters are its own
        copy.Moves[0].CurrentPP = 1;
        copy.StatStages[StatType.Attack] = 3;
        copy.EvolutionProgress["steps"] = 9;
        Assert.NotEqual(1, p.Moves[0].CurrentPP);
        Assert.NotEqual(3, p.StatStages.GetValueOrDefault(StatType.Attack));
        Assert.Empty(p.EvolutionProgress);

        // Change everything a battle and the field can change, then take it all over
        p.GainExp(40000, out _);
        p.EvolveInto(PokemonDatabase.Get("Grotle")!);
        p.CurrentHP = 7;
        p.Status = StatusCondition.Toxic;
        p.ToxicCounter = 3;
        p.SleepTurns = 2;
        p.StatStages[StatType.Speed] = -2;
        p.HeldItem = ItemDatabase.Get("Leftovers");
        p.AbilityName = "Shell Armor";
        p.Friendship = 201;
        p.Beauty = 12;
        p.Cool = 3;
        p.Cute = 4;
        p.Smart = 5;
        p.Tough = 6;
        p.Sheen = 77;
        p.Ball = "Dusk Ball";
        p.Nickname = "Moss";
        p.EvHP = 9;
        p.EvSpeed = 31;
        p.IvAttack = 30;
        p.Gender = Gender.Female;
        p.Nature = Nature.Jolly;
        p.IsShiny = true;
        p.Personality = 0xBEEF;
        p.IsEgg = true;
        p.EvolutionProgress["uses:Rage Fist"] = 4;
        p.ReplaceMove(0, "Razor Leaf");
        p.Moves[1].CurrentPP = 2;
        var keptMove = copy.Moves[1];

        copy.CopyStateFrom(p);
        AssertSame(p, copy);
        // A move that is still the same move is still the same object, with the other's PP
        if (p.Moves[1].Data == keptMove.Data) Assert.Same(keptMove, copy.Moves[1]);

        static void AssertSame(Pokemon a, Pokemon b)
        {
            foreach (var property in typeof(Pokemon).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                object? x = property.GetValue(a), y = property.GetValue(b);
                switch (x)
                {
                    case List<Move> moves:
                        Assert.Equal(moves.Select(m => (m.Data, m.CurrentPP)), ((List<Move>)y!).Select(m => (m.Data, m.CurrentPP)));
                        Assert.NotSame(x, y);
                        break;
                    case IDictionary dictionary:
                        Assert.Equal(Entries(dictionary), Entries((IDictionary)y!));
                        Assert.NotSame(x, y);
                        break;
                    default:
                        Assert.True(Equals(x, y), $"{property.Name}: {x} and {y}");
                        break;
                }
            }
        }

        static List<string> Entries(IDictionary dictionary)
        {
            var entries = new List<string>();
            var each = dictionary.GetEnumerator();
            while (each.MoveNext()) entries.Add($"{each.Key} = {each.Value}");
            entries.Sort(StringComparer.Ordinal);
            return entries;
        }
    }

    [Fact]
    public void TheScreensPokemonAreBehindTheRulesWhileATurnIsShown()
    {
        var machamp = Mon("Machamp", 50, "Strength");
        var blastoise = Mon("Blastoise", 50);
        var battle = Battle(machamp, blastoise);
        var theirs = battle.Core.EnemySlots[0].Pokemon!;
        Assert.NotSame(blastoise, theirs);
        Assert.Same(blastoise, battle.EnemyPokemon);

        battle.SelectMove(0);

        // The rules have played the whole turn; the screen has only begun to show it
        Assert.Equal(92, theirs.CurrentHP);
        Assert.Equal(139, blastoise.CurrentHP);
        Assert.Equal(15, machamp.Moves[0].CurrentPP);

        // The hit is seen when its line is on screen and the move has landed
        for (int i = 0; i < 20 && battle.CurrentMessage != "Machamp used Strength!"; i++) battle.ConfirmMessage();
        Assert.Equal("Machamp used Strength!", battle.CurrentMessage);
        Assert.Equal(139, blastoise.CurrentHP);
        battle.Update(0.2f);
        Assert.Equal(139, blastoise.CurrentHP);
        battle.Update(0.2f);
        Assert.Equal(92, blastoise.CurrentHP);

        // Once the menu is open again the two are the same in everything
        Settle(battle);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Equal(14, machamp.Moves[0].CurrentPP);
        Assert.Equal(theirs.CurrentHP, blastoise.CurrentHP);

        // And what is done to the screen's Pokémon between turns, the rules take over before they play the next
        blastoise.Status = StatusCondition.Burn;
        battle.PlayerSlots[0].ConfusionTurns = 2;
        battle.SelectMove(0);
        Assert.Equal(StatusCondition.Burn, theirs.Status);
        Assert.True(theirs.CurrentHP < 92 - 139 / 8 + 1);
        var said = Settle(battle);
        Assert.Contains("Foe Blastoise is hurt by its burn!", said);
        Assert.Contains(said, l => l.Contains("confus"));
    }

    [Fact]
    public void TheGamesOwnBattlesRollWithPlatinumsGenerator()
    {
        var party = new Party();
        party.Add(Mon("Bidoof", 5));
        var battle = new BattleEngine(party, Mon("Starly", 5), new Inventory(), new Pokedex());
        Assert.IsType<BattleRandom>(battle.Random);
        Assert.Same(battle.Core.Random, battle.Random);
    }
}
