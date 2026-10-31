using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Z-Moves and Ultra Burst (plan 06 · R21), each by Pokémon Showdown's rule at the pinned commit (sim/battle-actions.ts'
/// getZMove, getActiveZMove, canZMove, runZPower and modifyDamage's zBrokeProtect, sim/side.ts' once a battle,
/// data/items.ts' crystals, data/moves.ts' signature Z-Moves). One rule to a test, under the modern rules and on the
/// rules alone, both sides answered for from outside unless the trainer AI is what is tested; the rolls fixed by kind
/// (<see cref="CoreScenario.Calm"/>: no critical hit, the strongest roll). The Pokémon have no IVs, EVs or nature
/// (<see cref="Scenario.Mon"/>): a stat at level 50 is base + 5, and HP base + 60.
/// </summary>
public class ZMoveTests
{
    private static readonly Ruleset Modern = Ruleset.Modern;

    /// <summary>A wild battle with the player holding a Z-Ring (or not), under the rules given.</summary>
    private static BattleCore Wild(Pokemon[] mine, Pokemon foe, bool zRing = true, Ruleset? rules = null)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = rules ?? Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", ZRing = zRing
        });
        core.Start();
        return core;
    }

    /// <summary>A battle against a trainer with a Z-Ring of their own; the player's side has one too.</summary>
    private static BattleCore Against(Pokemon[] mine, Pokemon[] theirs, IBattleController? foeMind = null, Ruleset? rules = null,
        BattleFormat format = BattleFormat.Single)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, ZRing = true, DoubleBattle = format == BattleFormat.Double };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = Calm(), Rules = rules ?? Modern, Format = format,
            PlayerController = null, EnemyController = foeMind, PlayerName = "Lucas", ZRing = true
        });
        core.Start();
        return core;
    }

    private static Pokemon Holding(Pokemon p, string item)
    {
        p.HeldItem = ItemDatabase.Get(item) ?? throw new ArgumentException(item);
        return p;
    }

    /// <summary>A Bidoof at 50 (Attack 45 + 5 = 50, a Normal type) holding Normalium Z.</summary>
    private static Pokemon ZBidoof(params string[] moves) => Holding(Mon("Bidoof", 50, moves), "Normalium Z");

    private static List<string> ZTurn(BattleCore core, int mine = 0, int foe = 0)
    {
        core.Submit(BattleChoice.Fight(Mine, mine) with { ZMove = true }, BattleChoice.Fight(Foe, foe));
        return Lines(core.TakeLog());
    }

    private static Party PartyOf(Pokemon p)
    {
        var party = new Party();
        party.Add(p);
        return party;
    }

    [Fact]
    public void ADamagingMoveBecomesItsTypesZMoveOfItsPowerAsAZMove()
    {
        // Tackle's power as a Z-Move is 100 (Breakneck Blitz, physical, of Tackle's category). Bidoof's Attack 50
        // against Snorlax's Defense 65 + 5 = 70: floor(floor(22 × 100 × 50 / 70) / 50) + 2 = floor(1571 / 50) + 2 = 33;
        // the strongest roll keeps 33, the same-type bonus makes 33 × 6144 / 4096 = 49.5, rounded half down 49.
        // Snorlax's HP is 160 + 60 = 220
        var foe = Mon("Snorlax", 50, "Splash");
        var core = Wild(new[] { ZBidoof("Tackle") }, foe);
        var said = ZTurn(core);
        InOrder(said, "Bidoof surrounds itself with its Z-Power!", "Bidoof unleashes its full-force Z-Move!", "Bidoof used Breakneck Blitz!");
        Assert.Equal(220 - 49, foe.CurrentHP);
    }

    [Fact]
    public void TheLineOfTheZPowerCarriesTheEvent()
    {
        var core = Wild(new[] { ZBidoof("Tackle") }, Mon("Snorlax", 50, "Splash"));
        core.Submit(BattleChoice.Fight(Mine, 0) with { ZMove = true }, BattleChoice.Fight(Foe, 0));
        var line = core.TakeLog().OfType<Said>().Single(s => s.Text.Contains("Z-Power"));
        Assert.Contains(new ZPowerUsed(Mine, "Breakneck Blitz", "Tackle"), line.Shows);
    }

    [Fact]
    public void AZMoveThroughProtectDoesAQuarterAndSaysSo()
    {
        // The 49 of the hit above, then modifyDamage's last step: 49 × 1024 / 4096 = 12.25, rounded half down 12
        var foe = Mon("Snorlax", 50, "Protect");
        var core = Wild(new[] { ZBidoof("Tackle") }, foe);
        var said = ZTurn(core);
        InOrder(said, "Foe Snorlax protected itself!", "Bidoof used Breakneck Blitz!", "Foe Snorlax couldn't fully protect itself and got hurt!");
        Assert.Equal(220 - 12, foe.CurrentHP);
    }

    [Fact]
    public void AZMoveKeepsThePriorityOfTheMoveItIsMadeOf()
    {
        // Bidoof (Speed 31 + 5 = 36) is slower than Jolteon (135); a Z Quick Attack keeps its +1
        var core = Wild(new[] { ZBidoof("Quick Attack", "Tackle") }, Mon("Jolteon", 50, "Splash"));
        InOrder(Turn(core, mine: 1), "Foe Jolteon used Splash!", "Bidoof used Tackle!");
        InOrder(ZTurn(core), "Bidoof used Breakneck Blitz!", "Foe Jolteon used Splash!");
    }

    [Fact]
    public void AStatusMoveIsUsedAsItsZPowerVersionWithItsBonusFirst()
    {
        // Splash's Z-Power: three stages of Attack, after "used Z-Splash!" and before the move's own nothing (a Rattata:
        // Bidoof's Simple would double them)
        var mine = Holding(Mon("Rattata", 50, "Splash"), "Normalium Z");
        var core = Wild(new[] { mine }, Mon("Snorlax", 50, "Splash"));
        var said = ZTurn(core);
        InOrder(said, "Rattata surrounds itself with its Z-Power!", "Rattata used Z-Splash!", "But nothing happened!");
        Assert.DoesNotContain("Rattata unleashes its full-force Z-Move!", said);
        Assert.Equal(3, mine.StatStages.GetValueOrDefault(StatType.Attack));
    }

    [Fact]
    public void AZMovePaysTheMovesOwnPp()
    {
        var mine = ZBidoof("Tackle");
        int pp = mine.Moves[0].CurrentPP;
        ZTurn(Wild(new[] { mine }, Mon("Snorlax", 50, "Splash")));
        Assert.Equal(pp - 1, mine.Moves[0].CurrentPP);
    }

    [Fact]
    public void ATrainerUsesAZMoveOnlyOnceInABattle()
    {
        var core = Wild(new[] { ZBidoof("Tackle") }, Mon("Snorlax", 50, "Splash"));
        ZTurn(core);
        Assert.Equal("You can use a Z-Move only once in a battle!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { ZMove = true }));
        Assert.Throws<ArgumentException>(() => ZTurn(core));
    }

    [Fact]
    public void TwoZMovesOfOneTrainersInATurnAreRefused()
    {
        var core = Against(new[] { ZBidoof("Tackle"), ZBidoof("Tackle") },
            new[] { Mon("Magikarp", 50, "Splash"), Mon("Magikarp", 50, "Splash") }, format: BattleFormat.Double);
        Assert.Throws<ArgumentException>(() => core.Submit(BattleChoice.Fight(Mine, 0) with { ZMove = true }, BattleChoice.Fight(Mine2, 0) with { ZMove = true },
            BattleChoice.Fight(Foe, 0), BattleChoice.Fight(Foe2, 0)));
    }

    [Fact]
    public void WithoutAZRingNobodyUsesAZMove()
    {
        var core = Wild(new[] { ZBidoof("Tackle") }, Mon("Snorlax", 50, "Splash"), zRing: false);
        Assert.Equal("Bidoof can't use a Z-Move!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { ZMove = true }));
    }

    [Fact]
    public void PlatinumsRulesHaveNoZMoves()
    {
        var core = Wild(new[] { ZBidoof("Tackle") }, Mon("Snorlax", 50, "Splash"), rules: Ruleset.Platinum);
        Assert.NotNull(core.WhyNot(BattleChoice.Fight(Mine, 0) with { ZMove = true }));
        Assert.False(core.CanUseZMove(core.At(Mine), 0));
    }

    [Fact]
    public void ACrystalOfAnotherTypeMakesNoZMove()
    {
        // Normalium Z and Ember: no Z-Move of a Fire move
        var core = Wild(new[] { Holding(Mon("Bidoof", 50, "Ember", "Tackle"), "Normalium Z") }, Mon("Snorlax", 50, "Splash"));
        Assert.False(core.CanUseZMove(core.At(Mine), 0));
        Assert.True(core.CanUseZMove(core.At(Mine), 1));
    }

    [Fact]
    public void ASpeciesCrystalTurnsItsOwnMoveIntoItsOwnZMoveInItsOwnHandsAlone()
    {
        // Pikanium Z and Volt Tackle make Catastropika, of its own power (210)
        var pikachu = Holding(Mon("Pikachu", 50, "Volt Tackle"), "Pikanium Z");
        var core = Wild(new[] { pikachu }, Mon("Snorlax", 50, "Splash"));
        Assert.Equal("Catastropika", BattleCore.ZMoveOf(pikachu, pikachu.Moves[0])?.Name);
        InOrder(ZTurn(core), "Pikachu unleashes its full-force Z-Move!", "Pikachu used Catastropika!");
        var raichu = Holding(Mon("Raichu", 50, "Volt Tackle"), "Pikanium Z");
        Assert.Null(BattleCore.ZMoveOf(raichu, raichu.Moves[0]));
    }

    [Fact]
    public void GuardianOfAlolaTakesThreeQuartersOfTheTargetsHp()
    {
        // Snorlax's 220: 220 × 3 / 4 = 165
        var foe = Mon("Snorlax", 50, "Splash");
        var core = Wild(new[] { Holding(Mon("Tapu Koko", 50, "Nature's Madness"), "Tapunium Z") }, foe);
        InOrder(ZTurn(core), "Tapu Koko used Guardian of Alola!");
        Assert.Equal(220 - 165, foe.CurrentHP);
    }

    [Fact]
    public void ClangorousSoulblazeRaisesEveryStatOfItsUserOnceItHits()
    {
        var kommo = Holding(Mon("Kommo-o", 50, "Clanging Scales"), "Kommonium Z");
        var core = Wild(new[] { kommo }, Mon("Snorlax", 50, "Splash"));
        InOrder(ZTurn(core), "Kommo-o used Clangorous Soulblaze!");
        foreach (var stat in new[] { StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed })
            Assert.Equal(1, kommo.StatStages.GetValueOrDefault(stat));
    }

    [Fact]
    public void AZCrystalCantBeKnockedOff()
    {
        var mine = ZBidoof("Splash");
        var core = Against(new[] { mine }, new[] { Mon("Sneasel", 50, "Knock Off") });
        Turn(core);
        Assert.Equal("Normalium Z", mine.HeldItem?.Name);
    }

    [Fact]
    public void ANecrozmaFusedWithSolgaleoUltraBurstsAndGoesBackAsTheBattleEnds()
    {
        var necrozma = Holding(Mon("Necrozma", 50, "Tackle"), BattleCore.UltranecroziumZ);
        necrozma.ChangeForm("Necrozma-Dusk");
        var core = Wild(new[] { necrozma }, Mon("Magikarp", 5, "Splash"));
        Assert.True(core.CanUltraBurst(core.At(Mine)));
        // Ultranecrozium Z's Z-Move is Necrozma-Ultra's alone (its itemUser): none before the burst
        Assert.False(core.CanUseZMove(core.At(Mine), 0));
        core.Submit(BattleChoice.Fight(Mine, 0) with { UltraBurst = true }, BattleChoice.Fight(Foe, 0));
        var log = core.TakeLog();
        InOrder(Lines(log), "Bright light is about to burst out of Necrozma!", "Necrozma regained its true power through Ultra Burst!", "Necrozma used Tackle!");
        Assert.Contains(log.OfType<Said>().SelectMany(s => s.Shows), e => e is UltraBursted { Form: "Necrozma-Ultra" });
        Assert.Equal(BattleResult.PlayerVictory, core.Result);
        Assert.Equal("Necrozma-Dusk", necrozma.Form);
    }

    [Fact]
    public void ANecrozmaThatHasNotFusedCantUltraBurst()
    {
        var core = Wild(new[] { Holding(Mon("Necrozma", 50, "Tackle"), BattleCore.UltranecroziumZ) }, Mon("Magikarp", 50, "Splash"));
        Assert.Equal("Necrozma can't Ultra Burst!", core.WhyNot(BattleChoice.Fight(Mine, 0) with { UltraBurst = true }));
    }

    [Fact]
    public void TheTrainerAiUsesAZMoveOnAMoveAtLeastNeutral()
    {
        var foe = ZBidoof("Tackle");
        var mine = Mon("Snorlax", 50, "Splash");
        var core = Against(new[] { mine }, new[] { foe }, TrainerAi.Instance);
        var said = Turn(core);
        Assert.Contains("Foe Bidoof used Breakneck Blitz!", said);
    }

    [Fact]
    public void TheTrainerAiNeverUsesAZMoveUnderPlatinumsRules()
    {
        var core = Against(new[] { Mon("Snorlax", 50, "Splash") }, new[] { ZBidoof("Tackle") }, TrainerAi.Instance, Ruleset.Platinum);
        var said = Turn(core);
        Assert.DoesNotContain(said, l => l.Contains("Z-Power"));
    }

    [Fact]
    public void ABattleWithAZMoveReplaysTheSame()
    {
        BattleCore Make() => new(new CoreSetup
        {
            PlayerParty = PartyOf(ZBidoof("Tackle")),
            WildPokemon = new List<Pokemon> { Mon("Snorlax", 50, "Tackle") }, Random = new BattleRandom(7), Rules = Modern,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", ZRing = true
        });
        var first = Make();
        first.Start();
        first.Submit(BattleChoice.Fight(Mine, 0) with { ZMove = true }, BattleChoice.Fight(Foe, 0));
        first.Submit(BattleChoice.Fight(Mine, 0), BattleChoice.Fight(Foe, 0));
        var again = Make();
        again.Replay(first.Record!);
        Assert.Equal(Lines(first.Log), Lines(again.Log));
        Assert.Contains("Bidoof used Breakneck Blitz!", Lines(again.Log));
    }

    [Fact]
    public void TheMoveMenusItemButtonArmsTheZPowerForTheMove()
    {
        // A Z-Ring in the bag is the player's; the item button in the move menu arms the Z-Power, the move takes it along
        var bag = new Inventory();
        bag.AddItem(ItemDatabase.Get(BattleCore.ZRingItem)!);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = PartyOf(ZBidoof("Tackle")), Inventory = bag, Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon("Snorlax", 50, "Splash") }, Random = Calm(), Rules = Modern
        });
        Settle(battle);
        battle.SelectMainMenuOption(0);
        Assert.False(battle.CanMegaEvolveNow);
        Assert.True(battle.CanZMoveNow);
        Assert.Equal("Breakneck Blitz", battle.ZMoveAt(0)?.Name);
        Assert.True(battle.ToggleLaterMechanic());
        Assert.True(battle.ZArmed);
        battle.SelectMove(0);
        var said = Settle(battle);
        Assert.Contains("Bidoof used Breakneck Blitz!", said);
        Assert.False(battle.ZArmed);
    }
}
