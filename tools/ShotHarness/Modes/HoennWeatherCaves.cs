using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Hoenn's post-game, part e (plan 21 · H12), after part d's shots at the end of the <c>hoenn</c> mode
    /// (<c>HOENN_FROM=postgame</c>): the Weather Institute's scientist and his report (<c>h2a0</c>, <c>h2a1</c>), Route 114
    /// under the drought with Terra Cave's mouth open (<c>h2a2</c>), the cave's mouth and inner room with Groudon
    /// (<c>h2a3</c>, <c>h2a4</c>) and its battle (<c>h2a5</c>), Route 105 under the rain with its deep water (<c>h2a6</c>),
    /// the sea floor and the passage into the pool (<c>h2a7</c>), the pool under the sea (<c>h2a8</c>), the Marine Cave's
    /// mouth and inner room with Kyogre (<c>h2a9</c>, <c>h2aa</c>) and its battle (<c>h2ab</c>). It prints the place drawn
    /// and the flags of the sixteen. Written, not run, by the session that built it.
    /// </summary>
    void HoennWeatherCavesShots()
    {
        if (!HoennReady("the Marine Cave and Terra Cave")) return;
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
        void Laid(string label)
        {
            var set = string.Join(", ", new[] { "114_NORTH", "114_SOUTH", "115_WEST", "115_EAST", "116_NORTH", "116_SOUTH", "118_EAST", "118_WEST",
                    "105_NORTH", "105_SOUTH", "125_WEST", "125_EAST", "127_NORTH", "127_SOUTH", "129_WEST", "129_EAST" }
                .Where(p => game.Story.Has("FLAG_HOENN_ABNORMAL_WEATHER_ROUTE_" + p)));
            Console.WriteLine($"  {label}: VAR_ABNORMAL_WEATHER_LOCATION {game.Story.Var("VAR_ABNORMAL_WEATHER_LOCATION")}, laid: {(set.Length > 0 ? set : "none")}, " +
                $"Kyogre {(game.Story.Has("FLAG_DEFEATED_KYOGRE") ? "beaten" : "waiting")}, Groudon {(game.Story.Has("FLAG_DEFEATED_GROUDON") ? "beaten" : "waiting")}");
        }
        void Lay(int place, string flag)
        {
            game.Story.SetVar("VAR_ABNORMAL_WEATHER_LOCATION", place);
            game.Story.Set("FLAG_HOENN_ABNORMAL_WEATHER_ROUTE_" + flag);
            Frames(2);
        }

        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        game.Story.Set("HoennHallOfFame");
        game.Story.Set("FLAG_RECEIVED_CASTFORM");
        game.Story.SetVar("VAR_WEATHER_INSTITUTE_STATE", 1);
        game.Story.Unset("FLAG_DEFEATED_KYOGRE");
        game.Story.Unset("FLAG_DEFEATED_GROUDON");

        // ---- The scientist's report
        At("Route119_WeatherInstitute_2F", 4, 7, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        Shot("h2a0_weather_institute_scientist");
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "the scientist's words");
        for (int i = 0; i < 2 && Box().IsActive; i++) { Until(() => Box().IsCurrentLineComplete, "a line"); Box().Advance(); Frames(4); }
        Until(() => Box().IsCurrentLineComplete, "the report");
        Shot("h2a1_weather_report");
        ReadOn();
        Laid("the report");

        // ---- Terra Cave: Route 114 under the drought, its mouth, the inner room and Groudon
        foreach (string p in new[] { "114_NORTH", "115_WEST", "115_EAST", "116_NORTH", "116_SOUTH", "118_EAST", "118_WEST",
                     "105_NORTH", "105_SOUTH", "125_WEST", "125_EAST", "127_NORTH", "127_SOUTH", "129_WEST", "129_EAST" })
            game.Story.Unset("FLAG_HOENN_ABNORMAL_WEATHER_ROUTE_" + p);
        Lay(2, "114_SOUTH");
        At("Hoenn", 46, 51, Direction.Up); Frames(40);
        Shot("h2a2_route114_drought_terra_cave");
        At("TerraCave_Entrance", 8, 17, Direction.Up); Frames(20);
        Shot("h2a3_terra_cave_entrance");
        At("TerraCave_End", 17, 27, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        Shot("h2a4_terra_cave_groudon");
        engine.Steering = (Direction.Up, false); Frames(16); engine.Steering = null;
        ReadOn();
        Until(() => game.State == GameState.Battle, "Groudon's battle", 900);
        if (game.State == GameState.Battle) { ToMainMenu(game.Battle); Shot("h2a5_terra_cave_groudon_battle"); }
        Laid("Groudon met");

        // ---- The Marine Cave: Route 105 under the rain, its deep water, the floor, the pool and Kyogre
        game.State = GameState.Overworld;
        game.Story.Unset("FLAG_HOENN_ABNORMAL_WEATHER_ROUTE_114_SOUTH");
        Lay(9, "105_NORTH");
        At("Hoenn", 11, 289, Direction.Down); Frames(40);
        Shot("h2a6_route105_rain_deep_water");
        At("Underwater_Route105", 13, 268, Direction.Up); Frames(30);
        Shot("h2a7_underwater_route105_passage");
        At("Underwater_MarineCave", 9, 7, Direction.Up); Frames(30);
        Shot("h2a8_underwater_marine_cave");
        At("MarineCave_Entrance", 14, 3, Direction.Down); Frames(20);
        Shot("h2a9_marine_cave_entrance");
        At("MarineCave_End", 9, 27, Direction.Up);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        Shot("h2aa_marine_cave_kyogre");
        engine.Steering = (Direction.Up, false); Frames(16); engine.Steering = null;
        ReadOn();
        Until(() => game.State == GameState.Battle, "Kyogre's battle", 900);
        if (game.State == GameState.Battle) { ToMainMenu(game.Battle); Shot("h2ab_marine_cave_kyogre_battle"); }
        Laid("Kyogre met");
        game.State = GameState.Overworld;
    }
}
