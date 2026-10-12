using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumTests.Golden;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// A condition that lasts on a Pokémon (plan 09 · L2): every line where a confusion or a love begins or ends carries
/// its event, and every send-out tells what the newcomer brings; a look that follows the log's events alone is what
/// the rules have at every menu of every battle, and never disagrees with a moment the turn plays; the screen's look
/// is set by the lines and not by the rules; a sleeper's eyes are shut and a frozen body is held; and every mark is
/// a pure function of its age and seed, under its cap, off the middle of the body.
/// </summary>
public class BattleConditionLookTests
{
    // ------------------------------------------------------------------ following a log

    /// <summary>What a moment of the turn says its Pokémon has, for the conditions that last: the look must have it too.</summary>
    private static string? Disagreement(MomentShown moment, ConditionLook look) => moment.Moment switch
    {
        BattleMoment.Asleep when look.Status != StatusCondition.Sleep => $"asleep, but shown {look.Status}",
        BattleMoment.Poisoned when look.Status is not (StatusCondition.Poison or StatusCondition.Toxic) => $"poisoned, but shown {look.Status}",
        BattleMoment.Burned when look.Status != StatusCondition.Burn => $"burned, but shown {look.Status}",
        BattleMoment.Frozen when look.Status != StatusCondition.Freeze => $"frozen, but shown {look.Status}",
        BattleMoment.Paralyzed when look.Status != StatusCondition.Paralyze => $"paralysed, but shown {look.Status}",
        BattleMoment.Confused when !look.Confused => "confused, but not shown so",
        BattleMoment.Infatuated when !look.InLove => "in love, but not shown so",
        _ => null
    };

    /// <summary>Looks that know the Pokémon's conditions only by what the log's events tell, as the screen does.</summary>
    private sealed class Follower
    {
        public readonly ConditionLooks Looks = new();
        public int Moments;
        private int read;

        /// <summary>
        /// Takes what the log has said since it last looked. Along the way every moment the turn plays must agree
        /// with what is shown at its line; at the end, which is a menu or the battle's end, the looks are held to
        /// the rules.
        /// </summary>
        public void Read(BattleCore core)
        {
            for (; read < core.Log.Count; read++)
            {
                if (core.Log[read] is not Said said)
                {
                    Looks.Tell(core.Log[read]);
                    continue;
                }
                foreach (var shown in said.Shows) Looks.Tell(shown);
                foreach (var moment in said.Shows.OfType<MomentShown>())
                {
                    Moments++;
                    string? wrong = Disagreement(moment, Looks.Of(moment.Place));
                    Assert.True(wrong == null, $"event {read}, \"{said.Text}\": the turn plays it as {wrong}");
                }
                foreach (var landing in said.OnImpact) Looks.Tell(landing);
            }
            Assert.True(Looks.Difference(core) == null, $"after {core.Log.Count} events: {Looks.Difference(core)}");
        }
    }

    private static bool OfACondition(BattleEvent happening) => happening is VolatileChanged or CameInWith;

    /// <summary>Every event of a passing condition or of what a newcomer brings, with the line it rides on (null for one that stands by itself).</summary>
    private static List<(string? Line, BattleEvent Event)> Told(BattleCore core)
    {
        var told = new List<(string?, BattleEvent)>();
        foreach (var happening in core.Log)
        {
            if (happening is Said said) told.AddRange(said.Shows.Concat(said.OnImpact).Where(OfACondition).Select(e => ((string?)said.Text, e)));
            else if (OfACondition(happening)) told.Add((null, happening));
        }
        return told;
    }

    /// <summary>The rules' battle with a follower that has read its opening.</summary>
    private static (BattleCore Core, Follower Follower) Followed(BattleCore core)
    {
        var follower = new Follower();
        follower.Read(core);
        return (core, follower);
    }

    private static void Play(BattleCore core, Follower follower, int mine = 0, int foe = 0)
    {
        Turn(core, mine, foe);
        follower.Read(core);
    }

    /// <summary>Confusion that lasts its longest and never makes its Pokémon hurt itself; nobody is held by love.</summary>
    private static BattleRandom Gentle() => Calm().Force(RollKind.ConfusionTurns, 3).Force(RollKind.ConfusionSelfHit, 1).Force(RollKind.Infatuation, 1);

    // ------------------------------------------------------------------ the rules' events, line by line

    [Theory]
    [InlineData("Confuse Ray")]
    [InlineData("Supersonic")]
    [InlineData("Sweet Kiss")]
    [InlineData("Teeter Dance")]
    [InlineData("Swagger")]
    [InlineData("Flatter")]
    public void AMoveThatConfusesTellsSoOnItsLine(string move)
    {
        Assert.Equal(move, MoveDatabase.Get(move).Name);
        var (core, follower) = Followed(Wild(Idling(Mon("Gengar", 50, move)), Mon("Snorlax", 50), Gentle()));
        Play(core, follower);
        Assert.Equal(new (string?, BattleEvent)[] { ("Foe Snorlax became confused!", new VolatileChanged(Foe, Confused: true, InLove: false)) }, Told(core));
        // Used again while it lasts, it fails and tells nothing more
        Play(core, follower);
        Assert.Single(Told(core));
    }

