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

        SpeakingMode();
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
