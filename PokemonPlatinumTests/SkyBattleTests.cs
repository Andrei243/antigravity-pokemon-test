using System;
using System.Collections.Generic;
using System.Linq;
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
/// Plan 14 · B2: sky battles (<c>docs/mechanics/rulings.md</c>, "Sky battles"). A flag of the battle's conditions, never a
/// format: only Pokémon that fly or float take part (<see cref="SkyBattles.CanFly"/>), the battle refuses to begin with any
/// other, the moves that need the ground are refused where every move is refused, the battle says so as it opens, a record
/// keeps it, and the stage is the sky's.
/// </summary>
public class SkyBattleTests
{
    private static BattleCore Sky(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null, IBattleController? foe = null)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "sky", Name = "Gale", TrainerClass = "Ace Trainer", PrizeMoney = 100, Format = "sky", Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = rolls ?? Calm(), Rules = Ruleset.Modern,
            Conditions = new BattleConditions { InTheAir = true }, PlayerController = null, EnemyController = foe, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    [Fact]
    public void WhoMayTakePartFliesOrFloats()
    {
        Assert.True(SkyBattles.CanFly(Mon("Staravia", 30)));
        Assert.True(SkyBattles.CanFly(Mon("Gyarados", 30)));
        Assert.False(SkyBattles.CanFly(Mon("Bidoof", 30)));
        // A Flying type that flaps rather than flies (X and Y's own list)
        Assert.False(SkyBattles.CanFly(Mon("Starly", 30)));
        Assert.False(SkyBattles.CanFly(Mon("Chatot", 30)));
        // Levitate floats, whatever the types
        var bronzor = Mon("Bronzor", 30);
        bronzor.AbilityName = "Levitate";
        Assert.True(SkyBattles.CanFly(bronzor));
        // An Egg never does
        var egg = Mon("Staravia", 1);
        egg.IsEgg = true;
        Assert.False(SkyBattles.CanFly(egg));
        // The team the game hands a sky battle is the fliers alone, the same Pokémon in their order
        var team = new Party();
        Pokemon a = Mon("Bidoof", 20), b = Mon("Staravia", 20), c = Mon("Golem", 20), d = Mon("Gyarados", 20);
        foreach (var p in new[] { a, b, c, d }) team.Add(p);
        Assert.Equal(new[] { b, d }, SkyBattles.Fliers(team).Members);
        Assert.True(SkyBattles.CanTakePart(team));
        b.CurrentHP = 0;
        d.CurrentHP = 0;
        Assert.False(SkyBattles.CanTakePart(team));
    }

    [Fact]
    public void TheFilesListsAreTheDatasOwnMovesAndSpecies()
    {
        Assert.NotEmpty(SkyBattles.File.GroundedMoves);
        Assert.All(SkyBattles.File.GroundedMoves, name => Assert.True(MoveDatabase.Get(name) != null, $"{name} is no move of moves.json"));
        Assert.All(SkyBattles.File.GroundedSpecies, name => Assert.True(PokemonDatabase.Get(name) != null, $"{name} is no species of species.json"));
        Assert.All(SkyBattles.File.GroundedSpecies, name => Assert.True(PokemonDatabase.Get(name)!.PrimaryType == PokemonType.Flying || PokemonDatabase.Get(name)!.SecondaryType == PokemonType.Flying, $"{name}: the list is of Flying types that can't take part"));
        Assert.True(SkyBattles.Grounded(MoveDatabase.Get("Earthquake")!));
        Assert.False(SkyBattles.Grounded(MoveDatabase.Get("Wing Attack")!));
    }

