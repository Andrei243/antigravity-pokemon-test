using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 35 · BN1, the care book: a battle's soil on those it sent out, the day's meals, a pat and a grooming once a day, a
/// cure once for each battle, the mood and how it settles, a page that follows its Pokémon through a box, a job and a
/// save and goes when the Pokémon leaves, and an older save that reads as no care.
/// </summary>
[Collection("Shared clocks and dice")]
public class CareTests
{
    private static readonly DateTime Monday = new(2026, 11, 9);
    private static int Day(DateTime date) => CareRules.DayOf(date);

    private static Pokemon Owned(string species, int level, int id)
    {
        var p = Mon(species, level);
        p.Id = new Guid(id, 0, 0, new byte[8]);
        return p;
    }

    private static CareRules.BattleOutcome Outcome(IReadOnlyCollection<Pokemon> fought, BattleTerrain terrain, BattleWeather weather = BattleWeather.None,
        bool won = true, bool lost = false, IReadOnlyCollection<Pokemon>? levelled = null) =>
        new(fought, won, lost, levelled ?? Array.Empty<Pokemon>(), terrain, weather);

    // ------------------------------------------------------------------ soil

    [Fact]
    public void ABattleOnSandLeavesThoseItSentOutDustyAndNobodyElse()
    {
        var book = new CareBook();
        var fought = Owned("Turtwig", 10, 1);
        var benched = Owned("Starly", 10, 2);
        CareRules.AfterBattle(book, Outcome(new[] { fought }, BattleTerrain.Sand));
        Assert.Equal(new[] { SoilMark.Dust }, book.Of(fought).Marks);
        Assert.Empty(book.Of(benched).Marks);
    }

    [Fact]
    public void ABattleInABuildingOnABridgeOrAPlainLeavesNothingButTheWeatherDoes()
    {
        foreach (var terrain in new[] { BattleTerrain.Building, BattleTerrain.Bridge, BattleTerrain.Plain, BattleTerrain.Cave, BattleTerrain.Special })
            Assert.Equal(SoilMark.None, CareRules.SoilFor(terrain, BattleWeather.None));
        Assert.Equal(SoilMark.Dust, CareRules.SoilFor(BattleTerrain.Building, BattleWeather.Sandstorm));
        Assert.Equal(SoilMark.Wet, CareRules.SoilFor(BattleTerrain.Plain, BattleWeather.Rain));
        Assert.Equal(SoilMark.Snow, CareRules.SoilFor(BattleTerrain.Grass, BattleWeather.Hail));
        Assert.Equal(SoilMark.Mud, CareRules.SoilFor(BattleTerrain.GreatMarsh, BattleWeather.None));
        Assert.Equal(SoilMark.Leaves, CareRules.SoilFor(BattleTerrain.Grass, BattleWeather.None));
        Assert.Equal(SoilMark.Snow, CareRules.SoilFor(BattleTerrain.Ice, BattleWeather.None));
        Assert.Equal(SoilMark.Wet, CareRules.SoilFor(BattleTerrain.Water, BattleWeather.None));
    }

    [Fact]
    public void ACoatKeepsTwoMarksAtMostTheNewestKept()
    {
        var book = new CareBook();
        var p = Owned("Turtwig", 10, 1);
        foreach (var terrain in new[] { BattleTerrain.Sand, BattleTerrain.Grass, BattleTerrain.GreatMarsh, BattleTerrain.Grass })
            CareRules.AfterBattle(book, Outcome(new[] { p }, terrain));
        Assert.Equal(new[] { SoilMark.Mud, SoilMark.Leaves }, book.Of(p).Marks);
    }

    [Fact]
    public void GroomingTakesAMarkOffAndCountsForFriendshipOnceADay()
    {
        var book = new CareBook();
        var p = Owned("Turtwig", 10, 1);
        p.Friendship = 70;
        var page = book.Of(p);
        CareRules.Soil(page, SoilMark.Dust);
        CareRules.Soil(page, SoilMark.Mud);
        Assert.True(CareRules.Groom(page, p, SoilMark.Dust, Day(Monday)));
        Assert.Equal(70 + FriendshipRules.ChangeFor(FriendshipEvent.Groomed).Low, p.Friendship);
        Assert.True(CareRules.Groom(page, p, SoilMark.Mud, Day(Monday)));
        Assert.Equal(72, p.Friendship);
        Assert.False(CareRules.Groom(page, p, SoilMark.Mud, Day(Monday)));
        Assert.True(CareRules.BrushTakes(SoilMark.Dust) && !CareRules.BrushTakes(SoilMark.Mud) && !CareRules.BrushTakes(SoilMark.Wet));
    }

