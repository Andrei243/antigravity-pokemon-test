partial class Harness
{
    // ---------------------------------------------------------------- field moves (plan 02 · S2)

    // The field moves at work (fm*): a tree cut down, a boulder pushed, a dark cave lit from the party menu, the party
    // menu's word when a move can't be used, Fly's map and a flight, Surf's question, the Cycling Road's gate turning
    // someone on foot back, the Pokétch's apps, and a rod's cast from the bite to the battle. Not part of "all".
    public void FieldMovesMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var team = game.Party;
        var bag = game.Bag;
        var me = game.Player;
        var partyMenu = game.PartyScreen;

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        void ReadOn(int most = 12)
        {
            for (int i = 0; i < most && Box().IsActive && !Box().IsQuestion; i++) { Whole(); Next(); }
        }
        void Talk() { game.Interact(); Frames(3); }
        (int X, int Y, Direction Facing) Beside(Map map, int x, int y)
        {
            foreach (var (dx, dy, facing) in new[] { (-1, 0, Direction.Right), (1, 0, Direction.Left), (0, 1, Direction.Up), (0, -1, Direction.Down) })
                if (map.IsWalkable(x + dx, y + dy) && map.GetNpcAt(x + dx, y + dy) == null) return (x + dx, y + dy, facing);
            throw new InvalidOperationException($"Nobody can stand beside {x},{y} of {map.Name}");
        }
        Pokemon Knowing(string species, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, 40);
            p.Moves.Clear();
            foreach (var m in moves) p.Moves.Add(new Move(MoveDatabase.Get(m)!));
            return p;
        }

        // A team that knows every field move between them, and every badge
        team.Clear();
        team.Add(Knowing("Bibarel", "Cut", "Rock Smash", "Strength", "Surf"));
        team.Add(Knowing("Staraptor", "Fly", "Defog", "Quick Attack"));
        team.Add(Knowing("Golduck", "Waterfall", "Flash", "Rock Climb", "Dig"));
        team.Add(Knowing("Chansey", "Soft-Boiled", "Sweet Scent", "Teleport"));
        team.Members[3].CurrentHP = team.Members[3].MaxHP;
        team.Members[1].CurrentHP = team.Members[1].MaxHP / 3;
        story.SetBadges(0xFF);

        // ---- Eterna City's first tree: the question, the line, the cut-in, the tree gone in a burst of leaves
        var sinnoh = MapDatabase.Get("Sinnoh");
        var tree = sinnoh.Everyone.First(n => n.Obstacle == PropType.CutTree && (n.GridX, n.GridY) == (304, 521));
        var (sx, sy, sf) = Beside(sinnoh, tree.GridX, tree.GridY);
        At("Sinnoh", sx, sy, sf);
        Frames(10); Shot("fm01_cut_tree");
        Talk(); Whole(); Until(() => engine.Choice.IsOpen, "the tree's question"); Frames(20); Shot("fm02_cut_asks");
        engine.Choice.Confirm(); Frames(4);
        Whole(); Shot("fm03_cut_used");
        Next(); Frames(26); Shot("fm04_cut_in");
        Until(() => !engine.ScriptRunning, "the tree's script to end"); Frames(4); Shot("fm05_tree_falls");
        Frames(30); Shot("fm06_tree_gone");
        Console.WriteLine($"cut: tree gone {!sinnoh.NPCs.Contains(tree)}, its flag {story.Has(tree.HiddenBy!)}");

        // ---- a boulder in Oreburgh Gate: Strength, and the boulder pushed a tile on
        var gate = MapDatabase.Get("OreburghGateB1F");
        NPC boulder = null;
        Direction push = Direction.Up;
        foreach (var b in gate.Everyone.Where(n => n.Obstacle == PropType.StrengthBoulder))
            foreach (var d in new[] { Direction.Left, Direction.Right, Direction.Up, Direction.Down })
            {
                var (dx, dy) = FieldMovement.Delta(d);
                if (boulder == null && gate.IsWalkable(b.GridX - dx, b.GridY - dy) && gate.GetNpcAt(b.GridX - dx, b.GridY - dy) == null && FieldMovement.CanPush(gate, b, d))
                    (boulder, push) = (b, d);
            }
        if (boulder != null)
        {
            var (pdx, pdy) = FieldMovement.Delta(push);
            At("OreburghGateB1F", boulder.GridX - pdx, boulder.GridY - pdy, push);
            Talk(); Whole(); Until(() => engine.Choice.IsOpen, "the boulder's question"); Frames(20); Shot("fm07_strength_asks");
            engine.Choice.Confirm(); Frames(4);
            for (int i = 0; i < 4 && engine.ScriptRunning; i++)
            {
                Until(() => Box().IsActive || !engine.ScriptRunning, "Strength's lines", 200);
                ReadOn();
            }
            Until(() => !engine.ScriptRunning, "Strength's script to end");
            int was = boulder.GridX * 1000 + boulder.GridY;
            engine.Steering = (push, false);
            Frames(12); Shot("fm08_boulder_sliding");
            engine.Steering = null;
            Frames(40); Shot("fm09_boulder_pushed");
            Console.WriteLine($"strength: in force {story.Has(FieldMoveRules.StrengthFlag)}, the boulder moved {boulder.GridX * 1000 + boulder.GridY != was}");
        }
        else Console.WriteLine("  !! no boulder to push in Oreburgh Gate");

        // ---- Wayward Cave in the dark, lit by Flash chosen in the party menu
        var cave = MapDatabase.Get("WaywardCave1F");
        var mouth = cave.Warps[0];
        At("WaywardCave1F", mouth.SourceX, mouth.SourceY, Direction.Up);
        Frames(10); Shot("fm10_cave_dark");
        game.ChooseFromStartMenu(StartMenuChoice.Pokemon);
        Frames(30);
        partyMenu.SelectedIndex = 2;
        partyMenu.Confirm(team); Frames(10);
        partyMenu.MoveAction(2); Frames(4); Shot("fm11_party_menu");
        partyMenu.Confirm(team);
        Frames(2);
        Until(() => Box().IsActive, "Flash's line");
        ReadOn();
        Until(() => !engine.ScriptRunning, "Flash's script to end");
        Frames(20); Shot("fm12_cave_lit");
        Console.WriteLine($"flash: lit {story.Has(FieldMoveRules.FlashFlag)}");

        // ---- In Twinleaf Town Cut has nothing to cut: the party menu says so
        At("Sinnoh", 116, 888, Direction.Down);
        game.ChooseFromStartMenu(StartMenuChoice.Pokemon);
        Frames(30);
        partyMenu.SelectedIndex = 0;
        partyMenu.Confirm(team);
        partyMenu.MoveAction(1);
        partyMenu.Confirm(team);
        Frames(4); Shot("fm13_party_cannot");
        partyMenu.Close(); Frames(4);

        // ---- Fly: the towns arrived in, on the map; and the flight to one
        foreach (var key in new[] { "twinleaf_town", "sandgem_town", "jubilife_city", "oreburgh_city", "floaroma_town", "eterna_city", "hearthome_city", "solaceon_town" })
            story.Set(SpawnLocations.ArrivedIn(key)!.ArrivalFlag);
        At("Sinnoh", 116, 888, Direction.Down);
        game.ChooseFromStartMenu(StartMenuChoice.Pokemon);
        Frames(30);
        partyMenu.SelectedIndex = 1;
        partyMenu.Confirm(team);
        partyMenu.MoveAction(1);
        partyMenu.Confirm(team);
        Frames(20);
        var fly = game.FlyScreen;
        for (int i = 0; i < 9 && fly.Towns[fly.Cursor].Area != "hearthome_city"; i++) fly.Move(1);
        Frames(10); Shot("fm14_fly_map");
        fly.Confirm(); Frames(2);
        Whole(); Next(); Frames(26); Shot("fm15_fly_cut_in");
        Until(() => !engine.ScriptRunning && State() == GameState.Overworld, "the flight", 600);
        Frames(20); Shot("fm16_flown");
        Console.WriteLine($"fly: at {me.GridX},{me.GridY} of {game.Map.Name} (465,698 is Hearthome's Pokémon Center)");

        // ---- the Cycling Road's gate turns back someone on foot
        At("Sinnoh", 304, 567, Direction.Down);
        engine.Steering = (Direction.Down, false);
        Until(() => Box().IsActive, "the gate keeper", 300);
        engine.Steering = null;
        Whole(); Shot("fm17_gate_refuses");
        ReadOn();
        Until(() => !engine.ScriptRunning, "the gate keeper's script to end");
        Frames(10);
        Console.WriteLine($"gate: turned back to {me.GridX},{me.GridY} on {me.Mode}");

        // ---- Surf's question at the lake's edge
        var lake = MapDatabase.Get("LakeVerity");
        (int X, int Y)? shore = null;
        for (int y = 0; y < lake.Height && shore == null; y++)
            for (int x = 0; x < lake.Width && shore == null; x++)
                if (lake.IsWalkable(x, y) && FieldMovement.CanStartSurf(lake, x, y, Direction.Up, new Walker(TravelMode.OnFoot, lake.HeightAt(x, y)))) shore = (x, y);
        if (shore is var (wx, wy))
        {
            At("LakeVerity", wx, wy, Direction.Up);
            Talk(); Whole(); Until(() => engine.Choice.IsOpen, "Surf's question"); Frames(20); Shot("fm18_surf_asks");
            engine.Choice.Confirm(); Frames(4);
            Until(() => Box().IsActive, "Surf's line");
            ReadOn();
            Until(() => !engine.ScriptRunning, "Surf's script to end", 600);
            Frames(30); Shot("fm19_surfing");
            Console.WriteLine($"surf: {me.Mode} at {me.GridX},{me.GridY}");
        }

        // ---- the Pokétch: the watch, the pedometer, the team
        var poketch = game.Poketch;
        var watch = game.PoketchView;
        poketch.Enabled = true;
        foreach (var app in new[] { PoketchApp.DigitalWatch, PoketchApp.Calculator, PoketchApp.Pedometer, PoketchApp.PartyStatus }) poketch.Register(app);
        for (int i = 0; i < 1234; i++) poketch.Step();
        At("Sinnoh", 116, 888, Direction.Down);
        watch.Toggle(poketch); Frames(20); Shot("fm20_poketch_watch");
        watch.NextApp(poketch); Frames(4); Shot("fm21_poketch_pedometer");
        watch.NextApp(poketch); Frames(4); Shot("fm22_poketch_party");
        watch.Toggle(poketch); Frames(20);

        // ---- a rod's cast at the lake: the bite, the catch, the battle
        if (shore is var (fx, fy))
        {
            At("LakeVerity", fx, fy, Direction.Up);
            game.Fishing = new FishingAttempt(FishingRod.Good, new WildEncounterEntry { SpeciesName = "Magikarp", MinLevel = 12, MaxLevel = 12 }, new Random(3));
            Frames(40); Shot("fm23_fishing_waiting");
            Until(() => engine.CastStage == FishingStage.Hooked, "the bite", 400);
            Frames(4); Shot("fm24_fish_bites");
            engine.FishingPress = true; Frames(4);
            Whole(); Shot("fm25_fish_landed");
            Next();
            Until(() => State() == GameState.Battle, "the fish's battle", 600);
            var fight = game.Battle;
            ToMainMenu(fight);
            Shot("fm26_fish_battle");
        }
    }
}
