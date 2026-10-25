using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Contest;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Hoenn's Contest Hall and Pokéblocks (plan 21 · H11; not part of <c>all</c>; <c>hc*</c>): a new game in Hoenn with a
    /// team of three and a bag of berries, Lilycove's Contest Lobby from its door (<c>hc01_lobby</c>) and the
    /// counter, the Berry Blender's three drums and the winners' wall (<c>hc02</c> to <c>hc04</c>), the Contest Hall
    /// behind it (<c>hc05_hall</c>); then a Beauty Contest of the Normal Rank on the contest screen's own buttons: the
    /// line-up (<c>hc10_lineup</c>), the first choice of a move (<c>hc11_choose</c>), a turn's appeal and a round's end
    /// (<c>hc12_turn</c>, <c>hc13_round_end</c>), the results' board and the winner (<c>hc14_results</c>,
    /// <c>hc15_winner</c>) and the award (<c>hc16_award</c>); the Berry Blender at work (<c>hc20_blender_ready</c> to
    /// <c>hc23_blender_result</c>, on a speed the harness's own presses reach) and the Pokéblock Case with a Pokémon
    /// fed one (<c>hc30_case</c>, <c>hc31_case_action</c>, <c>hc32_case_fed</c>). It prints the condition each
    /// contestant went in with, each round's points, the standings, what the win left on the Pokémon and what the
    /// blender made; the dice are the engine's own seeded ones, so the contest is the same every run.
    /// </summary>
    public void HoennContestMode()
    {
        engine.NewGameRegion = "Hoenn";
        engine.StartNewGame();
        game.Story.SetVar("VAR_LITTLEROOT_INTRO_STATE", 7);
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        Frames(20);

        var team = game.Party;
        team.Clear();
        var star = new Pokemon(PokemonDatabase.Get("Milotic")!, 35) { Cool = 90, Beauty = 180, Cute = 120, Smart = 60, Tough = 40, Sheen = 60 };
        star.Moves.Clear();
        foreach (string move in new[] { "Surf", "Aqua Ring", "Safeguard", "Recover" }) star.Moves.Add(new Move(MoveDatabase.Get(move)!));
        team.Add(star);
        team.Add(new Pokemon(PokemonDatabase.Get("Zigzagoon")!, 12));
        team.Add(new Pokemon(PokemonDatabase.Get("Wingull")!, 14));
        game.Story.Set("FLAG_RECEIVED_POKEBLOCK_CASE");
        game.Bag.AddItem(ItemDatabase.Get("Pokéblock Case")!);
        foreach (string berryName in new[] { "Cheri Berry", "Chesto Berry", "Pecha Berry", "Rawst Berry", "Aspear Berry" })
            if (ItemDatabase.Get(berryName) is { } item) game.Bag.AddItem(item, 5);

        // ---- the rooms
        var lobby = MapDatabase.Get("LilycoveCity_ContestLobby");
        var receptionist = lobby.Everyone.FirstOrDefault(n => n.Script == "ContestReceptionist");
        Console.WriteLine($"the Contest Lobby: {lobby.Width} by {lobby.Height}, the receptionist at "
            + (receptionist == null ? "nowhere" : $"({receptionist.GridX}, {receptionist.GridY})")
            + $", {lobby.Everyone.Count()} people");
        if (receptionist != null)
        {
            At("LilycoveCity_ContestLobby", receptionist.GridX, receptionist.GridY + 3, Direction.Up);
            Frames(20);
            game.LocationSign.Hide();
            Shot("hc01_lobby");
        }
        foreach (var (name, script) in new[] { ("hc02_blender_one", "BlenderOne"), ("hc03_blender_three", "BlenderThree") })
        {
            var drum = lobby.SignScripts.FirstOrDefault(s => s.Value == script);
            if (drum.Value == null) { Console.WriteLine($"  !! no sign runs {script}"); continue; }
            At("LilycoveCity_ContestLobby", drum.Key.X, drum.Key.Y + 2, Direction.Up);
            Frames(20);
            Shot(name);
        }
        var wall = lobby.SignScripts.FirstOrDefault(s => s.Value == "Winner1");
        if (wall.Value != null)
        {
            At("LilycoveCity_ContestLobby", wall.Key.X, wall.Key.Y + 2, Direction.Up);
            Frames(20);
            Shot("hc04_winners_wall");
        }
        var hall = MapDatabase.Get("LilycoveCity_ContestHall");
        At("LilycoveCity_ContestHall", hall.Width / 2, hall.Height - 3, Direction.Up);
        Frames(20);
        Shot("hc05_hall");
        game.State = GameState.Overworld;

        // ---- a Beauty Contest of the Normal Rank, on the contest screen's own buttons
        var screen = game.AppealScreen;
        game.State = GameState.Appeals;
        screen.Begin(star, "Brendan", ContestStat.Beauty, AppealRank.Normal, afterTheGame: false, new Random(5));
        foreach (var a in screen.Round!.Appealers) PokemonSprites.Request(a.Pokemon.ModelName);
        Frames(40);
        foreach (var a in screen.Round.Appealers)
            Console.WriteLine($"  {a.Trainer}'s {a.Pokemon.DisplayName}: condition {a.Round1Points}, {a.Moves.Count} moves"
                + (a.IsPlayer ? " (the player's)" : ""));
        Shot("hc10_lineup");
        void Press(int times = 1) { for (int i = 0; i < times; i++) { screen.PressConfirm(); Frames(6); } }
        void Until(AppealPhase phase, int most = 200)
        {
            for (int i = 0; i < most && screen.Phase != phase && screen.IsActive; i++) { screen.PressConfirm(); screen.Advance(0.5f); Frames(1); }
            if (screen.Phase != phase) Console.WriteLine($"  !! the contest never came to {phase} (it is at {screen.Phase})");
        }
        Until(AppealPhase.Choose);
        Frames(20);
        Shot("hc11_choose");
        int round = 0;
        while (screen.IsActive && screen.Phase is not (AppealPhase.Results or AppealPhase.Award or AppealPhase.Done) && round < AppealRound.Appeals)
        {
            if (screen.Phase == AppealPhase.Choose)
            {
                // The moves in turn, so that nothing repeats and the crowd is stirred by the ones of the contest's quality
                while (screen.MoveCursor != round % screen.Round.Appealers[screen.Player].Moves.Count) screen.Move(0, 1);
                screen.PressConfirm();
                Frames(6);
                round++;
            }
            else if (screen.Phase == AppealPhase.Turn)
            {
                screen.Advance(0.9f);
                Frames(2);
                if (round == 1 && screen.Mover >= 0) Shot("hc12_turn");
                screen.PressConfirm();
                Frames(2);
            }
            else if (screen.Phase == AppealPhase.RoundEnd)
            {
                if (round == 1) Shot("hc13_round_end");
                screen.PressConfirm();
                Frames(2);
            }
            else { screen.PressConfirm(); Frames(1); }
        }
        Until(AppealPhase.Results, 400);
        Frames(30);
        Shot("hc14_results");
        for (int i = 0; i < 20 && screen.IsActive && screen.Phase == AppealPhase.Results; i++) { screen.PressConfirm(); Frames(4); if (i == 6) Shot("hc15_winner"); }
        if (screen.IsActive && screen.Phase == AppealPhase.Award) { Frames(30); Shot("hc16_award"); }
        var outcome = screen.Outcome;
        Console.WriteLine($"  the contest: place {outcome?.Placement}, {outcome?.Points} points, ribbon {outcome?.Ribbon ?? "none"}; "
            + string.Join(", ", screen.Round!.Appealers.Select((a, i) => $"{a.Pokemon.DisplayName} {screen.Round.TotalPoints[i]}")));
        screen.Close();
        game.State = GameState.Overworld;

        // ---- the Berry Blender, with a speed the harness's own presses reach
        var berry = ItemDatabase.Get("Cheri Berry")!;
        game.BeginBlender(berry, 2, new Random(3));
        game.State = GameState.Blender;
        var blender = game.BlenderScreen;
        Frames(10);
        Shot("hc20_blender_ready");
        blender.PressConfirm();
        blender.Advance(1.0f);
        Frames(2);
        Shot("hc21_blender_count");
        blender.Advance(BlenderScreen.CountStep * 3);
        for (int i = 0; i < 200 && blender.Phase == BlenderPhase.Play; i++)
        {
            blender.Advance(1f / BlenderGame.FramesPerSecond);
            if (blender.Game!.PocketUnderArm() == 0) blender.PressConfirm();
            if (i == 120) { Frames(2); Shot("hc22_blender_playing"); }
        }
        for (int i = 0; i < 2000 && blender.Phase == BlenderPhase.Play; i++) blender.Advance(1f / 30);
        Frames(20);
        Shot("hc23_blender_result");
        Console.WriteLine($"  the blender: {blender.Game!.Bests} best, {blender.Game.Goods} good, {blender.Game.Misses} missed, "
            + $"top speed {BlenderGame.RpmText(blender.Game.MaxRpm)}, made {blender.Result?.Name} ({blender.Result?.Level}, feel {blender.Result?.ShownFeel}), kept {blender.Kept}");
        blender.Close();
        game.State = GameState.Overworld;

        // ---- the Pokéblock Case, a Pokémon fed
        foreach (var block in new[]
        {
            new Pokeblock(PokeblockColor.Red, 14, 0, 0, 0, 0, 25), new Pokeblock(PokeblockColor.Pink, 0, 0, 22, 0, 0, 30),
            new Pokeblock(PokeblockColor.Purple, 12, 8, 0, 0, 0, 20), new Pokeblock(PokeblockColor.Gray, 9, 0, 6, 12, 0, 18),
            new Pokeblock(PokeblockColor.Black, 2, 0, 2, 0, 2, 5)
        }) game.PokeblockCase.Add(block);
        game.OpenPokeblockCase();
        Frames(20);
        Shot("hc30_case");
        var cases = game.PokeblockCaseScreen;
        cases.Confirm();
        Frames(10);
        Shot("hc31_case_action");
        cases.Confirm();
        Frames(10);
        cases.Confirm();
        Frames(30);
        Shot("hc32_case_fed");
        Console.WriteLine($"  fed: {star.DisplayName}'s condition is now {star.Cool}/{star.Beauty}/{star.Cute}/{star.Smart}/{star.Tough}, sheen {star.Sheen}; the case holds {game.PokeblockCase.Count}");
        game.State = GameState.Overworld;
        engine.NewGameRegion = "Sinnoh";
    }
}
