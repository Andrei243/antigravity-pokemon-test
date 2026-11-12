using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 14 · B13: the Battle Royal (<c>docs/mechanics/rulings.md</c>, "The Battle Royal"). Four trainers each for
/// themselves on two sides: the player's one place and the three others on the other side's three places, every other
/// place a foe's (<see cref="BattleCore.AreFoes"/>); the battle ends as one camp runs out and they are placed by
/// knock-outs and Pokémon left; first is a victory, any other <see cref="BattleResult.Placed"/>, which costs nothing.
/// </summary>
public class BattleRoyalTests
{
    private static Trainer Rival(string name, params Pokemon[] team)
    {
        var t = new Trainer { Id = "royal_" + name.ToLowerInvariant(), Name = name, TrainerClass = "Ace Trainer", PrizeMoney = 100, Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic };
        foreach (var p in team) t.Party.Add(p);
        return t;
    }

    private static BattleCore Royal(Pokemon[] mine, Trainer[] others, BattleRandom? rolls = null, Ruleset? rules = null, IBattleController? foes = null, IBattleController? me = null)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = others.ToList(), Format = BattleFormat.Royal, Random = rolls ?? Calm(), Rules = rules ?? Ruleset.Modern,
            PlayerController = me, EnemyController = foes, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    private static Trainer[] ThreeOthers(int level = 30) => new[]
    {
        Rival("Ana", Mon("Bidoof", level, "Tackle"), Mon("Bidoof", level, "Tackle")),
        Rival("Bo", Mon("Shinx", level, "Tackle"), Mon("Shinx", level, "Tackle")),
        Rival("Cy", Mon("Starly", level, "Tackle"), Mon("Starly", level, "Tackle"))
    };

    [Fact]
    public void ARoyalIsTheOneAgainstThreeOthersEachForThemselves()
    {
        var core = Royal(new[] { Mon("Golem", 40, "Tackle"), Mon("Golem", 40, "Tackle") }, ThreeOthers());
        Assert.Equal(BattleFormat.Royal, core.Format);
        Assert.True(core.IsRoyal);
        Assert.Single(core.PlayerSlots);
        Assert.Equal(3, core.EnemySlots.Count);
        Assert.Equal(3, BattleFormat.Royal.PlacesOf(BattleSide.Enemy));
        // Each of the other side's places has its own trainer and team
        Assert.Equal(new[] { "Ana", "Bo", "Cy" }, core.EnemySlots.Select(b => b.Trainer!.Name));
        Assert.Equal(3, core.EnemySlots.Select(b => b.Roster).Distinct().Count());
        // Everyone else is a foe, the three of the other side among themselves too
        Assert.True(core.AreFoes(core.At(Foe), core.At(Foe2)));
        Assert.True(core.AreFoes(core.At(Mine), core.At(Foe3)));
        Assert.False(core.AreFoes(core.At(Foe), core.At(Foe)));
        Assert.Equal(3, core.ActiveFoes(core.At(Foe2)).Count());
        // The camps
        Assert.Equal(0, BattleCore.CampOf(Mine));
        Assert.Equal(3, BattleCore.CampOf(Foe3));
        // No EXP, prize or white-out, nothing from the bag
        Assert.True(core.NoRewards);
        Assert.True(core.NoBattleItems);
        Assert.Contains(PokemonPlatinumEngine.Core.Text.Of("battle.royalBegins"), Lines(core.Log));
    }

