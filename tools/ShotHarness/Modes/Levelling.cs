partial class Harness
{
    // ---------------------------------------------------------------- levelling the world (plan 18 · W4b, plan 03 · D15)

    // A game begun in Sinnoh with its Hall of Fame entered, met in Johto (not part of `all`; lv*): Route 30's Joey and the
    // line written for the Pokémon his Rattata grows into at 105 (lv01_joey_line), the battle with it (lv02_joey_grown,
    // W4b-2 and D15-A), a Torterra held at Johto's ceiling and the summary's line under its EXP bar (lv03_summary_held,
    // W4b-3), a Gyrodrill of an "ours" slot of Mt. Silver met at its own levels (lv04_ours_wild, D15-B), and last Cynthia's
    // rematch back in Sinnoh at Johto's levels once Johto's Hall of Fame is entered (lv05_rematch_gone_past, L9). It
    // prints each foe's species and level beside the data's, the ceiling and the summary's line
    public void LevellingMode()
    {
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ToBattle()
        {
            for (int i = 0; i < 40 && game.State != GameState.Battle; i++)
            {
                if (Box().IsActive) { Box().FinishLine(); Frames(2); Box().Advance(); }
                Frames(4);
            }
            if (game.State != GameState.Battle) Console.WriteLine("  !! no battle began");
        }
        void LeaveBattle()
        {
            game.Battle = null;
            game.State = GameState.Overworld;
            Frames(10);
        }

        BeginRun(new RunRules());
        game.Story.Set("SinnohHallOfFame");
        var team = game.Party;
        team.Clear();
        var torterra = new Pokemon(PokemonDatabase.Get("Torterra")!, 122);
        team.Add(torterra);

        // ---- Route 30's Joey: his line, then his Pokémon as it is met
        var johto = MapDatabase.Get("Johto");
        game.Place(johto, 553, 334, Direction.Up);
        Frames(2);
        var joey = game.Map.Everyone.FirstOrDefault(n => n.Key == "r30_gsboy2");
        if (joey == null) Console.WriteLine("  !! Joey is not on Route 30");
        else
        {
            var (sx, sy, facing) = new[] { (0, 1, Direction.Up), (0, -1, Direction.Down), (1, 0, Direction.Left), (-1, 0, Direction.Right) }
                .Select(d => (joey.GridX + d.Item1, joey.GridY + d.Item2, d.Item3))
                .FirstOrDefault(s => game.Map.IsWalkable(s.Item1, s.Item2) && game.Map.GetNpcAt(s.Item1, s.Item2) == null);
            game.Player.SetPosition(sx, sy, facing);
            Frames(3);
            game.Interact();
            Until(() => Box().IsActive, "Joey's line");
            Box().FinishLine();
            Frames(4);
            Shot("lv01_joey_line");
            Console.WriteLine($"levelling: Joey says \"{Box().CurrentLine}\"");
            ToBattle();
            if (game.Battle is { } b)
            {
                ToMainMenu(b);
                Shot("lv02_joey_grown");
                var data = joey.TrainerData!.Party.Members[0];
                Console.WriteLine($"levelling: Joey's first is {b.EnemyPokemon.Species.Name} Lv{b.EnemyPokemon.Level} (the data's {data.Species.Name} Lv{data.Level})");
                LeaveBattle();
            }
        }

        // ---- the summary of a Pokémon held at Johto's ceiling
        int? ceiling = game.LevelCap;
        Console.WriteLine($"levelling: the ceiling with no Johto Badge is {ceiling?.ToString() ?? "none"}; the Torterra is Lv{torterra.Level}");
        var partyScreen = game.PartyScreen;
        game.State = GameState.PartyMenu;
        partyScreen.Open();
        Frames(20);
        partyScreen.ShowSummary = true;
        Frames(30);
        Shot("lv03_summary_held");
        partyScreen.ShowSummary = false;
        game.State = GameState.Overworld;
        Frames(10);

        // ---- an "ours" slot of Mt. Silver met at its own levels
        var (silver, mx, my) = AreaSpot("mount_silver");
        game.Place(silver, mx, my, Direction.Up);
        Frames(2);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Gyrodrill", MinLevel = 110, MaxLevel = 114, Weight = 100, Unmapped = true });
        Until(() => game.State == GameState.Battle, "the wild battle");
        if (game.Battle is { } wild)
        {
            ToMainMenu(wild);
            Shot("lv04_ours_wild");
            Console.WriteLine($"levelling: met {wild.EnemyPokemon.Species.Name} Lv{wild.EnemyPokemon.Level} (the slot's 110 to 114, never mapped)");
            LeaveBattle();
        }

        // ---- back in Sinnoh with Johto's Hall of Fame: Cynthia's rematch at Johto's levels
        game.Story.Set("KantoHallOfFame");
        game.Story.Set("JohtoHallOfFame");
        At("Sinnoh", 116, 886, Direction.Up);
        var someone = game.Map.Everyone.FirstOrDefault(n => n.IsTrainer && n.TrainerData != null);
        if (someone == null) { Console.WriteLine("  !! no trainer to stand in for Cynthia"); return; }
        var own = someone.TrainerData;
        var cynthia = new Trainer { Id = "champion_cynthia_rematch" };
        TrainerDatabase.Fill(cynthia, TrainerDatabase.Get(cynthia.Id)!);
        someone.TrainerData = cynthia;
        game.StartTrainerBattle(someone);
        Until(() => game.State == GameState.Battle, "the rematch");
        if (game.Battle is { } rematch)
        {
            ToMainMenu(rematch);
            Shot("lv05_rematch_gone_past");
            Console.WriteLine($"levelling: Cynthia's rematch leads with {rematch.EnemyPokemon.Species.Name} Lv{rematch.EnemyPokemon.Level} (the data's {cynthia.Party.Members[0].Species.Name} Lv{cynthia.Party.Members[0].Level}; Johto's levels give 149)");
            LeaveBattle();
        }
        someone.TrainerData = own;
    }
}
