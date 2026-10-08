partial class Harness
{
    // ---------------------------------------------------------------- conditions and the field (plan 06 · R3)

    // conditions: what a battle's passing states look like. A Pokémon that has flown up is off its platform while its
    // HP box stays (96_flight_away), and is back as it strikes (96_flight_strike, 96_flight_back). Not part of "all"
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
    }
}
