using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V11, New Game+: what a finished save carries into a new journey and what it leaves, the run a step harder,
/// the title's third answer only for a finished save, and the story begun as fresh as any new game's.
/// </summary>
[Collection("Shared clocks and dice")]
public class NewGamePlusTests
{
    private static SaveData Finished(params string[] more) => new()
    {
        PlayerName = "Lucas", PlayTimeSeconds = 90000f, Money = 120000,
        StoryFlags = new List<string> { "SinnohHallOfFame" }.Concat(more).ToList(),
        Party = { SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get("Torterra")!, 62)) }
    };

    // ------------------------------------------------------------------ the rules

    [Fact]
    public void OnlyASaveWithARegionsHallOfFameIsFinished()
    {
        Assert.False(NewGamePlus.IsFinished(null));
        Assert.False(NewGamePlus.IsFinished(new SaveData { StoryFlags = { "FLAG_GAME_COMPLETED_NOT" } }));
        Assert.True(NewGamePlus.IsFinished(Finished()));
        Assert.Equal("Sinnoh", NewGamePlus.FinishedRegion(Finished())!.Id);
    }

    [Theory]
    [InlineData(Difficulty.Easy, Difficulty.Normal)]
    [InlineData(Difficulty.Normal, Difficulty.Challenge)]
    [InlineData(Difficulty.Challenge, Difficulty.Challenge)]
    public void TheDifficultyGoesAStepUpAndTrainersScale(Difficulty was, Difficulty now)
    {
        var raised = NewGamePlus.Raised(RunRules.Normal with { Difficulty = was, Nuzlocke = true });
        Assert.Equal(now, raised.Difficulty);
        Assert.True(raised.ScaleTrainers);
        // The rest of the run is the save's own
        Assert.True(raised.Nuzlocke);
        Assert.Equal(Difficulty.Challenge, NewGamePlus.Raised(null).Difficulty);
    }

    [Fact]
    public void HalfTheMoneyIsCarriedNeverLessThanANewGames()
    {
        Assert.Equal(60000, NewGamePlus.Money(Finished()));
        Assert.Equal(NewGamePlus.StartingMoney, NewGamePlus.Money(new SaveData { Money = 1000 }));
    }

    [Fact]
    public void TheClearsCountOnFromTheSaves()
    {
        Assert.Equal(1, NewGamePlus.Clears(Finished()));
        var again = Finished();
        again.RunState = new RunState { Clears = 2 };
        Assert.Equal(3, NewGamePlus.Clears(again));
    }

    [Fact]
    public void NewGamePlusIsOfferedOnlyWithRoomInTheBoxesForTheTeam()
    {
        var full = Finished();
        full.Boxes = new SavedBoxes();
        for (int b = 0; b < PcBoxes.BoxCount; b++)
        {
            var box = new SavedBox { Name = PcBoxes.DefaultName(b) };
            for (int s = 0; s < PcBoxes.BoxSize; s++) box.Pokemon.Add(new SavedBoxedPokemon { Slot = s, Pokemon = new SavedPokemonData { SpeciesName = "Bidoof", Level = 5 } });
            full.Boxes.Boxes.Add(box);
        }
        Assert.False(NewGamePlus.Offered(full));
        // ... and with the National Pokédex the hundred boxes make room
        full.NationalPokedex = true;
        Assert.True(NewGamePlus.Offered(full));
    }

    [Fact]
    public void TheRunStateKeepsItsClearsAndWritesNoneForAFirstJourney()
    {
        Assert.True(new RunState().IsEmpty);
        Assert.DoesNotContain("Clears", JsonSerializer.Serialize(new RunState { Timer = new RunTimer() }));
        var cleared = new RunState { Clears = 2 };
        Assert.False(cleared.IsEmpty);
        Assert.Equal(2, JsonSerializer.Deserialize<RunState>(JsonSerializer.Serialize(cleared))!.Clears);
    }

    // ------------------------------------------------------------------ the title

    private static TitleScreen AtNewGameQuestion(SaveData save)
    {
        var title = new TitleScreen(save);
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        title.Move(1);
        Assert.Equal(TitleChoice.NewGame, title.Entries[title.SelectedIndex]);
        title.PressConfirm();
        Assert.Equal(TitlePhase.ConfirmNewGame, title.Phase);
        return title;
    }

    [Fact]
    public void AnUnfinishedSaveIsAskedTheTwoAnswersAsEver()
    {
        var title = AtNewGameQuestion(new SaveData { PlayerName = "Lucas", Party = Finished().Party });
        Assert.Null(title.PlusCard);
        title.Move(1);
        Assert.True(title.ConfirmYes);
        title.Move(1);
        Assert.False(title.ConfirmYes);
        Assert.False(title.PlusHighlighted);
    }

    [Fact]
    public void AFinishedSaveIsOfferedNewGamePlusAsTheThirdAnswer()
    {
        var finished = Finished();
        finished.Run = RunRules.Normal with { Difficulty = Difficulty.Easy };
        var title = AtNewGameQuestion(finished);
        Assert.Equal(1, title.PlusCard!.Number);
        Assert.False(title.ConfirmYes || title.PlusHighlighted);
        title.Move(1);
        Assert.True(title.ConfirmYes);
        title.Move(1);
        Assert.True(title.PlusHighlighted);
        Assert.False(title.ConfirmYes);
        // ... round to NO, and back
        title.Move(1);
        Assert.False(title.ConfirmYes || title.PlusHighlighted);
        title.Move(-1);
        Assert.True(title.PlusHighlighted);

        // NEW GAME+ asks nothing more: the run is the save's a step harder, the game goes to a slot of its own
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        for (float t = 0; t < TitleScreen.LeaveTime + 0.05f; t += 1f / 60f) title.Advance(1f / 60f);
        Assert.Equal(TitleChoice.NewGame, title.TakeChoice());
        Assert.Same(finished, title.ChosenPlus);
        Assert.Equal(1, title.PlusSlot);
        Assert.Equal(2, title.Slot);
        Assert.Equal(Difficulty.Normal, title.ChosenRun.Difficulty);
        Assert.True(title.ChosenRun.ScaleTrainers);
    }

    [Fact]
    public void APlainNewGameAfterAFinishedSaveCarriesNothing()
    {
        var title = AtNewGameQuestion(Finished());
        title.Move(1);
        title.PressConfirm();
        Assert.Equal(TitlePhase.ChooseRun, title.Phase);
        title.PressConfirm();
        for (float t = 0; t < TitleScreen.LeaveTime + 0.05f; t += 1f / 60f) title.Advance(1f / 60f);
        Assert.Equal(TitleChoice.NewGame, title.TakeChoice());
        Assert.Null(title.ChosenPlus);
        Assert.True(title.ChosenRun.IsNormal);
    }

    [Fact]
    public void OfSeveralFinishedSavesTheOnePlayedLongestIsOffered()
    {
        var shorter = Finished();
        shorter.PlayTimeSeconds = 1000f;
        var longer = Finished();
        var title = TitleScreen.WithSlots(new[] { new TitleSlot(1, shorter), new TitleSlot(3, longer), new TitleSlot(2, new SaveData { PlayerName = "Dawn" }) });
        Assert.Equal(3, title.PlusCard!.Number);
    }

    // ------------------------------------------------------------------ the carry-over

    [Fact]
    public void AFinishedSaveStartsAgainInSinnohWithItsBoxesAndAHarderRoad()
    {
        string folder = Path.Combine(Path.GetTempPath(), "ngplus-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(7);

            // A game played to Sinnoh's Hall of Fame, as plain data: a team, a box, the Day Care, the Pokédex, money, key items
            var first = Headless();
            first.StartNewGame("Lucas", PlayerLook.Boy);
            var game = first.Drive;
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Torterra")!, 62));
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 58));
            game.Boxes.Store(new Pokemon(PokemonDatabase.Get("Bidoof")!, 12));
            foreach (var p in game.Party.Members.Concat(game.Boxes.All)) { game.Pokedex.RegisterSeen(p); game.Pokedex.RegisterCaught(p); }
            game.Pokedex.RegisterSeen(PokemonDatabase.Get("Giratina")!.DexNumber);
            game.Bag.AddItem(ItemDatabase.Get("Bicycle")!, 1);
            game.Money = 99999;
            game.Story.Set("SinnohHallOfFame");
            var finished = game.BuildSave();
            finished.DayCare = new DayCareSave { Places = { new SavedDayCarePlace { Pokemon = SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get("Ditto")!, 30)) }, null } };
            string before = JsonSerializer.Serialize(finished);
            var carriedIds = finished.Party.Select(p => p.Id).Concat(finished.Boxes!.Boxes.SelectMany(b => b.Pokemon).Select(b => b.Pokemon.Id)).ToList();
            Assert.All(carriedIds, id => Assert.NotNull(id));
            first.Close();

            // And a plain new game, for the story every game begins with
            var plain = Headless();
            plain.StartNewGame("Lucas", PlayerLook.Boy);
            var freshFlags = plain.Drive.Story.Flags.OrderBy(f => f).ToList();
            plain.Close();

            var next = Headless();
            next.StartNewGamePlus(finished, "Lucas", PlayerLook.Boy);
            var again = next.Drive;

            // In Sinnoh, in the player's room, with nothing in hand: the team waits in the boxes
            Assert.Equal("Sinnoh", RegionDatabase.Get(again.BuildSave().BeganInOrHere())!.Id);
            Assert.Equal(RegionDatabase.First.Start!.Map, again.Map.Name);
            Assert.Equal(0, again.Party.Count);
            Assert.Empty(again.Bag.GetPocketItems(ItemPocket.KeyItems));
            Assert.Equal(new[] { "Bidoof", "Ditto", "Staraptor", "Torterra" }, again.Boxes.All.Select(p => p.Species.Name).OrderBy(n => n));
            Assert.Equal(62, again.Boxes.All.Single(p => p.Species.Name == "Torterra").Level);
            // Each the same Pokémon, its id kept, and none given twice
            var ids = again.Boxes.All.Select(p => p.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.All(carriedIds, id => Assert.Contains(id!.Value, ids));
            Assert.Equal(finished.TrainerId, again.BuildSave().TrainerId);

            // The Pokédex as it was filled, but not the National Pokédex; half the money; one clear; a step harder
            Assert.True(again.Pokedex.IsCaught(PokemonDatabase.Get("Torterra")!.DexNumber));
            Assert.True(again.Pokedex.IsSeen(PokemonDatabase.Get("Giratina")!.DexNumber));
            Assert.False(again.Pokedex.NationalUnlocked);
            Assert.Equal(99999 / 2, again.Money);
            Assert.Equal(1, again.RunState.Clears);
            Assert.Equal(Difficulty.Challenge, again.Run.Difficulty);
            Assert.True(again.Run.ScaleTrainers);

            // The story begins as every game's: nothing of the finished one, and StoryMigration's new game untouched
            Assert.Equal(freshFlags, again.Story.Flags.OrderBy(f => f).ToList());
            Assert.False(again.Story.Has("SinnohHallOfFame"));

            // The finished save is only read
            Assert.Equal(before, JsonSerializer.Serialize(finished));

            // ... and the clears go with the save
            var saved = again.BuildSave();
            Assert.Equal(1, saved.RunState!.Clears);
            Assert.Equal(2, NewGamePlus.Clears(saved));
            next.Close();
        }
        finally
        {
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            RunRules.Current = RunRules.Normal;
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }
}
