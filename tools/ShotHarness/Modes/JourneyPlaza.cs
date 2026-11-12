partial class Harness
{
    /// <summary>
    /// Pennant Hall (plan 41 · EG3; <c>JOURNEY_FROM=plaza</c>): a Champion of Sinnoh, Kanto, Johto and Hoenn comes into the
    /// hall on its first day, its pitches empty and three guests about (<c>eg30</c>), the manager's question of which stall
    /// stands at pitch 1 (<c>eg31</c>), three stalls under their awnings (<c>eg32</c>), a guest's battle at its menu
    /// (<c>eg33</c>) and the hall at its top rank with eight stalls (<c>eg34</c>). Each through the game's own
    /// <c>Interact</c>, scripts and question box; it prints the rank, the pennants and the guests.
    /// </summary>
    private void JourneyPlaza()
    {
        DialogueManager Box() => game.Dialogue;
        void ReadToTheQuestion(int most = 1200)
        {
            for (int i = 0; i < most && !engine.Choice.IsOpen; i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(12);
        }
        void ReadOn(int most = 1200)
        {
            for (int i = 0; i < most && (game.State != GameState.Overworld || Box().IsActive || engine.Choice.IsOpen); i++)
            {
                if (engine.Choice.IsOpen) { engine.Choice.Move(engine.Choice.Options.Count - 1 - engine.Choice.Cursor); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(10);
        }
        void Tell(string what) =>
            Console.WriteLine($"  {what}: rank {game.Plaza.Rank}, {game.Plaza.Coins} pennants ({game.Plaza.Earned} earned), stalls {string.Join(", ", game.Plaza.Pitches.Select(p => p ?? "-"))}, guests {string.Join(", ", game.Plaza.Guests)}");

        BeginAtTheHub();
        At("Hub_Plaza", 11, 13, Direction.Up);
        game.ArriveOnMap();
        game.LocationSign.Hide();
        Frames(30);
        Shot("eg30_plaza_first_day");
        Tell("first day");

        // ---- The manager's question: SET A STALL, pitch 1
        At("Hub_Plaza", 12, 12, Direction.Right);
        game.Interact();
        ReadToTheQuestion();
        engine.Choice.Confirm(); Frames(6);
        ReadToTheQuestion();
        engine.Choice.Confirm(); Frames(6);
        ReadToTheQuestion();
        Shot("eg31_manager_asks_a_pitch");
        engine.Choice.Confirm(); Frames(6);
        ReadOn();
        Tell("a stall set");

        // ---- Three stalls under their awnings
        game.Plaza.Give(160);
        game.Plaza.Set(2, "lottery", game.Story);
        game.Plaza.Set(3, "seals", game.Story);
        game.FurnishPlaza();
        At("Hub_Plaza", 11, 7, Direction.Up);
        Frames(30);
        Shot("eg32_three_stalls");
        Tell("three stalls");

        // ---- A guest's battle
        var guest = game.Map.NPCs.First(n => n.Id == "guest_1");
        var facing = Direction.Down;
        At("Hub_Plaza", guest.GridX, guest.GridY + 1, Direction.Up);
        guest.Facing = facing;
        game.Interact();
        for (int i = 0; i < 1800 && game.State != GameState.Battle; i++)
        {
            if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
            else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        if (game.State == GameState.Battle)
        {
            Skip(1.0);
            ToMainMenu(game.Battle);
            Shot("eg33_guest_battle");
            Console.WriteLine($"  the guest's team: {string.Join(", ", game.Battle.EnemyParty?.Members.Select(p => $"{p.Species.Name} {p.Level}") ?? Array.Empty<string>())}");
            // The guest left a single HP at each menu, the lead's first move until it is over
            var fight = game.Battle;
            for (int guard = 0; guard < 300 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(0);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            for (int i = 0; i < 2400 && game.State != GameState.Overworld; i++) Frames(1);
            ReadOn(2400);
        }
        else Console.WriteLine("  !! the guest's battle never began");
        Tell("after the guest");

        // ---- The top rank: every pitch taken
        game.Plaza.Give(900);
        string[] eight = { "goods", "lottery", "seals", "poffin", "relearner", "kurt", "blender" };
        for (int p = 0; p < eight.Length; p++) game.Plaza.Set(p + 1, eight[p], game.Story);
        game.FurnishPlaza();
        At("Hub_Plaza", 11, 12, Direction.Up);
        Frames(30);
        Shot("eg34_plaza_top_rank");
        Tell("top rank");
    }

    /// <summary>
    /// The Week's Work (plan 41 · EG6; <c>JOURNEY_FROM=board</c>): the board by the avenue read (<c>eg60</c>), a timed mission
    /// begun (<c>eg61</c>), its corner in its region's field (<c>eg62</c>), the board with a mission done (<c>eg63</c>) and its
    /// reward claimed (<c>eg64</c>). It prints the week's missions.
    /// </summary>
    private void JourneyBoard()
    {
        DialogueManager Box() => game.Dialogue;
        BeginAtTheHub();
        At("Hub", 27, 22, Direction.Up);
        game.ArriveOnMap();
        game.LocationSign.Hide();
        Frames(20);
        game.Interact();
        for (int i = 0; i < 600 && game.State != GameState.MissionBoard; i++) Frames(1);
        Frames(30);
        Shot("eg60_board");
        foreach (var m in game.Missions.Missions) Console.WriteLine($"  {m.Kind} {m.Region} {m.Area} {m.Type} {m.Goal} for {m.Reward}{(m.Item != null ? " and " + m.Item : "")}{(m.IsTimed ? $", {m.Minutes} minutes" : "")}");
        var screen = game.MissionBoardScreen;
        int timed = game.Missions.Missions.FindIndex(m => m.IsTimed);
        while (screen.Cursor != timed) screen.Move(1);
        screen.Confirm();
        Frames(20);
        Shot("eg61_timed_begun");
        screen.Cancel();
        Frames(10);
        for (int i = 0; i < 300 && game.State != GameState.Overworld; i++) Frames(1);

        // ---- Its corner in its region's field
        var running = game.Missions.Running!;
        if (running.Region == RegionDatabase.Kanto) At("Kanto", 70, 272, Direction.Up);
        else At("Sinnoh", 175, 840, Direction.Up);
        game.ArriveOnMap();
        game.LocationSign.Hide();
        Frames(90);
        Shot("eg62_timed_corner");
        Console.WriteLine($"  the timed mission in {running.Region}: {game.Missions.SecondsLeft:0.0} s left");

        // ---- A mission done, and its reward claimed
        var first = game.Missions.Missions[0];
        for (int n = first.Progress; n < first.Goal; n++)
            game.Missions.Tell(new MissionEvent(first.Kind switch
            {
                MissionKind.CatchType or MissionKind.CatchInArea => MissionEventKind.Caught,
                MissionKind.BeatTrainers => MissionEventKind.BeatTrainer,
                MissionKind.WinRematch => MissionEventKind.BeatRematch,
                MissionKind.HatchEggs => MissionEventKind.Hatched,
                MissionKind.Landmark => MissionEventKind.Arrived,
                MissionKind.WinFacility => MissionEventKind.FacilityWin,
                _ => MissionEventKind.ContestWin
            }, first.Region, first.Area, first.Type, null));
        At("Hub", 27, 22, Direction.Up);
        game.ArriveOnMap();
        game.LocationSign.Hide();
        Frames(10);
        game.Interact();
        for (int i = 0; i < 600 && game.State != GameState.MissionBoard; i++) Frames(1);
        Frames(30);
        Shot("eg63_mission_done");
        while (screen.Cursor != 0) screen.Move(1);
        screen.Confirm();
        Frames(20);
        Shot("eg64_reward_claimed");
        Console.WriteLine($"  claimed: {game.Plaza.Coins} pennants, rank {game.Plaza.Rank}");
        screen.Cancel();
        Frames(10);
        for (int i = 0; i < 300 && (game.State != GameState.Overworld || Box().IsActive); i++) Frames(1);
    }

    /// <summary>A new game whose player is Champion of Sinnoh, Kanto, Johto and Hoenn, with a strong team, as the hub's parts begin.</summary>
    private void BeginAtTheHub()
    {
        engine.NewGameRegion = RegionDatabase.Sinnoh;
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        game.Party.Members.Clear();
        foreach (string species in new[] { "Garchomp", "Staraptor", "Infernape" })
            game.Party.Members.Add(new Pokemon(PokemonDatabase.Get(species)!, 100));
        foreach (string region in new[] { RegionDatabase.Sinnoh, RegionDatabase.Kanto, RegionDatabase.Johto, RegionDatabase.Hoenn })
        {
            game.Story.EnterRegion(region);
            game.Story.SetBadges(region, 0xFF);
            game.Story.Set(region + "HallOfFame");
        }
    }
}
