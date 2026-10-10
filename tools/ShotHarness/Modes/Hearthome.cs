partial class Harness
{
    // The fourth chapter (plan 02 · S7; not part of `all`), played by its own scripts: the assistant's Vs. Seeker on
    // Route 207, Cyrus in Mt. Coronet, Mira lost in Wayward Cave, Keira and her Buneary coming into Hearthome City, Mom
    // and Fantina in the Contest Hall's lobby, the rival at the gate to Route 209 (the Relic Badge given as the Gym's
    // script gives it: the Gym is plan 01 · M9's), the Hallowed Tower and its Spiritomb (plan 08 · P12), the rival in
    // Solaceon Town, the ruins' inscription and hiker, the Psyduck that still block Route 210, and Amity Square's gate
    public void HearthomeMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        var torterra = new Pokemon(PokemonDatabase.Get("Torterra")!, 42);
        torterra.Moves.Clear();
        foreach (string move in new[] { "Razor Leaf", "Earthquake", "Crunch", "Strength" }) torterra.Moves.Add(new Move(MoveDatabase.Get(move)!));
        team.Members.Insert(0, torterra);
        team.Add(new Pokemon(PokemonDatabase.Get("Pikachu")!, 30));
        story.ChooseStarter("Turtwig");

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        Player Me() => game.Player;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void ReadTo(string? text, int most = 2400)
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
        // A battle won: every foe brought down to 1 HP and struck, and the team kept standing (a foe's Endeavor and a
        // quick Mach Punch would otherwise bring the lead down first)
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 300 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var mine in team.Members) mine.CurrentHP = mine.MaxHP;
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    foreach (var wild in fight.Core.EnemySlots.Where(b => b.Pokemon != null)) wild.Pokemon!.CurrentHP = Math.Min(wild.Pokemon.CurrentHP, 1);
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
        // A step onto a tile of a map, from the tile before it, that starts a scene
        void StepOnto(string map, int x, int y, Direction way)
        {
            var (dx, dy) = way switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            At(map, x - dx, y - dy, way);
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning, $"the scene at {x},{y}", 120);
            engine.Steering = null;
        }
        // Comes into a map through a warp onto a tile, as the game does: its own script, then the trigger stepped out onto
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            game.SteppedOutOfWarp = true;
            Frames(2);
        }
        // Talks to someone of a map (a room's people belong to no area: place null), from below unless told otherwise
        void TalkTo(string map, string key, string? place, int dx = 0, int dy = 1)
        {
            var m = MapDatabase.Get(map);
            var npc = m.NPCs.First(n => n.Key == key && (place == null || n.ScriptFile == place));
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }

        Frames(5);
        ReadTo(null, 600);

        // HEARTHOME_FROM=gate starts the run at the rival's gate (the harness runs slowly under software rendering): the
        // scenes before it change nothing the later ones need
        string from = Environment.GetEnvironmentVariable("HEARTHOME_FROM") ?? "";
        if (from == "")
        {
            // ---- Route 207: the assistant catches the player up before the mountain
            StepOnto("Sinnoh", 340, 713, Direction.Right);
            ReadTo("caught you"); Frames(10); Shot("ht01_route207_the_assistant");
            ReadTo("Vs. Seeker"); Frames(10); Shot("ht02_route207_the_vs_seeker");
            ReadTo(null); Frames(20);
            Console.WriteLine($"route 207: vs. seeker {bag.GetQuantity(ItemDatabase.Get("Vs. Seeker")!)}, state {story.Var("VAR_ROUTE_207_COUNTERPART_TRIGGER_STATE")}");

            // ---- Mt. Coronet: Cyrus walks down to the player
            StepOnto("MtCoronet1FSouth", 14, 23, Direction.Up);
            ReadTo("history began"); Frames(10); Shot("ht03_coronet_cyrus");
            ReadTo(null); Frames(20); Shot("ht04_coronet_cyrus_gone");

            // ---- Wayward Cave: Mira, lost in the dark (lit, for the picture, as Flash would)
            story.Set(FieldMoveRules.FlashFlag);
            TalkTo("WaywardCave1F", "mira", "wayward_cave_1f");
            ReadTo("I'm Mira"); Frames(10); Shot("ht05_wayward_mira");
            ReadTo(null); Frames(20); Shot("ht06_wayward_mira_follows");
            Console.WriteLine($"wayward cave: mira along {story.Var("VAR_WAYWARD_CAVE_1F_FOLLOWER_MIRA_STATE")}");
            // On to the way out under Route 206, with her a step behind
            At("WaywardCave1F", 41, 51, Direction.Down);
            game.KeepPartnerAlong();
            Frames(10);
            StepOnto("WaywardCave1F", 41, 53, Direction.Down);
            ReadTo("Daylight"); Frames(10); Shot("ht07_wayward_mira_goes_out");
            ReadTo(null); Frames(20);
            story.Unset(FieldMoveRules.FlashFlag);
            Console.WriteLine($"wayward cave: mira {story.Var("VAR_WAYWARD_CAVE_1F_FOLLOWER_MIRA_STATE")}, travelled {story.Has("FLAG_TRAVELED_WITH_MIRA")}");

            // ---- Hearthome City: Buneary runs up, and Keira after it
            StepOnto("Sinnoh", 461, 727, Direction.Right);
            ReadTo("thank goodness"); Frames(10); Shot("ht08_hearthome_keira_and_buneary");
            ReadTo("I'm Keira"); Frames(10); Shot("ht09_hearthome_keira");
            ReadTo(null); Frames(20);

            // ---- Fantina's guide at her Gym's door, and the crowd on the way to Route 209
            At("Sinnoh", 498, 700, Direction.Up); Frames(30); Shot("ht10_hearthome_the_gym_guide");
            At("Sinnoh", 506, 722, Direction.Down); Frames(30); Shot("ht11_hearthome_the_crowd");

            // ---- the Contest Hall: Keira, Mom and her outfit, and Fantina
            ComeThrough("ContestHallLobby", 16, 13, Direction.Up);
            ReadTo("my hero"); Frames(10); Shot("ht12_contest_hall_keira");
            ReadTo("on that stage"); Frames(10); Shot("ht13_contest_hall_mom");
            ReadTo(null); Frames(20); Shot("ht14_contest_hall_lobby");
            TalkTo("ContestHallLobby", "fantina", null);
            ReadTo("bonjour"); Frames(10); Shot("ht15_contest_hall_fantina");
            ReadTo(null); Frames(20);
            Console.WriteLine($"contest hall: visited {story.Has("FLAG_CONTEST_HALL_VISITED")}, guide gone {story.Has("FLAG_HIDE_HEARTHOME_CITY_GYM_GUIDE")}");
        }

        // ---- the Relic Badge, as Fantina's script gives it, and the rival at the gate to Route 209
        story.GiveBadge(Badge.Relic);
        story.SetVar("VAR_ROUTE_209_GATE_TO_HEARTHOME_CITY_STATE", 1);
        story.Set("FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_BLOCKADE");
        story.Unset("FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_GATE_RIVAL");
        At("Sinnoh", 500, 726, Direction.Right); Frames(30); Shot("ht16_hearthome_the_rival_waits");
        StepOnto("Sinnoh", 503, 726, Direction.Right);
        ReadTo("stage is set"); Frames(10); Shot("ht17_hearthome_the_rival");
        IntoBattle("the rival");
        ToMainMenu(game.Battle); Shot("ht18_hearthome_the_rivals_battle");
        Win();
        ReadTo("Solaceon"); Frames(10); Shot("ht19_hearthome_the_rival_off");
        ReadTo(null); Frames(20);
        Console.WriteLine($"gate: state {story.Var("VAR_ROUTE_209_GATE_TO_HEARTHOME_CITY_STATE")}");

        // ---- the Hallowed Tower (plan 08 · P12): broken, set with the keystone, and Spiritomb once enough people are met
        At("Sinnoh", 566, 715, Direction.Up); Frames(30); Shot("ht20_route209_the_hallowed_tower");
        bag.AddItem(ItemDatabase.Get("Odd Keystone")!, 1);
        game.Interact();
        ReadTo("Set the Odd Keystone"); Frames(10); Shot("ht21_route209_the_keystone");
        ReadTo(null); Frames(20);
        for (int i = 0; i < 32; i++) story.Greet($"harness/person_{i}");
        game.Interact();
        IntoBattle("Spiritomb");
        ToMainMenu(game.Battle); Shot("ht22_route209_spiritomb");
        Win(); ReadTo(null); Frames(10);
        Console.WriteLine($"hallowed tower: state {story.Var("VAR_HALLOWED_TOWER_STATE")}, people met {story.Greeted.Count}");

        // ---- Solaceon Town: the rival runs down to the player
        StepOnto("Sinnoh", 560, 669, Direction.Down);
        ReadTo("Just the person"); Frames(10); Shot("ht23_solaceon_the_rival");
        ReadTo(null); Frames(20);

        // ---- the Solaceon Ruins: an inscription, and the hiker who would borrow HM05
        At("SolaceonRuinsRoom1", 5, 2, Direction.Up);
        game.Interact();
        ReadTo("THE WAY ON"); Frames(10); Shot("ht24_ruins_the_inscription");
        ReadTo(null); Frames(10);
        TalkTo("SolaceonRuinsRoom2", "hiker", "solaceon_ruins_room_2");
        ReadTo("Defog"); Frames(10); Shot("ht25_ruins_the_hiker");
        ReadTo(null); Frames(10);

        // ---- Route 210: the Psyduck still stand in the way
        At("Sinnoh", 560, 588, Direction.Up); Frames(30);
        game.Interact();
        ReadTo("standing firm"); Frames(10); Shot("ht26_route210_the_psyduck");
        ReadTo(null); Frames(10);

        // ---- Amity Square: the receptionist lets the player in with a Pikachu
        StepOnto("AmitySquare", 12, 46, Direction.Up);
        ReadTo("stroll"); Frames(10); Shot("ht27_amity_square_the_gate");
        ReadTo(null); Frames(20); Shot("ht28_amity_square_in");
        Console.WriteLine($"amity square: strolling {story.Var("VAR_FOLLOWER_MON_ACTIVE")}, player at {Me().GridX},{Me().GridY}");
    }
}