    [Fact]
    public void AConfusionRunningOutIsToldOfOnItsLine()
    {
        var (core, follower) = Followed(Wild(Idling(Mon("Gengar", 50, "Confuse Ray")), Mon("Snorlax", 50),
            Calm().Force(RollKind.ConfusionTurns, 0).Force(RollKind.ConfusionSelfHit, 1)));
        Play(core, follower);
        for (int turn = 0; turn < 8 && core.At(Foe).IsConfused; turn++) Play(core, follower, mine: 1);
        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Foe Snorlax became confused!", new VolatileChanged(Foe, true, false)),
            ("Foe Snorlax snapped out of confusion!", new VolatileChanged(Foe, false, false))
        }, Told(core));
    }

    [Fact]
    public void ARampagesEndConfusesOnItsOwnLine()
    {
        var (core, follower) = Followed(Wild(Mon("Dragonite", 50, "Outrage"), Mon("Steelix", 100), Gentle()));
        for (int turn = 0; turn < 6 && !core.At(Mine).IsConfused; turn++) Play(core, follower);
        var told = Assert.Single(Told(core));
        Assert.NotNull(told.Line);
        Assert.Equal(new VolatileChanged(Mine, true, false), told.Event);
    }

    [Fact]
    public void ABerryOfAFlavourItsNatureDislikesConfusesOnItsLine()
    {
        var sour = Enum.GetValues<Nature>().First(n => HeldItemEffects.DislikedBy(n) == Flavor.Spicy);
        var foe = new Pokemon(PokemonDatabase.Get("Snorlax")!, 50, Gender.Male, sour, false) { HeldItem = ItemDatabase.Get("Figy Berry") };
        foe.Moves.Clear();
        foe.Moves.Add(new Move(Idle));
        foe.CurrentHP = foe.MaxHP / 2 + 1;
        var (core, follower) = Followed(Wild(Mon("Machop", 20, "Tackle"), foe, Gentle()));
        Play(core, follower);
        var told = Assert.Single(Told(core));
        Assert.Equal(new VolatileChanged(Foe, true, false), told.Event);
        Assert.NotNull(told.Line);
    }

    [Fact]
    public void APersimBerryEndsAConfusionOnTheLineThatTellsOfIt()
    {
        var (core, follower) = Followed(Wild(Idling(Mon("Gengar", 50, "Confuse Ray")), With(Mon("Snorlax", 50), item: "Persim Berry"), Gentle()));
        Play(core, follower);
        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Foe Snorlax became confused!", new VolatileChanged(Foe, true, false)),
            ("Foe Snorlax's Persim Berry snapped it out of its confusion!", new VolatileChanged(Foe, false, false))
        }, Told(core));
    }

    [Fact]
    public void ALumBerryThatCuresAConfusionTellsOfItOnItsLine()
    {
        var (core, follower) = Followed(Wild(Idling(Mon("Gengar", 50, "Confuse Ray")), With(Mon("Snorlax", 50), item: "Lum Berry"), Gentle()));
        Play(core, follower);
        var told = Told(core);
        Assert.Equal(new VolatileChanged(Foe, true, false), told[0].Event);
        Assert.Equal(new VolatileChanged(Foe, false, false), told[1].Event);
        Assert.Contains("Lum Berry", told[1].Line);
        Assert.Equal(2, told.Count);
    }

    [Fact]
    public void AttractTellsOfTheLoveOnItsLine()
    {
        var (core, follower) = Followed(Wild(Idling(Of(Gender.Male, "Snorlax", 50, "Attract")), Of(Gender.Female, "Blissey", 50), Gentle()));
        Play(core, follower);
        Assert.Equal(new (string?, BattleEvent)[] { ("Foe Blissey fell in love!", new VolatileChanged(Foe, false, InLove: true)) }, Told(core));
        Play(core, follower);
        Assert.Single(Told(core));
    }

    [Fact]
    public void CuteCharmTellsOfTheLoveOnTheLineThatNamesIt()
    {
        var (core, follower) = Followed(Wild(Of(Gender.Male, "Machop", 20, "Tackle"), With(Of(Gender.Female, "Clefairy", 50), ability: "Cute Charm"),
            Gentle().Force(RollKind.AbilityChance, 0)));
        Play(core, follower);
        Assert.Equal(new (string?, BattleEvent)[] { ("Foe Clefairy's Cute Charm infatuated Machop!", new VolatileChanged(Mine, false, true)) }, Told(core));
    }

    [Fact]
    public void ADestinyKnotTellsOfTheSecondLoveOnItsOwnLine()
    {
        var (core, follower) = Followed(Wild(Idling(Of(Gender.Male, "Snorlax", 50, "Attract")), With(Of(Gender.Female, "Blissey", 50), item: "Destiny Knot"), Gentle()));
        Play(core, follower);
        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Foe Blissey fell in love!", new VolatileChanged(Foe, false, true)),
            ("Foe Blissey's Destiny Knot made Snorlax fall in love!", new VolatileChanged(Mine, false, true))
        }, Told(core));
    }

    [Fact]
    public void AMentalHerbEndsTheLoveOnItsLine()
    {
        var (core, follower) = Followed(Wild(Idling(Of(Gender.Male, "Snorlax", 50, "Attract")), With(Of(Gender.Female, "Blissey", 50), item: "Mental Herb"), Gentle()));
        Play(core, follower);
        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Foe Blissey fell in love!", new VolatileChanged(Foe, false, true)),
            ("Foe Blissey cured its infatuation using its Mental Herb!", new VolatileChanged(Foe, false, false))
        }, Told(core));
    }

    [Theory]
    [InlineData("Full Heal", "Confuse Ray", "Snorlax snapped out of its confusion!")]
    [InlineData("Red Flute", "Attract", "Snorlax got over its infatuation!")]
    public void AnItemFromTheBagEndsItOnItsLine(string item, string foesMove, string line)
    {
        var (core, follower) = Followed(Wild(Of(Gender.Male, "Snorlax", 50), Of(Gender.Female, "Gengar", 50, foesMove), Gentle()));
        Play(core, follower);
        Assert.Single(Told(core));
        core.Submit(BattleChoice.UseItem(Mine, item), BattleChoice.Fight(Foe, 0));
        // (The foe gives it again in the same turn, on its own line)
        Assert.Equal((line, new VolatileChanged(Mine, false, false)), Told(core)[1]);
        follower.Read(core);
    }

    [Fact]
    public void TheOneLovedCalledBackEndsTheLoveWithAnEventByItself()
    {
        var (core, follower) = Followed(Wild(Idling(Of(Gender.Male, "Snorlax", 50, "Attract")), Of(Gender.Female, "Blissey", 50), Gentle(), bench: Mon("Bidoof", 20)));
        Play(core, follower);
        SwitchTo(core, 1);
        follower.Read(core);
        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Foe Blissey fell in love!", new VolatileChanged(Foe, false, true)),
            (null, new VolatileChanged(Foe, false, false))
        }, Told(core));
    }

    [Fact]
    public void TheOneLovedFaintingEndsTheLoveOnTheLineOfItsFall()
    {
        var (core, follower) = Followed(Wild(Of(Gender.Male, "Ninjask", 40, "Attract"), Of(Gender.Female, "Machamp", 70, "Strength"), Gentle()));
        Turn(core);
        follower.Read(core);
        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Foe Machamp fell in love!", new VolatileChanged(Foe, false, true)),
            ("Ninjask fainted!", new VolatileChanged(Foe, false, false))
        }, Told(core));
    }

    [Fact]
    public void AConfusionHandedOnByBatonPassComesInOnTheSendOutsLine()
    {
        var (core, follower) = Followed(Wild(Idling(Mon("Ninjask", 50, "Baton Pass")), Mon("Gengar", 50, "Confuse Ray"), Gentle(), bench: Mon("Snorlax", 50)));
        Play(core, follower, mine: 1);
        Assert.True(core.At(Mine).IsConfused);

        core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        SendIn(core, 1);
        follower.Read(core);
        Assert.True(core.At(Mine).IsConfused);
        Assert.Contains(("Go! Snorlax!", (BattleEvent)new VolatileChanged(Mine, true, false)), Told(core));
    }

    [Fact]
    public void APokemonThatComesInWithAConditionSaysSoAsItTakesItsPlace()
    {
        var asleep = Mon("Snorlax", 50);
        asleep.Status = StatusCondition.Sleep;
        asleep.SleepTurns = 4;
        var poisoned = Mon("Bidoof", 20);
        poisoned.Status = StatusCondition.Poison;
        var (core, follower) = Followed(Wild(Mon("Turtwig", 20), asleep, bench: poisoned));

        // The wild Pokémon's stands beside its entry, by itself; the healthy lead brings nothing
        Assert.Equal(new (string?, BattleEvent)[] { (null, new CameInWith(Foe, StatusCondition.Sleep)) }, Told(core));
        int entry = core.Log.ToList().FindIndex(e => e is Entered entered && entered.Place == Foe);
        Assert.Equal(new CameInWith(Foe, StatusCondition.Sleep), core.Log[entry + 1]);

        SwitchTo(core, 1);
        follower.Read(core);
        Assert.Contains(("Go! Bidoof!", (BattleEvent)new CameInWith(Mine, StatusCondition.Poison)), Told(core));
        // Called back and sent out again healthy (a Natural Cure does it on the bench), it brings nothing
        Assert.Equal(2, Told(core).Count);
    }

    [Fact]
    public void ATrainersPokemonAndAReplacementTellWhatTheyBringOnTheirOwnLines()
    {
        var burned = Mon("Machop", 20);
        burned.Status = StatusCondition.Burn;
        var paralysed = Mon("Geodude", 20);
        paralysed.Status = StatusCondition.Paralyze;
        var frozen = Mon("Bidoof", 5);
        frozen.Status = StatusCondition.Freeze;
        var core = Against(new[] { frozen, paralysed }, new[] { burned, Mon("Snorlax", 50, "Strength") }, Calm().Force(RollKind.Thaw, 1));
        var follower = new Follower();
        follower.Read(core);
        var told = Told(core);
        Assert.Contains(told, t => t.Line!.Contains("sent out Machop") && t.Event.Equals(new CameInWith(Foe, StatusCondition.Burn)));
        Assert.Contains(("Go! Bidoof!", (BattleEvent)new CameInWith(Mine, StatusCondition.Freeze)), told);
    }

    // ------------------------------------------------------------------ the events are all the look needs

    [Fact]
    public void ALookThatFollowsTheEventsIsTheRulesAtTheEndOfEveryGoldenBattleAndAgreesWithEveryMomentOnTheWay()
    {
        // Two hundred battles of every kind, both sides chosen for from inside. Whatever gave, cured, passed on or
        // took away a condition anywhere in them was told of: or a moment of the turn (asleep, poison's bite, a
        // confusion as it tries to move) would play on a Pokémon that doesn't show it, or the looks would differ from
        // the rules' at the end
        int moments = 0, passing = 0, brought = 0;
        for (int seed = 1; seed <= BattleCorpus.Battles; seed++)
        {
            var battle = new BattleCorpus(seed);
            battle.Core.Start();
            var follower = new Follower();
            try
            {
                follower.Read(battle.Core);
            }
            catch (Xunit.Sdk.XunitException wrong)
            {
                Assert.Fail($"battle {seed}: {wrong.Message}");
            }
            moments += follower.Moments;
            var told = Told(battle.Core);
            passing += told.Count(t => t.Event is VolatileChanged);
            brought += told.Count(t => t.Event is CameInWith);
        }
        // The corpus does have conditions that last, begin, end and are carried off and on: the test would hold of nothing otherwise
        Assert.True(moments >= 250 && passing >= 60, $"{moments} moments checked, {passing} passing conditions told of, {brought} brought in");
    }

    private static readonly string[] ConditionMoves =
    {
        "Thunder Wave", "Toxic", "Poison Powder", "Will-O-Wisp", "Spore", "Hypnosis", "Yawn", "Confuse Ray", "Swagger", "Teeter Dance", "Attract",
        "Rest", "Heal Bell", "Aromatherapy", "Refresh", "Psycho Shift", "Baton Pass", "U-turn", "Roar", "Whirlwind", "Ice Beam", "Blizzard",
        "Thunder Punch", "Flame Wheel", "Outrage", "Thrash", "Uproar", "Sleep Talk", "Snore", "Facade", "Wake-Up Slap", "Substitute", "Healing Wish",
        "Captivate", "Tackle", "Protect", "Fling", "Bug Bite", "Tri Attack", "Dizzy Punch", "Sacred Fire", "Scald", "Safeguard", "Transform"
    };

    private static readonly (string Species, string[] Abilities)[] ConditionSpecies =
    {
        ("Snorlax", new[] { "Immunity", "Thick Fat" }), ("Blissey", new[] { "Natural Cure", "Serene Grace" }), ("Starmie", new[] { "Natural Cure", "Illuminate" }),
        ("Arbok", new[] { "Shed Skin", "Intimidate" }), ("Alakazam", new[] { "Synchronize", "Inner Focus" }), ("Clefable", new[] { "Cute Charm", "Magic Guard" }),
        ("Slowbro", new[] { "Own Tempo", "Oblivious" }), ("Breloom", new[] { "Poison Heal", "Effect Spore" }), ("Machamp", new[] { "Guts", "No Guard" }),
        ("Vaporeon", new[] { "Water Absorb" }), ("Dodrio", new[] { "Early Bird", "Run Away" }), ("Milotic", new[] { "Marvel Scale" }),
        ("Gengar", new[] { "Levitate" }), ("Ninjask", new[] { "Speed Boost" }), ("Roserade", new[] { "Poison Point", "Natural Cure" }),
        ("Magmortar", new[] { "Flame Body" }), ("Electivire", new[] { "Motor Drive" }), ("Lopunny", new[] { "Cute Charm", "Klutz" })
    };

    private static readonly string?[] ConditionItems =
    {
        null, null, null, "Lum Berry", "Persim Berry", "Chesto Berry", "Mental Herb", "Destiny Knot", "Figy Berry", "Flame Orb", "Toxic Orb", "Leftovers", "Cheri Berry", "Aspear Berry"
    };

    [Fact]
    public void ALookThatFollowsTheEventsIsTheRulesAfterEveryAnswerOfSixtyBattles()
    {
        int answers = 0, statuses = 0, passing = 0, brought = 0, moments = 0;
        for (int seed = 1; seed <= 60; seed++)
        {
            try
            {
                var (core, follower, asked) = AnsweredFromOutside(seed, doubles: seed % 2 == 0);
                answers += asked;
                moments += follower.Moments;
                statuses += core.Log.SelectMany(e => e is Said said ? said.Shows.Concat(said.OnImpact).Append(e) : new[] { e }).Count(e => e is StatusChanged);
                passing += Told(core).Count(t => t.Event is VolatileChanged);
                brought += Told(core).Count(t => t.Event is CameInWith);
            }
            catch (Xunit.Sdk.XunitException wrong)
            {
                Assert.Fail($"battle {seed}: {wrong.Message}");
            }
        }
        // The battles do what they are for: the test would hold of nothing otherwise
        Assert.True(answers >= 1200 && statuses >= 300 && passing >= 200 && brought >= 100 && moments >= 800,
            $"{answers} answers, {statuses} conditions given or cured, {passing} confusions and loves begun or ended, {brought} brought in, {moments} moments checked");
    }

    /// <summary>
    /// A battle between teams that do little but give, cure and pass on conditions, of both genders, with the
    /// abilities and the items that have a say in them, answered for from outside by a die of the test's own: after
    /// every answer the looks that read the log are what the rules have of every Pokémon on the field.
    /// </summary>
    private static (BattleCore Core, Follower Follower, int Answers) AnsweredFromOutside(int seed, bool doubles)
    {
        var moves = ConditionMoves.Where(name => MoveDatabase.Get(name).Name == name).ToArray();
        Assert.True(moves.Length >= 36, $"only {moves.Length} of the moves are in the data");
        var die = new BattleRandom((uint)seed * 2246822519u);
        Party Team()
        {
            var party = new Party();
            for (int i = 0; i < 4; i++)
            {
                var (species, abilities) = ConditionSpecies[die.Next(ConditionSpecies.Length)];
                var known = Enumerable.Range(0, 4).Select(_ => moves[die.Next(moves.Length)]).Distinct().ToArray();
                var p = Of(die.Next(2) == 0 ? Gender.Male : Gender.Female, species, 50, known);
                With(p, ability: abilities[die.Next(abilities.Length)], item: ConditionItems[die.Next(ConditionItems.Length)]);
                // Now and then one that has something as the battle begins
                if (die.Next(5) == 0) p.Status = new[] { StatusCondition.Poison, StatusCondition.Burn, StatusCondition.Paralyze, StatusCondition.Toxic }[die.Next(4)];
                party.Add(p);
            }
            return party;
        }

        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, DoubleBattle = doubles, Party = Team() };
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = Team(), Trainers = new List<Trainer> { trainer }, Format = doubles ? BattleFormat.Double : BattleFormat.Single,
            Random = new BattleRandom((uint)seed), Rules = Ruleset.Platinum, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        var follower = new Follower();
        follower.Read(core);

        int answers = 0;
        for (; answers < 400 && core.Request != null; answers++)
        {
            var choices = new List<BattleChoice>();
            if (core.Request is ReplacementRequest replacement)
            {
                var place = core.At(replacement.Place);
                choices.Add(BattleChoice.Switch(replacement.Place, Enumerable.Range(0, place.Roster!.Count).First(i => core.CanSendIn(place, i))));
            }
            else
            {
                var switching = new HashSet<BattleSide>();
                foreach (var who in ((ActionRequest)core.Request).Places)
                {
                    var b = core.At(who);
                    var bench = Enumerable.Range(0, b.Roster!.Count).Where(i => core.WhyNot(BattleChoice.Switch(who, i)) == null).ToList();
                    if (bench.Count > 0 && die.Next(7) == 0 && switching.Add(who.Side))
                    {
                        choices.Add(BattleChoice.Switch(who, bench[die.Next(bench.Count)]));
                        continue;
                    }
                    var usable = Enumerable.Range(0, b.Pokemon!.Moves.Count).Where(i => core.WhyNot(BattleChoice.Fight(who, i)) == null).ToList();
                    choices.Add(BattleChoice.Fight(who, usable.Count == 0 ? -1 : usable[die.Next(usable.Count)]));
                }
            }
            core.Submit(choices);
            follower.Read(core);
        }

        return (core, follower, answers);
    }

    [Fact]
    public void ARecordedBattleWithConditionsStillReplaysTheSame()
    {
        // The new events are written by the same lines whatever plays them: a battle played again from its seed and
        // its answers says the same, event for event
        Pokemon[] Mine() => new[] { Of(Gender.Male, "Gengar", 50, "Confuse Ray", "Attract", "Toxic"), Of(Gender.Male, "Breloom", 50, "Spore", "Baton Pass") };
        Pokemon[] Theirs() => new[] { With(Of(Gender.Female, "Blissey", 50, "Attract", "Thunder Wave"), item: "Destiny Knot"), Of(Gender.Female, "Lopunny", 50, "Teeter Dance", "U-turn") };
        var first = Against(Mine(), Theirs(), new BattleRandom(77));
        for (int turn = 0; turn < 12 && first.Request is ActionRequest asking; turn++)
        {
            // Each takes its moves in turn, of those it may use
            first.Submit(asking.Places.Select(who =>
            {
                var b = first.At(who);
                var usable = Enumerable.Range(0, b.Pokemon!.Moves.Count).Where(i => first.WhyNot(BattleChoice.Fight(who, i)) == null).ToList();
                return BattleChoice.Fight(who, usable.Count == 0 ? -1 : usable[turn % usable.Count]);
            }).ToList());
            while (first.Request is ReplacementRequest asked)
            {
                var place = first.At(asked.Place);
                first.Submit(BattleChoice.Switch(asked.Place, Enumerable.Range(0, place.Roster!.Count).First(i => first.CanSendIn(place, i))));
            }
        }
        Assert.NotEmpty(Told(first));

        var again = Against(Mine(), Theirs(), new BattleRandom(77));
        foreach (var answer in first.Record!.Answers) again.Submit(answer.ToList());
        Assert.Equal(Golden.BattleText.Written(first), Golden.BattleText.Written(again));
        Assert.Equal(Told(first).Select(t => (t.Line, t.Event)), Told(again).Select(t => (t.Line, t.Event)));
    }

    // ------------------------------------------------------------------ the screen's look goes by the lines

    private static List<(string Line, T Seen)> ReadOn<T>(BattleEngine battle, Func<T> see)
    {
        var seen = new List<(string, T)>();
        for (int i = 0; i < 400 && !battle.IsBattleOver; i++)
        {
            if (battle.HUD.MenuState is not (BattleMenuState.Message or BattleMenuState.LearnMove)) break;
            if (battle.IsWaitingForConfirm) seen.Add((battle.CurrentMessage, see()));
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        return seen;
    }

    [Fact]
    public void TheViewsConditionIsSetByTheLineThatTellsOfItNotByTheRules()
    {
        var battle = Battle(Idling(Mon("Breloom", 50, "Spore")), Mon("Snorlax", 50), Steady().Force(RollKind.SleepTurns, 2));
        var look = battle.Anim.Enemy.Condition;
        Assert.Same(look, battle.Anim.Conditions.Of(Foe));
        battle.SelectMove(0);
        // The rules have it asleep at once; the screen doesn't until the line is read
        Assert.Equal(StatusCondition.Sleep, battle.Core.At(Foe).Pokemon!.Status);
        Assert.Equal(StatusCondition.None, look.Status);

        var seen = ReadOn(battle, () => look.Status);
        Assert.Contains(("Breloom used Spore!", StatusCondition.None), seen);
        Assert.Contains(("Foe Snorlax fell asleep!", StatusCondition.Sleep), seen);
        // It has only begun to come in: the menu's settling didn't put it there whole
        Assert.True(look.Ailment.Age < 1f && look.Ailment.Up, $"its age is {look.Ailment.Age}");
        Assert.Null(battle.Anim.Conditions.Difference(battle.Core));

        // It stays through every line until the one that says it woke
        var woke = new List<(string Line, StatusCondition Seen)>();
        for (int turn = 0; turn < 8 && look.Status == StatusCondition.Sleep; turn++)
        {
            battle.SelectMove(1);
            woke = ReadOn(battle, () => look.Status);
        }
        int waking = woke.FindIndex(line => line.Line == "Foe Snorlax woke up!");
        Assert.True(waking >= 0, string.Join(" / ", woke.Select(w => w.Line)));
        Assert.All(woke.Take(waking), line => Assert.Equal(StatusCondition.Sleep, line.Seen));
        Assert.Equal(StatusCondition.None, woke[waking].Seen);
        // And its mark is still fading as the next line is read: gone a moment later
        Assert.Equal(StatusCondition.Sleep, look.GoingStatus);
        battle.Anim.Update(BattleAnimator.ConditionFadeTime + 0.05f, null, null);
        Assert.False(look.Any);
    }

    [Fact]
    public void AConfusionAndALoveShowFromTheirLinesAndGoWithTheirs()
    {
        var battle = Battle(Idling(Of(Gender.Male, "Gengar", 50, "Confuse Ray", "Attract")), With(Of(Gender.Female, "Blissey", 50), item: "Mental Herb"),
            Steady().Force(RollKind.ConfusionTurns, 3).Force(RollKind.ConfusionSelfHit, 1));
        var look = battle.Anim.Enemy.Condition;
        battle.SelectMove(0);
        Assert.True(battle.Core.At(Foe).IsConfused);
        Assert.False(look.Confused);
        var seen = ReadOn(battle, () => look.Confused);
        Assert.Contains(("Gengar used Confuse Ray!", false), seen);
        Assert.Contains(("Foe Blissey became confused!", true), seen);

        // Love comes on its line and the herb takes it away on the herb's own, the confusion staying throughout
        battle.SelectMove(1);
        var loved = ReadOn(battle, () => (look.InLove, look.Confused));
        Assert.Contains(("Foe Blissey fell in love!", (true, true)), loved);
        Assert.Contains(("Foe Blissey cured its infatuation using its Mental Herb!", (false, true)), loved);
        Assert.Null(battle.Anim.Conditions.Difference(battle.Core));
    }

    [Fact]
    public void AFaintAndALeavingClearThePlaceAndANewcomerShowsWhatItBrings()
    {
        var poisoned = Mon("Bidoof", 5);
        poisoned.Status = StatusCondition.Poison;
        var asleep = Mon("Snorlax", 50, "Tackle");
        asleep.Status = StatusCondition.Sleep;
        asleep.SleepTurns = 6;
        var party = new Party();
        party.Add(Idling(Mon("Machamp", 50, "Strength")));
        party.Add(asleep);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { poisoned },
            Random = Steady(), Rules = Ruleset.Platinum
        });
        // The wild Pokémon is poisoned from the moment it is met
        var foe = battle.Anim.Enemy.Condition;
        var first = ReadOn(battle, () => foe.Status);
        Assert.All(first, line => Assert.Equal(StatusCondition.Poison, line.Seen));

        // Called back for a sleeper: the place shows nothing as the first leaves, and sleep from the send-out's line
        var mine = battle.Anim.Player.Condition;
        battle.SelectSwitch(1);
        var switched = ReadOn(battle, () => mine.Status);
        Assert.Contains(("Go! Snorlax!", StatusCondition.Sleep), switched);
        Assert.Contains(switched, line => line.Line.StartsWith("Come back") && line.Seen == StatusCondition.None);
        Assert.Null(battle.Anim.Conditions.Difference(battle.Core));

        // The foe falls: its place is cleared on the line of its fall
        battle.SelectSwitch(0);
        ReadOn(battle, () => 0);
        battle.SelectMove(0);
        var fell = ReadOn(battle, () => foe.Status);
        Assert.Contains(("Foe Bidoof fainted!", StatusCondition.None), fell);
        Assert.False(foe.Any);
    }

    [Fact]
    public void AMenuOpeningBringsTheLookLevelWithAPokemonChangedFromOutside()
    {
        // A tool that writes a condition between turns (the harness, a scripted scene) is caught up with at the next
        // menu, with nothing played: what is there is there whole
        var battle = Battle(Idling(Mon("Snorlax", 50)), Mon("Shuckle", 50));
        battle.EnemyPokemon.Status = StatusCondition.Burn;
        Assert.False(battle.Anim.Conditions.Any);
        battle.SelectMove(0);
        Settle(battle);
        var look = battle.Anim.Enemy.Condition;
        Assert.Equal(StatusCondition.Burn, look.Status);
        Assert.Equal(1f, look.Ailment.Strength);
        Assert.Null(battle.Anim.Conditions.Difference(battle.Core));

        // Given by the battle's own rules between turns, it is there at once and its line says so afterwards
        Assert.True(battle.TryInflictStatus(battle.PlayerSlots[0], StatusCondition.Paralyze, null));
        Assert.Equal(StatusCondition.Paralyze, battle.Anim.Player.Condition.Status);

        // And taken away from outside, it is gone at once, with no fade
        battle.EnemyPokemon.Status = StatusCondition.None;
        battle.SelectMove(0);
        Settle(battle);
        Assert.False(look.Any);
    }

    // ------------------------------------------------------------------ the HP box's pill goes by the lines too

    private static Pokemon Ailing(Pokemon p, StatusCondition status)
    {
        p.Status = status;
        if (status == StatusCondition.Sleep) p.SleepTurns = 6;
        return p;
    }

    /// <summary>
    /// A battle on screen against a trainer who thinks about nothing: each of their Pokémon knows one move and uses
    /// it. Not read on: its opening lines are still to come.
    /// </summary>
    private static BattleEngine OnScreen(Pokemon[] mine, Pokemon[] theirs, Inventory? bag = null)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.None };
        foreach (var p in theirs) trainer.Party.Add(p);
        return new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag ?? new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { trainer },
            Random = Steady(), Rules = Ruleset.Platinum
        });
    }

    /// <summary>
    /// What a place's HP box says of its Pokémon: who it is and the condition on its pill. The boxes draw both from
    /// the Pokémon the view shows (<c>ModernUi.StatusPill(…, view.Shown.Status)</c>), which is the screen's own copy.
    /// </summary>
    private static (string? Who, StatusCondition? Pill) Box(BattleEngine battle, Place place) =>
        battle.Anim[place.Side, place.Slot].Shown is { } shown ? (shown.Species.Name, shown.Status) : (null, null);

    private static (string? Who, StatusCondition? Pill) Says(string who, StatusCondition pill) => (who, pill);

    /// <summary>Reads a battle on, with what a place's HP box says at each line and every sound asked for on the way.</summary>
    private static (List<(string Line, (string? Who, StatusCondition? Pill) Seen)> Boxes, List<string> Heard) ReadTheBox(BattleEngine battle, Place place)
    {
        var heard = PokemonPlatinumEngine.Core.AudioManager.Listen();
        try
        {
            return (ReadOn(battle, () => Box(battle, place)), heard.ToList());
        }
        finally
        {
            PokemonPlatinumEngine.Core.AudioManager.StopListening();
        }
    }

    /// <summary>From the newcomer's line to the next menu its box names it with nothing on its pill, and its look agrees.</summary>
    private static void ComesInWithNothing(BattleEngine battle, Place place, string who, string sendOut, List<(string Line, (string? Who, StatusCondition? Pill) Seen)> boxes)
    {
        string all = string.Join(" / ", boxes.Select(b => $"{b.Line} [{b.Seen.Who} {b.Seen.Pill}]"));
        int from = boxes.FindIndex(b => b.Line == sendOut);
        Assert.True(from >= 0, all);
        Assert.All(boxes.Skip(from), b => Assert.True(b.Seen == Says(who, StatusCondition.None), $"at \"{b.Line}\" of: {all}"));
        // And the menu that opens brings nothing new: the box already had it right
        Assert.Equal(Says(who, StatusCondition.None), Box(battle, place));
        Assert.Equal(StatusCondition.None, battle.Anim.Conditions.Of(place).Status);
        Assert.Null(battle.Anim.Conditions.Difference(battle.Core));
    }

    [Theory]
    [InlineData("Heal Bell", "A bell chimed!")]
    [InlineData("Aromatherapy", "A soothing aroma wafted through the area!")]
    public void APokemonCuredOnTheBenchAndDraggedOutTheSameTurnHasNothingOnItsPill(string cure, string chime)
    {
        // The party's cure is told of nobody on the bench (no event: nobody stands there to show it), and Roar, which
        // goes last, drags the cured Pokémon out before any menu has brought the screen's copies level with the rules'
        var burned = Ailing(Mon("Bidoof", 20), StatusCondition.Burn);
        var battle = OnScreen(new[] { Mon("Miltank", 50, cure), burned }, new[] { Mon("Snorlax", 60, "Roar") });
        Settle(battle);
        battle.SelectMove(0);
        // The rules are a turn ahead, with theirs cured and out already; the screen's own is on the bench as it was
        Assert.Equal(("Bidoof", StatusCondition.None), (battle.Core.At(Mine).Pokemon!.Species.Name, battle.Core.At(Mine).Pokemon!.Status));
        Assert.Equal(StatusCondition.Burn, burned.Status);
        Assert.Equal(Says("Miltank", StatusCondition.None), Box(battle, Mine));

        var (boxes, heard) = ReadTheBox(battle, Mine);
        Assert.Contains(boxes, b => b.Line == chime && b.Seen.Who == "Miltank");
        ComesInWithNothing(battle, Mine, "Bidoof", "Bidoof was dragged out!", boxes);
        // It cries as one that is well, too: the cry goes by the same condition (Cries.SendOutMode)
        Assert.Contains("cry Bidoof", heard);
        Assert.DoesNotContain("cry Bidoof Pinch", heard);
    }

    [Fact]
    public void MedicineGivenOnTheBenchShowsOnThePillOfOneDraggedOutTheSameTurn()
    {
        // A Full Heal used on the bench has nowhere to show, so no event tells of it; the foe's Roar then drags that
        // Pokémon out
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Full Heal")!);
        var paralysed = Ailing(Mon("Bidoof", 20), StatusCondition.Paralyze);
        var battle = OnScreen(new[] { Mon("Miltank", 50), paralysed }, new[] { Mon("Snorlax", 60, "Roar") }, bag);
        Settle(battle);
        battle.UseBagItem("Full Heal", onPartyMember: 1);
        Assert.Equal(("Bidoof", StatusCondition.None), (battle.Core.At(Mine).Pokemon!.Species.Name, battle.Core.At(Mine).Pokemon!.Status));
        Assert.Equal(StatusCondition.Paralyze, paralysed.Status);

        var (boxes, heard) = ReadTheBox(battle, Mine);
        ComesInWithNothing(battle, Mine, "Bidoof", "Bidoof was dragged out!", boxes);
        Assert.Contains("cry Bidoof", heard);
        Assert.DoesNotContain("cry Bidoof Pinch", heard);
    }

    [Fact]
    public void ANaturalCureOnTheWayBackShowsOnThePillWhenTheSameTurnBringsItOutAgain()
    {
        // Called back, Starmie is cured with no line and no event; Roar then drags it out again, the only other there is
        var poisoned = Ailing(With(Mon("Starmie", 50), ability: "Natural Cure"), StatusCondition.Poison);
        var battle = OnScreen(new[] { poisoned, Mon("Bidoof", 20) }, new[] { Mon("Snorlax", 60, "Roar") });
        var opening = ReadOn(battle, () => Box(battle, Mine));
        Assert.Contains(("Go! Starmie!", Says("Starmie", StatusCondition.Poison)), opening);

        battle.SelectSwitch(1);
        Assert.Equal(StatusCondition.None, battle.Core.At(Mine).Pokemon!.Status);
        Assert.Equal(StatusCondition.Poison, poisoned.Status);

        var (boxes, heard) = ReadTheBox(battle, Mine);
        // It goes back as it was: nothing has said otherwise yet
        Assert.Contains(("Come back, Starmie!", Says("Starmie", StatusCondition.Poison)), boxes);
        Assert.Contains(("Go! Bidoof!", Says("Bidoof", StatusCondition.None)), boxes);
        ComesInWithNothing(battle, Mine, "Starmie", "Starmie was dragged out!", boxes);
        Assert.Contains("cry Starmie", heard);
        Assert.DoesNotContain("cry Starmie Pinch", heard);
    }

    [Fact]
    public void ATrainersNextCuredOnTheBenchHasNothingOnItsPill()
    {
        // Chimecho (Speed 44) rings before Snorlax (35) strikes; Strength then fells it (132 of its 79 HP), and the
        // trainer's next comes in with no menu between: the trainer chooses from inside the rules
        var poisoned = Ailing(Mon("Bidoof", 20), StatusCondition.Poison);
        var battle = OnScreen(new[] { Mon("Snorlax", 50, "Strength") }, new[] { Mon("Chimecho", 30, "Heal Bell"), poisoned });
        Settle(battle);
        battle.SelectMove(0);
        Assert.Equal(("Bidoof", StatusCondition.None), (battle.Core.At(Foe).Pokemon!.Species.Name, battle.Core.At(Foe).Pokemon!.Status));
        Assert.Equal(StatusCondition.Poison, poisoned.Status);

        var (boxes, heard) = ReadTheBox(battle, Foe);
        InOrder(boxes.Select(b => b.Line).ToList(), "A bell chimed!", "Snorlax used Strength!", "Foe Chimecho fainted!", "Ace Trainer Vera sent out Bidoof!");
        ComesInWithNothing(battle, Foe, "Bidoof", "Ace Trainer Vera sent out Bidoof!", boxes);
        Assert.Contains("cry Bidoof", heard);
        Assert.DoesNotContain("cry Bidoof Pinch", heard);
    }

    [Fact]
    public void ANewcomersPillShowsTheConditionItsLineSaysItBrings()
    {
        // The other way round: whoever does bring a condition has it on its pill from its own line on, whether it is
        // a trainer's first, a trainer's next after a faint, the player's own switched in, or a wild Pokémon met
        var asleep = Ailing(Mon("Snorlax", 50), StatusCondition.Sleep);
        var battle = OnScreen(new[] { Mon("Machamp", 50, "Strength"), asleep },
            new[] { Ailing(Mon("Machop", 5), StatusCondition.Burn), Ailing(Mon("Geodude", 5), StatusCondition.Paralyze) });
        var opening = ReadOn(battle, () => Box(battle, Foe));
        int first = opening.FindIndex(b => b.Line == "Ace Trainer Vera sent out Machop!");
        Assert.True(first >= 0, string.Join(" / ", opening.Select(b => b.Line)));
        Assert.All(opening.Skip(first), b => Assert.Equal(Says("Machop", StatusCondition.Burn), b.Seen));

        battle.SelectMove(0);
        var next = ReadOn(battle, () => Box(battle, Foe));
        int second = next.FindIndex(b => b.Line == "Ace Trainer Vera sent out Geodude!");
        Assert.True(second >= 0, string.Join(" / ", next.Select(b => b.Line)));
        Assert.All(next.Skip(second), b => Assert.Equal(Says("Geodude", StatusCondition.Paralyze), b.Seen));

        battle.SelectSwitch(1);
        var switched = ReadOn(battle, () => Box(battle, Mine));
        int mine = switched.FindIndex(b => b.Line == "Go! Snorlax!");
        Assert.True(mine >= 0, string.Join(" / ", switched.Select(b => b.Line)));
        Assert.All(switched.Skip(mine), b => Assert.Equal(Says("Snorlax", StatusCondition.Sleep), b.Seen));
        Assert.Null(battle.Anim.Conditions.Difference(battle.Core));

        // A wild Pokémon's condition stands beside its entry, by itself, before its line
        var party = new Party();
        party.Add(Mon("Turtwig", 20));
        var wild = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Ailing(Mon("Bidoof", 5), StatusCondition.Poison) }, Random = Steady(), Rules = Ruleset.Platinum
        });
        var met = ReadOn(wild, () => Box(wild, Foe));
        Assert.Equal("A wild Bidoof appeared!", met[0].Line);
        Assert.All(met, b => Assert.Equal(Says("Bidoof", StatusCondition.Poison), b.Seen));
    }

    [Fact]
    public void TheMarksStandBackBehindADollOutOfSightAndWhileAMomentPlays()
    {
        var anim = new BattleAnimator();
        var p = Mon("Snorlax", 50);
        anim.Appear(BattleSide.Enemy, p);
        anim.Conditions.Tell(new StatusChanged(Foe, StatusCondition.Poison));
        Assert.Equal(1f, anim.LastingShown(BattleSide.Enemy, 0));
        // Nobody on the other platform yet: nothing to show there
        Assert.Equal(0f, anim.LastingShown(BattleSide.Player, 0));

        // A moment of the turn plays alone: the marks go as it begins and are back as it ends
        var cue = anim.Moment(BattleSide.Enemy, 0, BattleMoment.Poisoned);
        anim.Update(BattleAnimator.ConditionStandBackTime, null, null);
        Assert.Equal(0f, anim.LastingShown(BattleSide.Enemy, 0), 3);
        anim.Update(BattleAnimator.MomentTime * 0.5f, null, null);
        Assert.Equal(0f, anim.LastingShown(BattleSide.Enemy, 0));
        anim.Update(cue.Length, null, null);
        Assert.Equal(1f, anim.LastingShown(BattleSide.Enemy, 0));
        // A mark given, a stat's change and a heal do the same; a move's own effect doesn't
        foreach (var give in new Func<EffectCue>[] { () => anim.StatusGiven(BattleSide.Enemy, 0, StatusCondition.Poison), () => anim.StatChange(BattleSide.Enemy, 0, up: false), () => anim.Heal(BattleSide.Enemy, 0) })
        {
            var mark = give();
            anim.Update(0.4f, null, null);
            Assert.Equal(0f, anim.LastingShown(BattleSide.Enemy, 0));
            anim.Update(mark.Length, null, null);
        }
        anim.Cue(new EffectCue { Kind = CueKind.Move, Move = "Tackle", FromSide = BattleSide.Player, ToSide = BattleSide.Enemy });
        anim.Update(0.4f, null, null);
        Assert.Equal(1f, anim.LastingShown(BattleSide.Enemy, 0));

        // Out of sight in the middle of a move, behind a doll, fainting, called back, drawn into a ball: none
        anim[BattleSide.Enemy].Away = true;
        Assert.Equal(0f, anim.LastingShown(BattleSide.Enemy, 0));
        anim[BattleSide.Enemy].Away = false;
        anim.SubstituteUp(BattleSide.Enemy);
        Assert.Equal(0f, anim.LastingShown(BattleSide.Enemy, 0));
        anim.SubstituteDown(BattleSide.Enemy);
        anim.Update(BattleAnimator.SubstituteOutTime * 0.5f, null, null);
        Assert.InRange(anim.LastingShown(BattleSide.Enemy, 0), 0.3f, 0.7f);
        anim.Update(BattleAnimator.SubstituteOutTime, null, null);
        Assert.Equal(1f, anim.LastingShown(BattleSide.Enemy, 0));
        anim.Recall(BattleSide.Enemy);
        Assert.Equal(0f, anim.LastingShown(BattleSide.Enemy, 0));
        anim.Appear(BattleSide.Enemy, p);
        anim.Faint(BattleSide.Enemy, 0.15f);
        Assert.Equal(0f, anim.LastingShown(BattleSide.Enemy, 0));
        anim.Appear(BattleSide.Enemy, p);
        anim.Capture(0f);
        Assert.Equal(0f, anim.LastingShown(BattleSide.Enemy, 0));

        // Thrown out of its ball, they come with the last of its growing
        anim.SendOut(BattleSide.Enemy, p);
        Assert.Equal(0f, anim.LastingShown(BattleSide.Enemy, 0));
        anim.Update(BattleAnimator.SendOutBallTime + 0.1f, null, null);
        Assert.Equal(0f, anim.LastingShown(BattleSide.Enemy, 0));
        anim.Update(BattleAnimator.SendOutTime, null, null);
        Assert.Equal(1f, anim.LastingShown(BattleSide.Enemy, 0));
    }

    // ------------------------------------------------------------------ a place's look

    [Fact]
    public void ALookComesInFadesOutAndGivesWayToTheNext()
    {
        var looks = new ConditionLooks();
        var look = looks.Of(Foe);
        Assert.False(looks.Any);
        Assert.False(looks.Tell(new Lunged(Foe, MoveCategory.Physical)));

        Assert.True(looks.Tell(new StatusChanged(Foe, StatusCondition.Poison)));
        Assert.Equal((StatusCondition.Poison, 0f), (look.Status, look.Ailment.Strength));
        int seed = look.Ailment.Seed;
        looks.Advance(BattleAnimator.ConditionArriveTime * 0.5f);
        Assert.InRange(look.Ailment.Strength, 0.45f, 0.55f);
        looks.Advance(3f);
        Assert.Equal(1f, look.Ailment.Strength);
        // Told again of what it has, nothing begins anew
        looks.Tell(new StatusChanged(Foe, StatusCondition.Poison));
        Assert.Equal((seed, 3f + BattleAnimator.ConditionArriveTime * 0.5f), (look.Ailment.Seed, look.Ailment.Age));

        // Rest: what it had gives way, fading while sleep comes in with a seed of its own
        looks.Tell(new StatusChanged(Foe, StatusCondition.Sleep));
        Assert.Equal((StatusCondition.Sleep, StatusCondition.Poison), (look.Status, look.GoingStatus));
        Assert.Equal((0f, 1f, seed), (look.Ailment.Strength, look.Going.Strength, look.Going.Seed));
        Assert.NotEqual(seed, look.Ailment.Seed);
        looks.Advance(BattleAnimator.ConditionFadeTime * 0.5f);
        Assert.InRange(look.Going.Strength, 0.4f, 0.6f);
        // (the loop of what is going runs on, so nothing jumps as it fades)
        Assert.True(look.Going.Age > 3f);
        looks.Advance(BattleAnimator.ConditionFadeTime);
        Assert.Equal(StatusCondition.None, look.GoingStatus);

        // Cured before it had come in whole, it fades from where it stood
        looks.Tell(new StatusChanged(Foe, StatusCondition.None));
        looks.Tell(new StatusChanged(Foe, StatusCondition.Burn));
        looks.Advance(BattleAnimator.ConditionArriveTime * 0.5f);
        looks.Tell(new StatusChanged(Foe, StatusCondition.None));
        Assert.InRange(look.Going.Strength, 0.45f, 0.55f);

        // Confusion and love are marks of their own, each with its ages; a faint clears the place at once
        looks.Tell(new VolatileChanged(Foe, true, true));
        Assert.True(look.Confused && look.InLove);
        Assert.NotEqual(look.Confusion.Seed, look.Love.Seed);
        looks.Tell(new VolatileChanged(Foe, false, true));
        Assert.True(!look.Confused && look.InLove && look.Confusion.Visible);
        looks.Tell(new Fainted(Foe));
        Assert.False(looks.Any);

        // A newcomer shows nothing until its line says what it brings; a place is its own
        looks.Tell(new StatusChanged(Mine, StatusCondition.Paralyze));
        looks.Tell(new Entered(Foe, Mon("Bidoof", 5), FromBall: true));
        looks.Tell(new CameInWith(Foe, StatusCondition.Toxic));
        Assert.Equal((StatusCondition.Toxic, StatusCondition.Paralyze), (look.Status, looks.Of(Mine).Status));
        looks.Tell(new Left(Mine));
        Assert.Equal(StatusCondition.None, looks.Of(Mine).Status);

        // Settled, it is there whole or gone at once
        looks.Settle(Foe, (StatusCondition.Freeze, true, false));
        Assert.Equal((StatusCondition.Freeze, 1f, true, 1f), (look.Status, look.Ailment.Strength, look.Confused, look.Confusion.Strength));
        Assert.False(look.Going.Visible);
        looks.Settle(Foe, (StatusCondition.None, false, false));
        Assert.False(look.Any);
    }

    // ------------------------------------------------------------------ the body's own: the eyes and the hold

    [Fact]
    public void ASleepersEyesAreShutWhateverElseItDoes()
    {
        Assert.Equal(EyeState.Shut, PokemonAnimation.EyesOf(new PokePose { Asleep = true }));
        // Hit in its sleep, or striking in it (Snore, Sleep Talk), it keeps them shut
        Assert.Equal(EyeState.Shut, PokemonAnimation.EyesOf(new PokePose { Asleep = true, Hurt = 0.3f }));
        Assert.Equal(EyeState.Shut, PokemonAnimation.EyesOf(new PokePose { Asleep = true, Attack = 0.4f, Kind = MoveCategory.Physical }));
        // Awake, the same pose squeezes and glares as it always did
        Assert.Equal(EyeState.Squeeze, PokemonAnimation.EyesOf(new PokePose { Hurt = 0.3f }));
        Assert.Equal(EyeState.Fierce, PokemonAnimation.EyesOf(new PokePose { Attack = 0.4f, Kind = MoveCategory.Physical }));
        Assert.Equal(EyeState.Open, PokemonAnimation.EyesOf(new PokePose()));
    }

    [Fact]
    public void AFrozenFaceIsHeldOpenButStillAnswersAHitAndAFaint()
    {
        // No blink reaches a frozen face
        Assert.Equal(EyeState.Shut, PokemonAnimation.EyesOf(new PokePose { Blink = 1f }));
        Assert.Equal(EyeState.Open, PokemonAnimation.EyesOf(new PokePose { Blink = 1f, Frozen = true }));
        // What comes above it in the order still holds: a hit, a faint
        Assert.Equal(EyeState.Squeeze, PokemonAnimation.EyesOf(new PokePose { Frozen = true, Hurt = 0.3f }));
        Assert.Equal(EyeState.Shut, PokemonAnimation.EyesOf(new PokePose { Frozen = true, Faint = 0.5f }));
    }

    [Theory]
    [InlineData("Chimchar")]
    [InlineData("Staravia")]
    public void AFrozenBodyIsHeldAtOneMomentWhateverTheClockSays(string species)
    {
        var model = PokemonModels.Build(species);
        Matrix4x4[] At(PokePose pose)
        {
            model.Animate(pose);
            return model.Skin.ToArray();
        }

        // Free, it moves with the clock; frozen, two times give the same pose, bone for bone
        Assert.NotEqual(At(new PokePose { Time = 0.4f }), At(new PokePose { Time = 1.3f }));
        var held = At(new PokePose { Time = 0.4f, Frozen = true });
        Assert.Equal(held, At(new PokePose { Time = 1.3f, Frozen = true }));
        Assert.Equal(held, At(new PokePose { Time = 77.7f, Frozen = true }));
        // The moment it is held at is the loop's start
        Assert.Equal(At(new PokePose { Time = PokemonAnimation.HeldMoment }), held);
        // A hit still jolts it, over the held idle
        Assert.NotEqual(held, At(new PokePose { Time = 1.3f, Frozen = true, Hurt = 0.12f }));

        // The battle knows how wide the body is from in front and from behind, for what it draws beside it
        foreach (var view in new[] { SpriteView.Front, SpriteView.Back })
            Assert.InRange(PokemonSprites.Framing(model, view, PokemonSprites.Size).HalfWidth, model.Height * 0.1f, model.Height * 2f);
    }

    // ------------------------------------------------------------------ the looks are pure, small and off the body

    private static readonly Vector3 Feet = new(2f, 1f, -3f);
    private const float H = 2f;

    /// <summary>Half its body's width: two fifths of its height, a body about as round as most.</summary>
    private const float W = 0.8f;

    public static IEnumerable<object[]> Marks => new[]
    {
        new object[] { "asleep" }, new object[] { "poisoned" }, new object[] { "badly poisoned" }, new object[] { "burned" },
        new object[] { "paralysed" }, new object[] { "frozen" }, new object[] { "confused" }, new object[] { "in love" }
    };

    private static StatusCondition StatusOf(string mark) => mark switch
    {
        "asleep" => StatusCondition.Sleep, "poisoned" => StatusCondition.Poison, "badly poisoned" => StatusCondition.Toxic,
        "burned" => StatusCondition.Burn, "paralysed" => StatusCondition.Paralyze, "frozen" => StatusCondition.Freeze, _ => StatusCondition.None
    };

    private static int CapOf(string mark) => mark switch
    {
        "confused" => MoveFx.ConfusionCap, "in love" => MoveFx.LoveCap, _ => MoveFx.ConditionCap(StatusOf(mark))
    };

    private static FxList Drawn(string mark, float age, float strength = 1f, int seed = 5591)
    {
        var fx = new FxList();
        if (mark == "confused") MoveFx.ConfusionStars(age, strength, seed, Feet, H, W, fx);
        else if (mark == "in love") MoveFx.LoveHearts(age, strength, seed, Feet, H, fx);
        else MoveFx.Ailment(StatusOf(mark), age, strength, seed, Feet, H, W, fx);
        return fx;
    }

    private static string Written(FxList fx) => string.Join("\n",
        fx.Quads.Select(q => $"{q.Shape} {q.Blend} {q.Depth} {q.Center} {q.Size} {q.Rotation} {q.Color.R},{q.Color.G},{q.Color.B},{q.Color.A}")
            .Concat(fx.Ribbons.Select(r => $"{r.Shape} {r.Blend} {r.Width} {string.Join(";", r.Points)} {r.Color.R},{r.Color.G},{r.Color.B},{r.Color.A}")));

    /// <summary>Ages a mark is looked at: a tenth of a second apart over fourteen seconds, and the fits' own moments for a paralysed one.</summary>
    private static IEnumerable<float> Ages()
    {
        for (int i = 0; i <= 420; i++) yield return i / 30f;
    }

    [Fact]
    public void NothingIsDrawnForNoCondition()
    {
        var fx = new FxList();
        MoveFx.Ailment(StatusCondition.None, 1f, 1f, 5591, Feet, H, W, fx);
        MoveFx.Ailment(StatusCondition.Faint, 1f, 1f, 5591, Feet, H, W, fx);
        MoveFx.Condition(new ConditionLook(), Feet, H, W, 1f, fx);
        // A battle in which nobody has anything adds nothing to the frame's list
        var anim = new BattleAnimator();
        anim.Appear(BattleSide.Enemy, Mon("Snorlax", 50));
        MoveFx.Conditions(anim, new FxPlaces(), fx);
        Assert.Equal(0, fx.Count);
        Assert.Equal(0f, MoveFx.Cast(new ConditionLook()).Amount);
    }

    [Theory]
    [MemberData(nameof(Marks))]
    public void AMarkIsPureInItsAgeItsStrengthAndItsSeed(string mark)
    {
        bool any = false;
        foreach (float age in Ages())
        {
            string first = Written(Drawn(mark, age));
            Assert.Equal(first, Written(Drawn(mark, age)));
            any |= first.Length > 0;
        }
        Assert.True(any, $"{mark} never draws anything");
        // Not there at all, nothing is drawn; a mark's pieces are placed by its seed where it has any chance in it
        Assert.Equal(0, Drawn(mark, 3f, strength: 0f).Count);
        if (mark is "poisoned" or "badly poisoned" or "burned" or "paralysed" or "confused")
            Assert.Contains(Ages(), age => Written(Drawn(mark, age, seed: 5591)) != Written(Drawn(mark, age, seed: 11182)));
    }

    [Theory]
    [MemberData(nameof(Marks))]
    public void AMarkNeverDrawsMoreThanItsCapAndNeverFlashesOrShakes(string mark)
    {
        int cap = CapOf(mark), most = 0;
        Assert.InRange(cap, 1, MoveFx.MomentCap);
        foreach (int seed in new[] { 5591, 11182, 16780, 99991 })
            foreach (float age in Ages())
            {
                var fx = Drawn(mark, age, seed: seed);
                most = Math.Max(most, fx.Count);
                Assert.True(fx.Count <= cap, $"{mark} draws {fx.Count} at {age}, over its cap of {cap}");
                Assert.Equal(0f, fx.Flash);
                Assert.Equal(0f, fx.Shake);
            }
        // The cap is the look's own number, not a loose bound
        Assert.Equal(cap, most);
    }

    [Fact]
    public void APokemonWithEverythingAtOnceStaysUnderAMomentsCap()
    {
        Assert.Equal(MoveFx.ConditionCapOnOne, MoveFx.ConditionCap(StatusCondition.Toxic) + MoveFx.ConfusionCap + MoveFx.LoveCap);
        Assert.True(MoveFx.ConditionCapOnOne <= MoveFx.MomentCap * 0.6f, $"{MoveFx.ConditionCapOnOne} is too near a moment's {MoveFx.MomentCap}");
        foreach (var status in new[] { StatusCondition.Sleep, StatusCondition.Poison, StatusCondition.Toxic, StatusCondition.Burn, StatusCondition.Paralyze, StatusCondition.Freeze })
        {
            var looks = new ConditionLooks();
            looks.Tell(new StatusChanged(Foe, status));
            looks.Tell(new VolatileChanged(Foe, true, true));
            foreach (float age in Ages())
            {
                var fx = new FxList();
                MoveFx.Condition(looks.Of(Foe), Feet, H, W, 1f, fx);
                Assert.True(fx.Count <= MoveFx.ConditionCapOnOne, $"{status}, confused and in love draws {fx.Count} at {age}");
                looks.Advance(1f / 30f);
            }
            // Even as one condition gives way to the heaviest, the two together stay under a moment's cap
            looks.Tell(new StatusChanged(Foe, status == StatusCondition.Toxic ? StatusCondition.Freeze : StatusCondition.Toxic));
            for (int i = 0; i < 20; i++)
            {
                var fx = new FxList();
                MoveFx.Condition(looks.Of(Foe), Feet, H, W, 1f, fx);
                Assert.True(fx.Count <= MoveFx.MomentCap, $"{status} giving way draws {fx.Count}");
                looks.Advance(1f / 30f);
            }
        }
    }

    [Fact]
    public void EveryMarkIsItsOwn()
    {
        // At every moment the eight draw differently: none is another's with a new name
        var marks = Marks.Select(m => (string)m[0]).ToList();
        for (int a = 0; a < marks.Count; a++)
            for (int b = a + 1; b < marks.Count; b++)
                Assert.Contains(Ages(), age => Written(Drawn(marks[a], age)) != Written(Drawn(marks[b], age)) && Drawn(marks[a], age).Count > 0);
    }

    [Theory]
    [MemberData(nameof(Marks))]
    public void AMarkKeepsOffTheMiddleOfTheBodyAndUnderTheHpBoxesRoom(string mark)
    {
        // What is there for turns on end stands at the body's edge or over its head. (A paralysed Pokémon's bolts
        // cross its body for a quarter of a second in every 1.7, and are no part of this.)
        foreach (float age in Ages())
        {
            var fx = Drawn(mark, age);
            foreach (var quad in fx.Quads)
            {
                var at = (quad.Center - Feet) / H;
                // Nothing higher than one and a half heights: over that, on the far platform, is the HP box's side of the picture
                Assert.True(at.Y + quad.Size / H <= 1.5f, $"{mark} at {age}: a {quad.Shape} reaches {at.Y + quad.Size / H:0.00} heights up");
                Assert.True(MathF.Abs(at.X) <= 0.75f, $"{mark} at {age}: a {quad.Shape} is {at.X:0.00} heights aside");
                if (mark == "paralysed") continue;
                bool onTheBody = MathF.Abs(quad.Center.X - Feet.X) < 0.5f * W && at.Y > 0.3f && at.Y < 0.85f;
                Assert.False(onTheBody, $"{mark} at {age}: a {quad.Shape} sits on the body's middle at {at.X:0.00}, {at.Y:0.00}");
                // And none is big enough to hide it
                Assert.True(quad.Size / H <= 0.22f, $"{mark} at {age}: a {quad.Shape} of {quad.Size / H:0.00} heights");
            }
        }
    }

    [Fact]
    public void WhatStandsBesideABodyGoesByItsWidth()
    {
        // The same marks about a narrow body and a wide one of one height: they hug the narrow one and clear the wide
        float Furthest(string mark, float w) => Ages().Max(age =>
        {
            var fx = new FxList();
            MoveFx.Ailment(StatusOf(mark), age, 1f, 5591, Feet, H, w, fx);
            return fx.Quads.Count == 0 ? 0f : fx.Quads.Max(q => MathF.Abs(q.Center.X - Feet.X));
        });
        foreach (string mark in new[] { "poisoned", "badly poisoned", "burned", "frozen", "paralysed" })
            Assert.True(Furthest(mark, 0.5f) < Furthest(mark, 1.1f) * 0.65f, $"{mark}: {Furthest(mark, 0.5f)} about a narrow body, {Furthest(mark, 1.1f)} about a wide one");

        // A confused head's ring is about its body's half-width, between a fifth and a third of its height
        Assert.Equal(0.2f * H, MoveFx.HeadRing(H, 0.1f));
        Assert.Equal(0.32f * H, MoveFx.HeadRing(H, 5f));
        Assert.Equal(0.8f * 0.7f, MoveFx.HeadRing(H, 0.7f), 4);

        // The places take a body nobody measured for a typical one, and never a tail or a pair of wings for a body
        var places = new FxPlaces();
        places.Set(BattleSide.Enemy, 0, Vector3.Zero, 2f);
        Assert.Equal(2f * FxPlaces.TypicalHalfWidth, places.HalfWidth(BattleSide.Enemy, 0));
        places.Set(BattleSide.Enemy, 0, Vector3.Zero, 2f, 0.1f);
        Assert.Equal(0.4f, places.HalfWidth(BattleSide.Enemy, 0));
        places.Set(BattleSide.Enemy, 0, Vector3.Zero, 2f, 3f);
        Assert.Equal(1.2f, places.HalfWidth(BattleSide.Enemy, 0), 4);
        places.Set(BattleSide.Enemy, 0, Vector3.Zero, 2f, 0.9f);
        Assert.Equal(0.9f, places.HalfWidth(BattleSide.Enemy, 0));
    }

    [Fact]
    public void WhatCirclesAHeadOrStandsBehindABodyIsHiddenByItAndWhatIsBeforeItIsDrawnOverIt()
    {
        foreach (float age in Ages())
        {
            // A confused head's stars: behind it they are in the scene, before it over it; just over the head on both platforms
            foreach (var quad in Drawn("confused", age).Quads)
            {
                Assert.Equal(quad.Center.Z < Feet.Z, quad.Depth);
                Assert.InRange((quad.Center.Y - Feet.Y) / H, MoveFx.OverTheHead - 0.03f, MoveFx.OverTheHead + 0.03f);
            }
            // The shell's shards the same; the glint is light over all of it
            foreach (var quad in Drawn("frozen", age).Quads.Where(q => q.Shape == FxShape.Shard))
                if (quad.Center.Z < Feet.Z) Assert.True(quad.Depth);
                else Assert.False(quad.Depth);
        }
    }

    [Fact]
    public void WhatShouldKeepItsColourIsMatter()
    {
        // Over a bright field light alone washes out to white: the Z's, the bubbles, the flames' bodies, the bolts'
        // bodies, the shards, the stars and the hearts are alpha-blended in their own colour
        foreach (string mark in Marks.Select(m => (string)m[0]))
        {
            bool matter = false;
            foreach (float age in Ages())
            {
                var fx = Drawn(mark, age);
                matter |= fx.Quads.Any(q => q.Blend == FxBlend.Alpha) || fx.Ribbons.Any(r => r.Blend == FxBlend.Alpha);
                // What is added is the smaller part: a flame's heart, a bolt's core, a glint
                int added = fx.Quads.Count(q => q.Blend == FxBlend.Add) + fx.Ribbons.Count(r => r.Blend == FxBlend.Add);
                Assert.True(added * 2 <= fx.Count, $"{mark} at {age}: {added} of {fx.Count} are light");
            }
            Assert.True(matter, mark);
        }
    }

    [Fact]
    public void TheMarksLoopAsTheStyleGuideSays()
    {
        // Asleep: a Z leaves at once, and the three are on their way together from two thirds of the loop on
        Assert.Equal(2, Drawn("asleep", 0.3f).Count);
        Assert.Equal(6, Drawn("asleep", MoveFx.ZzzTime + 0.3f).Count);
        // Paralysed: a fit of a quarter of a second in every 1.7, and nothing between
        Assert.True(Drawn("paralysed", 0.1f).Count > 0);
        Assert.Equal(0, Drawn("paralysed", MoveFx.FitTime + 0.05f).Count);
        Assert.Equal(0, Drawn("paralysed", MoveFx.FitEvery - 0.05f).Count);
        Assert.True(Drawn("paralysed", MoveFx.FitEvery + 0.1f).Count > 0);
        Assert.InRange(MoveFx.FitAt(0.1f).Into, 0.099f, 0.101f);
        Assert.Equal((0, 1), (MoveFx.FitAt(0.1f).Fit, MoveFx.FitAt(MoveFx.FitEvery + 0.1f).Fit));
        Assert.Equal(-1f, MoveFx.FitAt(1f).Into);
        // Badly poisoned is poison deepened: half as many bubbles again
        Assert.Equal(MoveFx.ConditionCap(StatusCondition.Poison) * 3 / 2, MoveFx.ConditionCap(StatusCondition.Toxic));
        // A star goes once round in its time
        var first = Drawn("confused", 1f).Quads[1].Center;
        Assert.True(Vector3.Distance(first, Drawn("confused", 1f + MoveFx.StarRound).Quads[1].Center) < 0.01f);
        Assert.True(Vector3.Distance(first, Drawn("confused", 1f + MoveFx.StarRound * 0.5f).Quads[1].Center) > 0.3f);
        // A mark half come in is drawn at half strength, in the same places
        var whole = Drawn("in love", 0.6f).Quads;
        var half = Drawn("in love", 0.6f, strength: 0.5f).Quads;
        Assert.Equal(whole.Select(q => q.Center), half.Select(q => q.Center));
        Assert.All(whole.Zip(half), pair => Assert.InRange(pair.Second.Color.A, pair.First.Color.A / 2 - 2, pair.First.Color.A / 2 + 2));
    }

    [Fact]
    public void TheCastIsIceBlueOnAFrozenBodyAndAPulseOnAPoisonedOne()
    {
        // Frozen: there for as long as it is, about two fifths, more blue than red
        foreach (float age in Ages())
        {
            var (colour, amount) = MoveFx.CastOf(StatusCondition.Freeze, age, 1f);
            Assert.InRange(amount, 0.35f, 0.45f);
            Assert.True(colour.B > colour.G && colour.G > colour.R, $"{colour.R},{colour.G},{colour.B}");
        }
        Assert.InRange(MoveFx.CastOf(StatusCondition.Freeze, 1f, 0.5f).Amount, 0.17f, 0.23f);

        // Poison comes and goes; bad poison is darker, stronger and more often
        float Peak(StatusCondition status) => Ages().Max(age => MoveFx.CastOf(status, age, 1f).Amount);
        float Share(StatusCondition status) => Ages().Count(age => MoveFx.CastOf(status, age, 1f).Amount > 0.01f) / (float)Ages().Count();
        Assert.InRange(Peak(StatusCondition.Poison), 0.2f, 0.25f);
        Assert.InRange(Peak(StatusCondition.Toxic), 0.3f, 0.35f);
        Assert.InRange(Share(StatusCondition.Poison), 0.3f, 0.45f);
        Assert.True(Share(StatusCondition.Toxic) > Share(StatusCondition.Poison));
        var poison = MoveFx.CastOf(StatusCondition.Poison, 0.45f, 1f).Color;
        var toxic = MoveFx.CastOf(StatusCondition.Toxic, 0.45f, 1f).Color;
        Assert.True(toxic.R + toxic.G + toxic.B < poison.R + poison.G + poison.B);

        // A paralysed body glints as each fit begins, and only then; nothing is cast on a sleeper or a burned one
        Assert.True(MoveFx.CastOf(StatusCondition.Paralyze, 0.02f, 1f).Amount > 0.15f);
        Assert.Equal(0f, MoveFx.CastOf(StatusCondition.Paralyze, 0.5f, 1f).Amount);
        Assert.All(Ages(), age => Assert.Equal(0f, MoveFx.CastOf(StatusCondition.Sleep, age, 1f).Amount));
        Assert.All(Ages(), age => Assert.Equal(0f, MoveFx.CastOf(StatusCondition.Burn, age, 1f).Amount));

        // The cast of a condition that is going fades with it
        var looks = new ConditionLooks();
        looks.Settle(Foe, (StatusCondition.Freeze, false, false));
        Assert.InRange(MoveFx.Cast(looks.Of(Foe)).Amount, 0.35f, 0.45f);
        looks.Tell(new StatusChanged(Foe, StatusCondition.None));
        looks.Advance(BattleAnimator.ConditionFadeTime * 0.5f);
        Assert.InRange(MoveFx.Cast(looks.Of(Foe)).Amount, 0.15f, 0.25f);
        looks.Advance(BattleAnimator.ConditionFadeTime);
        Assert.Equal(0f, MoveFx.Cast(looks.Of(Foe)).Amount);
    }

    [Fact]
    public void TheWholeFieldsMarksAreDrawnWhereEachPokemonStandsAndOnlyWhereItCanBeSeen()
    {
        var anim = new BattleAnimator { Slots = 2 };
        var places = new FxPlaces();
        for (int slot = 0; slot < 2; slot++)
        {
            anim.Appear(BattleSide.Enemy, Mon("Snorlax", 50), slot);
            anim.Appear(BattleSide.Player, Mon("Bidoof", 50), slot);
        }
        anim.Conditions.Tell(new StatusChanged(Foe, StatusCondition.Burn));
        anim.Conditions.Tell(new VolatileChanged(Mine2, true, false));
        anim.Update(1f, null, null);

        var fx = new FxList();
        MoveFx.Conditions(anim, places, fx);
        Assert.Equal(MoveFx.ConditionCap(StatusCondition.Burn) + MoveFx.ConfusionCap, fx.Count);
        // Each about its own Pokémon: the flames by the foe's first place, the stars over the player's second
        Assert.All(fx.Quads.Where(q => q.Shape == FxShape.Flame), q => Assert.True(Vector3.Distance(q.Center, places.Body(BattleSide.Enemy, 0)) < places.Height(BattleSide.Enemy, 0)));
        Assert.All(fx.Quads.Where(q => q.Shape == FxShape.Star), q => Assert.True(Vector3.Distance(q.Center, places.Head(BattleSide.Player, 1)) < places.Height(BattleSide.Player, 1)));

        // Behind a doll the flames aren't drawn; the stars over the other still are
        anim.SubstituteUp(BattleSide.Enemy);
        fx.Clear();
        MoveFx.Conditions(anim, places, fx);
        Assert.Equal(MoveFx.ConfusionCap, fx.Count);
    }
}
