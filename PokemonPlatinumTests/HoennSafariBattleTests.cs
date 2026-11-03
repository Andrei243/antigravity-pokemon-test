using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Hoenn's Safari Zone plays Emerald's Safari battle (plan 21 · H11, part e; rulings, "Emerald's Safari battle"): the
/// catch and escape factors as the battle begins, GO NEAR's steps, a Pokéblock thrown and how the Pokémon's nature takes
/// it (with Emerald's own bug kept), the run decided by the factor the turn began with, a Safari Ball's catch rate from
/// the catch factor, the choices each kind of Safari battle refuses, and the battle's face: its commands and the case's
/// list. The numbers are pret/pokeemerald's (src/battle_util.c, src/battle_main.c, src/battle_ai_script_commands.c).
/// </summary>
public class HoennSafariBattleTests
{
    private static Pokemon Met(string species, Nature nature)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, 25, Gender.Male, nature, false);
        return p;
    }

    private static BattleCore Zone(Pokemon wild, BattleRandom? rolls = null, bool emerald = true)
    {
        var party = new Party();
        party.Add(Scenario.Mon("Bidoof", 30, "Tackle"));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { wild }, Kind = BattleKind.Safari, SpecialBalls = 30,
            EmeraldSafari = emerald, Random = rolls ?? Calm().Force(RollKind.Safari, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99),
            Rules = Ruleset.Modern, PlayerController = null, PlayerName = "May"
        });
        core.Start();
        return core;
    }

    private static List<string> Lines(BattleCore core) => core.TakeLog().OfType<Said>().Select(s => s.Text).ToList();

    // A Pokéblock of one flavour: Adamant likes spicy and dislikes dry; Hardy has no taste
    private static readonly Pokeblock Spicy = new(PokeblockColor.Red, 20, 0, 0, 0, 0, 10);
    private static readonly Pokeblock DryBlock = new(PokeblockColor.Blue, 0, 20, 0, 0, 0, 10);

    [Fact]
    public void TheFactorsBeginFromTheCatchRateAndThree()
    {
        // battle_main.c: safariCatchFactor = catchRate * 100 / 1275; safariEscapeFactor = 3
        var caterpie = Zone(Met("Caterpie", Nature.Hardy));
        Assert.Equal(255 * 100 / 1275, caterpie.SafariCatchFactor);
        Assert.Equal(20, caterpie.SafariCatchFactor);
        Assert.Equal(3, caterpie.SafariEscapeFactor);
        Assert.True(caterpie.EmeraldSafari);

        var wild = Met("Pikachu", Nature.Hardy);
        var pikachu = Zone(wild);
        Assert.Equal(wild.CatchRate * 100 / 1275, pikachu.SafariCatchFactor);
        // The Safari Ball's catch rate is the factor taken back: 190 → 14 → 178
        Assert.Equal(pikachu.SafariCatchFactor * 1275 / 100, pikachu.EmeraldSafariRate);
    }

    [Fact]
    public void GoNearRaisesBothFactorsByItsTableUntilThereIsNoGettingCloser()
    {
        var core = Zone(Met("Pikachu", Nature.Hardy));
        int catchFactor = core.SafariCatchFactor;
        int[] catchSteps = { 4, 3, 2, 1, 1 };
        for (int i = 0; i < catchSteps.Length; i++)
        {
            core.Submit(BattleChoice.GoNear(Mine));
            var lines = Lines(core);
            catchFactor = System.Math.Min(20, catchFactor + catchSteps[i]);
            Assert.Equal(catchFactor, core.SafariCatchFactor);
            Assert.Equal(System.Math.Min(20, 3 + 4 * (i + 1)), core.SafariEscapeFactor);
            Assert.Contains(lines, l => l.Contains(i < 3 ? "edged a little closer" : "can't get any closer"));
        }
        Assert.Equal(3, core.SafariGoNearCount);
    }

    [Fact]
    public void APokeblockLowersTheEscapeFactorByHowTheNatureTakesIt()
    {
        // Enthralled at the first throw: 3 is under the step of 5, so the factor drops to 1
        var liked = Zone(Met("Pikachu", Nature.Adamant));
        liked.Submit(BattleChoice.Pokeblock(Mine, Spicy));
        Assert.Contains(Lines(liked), l => l.Contains("can't take its eyes off"));
        Assert.Equal(1, liked.SafariEscapeFactor);
        Assert.Equal(1, liked.PokeblocksThrown);

        // Ignored: nothing comes off
        var disliked = Zone(Met("Pikachu", Nature.Adamant));
        disliked.Submit(BattleChoice.Pokeblock(Mine, DryBlock));
        Assert.Contains(Lines(disliked), l => l.Contains("turned its nose up"));
        Assert.Equal(3, disliked.SafariEscapeFactor);

        // Curious after two steps closer (escape 11): the steps by the count are 3, then 2, then 1, then 1 again
        var curious = Zone(Met("Pikachu", Nature.Hardy));
        curious.Submit(BattleChoice.GoNear(Mine));
        curious.Submit(BattleChoice.GoNear(Mine));
        Assert.Equal(11, curious.SafariEscapeFactor);
        int[] after = { 8, 6, 5, 4 };
        foreach (int expected in after)
        {
            curious.Submit(BattleChoice.Pokeblock(Mine, Spicy));
            Assert.Contains(Lines(curious), l => l.Contains("is sniffing at"));
            Assert.Equal(expected, curious.SafariEscapeFactor);
        }
        Assert.Equal(3, curious.SafariPokeblockCount);
        Assert.Equal(4, curious.PokeblocksThrown);
    }

    [Fact]
    public void EmeraldsPokeblockThrowBugIsKeptAFactorEqualToTheStepFallsToNothing()
    {
        // HandleAction_ThrowPokeblock compares with <, not <=: a factor of 3 meeting a step of 3 is left at 0,
        // and a Pokémon whose factor is 0 never runs
        // The first turn begins at 3 (a roll under 15 would run): 99 stays
        var core = Zone(Met("Pikachu", Nature.Hardy), Calm().Force(RollKind.Safari, 99, 0, 0, 0));
        core.Submit(BattleChoice.Pokeblock(Mine, Spicy));
        Assert.Equal(0, core.SafariEscapeFactor);
        Assert.Equal(BattleResult.None, core.Result);
        // The turn after begins at 0: even a roll of 0 is not under 0 × 5
        core.Submit(BattleChoice.Pokeblock(Mine, Spicy));
        Assert.Equal(0, core.SafariEscapeFactor);
        Assert.Equal(BattleResult.None, core.Result);
    }

    [Fact]
    public void ThePokemonRunsOnTheFactorTheTurnBeganWith()
    {
        // Escape 3: a roll under 15 runs; GO NEAR's +4 counts only from the next turn
        var stays = Zone(Met("Pikachu", Nature.Hardy), Calm().Force(RollKind.Safari, 15, 34));
        stays.Submit(BattleChoice.GoNear(Mine));
        Assert.Equal(BattleResult.None, stays.Result);
        Assert.Contains(Lines(stays), l => l.Contains("keeping a close eye"));
        stays.Submit(BattleChoice.GoNear(Mine));
        Assert.Equal(BattleResult.EnemyFled, stays.Result);

        var runs = Zone(Met("Pikachu", Nature.Hardy), Calm().Force(RollKind.Safari, 14));
        runs.Submit(BattleChoice.GoNear(Mine));
        Assert.Equal(BattleResult.EnemyFled, runs.Result);
        Assert.Contains(Lines(runs), l => l.Contains("fled"));
    }

    [Fact]
    public void ASafariBallTakesItsRateFromTheCatchFactor()
    {
        var wild = Met("Pikachu", Nature.Hardy);
        var ball = ItemDatabase.Get("Safari Ball")!;
        // The Great Marsh's stage 6 is the species' own rate; Emerald's factor 14 gives 178, under 190
        int rate = 14 * 1275 / 100;
        Assert.Equal(178, rate);
        // The rate given stands in for the species' own: a Caterpie (255) shakes the same with or without it, and a
        // Pikachu given 255 never shakes less than on its own 190, on the same rolls
        var caterpie = Met("Caterpie", Nature.Hardy);
        for (uint seed = 1; seed <= 20; seed++)
        {
            Assert.Equal(CatchCalculator.Shakes(caterpie, ball, new BattleRandom(seed), 0, new BattleConditions(), 6, Ruleset.Modern),
                CatchCalculator.Shakes(caterpie, ball, new BattleRandom(seed), 0, new BattleConditions(), 6, Ruleset.Modern, null, safariRate: 255));
            Assert.True(CatchCalculator.Shakes(wild, ball, new BattleRandom(seed), 0, new BattleConditions(), 6, Ruleset.Modern, null, safariRate: 255)
                >= CatchCalculator.Shakes(wild, ball, new BattleRandom(seed), 0, new BattleConditions(), 6, Ruleset.Modern));
        }

        var core = Zone(wild);
        for (int i = 0; i < 4; i++) core.Submit(BattleChoice.GoNear(Mine));
        Assert.Equal(20, core.SafariCatchFactor);
        Assert.Equal(255, core.EmeraldSafariRate);
    }

    [Fact]
    public void EachSafariBattleRefusesTheOthersThrows()
    {
        var emerald = Zone(Met("Pikachu", Nature.Hardy));
        Assert.NotNull(emerald.WhyNot(BattleChoice.Bait(Mine)));
        Assert.NotNull(emerald.WhyNot(BattleChoice.Mud(Mine)));
        Assert.Null(emerald.WhyNot(BattleChoice.GoNear(Mine)));
        Assert.Null(emerald.WhyNot(BattleChoice.Pokeblock(Mine, Spicy)));

        var marsh = Zone(Met("Pikachu", Nature.Hardy), emerald: false);
        Assert.False(marsh.EmeraldSafari);
        Assert.Null(marsh.WhyNot(BattleChoice.Bait(Mine)));
        Assert.NotNull(marsh.WhyNot(BattleChoice.GoNear(Mine)));
        Assert.NotNull(marsh.WhyNot(BattleChoice.Pokeblock(Mine, Spicy)));
    }

    [Fact]
    public void TheLastBallSaysTheSafariZonesEndNotTheMarshs()
    {
        var party = new Party();
        party.Add(Scenario.Mon("Bidoof", 30, "Tackle"));
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { Met("Pikachu", Nature.Hardy) }, Kind = BattleKind.Safari, SpecialBalls = 1,
            EmeraldSafari = true, Random = Calm().Force(RollKind.CatchShake, 65535), Rules = Ruleset.Modern, PlayerController = null, PlayerName = "May"
        });
        core.Start();
        core.Submit(BattleChoice.UseItem(Mine, "Safari Ball"));
        var lines = Lines(core);
        Assert.Contains(lines, l => l.Contains("Safari Zone"));
        Assert.DoesNotContain(lines, l => l.Contains("Great Marsh"));
    }

    [Fact]
    public void TheFaceOffersPokeblockAndGoNearAndThrowsFromTheCase()
    {
        var party = new Party();
        party.Add(Scenario.Mon("Bidoof", 30, "Tackle"));
        var pokeblocks = new PokeblockCase();
        pokeblocks.Add(Spicy);
        pokeblocks.Add(DryBlock);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = { Met("Pikachu", Nature.Adamant) },
            Kind = BattleKind.Safari, SpecialBalls = 30, EmeraldSafari = true, Pokeblocks = pokeblocks,
            Random = Calm().Force(RollKind.Safari, 99, 99, 99), Rules = Ruleset.Modern
        });
        Scenario.Settle(battle);
        Assert.True(battle.EmeraldSafari);
        var (_, small) = ModernUi.CommandLabels(battle);
        Assert.Equal(new[] { "POKéBLOCK", "GO NEAR", "RUN" }, small);
        Assert.Equal(2, ModernUi.PokeblockLabels(battle).Length);

        battle.SelectMainMenuOption(1);
        Assert.Equal(BattleMenuState.Pokeblocks, battle.HUD.MenuState);
        battle.SelectPokeblock(0);
        var said = Scenario.Settle(battle);
        Assert.Equal(1, pokeblocks.Count);
        Assert.Equal(PokeblockColor.Blue, pokeblocks.Listed()[0].Block.Color);
        Assert.Contains(said, l => l.Contains("can't take its eyes off"));
        Assert.Equal(1, battle.Core.SafariEscapeFactor);

        battle.SelectMainMenuOption(2);
        Scenario.Settle(battle);
        Assert.Equal(1, battle.Core.SafariGoNearCount);

        // With the case empty POKéBLOCK says so and the commands stay
        pokeblocks.Clear();
        battle.SelectMainMenuOption(1);
        Assert.NotEqual(BattleMenuState.Pokeblocks, battle.HUD.MenuState);
        Assert.Contains(Scenario.Settle(battle), l => l.Contains("no Pokéblocks"));
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
    }

    [Fact]
    public void AnotherBattleOfTheSameSetupKeepsEmeraldsRules()
    {
        var setup = new BattleSetup
        {
            PlayerParty = new Party(), Inventory = new Inventory(), Pokedex = new Pokedex(), Kind = BattleKind.Safari,
            EmeraldSafari = true, Pokeblocks = new PokeblockCase()
        };
        var again = setup.Again(new List<Pokemon>(), new List<Trainer>(), null, null, null);
        Assert.True(again.EmeraldSafari);
        Assert.Same(setup.Pokeblocks, again.Pokeblocks);
    }
}
