partial class Harness
{
    // ---------------------------------------------------------------- conditions and the field (plan 06 · R3)

    // conditions: what a battle's passing states look like. A Pokémon that has flown up is off its platform while its
    // HP box stays (96_flight_away), and is back as it strikes (96_flight_strike, 96_flight_back); and last, trainers who
    // speak in battle (98_speaks_*, SpeakingMode). Not part of "all"
    public void ConditionsMode()
    {
        var flyer = new Pokemon(PokemonDatabase.Get("Staravia")!, 30);
        flyer.Moves.Clear();
        flyer.Moves.Add(new Move(MoveDatabase.Get("Fly")));
        party.Members.Insert(0, flyer);
        var cb = StartBattle("Bidoof", 12);
        ToMainMenu(cb);
        cb.SelectMove(0);

        // Reads on to a line and lets what it starts play for a moment
        void ReadTo(string line)
        {
            for (int i = 0; i < 40 && cb.CurrentMessage != line; i++)
            {
                Confirm(cb);
                Skip(0.3);
            }
            Skip(0.6);
        }

        ReadTo("Staravia flew up high!");
        Shot("96_flight_away");
        ReadTo("Staravia used Fly!");
        Shot("96_flight_strike");
        Skip(1.6);
        Shot("96_flight_back");
        party.Members.Remove(flyer);
        game.State = GameState.Overworld;

        // A move of several hits (plan 06 · R4): the hits land one after another and the count is told after the last
        // (96_hits_landing, 96_hits_told). The count is fixed at five, on a foe that can take them
        var pecker = new Pokemon(PokemonDatabase.Get("Staravia")!, 30);
        pecker.Moves.Clear();
        pecker.Moves.Add(new Move(MoveDatabase.Get("Fury Attack")));
        party.Members.Insert(0, pecker);
        cb = StartBattle("Bidoof", 30, chance: new BattleRandom(7).Force(RollKind.HitCount, 2, 3));
        ToMainMenu(cb);
        cb.SelectMove(0);
        ReadTo("Staravia used Fury Attack!");
        Shot("96_hits_landing");
        for (int i = 0; i < 40 && !cb.CurrentMessage.StartsWith("Hit "); i++)
        {
            Confirm(cb);
            Skip(0.3);
        }
        Skip(0.6);
        Shot("96_hits_told");
        party.Members.Remove(pecker);
        game.State = GameState.Overworld;

        // Transform (plan 06 · R5): Ditto takes the foe's shape on the field as it does in the rules (96_transformed)
        var ditto = new Pokemon(PokemonDatabase.Get("Ditto")!, 30);
        ditto.Moves.Clear();
        ditto.Moves.Add(new Move(MoveDatabase.Get("Transform")));
        party.Members.Insert(0, ditto);
        cb = StartBattle("Bidoof", 12);
        ToMainMenu(cb);
        cb.SelectMove(0);
        ReadTo("Ditto transformed into Foe Bidoof!");
        Skip(1.0);
        Shot("96_transformed");
        party.Members.Remove(ditto);
        game.State = GameState.Overworld;

        // A move to learn with four known (plan 06 · R10): the question, then the moves to forget beside the field
        // (96_learn_ask, 96_learn_choose), and the line once one is forgotten (96_learn_learned)
        var learner = new Pokemon(PokemonDatabase.Get("Turtwig")!, 8);
        learner.Moves.Clear();
        foreach (var m in new[] { "Tackle", "Withdraw", "Growl", "Leer" }) learner.Moves.Add(new Move(MoveDatabase.Get(m)));
        learner.CurrentExp = learner.ExpForNextLevel - 1;
        party.Members.Insert(0, learner);
        cb = StartBattle("Bidoof", 3);
        ToMainMenu(cb);
        cb.EnemyPokemon.CurrentHP = 1;
        cb.SelectMove(0);
        for (int i = 0; i < 80 && !cb.IsLearningMove; i++)
        {
            Confirm(cb);
            Skip(0.3);
        }
        Skip(0.5);
        Shot("96_learn_ask");
        cb.ChooseLearn(0);
        for (int i = 0; i < 20 && !cb.IsLearningMove; i++)
        {
            Confirm(cb);
            Skip(0.3);
        }
        cb.MoveLearnCursor(1);
        Skip(0.5);
        Shot("96_learn_choose");
        cb.ChooseLearn(1);
        ReadTo("Turtwig learned Absorb!");
        Shot("96_learn_learned");
        party.Members.Remove(learner);
        game.State = GameState.Overworld;

        WeatherAndDollMode();
        SpeakingMode();
        // What lies on the field (plan 25 · Y2, Modes/ConditionsField.cs): last, so the shots before it stay as they were
        FieldLookMode();
    }

