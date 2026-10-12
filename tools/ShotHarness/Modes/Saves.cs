partial class Harness
{
    // ---------------------------------------------------------------- saves of today (plan 24 · X3)

    // Saves as today's game writes them, for the test project's corpus of every version (PokemonPlatinumTests/Fixtures/
    // saves, SaveCorpusTests): a new game in the player's room once the television has had its say, a game past the
    // first chapter in front of the house, surfing, on a bridge's deck, a Safari Game under way, the girl, and a game
    // under the modern rules. Each is the engine's own BuildSave written as SaveManager writes savegame.json, into
    // <out dir>/saves/ as w<world>-s<story>-<moment>.json. No shots. Not part of "all". A session that raises
    // SaveData.CurrentWorld or StoryState.CurrentVersion runs it and copies the files into the corpus.
    public void SavesMode()
    {
        string folder = Path.Combine(outDir, "saves");
        Directory.CreateDirectory(folder);
        string prefix = $"w{SaveData.CurrentWorld}-s{StoryState.CurrentVersion}-";
        var sinnoh = MapDatabase.Get("Sinnoh");

        void Write(string moment)
        {
            Frames(2);
            var save = game.BuildSave();
            string name = prefix + moment + ".json";
            File.WriteAllText(Path.Combine(folder, name), SaveManager.Write(save));
            string height = save.PlayerHeight is { } h ? $" at height {h:0.##}" : "";
            string safari = save.Safari is { } s ? $", a Safari Game with {s.Balls} balls and {s.Steps} steps left" : "";
            Console.WriteLine($"{name}: {save.CurrentMapName} {save.PlayerGridX},{save.PlayerGridY} facing {save.PlayerFacing}, {save.Travel}{height}{safari}; " +
                $"{save.Rules} rules, {save.Look} {save.PlayerName}, team {string.Join(", ", save.Party.Select(p => $"{p.SpeciesName} {p.Level}"))}, " +
                $"{save.StoryFlags.Count} flags, {save.StoryVariables.Count} variables, started {save.Started:yyyy-MM-dd}");
        }
        void Begin(PlayerLook look = PlayerLook.Boy, RulesPreset rules = RulesPreset.Platinum)
        {
            engine.StartNewGame(null, look, rules);
            game.State = GameState.Overworld;
            game.LocationSign.Hide();
        }
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // Reads a script on to its end, saying yes to every question
        void ToTheEnd()
        {
            var box = game.Dialogue;
            for (int i = 0; i < 2400; i++)
            {
                if (!engine.ScriptRunning && !box.IsActive) return;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine("  !! the script never ended");
        }
        (int X, int Y)? Nearest(Map map, int x, int y, Func<int, int, bool> wanted, int reach = 40)
        {
            for (int r = 0; r < reach; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r && map.InBounds(x + dx, y + dy) && wanted(x + dx, y + dy)) return (x + dx, y + dy);
            return null;
        }

        // ---- a new game, in the player's room once the television has had its say (as `opening` begins it)
        Begin();
        Until(() => game.Dialogue.IsActive, "the television");
        ToTheEnd();
        Frames(30);
        Write("new-game-in-the-bedroom");

        // ---- past the first chapter, in front of the house (as every other mode begins)
        Begin();
        PastTheOpening();
        Write("past-the-opening");

        // ---- out on Twinleaf Town's pond, on the back of a Pokémon that knows Surf
        var swimmer = new Pokemon(PokemonDatabase.Get("Bibarel")!, 30);
        while (swimmer.Moves.Count >= 4) swimmer.Moves.RemoveAt(swimmer.Moves.Count - 1);
        swimmer.TryLearn("Surf");
        game.Party.Add(swimmer);
        if (Nearest(sinnoh, 111, 892, (x, y) => sinnoh.IsDeepWater(x, y) && !sinnoh.IsSolid(x, y)) is { } pond)
        {
            game.Place(sinnoh, pond.X, pond.Y, Direction.Right);
            game.Player.SetMode(TravelMode.Surfing);
            Write("surfing");
        }
        else Console.WriteLine("  !! no water near Twinleaf Town");

        // ---- on the deck of the bridge nearest Canalave City (its own is drawn at the ground's height, so it may be another)
        if (Nearest(sinnoh, 47, 725, (x, y) => sinnoh.DeckAt(x, y) != null && !sinnoh.IsSolid(x, y), reach: 960) is { } bridge)
        {
            game.Place(sinnoh, bridge.X, bridge.Y, Direction.Left);
            game.Player.SetMode(TravelMode.OnFoot);
            game.Player.SetHeight(sinnoh.DeckAt(bridge.X, bridge.Y)!.Value);
            Write("on-a-bridge");
        }
        else Console.WriteLine("  !! no bridge in Sinnoh");

        // ---- in the Great Marsh with a Safari Game under way: a few steps taken and a few balls thrown
        var (marsh, mx, my) = AreaSpot("great_marsh_1");
        game.Place(marsh, mx, my, Direction.Up);
        game.Safari.Start();
        for (int i = 0; i < 25; i++) game.Safari.Step();
        game.Safari.Balls -= 3;
        Write("safari-game");

        // ---- the girl, past the first chapter
        Begin(PlayerLook.Girl);
        PastTheOpening();
        Write("the-girl");

        // ---- a game played by the modern rules, past the first chapter
        Begin(rules: RulesPreset.Modern);
        PastTheOpening();
        Write("modern-rules");

        // Platinum's rules again, as every mode expects
        Begin();
        PastTheOpening();
        Console.WriteLine($"{Directory.GetFiles(folder, prefix + "*.json").Length} saves in {folder}");
    }
}
