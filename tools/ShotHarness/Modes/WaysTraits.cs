partial class Harness
{
    // V7, the randomiser's abilities, moves and types (plan 13; wy_*, in `ways`, after V6's): the ways-to-play panel with
    // the three rows on (wy_run_panel_traits), then a game of its own under them, past the first chapter: a wild battle on
    // Route 201 with the Turtwig's moves open, the moves it learned by its line's new learnset and its types as the card's
    // hints tell them (wy_traits_moves), and Roark's battle, whose copies keep a Rock move each (wy_traits_roark). It prints
    // the Turtwig's types, ability and moves and Roark's team's moves beside his own. Written, not run, in the session that
    // built it.
    void TraitsMode()
    {
        var run = RunRules.Normal with { Random = new RandomizerOptions { Seed = ShotSeed, Species = false, Items = false, Abilities = true, Moves = true, Types = true } };

        // The panel with the three rows on
        var title = new TitleScreen((SaveData?)null);
        game.Title = title;
        game.State = GameState.Title;
        title.PressConfirm();
        title.PressConfirm();
        title.PressConfirm();
        title.Run.RollSeed = () => ShotSeed;
        foreach (var row in new[] { PokemonPlatinumEngine.UI.Kit.RunRow.RandomAbilities, PokemonPlatinumEngine.UI.Kit.RunRow.RandomMoves, PokemonPlatinumEngine.UI.Kit.RunRow.RandomTypes })
        {
            while (title.Run.SelectedRow != row) title.Run.Move(1);
            title.Change(1);
        }
        Frames(30);
        Shot("wy_run_panel_traits");
        Console.WriteLine($"traits: the panel's run is {title.Run.Rules.Random}");
        title.PressCancel();

        // A game of its own under them
        BeginRun(run);
        var turtwig = game.Party.Members[0];
        Console.WriteLine($"traits: the Turtwig is {turtwig.PrimaryType}/{turtwig.SecondaryType?.ToString() ?? "-"} with {turtwig.AbilityName}, knowing {string.Join(", ", turtwig.Moves.Select(m => $"{m.Data.Name} ({m.Data.Type})"))}");
        Console.WriteLine($"traits: Turtwig learns {string.Join(", ", turtwig.Learnset.Take(8).Select(l => $"{l.Level} {l.MoveName}"))}");

        var sinnoh = MapDatabase.Get("Sinnoh");
        var (gx, gy) = NearestGrass(sinnoh, 116, 886);
        At("Sinnoh", gx, gy, Direction.Up);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 3, MaxLevel = 3, Weight = 100 });
        for (int i = 0; i < 900 && game.State != GameState.Battle; i += 2) Frames(2);
        var b = game.Battle;
        ToMainMenu(b);
        b.SelectMainMenuOption(0);
        Skip(0.4);
        Shot("wy_traits_moves");
        Console.WriteLine($"traits: the wild Starly is {b.EnemyPokemon.PrimaryType}/{b.EnemyPokemon.SecondaryType?.ToString() ?? "-"} with {b.EnemyPokemon.AbilityName}");
        game.State = GameState.Overworld;
        Frames(10);

        // Roark, whose chosen moves are others of their types
        var hall = MapDatabase.Get("OreburghGym");
        var person = hall.Everyone.First(n => n.Name.Equals("Roark", StringComparison.OrdinalIgnoreCase));
        At("OreburghGym", person.GridX, person.GridY + 1, Direction.Up);
        Frames(30);
        var roark = Carrying("leader_roark");
        roark.NpcType = person.NpcType;
        game.StartTrainerBattle(roark);
        for (int i = 0; i < 900 && game.State != GameState.Battle; i += 2) Frames(2);
        var fight = game.Battle;
        Console.WriteLine($"traits: Roark brings {string.Join("; ", fight.Trainers[0].Party.Members.Select(p => $"{p.Species.Name} {p.PrimaryType} {p.AbilityName}: {string.Join(", ", p.Moves.Select(m => m.Data.Name))}"))}");
        Console.WriteLine($"traits: his own team knows {string.Join("; ", roark.TrainerData!.Party.Members.Select(p => string.Join(", ", p.Moves.Select(m => m.Data.Name))))}");
        ToMainMenu(fight);
        Shot("wy_traits_roark");
        game.State = GameState.Overworld;
        Frames(10);

        // The data as it was loaded, for whatever runs after
        Ruleset.Use(RulesPreset.Modern);
    }
}
