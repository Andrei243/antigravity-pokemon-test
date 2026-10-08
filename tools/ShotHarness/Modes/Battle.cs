partial class Harness
{
    // ---------------------------------------------------------------- battles
    public void BattleMode()
    {
        // Wild battle: camera sweep, send-out, attack, hit, faint
        var b = StartBattle("Shinx", 5);
        Frames(1); Shot("40_wild_intro_start");
        Skip(50 / 60.0); Shot("41_wild_intro_mid");
        Skip(80 / 60.0); Shot("42_wild_appeared");
        Confirm(b); Skip(10 / 60.0); Shot("43_go_sendout");
        Skip(60 / 60.0); Confirm(b); Skip(2 / 60.0); Shot("44_main_menu");
        b.HUD.MenuState = BattleMenuState.Moves; Frames(1); Shot("44b_moves");
        b.HUD.MenuState = BattleMenuState.Main;

        // Whoever is faster attacks first: lunge, then the hit lands and the HP bar drains
        b.SelectMove(0);
        Console.WriteLine("  msg: " + b.CurrentMessage);
        Skip(8 / 60.0); Shot("45_attack_lunge");
        Skip(14 / 60.0); Shot("46_attack_hit");
        Skip(30 / 60.0); Shot("47_hp_drain");
        for (int guard = 0; guard < 12 && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; guard++)
        {
            Confirm(b);
            Skip(30 / 60.0);
        }
        Shot("49_after_turn");

        // Knock the foe out
        b.EnemyPokemon.CurrentHP = 1;
        b.SelectMove(0);
        Skip(40 / 60.0);
        for (int guard = 0; guard < 6 && !b.CurrentMessage.Contains("fainted"); guard++) { Confirm(b); Skip(20 / 60.0); }
        Skip(18 / 60.0); Shot("50_faint_mid");
        Skip(40 / 60.0); Shot("50b_faint_done");

        // Capture with a Master Ball (always caught) and with a Poké Ball that shakes twice and breaks open: the
        // harness's dice are seeded, so the shake checks are fixed here or the ball would do the same thing every run
        // by accident
        foreach (var (ball, tag) in new[] { ("Master Ball", "caught"), ("Poké Ball", "free") })
        {
            b = StartBattle("Starly", 4, chance: tag == "free"
                ? new PokemonPlatinumEngine.Battle.Sim.BattleRandom(4).Force(PokemonPlatinumEngine.Battle.Sim.RollKind.CatchShake, 0, 0, 65535)
                : null);
            ToMainMenu(b);
            b.UseItem(ItemDatabase.Get(ball)!);
            Confirm(b);
            // The player runs in and throws (0.6 s), the ball flies (0.8 s), opens (0.55 s), drops (0.3 s), then wobbles
            Skip(0.45); Shot($"50c_{tag}_ball_throw");
            Skip(0.55); Shot($"51_{tag}_ball_flight");
            Skip(0.6); Shot($"52_{tag}_ball_open");
            Skip(0.55); Shot($"53_{tag}_ball_ground");
            Skip(0.3); Shot($"54_{tag}_ball_wobble");
            Skip(2.6); Shot($"55_{tag}_result");
            Console.WriteLine("  msg: " + b.CurrentMessage);
        }

        // Trainer battle: both trainers on their platforms, then they step aside as the Pokémon come out
        var trainer = MapDatabase.Get("Sinnoh").NPCs.First(n => n.IsTrainer).TrainerData!;
        b = StartBattle("", 0, trainer);
        Frames(1); Shot("60_trainer_intro_start");
        Skip(130 / 60.0); Shot("61_trainer_wants");
        Confirm(b); Skip(14 / 60.0); Shot("62_trainer_sendout");
        Skip(60 / 60.0); Shot("63_trainer_gone");
        Confirm(b); Skip(14 / 60.0); Shot("64_player_sendout");
        Skip(60 / 60.0); Confirm(b); Skip(2 / 60.0); Shot("65_trainer_main");

        // Forest styles and a range of sizes
        int k = 0;
        foreach (var (foe, map) in new[] { ("Bidoof", "TwinleafTown"), ("Gible", "Route201"), ("Riolu", "LakeVerity"), ("Giratina", "Route201"), ("Starly", "Route202") })
        {
            b = StartBattle(foe, 5, null, map);
            ToMainMenu(b);
            Shot($"7{k++}_field_{foe}");
        }

        // Another lead, for its back sprite
        party.Swap(0, 1);
        b = StartBattle("Turtwig", 5);
        ToMainMenu(b);
        Shot("76_field_second_lead");
        party.Swap(0, 1);

        StartBattle("Luxray", 30);
        Timing("battle");
    }
}
