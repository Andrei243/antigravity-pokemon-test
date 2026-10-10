partial class Harness
{
    // ---------------------------------------------------------------- the day's events and berries (plan 06 · R14a)

    // Berry patches and the Lottery Corner, in a game of their own (b*; not part of "all"): two patches of Route
    // 206's soil in each stage, mulched and bare, the question of a patch in fruit and what picking it leaves, and
    // the lobby of Jubilife TV with its clerk's draw. It prints what each patch shows and what the bag gained.
    public void BerriesMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var patches = game.Berries;
        var bag = game.Bag;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }

        // Patches 18 and 19 stand side by side on Route 206, at (293, 627) and (294, 627)
        void Show(int patch, BerryStage stage, string? berry, Mulch mulch)
        {
            var p = patches[patch];
            p.Berry = stage == BerryStage.None ? null : berry;
            p.Stage = stage;
            p.Mulch = mulch;
            p.Yield = stage == BerryStage.Fruit ? 4 : 0;
            patches.Water(patch); // moves the patches' revision on, so the soil looks again
        }
        void Pair(string name, (BerryStage Stage, string? Berry, Mulch Mulch) left, (BerryStage Stage, string? Berry, Mulch Mulch) right)
        {
            Show(18, left.Stage, left.Berry, left.Mulch);
            Show(19, right.Stage, right.Berry, right.Mulch);
            At("Sinnoh", 293, 628, Direction.Up);
            game.LocationSign.Hide();
            Frames(4);
            Shot(name);
            Console.WriteLine($"  {name}: {left.Stage} {left.Berry} {left.Mulch} | {right.Stage} {right.Berry} {right.Mulch}");
        }

        Pair("b01_bare_and_mulched", (BerryStage.None, null, Mulch.None), (BerryStage.None, null, Mulch.Damp));
        Pair("b02_planted_and_sprouted", (BerryStage.Planted, "Cheri Berry", Mulch.None), (BerryStage.Sprouted, "Oran Berry", Mulch.Growth));
        Pair("b03_growing_and_blooming", (BerryStage.Growing, "Pecha Berry", Mulch.None), (BerryStage.Blooming, "Rawst Berry", Mulch.None));
        Pair("b04_fruit", (BerryStage.Fruit, "Cheri Berry", Mulch.None), (BerryStage.Fruit, "Chesto Berry", Mulch.Stable));
        ShotCrop("b05_fruit_close", 760, 300, 400, 300, 2);

        // The question of a patch in fruit, and the patch picked
        var cheri = ItemDatabase.Get("Cheri Berry")!;
        int before = bag.GetQuantity(cheri);
        game.Interact(); Frames(3);
        Until(() => engine.Choice.IsOpen, "the patch's question"); Frames(20);
        Shot("b06_pick_question");
        engine.Choice.Confirm(); Frames(4);
        for (int i = 0; i < 6 && Box().IsActive; i++) { Whole(); if (i == 0) Shot("b07_picked"); Next(); }
        Until(() => game.State == GameState.Overworld && !Box().IsActive, "the field again");
        Frames(4);
        Shot("b08_after_picking");
        Console.WriteLine($"  picked: {bag.GetQuantity(cheri) - before} Cheri Berry; patch 18 is {patches[18].Stage}");

        // The Lottery Corner
        At("JubilifeTV1F", 3, 4, Direction.Up);
        game.LocationSign.Hide();
        Frames(4);
        Shot("b10_tv_lobby");
        game.Interact(); Frames(3);
        Whole();
        Shot("b11_lottery_clerk");
        for (int i = 0; i < 12 && (Box().IsActive || engine.Choice.IsOpen); i++)
        {
            if (engine.Choice.IsOpen) { Frames(20); Shot("b12_lottery_question"); engine.Choice.Confirm(); Frames(4); continue; }
            Whole(); Next();
        }
        Console.WriteLine($"  the lottery: checked today {game.Story.Has("FLAG_DAILY_CHECKED_LUCKY_NUMBER")}");
    }
}
