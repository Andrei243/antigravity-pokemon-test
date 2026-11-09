partial class Harness
{
    // ---------------------------------------------------------------- what lives here (plan 10 · F11)

    // The very last of `menus`: the DexNav put in the bag, the player on Route 201's grass, the Pokédex opened from the
    // start menu as the game opens it (which tells it what lives here) and the Item button's HERE panel over it, by
    // morning (26n) and after dark (26o). The DexNav, the place and the clock are put back after.
    void MenusHere()
    {
        var dexNav = ItemDatabase.Get(DexNavRules.Item);
        if (dexNav == null)
        {
            Console.WriteLine("!! here: no DexNav in the item table");
            return;
        }
        var map = game.Map;
        int px = game.Player.GridX, py = game.Player.GridY;
        var facing = game.Player.Facing;
        var time = engine.Settings.TimeOfDay;
        bool had = game.Bag.GetQuantity(dexNav) > 0;
        if (!had) game.Bag.AddItem(dexNav, 1);
        var sinnoh = MapDatabase.Get("Sinnoh");
        // A tile of Route 201's tall grass near its middle
        (int X, int Y) grass = (115, 854);
        for (int reach = 0; reach < 20 && !sinnoh.IsTallGrass(grass.X, grass.Y); reach++)
            for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                    if (!sinnoh.IsTallGrass(grass.X, grass.Y) && sinnoh.IsTallGrass(115 + dx, 854 + dy) && sinnoh.IsWalkable(115 + dx, 854 + dy)) grass = (115 + dx, 854 + dy);
        At("Sinnoh", grass.X, grass.Y, Direction.Up);
        var dexScreen = game.PokedexScreen;
        foreach (var (shot, hour) in new[] { ("26n_pokedex_here_morning", TimeOfDay.Morning), ("26o_pokedex_here_night", TimeOfDay.Night) })
        {
            engine.Settings.TimeOfDay = hour;
            engine.ApplySettings(window: false);
            game.State = GameState.Overworld;
            Frames(2);
            game.ChooseFromStartMenu(StartMenuChoice.Pokedex);
            var here = dexScreen.HereEntries;
            foreach (var e in here ?? Array.Empty<DexNavEntry>()) PokemonSprites.Request(e.Species);
            PokemonSprites.Flush(game.RenderContext);
            dexScreen.ToggleHere();
            Frames(30);
            Console.WriteLine($"{shot}: at {grass.X},{grass.Y} ({dexScreen.HerePlace}), focus {dexScreen.Focus}: "
                + string.Join(", ", (here ?? Array.Empty<DexNavEntry>()).Select(e => $"{e.Species} ({e.Ways}{(e.Caught ? ", caught" : e.Seen ? ", seen" : "")}, level {e.Level})")));
            Shot(shot);
            dexScreen.ToggleHere();
            dexScreen.Close();
            game.State = GameState.Overworld;
        }
        engine.Settings.TimeOfDay = time;
        engine.ApplySettings(window: false);
        if (!had) game.Bag.RemoveItem(dexNav, 1);
        game.Map = map;
        game.Player.SetPosition(px, py, facing);
        game.State = GameState.Overworld;
        Frames(2);
    }
}
