using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 23 · Z6: a trial's totem (<c>docs/mechanics/rulings.md</c>, "Totems and their aura"). It enters with its aura's
/// stages, its first action is a call that is always answered (by the ally its aura names, or its own line), no ball may
/// be thrown, and the battle is won once it falls; the script's word for it; and a Z-Move refused without its crystal.
/// </summary>
public class TotemTests
{
    private static readonly Place Foe2 = new(BattleSide.Enemy, 1);

    private static BattleCore Totem(Pokemon mine, Pokemon totem, TotemAura aura, BattleRandom? rolls = null, bool zRing = false)
    {
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { totem }, Format = BattleFormat.Sos, Random = rolls ?? Calm(), Rules = Ruleset.Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", CannotFlee = true, ZRing = zRing,
            Conditions = new BattleConditions { TotemAura = aura }
        });
        core.Start();
        return core;
    }

    private static List<string> Answer(BattleCore core, int mine = 0, int foes = 0)
    {
        var asked = Assert.IsType<ActionRequest>(core.Request).Places;
        core.Submit(asked.Select(p => p.Side == BattleSide.Player ? BattleChoice.Fight(p, mine) : BattleChoice.Fight(p, foes)).ToList());
        return Lines(core.TakeLog());
    }

    [Fact]
    public void ATotemEntersWithItsAurasStages()
    {
        var gumshoos = Mon("Gumshoos", 12, "Tackle");
        var core = Totem(Mon("Snorlax", 50, "Splash"), gumshoos, new TotemAura(new[] { StatType.Defense }));
        var totem = core.EnemySlots[0];
        Assert.Equal(1, totem.Pokemon!.StatStages.GetValueOrDefault(StatType.Defense));
        Assert.Equal(0, totem.Pokemon.StatStages.GetValueOrDefault(StatType.Attack));
        var aura = core.Log.OfType<Said>().Single(s => s.Shows.OfType<TotemAuraRaised>().Any());
        Assert.Equal(Text.Of("battle.totemAura", ("pokemon", totem.Name)), aura.Text);
        Assert.Equal(Foe, aura.Shows.OfType<TotemAuraRaised>().Single().Place);
        // The stage comes after the aura's line, on a line of its own
        var stage = core.Log.OfType<Said>().First(s => s.Shows.OfType<StageChanged>().Any());
        Assert.True(core.Log.ToList().IndexOf(aura) < core.Log.ToList().IndexOf(stage));

        // "all" is the five stats, by two stages
        var all = Totem(Mon("Snorlax", 50, "Splash"), Mon("Lurantis", 24, "Leafage"), new TotemAura(TotemAura.StatsOf("all").ToList(), 2));
        foreach (var stat in new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed })
            Assert.Equal(2, all.EnemySlots[0].Pokemon!.StatStages.GetValueOrDefault(stat));
    }

    [Fact]
    public void ItsFirstActionIsACallAlwaysAnsweredByTheAllyItsAuraNames()
    {
        // A full Wishiwashi would never call on a roll of 99; a totem calls on its first action whatever the roll
        var core = Totem(Mon("Snorlax", 50, "Splash"), Mon("Wishiwashi", 20, "Water Gun"), new TotemAura(new[] { StatType.Defense }, 1, "Alomomola"),
            Calm().Force(RollKind.Call, 99, 99, 99, 99).Force(RollKind.Ally, 0));
        string caller = core.EnemySlots[0].Name;
        core.TakeLog();
        var said = Answer(core);
        Assert.Contains(Text.Of("battle.calledForHelp", ("pokemon", caller)), said);
        Assert.DoesNotContain(said, s => s.Contains("Water Gun"));
        Assert.True(core.EnemySlots[1].IsActive);
        Assert.Equal("Alomomola", core.EnemySlots[1].Pokemon!.Species.Name);
        Assert.Equal(Foe2, core.Log.OfType<AllyJoined>().Single().Place);

        // Its own line where its aura names none
        var own = Totem(Mon("Snorlax", 50, "Splash"), Mon("Gumshoos", 12, "Tackle"), new TotemAura(new[] { StatType.Defense }),
            Calm().Force(RollKind.Call, 99, 99).Force(RollKind.Ally, 0, 0));
        Answer(own);
        Assert.Contains(own.EnemySlots[1].Pokemon!.Species.Name, new[] { "Yungoos", "Gumshoos" });
    }

    [Fact]
    public void NoBallMayBeThrownAndTheBattleIsWonOnceTheTotemFalls()
    {
        // Rock Throw strikes one foe alone: the totem, at the first place
        var core = Totem(Idling(Mon("Golem", 60, "Rock Throw")), Mon("Gumshoos", 12, "Tackle"), new TotemAura(new[] { StatType.Defense }),
            Calm().Force(RollKind.Call, 99, 99).Force(RollKind.Ally, 0, 0));
        Assert.Equal(BattleCore.TotemRefusal, core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
        Assert.True(core.CannotFlee);
        Answer(core, mine: 1);
        Assert.True(core.EnemySlots[1].IsActive, "the ally came");
        // Only the totem falls: the ally stands, and the battle is won all the same
        core.EnemySlots[0].Pokemon!.CurrentHP = 1;
        core.EnemySlots[1].Pokemon!.CurrentHP = core.EnemySlots[1].Pokemon!.MaxHP;
        var asked = Assert.IsType<ActionRequest>(core.Request).Places;
        core.Submit(asked.Select(p => p.Side == BattleSide.Player ? BattleChoice.Fight(p, 0, Foe) : BattleChoice.Fight(p, 0)).ToList());
        var said = Lines(core.TakeLog());
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.False(core.EnemySlots[1].Pokemon!.IsFainted);
        Assert.Contains(said, s => s.StartsWith("Lucas defeated the totem"));
    }

    [Fact]
    public void AnyOtherWildBattleIsAsItWas()
    {
        var party = new Party();
        party.Add(Mon("Snorlax", 50, "Splash"));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Mon("Gumshoos", 12, "Tackle") }, Format = BattleFormat.Sos, Random = Calm(),
            Rules = Ruleset.Modern, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        Assert.Null(core.Totem);
        Assert.Empty(core.Log.OfType<Said>().SelectMany(s => s.Shows).OfType<TotemAuraRaised>());
        Assert.Null(core.WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
    }

    [Fact]
    public void TheRecordedFileKeepsTheAura()
    {
        var file = new RecordedConditions { TotemAura = new TotemAura(new[] { StatType.Speed }, 2, "Comfey") };
        string json = GameDataFiles.Serialize(file);
        var back = GameDataFiles.Deserialize<RecordedConditions>(json);
        Assert.Equal(new[] { StatType.Speed }, back.TotemAura!.Stats.ToArray());
        Assert.Equal(2, back.TotemAura.Stages);
        Assert.Equal("Comfey", back.TotemAura.Ally);
    }

    [Fact]
    public void AScriptMeetsATotemWithItsAura()
    {
        const string source = "script T\n  wildbattle \"Wishiwashi\" 20 totem defense spdefense 2 ally \"Alomomola\"\n  end\n";
        var parsed = ScriptParser.Parse("test", source);
        var i = parsed.Single().Code.Single(x => x.Op == Op.WildBattle);
        Assert.True(i.Option, "a totem can't be run from");
        Assert.Equal(new[] { StatType.Defense, StatType.SpDefense }, i.Totem!.Stats.ToArray());
        Assert.Equal(2, i.Totem.Stages);
        Assert.Equal("Alomomola", i.Totem.Ally);
        Assert.ThrowsAny<System.Exception>(() => ScriptParser.Parse("test", "script T\n  wildbattle \"Gumshoos\" 12 totem\n  end\n"));
        Assert.ThrowsAny<System.Exception>(() => ScriptParser.Parse("test", "script T\n  wildbattle \"Gumshoos\" 12 ally \"Yungoos\"\n  end\n"));

        var host = new HeadlessScriptHost();
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Litten")!, 20));
        var library = ScriptLibrary.FromSources(("test", source));
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find("T", "test")!, null);
        runner.RunToEnd();
        Assert.Contains("totembattle Wishiwashi 20 Defense,SpDefense+2 ally Alomomola Won", host.Log);
    }

    [Fact]
    public void AZMoveIsRefusedWithoutItsCrystal()
    {
        var bare = Mon("Litten", 20, "Tackle");
        var core = Totem(bare, Mon("Gumshoos", 12, "Tackle"), new TotemAura(new[] { StatType.Defense }), zRing: true);
        Assert.NotNull(core.WhyNotZMove(core.PlayerSlots[0], 0));

        var holding = Mon("Litten", 20, "Tackle");
        holding.HeldItem = ItemDatabase.Get("Normalium Z");
        var armed = Totem(holding, Mon("Gumshoos", 12, "Tackle"), new TotemAura(new[] { StatType.Defense }), zRing: true);
        Assert.Null(armed.WhyNotZMove(armed.PlayerSlots[0], 0));
        // And with the crystal but no Z-Ring, refused
        var ringless = Mon("Litten", 20, "Tackle");
        ringless.HeldItem = ItemDatabase.Get("Normalium Z");
        var bareHanded = Totem(ringless, Mon("Gumshoos", 12, "Tackle"), new TotemAura(new[] { StatType.Defense }));
        Assert.NotNull(bareHanded.WhyNotZMove(bareHanded.PlayerSlots[0], 0));
    }
}
