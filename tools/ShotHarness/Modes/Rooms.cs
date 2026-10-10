partial class Harness
{
    // ---------------------------------------------------------------- rooms rebuilt to the original's plans

    // The rooms built to the original's own floor plans, so its people stand on its tiles (not part of `all`): the
    // three houses of the in-game trades, the Valley Windworks' hall, from the door and from inside, and Hearthome's
    // Contest Hall lobby and gate to Route 209
    public void RoomsMode()
    {
        (string Name, string Map, int X, int Y, Direction Facing)[] plans =
        {
            ("r1_oreburgh_north_house", "OreburghNorthHouse1F", 11, 12, Direction.Up),
            ("r2_eterna_condominiums", "EternaCondominiums1F", 11, 12, Direction.Up),
            ("r3_snowpoint_west_house", "SnowpointWestHouse", 4, 8, Direction.Up),
            ("r4_windworks_door", "ValleyWindworksBuilding", 12, 16, Direction.Up),
            ("r5_windworks_hall", "ValleyWindworksBuilding", 8, 7, Direction.Up),
            ("r6_windworks_commander", "ValleyWindworksBuilding", 18, 6, Direction.Right),
            // Hearthome City's Contest Hall lobby and the gate to Route 209 (plan 02 · S7)
            ("r9_contest_hall_door", "ContestHallLobby", 16, 13, Direction.Up),
            ("r9_contest_hall_booths", "ContestHallLobby", 16, 7, Direction.Up),
            ("r9_gate_209", "Route209GateToHearthomeCity", 1, 7, Direction.Right),
            // Route 209's Lost Tower: its ground floor from the door, a floor of tombs and the top lost in fog (plan 02 · S7)
            ("r9_lost_tower_1f", "LostTower1F", 7, 14, Direction.Up),
            ("r9_lost_tower_3f", "LostTower3F", 12, 3, Direction.Down),
            ("r9_lost_tower_5f_fog", "LostTower5F", 12, 3, Direction.Down)
        };
        foreach (var (name, map, x, y, facing) in plans)
        {
            At(map, x, y, facing); Frames(2); Shot(name);
        }
        At("ValleyWindworksBuilding", 8, 7, Direction.Up); Frames(2);
        ShotCrop("r7_windworks_hall_native", 560, 240, 800, 450, 2);
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("SnowpointWestHouse", 4, 8, Direction.Up); Frames(2); Shot("r8_snowpoint_west_house_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
    }
}
