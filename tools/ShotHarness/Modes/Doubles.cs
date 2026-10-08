partial class Harness
{
    // ---------------------------------------------------------------- double battles
    public void DoublesMode()
    {
        BattleEngine StartDouble(Trainer[] trainers, Pokemon[] wild)
        {
            game.Map = MapDatabase.Get("Sinnoh");
            var d = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Double,
                Trainers = trainers.ToList(), WildPokemon = wild.ToList(), Random = new Random(5)
            });
            game.BattleRenderer.SetArena(game.Map);
            game.Battle = d;
            game.State = GameState.Battle;
            return d;
        }

        // Twins: one trainer sending two at once
        party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 8));
        var twins = new Trainer { Name = "Liv & Liz", TrainerClass = "Lass", DoubleBattle = true };
        twins.Party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 6));
        twins.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 6));
        twins.Party.Add(new Pokemon(PokemonDatabase.Get("Gible")!, 6));
        var d = StartDouble(new[] { twins }, Array.Empty<Pokemon>());
        Skip(130 / 60.0); Shot("90_double_intro");
        Confirm(d); Skip(70 / 60.0); Shot("91_double_foes_out");
        Confirm(d); Skip(70 / 60.0); Shot("92_double_mine_out");
        for (int guard = 0; guard < 6 && d.HUD.MenuState == BattleMenuState.Message; guard++) { Confirm(d); Skip(20 / 60.0); }
        Skip(2 / 60.0); Shot("93_double_main_first");
        d.SelectMainMenuOption(0); Skip(2 / 60.0); Shot("94_double_moves");
        int single = d.PlayerPokemon.Moves.FindIndex(m => m.Target == MoveTarget.Selected && m.Category != MoveCategory.Status);
        d.HUD.MoveMenuIndex = Math.Max(0, single);
        d.SelectMove(Math.Max(0, single)); Skip(2 / 60.0); Shot("95_double_target");
        d.HUD.TargetMenuIndex = 1; Skip(2 / 60.0); Shot("95b_double_target_right");
        d.SelectTarget(1); Skip(2 / 60.0); Shot("96_double_main_second");
        d.SelectMove(0);
        if (d.HUD.MenuState == BattleMenuState.SelectTarget) d.SelectTarget(0);
        Skip(8 / 60.0); Shot("97_double_attack");
        for (int guard = 0; guard < 20 && d.HUD.MenuState == BattleMenuState.Message && !d.IsBattleOver; guard++) { Confirm(d); Skip(25 / 60.0); }
        Skip(0.8); Shot("98_double_after_turn");

        // A fainted Pokémon of the player's has to be replaced
        d.PlayerSlots[1].Pokemon!.CurrentHP = 1;
        d.PlayerSlots[1].Pokemon!.Status = StatusCondition.Burn;
        d.SelectMove(0); if (d.HUD.MenuState == BattleMenuState.SelectTarget) d.SelectTarget(0);
        if (d.HUD.MenuState == BattleMenuState.Main) { d.SelectMove(0); if (d.HUD.MenuState == BattleMenuState.SelectTarget) d.SelectTarget(0); }
        for (int guard = 0; guard < 25 && d.HUD.MenuState == BattleMenuState.Message && !d.IsBattleOver; guard++) { Confirm(d); Skip(25 / 60.0); }
        Shot("99_double_replace");

        // Two trainers together, and two wild Pokémon
        var a = new Trainer { Name = "Ana", TrainerClass = "Youngster" };
        a.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 6));
        var c = new Trainer { Name = "Cal", TrainerClass = "Lass" };
        c.Party.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 6));
        party.HealAll();
        d = StartDouble(new[] { a, c }, Array.Empty<Pokemon>());
        Skip(130 / 60.0); Shot("9a_two_trainers");
        d = StartDouble(Array.Empty<Trainer>(), new[] { new Pokemon(PokemonDatabase.Get("Bidoof")!, 4), new Pokemon(PokemonDatabase.Get("Gible")!, 4) });
        Skip(130 / 60.0); Shot("9b_wild_pair");
        Confirm(d); Skip(70 / 60.0); Confirm(d); Skip(2 / 60.0); Shot("9c_wild_pair_main");

        // A tag battle (plan 06 · R9): Cheryl at the player's side against two grunts
        BattleEngine StartSpecial(BattleSetup setup)
        {
            var e = new BattleEngine(setup);
            game.BattleRenderer.SetArena(game.Map);
            game.Battle = e;
            game.State = GameState.Battle;
            return e;
        }
        party.HealAll();
        var cheryl = new Trainer { Id = "cheryl", Name = "Cheryl", TrainerClass = "Pokémon Trainer" };
        cheryl.Party.Add(new Pokemon(PokemonDatabase.Get("Chansey")!, 20));
        var g1 = new Trainer { Name = "Grunt", TrainerClass = "Galactic Grunt" };
        g1.Party.Add(new Pokemon(PokemonDatabase.Get("Stunky")!, 11));
        var g2 = new Trainer { Name = "Grunt", TrainerClass = "Galactic Grunt" };
        g2.Party.Add(new Pokemon(PokemonDatabase.Get("Glameow")!, 11));
        d = StartSpecial(new BattleSetup
        {
            PlayerParty = party, Inventory = inventory, Pokedex = pokedex, Format = BattleFormat.Double, Trainers = new List<Trainer> { g1, g2 },
            Partner = cheryl, Random = new Random(5)
        });
        Skip(130 / 60.0); Shot("9d_tag_intro");
        for (int guard = 0; guard < 8 && d.HUD.MenuState == BattleMenuState.Message; guard++) { Confirm(d); Skip(70 / 60.0); }
        Skip(2 / 60.0); Shot("9e_tag_main");
        d.SelectMove(0); if (d.HUD.MenuState == BattleMenuState.SelectTarget) d.SelectTarget(0);
        Skip(8 / 60.0);
        for (int guard = 0; guard < 20 && d.HUD.MenuState == BattleMenuState.Message && !d.IsBattleOver; guard++) { Confirm(d); Skip(25 / 60.0); }
        Skip(0.8); Shot("9f_tag_after_turn");

        // The Great Marsh: balls, bait and mud, and no Pokémon of the player's
        d = StartSpecial(new BattleSetup
        {
            PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { new Pokemon(PokemonDatabase.Get("Carnivine")!, 25) },
            Kind = BattleKind.Safari, SpecialBalls = 30, Random = new Random(5)
        });
        Skip(130 / 60.0); Confirm(d); Skip(70 / 60.0);
        for (int guard = 0; guard < 4 && d.HUD.MenuState == BattleMenuState.Message; guard++) { Confirm(d); Skip(30 / 60.0); }
        Skip(2 / 60.0); Shot("9g_safari_main");
        d.SelectMainMenuOption(1); Skip(20 / 60.0); Shot("9h_safari_bait");

        // The catching lesson, which the player watches
        var lessonParty = new Party();
        lessonParty.Add(new Pokemon(PokemonDatabase.Get("Piplup")!, 5));
        var lessonBag = new Inventory();
        lessonBag.AddItem(ItemDatabase.Get("Poké Ball")!, 20);
        d = StartSpecial(new BattleSetup
        {
            PlayerParty = lessonParty, Inventory = lessonBag, Pokedex = pokedex, WildPokemon = new List<Pokemon> { new Pokemon(PokemonDatabase.Get("Bidoof")!, 2) },
            Kind = BattleKind.CatchingLesson, PlayerName = "Dawn", Random = new Random(5)
        });
        Skip(130 / 60.0); Shot("9i_lesson_intro");
        for (int guard = 0; guard < 10 && !d.IsBattleOver; guard++) { Confirm(d); Skip(60 / 60.0); }
        Shot("9j_lesson_caught");
        Timing("double battle");
    }
}
