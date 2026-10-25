partial class Harness
{
    // V6, the randomiser's species and items (plan 13; wy_*, in `ways`): the ways-to-play panel with the randomiser on and
    // the keyboard the seed is typed on (wy_run_panel_random, wy_seed_keyboard), the briefcase of a seeded game showing
    // the three species its slots go to (wy_briefcase_seeded), a seeded game's first wild battle on Route 201 (wy_seeded_wild:
    // the Starly of the table is the species Starly goes to), a trainer's battle with a mapped team (wy_seeded_trainer: Roark)
    // and the Pokédex's area page of the species met there (wy_seeded_area: the places of the species that stand for it).
    // It prints the mapping and what each shows. Written, not run, in the session that built it.
    const int ShotSeed = 20261025;

    void RandomizerMode()
    {
        var run = RunRules.Normal with { Random = new RandomizerOptions { Seed = ShotSeed } };
        var random = Randomizer.For(run)!;

        // The panel with the randomiser on, and the keyboard open on the seed
        var title = new TitleScreen((SaveData?)null);
        game.Title = title;
        game.State = GameState.Title;
        title.PressConfirm();
        title.PressConfirm();
        title.PressConfirm();
        title.Run.RollSeed = () => ShotSeed;
        while (title.Run.SelectedRow != PokemonPlatinumEngine.UI.Kit.RunRow.RandomSpecies) title.Run.Move(1);
        title.Change(1);
        title.Run.Move(1);
        title.Change(1);
        Frames(30);
        Shot("wy_run_panel_random");
        while (title.Run.SelectedRow != PokemonPlatinumEngine.UI.Kit.RunRow.Seed) title.Run.Move(1);
        title.PressConfirm();
        for (int row = 0; row < 3; row++) title.Move(1);
        title.Change(1);
        title.PressConfirm();
        Frames(20);
        Shot("wy_seed_keyboard");
        Console.WriteLine($"randomizer: the panel's run is {title.Run.Rules.Random}, the keyboard has \"{title.Run.SeedEntry?.Text}\"");
        title.PressCancel();
        title.PressCancel();

        // A game of its own under the seed, past the first chapter
        BeginRun(run);
        Console.WriteLine($"randomizer: seed {ShotSeed}; Starly is {random.Species("Starly")}, Bidoof {random.Species("Bidoof")}, Turtwig {random.Species("Turtwig")}, Dialga {random.Species("Dialga")}");
        Console.WriteLine($"randomizer: a Potion on the ground is a {random.Item(ItemDatabase.Get("Potion")!, "FLAG_OBTAINED_ROUTE_201_POTION").Name} there");

        // The briefcase, as the professor opens it
        foreach (string s in random.Starters(RegionDatabase.Get(RegionDatabase.Sinnoh)!.Starters)) PokemonSprites.Request(s);
        PokemonSprites.Flush(game.RenderContext);
        game.State = GameState.StarterSelect;
        game.StarterSelect.Open(RegionDatabase.Get(RegionDatabase.Sinnoh)!, random);
        Frames(60);
        Shot("wy_briefcase_seeded");
        Console.WriteLine($"randomizer: the briefcase shows {string.Join(", ", game.StarterSelect.Starters)} for the slots {string.Join(", ", game.StarterSelect.Slots)}");
        game.StarterSelect.Close();
        game.State = GameState.Overworld;
        Frames(10);

        // The first wild battle on Route 201
        var sinnoh = MapDatabase.Get("Sinnoh");
        var (gx, gy) = NearestGrass(sinnoh, 116, 886);
        At("Sinnoh", gx, gy, Direction.Up);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 3, MaxLevel = 3, Weight = 100 });
        for (int i = 0; i < 900 && game.State != GameState.Battle; i += 2) Frames(2);
        var b = game.Battle;
        ToMainMenu(b);
        Shot("wy_seeded_wild");
        Console.WriteLine($"randomizer: the wild Starly of Route 201's table is a {b.EnemyPokemon.Species.Name}, level {b.EnemyPokemon.Level}");
        string met = b.EnemyPokemon.Species.Name;
        game.State = GameState.Overworld;
        Frames(10);

        // Roark, whose team is the mapped species at his levels
        var hall = MapDatabase.Get("OreburghGym");
        var person = hall.Everyone.First(n => n.Name.Equals("Roark", StringComparison.OrdinalIgnoreCase));
        At("OreburghGym", person.GridX, person.GridY + 1, Direction.Up);
        Frames(30);
        var roark = Carrying("leader_roark");
        roark.NpcType = person.NpcType;
        game.StartTrainerBattle(roark);
        for (int i = 0; i < 900 && game.State != GameState.Battle; i += 2) Frames(2);
        var fight = game.Battle;
        Console.WriteLine($"randomizer: Roark brings {string.Join(", ", fight.Trainers[0].Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}; his own team is {string.Join(", ", roark.TrainerData!.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}");
        ToMainMenu(fight);
        Shot("wy_seeded_trainer");
        game.State = GameState.Overworld;
        Frames(10);

        // The Pokédex's area page of the species met: the places of the species that stand for it
        game.Pokedex.RegisterSeen(PokemonDatabase.Get(met)!.DexNumber);
        PokemonSprites.Request(met);
        PokemonSprites.Flush(game.RenderContext);
        game.ChooseFromStartMenu(StartMenuChoice.Pokedex);
        var dex = game.PokedexScreen;
        dex.SelectedIndex = dex.Rows.ToList().FindIndex(r => r.Species.Name == met);
        if (dex.SelectedIndex < 0) Console.WriteLine($"randomizer: {met} is not in the Pokédex's list on show (not Sinnoh's)");
        else
        {
            dex.Confirm();
            while (dex.Page != PokedexPage.Area) dex.Sideways(1);
            Frames(4);
            Shot("wy_seeded_area");
            var places = Habitats.For("sinnoh")!.Of(random.Sources(met));
            Console.WriteLine($"randomizer: {met} lives in {string.Join(", ", places.Select(p => p.Name))} (the places of {string.Join(", ", random.Sources(met))})");
        }
        game.State = GameState.Overworld;
        Frames(10);
    }
}
