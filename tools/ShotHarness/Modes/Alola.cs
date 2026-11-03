partial class Harness
{
    /// <summary>
    /// Alola drawn by hand (plan 23 · Z5, first part; not part of <c>all</c>): a new game begun there (<c>--region Alola</c>)
    /// in the player's new home on Route 1, then Melemele's first places by day and after dark (<c>wa01</c> to <c>wa12</c>:
    /// the new home, Route 1's grass, Iki Town's stage, the Mahalo Trail's bridge and ruins, Hau'oli City's street and the
    /// professor's laboratory on the beach) and the rooms (<c>wa20</c> to <c>wa25</c>). The chapter's scenes are played
    /// headless by <c>AlolaTests</c>; this mode only looks.
    /// </summary>
    public void AlolaMode()
    {
        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        Console.WriteLine($"a new game in Alola begins on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), "
            + $"with {(game.Party.Members.Count == 0 ? "nobody" : string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}")))}");
        Shot("wa00_new_game");

        (string Day, string Night, int X, int Y, Direction Facing)[] places =
        {
            ("wa01_route1_home_day", "wa02_route1_home_night", 238, 160, Direction.Up),
            ("wa03_route1_grass_day", "wa04_route1_grass_night", 250, 146, Direction.Up),
            ("wa05_iki_stage_day", "wa06_iki_stage_night", 250, 130, Direction.Up),
            ("wa07_mahalo_bridge_day", "wa08_mahalo_bridge_night", 249, 86, Direction.Up),
            ("wa09_hauoli_day", "wa10_hauoli_night", 290, 167, Direction.Up),
            ("wa11_beach_lab_day", "wa12_beach_lab_night", 237, 174, Direction.Up),
        };
        foreach (var (day, _, x, y, facing) in places)
        {
            At("Melemele", x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(day);
            ShotCrop(day + "_native", 640, 300, 640, 360, 2);
        }
        (string Name, string Map, int X, int Y, Direction Facing)[] rooms =
        {
            ("wa20_home_inside", "AlolaRoute1_PlayersHouse", 6, 8, Direction.Up),
            ("wa21_kukui_lab", "AlolaRoute1_KukuiLab", 7, 9, Direction.Up),
            ("wa22_halas_house", "IkiTown_HalasHouse", 5, 7, Direction.Up),
            ("wa23_iki_house", "IkiTown_NortheastHouse", 5, 7, Direction.Up),
            ("wa24_hauoli_center", "HauoliCity_PokemonCenter_1F", 8, 10, Direction.Up),
            ("wa25_hauoli_house", "HauoliCity_WestHouse", 5, 7, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in rooms) { At(map, x, y, facing); Frames(2); Shot(name); }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (_, night, x, y, facing) in places)
        {
            At("Melemele", x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(night);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
    }
}
