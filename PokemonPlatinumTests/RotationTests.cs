using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 14 · B7: rotation battles (<c>docs/mechanics/rulings.md</c>, "Formats of the later games", "Rotation battles";
/// style guide, "Stage", "A rotation's turntable", "Camera (G8)" and "Battle panels", "The rotation arrows"). One place a
/// side under the rules that have them (<see cref="Ruleset.Rotations"/>), the front of a turntable of three; a move
/// chosen for a Pokémon behind turns the table as the turn begins and that Pokémon uses it; what lasted on one rotated
/// out stays on it; the end of the turn reaches the front alone; a fallen front with nobody to send in is turned away;
/// the AI turns to its best move; a record and its replay; a hundred rotation battles played to their end; the face.
/// </summary>
public class RotationTests
{
    [Fact]
    public void ARotationIsOneAgainstOneWithThreeOnEachTurntable()
    {
        Pokemon a = Mon("Golem", 40, "Tackle"), b = Mon("Bidoof", 40, "Tackle"), c = Mon("Ponyta", 40, "Ember"), d = Mon("Shinx", 40, "Tackle");
        var core = Rotations(new[] { a, b, c, d }, new[] { Mon("Bidoof", 30), Mon("Starly", 30), Mon("Shinx", 30) });
        Assert.Equal(BattleFormat.Rotation, core.Format);
        Assert.True(core.IsRotation);
        Assert.True(core.IsSingle);
        Assert.Single(core.PlayerSlots);
        Assert.Single(core.EnemySlots);
        Assert.Equal(1, BattleFormat.Rotation.PlacesOf(BattleSide.Player));
        // The front, the seat a turn of +1 brings forward, the one a turn of −1 brings forward; the fourth waits on the team
        Assert.Equal(new Pokemon?[] { a, b, c }, core.Turntable(BattleSide.Player).ToArray());
        Assert.Equal(3, core.Turntable(BattleSide.Enemy).Count);
        Assert.False(core.CanSendIn(core.At(Mine), 1));
        Assert.True(core.CanSendIn(core.At(Mine), 3));
        // All three foes are seen from the start
        Assert.Equal(3, core.Log.OfType<Seen>().Count());
        Assert.Empty(Rotations(new[] { a, b, c }, new[] { Mon("Bidoof", 30), Mon("Starly", 30), Mon("Shinx", 30) }, rules: Ruleset.Platinum).Turntable(BattleSide.Player));
    }

    [Fact]
    public void WhatCantBeARotationFallsBack()
    {
        Assert.Equal(BattleFormat.Single, Rotations(new[] { Mon("Golem", 40), Mon("Golem", 40), Mon("Golem", 40) }, new[] { Mon("Bidoof", 30), Mon("Bidoof", 30), Mon("Bidoof", 30) }, rules: Ruleset.Platinum).Format);
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Rotation, 2, 0, false, 1, 3, 0, BattleKind.Normal, Ruleset.Modern, out _));
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Rotation, 3, 0, false, 1, 2, 0, BattleKind.Normal, Ruleset.Modern, out _));
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Rotation, 3, 0, false, 0, 0, 3, BattleKind.Normal, Ruleset.Modern, out _));
        Assert.Equal(BattleFormat.Single, BattleFormats.FallBack(BattleFormat.Rotation, 3, 0, false, 2, 3, 0, BattleKind.Normal, Ruleset.Modern, out _));
        Assert.Equal(BattleFormat.Rotation, BattleFormats.FallBack(BattleFormat.Rotation, 3, 0, false, 1, 3, 0, BattleKind.Normal, Ruleset.Modern, out _));
        // A trainer asks for one by its word
        Assert.Equal(BattleFormat.Rotation, new Trainer { Format = "rotation" }.AskedFormat);
    }

    [Fact]
    public void ARotatedInPokemonUsesTheMoveChosenForIt()
    {
        Pokemon front = Mon("Bidoof", 50, "Tackle"), next = Mon("Golem", 50, "Rock Throw"), before = Mon("Ponyta", 50, "Ember");
        var core = Rotations(new[] { front, next, before }, new[] { Mon("Snorlax", 50, "Splash"), Mon("Snorlax", 50), Mon("Snorlax", 50) });
        var said = RotationTurn(core, (1, 0));
        InOrder(said, Text.Of("battle.rotatedToTheFront", ("pokemon", next.DisplayName)), Text.Of("battle.used", ("user", next.DisplayName), ("move", "Rock Throw")));
        Assert.DoesNotContain(Text.Of("battle.used", ("user", front.DisplayName), ("move", "Tackle")), said);
        Assert.Equal(new Pokemon?[] { next, before, front }, core.Turntable(BattleSide.Player).ToArray());
        Assert.Contains(core.Log, e => e is Said s && s.Shows.OfType<Rotated>().Any(r => r.Place == Mine && r.Way == 1 && r.Pokemon == next));
        // And the other way: −1 brings the seat before the front forward
        said = RotationTurn(core, (-1, 0));
        Assert.Contains(Text.Of("battle.used", ("user", front.DisplayName), ("move", "Tackle")), said);
        Assert.Equal(new Pokemon?[] { front, next, before }, core.Turntable(BattleSide.Player).ToArray());
    }

    [Fact]
    public void ARotationKeepsStagesAndASubstitute()
    {
        var front = Mon("Machamp", 50, "Substitute", "Swords Dance");
        var behind = Mon("Bidoof", 50, "Tackle");
        var core = Rotations(new[] { front, behind, Mon("Bidoof", 50, "Tackle") }, new[] { Mon("Magikarp", 50, "Splash"), Mon("Magikarp", 50, "Splash"), Mon("Magikarp", 50, "Splash") });
        RotationTurn(core, (0, 0));
        RotationTurn(core, (0, 1));
        Assert.True(core.At(Mine).HasSubstitute);
        Assert.Equal(2, front.StatStages[StatType.Attack]);
        // Rotated out, then back: the doll and the stages are as they were
        RotationTurn(core, (1, 0));
        Assert.Same(behind, core.At(Mine).Pokemon);
        Assert.False(core.At(Mine).HasSubstitute);
        RotationTurn(core, (-1, 1));
        Assert.Same(front, core.At(Mine).Pokemon);
        Assert.True(core.At(Mine).HasSubstitute);
        Assert.Equal(4, front.StatStages[StatType.Attack]);
        Assert.Contains(core.Log, e => e is Said s && s.Shows.OfType<Rotated>().Any(r => r.Pokemon == front && r.Substitute));
    }

    [Fact]
    public void PoisonBitesTheFrontAlone()
    {
        var poisoned = Mon("Machamp", 50, "Splash");
        poisoned.Status = StatusCondition.Poison;
        var core = Rotations(new[] { poisoned, Mon("Bidoof", 50, "Splash"), Mon("Bidoof", 50, "Splash") }, new[] { Mon("Magikarp", 50, "Splash"), Mon("Magikarp", 50, "Splash"), Mon("Magikarp", 50, "Splash") });
        RotationTurn(core, (0, 0));
        int afterTheFront = poisoned.CurrentHP;
        Assert.True(afterTheFront < poisoned.MaxHP);
        RotationTurn(core, (1, 0));
        RotationTurn(core, (0, 0));
        Assert.Equal(afterTheFront, poisoned.CurrentHP);
        // The line that brings it back says what it brings
        RotationTurn(core, (-1, 0));
        Assert.Contains(core.Log, e => e is Said s && s.Shows.OfType<Rotated>().Any(r => r.Pokemon == poisoned)
            && s.Shows.OfType<CameInWith>().Any(c => c.Status == StatusCondition.Poison));
    }

    [Fact]
    public void WhatCantBeRotatedIsRefused()
    {
        var fallen = Mon("Bidoof", 50, "Tackle");
        fallen.CurrentHP = 0;
        fallen.Status = StatusCondition.Faint;
        var core = Rotations(new[] { Mon("Golem", 50, "Tackle"), Mon("Bidoof", 50, "Tackle"), Mon("Bidoof", 50, "Tackle"), fallen }, new[] { Mon("Magikarp", 50), Mon("Magikarp", 50), Mon("Magikarp", 50) });
        string cant = Text.Of("battle.cantRotate", ("pokemon", core.At(Mine).Name));
        Assert.Null(core.WhyNot(BattleChoice.Fight(Mine, 0) with { Rotation = 1 }));
        Assert.Equal(cant, core.WhyNot(BattleChoice.Fight(Mine, 0) with { Rotation = 2 }));
        Assert.Equal(cant, core.WhyNot(BattleChoice.Switch(Mine, 3) with { Rotation = 1 }));
        Assert.Equal(cant, core.WhyNot(BattleChoice.Fight(Mine, 0) with { Rotation = 1, Mega = true }));
        // A move the Pokémon behind hasn't is refused as its own would be
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Mine, 3) with { Rotation = 1 }));
        // And nothing of a single battle turns
        var single = Against(new[] { Mon("Golem", 50, "Tackle"), Mon("Bidoof", 50, "Tackle"), Mon("Bidoof", 50, "Tackle") }, new[] { Mon("Magikarp", 50) });
        Assert.NotNull(single.WhyNot(BattleChoice.Fight(Mine, 0) with { Rotation = 1 }));
    }

    [Fact]
    public void AFallenFrontIsReplacedFromTheTeamFirstAndElseTurnedAway()
    {
        var weak = Mon("Bidoof", 20, "Splash");
        weak.CurrentHP = 1;
        var next = Mon("Golem", 50, "Splash");
        var core = Rotations(new[] { weak, next, Mon("Bidoof", 50, "Splash") }, new[] { Mon("Machamp", 60, "Karate Chop"), Mon("Magikarp", 50), Mon("Magikarp", 50) });
        var said = RotationTurn(core, (0, 0), (0, 0));
        Assert.True(weak.IsFainted);
        Assert.Equal(BattleResult.None, core.Result);
        // Nobody on the team to send: the seat after it comes forward
        Assert.Contains(Text.Of("battle.rotatedToTheFront", ("pokemon", next.DisplayName)), said);
        Assert.Same(next, core.At(Mine).Pokemon);
        Assert.IsType<ActionRequest>(core.Request);
        // A fourth on the team is asked for instead
        var weak2 = Mon("Bidoof", 20, "Splash");
        weak2.CurrentHP = 1;
        var bench = Mon("Shinx", 50, "Splash");
        var other = Rotations(new[] { weak2, Mon("Golem", 50), Mon("Bidoof", 50), bench }, new[] { Mon("Machamp", 60, "Karate Chop"), Mon("Magikarp", 50), Mon("Magikarp", 50) });
        RotationTurn(other, (0, 0), (0, 0));
        var replace = Assert.IsType<ReplacementRequest>(other.Request);
        Assert.False(other.CanSendIn(other.At(replace.Place), 1));
        other.Submit(BattleChoice.Switch(replace.Place, 3));
        Assert.Same(bench, other.At(Mine).Pokemon);
        Assert.Contains(bench, other.Turntable(BattleSide.Player));
    }

    [Fact]
    public void TheAiTurnsToThePokemonWithTheBestMove()
    {
        // The player's Ponyta in front; the foe's Bidoof in front and a Squirtle behind it with Water Gun
        var squirtle = Mon("Squirtle", 40, "Water Gun");
        var core = Rotations(new[] { Mon("Ponyta", 40, "Splash"), Mon("Bidoof", 40, "Splash"), Mon("Bidoof", 40, "Splash") },
            new[] { Mon("Bidoof", 40, "Splash"), squirtle, Mon("Bidoof", 40, "Splash") }, foe: TrainerAi.Instance);
        Assert.True(TrainerAi.RotationScore(core, squirtle, core.At(Foe), core.At(Mine)) > TrainerAi.RotationScore(core, core.At(Foe).Pokemon!, core.At(Foe), core.At(Mine)));
        var said = RotationTurn(core, (0, 0));
        Assert.Contains(Text.Of("battle.rotatedToTheFront", ("pokemon", core.At(Foe).Name)), said);
        Assert.Same(squirtle, core.At(Foe).Pokemon);
        Assert.Contains(said, l => l.Contains("Water Gun"));
    }

    // ------------------------------------------------------------------ a record, and the fuzz

    private static BattleCore Fresh(BattleFormat format, uint seed = 20261025)
    {
        var party = new Party();
        party.Add(Mon("Grotle", 30, "Razor Leaf", "Bite"));
        party.Add(Mon("Luxio", 30, "Spark", "Bite"));
        party.Add(Mon("Monferno", 30, "Ember", "Mach Punch"));
        party.Add(Mon("Bidoof", 28, "Tackle"));
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, Format = "rotation", Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic };
        foreach (var p in new[] { Mon("Staravia", 28, "Wing Attack", "Quick Attack"), Mon("Kricketune", 28, "Fury Cutter"), Mon("Ponyta", 29, "Ember"), Mon("Shinx", 27, "Spark") })
            trainer.Party.Add(p);
        return new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Format = format, Random = new BattleRandom(seed), Rules = Ruleset.Modern,
            PlayerController = null, EnemyController = TrainerAi.Instance, PlayerName = "Lucas"
        });
    }

    [Fact]
    public void ARecordedRotationReplaysTheSameAndOnlyAsARotation()
    {
        var core = Fresh(BattleFormat.Rotation);
        Assert.Equal(BattleFormat.Rotation, core.Format);
        core.Start();
        int turned = 0;
        for (int step = 0, turn = 0; step < 300 && core.Request != null; step++)
        {
            switch (core.Request)
            {
                case ActionRequest asked:
                {
                    // Turn the table every other turn while a seat behind can fight, else fight with the front
                    var place = asked.Places.Single();
                    var me = core.At(place);
                    int way = turn % 2 == 1 && core.RotatesIn(me, 1) is { IsFainted: false } ? 1 : 0;
                    var fighter = way == 0 ? me.Pokemon! : core.RotatesIn(me, way)!;
                    var choice = BattleChoice.Fight(place, turn % fighter.Moves.Count) with { Rotation = way };
                    if (core.WhyNot(choice) != null) choice = BattleChoice.Fight(place, -1) with { Rotation = way };
                    if (core.WhyNot(choice) != null) choice = BattleChoice.Fight(place, 0);
                    if (core.WhyNot(choice) != null) choice = BattleChoice.Fight(place, -1);
                    if (choice.Rotation != 0) turned++;
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
        Assert.True(turned >= 3, $"the table turned {turned} times");
        var record = core.Record!;
        Assert.Equal(BattleFormat.Rotation, record.Format);
        Assert.Contains(record.Answers.SelectMany(a => a), c => c.Rotation != 0);

        var again = Fresh(BattleFormat.Rotation);
        again.Replay(record);
        Assert.Equal(Lines(core.Log), Lines(again.Log));
        Assert.Equal(core.Result, again.Result);
        // The same seed and answers in a single are another battle: refused
        Assert.Throws<ArgumentException>(() => Fresh(BattleFormat.Single).Replay(record));
        // And the Vs. Recorder's choice keeps the turn
        var kept = RecordedChoice.Of(BattleChoice.Fight(Mine, 1) with { Rotation = -1 }).ToChoice();
        Assert.Equal(-1, kept.Rotation);
    }

    [Fact]
    public void AHundredRotationsAlwaysEndAndLeaveNothingOutOfRange()
    {
        var species = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        int rotations = 0;
        for (int battle = 0; battle < 100; battle++)
        {
            var dice = new Random(battle * 7919 + 5);
            var mine = new Party();
            for (int i = 0; i < 3 + battle % 4; i++) mine.Add(new Pokemon(species[dice.Next(species.Count)], dice.Next(10, 80), dice));
            var theirs = Enumerable.Range(0, 3 + (battle / 4) % 4).Select(_ => new Pokemon(species[dice.Next(species.Count)], dice.Next(10, 80), dice)).ToList();
            var trainer = new Trainer { Id = "fuzz", Name = "Fuzz", TrainerClass = "Ace Trainer", Format = "rotation", Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic | PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.EvalAttack };
            foreach (var p in theirs) trainer.Party.Add(p);
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = mine, Trainers = new List<Trainer> { trainer }, Random = new BattleRandom((uint)battle), Rules = Ruleset.Modern,
                Format = BattleFormat.Rotation, PlayerController = TrainerAi.Instance
            });
            Assert.Equal(BattleFormat.Rotation, core.Format);
            core.Start();

            Assert.Null(core.Request);
            Assert.True(core.Result is BattleResult.PlayerVictory or BattleResult.PlayerDefeat, $"rotation {battle} ended as {core.Result}");
            Assert.True(core.Turn < 1000, $"rotation {battle} took {core.Turn} turns");
            var log = core.TakeLog();
            Assert.IsType<Ended>(log.Last());
            rotations += log.OfType<Said>().SelectMany(s => s.Shows).OfType<Rotated>().Count();
            foreach (var p in mine.Members.Concat(theirs))
            {
                Assert.InRange(p.CurrentHP, 0, p.MaxHP);
                Assert.All(p.Moves, m => Assert.InRange(m.CurrentPP, 0, m.MaxPP));
                Assert.All(p.StatStages.Values, stage => Assert.InRange(stage, -6, 6));
            }
            bool mineDown = mine.Members.All(p => p.IsFainted), theirsDown = theirs.All(p => p.IsFainted);
            Assert.True(core.Result == BattleResult.PlayerDefeat ? mineDown : theirsDown, $"rotation {battle}: {core.Result}");
        }
        Assert.True(rotations > 100, $"the tables turned {rotations} times in a hundred battles");
    }

    // ------------------------------------------------------------------ the face

    private static BattleEngine RotationBattle(Pokemon[] mine, Pokemon[] theirs)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, Format = "rotation" };
        foreach (var p in theirs) trainer.Party.Add(p);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { trainer },
            Format = BattleFormat.Rotation, Random = Steady(), Rules = Ruleset.Modern
        });
        Settle(battle);
        return battle;
    }

    [Fact]
    public void TheArrowsBesideFightTurnTheTableForTheMove()
    {
        Pokemon front = Mon("Bidoof", 50, "Tackle"), next = Mon("Golem", 50, "Rock Throw", "Tackle"), before = Mon("Ponyta", 50, "Ember");
        var battle = RotationBattle(new[] { front, next, before }, new[] { Mon("Magikarp", 50, "Splash"), Mon("Magikarp", 50, "Splash"), Mon("Magikarp", 50, "Splash") });
        Assert.True(battle.IsRotation);
        Assert.True(battle.Anim.Turntables);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        Assert.Same(front, battle.Fighter);
        Assert.True(battle.RotateChoice(1));
        Assert.Equal(1, battle.RotationArmed);
        Assert.Same(next, battle.Fighter);
        // The same way again turns it back, and the other way to the seat before
        Assert.True(battle.RotateChoice(1));
        Assert.Same(front, battle.Fighter);
        Assert.True(battle.RotateChoice(-1));
        Assert.Same(before, battle.Fighter);
        Assert.True(battle.RotateChoice(1));

        battle.SelectMainMenuOption(0);
        Assert.Equal(BattleMenuState.Moves, battle.HUD.MenuState);
        battle.SelectMove(0);
        var said = Settle(battle);
        Assert.Contains(Text.Of("battle.rotatedToTheFront", ("pokemon", next.DisplayName)), said);
        Assert.Contains(Text.Of("battle.used", ("user", next.DisplayName), ("move", "Rock Throw")), said);
        Assert.Same(next, battle.PlayerSlots[0].Pokemon);
        Assert.Same(next, battle.Anim[BattleSide.Player, 0].Shown);
        Assert.Equal(1, battle.Anim.RotationWay(BattleSide.Player));
        battle.Anim.Update(BattleAnimator.RotationTime, null, null);
        Assert.Equal(1f, battle.Anim.TurntableTurn(BattleSide.Player));
        Assert.Equal(0, battle.RotationArmed);
        // Nothing turns outside a rotation battle
        var single = new BattleEngine(new BattleSetup
        {
            PlayerParty = Party(Mon("Golem", 50, "Tackle")), Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new List<Pokemon> { Mon("Bidoof", 5) },
            Random = Steady(), Rules = Ruleset.Modern
        });
        Settle(single);
        Assert.False(single.RotateChoice(1));
        Assert.False(single.Anim.Turntables);
    }

    private static Party Party(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    [Fact]
    public void TheTurntableTurnsInTheSceneAndTheCameraWatchesIt()
    {
        var anim = new BattleAnimator { Turntables = true };
        var places = new FxPlaces();
        var fx = new FxList();
        RotationFx.Emit(anim, places, fx);
        int standing = fx.Count;
        Assert.InRange(standing, 1, RotationFx.MaxQuadsPerSide * 2);
        Assert.All(fx.Quads, q => Assert.True(q.Depth, "a turntable lies in the scene"));

        anim.Rotate(BattleSide.Enemy, Mon("Bidoof", 30), 1);
        Assert.Equal(0f, anim.TurntableTurn(BattleSide.Enemy));
        var (_, kind, _) = BattleCamera.Direct(anim, places);
        Assert.Equal(ShotKind.Rotation, kind);
        fx.Clear();
        anim.Update(BattleAnimator.RotationTime / 2f, null, null);
        RotationFx.Emit(anim, places, fx);
        Assert.InRange(fx.Count, standing + 1, RotationFx.MaxQuadsPerSide * 2);
        float half = anim.TurntableTurn(BattleSide.Enemy);
        Assert.InRange(half, 0.1f, 0.9f);
        anim.Update(BattleAnimator.RotationTime, null, null);
        Assert.Equal(1f, anim.TurntableTurn(BattleSide.Enemy));
        Assert.Equal(-1f, anim.RotationAge(BattleSide.Enemy));
        Assert.NotEqual(ShotKind.Rotation, BattleCamera.Direct(anim, places).Kind);

        // A battle of any other format draws no turntable
        fx.Clear();
        RotationFx.Emit(new BattleAnimator(), places, fx);
        Assert.Equal(0, fx.Count);
    }
}