    // The weather and the doll (plan 25 · Y1): a battle in each of the five weathers at its menu (96_weather_<kind>)
    // and at the turn's line that says it goes on, with its cue playing again (96_weather_<kind>_turn); a Substitute's
    // doll dropped in (96_substitute_up), stepped aside for its owner's own move (96_substitute_aside) and fading in its
    // puff as a hit breaks it (96_substitute_faded)
    void WeatherAndDollMode()
    {
        BattleEngine Wild(string foe, int level, string[] foeMoves, BattleWeather sky, BattleRandom chance)
        {
            var enemy = new Pokemon(PokemonDatabase.Get(foe)!, level);
            enemy.Moves.Clear();
            foreach (var m in foeMoves) enemy.Moves.Add(new Move(MoveDatabase.Get(m)));
            var (name, x, y) = Place("Route201", -1, -1);
            game.Map = MapDatabase.Get(name);
            if (x >= 0) game.Player.SetPosition(x, y, Direction.Up);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = party, Inventory = inventory, Pokedex = pokedex, WildPokemon = new List<Pokemon> { enemy }, Random = chance,
                Conditions = new BattleConditions { Weather = sky }
            });
            game.BattleRenderer.SetArena(game.Map, x, y);
            game.Battle = b;
            game.State = GameState.Battle;
            return b;
        }

        // Reads on to a line, then lets what it starts play for a while
        void ReadTo(BattleEngine b, string line, double after)
        {
            for (int i = 0; i < 40 && b.CurrentMessage != line && !b.IsBattleOver; i++)
            {
                Confirm(b);
                Skip(0.3);
            }
            Skip(after);
            Console.WriteLine($"  line: {b.CurrentMessage}, weather {b.Anim.Weather}, doll {b.Anim[BattleSide.Player].Substitute?.Up.ToString() ?? "none"}");
        }

        BattleRandom Steady(uint seed) => new BattleRandom(seed).Force(RollKind.Critical, 1).Force(RollKind.Damage, 0).Force(RollKind.Accuracy, 0);

        var weathers = new (BattleWeather Sky, string Kind, string GoesOn)[]
        {
            (BattleWeather.Rain, "rain", "Rain continues to fall."),
            (BattleWeather.Sun, "sun", "The sunlight is strong."),
            (BattleWeather.Sandstorm, "sandstorm", "The sandstorm rages."),
            (BattleWeather.Hail, "hail", "Hail continues to fall."),
            (BattleWeather.Fog, "fog", "The fog is deep...")
        };
        var growler = new Pokemon(PokemonDatabase.Get("Empoleon")!, 40);
        growler.Moves.Clear();
        growler.Moves.Add(new Move(MoveDatabase.Get("Growl")));
        party.Members.Insert(0, growler);
        foreach (var (sky, kind, goesOn) in weathers)
        {
            party.HealAll();
            var wb = Wild("Bidoof", 12, new[] { "Growl" }, sky, Steady(25));
            ToMainMenu(wb);
            Skip(1.0);
            Shot($"96_weather_{kind}");
            wb.SelectMove(0);
            ReadTo(wb, goesOn, 0.5);
            Shot($"96_weather_{kind}_turn");
            game.State = GameState.Overworld;
        }
        party.Members.Remove(growler);

        // The doll: up, aside for its owner's Water Gun, and broken by a Machamp's Strength
        var blastoise = new Pokemon(PokemonDatabase.Get("Blastoise")!, 40);
        blastoise.Moves.Clear();
        foreach (var m in new[] { "Substitute", "Water Gun" }) blastoise.Moves.Add(new Move(MoveDatabase.Get(m)));
        party.Members.Insert(0, blastoise);
        var sb = Wild("Bidoof", 12, new[] { "Growl" }, BattleWeather.None, Steady(26));
        ToMainMenu(sb);
        sb.SelectMove(0);
        ReadTo(sb, "Blastoise made a substitute!", BattleAnimator.SubstituteInTime + 0.4);
        Shot("96_substitute_up");
        for (int i = 0; i < 40 && sb.HUD.MenuState != BattleMenuState.Main && !sb.IsBattleOver; i++)
        {
            Confirm(sb);
            Skip(0.3);
        }
        Skip(0.5);
        sb.SelectMove(1);
        // The line is up as the move is chosen (Blastoise is the faster). The doll is aside within a fifth of a second
        // of it, while the camera is still on its owner; a moment later the camera cuts to the foe
        // (BattleCamera.CutTime) and neither is in the picture, which is what a shot 0.3 s in showed (plan 25 · Y2
        // found it so). So it is taken 0.2 s in, and the mode's clock then brought to where it always stood
        Skip(0.2);
        Console.WriteLine($"  line: {sb.CurrentMessage}, weather {sb.Anim.Weather}, doll {sb.Anim[BattleSide.Player].Substitute?.Up.ToString() ?? "none"}, aside {SubstituteDoll.LookOf(sb.Anim[BattleSide.Player].Substitute).Aside:0.00}, camera {game.BattleRenderer.Shot}");
        Shot("96_substitute_aside");
        Skip(0.1);
        game.State = GameState.Overworld;

        party.HealAll();
        var fb = Wild("Machamp", 40, new[] { "Strength" }, BattleWeather.None, Steady(27));
        ToMainMenu(fb);
        fb.SelectMove(0);
        ReadTo(fb, "The substitute took damage for Blastoise!", BattleAnimator.SubstituteOutTime * 0.4);
        Shot("96_substitute_faded");
        party.Members.Remove(blastoise);
        game.State = GameState.Overworld;
    }

    // Trainers who speak in battle (plan 11 · C12): Roark with his own team, a Gible's Dragon Rage taking forty HP a
    // turn, at the three moments his lines are read (98_speaks_first_down, 98_speaks_last_pokemon, 98_speaks_low_hp),
    // and the last of them again with the option off (98_speaks_off), where nothing is said
    void SpeakingMode()
    {
        BattleEngine Against(bool lines)
        {
            var roark = new Trainer { Id = "leader_roark" };
            TrainerDatabase.Fill(roark, TrainerDatabase.Get("leader_roark")!);
            // His Potions would take the last one out of the red the turn it got there
            roark.Items.Clear();
            var gible = new Pokemon(PokemonDatabase.Get("Gible")!, 40);
            gible.Moves.Clear();
            gible.Moves.Add(new Move(MoveDatabase.Get("Dragon Rage")));
            var team = new Party();
            team.Add(gible);
            // On the Gym's own floor, for its stage
            At("OreburghGym", 5, 19, Direction.Up);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = team, Inventory = inventory, Pokedex = pokedex, Trainers = new List<Trainer> { roark },
                Random = new BattleRandom(1012), TrainerLines = lines
            });
            game.BattleRenderer.SetArena(game.Map, 5, 19);
            game.Battle = b;
            game.State = GameState.Battle;
            ToMainMenu(b);
            return b;
        }

        // Reads on until a trainer's line is on screen (or, with the option off, the line that would have come with it)
        // and lets it settle; then on to the next menu
        void ReadToSpeaker(BattleEngine b, string? orTo = null)
        {
            for (int i = 0; i < 40 && b.CurrentSpeaker == null && (orTo == null || b.CurrentMessage != orTo) && !b.IsBattleOver; i++)
            {
                Confirm(b);
                Skip(0.3);
            }
            Skip(0.8);
            Console.WriteLine($"  speaker: {b.CurrentSpeaker ?? "nobody"}, line: {b.CurrentMessage}");
        }
        void ToMenu(BattleEngine b)
        {
            for (int i = 0; i < 40 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver; i++)
            {
                Confirm(b);
                Skip(0.3);
            }
            Skip(0.5);
        }
        // Roark's last one, left forty HP above a fifth of its HP: the Dragon Rage leaves it in the red
        void IntoTheRed(BattleEngine b)
        {
            var last = b.EnemyPokemon;
            last.CurrentHP = Math.Min(last.MaxHP, 40 + Math.Max(1, last.MaxHP / 5));
            b.SelectMove(0);
        }

        var speaking = Against(lines: true);
        speaking.SelectMove(0);
        ReadToSpeaker(speaking);
        Shot("98_speaks_first_down");
        // The line read, Roark sends the next; the second down, his last comes out
        ToMenu(speaking);
        speaking.SelectMove(0);
        Skip(0.3);
        ReadToSpeaker(speaking);
        Shot("98_speaks_last_pokemon");
        ToMenu(speaking);
        IntoTheRed(speaking);
        ReadToSpeaker(speaking);
        Shot("98_speaks_low_hp");
        game.State = GameState.Overworld;

        var quiet = Against(lines: false);
        quiet.SelectMove(0);
        ToMenu(quiet);
        quiet.SelectMove(0);
        ToMenu(quiet);
        IntoTheRed(quiet);
        ReadToSpeaker(quiet, $"{quiet.PlayerPokemon.DisplayName} used Dragon Rage!");
        // The next line, where the option on would have had Roark's
        Confirm(quiet);
        Skip(0.8);
        Shot("98_speaks_off");
        game.State = GameState.Overworld;
    }
}
