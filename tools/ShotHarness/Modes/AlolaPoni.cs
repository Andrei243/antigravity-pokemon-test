using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    /// <summary>
    /// Poni's last places, Blush Mountain and Route 11 (the <c>alolaponi</c> mode, not part of <c>all</c>; plan 23, the
    /// wave after Ultra Space): a new game in Alola past its Hall of Fame, then the new places by day (<c>wp01</c> to
    /// <c>wp15</c>: Poni Grove and Stakataka's ring, Poni Plains, the Gauntlet's rough ground and the Battle Tree's doors,
    /// Poni Meadow and Resolution Cave's mouth, Poni Coast and its islet, the cave's hall and Zygarde's chamber, Route 11,
    /// Blush Mountain's foot and its power plant, Route 15's islet), the rides (<c>wp16</c> Mudsdale on the rough ground,
    /// <c>wp17</c> Sharpedo under a surfer), the altar's keeper (<c>wp18</c>) and Poni Plains after dark (<c>wp19</c>).
    /// </summary>
    public void AlolaPoniMode()
    {
        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        var story = game.Story;
        story.Set("AlolaHallOfFame");
        story.Set("FLAG_ALOLA_LIGHT_RETURNED");
        story.Set(RidePager.MudsdaleFlag);
        story.Set(RidePager.SharpedoFlag);
        story.Set("FLAG_ALOLA_RIDE_LAPRAS");

        (string Name, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("wp01_poni_grove", "Poni", 140, 190, Direction.Right),
            ("wp02_grove_stakataka", "Poni", 182, 210, Direction.Up),
            ("wp03_poni_plains", "Poni", 232, 172, Direction.Right),
            ("wp04_gauntlet_rough_ground", "Poni", 299, 98, Direction.Up),
            ("wp05_battle_tree", "Poni", 299, 22, Direction.Up),
            ("wp06_poni_meadow", "Poni", 262, 222, Direction.Up),
            ("wp07_resolution_cave_mouth", "Poni", 281, 212, Direction.Up),
            ("wp08_poni_coast", "Poni", 200, 262, Direction.Left),
            ("wp09_poni_coast_islet", "Poni", 278, 269, Direction.Down),
            ("wp10_resolution_cave", "ResolutionCave", 48, 60, Direction.Up),
            ("wp11_zygarde", "ResolutionCave", 48, 18, Direction.Up),
            ("wp12_route11", "Ulaula", 110, 287, Direction.Right),
            ("wp13_blush_mountain", "Ulaula", 120, 200, Direction.Up),
            ("wp14_blush_power_plant", "Ulaula", 120, 152, Direction.Up),
            ("wp15_route15_islet", "Ulaula", 301, 292, Direction.Down),
        };
        foreach (var (name, map, x, y, facing) in places)
        {
            At(map, x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(name);
        }

        // Mudsdale galloping over Blush Mountain's rough ground, and Sharpedo under a surfer off Route 15
        At("Ulaula", 120, 199, Direction.Up);
        game.Player.SetRiding("Mudsdale"); Frames(4); game.LocationSign.Hide(); Shot("wp16_mudsdale_rough_ground");
        game.Player.SetRiding(null);
        At("Ulaula", 301, 296, Direction.Up);
        game.Player.SetMode(TravelMode.Surfing);
        game.Player.Carrier = "Lapras";
        game.Player.SetJet("Sharpedo"); Frames(4); game.LocationSign.Hide(); Shot("wp17_sharpedo_jet");
        Console.WriteLine($"  on the water: {game.Player.Mode} on {game.Player.Carrier} ({game.Player.Steed})");
        game.Player.SetJet(null);
        game.Player.SetMode(TravelMode.OnFoot);

        At("PoniAltar", 44, 32, Direction.Up); Frames(2); Shot("wp18_altar_keeper");

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        At("Poni", 232, 172, Direction.Right); Frames(2); game.LocationSign.Hide(); Shot("wp19_poni_plains_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        game.DeleteSave();
    }
}