    // ------------------------------------------------------------------ meals, pats and cures

    [Fact]
    public void APatCountsForFriendshipOnceADayAndAgainTheNext()
    {
        var book = new CareBook();
        var p = Owned("Turtwig", 10, 1);
        p.Friendship = 70;
        var page = book.Of(p);
        Assert.True(CareRules.Pat(page, p, Day(Monday)));
        Assert.Equal(73, p.Friendship);
        Assert.False(CareRules.Pat(page, p, Day(Monday)));
        Assert.Equal(73, p.Friendship);
        Assert.True(page.Mood > 0);
        Assert.True(CareRules.Pat(page, p, Day(Monday.AddDays(1))));
        Assert.Equal(76, p.Friendship);
    }

    [Fact]
    public void FullnessFillsToFiveAndIsEmptyTheNextDay()
    {
        var book = new CareBook();
        var p = Owned("Turtwig", 10, 1);
        var page = book.Of(p);
        var berry = ItemDatabase.Get("Oran Berry")!;
        for (int i = 0; i < CareRules.Fullness; i++) Assert.True(CareRules.FeedBerry(page, p, Day(Monday), berry));
        Assert.Equal(CareRules.Fullness, CareRules.FullnessOn(page, Day(Monday)));
        Assert.False(CareRules.FeedBerry(page, p, Day(Monday), berry));
        Assert.Equal(0, CareRules.FullnessOn(page, Day(Monday.AddDays(1))));
        Assert.True(CareRules.FeedBerry(page, p, Day(Monday.AddDays(1)), berry));
    }

    [Fact]
    public void AMealsMoodFollowsItsTasteByTheNature()
    {
        // Adamant likes spicy and dislikes dry; Cheri is a spicy berry, Chesto a dry one
        Assert.Equal(MealTaste.Liked, CareRules.TasteOf(CareRules.StrongestFlavour(ItemDatabase.Get("Cheri Berry")!), Nature.Adamant));
        Assert.Equal(MealTaste.Disliked, CareRules.TasteOf(CareRules.StrongestFlavour(ItemDatabase.Get("Chesto Berry")!), Nature.Adamant));
        Assert.Equal(MealTaste.Plain, CareRules.TasteOf(CareRules.StrongestFlavour(ItemDatabase.Get("Cheri Berry")!), Nature.Hardy));
    }

    [Fact]
    public void ACureClearsAStatusOnceForEachBattleThePokemonFought()
    {
        var book = new CareBook();
        var p = Owned("Turtwig", 10, 1);
        var page = book.Of(p);
        p.Status = StatusCondition.Poison;
        // A page begun counts as after a battle, as a game loaded did for the camp
        Assert.True(CareRules.Cure(page, p));
        Assert.Equal(StatusCondition.None, p.Status);
        p.Status = StatusCondition.Burn;
        Assert.False(CareRules.Cure(page, p));
        CareRules.AfterBattle(book, Outcome(new[] { p }, BattleTerrain.Plain));
        Assert.True(CareRules.Cure(page, p));
        // A fainted Pokémon is no cure's
        p.Status = StatusCondition.Faint;
        CareRules.AfterBattle(book, Outcome(new[] { p }, BattleTerrain.Plain));
        Assert.False(CareRules.Cure(page, p));
    }

    // ------------------------------------------------------------------ mood

    [Fact]
    public void AFaintedPokemonsMoodFallsAWinnersRisesAndBothSettle()
    {
        var book = new CareBook();
        var winner = Owned("Turtwig", 10, 1);
        var fainted = Owned("Starly", 10, 2);
        fainted.CurrentHP = 0;
        fainted.Status = StatusCondition.Faint;
        CareRules.AfterBattle(book, Outcome(new[] { winner }, BattleTerrain.Plain));
        CareRules.AfterBattle(book, Outcome(new[] { fainted }, BattleTerrain.Plain, won: false, lost: true));
        Assert.Equal(CareRules.MoodWin, book.Of(winner).Mood);
        Assert.Equal(CareRules.MoodLoss + CareRules.MoodFaint, book.Of(fainted).Mood);
        Assert.Equal(CareMood.Low, CareRules.MoodOf(book.Of(fainted).Mood));
        for (int i = 0; i < 20; i++) CareRules.WalkCycle(book, new[] { winner, fainted });
        Assert.Equal(0, book.Of(winner).Mood);
        Assert.Equal(0, book.Of(fainted).Mood);
    }

