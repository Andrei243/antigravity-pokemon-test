using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Ai;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V2: Challenge and Easy. What a difficulty does to the trainers (on a copy), to a battle's EXP and to a
/// shop's prices; and that Normal does nothing, down to returning the same object.
/// </summary>
public class DifficultyRulesTests
{
    private static readonly RunRules Easy = new() { Difficulty = Difficulty.Easy };
    private static readonly RunRules Challenge = new() { Difficulty = Difficulty.Challenge };

    /// <summary>A trainer of the data as the world builds them: filled from its record.</summary>
    private static Trainer Made(string id)
    {
        var record = TrainerDatabase.Get(id) ?? throw new System.ArgumentException(id);
        var trainer = new Trainer { Id = record.Id };
        TrainerDatabase.Fill(trainer, record);
        return trainer;
    }

    private static string[] Levels(Trainer t) => t.Party.Members.Select(p => $"{p.Species.Name} {p.Level}").ToArray();

    [Fact]
    public void NormalHandsBackTheVeryTrainer()
    {
        var roark = Made("leader_roark");
        Assert.Same(roark, DifficultyRules.Prepare(roark, RunRules.Normal));
        Assert.Same(roark, DifficultyRules.Prepare(roark, null));
        // Nothing else being on changes no trainer either (the nuzlocke preset is Normal by difficulty)
        Assert.Same(roark, DifficultyRules.Prepare(roark, RunRules.NuzlockePreset));
    }

    [Fact]
    public void TheTableIsByClass()
    {
        foreach (string chief in new[] { "Leader", "Elite Four", "Champion" })
            Assert.Equal(DifficultyRules.Tier.Chief, DifficultyRules.TierOf(chief));
        foreach (string boss in new[] { "Rival", "Commander", "Galactic Boss", "Rival Early", "Rival Late" })
            Assert.Equal(DifficultyRules.Tier.Boss, DifficultyRules.TierOf(boss));
        foreach (string other in new[] { "Youngster", "Galactic", "Ace Trainer", "Trainer" })
            Assert.Equal(DifficultyRules.Tier.Everyone, DifficultyRules.TierOf(other));
        Assert.Equal(new[] { 4, 3, 1 }, new[] { DifficultyRules.Tier.Chief, DifficultyRules.Tier.Boss, DifficultyRules.Tier.Everyone }.Select(DifficultyRules.LevelsOf));
    }