    [Fact]
    public void WhatCantBeARoyalFallsBack()
    {
        Assert.Equal(BattleFormat.Royal, BattleFormats.FallBack(BattleFormat.Royal, 1, 0, false, 3, 1, 0, BattleKind.Normal, Ruleset.Modern, out _));
        // Platinum's rules have none: the first two trainers make the double
        Assert.Equal(BattleFormat.Double, BattleFormats.FallBack(BattleFormat.Royal, 2, 0, false, 3, 1, 0, BattleKind.Normal, Ruleset.Platinum, out _));
        Assert.Equal(BattleFormat.Double, BattleFormats.FallBack(BattleFormat.Royal, 2, 0, false, 2, 1, 0, BattleKind.Normal, Ruleset.Modern, out _));
        // One of the three with nobody able to fight, and a player with one Pokémon: no Royal, and no double either
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Royal, 1, 0, false, 3, 0, 0, BattleKind.Normal, Ruleset.Modern, out _));
        var underPlatinum = Royal(new[] { Mon("Golem", 40, "Tackle"), Mon("Golem", 40, "Tackle") }, ThreeOthers(), rules: Ruleset.Platinum);
        Assert.Equal(BattleFormat.Double, underPlatinum.Format);
        Assert.False(underPlatinum.IsRoyal);
        Assert.True(underPlatinum.AreFoes(underPlatinum.At(Mine), underPlatinum.At(Foe)));
        Assert.False(underPlatinum.AreFoes(underPlatinum.At(Foe), underPlatinum.At(Foe2)));
    }

    [Fact]
    public void AMoveMayBeAimedAtAnyOfTheThreeAndASpreadMoveHitsThemAll()
    {
        var core = Royal(new[] { Mon("Golem", 30, "Tackle", "Earthquake") }, ThreeOthers(45));
        // The third of the others aims at the second: a foe like any other
        var lines = DoubleTurn(core, (Mine, 0, Foe3), (Foe, 0, Foe2), (Foe2, 0, Foe), (Foe3, 0, Foe2));
        Assert.True(core.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<MoveShown>().Count(m => m.From == Foe3 && m.To == Foe2 || m.From == Foe && m.To == Foe2) >= 1, string.Join("\n", lines));
        // Earthquake goes for the three others, every one of them a foe (Cy's Starly, a Flying type, takes nothing)
        int before = core.Log.Count;
        DoubleTurn(core, (Mine, 1, null), (Foe, 0, Mine), (Foe2, 0, Mine), (Foe3, 0, Mine));
        var aimed = core.Log.Skip(before).OfType<Said>().SelectMany(s => s.Shows).OfType<MoveShown>()
            .Where(m => m.From == Mine && m.Move == "Earthquake").Select(m => m.To).ToHashSet();
        Assert.Equal(new HashSet<Place> { Foe, Foe2, Foe3 }, aimed);
        var struck = core.Log.Skip(before).OfType<Said>().SelectMany(s => s.OnImpact).OfType<Struck>().Select(s => s.Place).ToHashSet();
        Assert.Contains(Foe, struck);
        Assert.Contains(Foe2, struck);
    }

    [Fact]
    public void NobodySwitchesByChoice()
    {
        var core = Royal(new[] { Mon("Golem", 40, "Tackle"), Mon("Golem", 40, "Tackle") }, ThreeOthers());
        Assert.NotNull(core.WhyNot(BattleChoice.Switch(Mine, 1)));
        Assert.NotNull(core.WhyNot(BattleChoice.UseItem(Mine, "Potion")));
    }

    [Fact]
    public void TheRoyalEndsWhenOneRunsOutAndPlacesThemByKnockOutsAndPokemonLeft()
    {
        // The player's Golem knocks Ana's two Bidoof out one after another; Ana is out and the Royal is over
        var others = new[]
        {
            Rival("Ana", Mon("Bidoof", 5, "Splash"), Mon("Bidoof", 5, "Splash")),
            Rival("Bo", Mon("Shinx", 30, "Splash"), Mon("Shinx", 30, "Splash")),
            Rival("Cy", Mon("Starly", 30, "Splash"), Mon("Starly", 30, "Splash"))
        };
        var core = Royal(new[] { Mon("Golem", 60, "Tackle"), Mon("Golem", 60, "Tackle") }, others);
        for (int turn = 0; turn < 10 && core.Request != null; turn++)
        {
            if (core.Request is ReplacementRequest r)
            {
                core.Submit(BattleChoice.Switch(r.Place, Enumerable.Range(0, core.At(r.Place).Roster!.Count).First(i => core.CanSendIn(core.At(r.Place), i))));
                continue;
            }
            DoubleTurn(core, (Mine, 0, Foe), (Foe, 0, null), (Foe2, 0, null), (Foe3, 0, null));
        }
        Assert.Null(core.Request);
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Equal(1, core.RoyalPlace);
        Assert.Equal(2, core.RoyalKnockOuts(0));
        // Two knock-outs and two left: four points; Bo and Cy two left each; Ana nothing
        Assert.Equal(4, core.RoyalScore(0));
        Assert.Equal(0, core.RoyalScore(1));
        Assert.Equal(1, core.RoyalStanding[^1]);
        var placed = core.Log.OfType<RoyalPlaced>().Single();
        Assert.Equal(core.RoyalStanding, placed.Standing);
        Assert.IsType<Ended>(core.Log.Last());
        // Nothing paid
        Assert.Equal(0, core.MoneyLost);
        Assert.DoesNotContain(core.Log, e => e is ExpGained);
    }

    [Fact]
    public void PlacingLowerIsPlacedAndCostsNothing()
    {
        // The player's lone Bidoof can't stand against three Golem who all aim at it: the player runs out first
        var others = new[]
        {
            Rival("Ana", Mon("Golem", 60, "Rock Slide")),
            Rival("Bo", Mon("Golem", 60, "Rock Slide")),
            Rival("Cy", Mon("Golem", 60, "Rock Slide"))
        };
        var core = Royal(new[] { Mon("Bidoof", 5, "Splash") }, others);
        for (int turn = 0; turn < 10 && core.Request is ActionRequest; turn++)
            DoubleTurn(core, (Mine, 0, null), (Foe, 0, Mine), (Foe2, 0, Mine), (Foe3, 0, Mine));
        Assert.Null(core.Request);
        Assert.Equal(BattleResult.Placed, core.Result);
        Assert.Equal(4, core.RoyalPlace);
        Assert.Equal(0, core.MoneyLost);
        Assert.DoesNotContain(PokemonPlatinumEngine.Core.Text.Of("battle.whitedOut", ("player", "Lucas")), Lines(core.Log));
    }

    private static BattleCore Fresh(BattleFormat format, uint seed = 20261103)
    {
        var party = new Party();
        party.Add(Mon("Grotle", 30, "Razor Leaf", "Bite"));
        party.Add(Mon("Luxio", 30, "Spark", "Bite"));
        party.Add(Mon("Monferno", 30, "Ember", "Mach Punch"));
        var others = new[]
        {
            Rival("Ana", Mon("Staravia", 28, "Wing Attack", "Quick Attack"), Mon("Kricketune", 28, "Fury Cutter"), Mon("Ponyta", 29, "Ember")),
            Rival("Bo", Mon("Shinx", 27, "Spark"), Mon("Buizel", 28, "Water Gun"), Mon("Budew", 28, "Mega Drain")),
            Rival("Cy", Mon("Machop", 28, "Karate Chop"), Mon("Geodude", 28, "Rock Throw"), Mon("Psyduck", 28, "Confusion"))
        };
        return new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = others.ToList(), Format = format, Random = new BattleRandom(seed), Rules = Ruleset.Modern,
            PlayerController = null, EnemyController = TrainerAi.Instance, PlayerName = "Lucas"
        });
    }

    [Fact]
    public void ARecordedRoyalReplaysTheSameAndOnlyAsARoyal()
    {
        var core = Fresh(BattleFormat.Royal);
        Assert.Equal(BattleFormat.Royal, core.Format);
        core.Start();
        for (int step = 0, turn = 0; step < 300 && core.Request != null; step++)
        {
            switch (core.Request)
            {
                case ActionRequest asked:
                {
                    var place = asked.Places.Single();
                    var target = core.ActiveFoes(core.At(place)).ElementAt(turn % core.ActiveFoes(core.At(place)).Count()).Place;
                    var choice = BattleChoice.Fight(place, turn % core.At(place).Pokemon!.Moves.Count, target);
                    if (core.WhyNot(choice) != null) choice = BattleChoice.Fight(place, -1);
                    core.Submit(choice);
                    turn++;
                    break;
                }
                case ReplacementRequest replace:
                    core.Submit(BattleChoice.Switch(replace.Place, Enumerable.Range(0, core.PlayerParty.Count).First(i => core.CanSendIn(core.At(replace.Place), i))));
                    break;
            }
        }
        Assert.Null(core.Request);
        Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.Placed, $"{core.Result}");
        var record = core.Record!;
        Assert.Equal(BattleFormat.Royal, record.Format);
        var again = Fresh(BattleFormat.Royal);
        again.Replay(record);
        Assert.Equal(Lines(core.Log), Lines(again.Log));
        Assert.Equal(core.Result, again.Result);
        // The same seed and answers as a double are another battle: refused
        Assert.Throws<ArgumentException>(() => Fresh(BattleFormat.Double).Replay(record));
    }

    [Fact]
    public void AHundredRoyalsAlwaysEndAndLeaveNothingOutOfRange()
    {
        var species = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        int placed = 0, won = 0;
        for (int battle = 0; battle < 100; battle++)
        {
            var dice = new Random(battle * 104729 + 13);
            var mine = new Party();
            for (int i = 0; i < 3; i++) mine.Add(new Pokemon(species[dice.Next(species.Count)], dice.Next(20, 70), dice));
            var others = Enumerable.Range(0, 3).Select(k =>
            {
                var t = new Trainer { Id = $"fuzz{k}", Name = $"Fuzz{k}", TrainerClass = "Ace Trainer", Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic | PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.EvalAttack };
                for (int i = 0; i < 3; i++) t.Party.Add(new Pokemon(species[dice.Next(species.Count)], dice.Next(20, 70), dice));
                return t;
            }).ToList();
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = mine, Trainers = others, Random = new BattleRandom((uint)battle), Rules = Ruleset.Modern,
                Format = BattleFormat.Royal, PlayerController = TrainerAi.Instance
            });
            Assert.Equal(BattleFormat.Royal, core.Format);
            core.Start();
            Assert.Null(core.Request);
            Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.Placed, $"royal {battle} ended as {core.Result}");
            Assert.True(core.Turn < 1000, $"royal {battle} took {core.Turn} turns");
            var log = core.TakeLog();
            Assert.IsType<Ended>(log.Last());
            Assert.Single(log.OfType<RoyalPlaced>());
            if (core.Result == BattleResult.Placed) placed++; else won++;
            // One camp, at least, is out
            Assert.True(mine.Members.All(p => p.IsFainted) || others.Any(t => t.Party.Members.All(p => p.IsFainted)), $"royal {battle}: nobody ran out");
            foreach (var p in mine.Members.Concat(others.SelectMany(t => t.Party.Members)))
            {
                Assert.InRange(p.CurrentHP, 0, p.MaxHP);
                Assert.All(p.Moves, m => Assert.InRange(m.CurrentPP, 0, m.MaxPP));
            }
        }
        Assert.True(placed > 0 && won > 0, $"won {won}, placed {placed}");
    }

    [Fact]
    public void OnTheScreenTheThreeOthersAreTheTargets()
    {
        var party = new Party();
        party.Add(Mon("Golem", 60, "Tackle", "Earthquake"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = ThreeOthers().ToList(),
            Format = BattleFormat.Royal, Random = Steady(), Rules = Ruleset.Modern
        });
        Settle(battle);
        Assert.True(battle.IsRoyal);
        Assert.Equal(1, battle.Anim.SlotsOf(BattleSide.Player));
        Assert.Equal(3, battle.Anim.SlotsOf(BattleSide.Enemy));
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        // Tackle asks where to go: any of the three others, as the stage stands them
        battle.SelectMainMenuOption(0);
        battle.SelectMove(0);
        Assert.Equal(BattleMenuState.SelectTarget, battle.HUD.MenuState);
        Assert.Equal(battle.EnemySlots.Reverse().ToArray(), battle.TargetChoices.ToArray());
        // Nothing from the bag in a Royal
        Assert.True(battle.Core.NoBattleItems);

        // The ring's cards (style guide, "The ring's cards"): the foe across the field alone on top, under it the left
        // foe, the player's own card and the right foe; the cursor walks the lower row past the player's card and up
        var layout = battle.TargetLayout;
        Assert.Equal(3, battle.TargetColumns);
        Assert.Equal(new[] { null, battle.EnemySlots[1], null, battle.EnemySlots[2], battle.PlayerSlots[0], battle.EnemySlots[0] }, layout.ToArray());
        int left = battle.TargetChoices.ToList().IndexOf(battle.EnemySlots[2]);
        battle.HUD.TargetMenuIndex = left;
        battle.MoveCursor(1, 0);
        Assert.Equal(battle.EnemySlots[0], battle.TargetChoices[battle.HUD.TargetMenuIndex]);
        battle.MoveCursor(0, -1);
        Assert.Equal(battle.EnemySlots[1], battle.TargetChoices[battle.HUD.TargetMenuIndex]);
    }

    [Fact]
    public void TheRingStandsTheThreeOthersEachOnAPlatformOfTheirOwn()
    {
        // GPU-free (style guide, "A Battle Royal's ring"): the first on the arena's own platform, the second further back
        // and the third nearer, each on the ring's own, every platform clear of every other
        Assert.Equal(BattleStage.EnemySpot, BattlePlaces.Spot(BattleSide.Enemy, 0, 3, royal: true));
        var second = BattlePlaces.Spot(BattleSide.Enemy, 1, 3, royal: true);
        var third = BattlePlaces.Spot(BattleSide.Enemy, 2, 3, royal: true);
        Assert.True(second.Z < 0f && third.Z > 0f, "the second stands behind the field, the third in front of it");
        Assert.True(BattleStage.EnemySpot.X > second.X && second.X > third.X, "left to right from the overview: the third, the second, the first");
        foreach (var (a, b, room) in new[] { (second, third, 2f * BattlePlaces.RingPlatformRadius), (second, BattleStage.EnemySpot, BattleStage.EnemyPlatformRadius + BattlePlaces.RingPlatformRadius),
            (third, BattleStage.EnemySpot, BattleStage.EnemyPlatformRadius + BattlePlaces.RingPlatformRadius), (third, BattleStage.PlayerSpot, BattleStage.PlayerPlatformRadius + BattlePlaces.RingPlatformRadius) })
            Assert.True(Vector3.Distance(a, b) > room, $"{a} and {b} overlap");
        Assert.Equal(BattlePlaces.RingFit, BattlePlaces.Fit(3, royal: true, BattleSide.Enemy));
        // The first, where a single's foe stands, isn't turned; the others turn toward the ring's middle
        Assert.Equal(0f, BattlePlaces.RingTurn(0));
        Assert.NotEqual(0f, BattlePlaces.RingTurn(2));
        // Nothing else stands in the ring: a horde's three and the player's side are as they were
        Assert.Equal(BattlePlaces.Spot(BattleSide.Enemy, 0, 3), BattlePlaces.Spot(BattleSide.Enemy, 0, 3, royal: false));
        Assert.Equal(BattlePlaces.Spot(BattleSide.Player, 0, 1), BattlePlaces.Spot(BattleSide.Player, 0, 1, royal: true));
        Assert.Equal(BattlePlaces.Fit(3), BattlePlaces.Fit(3, royal: false, BattleSide.Enemy));
    }

    [Fact]
    public void ATrainerTheAiPlaysAimsAtTheOthersOfItsOwnSideToo()
    {
        // Ana's only move is Tackle; Bo and Cy stand beside her on the same side, and the player's Pokémon is out of
        // reach of nothing: in a hundred turns she aims at least once at one of them
        bool aimedAtAFellow = false;
        for (uint seed = 1; seed < 40 && !aimedAtAFellow; seed++)
        {
            var core = Royal(new[] { Mon("Golem", 80, "Splash") }, new[]
            {
                Rival("Ana", Mon("Bidoof", 30, "Tackle")),
                Rival("Bo", Mon("Shinx", 30, "Splash")),
                Rival("Cy", Mon("Starly", 30, "Splash"))
            }, new BattleRandom(seed), foes: TrainerAi.Instance);
            if (core.Request is ActionRequest) core.Submit(BattleChoice.Fight(Mine, 0));
            aimedAtAFellow = core.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<MoveShown>().Any(m => m.From == Foe && m.To.Side == BattleSide.Enemy && m.To != Foe);
        }
        Assert.True(aimedAtAFellow, "the AI never aimed at another of the three");
    }
}
