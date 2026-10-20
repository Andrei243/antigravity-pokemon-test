partial class Harness
{
    /// <summary>
    /// The way to the League as plan 20 · J10, part b opens it, after the north in the <c>johto</c> mode (so nothing before
    /// it moves; <c>JOHTO_FROM=league</c> runs it alone in a new game of its own): Routes 27 and 26, Kanto's Route 22 and
    /// the Indigo Plateau by day and after dark from the open tile nearest each one's middle (<c>jw87</c> to <c>jw94</c>),
    /// a few places close up (<c>jw95</c> to <c>jw98</c>: Tohjo Falls' mouths, the League's reception gate from Route 26,
    /// the League from its plaza with its crop at the screen's own resolution) and every new room and cave from its own
    /// middle (<c>jr138</c> on), printing each place's name, song, size, people and ways out.
    /// </summary>
    void JohtoLeagueShots()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);
        string[] areas = { "route_27", "route_26", "johto_route_22", "indigo_plateau" };
        void Areas(string time)
        {
            for (int i = 0; i < areas.Length; i++)
            {
                var (map, x, y) = AreaSpot(areas[i]);
                At(map.Name, x, y, Direction.Up); Frames(2);
                Shot($"jw{87 + 2 * i + (time == "night" ? 1 : 0)}_{areas[i]}_{time}");
                if (time == "day") Console.WriteLine($"{areas[i]}: {map.Name} ({x}, {y}), {map.DisplayNameAt(x, y)}, music {map.BgmTrackAt(x, y)}");
            }
        }
        Areas("day");
        // Close up: Tohjo Falls' two mouths on Route 27, the reception gate at Route 26's head, and the League on its plaza
        At("Johto", 742, 399, Direction.Up); Frames(2); Shot("jw95_route_27_tohjo_falls");
        At("Johto", 910, 300, Direction.Up); Frames(2); Shot("jw96_route_26_league_gate");
        At("Johto", 912, 206, Direction.Up); Frames(2); Shot("jw97_indigo_plateau_league"); ShotCrop("jw98_indigo_plateau_native", 640, 300, 640, 360, 2);

        string[] rooms =
        {
            "route_27_house", "tohjo_falls", "tohjo_falls_hidden_room", "route_26_north_house", "route_26_week_siblings_house",
            "route_22_pokemon_league_reception_gate", "johto_victory_road_1f", "johto_victory_road_2f", "johto_victory_road_3f",
            "pokemon_league_entrance", "pokemon_league_entrance_wifi_room", "pokemon_league_will_room", "pokemon_league_koga_room",
            "pokemon_league_bruno_room", "pokemon_league_karen_room", "pokemon_league_lance_room", "johto_pokemon_league_hall_of_fame"
        };
        for (int i = 0; i < rooms.Length; i++)
        {
            var (room, x, y) = AreaSpot(rooms[i]);
            At(room.Name, x, y, Direction.Up); Frames(2);
            Shot($"jr{138 + i}_{rooms[i]}");
            Console.WriteLine($"{rooms[i]}: {room.Name} {room.Width}×{room.Height}, {room.NPCs.Count} people, {room.Props.Count} pieces, music {room.BgmTrackAt(x, y)}, ways out {string.Join(", ", room.Warps.Select(w => $"({w.SourceX}, {w.SourceY}) to {w.TargetMap}"))}");
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Areas("night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
    }
}
