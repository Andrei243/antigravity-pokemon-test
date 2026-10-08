partial class Harness
{
    // ---------------------------------------------------------------- arenas (plan 04 · G8)
    public void ArenasMode()
    {
        var renderer = game.BattleRenderer;
        void Arena(string name, BattleArena kind, PokemonType? theme = null, TreeStyle trees = TreeStyle.Round, bool lakeside = false, TimeOfDay time = TimeOfDay.Day)
        {
            if (!Wanted(name)) return;
            engine.Settings.TimeOfDay = time;
            engine.ApplySettings(window: false);
            game.Map = MapDatabase.Get("Sinnoh");
            renderer.SetArena(kind, theme, trees, lakeside);
            var b = new BattleEngine(party, new Pokemon(PokemonDatabase.Get("Shinx")!, 5, new Random(2)), inventory, pokedex);
            game.Battle = b;
            game.State = GameState.Battle;
            Skip(2.2); Confirm(b); Skip(1.2); Confirm(b); Skip(0.8);
            Shot(name);
        }
        Arena("a01_grass", BattleArena.Grass);
        Arena("a02_grass_pines_lake", BattleArena.Grass, null, TreeStyle.Pine, true);
        Arena("a03_forest", BattleArena.Forest);
        Arena("a04_forest_night", BattleArena.Forest, time: TimeOfDay.Night);
        Arena("a05_cave", BattleArena.Cave);
        Arena("a06_water", BattleArena.Water);
        Arena("a07_water_twilight", BattleArena.Water, time: TimeOfDay.Twilight);
        Arena("a08_snow", BattleArena.Snow);
        Arena("a09_sand", BattleArena.Sand);
        Arena("a10_indoors", BattleArena.Indoors);
        Arena("a11_indoors_night", BattleArena.Indoors, time: TimeOfDay.Night);
        int g = 20;
        foreach (var type in new[] { PokemonType.Rock, PokemonType.Grass, PokemonType.Fighting, PokemonType.Water, PokemonType.Ghost, PokemonType.Steel, PokemonType.Ice, PokemonType.Electric })
            Arena($"a{g++}_gym_{type.ToString().ToLowerInvariant()}", BattleArena.Gym, type);
        int l = 30;
        foreach (var type in new PokemonType?[] { PokemonType.Bug, PokemonType.Ground, PokemonType.Fire, PokemonType.Psychic, null })
            Arena($"a{l++}_league_{type?.ToString().ToLowerInvariant() ?? "champion"}", BattleArena.League, type);
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
    }
}
