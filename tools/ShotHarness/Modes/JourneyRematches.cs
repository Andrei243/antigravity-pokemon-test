partial class Harness
{
    /// <summary>
    /// The Leaders' rematches where a region had none (plan 41 · EG5; <c>JOURNEY_FROM=rematches</c>): Brock in his Gym with
    /// Kanto's Hall of Fame entered (<c>eg50</c>: his offer, the battle's menu against the team of ours), Cheren in
    /// Aspertia's Gym after Unova's (<c>eg51</c>, Black 2's second team) and Will in his room after Johto's
    /// (<c>eg52</c>, HeartGold's second round). Each is reached through the game's own <c>Interact</c>, scripts and
    /// question box, and prints the foe's team as the battle has it.
    /// </summary>
    private void JourneyRematches()
    {
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Strong()
        {
            game.Party.Members.Clear();
            foreach (string species in new[] { "Garchomp", "Lucario", "Gyarados" })
                game.Party.Members.Add(new Pokemon(PokemonDatabase.Get(species)!, 90));
        }
        // Reads on to a line of the script, then (when asked) to the battle and its first menu
        void ReadTo(string text, int most = 1800)
        {
            for (int i = 0; i < most; i++)
            {
                if (Box().IsActive && Translated.Saying(Box().CurrentLine, text)) { Box().FinishLine(); Frames(2); return; }
                if (engine.Choice.IsOpen) { Frames(10); return; }
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text}");
        }
        void IntoBattle(string what)
        {
            for (int i = 0; i < 1800 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            if (State() != GameState.Battle) { Console.WriteLine($"  !! never happened: {what}"); return; }
            Skip(1.0);
        }
        void TalkTo(string map, Func<NPC, bool> who, int dx = 0, int dy = 1)
        {
            var npc = MapDatabase.Get(map).NPCs.First(who);
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        void Foe(string what)
        {
            var foes = game.Battle.EnemyParty?.Members ?? new List<Pokemon>();
            Console.WriteLine($"  {what}: {string.Join(", ", foes.Select(p => $"{p.Species.Name} {p.Level}"))}; daily flags {string.Join(", ", game.Story.Flags.Where(f => f.StartsWith("FLAG_DAILY_", StringComparison.Ordinal) && f.EndsWith("_REMATCH", StringComparison.Ordinal)))}");
        }
        void Begin(string region)
        {
            engine.NewGameRegion = region;
            engine.StartNewGame();
            game.State = GameState.Overworld;
            game.SaveSlot = 1;
            game.LocationSign.Hide();
            Frames(30);
            Strong();
            game.Story.SetBadges(region, 0xFF);
            game.Story.Set(region + "HallOfFame");
        }

        // ---- Brock, once Kanto's Hall of Fame is entered
        Begin("Kanto");
        game.Story.Set("FLAG_GOT_TM39_FROM_BROCK");
        TalkTo("PewterCity_Gym", n => n.Key == "brock");
        ReadTo("Care to see what that came to"); Frames(10);
        IntoBattle("Brock's rematch");
        ToMainMenu(game.Battle); Shot("eg50_brock_rematch");
        Foe("Brock");

        // ---- Cheren, once Unova's is
        Begin("Unova");
        game.Story.Set("FLAG_UNOVA_GOT_TM83");
        TalkTo("AspertiaCity_Gym", n => n.Key == "0" && n.ScriptFile == "aspertia_city_gym");
        ReadTo("Shall we measure it"); Frames(10);
        IntoBattle("Cheren's rematch");
        ToMainMenu(game.Battle); Shot("eg51_cheren_rematch");
        Foe("Cheren");

        // ---- Will, in the League's first room, once Johto's is
        Begin("Johto");
        TalkTo("IndigoLeague_WillRoom", n => n.Key == "t10r0201_gsbigfour1");
        ReadTo("I will not hold anything in reserve"); Frames(10);
        IntoBattle("Will's second round");
        ToMainMenu(game.Battle); Shot("eg52_will_second_round");
        Foe("Will");

        engine.NewGameRegion = "Sinnoh";
    }
}
