partial class Harness
{
    // ---------------------------------------------------------------- the second chapter (plan 02 · S5)

    // The second chapter's scenes (plan 02 · S5), in a game of its own, by its own scripts (not part of "all"): the
    // assistant and Looker in Jubilife City, the rival and his parcel at the Trainers' School, the Pokétch campaign and
    // the Pokétch, the rival on Route 203, the hiker's HM in Oreburgh Gate, the boy who takes the player to the Gym and
    // the rival at its door, Roark down the mine, the Gym and its Leader's battle and Badge, and Team Galactic at
    // Jubilife's north gate with the assistant battling beside the player.
    public void JubilifeMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        var sinnoh = MapDatabase.Get("Sinnoh");
        bag.AddItem(ItemDatabase.Get("Parcel")!, 1);
        team.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Torterra")!, 40));

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void ReadTo(string? text, int most = 1500)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Box().CurrentLine.Contains(text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld && i > 60) break;
                if (text == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        // A battle a scene starts, won: every foe brought down to 1 HP and struck
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 200 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty.Members) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(0);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => State() == GameState.Overworld, "the field again", 1500);
            Frames(10);
        }
        // Reads on (yes to every question) until a scene's battle has begun
        void IntoBattle(string what)
        {
            for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            if (State() != GameState.Battle) Console.WriteLine($"  !! never happened: {what}");
            else Skip(1.0);
        }
        void StepOnto(int x, int y, Direction way)
        {
            var (dx, dy) = way switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            bool fromAnotherMap = game.Map != sinnoh;
            At("Sinnoh", x - dx, y - dy, way);
            // Out of a room the game comes back by a door, which brings back whoever a script hid on the map before
            // (and runs the place's own arrival script, which is let finish before the step it is waiting for)
            if (fromAnotherMap)
            {
                game.ArriveOnMap();
                Frames(2);
                Until(() => !engine.ScriptRunning, "the arrival's own script", 120);
            }
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning, $"the scene at {x},{y}", 120);
            engine.Steering = null;
        }
        void TalkTo(string map, string key, string? place = null)
        {
            var m = MapDatabase.Get(map);
            var npc = m.NPCs.First(n => n.Key == key && (place == null || m.ScriptFileAt(n.GridX, n.GridY) == place));
            At(map, npc.GridX, npc.GridY + 1, Direction.Up);
            game.Interact();
        }

        // Whatever the new game started (the bedroom's television) is read to its end first
        Frames(5);
        Console.WriteLine($"jubilife mode: script running {engine.ScriptRunning}, text {Box().IsActive}, state {State()}");
        ReadTo(null, 600);

        // ---- Jubilife City: the assistant comes to meet the player, and Looker
        StepOnto(174, 796, Direction.Up);
        Console.WriteLine($"first arrival: script running {engine.ScriptRunning}, at {game.Player.GridX},{game.Player.GridY}");
        ReadTo("Welcome to Jubilife City"); Frames(10); Shot("j01_jubilife_the_assistant");
        ReadTo("Surely the name"); Until(() => engine.Choice.IsOpen, "Looker's question"); Frames(20); Shot("j02_jubilife_looker");
        ReadTo("Trainers' School."); Frames(10); Shot("j03_jubilife_the_school");
        ReadTo(null);
        Console.WriteLine($"jubilife: state {story.Var("VAR_JUBILIFE_CITY_STATE")}, Vs. Recorder {bag.GetQuantity(ItemDatabase.Get("Vs. Recorder")!)}");
        StepOnto(188, 758, Direction.Right);
        ReadTo("Trainers' School first"); Frames(10); Shot("j04_jubilife_looker_blocks");
        ReadTo(null);

        // ---- the Trainers' School: the parcel
        At("TrainersSchool", 7, 10, Direction.Up); Frames(30); Shot("j05_trainers_school");
        TalkTo("TrainersSchool", "rival");
        ReadTo("Town Map!"); Frames(10); Shot("j06_school_the_parcel");
        ReadTo(null);
        Console.WriteLine($"school: parcel {bag.GetQuantity(ItemDatabase.Get("Parcel")!)}, campaign {story.Var("VAR_POKETCH_CAMPAIGN_STATE")}");

        // ---- the campaign: the president stops the player; the coupons; the Pokétch
        StepOnto(174, 776, Direction.Up);
        ReadTo("Three clowns"); Frames(10); Shot("j07_jubilife_the_president");
        ReadTo(null);
        foreach (var (coupon, flag) in new[] { ("Coupon 1", "FLAG_RECEIVED_COUPON_1"), ("Coupon 2", "FLAG_RECEIVED_COUPON_2"), ("Coupon 3", "FLAG_RECEIVED_COUPON_3") })
        {
            bag.AddItem(ItemDatabase.Get(coupon)!, 1);
            story.Set(flag);
        }
        TalkTo("Sinnoh", "poketch_co_president", "jubilife_city");
        ReadTo("received a Pokétch"); Frames(10); Shot("j08_jubilife_the_poketch");
        ReadTo(null);

        // ---- Route 203: the rival
        StepOnto(196, 757, Direction.Right);
        ReadTo("slacking"); Frames(10); Shot("j09_route203_the_rival");
        IntoBattle("the rival's battle");
        var rivalFight = game.Battle;
        ToMainMenu(rivalFight); Shot("j10_route203_the_battle");
        ReadTo(null);

        // ---- Oreburgh Gate: the hiker's HM
        var hiker = MapDatabase.Get("OreburghGate1F").NPCs.First(n => n.Key == "hiker");
        At("OreburghGate1F", hiker.GridX - 1, hiker.GridY + 2, Direction.Right);
        engine.Steering = (Direction.Right, false);
        Until(() => engine.ScriptRunning, "the hiker", 120);
        engine.Steering = null;
        ReadTo("you'll want this"); Frames(10); Shot("j11_oreburgh_gate_the_hiker");
        ReadTo(null);

        // ---- Oreburgh City: the boy, the rival at the Gym's door, Roark down the mine
        StepOnto(266, 749, Direction.Right);
        ReadTo("Follow me"); Frames(10); Shot("j12_oreburgh_the_boy");
        ReadTo("someone at the door"); Frames(10); Shot("j13_oreburgh_the_gym");
        ReadTo(null);
        TalkTo("Sinnoh", "rival", "oreburgh_city");
        ReadTo("Oreburgh Mine"); Frames(10); Shot("j14_oreburgh_the_rival_at_the_door");
        ReadTo(null);
        TalkTo("OreburghMineB2F", "roark");
        ReadTo("Stand back and watch"); Frames(10); Shot("j15_mine_roark");
        ReadTo("Rock Smash does it"); Frames(10); Shot("j16_mine_the_rock_smashed");
        ReadTo(null);

        // ---- the Gym: inside, its Leader, the Badge
        At("OreburghGym", 5, 24, Direction.Up); Frames(40); Shot("j17_gym_inside");
        At("OreburghGym", 9, 12, Direction.Up); Frames(30); Shot("j18_gym_the_rocks");
        TalkTo("OreburghGym", "roark");
        ReadTo("I'm Roark"); Frames(10); Shot("j19_gym_roark");
        IntoBattle("Roark's battle");
        var gymFight = game.Battle;
        ToMainMenu(gymFight); Shot("j20_gym_the_battle");
        ReadTo("received the Coal Badge"); Frames(10); Shot("j21_gym_the_coal_badge");
        ReadTo(null);
        Console.WriteLine($"gym: coal badge {story.HasBadge(Badge.Coal)}, TM76 {bag.GetQuantity(ItemDatabase.Get("TM76")!)}, jubilife {story.Var("VAR_JUBILIFE_CITY_STATE")}");

        // ---- the way out: the rival; then Jubilife's north gate
        StepOnto(262, 749, Direction.Left);
        ReadTo("Eterna City"); Frames(10); Shot("j22_oreburgh_the_rival_on_the_way_out");
        ReadTo(null);
        StepOnto(174, 743, Direction.Up);
        ReadTo("teach them some manners"); Frames(10); Shot("j23_jubilife_team_galactic");
        IntoBattle("the tag battle");
        var tag = game.Battle;
        ToMainMenu(tag); Shot("j24_jubilife_the_tag_battle");
        ReadTo("You two are a good team"); Frames(10); Shot("j25_jubilife_the_professor");
        ReadTo(null);
        Console.WriteLine($"north gate: jubilife {story.Var("VAR_JUBILIFE_CITY_STATE")}, grunts gone {story.Has("FLAG_HIDE_JUBILIFE_GALACTIC_GRUNTS")}");
    }
}
