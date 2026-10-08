partial class Harness
{
    // ---------------------------------------------------------------- the story's scripts

    // Scripts at work in the field (plan 02 · S1): the nurse's question and its answers, a clerk, a PC, a signboard, a
    // clown's quiz and its coupon, a scene written here (bubbles, walks, the camera sent away, someone gone, text
    // over a black screen), and a trainer who sees the player, challenges, is beaten and has a last word.
    public void StoryMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var team = game.Party;
        var bag = game.Bag;

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        // Runs frames until something holds; says so when it never does
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // The line being written is shown whole
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        // The A button on a line that is written out
        void Next() { Box().Advance(); Frames(2); }
        // Reads a talk to its end, line by line
        void ReadOn(int most = 12)
        {
            for (int i = 0; i < most && Box().IsActive && !Box().IsQuestion; i++) { Whole(); Next(); }
        }
        void Talk() { game.Interact(); Frames(3); }

        // ---- the nurse: a welcome, a question with its answers, the team made well
        At("PokemonCenter", 5, 4, Direction.Up);
        team.Members[0].CurrentHP = 1;
        Talk();
        Whole(); Shot("st01_nurse_greets");
        Next();
        Whole(); Until(() => engine.Choice.IsOpen, "the nurse's answers"); Frames(20); Shot("st02_nurse_asks");
        engine.Choice.Move(1); Frames(4); Shot("st03_nurse_no");
        engine.Choice.Move(-1); engine.Choice.Confirm(); Frames(20);
        Whole(); Shot("st04_nurse_takes_them");
        Next();
        Whole(); Shot("st05_nurse_done");
        ReadOn();
        Console.WriteLine($"nurse: healed {team.Members[0].CurrentHP == team.Members[0].MaxHP}, script running {engine.ScriptRunning}, state {State()}");

        // Asked again and answered no, nothing is healed
        team.Members[0].CurrentHP = 1;
        Talk(); Whole(); Next(); Whole();
        Until(() => engine.Choice.IsOpen, "the nurse's answers again");
        engine.Choice.Back(); Frames(20);
        ReadOn();
        Console.WriteLine($"nurse, told no: healed {team.Members[0].CurrentHP == team.Members[0].MaxHP}, script running {engine.ScriptRunning}");
        team.HealAll();

        // ---- the PC in the corner: a line, then the boxes
        var center = MapDatabase.Get("PokemonCenter");
        var terminal = center.NPCs.First(n => n.IsPCTerminal);
        At("PokemonCenter", terminal.GridX, terminal.GridY + 1, Direction.Up);
        Talk(); Whole(); Shot("st06_pc_switched_on");
        Next(); Frames(20);
        Console.WriteLine($"pc: state {State()}");
        game.PcScreen.Close(); Frames(6);
        Console.WriteLine($"pc closed: script running {engine.ScriptRunning}, state {State()}");

        // ---- a clerk: the greeting, the counter, a word on the way out
        var mart = MapDatabase.Get("PokeMart");
        var clerk = mart.NPCs.First(n => n.IsPokeMartClerk);
        // (The clerk stands behind the counter: the player speaks across it, from two tiles away)
        int reach = mart.IsCounter(clerk.GridX, clerk.GridY + 1) ? 2 : 1;
        At("PokeMart", clerk.GridX, clerk.GridY + reach, Direction.Up);
        Talk(); Whole(); Shot("st07_clerk_greets");
        Next(); Frames(30); Shot("st08_clerk_counter");
        game.Shop.Close(); Frames(6);
        Whole(); Shot("st09_clerk_goodbye");
        ReadOn();
        Console.WriteLine($"clerk: script running {engine.ScriptRunning}, state {State()}");

        // ---- a signboard
        At("Sinnoh", 176, 745, Direction.Up);
        Talk(); Whole(); Shot("st10_sign");
        ReadOn();

        // ---- a clown of the Pokétch campaign: the question, the right answer, the coupon
        var sinnoh = MapDatabase.Get("Sinnoh");
        // The campaign's first two clowns come out once the rival has had his parcel (plan 02 · S5)
        story.Unset("FLAG_HIDE_JUBILIFE_CITY_CLOWNS_1_AND_2");
        Frames(2);
        var clown = sinnoh.FindPerson("clown_1", "jubilife_city")!;
        At("Sinnoh", clown.GridX, clown.GridY + 1, Direction.Up);
        Talk(); Whole(); Next();
        Whole(); Until(() => engine.Choice.IsOpen, "the clown's answers"); Frames(20); Shot("st11_clown_asks");
        engine.Choice.Confirm(); Frames(20);
        Whole(); Shot("st12_clown_right");
        Next(); Whole(); Next(); Whole(); Next();
        Whole(); Shot("st13_clown_coupon");
        ReadOn();
        Console.WriteLine($"clown: coupons {bag.GetQuantity(ItemDatabase.Get("Coupon 1")!)}, flag {story.Has("FLAG_RECEIVED_COUPON_1")}, script running {engine.ScriptRunning}");
        // Spoken to again, the clown has no second coupon
        Talk(); Whole(); Shot("st14_clown_afterwards");
        ReadOn();
        Console.WriteLine($"clown again: coupons {bag.GetQuantity(ItemDatabase.Get("Coupon 1")!)}, questions open {engine.Choice.IsOpen}");

        // ---- a scene written for the picture: bubbles, a walk, the camera sent away, someone gone, text over black
        var breeder = sinnoh.FindPerson("pokemon_breeder_f", "twinleaf_town")!;
        var (bx, by) = (breeder.GridX, breeder.GridY);
        // (On the open ground west of her: the player five tiles off, and they walk up to one another)
        for (int x = bx - 5; x < bx; x++)
            if (!sinnoh.IsWalkable(x, by)) Console.WriteLine($"  !! the scene's ground is blocked at {x},{by}");
        At("Sinnoh", bx - 5, by, Direction.Right);
        var me = game.Player;
        var view = game.World;
        // (Written as a script of her town, so that its names mean her town's people)
        var scene = ScriptParser.Parse("twinleaf_town", $"""
            script Scene
              emote player exclaim 0.7
              face pokemon_breeder_f player
              emote pokemon_breeder_f question 0.7
              move pokemon_breeder_f left 2
              walk player right 2
              face player pokemon_breeder_f
              camera pan {bx - 9} {by - 6} 0.6
              wait 0.5
              camera release 0.5
              hide pokemon_breeder_f
              wait 0.6
              show pokemon_breeder_f
              fade out 0.3
              text "The afternoon went by, and nobody was any the wiser."
              fade in 0.3
              camera shake 0.5
              wait 0.5
            """)[0];
        engine.StartScript(scene);
        Until(() => me.BubbleTimer > 0f, "the player's bubble"); Frames(10); Shot("st15_scene_bubble");
        Until(() => breeder.BubbleTimer > 0f, "the other's bubble"); Frames(10); Shot("st16_scene_bubble_other");
        Until(() => me.IsMoving, "the walk"); Frames(14); Shot("st17_scene_walking");
        Until(() => view.PanEase > 0.97f, "the camera away"); Frames(4); Shot("st18_scene_camera_away");
        Until(() => !sinnoh.NPCs.Contains(breeder), "someone gone"); Frames(4); Shot("st19_scene_someone_gone");
        Until(() => Box().IsActive, "text over black"); Whole(); Shot("st20_scene_text_over_black");
        Next();
        Until(() => !engine.ScriptRunning, "the scene's end");
        Frames(30); Shot("st21_scene_over");
        Console.WriteLine($"scene: player at {me.GridX - bx},{me.GridY - by} of where she stood (-3,0 is right), she at {breeder.GridX - bx},{breeder.GridY - by} (-2,0 is right), back on the map {sinnoh.NPCs.Contains(breeder)}");
        breeder.GridX = bx; breeder.GridY = by;

        // ---- a trainer: seen, challenged, beaten, and a last word
        var tristan = sinnoh.FindPerson("youngster_tristan", "route_202")!;
        var (tx, ty) = (tristan.GridX, tristan.GridY);
        team.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Empoleon")!, 50));
        At("Sinnoh", tx, ty + 3, Direction.Up);
        game.TrainersLookOnArrival = true;
        Until(() => Box().IsActive, "the trainer's challenge");
        Whole(); Shot("st22_trainer_challenges");
        Next();
        Until(() => State() == GameState.Battle, "the battle");
        var fight = game.Battle;
        ToMainMenu(fight);
        Shot("st23_trainer_battle");
        for (int guard = 0; guard < 80 && !fight.IsBattleOver; guard++)
        {
            if (fight.HUD.MenuState == BattleMenuState.Main) { fight.EnemyPokemon.CurrentHP = 1; fight.SelectMove(0); }
            else Confirm(fight);
            Skip(0.5);
        }
        Until(() => State() == GameState.Overworld, "the field again", 1200);
        Frames(20);
        Console.WriteLine($"trainer: beaten {tristan.HasBattled}, the story knows {story.HasDefeated(tristan.TrainerData!.Id)}, script running {engine.ScriptRunning}, state {State()}");
        Shot("st24_trainer_beaten");
        Talk(); Whole(); Shot("st25_trainer_last_word");
        ReadOn();
        team.Members.RemoveAt(0);
        (tristan.GridX, tristan.GridY) = (tx, ty);

        // ---- items on the ground: the ball lying there, the finding, the ground afterwards; and one nobody can see
        // (A tile beside a place from which the player can look at it, and the way they then face. From the side
        // first: a ball right behind the player's own head can't be seen, as in the original)
        (int X, int Y, Direction Facing)? BesideOrNot(Map map, int x, int y)
        {
            foreach (var (dx, dy, facing) in new[] { (-1, 0, Direction.Right), (1, 0, Direction.Left), (0, -1, Direction.Down), (0, 1, Direction.Up) })
                if (map.IsWalkable(x + dx, y + dy)) return (x + dx, y + dy, facing);
            return null;
        }
        (int X, int Y, Direction Facing) Beside(Map map, int x, int y) =>
            BesideOrNot(map, x, y) ?? throw new InvalidOperationException($"Nobody can stand beside {x},{y} of {map.Name}");

        var potion = sinnoh.FindPerson("item_potion", "route_202")!;
        var stand = Beside(sinnoh, potion.GridX, potion.GridY);
        At("Sinnoh", stand.X, stand.Y, stand.Facing);
        Frames(6); Shot("st26_item_on_the_ground");
        ShotCrop("st26b_item_ball_close", 760, 390, 400, 300, 4);
        int potionsBefore = bag.GetQuantity(ItemDatabase.Get("Potion")!);
        Talk(); Whole(); Shot("st27_item_found");
        ReadOn();
        Frames(6); Shot("st28_item_gone");
        Console.WriteLine($"item: potions {potionsBefore} then {bag.GetQuantity(ItemDatabase.Get("Potion")!)}, its flag {story.Has(potion.HiddenBy!)}, ball still on the map {sinnoh.NPCs.Contains(potion)}, script running {engine.ScriptRunning}");
        // Nothing is there to pick up a second time
        Talk(); Frames(4);
        Console.WriteLine($"item again: potions {bag.GetQuantity(ItemDatabase.Get("Potion")!)}, text on screen {Box().IsActive}");

        // The Rare Candy of Floaroma Meadow, and a TM in Oreburgh Gate's cellar, which says what it holds
        var meadow = MapDatabase.Get("FloaromaMeadow");
        var candy = meadow.NPCs.First(n => n.Item == "Rare Candy");
        stand = Beside(meadow, candy.GridX, candy.GridY);
        At("FloaromaMeadow", stand.X, stand.Y, stand.Facing);
        Frames(6); Shot("st29_rare_candy_in_the_meadow");
        Talk(); Whole(); Shot("st30_rare_candy_found");
        ReadOn();
        var cellar = MapDatabase.Get("OreburghGateB1F");
        var disc = cellar.NPCs.First(n => n.Item != null && n.Item.StartsWith("TM"));
        stand = Beside(cellar, disc.GridX, disc.GridY);
        At("OreburghGateB1F", stand.X, stand.Y, stand.Facing);
        Frames(6); Shot("st31_item_in_a_cave");
        Talk(); Whole(); Next(); Whole(); Shot("st32_tm_says_what_it_holds");
        ReadOn();

        // Hidden in the ground: nothing shows, and looking at the place finds it once. (The first one that can be
        // walked up to: Twinleaf Town's Odd Keystone lies out in the pond, for whoever surfs.)
        var (spot, secret) = sinnoh.HiddenItems.First(h => BesideOrNot(sinnoh, h.Key.X, h.Key.Y) != null);
        stand = Beside(sinnoh, spot.X, spot.Y);
        At("Sinnoh", stand.X, stand.Y, stand.Facing);
        Frames(6); Shot("st33_hidden_item_shows_nothing");
        Talk(); Whole(); Shot("st34_hidden_item_found");
        ReadOn();
        Talk(); Frames(4);
        Console.WriteLine($"hidden: {secret.Item} at {spot.X},{spot.Y}: in the bag {bag.GetQuantity(ItemDatabase.Get(secret.Item)!)}, its flag {story.Has(secret.Flag)}, found again {Box().IsActive}");

        // ---- Pokémon standing in the field (plan 10 · F1): a Machop at the mine's coal face, spoken to, and the
        // three that work the yard in Oreburgh City
        var mine = MapDatabase.Get("OreburghMineB2F");
        var machop = mine.NPCs.First(n => n.Key == "machop_2");
        stand = Beside(mine, machop.GridX, machop.GridY);
        At("OreburghMineB2F", stand.X, stand.Y, stand.Facing);
        Frames(6); Shot("st35_machop_in_the_mine");
        ShotCrop("st35b_machop_close", 760, 390, 400, 300, 4);
        Talk(); Whole(); Shot("st36_machop_speaks");
        ReadOn();
        Console.WriteLine($"machop: a {machop.Species} at {machop.GridX},{machop.GridY}, facing {machop.Facing}, script running {engine.ScriptRunning}");
        var yard = sinnoh.FindPerson("machop_2", "oreburgh_city")!;
        stand = Beside(sinnoh, yard.GridX, yard.GridY);
        At("Sinnoh", stand.X, stand.Y, stand.Facing);
        Frames(6); Shot("st37_machop_in_the_yard");
    }
}
