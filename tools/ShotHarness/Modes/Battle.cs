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

        // The information panel (plan 12 · Q11), last so that no shot before it moves: a team, a Pokédex and chance of
        // its own, so neither the harness's team nor its dice are touched
        InfoPanels();

        // Battle Style Shift's offer (plan 08 · P1), last for the same reason, with its own team and chance
        ShiftOffer();
    }

    // A trainer's first Pokémon knocked out under Battle Style Shift: the offer's question with the next one named
    // (23b_battle_offer), the team its Yes opens (23c_battle_offer_team), and the switch made before the trainer's
    // Pokémon comes out (23d_battle_offer_switched)
    void ShiftOffer()
    {
        Pokemon Own(string species, int level, int seed, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level, new Random(seed));
            p.Moves.Clear();
            foreach (var move in moves) p.Moves.Add(new Move(MoveDatabase.Get(move)));
            return p;
        }
        var team = new Party();
        team.Add(Own("Luxray", 40, 41, "Spark", "Crunch"));
        team.Add(Own("Torterra", 40, 42, "Razor Leaf", "Earthquake"));
        var ace = new Trainer { Name = "Ana", TrainerClass = "Youngster" };
        ace.Party.Add(Own("Starly", 12, 43, "Tackle"));
        ace.Party.Add(Own("Gyarados", 30, 44, "Bite"));
        game.Map = MapDatabase.Get("Sinnoh");
        var e = new BattleEngine(new BattleSetup
        {
            PlayerParty = team, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = new List<Trainer> { ace },
            WildPokemon = new List<Pokemon>(), Random = new PokemonPlatinumEngine.Battle.Sim.BattleRandom(13), OffersSwitch = true
        });
        game.BattleRenderer.SetArena(game.Map);
        game.Battle = e;
        game.State = GameState.Battle;
        ToMainMenu(e);

        AnswersOffers = false;
        try
        {
            e.EnemyPokemon.CurrentHP = 1;
            e.SelectMove(0);
            for (int guard = 0; guard < 20 && !e.IsOfferingSwitch && e.HUD.MenuState == BattleMenuState.Message && !e.IsBattleOver; guard++)
            {
                Confirm(e);
                Skip(25 / 60.0);
            }
            Console.WriteLine($"  offer: {e.IsOfferingSwitch}, {e.CurrentMessage} (next: {e.OfferedFoe?.Species.Name})");
            Skip(0.4); Shot("23b_battle_offer");
            if (!e.IsOfferingSwitch) return;
            e.ChooseLearn(0);
            Frames(2); Shot("23c_battle_offer_team");
            e.SelectSwitch(1);
            for (int guard = 0; guard < 12 && e.HUD.MenuState == BattleMenuState.Message && !e.IsBattleOver; guard++)
            {
                Confirm(e);
                Skip(40 / 60.0);
            }
            Console.WriteLine($"  after the offer: {e.PlayerPokemon.Species.Name} against {e.EnemyPokemon.Species.Name}, menu {e.HUD.MenuState}");
            Skip(0.3); Shot("23d_battle_offer_switched");
        }
        finally
        {
            AnswersOffers = true;
        }
    }

    // A single and a double battle played a turn or two into a field with something of everything on it, then the
    // panel opened over the commands (23h_battle_info_*)
    void InfoPanels()
    {
        Pokemon Own(string species, int level, int seed, string ability, string item, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level, new Random(seed)) { AbilityName = ability };
            if (item != null) p.HeldItem = ItemDatabase.Get(item);
            p.Moves.Clear();
            foreach (var move in moves) p.Moves.Add(new Move(MoveDatabase.Get(move)));
            return p;
        }
        BattleEngine Start(Party team, BattleFormat format, params Pokemon[] wild)
        {
            game.Map = MapDatabase.Get("Sinnoh");
            var e = new BattleEngine(new BattleSetup
            {
                PlayerParty = team, Inventory = new Inventory(), Pokedex = new Pokedex(), Format = format,
                WildPokemon = wild.ToList(), Random = new PokemonPlatinumEngine.Battle.Sim.BattleRandom(12)
            });
            game.BattleRenderer.SetArena(game.Map);
            game.Battle = e;
            game.State = GameState.Battle;
            return e;
        }
        void Play(BattleEngine e, params int[] moves)
        {
            foreach (int move in moves)
            {
                if (e.HUD.MenuState != BattleMenuState.Main || e.IsBattleOver) break;
                e.SelectMove(move);
                if (e.HUD.MenuState == BattleMenuState.SelectTarget) e.SelectTarget(0);
            }
            for (int guard = 0; guard < 30 && e.HUD.MenuState == BattleMenuState.Message && !e.IsBattleOver; guard++) { Confirm(e); Skip(25 / 60.0); }
        }
        void Open(BattleEngine e, string shot)
        {
            Skip(0.8);
            e.OpenInfo();
            Frames(1);
            foreach (var line in e.InfoShown?.Lines ?? Enumerable.Empty<string>()) Console.WriteLine("  info: " + line);
            Shot(shot);
            e.CloseInfo();
        }

        // A single battle: the sand, Spikes and Reflect up, Swords Dance, and the foe's Leftovers gone off in the sand
        var team = new Party();
        team.Add(Own("Garchomp", 50, 31, "Sand Veil", "Muscle Band", "Sandstorm", "Spikes", "Swords Dance", "Reflect"));
        var single = Start(team, BattleFormat.Single, Own("Snorlax", 50, 32, "Thick Fat", "Leftovers", "Light Screen", "Curse"));
        ToMainMenu(single);
        for (int move = 0; move < 4; move++) Play(single, move);
        Open(single, "23h_battle_info_single");

        // A double battle: Sand Stream named as Hippowdon comes in, Trick Room, hazards and stages on both sides
        team = new Party();
        team.Add(Own("Garchomp", 50, 33, "Sand Veil", null, "Swords Dance", "Spikes"));
        team.Add(Own("Gyarados", 50, 34, "Intimidate", "Leftovers", "Dragon Dance", "Taunt"));
        var doubles = Start(team, BattleFormat.Double,
            Own("Hippowdon", 50, 35, "Sand Stream", "Smooth Rock", "Stealth Rock", "Yawn"),
            Own("Bronzong", 50, 36, "Levitate", "Light Clay", "Trick Room", "Reflect"));
        Skip(130 / 60.0);
        for (int guard = 0; guard < 12 && doubles.HUD.MenuState == BattleMenuState.Message; guard++) { Confirm(doubles); Skip(50 / 60.0); }
        Play(doubles, 0, 0);
        Play(doubles, 1, 1);
        Open(doubles, "23h_battle_info_double");
    }
}
