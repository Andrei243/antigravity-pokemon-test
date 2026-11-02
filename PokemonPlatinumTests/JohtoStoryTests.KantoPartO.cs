using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>Plan 20 · J15, part o: what the last of HeartGold's Kanto does.</summary>
public partial class JohtoStoryTests
{
    [Fact]
    public void ARotomWithFourMovesInTheRoomForgetsOneForTheAppliancesMove()
    {
        // HeartGold asks which move is to go for the appliance's: the tutors' question (`tutor learn`)
        var game = new Game();
        var rotom = new Pokemon(PokemonDatabase.Get("Rotom")!, 50);
        while (rotom.Moves.Count < 4) rotom.TryLearn(new[] { "Thunder Shock", "Astonish", "Double Team", "Thunder Wave", "Ominous Wind" }.First(m => !rotom.Knows(m)));
        string first = rotom.Moves[0].Name;
        game.Party.Add(rotom);
        game.Arrive("Saffron_SilphCo_RotomRoom", 6, 8);
        // Yes to the microwave, then the first move forgotten
        game.Answers.Enqueue(0);
        game.Answers.Enqueue(0);
        var host = game.Talk("t11r0702_rotomf");
        Assert.Equal("Rotom-Heat", rotom.Form);
        Assert.True(rotom.Knows("Overheat"));
        Assert.False(rotom.Knows(first));
        Assert.Equal(4, rotom.Moves.Count);
        Assert.Contains(host.Transcript, l => l.Text.Contains("forgot"));

        // Into the fan: Overheat goes for Air Slash with no question; out again, Air Slash goes and three moves are left
        game.Talk("t11r0702_rotoms");
        Assert.True(rotom.Knows("Air Slash"));
        game.Talk("t11r0702_rotoms");
        Assert.Null(rotom.Form);
        Assert.Equal(3, rotom.Moves.Count);

        // Four moves again, and the question given up: the form changes and nothing is learned
        rotom.TryLearn("Thunder Shock");
        Assert.Equal(4, rotom.Moves.Count);
        game.Answers.Enqueue(0);
        game.Answers.Enqueue(4);
        game.Talk("t11r0702_rotomw");
        Assert.Equal("Rotom-Wash", rotom.Form);
        Assert.False(rotom.Knows("Hydro Pump"));
        Assert.Equal(4, rotom.Moves.Count);
    }

