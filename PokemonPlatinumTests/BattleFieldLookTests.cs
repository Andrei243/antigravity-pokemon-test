using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
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
/// What lies on the field (plan 25 · Y2): every line that sets, wears off or clears a screen, a veil, a hazard, a
/// room or something sent ahead carries its event; a look that follows those events alone is the rules' field at
/// every turn of every battle; the screen's look rises and falls with the lines; and every look is a pure function
/// of its ages, the clock and its seed, under its cap of quads.
/// </summary>
public class BattleFieldLookTests
{
    // ------------------------------------------------------------------ following a log

    /// <summary>A look that knows the field only by what the log's events tell, as the screen does.</summary>
    private sealed class Follower
    {
        public readonly FieldLook Look = new();
        private int read;

        /// <summary>Takes what the log has said since it last looked, and holds the look to the rules' field.</summary>
        public void Read(BattleCore core)
        {
            for (; read < core.Log.Count; read++)
            {
                if (core.Log[read] is Said said)
                {
                    foreach (var shown in said.Shows) Look.Tell(shown);
                    foreach (var landing in said.OnImpact) Look.Tell(landing);
                }
                else Look.Tell(core.Log[read]);
            }
            Assert.True(Look.Difference(core.Field) == null, $"after {core.Log.Count} events: {Look.Difference(core.Field)}");
        }
    }

    private static bool OfTheField(BattleEvent happening) => happening is SideConditionChanged or FieldConditionChanged or PlaceConditionChanged;

    /// <summary>Every event of the field in a log, with the line it rides on (null for one that stands by itself).</summary>
    private static List<(string? Line, BattleEvent Event)> Told(BattleCore core)
    {
        var told = new List<(string?, BattleEvent)>();
        foreach (var happening in core.Log)
        {
            if (happening is Said said) told.AddRange(said.Shows.Concat(said.OnImpact).Where(OfTheField).Select(e => ((string?)said.Text, e)));
            else if (OfTheField(happening)) told.Add((null, happening));
        }
        return told;
    }

    /// <summary>Plays a turn and holds the follower to the field after it.</summary>
    private static void Play(BattleCore core, Follower follower, int mine = 0, int foe = 0)
    {
        Turn(core, mine, foe);
        follower.Read(core);
    }

    private static (BattleCore Core, Follower Follower) Open(Pokemon mine, Pokemon foe, params Pokemon[] bench)
    {
        var core = Wild(mine, foe, bench: bench);
        var follower = new Follower();
        follower.Read(core);
        return (core, follower);
    }

    // ------------------------------------------------------------------ the rules' events, line by line

    public static IEnumerable<object[]> Screens => new[]
    {
        new object[] { "Reflect", SideCondition.Reflect, "Reflect raised your team's Defense!", "Your team's Reflect wore off!" },
        new object[] { "Light Screen", SideCondition.LightScreen, "Light Screen raised your team's Special Defense!", "Your team's Light Screen wore off!" },
        new object[] { "Mist", SideCondition.Mist, "Your team became shrouded in mist!", "Your team's Mist wore off!" },
        new object[] { "Safeguard", SideCondition.Safeguard, "Your team became cloaked in a mystical veil!", "Your team is no longer protected by Safeguard!" },
        new object[] { "Lucky Chant", SideCondition.LuckyChant, "The Lucky Chant shielded your team from critical hits!", "Your team's Lucky Chant wore off!" },
        new object[] { "Tailwind", SideCondition.Tailwind, "The tailwind blew from behind your team!", "Your team's tailwind petered out!" }
    };

    [Theory]
    [MemberData(nameof(Screens))]
    public void AScreenGoesUpAndWearsOffOnItsOwnLines(string move, SideCondition what, string up, string off)
    {
        var (core, follower) = Open(Idling(Mon("Snorlax", 50, move)), Mon("Shuckle", 50));
        Play(core, follower);
        for (int turn = 0; turn < 8 && core.Field.Side(BattleSide.Player).Layers(what) > 0; turn++) Play(core, follower, mine: 1);

        // Up on the line that says so, down on the line that says so, and told of nowhere else
        Assert.Equal(new (string?, BattleEvent)[]
        {
            (up, new SideConditionChanged(BattleSide.Player, what, 1)),
            (off, new SideConditionChanged(BattleSide.Player, what, 0))
        }, Told(core));

        // Used while it is up, it fails and tells nothing more
        var (again, following) = Open(Mon("Snorlax", 50, move), Mon("Shuckle", 50));
        Play(again, following);
        Play(again, following);
        Assert.Single(Told(again));
    }

