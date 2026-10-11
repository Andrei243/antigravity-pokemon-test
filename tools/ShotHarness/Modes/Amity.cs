partial class Harness
{
    // ---------------------------------------------------------------- Amity Square's walk (plan 06 · R16c)

    // Hearthome's west gate and its receptionist, the square's lobby, the counter's check for a team with none of the
    // species and for one with two, a Pokémon let out, the walk with it through every turn, a step caught in its hop,
    // its mood, two hundred steps and what it turned up, a hut walked into and the hut it leads to, the east half:
    // the woman and her Happiny, the man of the park and his gift, the Spooky Plate; the goodbye, a starter grown up
    // asked about second and let out, every species of the square a step behind the player, and a game saved on a
    // walk taken up again (am*; not part of "all"). Everything goes through the game's own steps, scripts and
    // question boxes; it prints what each left behind (the story's variables, who walks, the bag and the Fashion Case).
    public void AmityMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var team = game.Party;
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        // Reads every line on to the end of the script, answering yes to whatever is asked
        void ReadOn()
        {
            for (int i = 0; i < 40 && (Box().IsActive || engine.Choice.IsOpen || State() == GameState.Dialogue); i++)
            {
                if (engine.Choice.IsOpen) { engine.Choice.Confirm(); Frames(4); continue; }
                Whole();
                Next();
            }
            Until(() => State() == GameState.Overworld && !Box().IsActive && !engine.ScriptRunning, "the field again");
        }
        // The player's own steps, a tile at a time: the key held until the step begins, and let go until it has ended
        // (or a script has the field)
        void Walk(Direction way, int tiles = 1)
        {
            for (int i = 0; i < tiles && !engine.ScriptRunning && State() == GameState.Overworld; i++)
            {
                engine.Steering = (way, false);
                for (int f = 0; f < 30 && !game.Player.IsMoving && !engine.ScriptRunning && State() == GameState.Overworld; f++) Frames(1);
                engine.Steering = null;
                for (int f = 0; f < 90 && game.Player.IsMoving; f++) Frames(1);
                Frames(1);
            }
        }
        // The Pokémon behind the player comes to rest
        void Settle() { Until(() => game.Along?.Moving != true, "the Pokémon to catch up", 240); Frames(12); }
        string Tell() => $"active {story.Var(AmitySquare.ActiveVar)}, place of the team {story.Var(AmitySquare.PartyVar)}, steps {AmitySquare.Steps(story)}, " +
            $"hidden {story.Has(AmitySquare.HideFlag)}, walking {game.Along?.Pokemon?.DisplayName ?? "nobody"}" +
            (game.Along is { } a ? $" at {a.Who.GridX},{a.Who.GridY}" : "") + $", player at {game.Player.GridX},{game.Player.GridY}";
        Pokemon Of(string species, int level = 30) => new(PokemonDatabase.Get(species)!, level);
        // Puts the player down in the square with whoever walks along behind them
        void Put(int x, int y, Direction facing)
        {
            At(AmitySquare.Map, x, y, facing);
            game.KeepPartnerAlong();
            game.LocationSign.Hide();
            Frames(12);
        }
        // Turns the player to whoever walks behind them, without a step
        void FaceTheWalker()
        {
            if (game.Along is not { } along) return;
            var way = ScriptWay((game.Player.GridX, game.Player.GridY), (along.Who.GridX, along.Who.GridY));
            // A tall one keeps two tiles back: a step toward it, onto the tile between, and it is within reach
            if (Math.Abs(along.Who.GridX - game.Player.GridX) + Math.Abs(along.Who.GridY - game.Player.GridY) > 1) Walk(way);
            game.Player.Facing = way;
            Frames(4);
        }

        // ---- A team with none of the square's species: the gate house, whose receptionist only talks
        team.Clear();
        team.Add(Of("Staraptor", 40));
        team.Add(Of("Bibarel"));
        At("HearthomeCityWestGateToAmitySquare", 5, 9, Direction.Up);
        Frames(8);
        Shot("am01_west_gate");
        At("HearthomeCityWestGateToAmitySquare", 5, 3, Direction.Right);
        Frames(4);
        game.Interact(); Frames(3);
        Whole(); Shot("am02_gate_receptionist");
        ReadOn();

        // Through the door in its back wall, by the game's own warp, into the lobby south of the counter
        At("HearthomeCityWestGateToAmitySquare", 5, 3, Direction.Up);
        Frames(4);
        Walk(Direction.Up);
        Until(() => game.Map.Name == AmitySquare.Map && State() == GameState.Overworld && !engine.ScriptRunning, "the square", 600);
        Frames(40);
        game.LocationSign.Hide();
        Frames(2);
        Shot("am03_lobby");
        var man = game.Map.Everyone.First(n => n.Key == "gift_man");
        Console.WriteLine($"arrived: {Tell()}; the man of the park at {man.GridX},{man.GridY} with gift {AmitySquare.Gifts[story.Var(AmitySquare.GiftVar)]}");

        // Up the lane to the tile before the counter: nobody of the team may walk here
        Walk(Direction.Right, 2);
        Walk(Direction.Up, 3);
        Shot("am04_the_lane");
        Walk(Direction.Up);
        Whole(); Shot("am05_none_may");
        Next(); Whole(); Shot("am06_the_species");
        ReadOn();
        Frames(20);
        Shot("am07_turned_back");
        Console.WriteLine($"turned back: {Tell()}");

        // ---- A team with two of them: asked about each in turn
        team.Clear();
        team.Add(Of("Staraptor", 40));
        team.Add(Of("Pikachu"));
        team.Add(Of("Torterra", 40));
        team.Members[0].CurrentHP = 10;
        Walk(Direction.Up);
        Until(() => engine.Choice.IsOpen, "the receptionist's question"); Frames(20);
        Shot("am10_asks_about_one");
        engine.Choice.Confirm(); Frames(4);
        Whole(); Frames(10); Shot("am11_let_out");
        Console.WriteLine($"let out: {Tell()}; the team's first at {team.Members[0].CurrentHP}/{team.Members[0].MaxHP} HP");
        ReadOn();
        Settle();
        Shot("am12_into_the_square");
        Console.WriteLine($"in the square: {Tell()}; next find kind {story.Var(AmitySquare.NextKindVar)}, " +
            $"accessory {SuperContestDatabase.AccessoryName(story.Var(AmitySquare.NextAccessoryVar))}, berry {AmitySquare.Berries[story.Var(AmitySquare.NextBerryVar)]}");

        // ---- The walk, through every turn: north, east, south and west, the Pokémon on the tile just left
        Walk(Direction.Up, 2); Settle();
        Shot("am13_north");
        Console.WriteLine($"north: {Tell()}");
        Walk(Direction.Right, 3); Settle();
        Shot("am14_east");
        Walk(Direction.Down, 2); Settle();
        Shot("am15_south");
        Walk(Direction.Left); Settle();
        Shot("am16_west");
        Console.WriteLine($"west: {Tell()}");
        // A step caught half way: the Pokémon off the ground in its hop, showing its second frame
        Walk(Direction.Up);
        engine.Steering = (Direction.Up, false);
        Frames(7);
        Shot("am17_mid_step");
        engine.Steering = null;
        Until(() => !game.Player.IsMoving, "the step's end", 120);
        Settle();
        // Walked back into, the two change places
        var before = (game.Player.GridX, game.Player.GridY);
        Walk(Direction.Down); Settle();
        Shot("am18_changed_places");
        Console.WriteLine($"walked back into: the player went from {before} and now {Tell()}");

        // ---- Spoken to: a mood
        FaceTheWalker();
        game.Interact(); Frames(3);
        Whole(); Shot("am20_mood");
        ReadOn();

        // ---- Two hundred steps on the lawn, and it has turned something up
        Put(14, 41, Direction.Right);
        for (int i = 0; i < 100; i++)
        {
            Walk(Direction.Right);
            Walk(Direction.Left);
        }
        Settle();
        Console.WriteLine($"after two hundred steps: {Tell()}");
        FaceTheWalker();
        game.Interact(); Frames(3);
        Whole(); Shot("am21_holding_something");
        Next(); Whole(); Shot("am22_found");
        ReadOn();
        Console.WriteLine($"found: {Tell()}; the bag's berries {string.Join(", ", AmitySquare.Berries.Select(b => (b, game.Bag.GetQuantity(ItemDatabase.Get(b)!))).Where(b => b.Item2 > 0).Select(b => $"{b.Item2} {b.b}"))}; " +
            $"the Fashion Case {string.Join(", ", game.FashionCase.Held.Select(h => $"{h.Count} {SuperContestDatabase.AccessoryName(h.Accessory)}"))}");

        // ---- The ruins: up to a hut, in at its door, and out of the hut it leads to
        var a = AmitySquare.Huts.First(h => h.Name == 'A');
        Put(a.X - 3, a.Door.Y + 1, Direction.Right);
        Walk(Direction.Right, 3); Settle();
        FaceUp();
        Shot("am30_before_a_hut");
        Walk(Direction.Up); Settle();
        Shot("am31_in_its_door");
        Walk(Direction.Up); Settle();
        Shot("am32_inside");
        Walk(Direction.Up);
        Until(() => !engine.ScriptRunning && State() == GameState.Overworld, "the hut's way", 600);
        Settle();
        Shot("am33_out_of_another");
        Console.WriteLine($"through hut A's north: {Tell()} (hut B's door is {AmitySquare.Huts.First(h => h.Name == 'B').Door})");
        Walk(Direction.Down, 2); Settle();
        Shot("am34_followed_out");
        Console.WriteLine($"two steps on: {Tell()}");

        // ---- The east half, which only the huts lead about: from the east hut's west side to the north one
        var c = AmitySquare.Huts.First(h => h.Name == 'C');
        Put(c.X, c.Door.Y + 1, Direction.Up);
        Shot("am40_the_east_hut");
        Walk(Direction.Up, 2);
        Walk(Direction.Left);
        Until(() => !engine.ScriptRunning && State() == GameState.Overworld, "the hut's way", 600);
        Settle();
        Console.WriteLine($"through hut C's west: {Tell()} (hut F's door is {AmitySquare.Huts.First(h => h.Name == 'F').Door})");
        // The woman and her Happiny on the north ground
        Walk(Direction.Down);
        Walk(Direction.Right, 3); Settle();
        game.Player.Facing = Direction.Down;
        Frames(6);
        Shot("am41_woman_and_happiny");
        game.Interact(); Frames(3);
        Whole(); Shot("am42_the_woman");
        ReadOn();
        Walk(Direction.Right); Settle();
        game.Player.Facing = Direction.Down;
        Frames(4);
        game.Interact(); Frames(3);
        Whole(); Shot("am43_the_happiny");
        ReadOn();

        // The man of the park, wherever the day has him, and his gift
        var beside = new[] { (0, 1, Direction.Up), (-1, 0, Direction.Right), (1, 0, Direction.Left), (0, -1, Direction.Down) }
            .Select(s => (X: man.GridX + s.Item1, Y: man.GridY + s.Item2, Way: s.Item3)).First(s => game.Map.IsWalkable(s.X, s.Y));
        At(AmitySquare.Map, beside.X, beside.Y, beside.Way);
        game.Along?.Behind(beside.X, beside.Y, beside.Way);
        game.LocationSign.Hide();
        Frames(12);
        Shot("am44_the_man_of_the_park");
        game.Interact(); Frames(3);
        Whole(); Next();
        Until(() => engine.Choice.IsOpen, "the man's question"); Frames(20);
        Shot("am45_his_gift");
        engine.Choice.Confirm(); Frames(4);
        Whole(); Shot("am46_given");
        ReadOn();
        Console.WriteLine($"the man's gift: {AmitySquare.Gifts[story.Var(AmitySquare.GiftVar)]}, given {story.Has(AmitySquare.GiftFlag)}; " +
            $"the bag's berries {string.Join(", ", AmitySquare.Berries.Select(b => (b, game.Bag.GetQuantity(ItemDatabase.Get(b)!))).Where(b => b.Item2 > 0).Select(b => $"{b.Item2} {b.b}"))}; " +
            $"the Fashion Case {string.Join(", ", game.FashionCase.Held.Select(h => $"{h.Count} {SuperContestDatabase.AccessoryName(h.Accessory)}"))}");

        // The Spooky Plate in its hollow, behind the south-east hut
        var plate = game.Map.Everyone.FirstOrDefault(n => n.Key == "item_spooky_plate");
        if (plate != null && game.Map.NPCs.Contains(plate))
        {
            Put(plate.GridX + 1, plate.GridY, Direction.Left);
            Shot("am47_spooky_plate");
            game.Interact(); Frames(3);
            ReadOn();
            Console.WriteLine($"the plate: {game.Bag.GetQuantity(ItemDatabase.Get("Spooky Plate")!)} in the bag");
        }

        // ---- Out by the west lane: the goodbye, and the Pokémon back in its ball
        Put(12, 44, Direction.Down);
        Walk(Direction.Down, 3);
        Whole(); Shot("am50_goodbye");
        ReadOn();
        Frames(20);
        Shot("am51_alone_again");
        Console.WriteLine($"left: {Tell()}");

        // ---- In again: no to the first, yes to the starter grown up, which stands taller than the player's waist
        Walk(Direction.Up, 2);
        Until(() => engine.Choice.IsOpen, "the receptionist's question"); Frames(10);
        engine.Choice.Back(); Frames(4);
        Until(() => engine.Choice.IsOpen, "her second question"); Frames(20);
        Shot("am52_asks_about_another");
        engine.Choice.Confirm(); Frames(4);
        Whole(); Frames(10);
        ReadOn();
        Settle();
        Console.WriteLine($"in again: {Tell()}");
        Walk(Direction.Up, 2); Settle();
        Shot("am53_a_big_one_north");
        Walk(Direction.Right, 3); Settle();
        Shot("am54_a_big_one_east");
        Walk(Direction.Down, 2); Settle();
        Shot("am55_a_big_one_south");
        Walk(Direction.Left); Settle();
        Shot("am56_a_big_one_west");
        // Two steps in a line, so that it stands straight behind: a step back toward it is onto the tile between
        Walk(Direction.Up, 2); Settle();
        FaceTheWalker();
        game.Interact(); Frames(3);
        Whole(); Shot("am57_its_mood");
        ReadOn();

        // ---- Every species the square lets in, a step behind the player on the lawn: facing east, then walking north
        var body = game.Map.Everyone.First(n => n.Key == AmitySquare.Body);
        foreach (string species in AmitySquare.Permitted.Concat(AmitySquare.WithNationalDex))
        {
            team.Members[1] = Of(species);
            game.WalkWith(body, team.Members[1]);
            Put(15, 42, Direction.Right);
            Until(() => !engine.ScriptRunning, "the model", 600);
            Walk(Direction.Right, 2); Settle();
            ShotCrop($"am6a_{species.ToLowerInvariant()}_east", 560, 330, 800, 420, 2);
            Walk(Direction.Up); Settle();
            ShotCrop($"am6b_{species.ToLowerInvariant()}_north", 560, 280, 800, 420, 2);
        }
        story.SetVar(AmitySquare.PartyVar, 1);

        // ---- A game saved on a walk is taken up again with the Pokémon out
        Put(20, 41, Direction.Right);
        Console.WriteLine($"saving: {Tell()}");
        bool saved = game.Save();
        engine.ContinueGame();
        Until(() => State() == GameState.Overworld && !engine.ScriptRunning, "the game taken up again", 900);
        Frames(90);
        game.LocationSign.Hide();
        Frames(2);
        Shot("am70_taken_up_again");
        Console.WriteLine($"taken up again (saved {saved}): {Tell()}; it is a {game.Along?.Who.Species} called {game.Along?.Who.Name}");
        Walk(Direction.Right, 2); Settle();
        Shot("am71_and_walks_on");
        Console.WriteLine($"and walks on: {Tell()}");
        File.Delete("savegame.json");

        void FaceUp() { game.Player.Facing = Direction.Up; Frames(4); }
        static Direction ScriptWay((int X, int Y) from, (int X, int Y) to) =>
            to.Y < from.Y ? Direction.Up : to.Y > from.Y ? Direction.Down : to.X < from.X ? Direction.Left : Direction.Right;
    }
}
