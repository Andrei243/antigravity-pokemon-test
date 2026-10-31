partial class Harness
{
    /// <summary>
    /// Plan 20 · J2, J5 and J8's leftovers (<c>JOHTO_FROM=leftovers</c>, the end of the <c>johto</c> mode): the places J8's
    /// part b opened and nobody shot (Routes 47 and 48, the cliff gate, Cliff Cave, the Safari Zone's gate, hall and areas,
    /// <c>jl01</c> to <c>jl12</c>), the rooms whose looks J2 and J5 left (Sprout Tower's floors, the Violet Gym, a
    /// Pokémon Center, a bedroom, the Burned Tower, a chamber of the Ruins, <c>jl20</c> to <c>jl28</c>), and the Ruins of
    /// Alph's hall come to by a panel put back together (<c>jl30</c> to <c>jl33</c>).
    /// </summary>
    void JohtoLeftoverShots()
    {
        void Spot(string shot, string key)
        {
            var (map, x, y) = AreaSpot(key);
            At(map.Name, x, y, Direction.Up); Frames(3);
            Shot(shot);
            Console.WriteLine($"{shot}: {key} on {map.Name} ({x}, {y}), {map.DisplayNameAt(x, y)}, music {map.BgmTrackAt(x, y)}, "
                + $"{map.NPCs.Count} people, ways out {string.Join(", ", map.Warps.Where(w => !w.Shut).Select(w => w.TargetMap).Distinct())}");
        }

        Spot("jl01_route_47", "route_47");
        Spot("jl02_route_48", "route_48");
        Spot("jl03_cliff_edge_gate", "cliff_edge_gate");
        Spot("jl04_cliff_cave", "cliff_cave");
        Spot("jl05_safari_zone_gate", "safari_zone_gate");
        Spot("jl06_safari_zone_hall", "safari_zone_entrance_interior");
        Spot("jl07_safari_zone_way_in", "safari_zone_entrance_exterior");
        Spot("jl08_safari_zone_01", "safari_zone_01");
        Spot("jl09_safari_zone_04", "safari_zone_04");
        Spot("jl10_safari_zone_07", "safari_zone_07");
        Spot("jl11_safari_zone_12", "safari_zone_12");
        Spot("jl12_safari_gate_center", "safari_zone_gate_pokecenter_1f");

        Spot("jl20_sprout_tower_1f", "sprout_tower_1f");
        Spot("jl21_sprout_tower_2f", "sprout_tower_2f");
        Spot("jl22_sprout_tower_3f", "sprout_tower_3f");
        Spot("jl23_violet_gym", "violet_gym");
        Spot("jl24_cherrygrove_center", "cherrygrove_pokecenter_1f");
        Spot("jl25_player_bedroom", "new_bark_player_house_2f");
        Spot("jl26_burned_tower", "burned_tower_1f");
        Spot("jl27_alph_chamber", "ruins_of_alph_northeast_entrance_first_room");
        Spot("jl28_violet_center", "violet_pokecenter_1f");

        // The Ruins' hall: its door on the grounds shut, a panel put back together, the fall into the hall, and the hall's
        // entrance with its stairs once the door has opened
        var ruins = MapDatabase.Get("Johto");
        var hallDoor = ruins.Warps.First(w => w.TargetMap == "RuinsOfAlph_HallEntrance");
        var door = (X: hallDoor.SourceX, Z: hallDoor.SourceY);
        At("Johto", door.X, door.Z + 2, Direction.Up); Frames(3); Shot("jl30_alph_hall_door_shut");
        Console.WriteLine($"jl30: the hall's door {(ruins.IsWalkable(door.X, door.Z) ? "is open" : "is shut")}");
        game.Story.Set("FLAG_ALPH_KABUTO_PANEL_SOLVED");
        game.Story.Set("FLAG_ALPH_HALL_OPEN");
        At("RuinsOfAlph_UndergroundHall", 26, 4, Direction.Down); game.ArriveOnMap(); Frames(10); Shot("jl31_alph_hall_landed");
        Spot("jl32_alph_underground_hall", "ruins_of_alph_underground_hall");
        Spot("jl33_alph_hall_entrance", "ruins_of_alph_hall_entrance");
        At("Johto", door.X, door.Z + 2, Direction.Up); game.ArriveOnMap(); Frames(3); Shot("jl34_alph_hall_door_open");
        Console.WriteLine($"jl34: the hall's door {(ruins.IsWalkable(door.X, door.Z) ? "is open" : "!! is shut")}");
    }
}
