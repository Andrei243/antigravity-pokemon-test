partial class Harness
{
    /// <summary>
    /// Ula'ula, Poni, Aether Paradise and the League drawn by hand (plan 23 · Z7; not part of <c>all</c>): a new game begun
    /// in Alola, then each new place by day (<c>wu01</c> to <c>wu14</c>: Malie City, Route 10, Mount Hokulani, Route 12,
    /// Tapu Village, Route 13, the foot of Mount Lanakila, Route 16, Po Town, Seafolk Village, Poni Wilds, Poni
    /// Breakers, Exeggutor Island and Aether Paradise), the four caves (<c>wu20</c> to <c>wu23</c>), the rooms
    /// (<c>wu30</c> to <c>wu42</c>: the trials' rooms, Po Town's manor, the head office and the League's) and Malie
    /// City, Tapu Village and Po Town after dark (<c>wu50</c> to <c>wu52</c>). The story is played headless by
    /// <c>AlolaUlaulaTests</c>; this mode only looks.
    /// </summary>
    // The chunks of a streamed map bake on a worker thread: wait for them, a few seconds at most
    void Settle() { for (int i = 0; i < 24; i++) { Frames(3); System.Threading.Thread.Sleep(120); } }

    public void UlaulaMode()
    {
        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        (string Name, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("wu01_malie_city", "Ulaula", 59, 345, Direction.Up), ("wu02_route_10", "Ulaula", 46, 290, Direction.Up),
            ("wu03_mount_hokulani", "Ulaula", 52, 210, Direction.Up), ("wu04_route_12", "Ulaula", 130, 235, Direction.Right),
            ("wu05_tapu_village", "Ulaula", 228, 226, Direction.Up), ("wu06_route_13", "Ulaula", 290, 225, Direction.Up),
            ("wu07_lanakila_foot", "Ulaula", 290, 175, Direction.Up), ("wu08_route_16", "Ulaula", 238, 160, Direction.Up),
            ("wu09_po_town", "Ulaula", 238, 115, Direction.Up), ("wu10_seafolk_village", "Poni", 58, 262, Direction.Up),
            ("wu11_poni_wilds", "Poni", 58, 195, Direction.Up), ("wu12_poni_breakers", "Poni", 100, 125, Direction.Right),
            ("wu13_exeggutor_island", "Poni", 240, 95, Direction.Up), ("wu14_aether_paradise", "AetherParadise", 48, 40, Direction.Up),
            ("wu20_ancient_poni_path", "AncientPoniPath", 32, 70, Direction.Up), ("wu21_vast_poni_canyon", "VastPoniCanyon", 48, 70, Direction.Up),
            ("wu22_poni_altar", "PoniAltar", 32, 60, Direction.Up), ("wu23_mount_lanakila", "MountLanakila", 48, 100, Direction.Up),
            ("wu30_hokulani_observatory", "HokulaniObservatory", 7, 10, Direction.Up), ("wu31_thrifty_megamart", "ThriftyMegamart", 8, 12, Direction.Up),
            ("wu32_shady_house", "ShadyHouse", 6, 8, Direction.Up), ("wu33_skull_manor", "SkullManor", 7, 12, Direction.Up),
            ("wu34_aether_office", "AetherParadise_Office", 8, 10, Direction.Up), ("wu35_malie_center", "MalieCity_PokemonCenter_1F", 8, 10, Direction.Up),
            ("wu36_malie_library", "MalieCity_Library", 5, 9, Direction.Up), ("wu37_league_lobby", "AlolaLeague_Lobby", 8, 10, Direction.Up),
            ("wu38_hala_room", "AlolaLeague_HalaRoom", 6, 9, Direction.Up), ("wu39_acerola_room", "AlolaLeague_AcerolaRoom", 6, 9, Direction.Up),
            ("wu40_kukui_room", "AlolaLeague_KukuiRoom", 7, 10, Direction.Up), ("wu41_hall_of_fame", "AlolaLeague_HallOfFame", 6, 8, Direction.Up),
            ("wu42_seafolk_house", "SeafolkVillage_HouseA", 5, 7, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Settle(); game.LocationSign.Hide(); Shot(name);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (name, map, x, y, facing) in places.Where(p => p.Name.StartsWith("wu01", StringComparison.Ordinal) || p.Name.StartsWith("wu05", StringComparison.Ordinal) || p.Name.StartsWith("wu09", StringComparison.Ordinal)))
        {
            At(map, x, y, facing); Settle(); game.LocationSign.Hide(); Shot(name.Replace("wu0", "wu5", StringComparison.Ordinal) + "_night");
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
    }
}
