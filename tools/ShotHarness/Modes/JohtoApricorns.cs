using PokemonPlatinumEngine.Models;
partial class Harness
{
    /// <summary>
    /// Plan 20 · J6, after part d in the <c>johto</c> mode (so nothing before it moves; <c>JOHTO_FROM=apricorn</c> runs it
    /// alone in a new game of its own): Apricorns. A Red Apricorn tree on Route 37 with its fruit on (<c>ap01_tree</c>),
    /// the man's Apricorn Box in the bag, the tree picked (<c>ap01b_picked</c>) and bare, the Box with the seven colours
    /// in it (<c>ap02_box</c>), Kurt taking the Red Apricorns (<c>ap03_kurt</c>), the next day his balls handed over
    /// (<c>ap04_kurt_ready</c>). It prints the Box's counts, the tree's look and the bag's Level Balls at each end.
    /// Written, not run.
    /// </summary>
    void JohtoApricornShots()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);
        var box = game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadTo(string? text, int most = 2400)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && box.IsActive && Translated.Saying(box.CurrentLine, text)) { Until(() => box.IsActive, "text"); box.FinishLine(); Frames(2); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !box.IsActive && game.State == GameState.Overworld) { if (++idle > 60) break; }
                else idle = 0;
                if (text == null && !engine.ScriptRunning && !box.IsActive && game.State == GameState.Overworld) return;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void Counts(string where) => Console.WriteLine($"  {where}: " + string.Join(", ", Enum.GetValues<Apricorn>().Select(c => $"{c} {game.Apricorns.Count(c)}"))
            + $"; Kurt's order {(game.Apricorns.Order is { } o ? $"{o.Count} {o.Colour}" : "none")}; {game.Bag.GetQuantity(ItemDatabase.Get("Level Ball")!)} Level Ball in the bag");

        // ---- Route 37's tree, in fruit, then picked into the Box the man gave
        var tree = MapDatabase.Get("Johto").NPCs.First(n => n.IsApricornTree && n.Key == "r37_bonguri");
        At("Johto", tree.GridX, tree.GridY + 1, Direction.Up); Frames(20);
        Console.WriteLine($"  tree: {tree.ApricornColour}, fruit on {tree.ApricornInFruit}");
        Shot("ap01_tree");
        game.Bag.AddItem(ItemDatabase.Get("Apricorn Box")!, 1);
        game.Interact();
        ReadTo("There's a Red Apricorn hanging"); Frames(10);
        ReadTo("You picked the Red Apricorn"); Frames(20);
        ReadTo(null); Frames(20);
        Console.WriteLine($"  tree: fruit on {tree.ApricornInFruit}");
        Shot("ap01b_picked");
        Counts("after the tree");

        // ---- The Box, with some of each colour in it
        int day = ApricornBox.DayOf(GameClock.Today);
        int n = 0;
        foreach (var colour in Enum.GetValues<Apricorn>())
            for (int i = 0; i <= (int)colour; i++) game.Apricorns.Pick($"harness/{n++}", colour, day);
        game.OpenApricornBox(); Frames(40);
        Shot("ap02_box");
        game.ApricornBoxScreen.Move(1); game.ApricornBoxScreen.Move(1); Frames(10);
        Shot("ap02b_box_yellow");
        game.ApricornBoxScreen.Cancel(); Frames(10);
        Counts("the Box filled");

        // ---- Kurt takes the Red Apricorns, and has Level Balls the next day
        game.Story.Set("FLAG_BEAT_AZALEA_ROCKETS");
        var kurt = MapDatabase.Get("Azalea_KurtsHouse").NPCs.First(p => p.Key == "t23r0501_gantetsu");
        At("Azalea_KurtsHouse", kurt.GridX, kurt.GridY + 1, Direction.Up); game.Interact();
        ReadTo("Give me every Apricorn of one colour"); Frames(10);
        ReadTo("Leave it with me"); Frames(10); Shot("ap03_kurt");
        ReadTo(null);
        Counts("ordered");
        GameClock.FixedDate = GameClock.FixedDate!.Value.AddDays(1);
        At("Azalea_KurtsHouse", kurt.GridX, kurt.GridY + 1, Direction.Up); game.Interact();
        ReadTo("There you are"); Frames(10); Shot("ap04_kurt_ready");
        ReadTo(null);
        Counts("collected");
    }
}