    [Fact]
    public void ACoatLeftSoiledThroughAWalkLowersTheMoodAndTheBoxesSettleByTheDay()
    {
        var book = new CareBook();
        var team = Owned("Turtwig", 10, 1);
        var boxed = Owned("Starly", 10, 2);
        CareRules.Soil(book.Of(team), SoilMark.Mud);
        CareRules.WalkCycle(book, new[] { team });
        Assert.Equal(CareRules.MoodSoiledWalk, book.Of(team).Mood);
        CareRules.Lift(book.Of(boxed), 35);
        CareRules.DaysPass(book, 2, new[] { team });
        Assert.Equal(35 - 2 * CareRules.SettleDay, book.Of(boxed).Mood);
        Assert.Equal(CareRules.MoodSoiledWalk, book.Of(team).Mood);
    }

    [Fact]
    public void TheMoodsFiveFacesAndItsBounds()
    {
        Assert.Equal(CareMood.Glum, CareRules.MoodOf(-100));
        Assert.Equal(CareMood.Low, CareRules.MoodOf(-30));
        Assert.Equal(CareMood.Calm, CareRules.MoodOf(0));
        Assert.Equal(CareMood.Cheerful, CareRules.MoodOf(30));
        Assert.Equal(CareMood.Elated, CareRules.MoodOf(80));
        var page = new CareRecord();
        CareRules.Lift(page, 500);
        Assert.Equal(CareRules.MoodHighest, page.Mood);
        CareRules.Lift(page, -1000);
        Assert.Equal(CareRules.MoodLowest, page.Mood);
    }

    [Fact]
    public void CareAloneTakesAPokemonFromSeventyToTwoHundredAndTwentyInNoFewerThanSevenDays()
    {
        // The plan's risk: friendship must not climb too fast. Every day: a pat, five berries and a grooming.
        var book = new CareBook();
        var p = Owned("Turtwig", 10, 1);
        p.Friendship = 70;
        var page = book.Of(p);
        var berry = ItemDatabase.Get("Oran Berry")!;
        int days = 0;
        while (p.Friendship < FriendshipRules.EvolveAt)
        {
            int day = Day(Monday.AddDays(days));
            CareRules.Pat(page, p, day);
            while (CareRules.FeedBerry(page, p, day, berry)) { }
            CareRules.Soil(page, SoilMark.Dust);
            CareRules.Groom(page, p, SoilMark.Dust, day);
            days++;
        }
        Assert.True(days >= 7, $"care alone made a close friend in {days} days");
    }

    // ------------------------------------------------------------------ the book

    [Fact]
    public void APageFollowsItsPokemonThroughABoxAJobAndASaveAndGoesWhenItLeaves()
    {
        var book = new CareBook();
        var p = Owned("Turtwig", 10, 1);
        CareRules.Lift(book.Of(p), 30);
        CareRules.Soil(book.Of(p), SoilMark.Leaves);
        // Through a job: held whole, then back, the same id
        var board = new JobBoard();
        var job = board.Post(p, JobKind.Errand, 1, Monday)!;
        book.Prune(board.Away.Select(a => a.Id!.Value));
        var back = job.Pokemon.ToPokemon();
        Assert.Equal(30, book.Of(back).Mood);
        // Through a save
        var saved = book.ToSave();
        var again = new CareBook();
        again.Restore(saved);
        Assert.Equal(new[] { SoilMark.Leaves }, again.Of(back).Marks);
        Assert.Equal(30, again.Of(back).Mood);
        // Gone with its Pokémon
        Assert.Equal(1, again.Prune(Array.Empty<Guid>()));
        Assert.Null(again.ToSave());
    }

    [Fact]
    public void AnOlderSaveReadsAsNoCareAndABlankPageIsNotKept()
    {
        var book = new CareBook();
        book.Restore(null);
        Assert.Equal(0, book.Count);
        book.Of(Owned("Turtwig", 10, 1));
        Assert.Null(book.ToSave());
        Assert.Null(new SaveData().Care);
    }

    [Fact]
    public void APokemonWithNoIdKeepsItsPageUntilItIsGivenOne()
    {
        var book = new CareBook();
        var p = Mon("Turtwig", 10);
        CareRules.Lift(book.Of(p), 12);
        p.Id = new Guid(7, 0, 0, new byte[8]);
        Assert.Equal(12, book.Of(p).Mood);
        Assert.Equal(12, book.Find(p.Id)!.Mood);
    }

