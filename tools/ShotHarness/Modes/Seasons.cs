partial class Harness
{
    /// <summary>
    /// Unova's seasons (plan 22 · U3; not part of <c>all</c>): a new game begun in Unova, then Route 19 and Floccesy Town
    /// in each of the four seasons the options fix (<c>season_&lt;name&gt;_route19</c>, <c>season_&lt;name&gt;_floccesy</c>, each
    /// with a native crop), the location sign with its second line in winter (<c>season_sign_winter</c>), and last a
    /// Sinnoh map in winter, which must be its spring's picture (<c>season_winter_twinleaf</c> beside
    /// <c>season_spring_twinleaf</c>). It prints the season the field holds at each, and the sign's two lines.
    /// Plan 22 · U4 adds, in each season, Route 20's south-west where its autumn chunks lie (<c>season_&lt;name&gt;_route20</c>:
    /// the autumn's own layout in autumn, spring's in the other three) and a Deerling and a Sawsbuck on the party screen
    /// in the season's coat (<c>season_&lt;name&gt;_deerling</c>); it prints the layout laid and the coats. Icirrus City's
    /// winter waits for the area (U10).
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
        // The two that wear the season, beside the starter (plan 22 · U4)
        var deerling = new Pokemon(PokemonDatabase.Get("Deerling")!, 12);
        var sawsbuck = new Pokemon(PokemonDatabase.Get("Sawsbuck")!, 36);
        game.Party.Add(deerling);
        game.Party.Add(sawsbuck);

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

            // Route 20's south-west, whose four chunks are others in autumn: the player on the open tile nearest a tile
            // autumn opens, in whichever layout is laid
            var unova = MapDatabase.Get("Unova");
            var (rx, ry) = NearestOpen(unova, 132, 641);
            At("Unova", rx, ry, Direction.Up); Frames(2);
            Console.WriteLine($"{season}: Route 20 lies in {(unova.SeasonLaid?.ToString() ?? "spring")}'s chunks; the player at {rx},{ry}");
            Shot($"season_{name}_route20");

            // The coats on the party screen
            game.State = GameState.PartyMenu;
            game.PartyScreen.Open();
            Frames(40);
            Console.WriteLine($"{season}: Deerling as {deerling.Form ?? "itself"}, Sawsbuck as {sawsbuck.Form ?? "itself"}");
            Shot($"season_{name}_deerling");
            game.PartyScreen.Close();
            game.State = GameState.Overworld;
        }
        game.Party.Remove(deerling);
        game.Party.Remove(sawsbuck);

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

    // The open tile nearest a tile of a map, ring by ring
    static (int X, int Y) NearestOpen(Map map, int x, int y)
    {
        for (int r = 0; r < 32; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r && map.IsWalkable(x + dx, y + dy)) return (x + dx, y + dy);
        return (x, y);
    }
}
