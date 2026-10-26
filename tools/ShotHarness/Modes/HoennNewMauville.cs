using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Hoenn's post-game, part d (plan 21 · H12), after part c's shots at the end of the <c>hoenn</c> mode
    /// (<c>HOENN_FROM=postgame</c>): New Mauville's entrance with its door shut and the key's question (<c>h290</c>,
    /// <c>h291</c>), the door open (<c>h292</c>), the plant inside with every barrier up, after the blue switch and after
    /// the green (<c>h293</c> to <c>h295</c>), the generator running and stopped (<c>h296</c>, <c>h297</c>), a Poké Ball
    /// that is a Voltorb and its battle (<c>h298</c>, <c>h299</c>), the Fossil Maniac's tunnel after the Hall of Fame
    /// (<c>h29a</c>) and the Desert Underpass with its fossil (<c>h29b</c>, <c>h29c</c>). It prints the swaps laid and the
    /// flags and variables each left. Written, not run, by the session that built it.
    /// </summary>
    void HoennNewMauvilleShots()
    {
        if (!HoennReady("New Mauville")) return;
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
        void Said(string label) =>
            Console.WriteLine($"  {label}: VAR_NEW_MAUVILLE_STATE {game.Story.Var("VAR_NEW_MAUVILLE_STATE")}, VAR_TEMP_1 {game.Story.Var("VAR_TEMP_1")}, " +
                $"VAR_TEMP_2 {game.Story.Var("VAR_TEMP_2")}, FLAG_HOENN_NEW_MAUVILLE_OPENED {(game.Story.Has("FLAG_HOENN_NEW_MAUVILLE_OPENED") ? "set" : "clear")}");
        void Swapped(string map) =>
            Console.WriteLine($"  {map}: " + string.Join(", ", PokemonPlatinumEngine.Data.MapDatabase.Get(map).SwappedPlaces
                .Select(p => $"{p.Flag ?? p.Var + "=" + p.Value} {(p.Swapped ? "laid" : "not laid")}")));

        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;

        // ---- The entrance: the door shut, the key's question, and the door open
        game.Story.SetVar("VAR_NEW_MAUVILLE_STATE", 0);
        game.Story.Unset("FLAG_HOENN_NEW_MAUVILLE_OPENED");
        game.Story.Set("FLAG_GOT_BASEMENT_KEY_FROM_WATTSON");
        At("NewMauville_Entrance", 4, 4, Direction.Up); Frames(20);
        Swapped("NewMauville_Entrance");
        Shot("h290_new_mauville_door_shut");
        At("NewMauville_Entrance", 4, 3, Direction.Up); Frames(4);
        engine.Steering = (Direction.Up, false); Frames(20); engine.Steering = null;
        Until(() => Box().IsQuestion, "the Basement Key's question", 600);
        Shot("h291_new_mauville_basement_key");
        Until(() => engine.Choice.IsOpen, "the yes and no"); Frames(10); engine.Choice.Confirm(); Frames(4);
        ReadOn();
        Said("the door");
        Swapped("NewMauville_Entrance");
        Shot("h292_new_mauville_door_open");

        // ---- Inside: every barrier up, then the blue switch, then the green
        game.Story.SetVar("VAR_TEMP_1", 0);
        game.Story.SetVar("VAR_TEMP_2", 0);
        At("NewMauville_Inside", 30, 36, Direction.Down); Frames(20);
        Swapped("NewMauville_Inside");
        Shot("h293_new_mauville_barriers_up");
        game.Story.SetVar("VAR_TEMP_1", 1);
        At("NewMauville_Inside", 25, 36, Direction.Left); Frames(20);
        Swapped("NewMauville_Inside");
        Shot("h294_new_mauville_blue_down");
        game.Story.SetVar("VAR_TEMP_1", 0);
        game.Story.SetVar("VAR_TEMP_2", 1);
        At("NewMauville_Inside", 18, 35, Direction.Down); Frames(20);
        Swapped("NewMauville_Inside");
        Shot("h295_new_mauville_green_down");

        // ---- The generator, running and stopped
        game.Story.SetVar("VAR_NEW_MAUVILLE_STATE", 1);
        At("NewMauville_Inside", 33, 7, Direction.Up); Frames(20);
        Shot("h296_new_mauville_generator_running");
        At("NewMauville_Inside", 33, 7, Direction.Up); Frames(2);
        engine.Steering = (Direction.Up, false); Frames(20); engine.Steering = null;
        ReadOn();
        Said("the red switch");
        Swapped("NewMauville_Inside");
        Shot("h297_new_mauville_generator_stopped");

        // ---- A Poké Ball that is a Voltorb
        game.Story.Unset("FLAG_DEFEATED_VOLTORB_1_NEW_MAUVILLE");
        game.Story.SetVar("VAR_TEMP_2", 0);
        At("NewMauville_Inside", 25, 19, Direction.Up); Frames(20);
        Shot("h298_new_mauville_voltorb_ball");
        game.Interact(); Frames(3);
        ReadOn();
        Until(() => game.State == GameState.Battle, "the Voltorb's battle", 600);
        if (game.State == GameState.Battle) { ToMainMenu(game.Battle); Shot("h299_new_mauville_voltorb_battle"); }

        // ---- The Fossil Maniac's tunnel after the Hall of Fame, and the Desert Underpass
        game.State = GameState.Overworld;
        game.Story.Set("HoennHallOfFame");
        At("Route114_FossilManiacsTunnel", 6, 6, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        Shot("h29a_fossil_tunnel_open");
        game.Story.Set("FLAG_CHOSE_ROOT_FOSSIL");
        game.Story.Unset("FLAG_HIDE_DESERT_UNDERPASS_FOSSIL");
        At("DesertUnderpass", 10, 11, Direction.Down); Frames(20);
        Shot("h29b_desert_underpass");
        At("DesertUnderpass", 132, 11, Direction.Up); Frames(20);
        Shot("h29c_desert_underpass_fossil");
        game.Interact(); Frames(3);
        ReadOn();
        Console.WriteLine($"  the fossil: FLAG_HIDE_DESERT_UNDERPASS_FOSSIL {(game.Story.Has("FLAG_HIDE_DESERT_UNDERPASS_FOSSIL") ? "set" : "clear")}");
    }
}