    // ------------------------------------------------------------------ in the game

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(1f / 60f);
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, bool press = false)
    {
        for (int f = 0; f < 60 * 120; f++)
        {
            if (done()) return;
            Run(engine, press && f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Fail($"Waited for {what}; the game is in {engine.Drive.State}.");
    }

    [Fact]
    public void AHeadlessBattleInASandstormLeavesTheTeamThatFoughtDustyAndTheBookIsSavedAndReloaded()
    {
        var paths = SavePaths.Current;
        var sinnoh = MapDatabase.Get("Sinnoh");
        try
        {
            Dice.Seed(12);
            GameClock.FixedDate = Monday;
            var engine = Headless();
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Clear();
            var lead = new Pokemon(PokemonDatabase.Get("Turtwig")!, 70);
            var bench = new Pokemon(PokemonDatabase.Get("Starly")!, 10);
            game.Party.Add(lead);
            game.Party.Add(bench);
            game.BuildSave(); // gives the team their ids
            game.State = GameState.Overworld;
            game.Place(sinnoh, 116, 886, Direction.Right);
            game.TrainersLookOnArrival = false;
            sinnoh.WeatherOverride = FieldWeather.Sandstorm;

            game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 2, MaxLevel = 2, Weight = 100 });
            Until(engine, () => game.State == GameState.Battle, "the battle");
            var battle = game.Battle!;
            int hit = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status);
            Until(engine, () => battle.IsBattleOver || battle.HUD.MenuState == BattleMenuState.Main, "the battle's menu", press: true);
            for (int guard = 0; guard < 60 * 120 && !battle.IsBattleOver; guard++)
            {
                if (battle.HUD.MenuState == BattleMenuState.Main) battle.SelectMove(hit);
                Run(engine, guard % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            }
            Until(engine, () => game.State == GameState.Overworld, "the field after the battle", press: true);

            Assert.Equal(new[] { SoilMark.Dust }, game.Care.Of(lead).Marks);
            Assert.Empty(game.Care.Of(bench).Marks);
            Assert.True(game.Care.Of(lead).Mood > 0);

            // The day's meals, then the next day's from empty
            var berry = ItemDatabase.Get("Oran Berry")!;
            Assert.True(CareRules.FeedBerry(game.Care.Of(lead), lead, game.CareDay, berry));
            Assert.Equal(1, CareRules.FullnessOn(game.Care.Of(lead), game.CareDay));
            GameClock.FixedDate = Monday.AddDays(1);
            Assert.Equal(0, CareRules.FullnessOn(game.Care.Of(lead), game.CareDay));

            // A save of it reloads with the same book
            var save = game.BuildSave();
            Assert.NotNull(save.Care);
            var read = SaveManager.Read(SaveManager.Write(save))!;
            var other = Headless();
            other.Drive.ApplySave(read);
            var loadedLead = other.Drive.Party.Members[0];
            Assert.Equal(lead.Id, loadedLead.Id);
            Assert.Equal(new[] { SoilMark.Dust }, other.Drive.Care.Of(loadedLead).Marks);
            Assert.Equal(game.Care.Of(lead).Mood, other.Drive.Care.Of(loadedLead).Mood);
        }
        finally
        {
            sinnoh.WeatherOverride = null;
            GameClock.FixedDate = null;
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
        }
    }

    [Fact]
    public void TheGamesCampIsAViewOverTheBook()
    {
        var paths = SavePaths.Current;
        try
        {
            Dice.Seed(41);
            GameClock.FixedDate = Monday;
            var engine = new GameEngine(new GameSettings { Muted = true, Follower = true });
            engine.InitializeHeadless();
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            var starly = new Pokemon(PokemonDatabase.Get("Starly")!, 12) { Friendship = 70 };
            game.Party.Clear();
            game.Party.Add(starly);
            game.BuildSave();
            game.State = GameState.Overworld;
            starly.Status = StatusCondition.Poison;
            game.ChooseFromStartMenu(StartMenuChoice.Camp);
            Assert.Equal(GameState.Camp, game.State);
            // The cure as the camp opens is the page's
            Assert.Equal(StatusCondition.None, starly.Status);
            Assert.False(game.Care.Of(starly).CureOwed);
            Assert.True(game.Camp.Pet());
            Assert.Equal(73, starly.Friendship);
            // A second visit the same day pets, but the friendship has counted the day's pat
            game.Camp.Cancel();
            game.State = GameState.Overworld;
            game.ChooseFromStartMenu(StartMenuChoice.Camp);
            Assert.True(game.Camp.Pet());
            Assert.Equal(73, starly.Friendship);
            // It eats until it is full, across visits
            var berry = ItemDatabase.Get("Oran Berry")!;
            game.Bag.AddItem(berry, 9);
            for (int i = 0; i < CareRules.Fullness; i++) Assert.True(game.Camp.Feed(berry, game.Bag));
            Assert.False(game.Camp.Feed(berry, game.Bag));
            Assert.Equal(CampRefusal.Full, game.Camp.Visit.WhyNotFeed(starly, berry, game.Bag));
        }
        finally
        {
            GameClock.FixedDate = null;
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
        }
    }
}