    [Fact]
    public void CeladonsGameCornerSellsCoinsAndACoinCaseAndItsPrizeCornerExchangesThem()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));

        game.Arrive("Celadon_GameCorner", 7, 12);
        // A table with no Coin Case: nothing to play with, nothing asked
        var table = game.Read(7, 7);
        Assert.Empty(table.Asked);
        // The clerk hands a Coin Case over and sells 50 coins for $1,000
        game.Answers.Enqueue(0);
        var clerk = game.Talk("t07sp0101_suit");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Coin Case")!));
        Assert.Equal(2000, clerk.Money);
        Assert.Equal(50, clerk.Pastimes.Coins.Coins);
        // With the case the tables ask to be played
        Assert.NotEmpty(game.Read(6, 6).Asked);
        // The Prize Corner's clerk is open
        game.Arrive("Celadon_PrizeCorner", 5, 7);
        Assert.Contains(game.Talk("t07r0501_gsman2").Transcript, l => l.Text.Contains("coins"));
    }

    [Fact]
    public void CeruleanCaveOpensAfterRedAndMewtwoWaitsUntilItHasBeenFought()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 70));
        // The man keeps the mouth until Red is beaten
        game.Arrive("Johto", 1290, 110);
        Assert.NotNull(game.Present("t04_gsman1_2"));
        game.Story.Set("FLAG_JOHTO_BEAT_RED");
        game.Arrive("Johto", 1290, 110);
        Assert.Null(game.Present("t04_gsman1_2"));

        // Mewtwo: run from, it is back on the next visit; fought, it is gone for good
        game.Arrive("Cerulean_Cave_B1F", 56, 26);
        Assert.NotNull(game.Present("d03r0103_tsure_poke_static_mewtwo"));
        game.Fight = BattleOutcome.Fled;
        var fled = game.Talk("d03r0103_tsure_poke_static_mewtwo");
        Assert.Contains(fled.Log, l => l.StartsWith("wildbattle Mewtwo 70"));
        Assert.Null(game.Present("d03r0103_tsure_poke_static_mewtwo"));
        game.Arrive("Cerulean_Cave_B1F", 56, 26);
        Assert.NotNull(game.Present("d03r0103_tsure_poke_static_mewtwo"));
        game.Fight = BattleOutcome.Won;
        game.Talk("d03r0103_tsure_poke_static_mewtwo");
        Assert.True(game.Story.Has("FLAG_JOHTO_FOUGHT_MEWTWO"));
        game.Arrive("Cerulean_Cave_B1F", 56, 26);
        Assert.Null(game.Present("d03r0103_tsure_poke_static_mewtwo"));
    }

    [Fact]
    public void CalBattlesInTheTrainerHouseOnceADay()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Arrive("Viridian_TrainerHouse_B1F", 8, 13);
        var won = game.Talk("t02r0302_gsman3");
        Assert.Contains(won.Log, l => l.StartsWith("battle johto_trainer_house_cal"));
        Assert.True(game.Story.Has("FLAG_DAILY_JOHTO_TRAINER_HOUSE"));
        // Once a day: no second battle until the day turns
        var again = game.Talk("t02r0302_gsman3");
        Assert.DoesNotContain(again.Log, l => l.StartsWith("battle"));
        game.Story.Unset("FLAG_DAILY_JOHTO_TRAINER_HOUSE");
        game.Fight = BattleOutcome.Lost;
        var lost = game.Talk("t02r0302_gsman3");
        Assert.Contains(lost.Log, l => l.StartsWith("battle johto_trainer_house_cal"));
        Assert.Contains(lost.Transcript, l => l.Text.Contains("try me again"));
    }

    [Fact]
    public void PalParksGateInFuchsiaCrossesToSinnohsPalParkWithTheNationalPokedex()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Arrive("Fuchsia_PalParkGate", 9, 6);
        var shut = game.Talk("t08r0201_workman");
        Assert.Empty(shut.Asked);
        Assert.DoesNotContain(shut.Log, l => l.StartsWith("travel"));
        game.Story.Set("JohtoHallOfFame");
        var open = new Game();
        open.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        open.Story.Set("JohtoHallOfFame");
        open.Arrive("Fuchsia_PalParkGate", 9, 6);
        var host = new HeadlessScriptHost(open.Story, open.Party, open.Bag) { Map = open.Map, NationalDex = true };
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("Attendant", "fuchsia_pal_park_entrance")!, open.Present("t08r0201_workman"));
        runner.RunToEnd();
        Assert.Contains(host.Log, l => l.StartsWith("travel to Sinnoh PalParkLobby"));
    }

    [Fact]
    public void TheClefairyDanceOnMondayNightsAndLeaveAMoonStone()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        // Not on a Monday morning
        game.Arrive("MountMoon_Square", 9, 18);
        Assert.Null(game.Present("clefairy_1"));
        // Monday night: six in a ring, and the step below them watches the dance
        game.Time = TimeOfDay.Night;
        game.Arrive("MountMoon_Square", 9, 18);
        Assert.Equal(6, game.Map.NPCs.Count(n => n.Key.StartsWith("clefairy_", StringComparison.Ordinal)));
        Assert.True(game.Fires("Dance"));
        game.Step("Dance");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Moon Stone")!));
        Assert.Null(game.Present("clefairy_1"));
        Assert.True(game.Story.Has("FLAG_DAILY_JOHTO_CLEFAIRY_DANCE"));
        // Once a night
        game.Arrive("MountMoon_Square", 9, 18);
        Assert.Null(game.Present("clefairy_1"));
        Assert.False(game.Fires("Dance"));
        // Not on a Tuesday night
        game.Story.Unset("FLAG_DAILY_JOHTO_CLEFAIRY_DANCE");
        game.Weekday = DayOfWeek.Tuesday;
        game.Arrive("MountMoon_Square", 9, 18);
        Assert.Null(game.Present("clefairy_1"));
    }

    [Fact]
    public void SuicuneStopsInVermilionAndOnRouteFourteenAndIsBattledOnTheCape()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        // Before Cianwood's stop nothing stands in Vermilion
        game.Arrive("Johto", 1325, 335);
        Assert.Null(game.Present("t06_tsure_poke_static_suicune"));
        // Past Cianwood, Vermilion is its next stop
        game.Story.SetVar("VAR_UNK_4076", 2);
        game.Arrive("Johto", 1325, 335);
        Assert.NotNull(game.Present("t06_tsure_poke_static_suicune"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_4070"));
        game.Step("SuicuneByTheHarbour");
        Assert.Equal(2, game.Story.Var("VAR_UNK_4070"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_4086"));
        game.Arrive("Johto", 1325, 335);
        Assert.Null(game.Present("t06_tsure_poke_static_suicune"));

        // Route 14: it looks, and bounds off to the cape
        game.Arrive("Johto", 1360, 416);
        Assert.NotNull(game.Present("r14_tsure_poke_static_suicune"));
        game.Step("SuicuneInTheGrass");
        Assert.Equal(1, game.Story.Var("VAR_UNK_4088"));
        game.Arrive("Johto", 1360, 416);
        Assert.Null(game.Present("r14_tsure_poke_static_suicune"));

        // The cape: Eusine comes over, and Suicune is battled; run from, it is back; fought, gone with Eusine
        game.Story.Set("FLAG_JOHTO_MISTY_BACK_IN_GYM");
        game.Story.SetVar("VAR_JOHTO_MISTY_DATE", 1);
        game.Arrive("Johto", 1410, 45);
        Assert.NotNull(game.Present("r25_tsure_poke_static_suicune"));
        Assert.NotNull(game.Present("r25_minaki"));
        game.Step("EusineOnTheCape");
        Assert.Equal(2, game.Story.Var("VAR_UNK_4088"));
        game.Fight = BattleOutcome.Fled;
        var fled = game.Talk("r25_tsure_poke_static_suicune");
        Assert.Contains(fled.Log, l => l.StartsWith("wildbattle Suicune 40"));
        game.Arrive("Johto", 1410, 45);
        Assert.NotNull(game.Present("r25_tsure_poke_static_suicune"));
        game.Fight = BattleOutcome.Won;
        game.Talk("r25_tsure_poke_static_suicune");
        Assert.True(game.Story.Has("FLAG_JOHTO_FOUGHT_SUICUNE"));
        game.Arrive("Johto", 1410, 45);
        Assert.Null(game.Present("r25_tsure_poke_static_suicune"));
        Assert.Null(game.Present("r25_minaki"));
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, "p01r0307_seaman_2_2", "p01r0307_fishing_2")]
    [InlineData(DayOfWeek.Wednesday, "p01r0307_fishing_2", "p01r0307_seaman_2_4")]
    [InlineData(DayOfWeek.Sunday, "p01r0307_seaman_2_4", "p01r0307_seaman_2_2")]
    public void TheShipsPassengersChangeWithTheDay(DayOfWeek day, string aboard, string ashore)
    {
        var game = new Game { Weekday = day };
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Arrive("SSAqua_1F", 24, 18);
        game.Arrive("SSAqua_B1F", 24, 4);
        Assert.NotNull(game.Present(aboard));
        Assert.Null(game.Present(ashore));
    }
}