    [Fact]
    public void ASkyBattleOpensWithItsLineAndRefusesTheGroundsMoves()
    {
        var core = Sky(new[] { Mon("Gyarados", 40, "Earthquake", "Waterfall", "Surf") }, new[] { Mon("Staravia", 40, "Wing Attack") });
        Assert.True(core.InTheAir);
        Assert.Equal(BattleFormat.Single, core.Format);
        Assert.Contains(BattleCore.SkyLine, Lines(core.Log));
        var mine = core.At(Mine);
        Assert.NotNull(core.WhyNotMove(mine, mine.Pokemon!.Moves[0]));
        Assert.Null(core.WhyNotMove(mine, mine.Pokemon!.Moves[1]));
        Assert.NotNull(core.WhyNotMove(mine, mine.Pokemon!.Moves[2]));
        Assert.Equal(new[] { "Waterfall" }, core.UsableMoves(mine).Select(m => m.Name));
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0)));
        // On the ground the same move is anyone's
        var ground = Against(new[] { Mon("Gyarados", 40, "Earthquake") }, new[] { Mon("Staravia", 40, "Wing Attack") }, rules: Ruleset.Modern);
        Assert.False(ground.InTheAir);
        Assert.Null(ground.WhyNotMove(ground.At(Mine), ground.At(Mine).Pokemon!.Moves[0]));
        Assert.DoesNotContain(BattleCore.SkyLine, Lines(ground.Log));
    }

    [Fact]
    public void ASkyBattleRefusesToBeginWithOneThatCantFly()
    {
        Assert.Throws<ArgumentException>(() => Sky(new[] { Mon("Gyarados", 40, "Waterfall"), Mon("Bidoof", 40, "Tackle") }, new[] { Mon("Staravia", 40, "Wing Attack") }));
        Assert.Throws<ArgumentException>(() => Sky(new[] { Mon("Gyarados", 40, "Waterfall") }, new[] { Mon("Shinx", 40, "Tackle") }));
    }

    [Fact]
    public void TheTrainerAiNeverReachesForAGroundedMove()
    {
        var core = Sky(new[] { Mon("Gyarados", 50, "Waterfall") }, new[] { Mon("Gyarados", 50, "Earthquake", "Surf", "Bite") }, new BattleRandom(7), TrainerAi.Instance);
        for (int i = 0; i < 20 && core.Request is ActionRequest; i++)
        {
            core.Submit(BattleChoice.Fight(Mine, 0));
            foreach (var p in new[] { core.At(Mine).Pokemon, core.At(Foe).Pokemon }) if (p != null) { p.CurrentHP = p.MaxHP; foreach (var m in p.Moves) m.CurrentPP = m.MaxPP; }
        }
        var used = core.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<MoveShown>().Where(m => m.From.Side == BattleSide.Enemy).Select(m => m.Move).Distinct().ToList();
        Assert.Contains("Bite", used);
        Assert.DoesNotContain("Earthquake", used);
        Assert.DoesNotContain("Surf", used);
    }

    [Fact]
    public void ARecordKeepsTheSkyAndItsFileToo()
    {
        BattleCore Fresh(bool sky)
        {
            var party = new Party();
            party.Add(Mon("Staraptor", 40, "Brave Bird", "Close Combat"));
            party.Add(Mon("Gyarados", 40, "Waterfall"));
            var trainer = new Trainer { Id = "sky", Name = "Gale", TrainerClass = "Ace Trainer", PrizeMoney = 100, Format = "sky", Ai = PokemonPlatinumEngine.Battle.Sim.Ai.AiFlags.Basic };
            trainer.Party.Add(Mon("Staravia", 36, "Wing Attack"));
            trainer.Party.Add(Mon("Drifblim", 36, "Gust"));
            return new BattleCore(new CoreSetup
            {
                PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = new BattleRandom(20261103), Rules = Ruleset.Modern,
                Conditions = new BattleConditions { InTheAir = sky }, PlayerController = null, EnemyController = TrainerAi.Instance, PlayerName = "Lucas"
            });
        }
        var core = Fresh(true);
        core.Start();
        for (int step = 0; step < 200 && core.Request != null; step++)
        {
            if (core.Request is ReplacementRequest r)
                core.Submit(BattleChoice.Switch(r.Place, Enumerable.Range(0, core.PlayerParty.Count).First(i => core.CanSendIn(core.At(r.Place), i))));
            else core.Submit(BattleChoice.Fight(Mine, 0));
        }
        Assert.Null(core.Request);
        var record = core.Record!;
        Assert.True(record.InTheAir);
        var again = Fresh(true);
        again.Replay(record);
        Assert.Equal(Lines(core.Log), Lines(again.Log));
        // On the ground it is another battle: refused
        Assert.Throws<ArgumentException>(() => Fresh(false).Replay(record));

        // The Vs. Recorder's file keeps the flag through its rebuilt setup
        var file = BattleRecordFile.Opening(new CoreSetup
        {
            PlayerParty = new Party(), Rules = Ruleset.Modern, Conditions = new BattleConditions { InTheAir = true }
        });
        Assert.True(file.Conditions.InTheAir);
        Assert.True(file.ToSetup().Conditions.InTheAir);
    }

    [Fact]
    public void ASkyTrainerAsksForNoFormatButTheAir()
    {
        var trainer = new Trainer { Format = "sky" };
        Assert.True(trainer.InTheAir);
        Assert.Null(trainer.AskedFormat);
        Assert.False(new Trainer { Format = "triple" }.InTheAir);
        Assert.False(new Trainer().InTheAir);
    }

    [Fact]
    public void TheSkysStageIsUnderTheOpenSkyWithSomethingInTheAir()
    {
        var spec = new ArenaSpec(BattleArena.Sky);
        Assert.True(spec.Outdoors);
        Assert.False(spec.Hall);
        var fx = new FxList();
        ArenaFx.Emit(spec, 3.7f, night: false, fx);
        Assert.True(fx.Count > 0);
        Assert.All(fx.Quads, q => Assert.True(q.Depth));
        Assert.Equal(BattleStage.PlatformHeight, BattleStage.PlatformTopOf(BattleArena.Sky));
    }
}
