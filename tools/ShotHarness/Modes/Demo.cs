partial class Harness
{
    // ---------------------------------------------------------------- scripted demo battle (plan 04 · G8)
    public void DemoMode()
    {
        var renderer = game.BattleRenderer;
        // Barry's Torterra is far stronger than the lead, so the show can go on: it takes every move without fainting,
        // and plays harmless moves itself (one attack, to show the foe's side of a move)
        var demoParty = new Party();
        var lead = new Pokemon(PokemonDatabase.Get("Infernape")!, 60, new Random(3));
        demoParty.Add(lead);
        var rival = new Trainer { Name = "Barry", TrainerClass = "Rival" };
        var torterra = new Pokemon(PokemonDatabase.Get("Torterra")!, 90, new Random(4));
        rival.Party.Add(torterra);

        game.Map = MapDatabase.Get("Sinnoh");
        renderer.SetArena(BattleArena.Grass);
        var b = new BattleEngine(new BattleSetup
        {
            PlayerParty = demoParty, Inventory = inventory, Pokedex = pokedex, Trainers = new List<Trainer> { rival }, Random = new Random(11)
        });
        game.Battle = b;
        game.State = GameState.Battle;

        // The opening: the sweep in, both trainers on their platforms, then each throws out their Pokémon
        Skip(0.05); Shot("d01_intro_sweep");
        Skip(1.0); Shot("d02_intro_trainers");
        Skip(1.0);
        Confirm(b);
        Skip(0.1); Shot("d03_foe_throw");
        Skip(0.22); Shot("d04_foe_ball_open");
        Skip(0.45); Shot("d05_foe_out");
        Skip(0.6);
        Confirm(b);
        Skip(0.1); Shot("d06_player_throw");
        Skip(0.25); Shot("d07_player_ball_open");
        Skip(0.5); Shot("d08_player_out");
        for (int guard = 0; guard < 6 && b.HUD.MenuState == BattleMenuState.Message; guard++) { Confirm(b); Skip(0.3); }
        Skip(0.6); Shot("d09_menu_overview");

        // Moves of every kind, through the real battle flow: the attacker's shot as it winds up, the cut to the
        // target as the effect arrives, the impact, and what follows. Both sides are healed between turns.
        int turn = 0;
        void Use(string move, string tag)
        {
            // Let the last turn's effects finish first
            Skip(1.0);
            lead.Moves.Clear();
            lead.Moves.Add(new Move(MoveDatabase.Get(move)!));
            torterra.Moves.Clear();
            torterra.Moves.Add(new Move(MoveDatabase.Get(turn++ switch { 0 => "Razor Leaf", 1 => "Withdraw", _ => "Splash" })!));
            lead.CurrentHP = lead.MaxHP;
            b.EnemyPokemon.CurrentHP = b.EnemyPokemon.MaxHP;
            lead.ResetStatStages();
            b.EnemyPokemon.ResetStatStages();
            // The lead always moves first, so each move's shots come before the foe's
            b.EnemyPokemon.StatStages[StatType.Speed] = -6;
            b.SelectMove(0);
            Console.WriteLine("  msg: " + b.CurrentMessage);
            Skip(0.12); Shot($"{tag}_a_windup");
            Skip(0.16); Shot($"{tag}_b_cut");
            Skip(0.12); Shot($"{tag}_c_impact");
            Skip(0.3); Shot($"{tag}_d_after");
            for (int guard = 0; guard < 12 && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; guard++)
            {
                Confirm(b);
                if (turn <= 2 && b.CurrentMessage.StartsWith("Foe") && b.CurrentMessage.Contains(" used "))
                {
                    // The foe's attack, and its status move on itself
                    Skip(0.12); Shot($"{tag}_e_foe_windup");
                    Skip(0.36); Shot($"{tag}_f_foe_impact");
                }
                Skip(0.35);
            }
        }
        Use("Flamethrower", "d10_flamethrower");
        Use("Thunderbolt", "d11_thunderbolt");
        Use("Surf", "d12_surf");
        Use("Ice Beam", "d13_ice_beam");
        Use("Shadow Ball", "d14_shadow_ball");
        Use("Razor Leaf", "d15_razor_leaf");
        Use("Close Combat", "d16_close_combat");
        Use("Earthquake", "d17_earthquake");
        Use("Swords Dance", "d18_swords_dance");
        Use("Charm", "d19_charm");
        Use("Psychic", "d20_psychic");
        Use("Dragon Pulse", "d21_dragon_pulse");

        // A critical hit punches in (cued by hand, so the dice don't decide, with the messages it would show)
        b.HUD.MenuState = BattleMenuState.Message;
        b.CurrentMessage = $"{lead.DisplayName} used Slash!";
        b.Anim.Attack(BattleSide.Player, 0, MoveCategory.Physical);
        b.Anim.Cue(new EffectCue { Move = "Slash", Type = PokemonType.Normal, Category = MoveCategory.Physical, FromSide = BattleSide.Player, ToSide = BattleSide.Enemy, Critical = true });
        Skip(0.36); b.Anim.Hit(BattleSide.Enemy, 0, 1.8f);
        Skip(0.12); Shot("d22_critical_punch_in");
        b.CurrentMessage = "A critical hit!";
        Skip(0.3); Shot("d23_critical_after");
        b.HUD.MenuState = BattleMenuState.Main;
        Skip(1.0);

        // The foe faints
        lead.Moves.Clear();
        lead.Moves.Add(new Move(MoveDatabase.Get("Flare Blitz")!));
        b.EnemyPokemon.CurrentHP = 1;
        b.SelectMove(0);
        for (int guard = 0; guard < 6 && !b.CurrentMessage.Contains("fainted"); guard++) { Confirm(b); Skip(0.3); }
        Skip(0.5); Shot("d24_faint");
        Skip(0.5); Shot("d25_faint_sink");

        // A wild Pokémon, a thrown Poké Ball: the run-in, the throw, the flight, the red light, the wobbles, the click
        game.Map = MapDatabase.Get("Sinnoh");
        renderer.SetArena(BattleArena.Grass);
        var wild = new Pokemon(PokemonDatabase.Get("Starly")!, 3) { CurrentHP = 1, Status = StatusCondition.Sleep };
        inventory.AddItem(ItemDatabase.Get("Poké Ball")!, 1);
        var c = new BattleEngine(party, wild, inventory, pokedex, null, new PcBoxes());
        game.Battle = c;
        Skip(2.2); Confirm(c); Skip(1.2); Confirm(c); Skip(0.6);
        c.UseItem(ItemDatabase.Get("Poké Ball")!);
        Confirm(c);
        Skip(0.2); Shot("d30_ball_run_in");
        Skip(0.25); Shot("d31_ball_throw");
        Skip(0.45); Shot("d32_ball_flight");
        Skip(0.65); Shot("d33_ball_open_red_light");
        Skip(0.55); Shot("d34_ball_drop");
        Skip(0.35); Shot("d35_ball_wobble");
        Skip(2.0); Shot("d36_ball_click");
        Skip(0.8); Shot("d37_gotcha");
        Console.WriteLine("  msg: " + c.CurrentMessage);
    }
}
