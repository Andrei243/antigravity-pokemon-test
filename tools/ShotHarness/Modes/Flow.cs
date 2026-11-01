partial class Harness
{
    // ---------------------------------------------------------------- the real encounter flow
    public void FlowMode()
    {
        // Grass encounter -> fade -> battle -> run -> fade back to the field (in grass no trainer is watching)
        GoTo("Route201", 24, 14, Direction.Up);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 3, MaxLevel = 3 });
        Frames(12); Shot("80_flow_fade_out");
        Frames(36); Shot("81_flow_fade_in");
        Frames(60); Shot("82_flow_battle");
        var fb = game.Battle;
        Confirm(fb); Frames(40);
        Confirm(fb); Frames(2);
        fb.SelectMainMenuOption(3); Frames(2); Shot("83_flow_run");
        Confirm(fb);
        Frames(14); Shot("84_flow_leave");
        Frames(60); Shot("85_flow_back");
        Console.WriteLine("state: " + game.State);

        RetryAndSuspendShots();
    }

    /// <summary>
    /// Plan 12 · Q25, last of the mode so that nothing before it moves: a battle lost with RETRY BATTLES on and its
    /// question, the same battle fought again, the close button's SUSPEND AND QUIT over it, and the battle taken up from
    /// the slot at the menu it was left at. The team, the options and the slot are put back as they were.
    /// </summary>
    void RetryAndSuspendShots()
    {
        var team = game.Party.Members.ToList();
        int money = game.Money;
        engine.Settings.RetryBattles = true;
        try
        {
            game.Party.Clear();
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Magikarp")!, 10));
            GoTo("Route201", 24, 14, Direction.Up);
            var before = game.Battle;
            game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 14, MaxLevel = 14 });
            for (int guard = 0; guard < 600 && (game.State != GameState.Battle || game.Battle == before); guard++) Frames(1);
            var lost = game.Battle!;
            ToMainMenu(lost);
            // Splash, turn after turn, until the Starly has the Magikarp down
            for (int guard = 0; guard < 400 && !lost.IsBattleOver; guard++)
            {
                if (lost.HUD.MenuState == BattleMenuState.Main && !lost.IsWaitingForConfirm) lost.SelectMove(0);
                else if (lost.IsWaitingForConfirm) Confirm(lost);
                Skip(0.5);
            }
            for (int guard = 0; guard < 120 && !game.RetryPrompt.IsOpen; guard++) Frames(1);
            Skip(0.4); Shot("86_flow_try_again");
            Console.WriteLine($"lost: result {lost.Result}, asked {game.RetryPrompt.IsOpen}, \"{game.RetryPrompt.Body}\", money {game.Money} (was {money})");

            game.AnswerRetry(RetryChoice.TryAgain);
            for (int guard = 0; guard < 240 && (game.State != GameState.Battle || game.Battle == lost); guard++) Frames(1);
            var again = game.Battle!;
            ToMainMenu(again);
            Shot("87_flow_tried_again");
            Console.WriteLine($"tried again: a new battle {again != lost}, foe {again.EnemyPokemon.Species.Name} {again.EnemyPokemon.CurrentHP}/{again.EnemyPokemon.MaxHP}, "
                + $"lead {game.Party.Members[0].CurrentHP}/{game.Party.Members[0].MaxHP}, retries {game.Statistics.Get(Statistics.Retries)}");

            // A turn on, the close button over the battle: SUSPEND AND QUIT, with the game saved in its slot to go back to
            game.Save();
            again.SelectMove(0);
            Skip(0.5);
            for (int guard = 0; guard < 40 && !(again.HUD.MenuState == BattleMenuState.Main && !again.IsWaitingForConfirm) && !again.IsBattleOver; guard++)
            {
                if (again.IsWaitingForConfirm) Confirm(again);
                Skip(0.5);
            }
            Skip(0.5);
            game.RequestClose();
            Skip(0.4); Shot("88_flow_suspend_prompt");
            Console.WriteLine($"the close button in battle: {string.Join(", ", game.ClosePrompt.Answers)}; \"{game.ClosePrompt.Body}\"");
            int answers = again.Core.Record?.Answers.Count ?? 0;
            game.AnswerClose(CloseChoice.SuspendAndQuit);
            game.ForgetQuit();
            Console.WriteLine($"suspended after {answers} answers: file {BattleSuspension.Exists(SavePaths.Current, game.SaveSlot)}");

            engine.ContinueGame(game.SaveSlot);
            Frames(60);
            var taken = game.Battle!;
            Shot("89_flow_resumed");
            Console.WriteLine($"taken up: state {game.State}, menu {taken.HUD.MenuState}, answers {taken.Core.Record?.Answers.Count}, "
                + $"foe {taken.EnemyPokemon.CurrentHP}/{taken.EnemyPokemon.MaxHP}, file left {BattleSuspension.Exists(SavePaths.Current, game.SaveSlot)}");
        }
        finally
        {
            engine.Settings.RetryBattles = false;
            game.Battle = null;
            game.Party.Clear();
            foreach (var p in team) game.Party.Add(p);
            game.Money = money;
            game.DeleteSave();
            GoTo("Route201", 24, 14, Direction.Up);
            game.State = GameState.Overworld;
        }
    }
}
