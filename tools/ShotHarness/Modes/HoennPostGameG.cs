using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Hoenn's post-game, part g (plan 21 · H12), after part f's shots at the end of the <c>hoenn</c> mode
    /// (<c>HOENN_FROM=postgame</c>): the ferry desk giving the Old Sea Map and its six places (<c>h2c0</c>, <c>h2c1</c>),
    /// Faraway Island's shore and its heart with Mew in the grass, Mew a step further off after the player's step and
    /// cornered, and its battle (<c>h2c2</c> to <c>h2c6</c>), the S.S. Tidal boarded at Slateport, its corridor, cabins,
    /// hold and the bed's rest, and the gangway at Lilycove (<c>h2c7</c> to <c>h2cc</c>), Roxanne asking for the rematch
    /// in her Gym (<c>h2cd</c>) and Scott with the Silver Shield (<c>h2ce</c>). It prints the passes, Mew's tile at each
    /// step and the voyage's state. Written, not run, by the session that built it.
    /// </summary>
    void HoennPostGameGShots()
    {
        if (!HoennReady("part g of the post-game")) return;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadOn()
        {
            for (int i = 0; i < 1800 && (engine.ScriptRunning || game.State != GameState.Overworld); i++)
            {
                if (game.State == GameState.Battle) return;
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(6);
            if (Box().IsActive) Box().Close();
        }
        void ToQuestion(string what)
        {
            for (int i = 0; i < 12 && Box().IsActive && !Box().IsQuestion; i++) { Until(() => Box().IsCurrentLineComplete, what); Box().Advance(); Frames(4); }
        }
        NPC? Who(string key) => game.Map.NPCs.FirstOrDefault(n => n.Key == key);

        // Whatever part f's last scene left running is read to its end first, its questions answered no, or its box
        // stands over every shot here
        game.State = GameState.Overworld;
        for (int i = 0; i < 2400 && (engine.ScriptRunning || Box().IsActive); i++)
        {
            if (engine.Choice.IsOpen) { engine.Choice.Move(1); engine.Choice.Confirm(); Frames(4); }
            else if (Box().IsActive && Box().IsCurrentLineComplete) { Box().Advance(); Frames(2); }
            else Frames(1);
            if (game.State == GameState.Battle) game.State = GameState.Overworld;
        }
        if (Box().IsActive) Box().Close();

        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        game.Story.Set("HoennHallOfFame");
        game.Story.Set("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND");
        game.Story.Set("FLAG_RECEIVED_SS_TICKET");
        game.Story.SetVar("VAR_BATTLE_TOWER_PRINT_STATE", 2);
        game.Story.SetVar("VAR_BATTLE_FACTORY_PRINT_STATE", 2);
        game.Story.SetVar("VAR_BATTLE_HALL_PRINT_STATE", 2);

        // ---- The ferry desk: the Old Sea Map and the six places
        At("LilycoveCity_Harbor", 8, 11, Direction.Up);
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "the attendant's words");
        for (int i = 0; i < 12 && Box().IsActive && !Box().IsQuestion && !Box().CurrentLine.Contains("Old Sea Map"); i++)
        { Until(() => Box().IsCurrentLineComplete, "a line"); Box().Advance(); Frames(4); }
        Until(() => Box().IsCurrentLineComplete, "the Old Sea Map");
        Shot("h2c0_ferry_desk_old_sea_map");
        ToQuestion("a line");
        Until(() => engine.Choice.IsOpen, "the places' list");
        Frames(10);
        Shot("h2c1_ferry_desk_six_places");
        Console.WriteLine($"  passes: Navel Rock {game.Story.Has("FLAG_ENABLE_SHIP_NAVEL_ROCK")}, Birth Island {game.Story.Has("FLAG_ENABLE_SHIP_BIRTH_ISLAND")}, Faraway Island {game.Story.Has("FLAG_ENABLE_SHIP_FARAWAY_ISLAND")}");
        for (int k = 0; k < 4; k++) engine.Choice.Move(1);
        engine.Choice.Confirm(); Frames(4);
        ReadOn();

        // ---- Faraway Island and Mew
        At("FarawayIsland_Entrance", 13, 38, Direction.Up); Frames(20);
        Shot("h2c2_faraway_island_shore");
        At("FarawayIsland_Interior", 12, 18, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        var mew = Who("faraway_island_mew");
        Console.WriteLine($"  Mew at {(mew == null ? "nowhere" : $"{mew.GridX},{mew.GridY}")}");
        Shot("h2c3_faraway_island_mew_in_the_grass");
        // Mew steps away as the player steps (the field's own people move here, unlike the rest of the harness)
        bool still = engine.PeopleStayPut;
        engine.PeopleStayPut = false;
        engine.Steering = (Direction.Up, false); Frames(20); engine.Steering = null; Frames(20);
        Console.WriteLine($"  after a step north, Mew at {(mew == null ? "nowhere" : $"{mew.GridX},{mew.GridY}")}");
        Shot("h2c4_faraway_island_mew_steps_away");
        engine.PeopleStayPut = still;
        if (mew != null)
        {
            At("FarawayIsland_Interior", mew.GridX, mew.GridY + 1, Direction.Up); Frames(10);
            Shot("h2c5_faraway_island_mew_cornered");
            game.Interact(); Frames(3);
            ReadOn();
            Until(() => game.State == GameState.Battle, "Mew's battle");
            if (game.State == GameState.Battle) { ToMainMenu(game.Battle); Shot("h2c6_faraway_island_mew_battle"); }
            // leave the battle and let the scene end on the island, or its box stays over every later shot
            if (game.State == GameState.Battle) WinAndReadOut("Mew's battle");
            Box().Close();
        }
        else Console.WriteLine("  !! Mew is not on the island");

        // ---- The S.S. Tidal: Slateport to Lilycove
        At("SlateportCity_Harbor", 8, 11, Direction.Up);
        game.Interact(); Frames(3);
        ToQuestion("the attendant's words");
        Until(() => engine.Choice.IsOpen, "the boarding question");
        Shot("h2c7_slateport_ferry_desk");
        engine.Choice.Confirm(); Frames(4);
        ReadOn();
        At("SSTidalCorridor", 1, 10, Direction.Up); Frames(20);
        Shot("h2c8_ss_tidal_corridor");
        Console.WriteLine($"  voyage: VAR_SS_TIDAL_STATE {game.Story.Var("VAR_SS_TIDAL_STATE")}");
        At("SSTidalRooms", 15, 13, Direction.Up); Frames(20);
        Shot("h2c9_ss_tidal_cabins");
        At("SSTidalLowerDeck", 15, 3, Direction.Down); Frames(20);
        Shot("h2ca_ss_tidal_hold");
        At("SSTidalRooms", 15, 13, Direction.Up); Frames(10);
        game.Interact(); Frames(3);
        Until(() => engine.Choice.IsOpen, "the bed's question");
        engine.Choice.Confirm(); Frames(4);
        for (int i = 0; i < 900 && !(Box().IsActive && Box().CurrentLine.Contains("arrived")); i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete) { Box().Advance(); Frames(2); } else Frames(1);
        }
        Until(() => Box().IsCurrentLineComplete, "the arrival");
        Shot("h2cb_ss_tidal_arrived");
        ReadOn();
        Console.WriteLine($"  voyage: VAR_SS_TIDAL_STATE {game.Story.Var("VAR_SS_TIDAL_STATE")}");
        At("SSTidalCorridor", 1, 10, Direction.Down);
        var gangway = Who("ss_tidal_exit_sailor");
        if (gangway != null)
        {
            At("SSTidalCorridor", gangway.GridX, gangway.GridY - 1, Direction.Down);
            game.Interact(); Frames(3);
            ReadOn();
        }
        Frames(20);
        Shot("h2cc_lilycove_after_the_voyage");
        Console.WriteLine($"  landed on {game.Map.Name}");

        // ---- Roxanne's rematch, and Scott's shield
        game.Story.Set(Pokegear.ContactFlag("roxanne"));
        game.Story.Set(PhoneRematch.WaitingFlag("roxanne"));
        At("RustboroCity_Gym", 5, 3, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        var roxanne = Who("roxanne");
        if (roxanne != null)
        {
            At("RustboroCity_Gym", roxanne.GridX, roxanne.GridY + 1, Direction.Up);
            game.Interact(); Frames(3);
            Until(() => Box().IsCurrentLineComplete, "Roxanne's challenge");
            Shot("h2cd_roxanne_rematch");
            ReadOn();
            Until(() => game.State == GameState.Battle, "Roxanne's rematch", 300);
            if (game.State == GameState.Battle) WinAndReadOut("Roxanne's rematch");
        }
        else Console.WriteLine("  !! Roxanne is not in her Gym");

        game.Story.Set("FLAG_RECEIVED_SCOTTS_BATTLE_POINTS");
        At("BattleFrontier_ScottsHouse", 2, 4, Direction.Up);
        game.Interact(); Frames(3);
        for (int i = 0; i < 6 && Box().IsActive && !Box().CurrentLine.Contains("Shield"); i++)
        { Until(() => Box().IsCurrentLineComplete, "a line"); Box().Advance(); Frames(4); }
        Until(() => Box().IsCurrentLineComplete, "the Silver Shield");
        Shot("h2ce_scott_silver_shield");
        ToQuestion("a line");
        if (engine.Choice.IsOpen) { engine.Choice.Move(1); engine.Choice.Confirm(); Frames(4); }
        ReadOn();
        Console.WriteLine($"  shields: silver {game.Story.Has("FLAG_RECEIVED_SILVER_SHIELD")}, gold {game.Story.Has("FLAG_RECEIVED_GOLD_SHIELD")}");
        game.State = GameState.Overworld;
        // Part h's (plan 21 · H12): Mirage Island
        HoennMirageIslandShots();
    }
}
