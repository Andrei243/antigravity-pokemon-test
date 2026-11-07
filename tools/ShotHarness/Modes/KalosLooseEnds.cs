using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

partial class Harness
{
    /// <summary>
    /// Plan 23 · Z4's loose ends (the last of the <c>kalos</c> mode, and the whole of it with <c>KALOS_FROM=loose</c>): Route 16
    /// and the Lost Hotel's mouth (<c>wk300</c> on), the caves' chambers, Frost Cavern's mouth on Route 17 and Terminus
    /// Cave's on Route 13, Kiloude City by day and after dark with its pier and landing, its rooms, and AZ on the landing
    /// after the Hall of Fame with his scene's first lines (<c>wks100</c> on). It prints what AZ's scene left behind.
    /// </summary>
    public void KalosLooseEnds()
    {
        var story = game.Story;
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        story.ChooseStarter("Froakie");
        game.Party.Add(new Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get("Greninja")!, 80));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        story.SetVar("VAR_KALOS_STORY", 31);
        story.Set("KalosHallOfFame");
        story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 2);
        Frames(2);

        (string Day, string? Night, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("wk300_lumiose_north_east_gate", null, "Kalos", 392, 304, Direction.Up),
            ("wk301_route16_gate_road", null, "Kalos", 392, 286, Direction.Up),
            ("wk302_route16_crossroad", "wk303_route16_crossroad_night", "Kalos", 405, 270, Direction.Right),
            ("wk304_route16_field", null, "Kalos", 422, 256, Direction.Up),
            ("wk305_lost_hotel_mouth", "wk306_lost_hotel_mouth_night", "Kalos", 422, 245, Direction.Up),
            ("wk307_lost_hotel_lobby", null, "LostHotel", 31, 52, Direction.Up),
            ("wk308_lost_hotel_hall", null, "LostHotel", 27, 36, Direction.Up),
            ("wk309_lost_hotel_gallery", null, "LostHotel", 28, 20, Direction.Up),
            ("wk310_route17_frost_mouth", null, "Kalos", 590, 207, Direction.Up),
            ("wk311_frost_cavern_hall", null, "FrostCavern", 31, 44, Direction.Up),
            ("wk312_frost_cavern_chamber", null, "FrostCavern", 28, 22, Direction.Up),
            ("wk313_route13_terminus_mouth", null, "Kalos", 246, 414, Direction.Up),
            ("wk314_terminus_cave_hall", null, "TerminusCave", 31, 44, Direction.Up),
            ("wk315_terminus_cave_chamber", null, "TerminusCave", 28, 22, Direction.Up),
            ("wk316_couriway_west_street", null, "Kalos", 606, 357, Direction.Left),
            ("wk317_kiloude_street", "wk318_kiloude_street_night", "Kalos", 560, 358, Direction.Left),
            ("wk319_kiloude_plaza", null, "Kalos", 563, 350, Direction.Down),
            ("wk320_kiloude_pier", "wk321_kiloude_pier_night", "Kalos", 563, 368, Direction.Down),
            ("wk322_kiloude_landing", null, "Kalos", 563, 379, Direction.Down),
            ("wk323_kiloude_center", null, "KiloudeCity_PokemonCenter_1F", 8, 11, Direction.Up),
            ("wk324_kiloude_north_house", null, "KiloudeCity_NorthHouse", 4, 8, Direction.Up),
            ("wk325_kiloude_sea_house", null, "KiloudeCity_SeaHouse", 4, 8, Direction.Up),
        };
        foreach (var (day, _, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(day);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (_, night, map, x, y, facing) in places)
        {
            if (night == null) continue;
            At(map, x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(night);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // AZ on the landing, spoken to: the first lines of his scene
        At("Kalos", 563, 380, Direction.Down); Frames(2); game.LocationSign.Hide();
        game.Interact();
        for (int i = 0; i < 900 && !game.Dialogue.IsActive; i++) Frames(1);
        game.Dialogue.FinishLine(); Frames(2);
        Shot("wks100_az_first_line");
        game.Dialogue.Advance(); Frames(2);
        for (int i = 0; i < 300 && !game.Dialogue.IsCurrentLineComplete; i++) Frames(1);
        game.Dialogue.FinishLine(); Frames(2);
        Shot("wks101_az_second_line");
        Console.WriteLine($"  AZ: scene running {engine.ScriptRunning}, done flag {story.Has("FLAG_KALOS_AZ_DONE")}");
    }
}
