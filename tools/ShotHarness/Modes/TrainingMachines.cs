partial class Harness
{
    // The training mode's machines part (TRAINING_PART=machines, plan 36 · TR9): the later games' machines and tutors at
    // Sunyshore's hall, a Champion's. The clerk's second shelf, the machines of ours from TM93 on, with the cursor on TM249,
    // Dragon Cheer (tr80), bought for money; from the bag, the team's cards saying who is able to learn it with a Dipplin of
    // level 41 first (tr81) and the move learned (tr81b); a Rare Candy's level then evolving it into Hydrapple, which
    // waits on Dragon Cheer (tr82, the scene's congratulations, Fickle Beam taken in Astonish's place); and last the
    // hall's tutor, behind the counter between the head coach and the clerk, offering the Turtwig of the team the later
    // tutors' moves with their Shards (tr83). Run alone, it comes into the hall with the Hall of Fame, the money, the
    // Dipplin and the Rare Candy it needs.
    void TrainingMachines()
    {
        game.State = GameState.Overworld;
        game.Story.Set(RegionDatabase.Get(RegionDatabase.Sinnoh)!.StoryCompleteFlag);
        if (game.Map.Name != GameEngine.SunyshoreTrainingHall) MachinesEnter(GameEngine.SunyshoreTrainingHall, 7, 9, Direction.Up);
        var cheer = ItemDatabase.GetAll().Single(i => MoveTeaching.IsLater(i) && i.TeachesMove == "Dragon Cheer");
        var context = new EvolutionContext { Party = party, Bag = inventory };

        // A Dipplin of level 41 at the head of the team, three moves known, its next level bringing none
        var dipplin = new Pokemon(PokemonDatabase.Get("Dipplin")!, 41, Gender.Female, Nature.Modest, false);
        dipplin.Moves.Clear();
        foreach (string move in new[] { "Dragon Breath", "Astonish", "Recycle" }) dipplin.Moves.Add(MoveDatabase.Create(move));
        dipplin.ReckonStats();
        dipplin.CurrentHP = dipplin.MaxHP;
        var rest = party.Members.ToList();
        party.Clear();
        party.Add(dipplin);
        foreach (var p in rest.Take(5)) party.Add(p);
        pokedex.RegisterSeen(dipplin.Species.DexNumber);
        pokedex.RegisterCaught(dipplin.Species.DexNumber);
        game.Money = Math.Max(game.Money, 30000);
        int moneyBefore = game.Money;

        // The clerk's second shelf: MACHINES, BUY, the cursor run down to TM249, then one bought
        var clerk = game.Map.Everyone.Single(n => n.Script == "TrainingClerk");
        machinesAnswers.Clear();
        machinesAnswers.Enqueue(1);
        MachinesTalk(clerk.GridX, clerk.GridY + 2, Direction.Up);
        MachinesReadOn(() => game.State == GameState.Shop);
        var shop = game.Shop;
        Frames(20);
        shop.Confirm(inventory, game.Money, engine.ShowNotification);
        var shelf = MartDatabase.Stock("training_hall_machines", 0).ToList();
        int at = shelf.FindIndex(i => i.Name == cheer.Name);
        for (int k = 0; k < at; k++) shop.Move(0, 1, game.Money);
        Frames(20);
        Shot("tr80_later_machine_stall");
        Console.WriteLine($"  the machine shelf: {shelf.Count} machines, {shelf[0].Name} ({shelf[0].TeachesMove}) to {shelf[^1].Name} ({shelf[^1].TeachesMove}); "
            + $"the cursor on {shop.Selected?.Name} ({shop.Selected?.TeachesMove}) at {shop.Selected?.Price}");
        shop.Confirm(inventory, game.Money, engine.ShowNotification);
        game.Money += shop.Confirm(inventory, game.Money, engine.ShowNotification);
        Frames(10);
        shop.Cancel();
        shop.Cancel();
        MachinesReadOn();
        Console.WriteLine($"  bought: {inventory.GetQuantity(cheer)} {cheer.Name}, money {moneyBefore} -> {game.Money}");

        // From the bag: the team's cards as the machine is chosen, then taught to the Dipplin
        var bag = game.BagScreen;
        game.State = GameState.BagMenu;
        bag.Open();
        bag.BeginTargetChoice(cheer);
        Frames(30);
        Shot("tr81_tm_teach");
        Console.WriteLine($"  who can learn it: {string.Join(", ", party.Members.Select(p => $"{p.Species.Name} {bag.TagFor(p, context)}"))}");
        bag.UseOnTarget(inventory, party, engine.ShowNotification, context);
        Frames(30);
        Shot("tr81b_tm_learned");
        Console.WriteLine($"  taught: {dipplin.Species.Name} knows {string.Join(", ", dipplin.Moves.Select(m => m.Name))}; {cheer.Name} left {inventory.GetQuantity(cheer)}");

        // A Rare Candy's level: the panel's two presses, and the game takes the evolution the level brings
        var candy = ItemDatabase.Get("Rare Candy")!;
        inventory.AddItem(candy, 1);
        bag.Open();
        bag.BeginTargetChoice(candy);
        Frames(20);
        bag.UseOnTarget(inventory, party, engine.ShowNotification, context);
        for (int press = 0; press < 2 && bag.LevelUp != null; press++)
        {
            Frames(10);
            bag.ConfirmLevelUp(engine.ShowNotification);
        }
        var scene = game.Evolution;
        for (int i = 0; i < 1500 && !(game.State == GameState.Evolution && scene.Phase == EvolutionPhase.Congratulate); i++) Frames(1);
        Frames(60);
        Shot("tr82_hydrapple_evolution");
        Console.WriteLine($"  the evolution: state {game.State}, phase {scene.Phase}; {dipplin.Species.Name} Lv{dipplin.Level}");
        // Hydrapple learns Fickle Beam as it evolves: Astonish goes for it
        for (int i = 0; i < 40 && scene.IsActive; i++)
        {
            if (scene.Phase == EvolutionPhase.MoveChoice)
            {
                int astonish = dipplin.Moves.FindIndex(m => m.Name == "Astonish");
                while (scene.Cursor < astonish) scene.MoveCursor(1);
                while (scene.Cursor > astonish) scene.MoveCursor(-1);
            }
            scene.PressConfirm();
            Frames(8);
        }
        Frames(70);
        Console.WriteLine($"  after: {dipplin.Species.Name} Lv{dipplin.Level} knows {string.Join(", ", dipplin.Moves.Select(m => m.Name))}; state {game.State}");
        bag.Close();
        game.State = GameState.Overworld;
        Frames(10);

        // The hall's tutor: yes, the Turtwig of the team, his moves for it with their Shards, the first taught for them
        var tutor = game.Map.Everyone.Single(n => n.Script == "TrainingTutor");
        int turtwig = party.Members.ToList().FindIndex(p => p.Species.Name == "Turtwig");
        if (turtwig < 0)
        {
            party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 30, Gender.Male, Nature.Hardy, false));
            turtwig = party.Count - 1;
        }
        var pupil = party.Members[turtwig];
        var offered = MoveTutors.Teachable(pupil, MoveTutors.TrainingHall);
        foreach (var (shard, count) in new[] { ("Red Shard", offered[0].Red), ("Blue Shard", offered[0].Blue), ("Yellow Shard", offered[0].Yellow), ("Green Shard", offered[0].Green) })
            if (count > 0) inventory.AddItem(ItemDatabase.Get(shard)!, count);
        machinesAnswers.Clear();
        machinesAnswers.Enqueue(0);
        machinesAnswers.Enqueue(0);
        machinesChoice = turtwig;
        MachinesTalk(tutor.GridX, tutor.GridY + 2, Direction.Up);
        MachinesQuestion(pupil.DisplayName, "tr83_hall_tutor");
        Console.WriteLine($"  the tutor offers {pupil.Species.Name}: {string.Join(", ", offered.Select(m => $"{m.Move} ({m.Price})"))}");
        MachinesReadOn();
        Console.WriteLine($"  taught by the tutor: {pupil.Species.Name} knows {string.Join(", ", pupil.Moves.Select(m => m.Name))}; "
            + $"Red {inventory.GetQuantity(ItemDatabase.Get("Red Shard")!)}, Green {inventory.GetQuantity(ItemDatabase.Get("Green Shard")!)} Shards left");
    }

    // What the machines part's next questions are answered with (the first answer once it has run out), and the team
    // member its next team screen chooses
    readonly Queue<int> machinesAnswers = new();
    int machinesChoice;

    // Reads on until the script has ended, or, given a test, until it holds: text read, questions answered, the team
    // screen's choice made
    void MachinesReadOn(Func<bool>? until = null, int most = 3000)
    {
        for (int i = 0; i < most; i++)
        {
            if (until != null && until()) return;
            if (until == null && !engine.ScriptRunning && !game.Dialogue.IsActive && game.State == GameState.Overworld) return;
            if (game.State == GameState.PartyMenu && game.PartyScreen.Choosing)
            {
                Frames(30);
                game.PartyScreen.SelectedIndex = machinesChoice;
                Frames(2);
                game.PartyScreen.Confirm(party);
                Frames(4);
                continue;
            }
            if (engine.Choice.IsOpen)
            {
                Frames(12);
                int answer = machinesAnswers.Count > 0 ? machinesAnswers.Dequeue() : 0;
                for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                Frames(2);
                engine.Choice.Confirm();
                Frames(4);
            }
            else if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) { game.Dialogue.Advance(); Frames(2); }
            else Frames(1);
        }
        Console.WriteLine("  !! the script never got there");
    }

    bool MachinesSay(string word) => game.Dialogue.IsActive && Translated.Saying(game.Dialogue.CurrentLine, word);

    // A question's line with its answers beside it, the cursor on the answer about to be given, shot
    void MachinesQuestion(string word, string shot)
    {
        MachinesReadOn(() => engine.Choice.IsOpen && MachinesSay(word));
        if (!engine.Choice.IsOpen) { Console.WriteLine($"  !! never asked: {word}"); return; }
        Frames(16);
        int answer = machinesAnswers.Count > 0 ? machinesAnswers.Peek() : 0;
        for (int k = 0; k < answer; k++) engine.Choice.Move(1);
        Frames(3);
        Shot(shot);
        for (int k = 0; k < answer; k++) engine.Choice.Move(-1);
    }

    // Spoken to from a tile, facing them
    void MachinesTalk(int x, int y, Direction facing)
    {
        game.Player.SetPosition(x, y, facing);
        Frames(3);
        game.Interact();
        Frames(3);
    }

    // Put down on a map as if come to it, its own arrival script read to its end
    void MachinesEnter(string map, int x, int y, Direction facing)
    {
        At(map, x, y, facing);
        game.ArriveOnMap();
        Frames(6);
        MachinesReadOn();
        game.LocationSign.Hide();
    }
}
