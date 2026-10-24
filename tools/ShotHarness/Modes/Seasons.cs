partial class Harness
{
    /// <summary>
    /// Unova's seasons (plan 22 · U3; not part of <c>all</c>): a new game begun in Unova, then Route 19 and Floccesy Town
    /// in each of the four seasons the options fix (<c>season_&lt;name&gt;_route19</c>, <c>season_&lt;name&gt;_floccesy</c>, each
    /// with a native crop), the location sign with its second line in winter (<c>season_sign_winter</c>), and last a
    /// Sinnoh map in winter, which must be its spring's picture (<c>season_winter_twinleaf</c> beside
    /// <c>season_spring_twinleaf</c>). It prints the season the field holds at each, and the sign's two lines.
    /// Plan 22 · U4 adds Icirrus City's winter and a Deerling on the party screen in each.
    /// </summary>
    public void SeasonsMode()
    {
        // Sinnoh first, in spring, for the picture winter must leave alone
        At("Sinnoh", 112, 880, Direction.Down); Frames(2);
        Console.WriteLine($"Twinleaf Town in spring: the field's season is {(engine.SeasonHere?.ToString() ?? "none")}");
        Shot("season_spring_twinleaf");

        engine.NewGameRegion = "Unova";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(10);

        foreach (var season in Enum.GetValues<Season>())
        {
            engine.Settings.Season = season;
            engine.ApplySettings(window: false);
            string name = season.ToString().ToLowerInvariant();
            At("Unova", 73, 693, Direction.Right); Frames(2);
            Console.WriteLine($"{season}: the field holds {engine.SeasonHere} on {game.Map.Name} ({game.Map.DisplayNameAt(game.Player.GridX, game.Player.GridY)})");
            Shot($"season_{name}_route19");
            ShotCrop($"season_{name}_route19_native", 640, 300, 640, 360, 2);
            At("Unova", 113, 678, Direction.Up); Frames(2);
            Shot($"season_{name}_floccesy");
            ShotCrop($"season_{name}_floccesy_native", 640, 300, 640, 360, 2);
        }

        // The sign as Floccesy Town shows it in winter, half a second in
        game.LocationSign.Show(game.Map.ShownNameAt(game.Player.GridX, game.Player.GridY), engine.SeasonHere);
        Console.WriteLine($"the sign: \"{game.LocationSign.Name}\" over \"{game.LocationSign.SecondLine}\"");
        Frames(30);
        Shot("season_sign_winter");
        game.LocationSign.Hide();

        // Winter fixed, back in Sinnoh: nothing of it may show
        At("Sinnoh", 112, 880, Direction.Down); Frames(2);
        Console.WriteLine($"Twinleaf Town with winter fixed: the field's season is {(engine.SeasonHere?.ToString() ?? "none")}");
        Shot("season_winter_twinleaf");

        engine.Settings.Season = Season.Spring;
        engine.ApplySettings(window: false);
    }
}
