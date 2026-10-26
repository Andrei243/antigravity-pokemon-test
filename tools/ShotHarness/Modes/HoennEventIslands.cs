using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Hoenn's post-game, part f (plan 21 · H12), after part e's shots at the end of the <c>hoenn</c> mode
    /// (<c>HOENN_FROM=postgame</c>): the ferry desk giving the Mystic and Aurora Tickets and its five places (<c>h2b0</c>,
    /// <c>h2b1</c>), Navel Rock's quay, shore, fork, peak with Ho-Oh and its battle, foot with Lugia (<c>h2b2</c> to
    /// <c>h2b7</c>), Birth Island's quay, the triangle at its first and its sixth place, Deoxys come down and its battle
    /// (<c>h2b8</c> to <c>h2bc</c>), the Devon researcher with the regenerator at work and the Pokémon handed back
    /// (<c>h2bd</c>, <c>h2be</c>) and Wattson in his Gym asking for the rematch (<c>h2bf</c>). It prints the passes, the
    /// triangle's place at each touch and the fossil's state. Written, not run, by the session that built it.
    /// </summary>
    void HoennEventIslandsShots()
    {
        if (!HoennReady("the event islands")) return;
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
        }
        void Battle(string shot, string what)
        {
            Until(() => game.State == GameState.Battle, what, 900);
            if (game.State == GameState.Battle) { ToMainMenu(game.Battle); Shot(shot); }
            game.State = GameState.Overworld;
        }

        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        game.Story.Set("HoennHallOfFame");
        game.Story.Set("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND");
        game.Story.Set("FLAG_RECEIVED_SS_TICKET");
        game.Story.SetVar("VAR_BATTLE_TOWER_PRINT_STATE", 2);
        game.Story.SetVar("VAR_BATTLE_FACTORY_PRINT_STATE", 2);

        // ---- The ferry desk: the two passes and the five places
        At("LilycoveCity_Harbor", 8, 11, Direction.Up);
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "the attendant's words");
        for (int i = 0; i < 3 && Box().IsActive && !Box().IsQuestion; i++) { Until(() => Box().IsCurrentLineComplete, "a line"); Box().Advance(); Frames(4); }
        Until(() => Box().IsCurrentLineComplete, "the Mystic Ticket");
        Shot("h2b0_ferry_desk_mystic_ticket");
        for (int i = 0; i < 12 && !Box().IsQuestion; i++) { Until(() => Box().IsCurrentLineComplete, "a line"); Box().Advance(); Frames(4); }
        Shot("h2b1_ferry_desk_five_places");
        Console.WriteLine($"  passes: Navel Rock {game.Story.Has("FLAG_ENABLE_SHIP_NAVEL_ROCK")}, Birth Island {game.Story.Has("FLAG_ENABLE_SHIP_BIRTH_ISLAND")}");
        for (int k = 0; k < 2; k++) engine.Choice.Move(1);
        engine.Choice.Confirm(); Frames(4);
        ReadOn();

        // ---- Navel Rock
        At("NavelRock_Harbor", 8, 4, Direction.Up); Frames(20);
        Shot("h2b2_navel_rock_harbor");
        At("NavelRock_Exterior", 10, 14, Direction.Up); Frames(20);
        Shot("h2b3_navel_rock_exterior");
        At("NavelRock_Fork", 11, 77, Direction.Up); Frames(20);
        Shot("h2b4_navel_rock_fork");
        At("NavelRock_Top", 12, 12, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        Shot("h2b5_navel_rock_top_ho_oh");
        engine.Steering = (Direction.Up, false); Frames(16); engine.Steering = null;
        ReadOn();
        Battle("h2b6_navel_rock_ho_oh_battle", "Ho-Oh's battle");
        At("NavelRock_Bottom", 11, 15, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        Shot("h2b7_navel_rock_bottom_lugia");

        // ---- Birth Island: the triangle walked round and Deoxys
        At("BirthIsland_Harbor", 8, 4, Direction.Up); Frames(20);
        Shot("h2b8_birth_island_harbor");
        At("BirthIsland_Exterior", 15, 17, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        Shot("h2b9_birth_island_triangle");
        (int X, int Y)[] places = { (15, 12), (11, 14), (15, 8), (19, 14), (12, 11), (18, 11), (15, 14), (11, 14), (19, 14), (15, 15), (15, 10) };
        for (int touch = 0; touch < places.Length; touch++)
        {
            var (x, y) = places[touch];
            At("BirthIsland_Exterior", x, y + 1, Direction.Up); Frames(4);
            game.Interact(); Frames(4);
            ReadOn();
            Console.WriteLine($"  touch {touch + 1}: VAR_DEOXYS_ROCK_LEVEL {game.Story.Var("VAR_DEOXYS_ROCK_LEVEL")}");
            if (touch == 5) Shot("h2ba_birth_island_triangle_moved");
            if (game.State == GameState.Battle) break;
        }
        if (game.State != GameState.Battle) Shot("h2bb_birth_island_deoxys");
        Battle("h2bc_birth_island_deoxys_battle", "Deoxys's battle");

        // ---- The Devon Corporation's Fossil Regenerator
        game.Bag.AddItem(PokemonPlatinumEngine.Data.ItemDatabase.Get("Root Fossil")!, 1);
        At("RustboroCity_DevonCorp_2F", 14, 9, Direction.Up);
        game.Interact(); Frames(3);
        for (int i = 0; i < 6 && !Box().IsQuestion; i++) { Until(() => Box().IsCurrentLineComplete, "a line"); Box().Advance(); Frames(4); }
        engine.Choice.Confirm(); Frames(4);
        ReadOn();
        Shot("h2bd_devon_regenerator_at_work");
        At("RustboroCity_DevonCorp_2F", 14, 9, Direction.Up);
        game.ArriveOnMap(); Frames(10);
        ReadOn();
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "the researcher's words");
        for (int i = 0; i < 2 && Box().IsActive; i++) { Until(() => Box().IsCurrentLineComplete, "a line"); Box().Advance(); Frames(4); }
        Until(() => Box().IsCurrentLineComplete, "Lileep handed back");
        Shot("h2be_devon_lileep_handed_back");
        ReadOn();
        Console.WriteLine($"  fossil: state {game.Story.Var("VAR_FOSSIL_RESURRECTION_STATE")}, received {game.Story.Has("FLAG_RECEIVED_REVIVED_FOSSIL_MON")}");

        // ---- Wattson's rematch in his Gym
        game.Story.Set("FLAG_GOT_TM_THUNDERBOLT_FROM_WATTSON");
        game.Story.Set("FLAG_WATTSON_REMATCH_AVAILABLE");
        game.Story.Set("FLAG_PHONE_REMATCH_WATTSON");
        game.Story.Unset("FLAG_HIDE_MAUVILLE_GYM_WATTSON");
        At("MauvilleCity_Gym", 5, 3, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        var wattson = game.Map.NPCs.FirstOrDefault(n => n.Key == "wattson");
        if (wattson != null)
        {
            At("MauvilleCity_Gym", wattson.GridX, wattson.GridY + 1, Direction.Up);
            game.Interact(); Frames(3);
            Until(() => Box().IsCurrentLineComplete, "Wattson's challenge");
            Shot("h2bf_wattson_rematch");
            ReadOn();
            if (game.State == GameState.Battle) game.State = GameState.Overworld;
        }
        else Console.WriteLine("  !! Wattson is not in his Gym");
        game.State = GameState.Overworld;
    }
}