    [Fact]
    public void RoarkUnderChallengeIsRaisedHasAFourthPokemonAndHoldsItems()
    {
        var roark = Made("leader_roark");
        var hard = DifficultyRules.Prepare(roark, Challenge);

        Assert.NotSame(roark, hard);
        // Platinum's levels 12, 12 and 14, four more each; Aerodactyl (the first of the rematch team whose line isn't his)
        // joins at the best one's level, before the last, which stays the one that finishes
        Assert.Equal(new[] { "Geodude 16", "Onix 16", "Aerodactyl 18", "Cranidos 18" }, Levels(hard));
        Assert.Equal("Sitrus Berry", hard.Party.Members[3].HeldItem?.Name);
        Assert.Equal("Leftovers", hard.Party.Members[2].HeldItem?.Name);
        Assert.All(hard.Party.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
        // Every routine of the AI, and prize money half as much again
        Assert.Equal(DifficultyRules.EveryRoutine, hard.Ai & DifficultyRules.EveryRoutine);
        Assert.Equal(1680 * 3 / 2, hard.PrizeMoney);
        Assert.Equal(roark.Name, hard.Name);
        Assert.Equal(roark.TrainerClass, hard.TrainerClass);

        // The trainer of the map is as the data made it
        Assert.Equal(new[] { "Geodude 12", "Onix 12", "Cranidos 14" }, Levels(roark));
        Assert.All(roark.Party.Members, p => Assert.Null(p.HeldItem));
        Assert.Equal(1680, roark.PrizeMoney);
        Assert.Equal(AiFlags.Basic | AiFlags.EvalAttack | AiFlags.Expert, roark.Ai);
    }

    [Fact]
    public void AMemberTheLevelCannotHaveReachedIsTheStageBefore()
    {
        Assert.Equal("Geodude", DifficultyRules.StageAt("Golem", 14));
        Assert.Equal("Graveler", DifficultyRules.StageAt("Golem", 29));
        Assert.Equal("Golem", DifficultyRules.StageAt("Golem", 36));
        Assert.Equal("Aerodactyl", DifficultyRules.StageAt("Aerodactyl", 5));
        Assert.Equal("Geodude", DifficultyRules.RootOf("Golem"));
        Assert.Equal("Pikachu", DifficultyRules.StageAt("Raichu", 20));
    }

    [Fact]
    public void ARosterOfSixIsNotAddedTo()
    {
        var cynthia = Made("champion_cynthia");
        Assert.Equal(6, cynthia.Party.Count);
        var hard = DifficultyRules.Prepare(cynthia, Challenge);
        Assert.Equal(6, hard.Party.Count);
        Assert.Equal(cynthia.Party.Members.Select(p => p.Level + 4), hard.Party.Members.Select(p => p.Level));
    }

    [Fact]
    public void EveryoneElseIsRaisedALittleAndGainsNoMemberAndNoItem()
    {
        var tristan = Made("youngster_tristan");
        var hard = DifficultyRules.Prepare(tristan, Challenge);
        Assert.Equal(tristan.Party.Members.Select(p => p.Level + 1), hard.Party.Members.Select(p => p.Level));
        Assert.Equal(tristan.Party.Count, hard.Party.Count);
        Assert.All(hard.Party.Members, p => Assert.Null(p.HeldItem));
        Assert.Equal(DifficultyRules.EveryRoutine, hard.Ai & DifficultyRules.EveryRoutine);
        Assert.Equal(80 * 3 / 2, hard.PrizeMoney);
    }

    [Fact]
    public void TheRivalIsRaisedByTheMiddleStepAndHoldsABerry()
    {
        var rival = Made("rival_route_201_turtwig");
        var hard = DifficultyRules.Prepare(rival, Challenge);
        Assert.Equal(rival.Party.Members[0].Level + 3, hard.Party.Members[0].Level);
        Assert.Equal(rival.Party.Count, hard.Party.Count);
        Assert.Equal("Sitrus Berry", hard.Party.Members[^1].HeldItem?.Name);
    }

    [Fact]
    public void EasyLowersTheLevelsByTheSameTableAndChangesNothingElse()
    {
        var roark = Made("leader_roark");
        var soft = DifficultyRules.Prepare(roark, Easy);
        Assert.NotSame(roark, soft);
        Assert.Equal(new[] { "Geodude 8", "Onix 8", "Cranidos 10" }, Levels(soft));
        Assert.All(soft.Party.Members, p => Assert.Null(p.HeldItem));
        Assert.Equal(roark.Ai, soft.Ai);
        Assert.Equal(roark.PrizeMoney, soft.PrizeMoney);

        // A level 3 Pokémon is not taken to nothing
        var lass = new Trainer { Id = "x", TrainerClass = "Rival" };
        lass.Party.Add(Mon("Starly", 3));
        Assert.Equal(2, DifficultyRules.Prepare(lass, Easy).Party.Members[0].Level);
        var egg = new Trainer { Id = "y", TrainerClass = "Youngster" };
        egg.Party.Add(Mon("Starly", 1));
        Assert.Equal(1, DifficultyRules.Prepare(egg, Easy).Party.Members[0].Level);
    }

    [Fact]
    public void ALevelNeverPassesTheRulesCap()
    {
        var t = new Trainer { Id = "z", TrainerClass = "Champion" };
        t.Party.Add(Mon("Garchomp", 100));
        Assert.Equal(100, DifficultyRules.Prepare(t, Challenge).Party.Members[0].Level);
    }

    [Fact]
    public void ExpAndPricesAreEasysAlone()
    {
        Assert.Equal(150, DifficultyRules.ScaleExp(100, Difficulty.Easy));
        Assert.Equal(1, DifficultyRules.ScaleExp(1, Difficulty.Easy));
        Assert.Equal(100, DifficultyRules.ScaleExp(100, Difficulty.Normal));
        Assert.Equal(100, DifficultyRules.ScaleExp(100, Difficulty.Challenge));
        Assert.Equal(150, DifficultyRules.ShopPrice(200, Difficulty.Easy));
        Assert.Equal(200, DifficultyRules.ShopPrice(200, Difficulty.Normal));
        Assert.Equal(200, DifficultyRules.ShopPrice(200, Difficulty.Challenge));
        Assert.Equal(0, DifficultyRules.ShopPrice(0, Difficulty.Easy));
        Assert.Equal(1, DifficultyRules.ShopPrice(1, Difficulty.Easy));
    }

    private static int ExpOf(Difficulty difficulty)
    {
        var mine = Mon("Turtwig", 30, "Tackle");
        var foe = Mon("Starly", 4, "Tackle");
        foe.CurrentHP = 1;
        var party = new Party();
        party.Add(mine);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = Calm(), Rules = Ruleset.Platinum,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas", Difficulty = difficulty
        });
        core.Start();
        core.TakeLog();
        Turn(core);
        return core.Log.OfType<ExpGained>().Single().Amount;
    }

    [Fact]
    public void ABattleGivesThreeHalvesOfTheExpUnderEasyAndTheSameOtherwise()
    {
        int normal = ExpOf(Difficulty.Normal);
        Assert.True(normal > 1);
        Assert.Equal(normal, ExpOf(Difficulty.Challenge));
        Assert.Equal(normal * 3 / 2, ExpOf(Difficulty.Easy));
    }

    private static Party Alone(Pokemon p)
    {
        var party = new Party();
        party.Add(p);
        return party;
    }

    [Fact]
    public void ARecordOfAnEasyBattleReplaysAndOneOfAnotherDifficultyIsRefused()
    {
        var record = new BattleRecord(1, new List<IReadOnlyList<BattleChoice>>(), false, false, Difficulty.Easy);
        var normal = new BattleCore(new CoreSetup
        {
            PlayerParty = Alone(Mon("Turtwig", 5, "Tackle")), WildPokemon = new List<Pokemon> { Mon("Starly", 3) }, Random = new BattleRandom(1), Rules = Ruleset.Platinum,
            PlayerController = null, EnemyController = null
        });
        Assert.Throws<System.ArgumentException>(() => normal.Replay(record));
    }

    [Fact]
    public void AShopTakesAQuarterOffUnderEasyAndChargesWhatItAsksOtherwise()
    {
        var shop = new ShopScreen();
        shop.Open("Sandgem Poké Mart", start: ShopMode.Buying);
        var potion = shop.Stock.First(i => i.Name == "Potion");
        Assert.Equal(potion.Price, shop.PriceOf(potion));

        shop.Difficulty = Difficulty.Easy;
        int each = potion.Price * 3 / 4;
        Assert.Equal(each, shop.PriceOf(potion));
        shop.SelectedIndex = shop.Stock.ToList().IndexOf(potion);
        var inventory = new Inventory();
        int money = each * 2 + 1;
        Assert.Equal(2, ShopScreen.MostAffordable(potion, money, Difficulty.Easy));
        Assert.Equal(0, shop.Confirm(inventory, money, _ => { }));
        shop.Move(1, 0, money);
        Assert.Equal(-each * 2, shop.Confirm(inventory, money, _ => { }));
        Assert.Equal(2, inventory.GetQuantity(potion));
    }
}
