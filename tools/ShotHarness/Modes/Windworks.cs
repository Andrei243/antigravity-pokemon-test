partial class Harness
{
    // The third chapter's first half (plan 02 · S6, part 1; not part of `all`), played by its own scripts: Floaroma Town
    // and its meadow, the Valley Windworks and Commander Mars, and Eterna Forest with Cheryl beside the player
    public void WindworksMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        team.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Torterra")!, 40));

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        Player Me() => game.Player;
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
        // A battle won: every foe brought down to 1 HP and struck
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 300 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    // A wild battle has no team of the foe's: its Pokémon are the ones on the field
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
        void TalkTo(string map, string key, string place)
        {
            var m = MapDatabase.Get(map);
            var npc = m.NPCs.First(n => n.Key == key && n.ScriptFile == place);
            At(map, npc.GridX, npc.GridY + 1, Direction.Up);
            game.Interact();
        }
        void Walk(Direction way, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                var (x0, y0) = (Me().GridX, Me().GridY);
                engine.Steering = (way, false);
                Until(() => Me().IsMoving, "a step", 60);
                Until(() => !Me().IsMoving || (Me().GridX, Me().GridY) != (x0, y0), "the step's end", 60);
            }
            engine.Steering = null;
            Frames(20);
        }

        Frames(5);
        ReadTo(null, 600);

        // ---- Floaroma Town: the grunts at the meadow's door, and its people about their day
        TalkTo("Sinnoh", "grunt_m_west", "floaroma_town");
        ReadTo("signed up"); Frames(10); Shot("ww01_floaroma_the_grunts");
        ReadTo(null);
        At("Sinnoh", 175, 657, Direction.Up); Frames(30); Shot("ww02_floaroma_people_stand");
        engine.PeopleStayPut = false;
        Frames(300);
        Shot("ww03_floaroma_people_about");
        engine.PeopleStayPut = true;

        // ---- Route 205: the little girl, the bridge
        StepOnto("Sinnoh", 211, 659, Direction.Right);
        ReadTo("spacemen"); Frames(10); Shot("ww04_route205_the_little_girl");
        ReadTo(null);
        StepOnto("Sinnoh", 217, 653, Direction.Up);
        ReadTo("Eterna Forest"); Frames(10); Shot("ww05_route205_the_bridge");
        ReadTo(null); Frames(20); Shot("ww06_route205_sent_back");
        Console.WriteLine($"route 205: windworks {story.Var("VAR_VALLEY_WINDWORKS_STATE")}, town grunts gone {story.Has("FLAG_HIDE_FLOAROMA_TOWN_GRUNTS")}");

        // ---- Floaroma Meadow: two battles, the Works Key
        StepOnto("FloaromaMeadow", 12, 48, Direction.Up);
        ReadTo("hand over the Honey"); Frames(10); Shot("ww07_meadow_the_grunts");
        IntoBattle("the first grunt");
        ToMainMenu(game.Battle); Shot("ww08_meadow_the_battle");
        ReadTo("dropped this"); Frames(10); Shot("ww09_meadow_the_works_key");
        ReadTo(null);
        Console.WriteLine($"meadow: works key {bag.GetQuantity(ItemDatabase.Get("Works Key")!)}, honey {bag.GetQuantity(ItemDatabase.Get("Honey")!)}");

        // ---- the Valley Windworks: the grunt at the door, the door
        TalkTo("Sinnoh", "grunt_m", "valley_windworks_outside");
        ReadTo("battle for it"); Frames(10); Shot("ww10_windworks_the_door_grunt");
        ReadTo("Ka-chunk"); Frames(10); Shot("ww11_windworks_locked_in");
        ReadTo(null);
        At("Sinnoh", 243, 655, Direction.Up);
        game.Interact();
        ReadTo("door is open"); Frames(10); Shot("ww12_windworks_the_door_opens");
        ReadTo(null);
        Console.WriteLine($"windworks: door open {story.Has("FLAG_UNLOCKED_VALLEY_WINDWORKS_DOOR")}");

        // ---- inside: the grunt who runs off, Commander Mars and Charon, the girl and her papa
        At("Sinnoh", 243, 655, Direction.Up);
        engine.Steering = (Direction.Up, false);
        Until(() => game.Map != MapDatabase.Get("Sinnoh"), "into the windworks", 300);
        engine.Steering = null;
        ReadTo("warn the Commander"); Frames(10); Shot("ww13_windworks_the_alarm");
        ReadTo(null);
        StepOnto("ValleyWindworksBuilding", 19, 7, Direction.Up);
        ReadTo("whole new world"); Frames(10); Shot("ww14_windworks_commander_mars");
        IntoBattle("Commander Mars");
        ToMainMenu(game.Battle); Shot("ww15_windworks_the_battle");
        ReadTo("Will you be quiet"); Frames(10); Shot("ww16_windworks_charon");
        ReadTo("all stinky"); Frames(10); Shot("ww17_windworks_the_girl_and_her_papa");
        ReadTo(null);
        Console.WriteLine($"windworks: freed {story.Var("VAR_VALLEY_WINDWORKS_STATE")}, bridge open {story.Has("FLAG_HIDE_ROUTE_205_SOUTH_GRUNTS")}");

        // ---- outside again: Looker
        ComeThrough("Sinnoh", 243, 655, Direction.Down);
        ReadTo("International Police"); Frames(10); Shot("ww20_windworks_looker");
        ReadTo("Eterna City"); Frames(10); Shot("ww21_windworks_looker_back");
        ReadTo(null);

        // ---- Eterna Forest: Cheryl
        ComeThrough("EternaForest", 28, 85, Direction.Up);
        ReadTo("My name is Cheryl"); Frames(10); Shot("ww22_forest_cheryl");
        ReadTo(null);
        Walk(Direction.Up, 2); Shot("ww23_forest_cheryl_follows");
        // A step west, and back east into her: the two swap round
        Walk(Direction.Left, 1); Walk(Direction.Right, 1); Shot("ww24_forest_turning_back_into_her");
        // A wild battle beside her: two Pokémon at once
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Wurmple", MinLevel = 12, MaxLevel = 12, Weight = 1 });
        Until(() => State() == GameState.Battle, "the wild double battle", 300);
        var wildFight = game.Battle;
        ToMainMenu(wildFight); Shot("ww25_forest_two_wild_pokemon");
        Win();
        Console.WriteLine($"forest: after the wild battle, team healed {team.Members.All(p => p.CurrentHP == p.MaxHP)}");
        // Two trainers who face one another come together
        At("EternaForest", 38, 68, Direction.Up);
        game.KeepPartnerAlong();
        Frames(10);
        engine.Steering = (Direction.Up, false);
        Until(() => Me().GridY == 67, "the step between the pair", 120);
        engine.Steering = null;
        Frames(30); Shot("ww26_forest_the_pair_see_the_player");
        Until(() => engine.ScriptRunning, "the pair's challenge", 300);
        IntoBattle("the pair's battle");
        var tag = game.Battle;
        ToMainMenu(tag); Shot("ww27_forest_the_tag_battle");
        Win();
        ReadTo(null);
        // The far side: she says goodbye
        At("EternaForest", 81, 36, Direction.Right);
        game.KeepPartnerAlong();
        Frames(10);
        StepOnto("EternaForest", 82, 36, Direction.Right);
        ReadTo("token of my thanks"); Frames(10); Shot("ww28_forest_cheryl_parts");
        ReadTo("meet again"); Frames(10); Shot("ww29_forest_cheryl_at_the_exit");
        ReadTo(null);
        Console.WriteLine($"forest: soothe bell {bag.GetQuantity(ItemDatabase.Get("Soothe Bell")!)}, travelled {story.Has("FLAG_TRAVELED_WITH_CHERYL")}");
    }
}
