partial class Harness
{
    // V11, New Game+: wy_new_game_plus (the title's "Start a new game?" over a finished save, its third answer NEW GAME+
    // highlighted) and wy_new_game_plus_card (the Trainer Card's Run page of the journey it begins, its clear counted).
    // The finished save is made up here, as the title mode's are, with no file behind it; the new game is begun through
    // the engine's own StartNewGamePlus and taken past the opening as BeginRun does. It prints what was offered and carried
    void NewGamePlusMode()
    {
        var finished = new SaveData
        {
            PlayerName = "Lucas", Look = PlayerLook.Boy, CurrentMapName = "Sinnoh", PlayerGridX = 112, PlayerGridY = 884,
            WorldVersion = SaveData.CurrentWorld, PlayTimeSeconds = 48.5f * 3600f, Badges = 0xFF, Money = 84210,
            CaughtSpecies = Enumerable.Range(387, 120).ToList(), SeenSpecies = Enumerable.Range(387, 151).ToList(),
            StoryFlags = { RegionDatabase.First.StoryCompleteFlag },
            Party = new[] { ("Torterra", 62), ("Staraptor", 58), ("Luxray", 57), ("Lucario", 59) }
                .Select(p => SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get(p.Item1)!, p.Item2))).ToList()
        };
        var unfinished = new SaveData
        {
            PlayerName = "Dawn", Look = PlayerLook.Girl, CurrentMapName = "Sinnoh", PlayerGridX = 305, PlayerGridY = 531,
            WorldVersion = SaveData.CurrentWorld, PlayTimeSeconds = 6.2f * 3600f, Badges = 0b11,
            Party = { SavedPokemonData.FromPokemon(new Pokemon(PokemonDatabase.Get("Monferno")!, 21)) }
        };
        var title = TitleScreen.WithSlots(new[] { new TitleSlot(1, finished), new TitleSlot(2, unfinished) });
        game.Title = title;
        game.State = GameState.Title;
        title.PressConfirm();
        Frames(72);
        title.PressConfirm();
        Frames(60);
        while (title.Entries[title.SelectedIndex] != TitleChoice.NewGame) title.Move(1);
        title.PressConfirm();
        title.Move(1);
        title.Move(1);
        Frames(30);
        Shot("wy_new_game_plus");
        Console.WriteLine($"new game+: phase {title.Phase}, offered from slot {title.PlusCard?.Number}, highlighted {title.PlusHighlighted}, " +
            $"\"{TitleScreen.PlusBlurb(title.FreeSlot ?? 1, title.PlusCard?.Number ?? 0, finished)}\"");
        title.PressConfirm();
        Console.WriteLine($"new game+: chosen run {title.ChosenRun.PresetKey} at {title.ChosenRun.Difficulty}, scaled {title.ChosenRun.ScaleTrainers}, into slot {title.Slot}");

        engine.StartNewGamePlus(finished, null, PlayerLook.Boy);
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        PastTheOpening();
        Console.WriteLine($"new game+: {game.Party.Count} in the team, {game.Boxes.Count} in the boxes ({string.Join(", ", game.Boxes.All.Select(p => $"{p.Species.Name} {p.Level}"))}), " +
            $"caught {game.Pokedex.CaughtCount}, money {game.Money}, clears {game.RunState.Clears}, run {game.Run.Difficulty}");
        game.ChooseFromStartMenu(StartMenuChoice.Trainer);
        while (game.TrainerCard.Page != CardPage.Run) game.TrainerCard.Turn(1);
        Frames(40);
        Shot("wy_new_game_plus_card");
        game.TrainerCard.Close();
        game.State = GameState.Overworld;
        Frames(10);
    }
}