    [Fact]
    public void EveryLayerScatteredTellsHowManyLieThere()
    {
        var (core, follower) = Open(Mon("Snorlax", 50, "Spikes", "Toxic Spikes", "Stealth Rock"), Mon("Shuckle", 50));
        for (int i = 0; i < 4; i++) Play(core, follower, mine: 0);
        for (int i = 0; i < 3; i++) Play(core, follower, mine: 1);
        for (int i = 0; i < 2; i++) Play(core, follower, mine: 2);

        // Three layers of Spikes, two of Toxic Spikes, the stones once: each on the foe's side, and a use too many says nothing
        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Spikes were scattered all around the foe's team's feet!", new SideConditionChanged(BattleSide.Enemy, SideCondition.Spikes, 1)),
            ("Spikes were scattered all around the foe's team's feet!", new SideConditionChanged(BattleSide.Enemy, SideCondition.Spikes, 2)),
            ("Spikes were scattered all around the foe's team's feet!", new SideConditionChanged(BattleSide.Enemy, SideCondition.Spikes, 3)),
            ("Poison spikes were scattered all around the foe's team's feet!", new SideConditionChanged(BattleSide.Enemy, SideCondition.ToxicSpikes, 1)),
            ("Poison spikes were scattered all around the foe's team's feet!", new SideConditionChanged(BattleSide.Enemy, SideCondition.ToxicSpikes, 2)),
            ("Pointed stones float in the air around the foe's team!", new SideConditionChanged(BattleSide.Enemy, SideCondition.StealthRock, 1))
        }, Told(core));
        Assert.Equal(3, follower.Look.Of(BattleSide.Enemy, SideCondition.Spikes).Layers);
        Assert.Equal(2, follower.Look.Of(BattleSide.Enemy, SideCondition.ToxicSpikes).Layers);
        Assert.True(follower.Look.Of(BattleSide.Enemy, SideCondition.StealthRock).Up);
    }

    [Fact]
    public void BrickBreakTakesBothScreensDownOnTheLineThatShattersThem()
    {
        var (core, follower) = Open(Idling(Mon("Machamp", 50, "Brick Break")), Idling(Mon("Shuckle", 50, "Reflect", "Light Screen")));
        Play(core, follower, mine: 1, foe: 0);
        Play(core, follower, mine: 1, foe: 1);
        Play(core, follower, mine: 0, foe: 2);

        var shattered = Told(core).Where(t => t.Line == "It shattered the barrier!").Select(t => t.Event).ToList();
        Assert.Equal(new BattleEvent[]
        {
            new SideConditionChanged(BattleSide.Enemy, SideCondition.Reflect, 0),
            new SideConditionChanged(BattleSide.Enemy, SideCondition.LightScreen, 0)
        }, shattered);
        Assert.False(follower.Look.Of(BattleSide.Enemy, SideCondition.Reflect).Up);
        Assert.False(follower.Look.Of(BattleSide.Enemy, SideCondition.LightScreen).Up);
    }

    [Fact]
    public void DefogTakesEachThingOffItsTargetsSideOnItsOwnLine()
    {
        // The foe puts up its screens and veils while its side is scattered with everything; then Defog
        var (core, follower) = Open(Mon("Skarmory", 50, "Spikes", "Toxic Spikes", "Stealth Rock", "Defog"),
            Idling(Mon("Shuckle", 50, "Reflect", "Light Screen", "Mist", "Safeguard")));
        Play(core, follower, 0, 0);
        Play(core, follower, 1, 1);
        Play(core, follower, 2, 2);
        Play(core, follower, 0, 3);
        int before = Told(core).Count;
        Play(core, follower, 3, 4);

        var blown = Told(core).Skip(before).ToList();
        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("The foe's team's Reflect was blown away!", new SideConditionChanged(BattleSide.Enemy, SideCondition.Reflect, 0)),
            ("The foe's team's Light Screen was blown away!", new SideConditionChanged(BattleSide.Enemy, SideCondition.LightScreen, 0)),
            ("The foe's team's Mist was blown away!", new SideConditionChanged(BattleSide.Enemy, SideCondition.Mist, 0)),
            ("The foe's team's Safeguard was blown away!", new SideConditionChanged(BattleSide.Enemy, SideCondition.Safeguard, 0)),
            ("The spikes around the foe's team were blown away!", new SideConditionChanged(BattleSide.Enemy, SideCondition.Spikes, 0)),
            ("The poison spikes around the foe's team were blown away!", new SideConditionChanged(BattleSide.Enemy, SideCondition.ToxicSpikes, 0)),
            ("The pointed stones around the foe's team were blown away!", new SideConditionChanged(BattleSide.Enemy, SideCondition.StealthRock, 0))
        }, blown);
        Assert.All(Enum.GetValues<SideCondition>(), kind => Assert.False(follower.Look.Of(BattleSide.Enemy, kind).Up, kind.ToString()));
    }

    [Fact]
    public void RapidSpinClearsItsOwnSideOnItsLines()
    {
        var (core, follower) = Open(Idling(Mon("Starmie", 50, "Rapid Spin")), Idling(Mon("Skarmory", 50, "Spikes", "Toxic Spikes", "Stealth Rock")));
        Play(core, follower, 1, 0);
        Play(core, follower, 1, 1);
        Play(core, follower, 1, 2);
        int before = Told(core).Count;
        Play(core, follower, 0, 3);

        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Starmie blew away Spikes!", new SideConditionChanged(BattleSide.Player, SideCondition.Spikes, 0)),
            ("Starmie blew away Toxic Spikes!", new SideConditionChanged(BattleSide.Player, SideCondition.ToxicSpikes, 0)),
            ("Starmie blew away Stealth Rock!", new SideConditionChanged(BattleSide.Player, SideCondition.StealthRock, 0))
        }, Told(core).Skip(before).ToList());
    }

    [Fact]
    public void APoisonTypeOnTheGroundTakesTheToxicSpikesUpAsItComesIn()
    {
        var (core, follower) = Open(Mon("Snorlax", 50), Idling(Mon("Skarmory", 50, "Toxic Spikes")), Mon("Grimer", 50));
        Play(core, follower, 0, 0);
        SwitchTo(core, 1, foe: 1);
        follower.Read(core);

        Assert.Equal(("The poison spikes disappeared from around your team's feet!", new SideConditionChanged(BattleSide.Player, SideCondition.ToxicSpikes, 0)),
            Told(core)[^1]);
        Assert.False(follower.Look.Of(BattleSide.Player, SideCondition.ToxicSpikes).Up);
    }

    [Fact]
    public void TrickRoomIsToldOfAsItIsTwistedAsItRunsOutAndAsItIsPutBack()
    {
        var (core, follower) = Open(Idling(Mon("Bronzong", 50, "Trick Room")), Mon("Shuckle", 50));
        Play(core, follower, 0);
        for (int turn = 0; turn < 6 && core.Field.TrickRoom; turn++) Play(core, follower, 1);
        // Twisted again, and put back by a second use
        Play(core, follower, 0);
        Play(core, follower, 0);

        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Bronzong twisted the dimensions!", new FieldConditionChanged(FieldCondition.TrickRoom, true)),
            ("The twisted dimensions returned to normal!", new FieldConditionChanged(FieldCondition.TrickRoom, false)),
            ("Bronzong twisted the dimensions!", new FieldConditionChanged(FieldCondition.TrickRoom, true)),
            ("Bronzong restored the twisted dimensions!", new FieldConditionChanged(FieldCondition.TrickRoom, false))
        }, Told(core));
    }

    [Fact]
    public void ABattleThatOpensUnderTrickRoomSaysSoOnItsOpeningLine()
    {
        // A place that bends the order of things (OpenUnderTheSky), and the Battle Arcade's board (plan 06 · R18d)
        var party = new Party();
        party.Add(Mon("Snorlax", 50));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Mon("Shuckle", 50) }, Random = Calm(), Rules = Ruleset.Platinum,
            Conditions = new BattleConditions { TrickRoom = true }, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        var follower = new Follower();
        follower.Read(core);

        Assert.Equal(("The dimensions became distorted!", new FieldConditionChanged(FieldCondition.TrickRoom, true)), Assert.Single(Told(core)));
        Assert.True(follower.Look.Of(FieldCondition.TrickRoom).Up);
    }

    [Fact]
    public void GravityIsToldOfAsItComesAndAsItGoes()
    {
        var (core, follower) = Open(Idling(Mon("Bronzong", 50, "Gravity")), Mon("Shuckle", 50));
        Play(core, follower, 0);
        Assert.True(follower.Look.Of(FieldCondition.Gravity).Up);
        for (int turn = 0; turn < 6 && core.Field.Gravity; turn++) Play(core, follower, 1);

        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Gravity intensified!", new FieldConditionChanged(FieldCondition.Gravity, true)),
            ("Gravity returned to normal!", new FieldConditionChanged(FieldCondition.Gravity, false))
        }, Told(core));
    }

    [Fact]
    public void AWishWaitsOverItsPlaceUntilItComesTrue()
    {
        var (core, follower) = Open(Idling(Mon("Blissey", 50, "Wish")), Mon("Shuckle", 50));
        Play(core, follower, 0);
        Assert.True(follower.Look.Of(Mine, PlaceCondition.Wish).Up);
        Play(core, follower, 1);

        Assert.Equal(new (string?, BattleEvent)[]
        {
            ("Blissey made a wish!", new PlaceConditionChanged(Mine, PlaceCondition.Wish, true)),
            ("Blissey's wish came true!", new PlaceConditionChanged(Mine, PlaceCondition.Wish, false))
        }, Told(core));
    }

    [Fact]
    public void AWishLostWithNobodyThereIsToldOfBetweenLines()
    {
        // The wisher is knocked out the turn its Wish would have come true: the rules drop the Wish with no line
        var (core, follower) = Open(Idling(Mon("Togepi", 5, "Wish")), Idling(Mon("Machamp", 50, "Strength")), Mon("Snorlax", 50));
        Play(core, follower, 0, 1);
        Turn(core, 1, 0);
        follower.Read(core);
        Assert.IsType<ReplacementRequest>(core.Request);

        Assert.DoesNotContain("Togepi's wish came true!", Lines(core.Log));
        Assert.Equal((null, new PlaceConditionChanged(Mine, PlaceCondition.Wish, false)), Told(core)[^1]);
        Assert.False(follower.Look.Of(Mine, PlaceCondition.Wish).Up);
    }

    [Theory]
    [InlineData("Alakazam", "Future Sight", PlaceCondition.FutureSight, "Alakazam foresaw an attack!")]
    [InlineData("Jirachi", "Doom Desire", PlaceCondition.DoomDesire, "Jirachi chose Doom Desire as its destiny!")]
    public void AnAttackSentAheadWaitsOverItsTargetUntilItLands(string user, string move, PlaceCondition what, string sent)
    {
        var (core, follower) = Open(Idling(Mon(user, 50, move)), Mon("Snorlax", 50));
        Play(core, follower, 0);
        Assert.True(follower.Look.Of(Foe, what).Up);
        Play(core, follower, 1);
        Assert.True(follower.Look.Of(Foe, what).Up);
        Play(core, follower, 1);

        Assert.Equal(new (string?, BattleEvent)[]
        {
            (sent, new PlaceConditionChanged(Foe, what, true)),
            ($"Foe Snorlax took the {move} attack!", new PlaceConditionChanged(Foe, what, false))
        }, Told(core));
    }

    [Fact]
    public void AGuardSpecFromTheBagPutsItsMistOnTheField()
    {
        var party = new Party();
        party.Add(Mon("Snorlax", 50));
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get("Guard Spec.")!, 1);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = bag, Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Mon("Shuckle", 50) },
            Random = Steady(), Rules = Ruleset.Platinum
        });
        Settle(battle);
        battle.UseBagItem("Guard Spec.");
        Settle(battle);

        Assert.Contains(("Your team became shrouded in mist!", new SideConditionChanged(BattleSide.Player, SideCondition.Mist, 1)), Told(battle.Core));
        Assert.True(battle.Anim.FieldLook.Of(BattleSide.Player, SideCondition.Mist).Up);
    }

    // ------------------------------------------------------------------ the events are all the look needs

    [Fact]
    public void ALookThatFollowsTheEventsIsTheFieldAtTheEndOfEveryGoldenBattle()
    {
        // Two hundred battles of every kind, both sides chosen for from inside: whatever set, wore off or cleared a
        // condition anywhere in them was told of, or the look and the rules' field would differ at the end
        int withAnything = 0, events = 0;
        for (int seed = 1; seed <= BattleCorpus.Battles; seed++)
        {
            var battle = new BattleCorpus(seed);
            battle.Core.Start();
            var follower = new Follower();
            follower.Read(battle.Core);
            int told = Told(battle.Core).Count;
            events += told;
            if (told > 0) withAnything++;
        }
        // The corpus does put things on the field: the test would hold of nothing otherwise
        Assert.True(withAnything >= 20 && events >= 60, $"{withAnything} battles told of the field, in {events} events");
    }

    private static readonly string[] FieldMoves =
    {
        "Spikes", "Toxic Spikes", "Stealth Rock", "Reflect", "Light Screen", "Safeguard", "Mist", "Tailwind", "Lucky Chant", "Trick Room", "Gravity",
        "Wish", "Future Sight", "Doom Desire", "Defog", "Rapid Spin", "Brick Break", "Baton Pass", "U-turn", "Roar", "Whirlwind", "Tackle", "Protect",
        // The rooms of plan 06 · R25
        "Wonder Room", "Magic Room"
    };

    private static readonly string[] FieldSpecies =
    {
        "Snorlax", "Shuckle", "Skarmory", "Grimer", "Tentacruel", "Bronzong", "Blissey", "Starmie", "Machamp", "Togekiss", "Forretress", "Drapion"
    };

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    public void ALookThatFollowsTheEventsIsTheFieldAfterEveryAnswer(int seed, bool doubles)
    {
        // Teams that do little but put things on the field and take them off, answered for from outside by a die
        // of the test's own: after every answer the look that read the log is the rules' field
        var die = new BattleRandom((uint)seed * 2654435761u);
        Party Team()
        {
            var party = new Party();
            for (int i = 0; i < 4; i++)
            {
                var moves = Enumerable.Range(0, 4).Select(_ => FieldMoves[die.Next(FieldMoves.Length)]).Distinct().ToArray();
                party.Add(Mon(FieldSpecies[die.Next(FieldSpecies.Length)], 50, moves));
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
        for (; answers < 300 && core.Request != null; answers++)
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
                    // Now and then a switch (one a side a turn, so two places never ask for the same Pokémon)
                    var bench = Enumerable.Range(0, b.Roster!.Count).Where(i => core.WhyNot(BattleChoice.Switch(who, i)) == null).ToList();
                    if (bench.Count > 0 && die.Next(6) == 0 && switching.Add(who.Side))
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

        Assert.True(answers >= 20, $"only {answers} answers were asked for");
        Assert.True(Told(core).Count >= 15, $"only {Told(core).Count} events of the field in {answers} answers");
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
    public void TheLookRisesWithTheLineThatTellsOfItNotWithTheRules()
    {
        var battle = Battle(Mon("Snorlax", 50, "Spikes"), Mon("Shuckle", 50));
        var spikes = battle.Anim.FieldLook.Of(BattleSide.Enemy, SideCondition.Spikes);
        battle.SelectMove(0);
        // The rules have them at once; the screen doesn't until the line is read
        Assert.Equal(1, battle.Core.Field.Side(BattleSide.Enemy).Spikes);
        Assert.Equal(0, spikes.Layers);

        var seen = ReadOn(battle, () => spikes.Layers);
        Assert.Contains(("Snorlax used Spikes!", 0), seen);
        Assert.Contains(("Spikes were scattered all around the foe's team's feet!", 1), seen);
        // It has only begun to come in: the menu's settling didn't put it there whole
        Assert.True(spikes.Age < BattleAnimator.FieldArriveTime, $"its age is {spikes.Age}");
        Assert.Null(battle.Anim.FieldLook.Difference(battle.Core.Field));
    }

    [Fact]
    public void TheLookFadesWithTheLineThatSaysItIsGone()
    {
        var battle = Battle(Idling(Mon("Bronzong", 50, "Trick Room")), Mon("Shuckle", 50));
        var room = battle.Anim.FieldLook.Of(FieldCondition.TrickRoom);
        battle.SelectMove(0);
        var seen = ReadOn(battle, () => room.Up);
        Assert.Contains(("Bronzong twisted the dimensions!", true), seen);

        // It is still there through the turn it ends in, until its own line
        for (int turn = 0; turn < 4; turn++)
        {
            battle.SelectMove(1);
            seen = ReadOn(battle, () => room.Up);
        }
        Assert.Equal(("The twisted dimensions returned to normal!", false), seen[^1]);
        Assert.All(seen.Take(seen.Count - 1), line => Assert.True(line.Seen, line.Line));
        Assert.Equal(1, room.Fading);

        // Then it eases away
        battle.Anim.Update(BattleAnimator.FieldFadeTime + 0.1f, null, null);
        Assert.False(room.Visible);
    }

    [Fact]
    public void ABattleOpenedUnderTrickRoomShowsItsGridFromItsOpeningLine()
    {
        var party = new Party();
        party.Add(Mon("Snorlax", 50));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Mon("Shuckle", 50) },
            Random = Steady(), Rules = Ruleset.Platinum, Conditions = new BattleConditions { TrickRoom = true }
        });
        var seen = ReadOn(battle, () => battle.Anim.FieldLook.Of(FieldCondition.TrickRoom).Up);
        int opening = seen.FindIndex(line => line.Line == "The dimensions became distorted!");
        Assert.True(opening > 0);
        Assert.All(seen.Take(opening), line => Assert.False(line.Seen, line.Line));
        Assert.All(seen.Skip(opening), line => Assert.True(line.Seen, line.Line));
    }

    [Fact]
    public void AMenuOpeningBringsTheLookLevelWithAFieldChangedFromOutside()
    {
        // A tool that writes the rules' field between turns (the harness's profile scene) is caught up with at the
        // next menu, with nothing played: what is there is there whole
        var battle = Battle(Mon("Snorlax", 50), Mon("Shuckle", 50));
        battle.Core.Field.Side(BattleSide.Enemy).Spikes = 2;
        battle.Core.Field.Side(BattleSide.Player).ReflectTurns = 5;
        Assert.False(battle.Anim.FieldLook.Any);

        battle.SelectMove(0);
        Settle(battle);
        var spikes = battle.Anim.FieldLook.Of(BattleSide.Enemy, SideCondition.Spikes);
        Assert.Equal(2, spikes.Layers);
        Assert.Equal(1f, spikes.Arrival);
        Assert.True(battle.Anim.FieldLook.Of(BattleSide.Player, SideCondition.Reflect).Up);
        Assert.Null(battle.Anim.FieldLook.Difference(battle.Core.Field));
    }

    // ------------------------------------------------------------------ a condition's view

    [Fact]
    public void AViewComesInFadesOutAndKeepsItsSeedThroughItsLayers()
    {
        var look = new FieldLook();
        var spikes = look.Of(BattleSide.Enemy, SideCondition.Spikes);
        Assert.False(look.Any);

        look.Set(BattleSide.Enemy, SideCondition.Spikes, 1);
        Assert.Equal((1, 0f, 1f), (spikes.Shown, spikes.Arrival, spikes.Strength));
        int seed = spikes.Seed;
        look.Advance(BattleAnimator.FieldArriveTime);
        Assert.Equal(1f, spikes.Arrival);

        // A layer more comes in by itself: what lay there already keeps its seed, so it stays where it lay
        look.Set(BattleSide.Enemy, SideCondition.Spikes, 2);
        Assert.Equal((2, 0f, seed), (spikes.Shown, spikes.Arrival, spikes.Seed));

        // Gone: the layers that stood ease away, and then nothing is drawn
        look.Advance(1f);
        look.Set(BattleSide.Enemy, SideCondition.Spikes, 0);
        Assert.False(spikes.Up);
        Assert.Equal((2, 1f, 1f), (spikes.Shown, spikes.Arrival, spikes.Strength));
        look.Advance(BattleAnimator.FieldFadeTime * 0.5f);
        Assert.InRange(spikes.Strength, 0.4f, 0.6f);
        look.Advance(BattleAnimator.FieldFadeTime);
        Assert.False(spikes.Visible);
        Assert.False(look.Any);

        // Each thing that comes up has a seed of its own, from the look's own count
        look.Set(BattleSide.Enemy, SideCondition.Spikes, 1);
        look.Set(BattleSide.Player, SideCondition.Spikes, 1);
        Assert.NotEqual(seed, spikes.Seed);
        Assert.NotEqual(spikes.Seed, look.Of(BattleSide.Player, SideCondition.Spikes).Seed);
    }

    // ------------------------------------------------------------------ the looks are pure, small and low

    private static readonly FxPlaces Places = new();

    private static ConditionView Standing(int layers, float age, int seed = 6151) => new() { Layers = layers, Age = age, Seed = seed };

    private static ConditionView Going(int layers, float fadingAge, int seed = 6151) => new() { Fading = layers, FadingAge = fadingAge, Seed = seed };

    private static int Most(SideCondition what) => what switch { SideCondition.Spikes => 3, SideCondition.ToxicSpikes => 2, _ => 1 };

    private static FxList Drawn(Action<FxList> draw)
    {
        var fx = new FxList();
        draw(fx);
        return fx;
    }

    /// <summary>Everything a list draws, quad by quad and ribbon by ribbon, as text to compare.</summary>
    private static string Written(FxList fx) => string.Join("\n",
        fx.Quads.Select(q => $"{q.Shape} {q.Blend} {q.Depth} {q.Center} {q.Size} {q.Rotation} {q.Color} {q.Along} {q.Stretch} {q.Normal}")
            .Concat(fx.Ribbons.Select(r => $"{r.Shape} {r.Blend} {r.Depth} {r.Width} {r.Color} {string.Join(" ", r.Points)}")));

    /// <summary>Every look there is, as something to draw from a view at a time, with its cap.</summary>
    public static IEnumerable<object[]> Looks()
    {
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
            foreach (var kind in Enum.GetValues<SideCondition>())
                yield return new object[] { $"{side} {kind}", Most(kind), BattleFieldFx.Cap(kind), (Action<ConditionView, float, FxList>)((v, t, fx) => BattleFieldFx.Side(side, kind, v, t, Places, fx)) };
        foreach (var kind in Enum.GetValues<FieldCondition>())
            yield return new object[] { kind.ToString(), 1, BattleFieldFx.Cap(kind), (Action<ConditionView, float, FxList>)((v, t, fx) => BattleFieldFx.Field(kind, v, t, Places, fx)) };
        foreach (var place in new[] { Mine, Foe, Mine2, Foe2 })
            foreach (var kind in Enum.GetValues<PlaceCondition>())
                yield return new object[] { $"{place.Side} {place.Slot} {kind}", 1, BattleFieldFx.Cap(kind), (Action<ConditionView, float, FxList>)((v, t, fx) => BattleFieldFx.Place(place, kind, v, t, Places, fx)) };
    }

    [Theory]
    [MemberData(nameof(Looks))]
    public void ALookIsPureInItsAgesItsSeedAndTheClock(string name, int layers, int cap, object drawer)
    {
        var draw = (Action<ConditionView, float, FxList>)drawer;
        var a = Drawn(fx => draw(Standing(layers, 2f), 7.3f, fx));
        var b = Drawn(fx => draw(Standing(layers, 2f), 7.3f, fx));
        Assert.True(a.Count > 0, name);
        Assert.Equal(Written(a), Written(b));

        // It moves with the clock (but for Spikes, which lie still), lies otherwise by another seed (but for the grid and
        // the panes, which stand where they stand), and sits in the scene: a body hides what is behind it
        if (!name.EndsWith(" Spikes")) Assert.NotEqual(Written(a), Written(Drawn(fx => draw(Standing(layers, 2f), 7.9f, fx))));
        if (cap > 3 && !name.Contains("TrickRoom")) Assert.NotEqual(Written(a), Written(Drawn(fx => draw(Standing(layers, 2f, seed: 12302), 7.3f, fx))));
        Assert.All(a.Quads, q => Assert.True(q.Depth, name));
        Assert.All(a.Ribbons, r => Assert.True(r.Depth, name));
        Assert.Equal(0f, a.Flash);
        Assert.Equal(0f, a.Shake);

        // Nothing of it is drawn before it stands or once it has gone
        Assert.Equal(0, Drawn(fx => draw(new ConditionView(), 7.3f, fx)).Count);
        Assert.Equal(0, Drawn(fx => draw(Going(layers, BattleAnimator.FieldFadeTime), 7.3f, fx)).Count);
    }

    [Theory]
    [MemberData(nameof(Looks))]
    public void ALookNeverDrawsMoreThanItsCap(string name, int layers, int cap, object drawer)
    {
        var draw = (Action<ConditionView, float, FxList>)drawer;
        Assert.True(cap > 0, name);
        int most = 0;
        for (int step = 0; step < 240; step++)
        {
            float time = step * 0.137f;
            // Coming in layer by layer, standing, and fading away
            for (int n = 1; n <= layers; n++)
            {
                most = Math.Max(most, Drawn(fx => draw(Standing(n, step * 0.01f, 6151 + step), time, fx)).Count);
                most = Math.Max(most, Drawn(fx => draw(Standing(n, 5f + step, 6151 + step), time, fx)).Count);
                most = Math.Max(most, Drawn(fx => draw(Going(n, step * BattleAnimator.FieldFadeTime / 240f, 6151 + step), time, fx)).Count);
            }
        }
        Assert.InRange(most, 1, cap);
    }

    [Fact]
    public void AWholeFieldStaysUnderTheSumOfItsCaps()
    {
        var look = new FieldLook();
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
        {
            foreach (var kind in Enum.GetValues<SideCondition>()) look.Set(side, kind, Most(kind));
            for (int slot = 0; slot < 2; slot++)
            {
                look.Set(new Place(side, slot), PlaceCondition.Wish, true);
                look.Set(new Place(side, slot), PlaceCondition.DoomDesire, true);
            }
        }
        // The field has one terrain at a time (plan 06 · R19b): the heaviest stands with the rest
        foreach (var kind in Enum.GetValues<FieldCondition>()) if (!FieldState.IsTerrain(kind)) look.Set(kind, true);
        look.Set(FieldCondition.GrassyTerrain, true);
        look.Advance(3f);

        int most = 0;
        for (int step = 0; step < 120; step++) most = Math.Max(most, Drawn(fx => BattleFieldFx.Emit(look, step * 0.21f, Places, 2, fx)).Count);
        Assert.InRange(most, BattleFieldFx.MaxQuads / 2, BattleFieldFx.MaxQuads);
        // Small things, for all that there are many kinds of them: under five hundred and fifty with every kind up at once,
        // Aurora Veil, Sticky Web and the Pledges' three on both sides and the heaviest terrain among them (plan 06 · R19b, R25)
        Assert.True(BattleFieldFx.MaxQuads <= 550, $"{BattleFieldFx.MaxQuads} quads at the most");
        // (The number the plan and CLAUDE.md give: a look made heavier changes it, and them)
        Assert.Equal(533, BattleFieldFx.MaxQuads);
        Assert.Equal(BattleFieldFx.Cap(FieldCondition.GrassyTerrain),
            Enum.GetValues<FieldCondition>().Where(FieldState.IsTerrain).Max(BattleFieldFx.Cap));

        // A single battle draws nothing over the second places, and an empty look nothing at all
        Assert.True(Drawn(fx => BattleFieldFx.Emit(look, 1f, Places, 1, fx)).Count < Drawn(fx => BattleFieldFx.Emit(look, 1f, Places, 2, fx)).Count);
        Assert.Equal(0, Drawn(fx => BattleFieldFx.Emit(new FieldLook(), 1f, Places, 2, fx)).Count);
    }

    /// <summary>How far round a platform a point is from the way to the camera, in radians (0: right before it).</summary>
    private static float FromTheCamera(BattleSide side, Vector3 at)
    {
        var centre = side == BattleSide.Player ? BattleStage.PlayerSpot : BattleStage.EnemySpot;
        var toPoint = Vector2.Normalize(new Vector2(at.X - centre.X, at.Z - centre.Z));
        var toCamera = Vector2.Normalize(new Vector2(BattleStage.CameraPosition.X - centre.X, BattleStage.CameraPosition.Z - centre.Z));
        return MathF.Acos(Math.Clamp(Vector2.Dot(toPoint, toCamera), -1f, 1f));
    }

    [Theory]
    [InlineData(BattleSide.Player)]
    [InlineData(BattleSide.Enemy)]
    public void WhatStandsUpKeepsOffTheArcThatFacesTheCamera(BattleSide side)
    {
        float radius = side == BattleSide.Player ? BattleStage.PlayerPlatformRadius : BattleStage.EnemyPlatformRadius;
        float top = Places.Feet(side, 0).Y;
        for (int step = 0; step < 60; step++)
        {
            float time = step * 0.31f;
            // The stones and the charms stand over the rim: never between the lens and the Pokémon
            foreach (var kind in new[] { SideCondition.StealthRock, SideCondition.LuckyChant })
            {
                var fx = Drawn(f => BattleFieldFx.Side(side, kind, Standing(1, 3f, 6151 + step), time, Places, f));
                foreach (var q in fx.Quads.Where(q => q.Normal == Vector3.Zero))
                    Assert.True(FromTheCamera(side, q.Center) > BattleFieldFx.CameraArc - 0.12f, $"{kind} at {FromTheCamera(side, q.Center)} from the camera");
            }
            // The panes do stand before the foe's Pokémon, so they are held low: under a ninth of the radius
            foreach (var kind in new[] { SideCondition.Reflect, SideCondition.LightScreen })
            {
                var fx = Drawn(f => BattleFieldFx.Side(side, kind, Standing(1, 3f, 6151 + step), time, Places, f));
                foreach (var ribbon in fx.Ribbons)
                    Assert.All(ribbon.Points, p => Assert.True(p.Y + ribbon.Width * 0.3f <= top + radius * (BattleFieldFx.TallestBeforeTheCamera + 0.005f), $"{kind} stands {p.Y - top} over the platform"));
            }
            // What lies in wait lies on the platform: a caltrop is a fraction of the radius tall
            foreach (var kind in new[] { SideCondition.Spikes, SideCondition.ToxicSpikes })
            {
                var fx = Drawn(f => BattleFieldFx.Side(side, kind, Standing(Most(kind), 3f, 6151 + step), time, Places, f));
                Assert.All(fx.Quads.Where(q => q.Shape == FxShape.Shard), q => Assert.True(q.Center.Y + q.Size <= top + radius * 0.16f, $"{kind} stands {q.Center.Y + q.Size - top}"));
            }
        }
        // The style guide's number: under a ninth of the radius, which reaches a Pokémon's feet from the far platform's rim and no further
        Assert.True(BattleFieldFx.TallestBeforeTheCamera <= 1f / 9f);
        Assert.Equal(65f, BattleFieldFx.CameraArc * 180f / MathF.PI, 3);
    }

    [Theory]
    [InlineData(BattleSide.Player)]
    [InlineData(BattleSide.Enemy)]
    public void TheLaterLooksStandOnlyWhereTheyFaceNobody(BattleSide side)
    {
        // Plan 06 · R19b: Aurora Veil's curtains hang over the far rim, and a terrain's pieces stand there too
        for (int step = 0; step < 40; step++)
        {
            float time = step * 0.29f;
            var veil = Drawn(f => BattleFieldFx.Side(side, SideCondition.AuroraVeil, Standing(1, 3f, 6151 + step), time, Places, f));
            foreach (var ribbon in veil.Ribbons)
                Assert.All(ribbon.Points, p => Assert.True(FromTheCamera(side, p) > BattleFieldFx.CameraArc - 0.12f, $"the veil at {FromTheCamera(side, p)} from the camera"));
            foreach (var q in veil.Quads)
                Assert.True(FromTheCamera(side, q.Center) > BattleFieldFx.CameraArc - 0.12f, $"a glint at {FromTheCamera(side, q.Center)}");
            foreach (var terrain in Enum.GetValues<FieldCondition>().Where(FieldState.IsTerrain))
            {
                var fx = Drawn(f => BattleFieldFx.Field(terrain, Standing(1, 3f, 6151 + step), time, Places, f));
                var centre = side == BattleSide.Player ? BattleStage.PlayerSpot : BattleStage.EnemySpot;
                float radius = side == BattleSide.Player ? BattleStage.PlayerPlatformRadius : BattleStage.EnemyPlatformRadius;
                // What stands up (a sprite or a streak, not a wash lying on the platform) near this platform's rim
                foreach (var q in fx.Quads.Where(q => q.Normal == Vector3.Zero && Vector2.Distance(new(q.Center.X, q.Center.Z), new(centre.X, centre.Z)) < radius * 1.5f))
                    Assert.True(FromTheCamera(side, q.Center) > BattleFieldFx.CameraArc - 0.12f, $"{terrain} at {FromTheCamera(side, q.Center)} from the camera");
            }
        }
    }

    [Fact]
    public void TrickRoomsWallsNeverStandBetweenTheCameraAndTheField()
    {
        var fx = Drawn(f => BattleFieldFx.Field(FieldCondition.TrickRoom, Standing(1, 3f), 2f, Places, f));
        Assert.Equal(BattleFieldFx.Cap(FieldCondition.TrickRoom), fx.Count);
        // Every line is a line of the world (a ribbon from end to end), not a sprite turned to the camera
        Assert.Empty(fx.Quads);
        Assert.All(fx.Ribbons, r => Assert.Equal(2, r.Points.Length));
        // Every upright stands on the far wall or on a side wall; the near edge of the grid has none
        var uprights = fx.Ribbons.Where(r => MathF.Abs(r.Points[1].Y - r.Points[0].Y) > 0.5f).ToList();
        Assert.Equal(3 * BattleFieldFx.GridLines, uprights.Count);
        Assert.All(uprights, r => Assert.True(
            MathF.Abs(r.Points[0].Z - BattleFieldFx.GridMin.Y) < 0.01f || MathF.Abs(r.Points[0].X - BattleFieldFx.GridMin.X) < 0.01f || MathF.Abs(r.Points[0].X - BattleFieldFx.GridMax.X) < 0.01f));
        // And every rail and floor line stays inside the grid's ground
        Assert.All(fx.Ribbons.SelectMany(r => r.Points), p => Assert.True(
            p.X >= BattleFieldFx.GridMin.X - 0.01f && p.X <= BattleFieldFx.GridMax.X + 0.01f && p.Z >= BattleFieldFx.GridMin.Y - 0.01f && p.Z <= BattleFieldFx.GridMax.Y + 0.01f
            && p.Y >= 0f && p.Y <= BattleFieldFx.WallHeight + 0.01f));
        // Both platforms and the camera's feet are inside the grid
        foreach (var spot in new[] { BattleStage.PlayerSpot, BattleStage.EnemySpot })
        {
            Assert.InRange(spot.X, BattleFieldFx.GridMin.X + 1f, BattleFieldFx.GridMax.X - 1f);
            Assert.InRange(spot.Z, BattleFieldFx.GridMin.Y + 1f, BattleFieldFx.GridMax.Y - 1f);
        }
        Assert.True(BattleStage.CameraPosition.Z > BattleFieldFx.GridMax.Y, "the camera stands before the grid's near edge, where no wall is");
    }

    [Fact]
    public void GravityLowersTheStagesLight()
    {
        var day = ArtLook.ArenaRig(new ArenaSpec(PokemonPlatinumEngine.Overworld.BattleArena.Grass), 12f);
        Assert.Equal(day, ArtLook.UnderGravity(day, 0f));
        var under = ArtLook.UnderGravity(day, 1f);
        Assert.Equal(day.Light.SunColor * ArtLook.GravityDim, under.Light.SunColor);
        Assert.Equal(day.Light.SkyAmbient * ArtLook.GravityDim, under.Light.SkyAmbient);
        Assert.Equal(day.Post.Vignette + 0.08f, under.Post.Vignette, 4);
        // Half way in, half way down
        var half = ArtLook.UnderGravity(day, 0.5f);
        Assert.InRange(half.Light.SunColor.X, under.Light.SunColor.X, day.Light.SunColor.X);
    }
}
