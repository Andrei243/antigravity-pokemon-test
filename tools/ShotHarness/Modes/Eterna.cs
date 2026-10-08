partial class Harness
{
    // The third chapter's second half (plan 02 · S6, part 2; not part of `all`), played by its own scripts: the rival and
    // Cyrus at Eterna City's statue, Cynthia and HM01, Gardenia at her Gym's door, the tree before Team Galactic's
    // building (the Forest Badge is given as the Gym gives it: the Gym's inside is plan 01 · M9's), Looker in disguise,
    // the building's floors and Commander Jupiter, the cycle shop's Bicycle, the ways out watched until the Explorer Kit,
    // the Underground Man, the Pokémon Center and Gardenia before the Old Chateau
    public void EternaMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        var torterra = new Pokemon(PokemonDatabase.Get("Torterra")!, 40);
        torterra.Moves.Clear();
        foreach (string move in new[] { "Razor Leaf", "Earthquake", "Cut", "Bite" }) torterra.Moves.Add(new Move(MoveDatabase.Get(move)!));
        team.Members.Insert(0, torterra);

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
        // A battle won: every foe brought down to 1 HP and struck
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 300 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
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
        // Walks into a door ahead until the map changes, and lets the arrival's own script start
        void Into(Direction way, string what)
        {
            var from = game.Map;
            engine.Steering = (way, false);
            Until(() => game.Map != from, what, 300);
            engine.Steering = null;
            Frames(40);
        }

        Frames(5);
        ReadTo(null, 600);

        // ---- Eterna City as the chapter finds it: Cyrus before the statue, Gardenia at her Gym's door, grunts about
        At("Sinnoh", 322, 524, Direction.Right); Frames(30); Shot("et01_eterna_cyrus_at_the_statue");
        // The row of pines south of the Gym's door hides whoever stands right in front of it: she is seen from her side
        At("Sinnoh", 311, 563, Direction.Right); Frames(30); Shot("et02_eterna_gardenia_at_her_door");

        // ETERNA_FROM=cynthia or =building starts the run later on (the harness runs slowly under software rendering),
        // the story set as the scenes before would have left it
        string from = Environment.GetEnvironmentVariable("ETERNA_FROM") ?? "";
        if (from is "cynthia" or "building")
        {
            story.SetVar("VAR_ETERNA_CITY_STATE", 1);
            story.Set("FLAG_HIDE_ETERNA_CITY_CYRUS");
            story.Set("FLAG_HIDE_ETERNA_CITY_RIVAL");
        }
        if (from == "building")
        {
            story.SetVar("VAR_ETERNA_CITY_STATE", 2);
            story.Set("FLAG_HIDE_ETERNA_CITY_CYNTHIA");
            story.Set("FLAG_HIDE_ETERNA_CITY_GARDENIA");
            bag.AddItem(ItemDatabase.Get("HM01")!, 1);
            story.GiveBadge(Badge.Forest);
            story.Unset("FLAG_HIDE_ETERNA_FOREST_GARDENIA");
        }

        // ---- the rival runs into the player and takes them to the statue; Cyrus
        if (from == "")
        {
            StepOnto("Sinnoh", 303, 524, Direction.Right);
            ReadTo("Thud"); Frames(10); Shot("et03_eterna_the_rival_bumps");
            ReadTo("show you the way"); Frames(10); Shot("et04_eterna_the_rival");
            ReadTo("Who's that"); Frames(10); Shot("et05_eterna_at_the_statue");
            ReadTo("Time and space"); Frames(10); Shot("et06_eterna_cyrus");
            ReadTo("in my way"); Frames(10); Shot("et07_eterna_cyrus_steps_up");
            ReadTo("best idea"); Frames(10); Shot("et08_eterna_the_rivals_idea");
            ReadTo(null); Frames(20); Shot("et09_eterna_the_rival_gone");
            Console.WriteLine($"eterna: state {story.Var("VAR_ETERNA_CITY_STATE")}, player at {Me().GridX},{Me().GridY}");
        }

        // ---- Cynthia and HM01, before Team Galactic's building
        if (from != "building")
        {
            StepOnto("Sinnoh", 304, 523, Direction.Up);
            ReadTo("Pokédex"); Frames(10); Shot("et10_eterna_cynthia");
            ReadTo("HM01"); Frames(10); Shot("et11_eterna_hm01");
            ReadTo("Professor Rowan"); Frames(10); Shot("et12_eterna_cynthias_regards");
            ReadTo(null); Frames(20);
            Console.WriteLine($"eterna: state {story.Var("VAR_ETERNA_CITY_STATE")}, HM01 {bag.GetQuantity(ItemDatabase.Get("HM01")!)}");

            // ---- Gardenia at her Gym's door
            TalkTo("Sinnoh", "gardenia", "eterna_city", -1, 0);
            ReadTo("Gym Leader"); Frames(10); Shot("et13_eterna_gardenia");
            ReadTo(null); Frames(20); Shot("et14_eterna_gardenia_gone_in");

            // ---- the Forest Badge as the Gym gives it, and the tree before the building cut down
            story.GiveBadge(Badge.Forest);
            story.Unset("FLAG_HIDE_ETERNA_FOREST_GARDENIA");
            At("Sinnoh", 305, 522, Direction.Up);
            game.Interact();
            ReadTo("Use Cut"); Frames(10); Shot("et15_eterna_the_tree");
            ReadTo(null); Frames(20); Shot("et16_eterna_the_way_in");
        }

        // ETERNA_ONLY=city stops once the way into the building is open
        if (Environment.GetEnvironmentVariable("ETERNA_ONLY") != "city")
        {
            // ---- inside: Looker in disguise
            if (from == "building") At("Sinnoh", 305, 520, Direction.Up);
            Into(Direction.Up, "into the building");
            ReadTo("It is I"); Frames(10); Shot("et17_building_looker_in_disguise");
            ReadTo("two staircases"); Frames(10); Shot("et18_building_looker_himself");
            ReadTo(null); Frames(20);

            // ---- the floors
            At("TeamGalacticEternaBuilding1F", 11, 13, Direction.Up); Frames(30); Shot("et19_building_1f");
            At("TeamGalacticEternaBuilding1F", 15, 7, Direction.Left); Frames(30); Shot("et20_building_1f_stairs");
            At("TeamGalacticEternaBuilding2F", 15, 6, Direction.Up); Frames(30); Shot("et21_building_2f");
            At("TeamGalacticEternaBuilding3F", 13, 11, Direction.Up); Frames(30); Shot("et22_building_3f");
            // A trainer of the floors
            TalkTo("TeamGalacticEternaBuilding3F", "scientist_travon", null, 1, 0);
            IntoBattle("Travon");
            ToMainMenu(game.Battle); Shot("et23_building_travon");
            Win(); ReadTo(null);

            // ---- Commander Jupiter
            At("TeamGalacticEternaBuilding4F", 14, 8, Direction.Up); Frames(30); Shot("et24_building_4f");
            TalkTo("TeamGalacticEternaBuilding4F", "jupiter", null);
            ReadTo("Jupiter, a Commander"); Frames(10); Shot("et25_building_jupiter");
            IntoBattle("Jupiter");
            ToMainMenu(game.Battle); Shot("et26_building_the_battle");
            Win();
            ReadTo("myths"); Frames(10); Shot("et27_building_jupiters_warning");
            ReadTo("cycle shop"); Frames(10); Shot("et28_building_the_manager");
            ReadTo(null); Frames(20); Shot("et29_building_free");
            Console.WriteLine($"building: state {story.Var("VAR_ETERNA_CITY_STATE")}, galactic gone {story.Has("FLAG_TEAM_GALACTIC_LEFT_ETERNA_BUILDING")}");

            // ---- the cycle shop's Bicycle
            At("EternaCycleShop", 7, 9, Direction.Up); Frames(30); Shot("et30_cycle_shop");
            TalkTo("EternaCycleShop", "pokefan_m", null);
            ReadTo("newest Bicycle"); Frames(10); Shot("et31_cycle_shop_the_manager");
            ReadTo(null); Frames(10);
            Console.WriteLine($"cycle shop: bicycle {bag.GetQuantity(ItemDatabase.Get("Bicycle")!)}, exits watched {story.Var("VAR_ETERNA_CITY_BLOCK_EXITS_STATE")}");

            // ---- the ways out, watched until the Explorer Kit
            ComeThrough("Sinnoh", 310, 540, Direction.Down);
            StepOnto("Sinnoh", 297, 533, Direction.Left);
            ReadTo("You've got a Bicycle"); Frames(10); Shot("et32_eterna_the_west_way_out");
            ReadTo(null); Frames(20);
            StepOnto("Sinnoh", 305, 565, Direction.Down);
            ReadTo("Explorer Kit"); Frames(10); Shot("et33_eterna_the_south_way_out");
            ReadTo(null); Frames(20);

            // ---- the Underground Man
            At("EternaUndergroundManHouse", 4, 7, Direction.Up); Frames(30); Shot("et34_underground_mans_house");
            TalkTo("EternaUndergroundManHouse", "underground_man", null);
            ReadTo("Underground Man, they call me"); Frames(10); Shot("et35_the_underground_man");
            ReadTo(null); Frames(10);
            ComeThrough("Sinnoh", 310, 531, Direction.Down);
            Console.WriteLine($"eterna: explorer kit {bag.GetQuantity(ItemDatabase.Get("Explorer Kit")!)}, exits watched {story.Var("VAR_ETERNA_CITY_BLOCK_EXITS_STATE")}");

            // ---- the Pokémon Center's people
            At("EternaPokemonCenter", 5, 6, Direction.Up); Frames(30); Shot("et36_pokemon_center");

            // ---- Gardenia before the Old Chateau
            At("EternaForest", 73, 35, Direction.Up); Frames(30); Shot("et37_forest_gardenia");
            TalkTo("EternaForest", "gardenia", "eterna_forest");
            ReadTo("Old Chateau"); Frames(10); Shot("et38_forest_gardenia_and_the_chateau");
            ReadTo("frightened"); Frames(10); Shot("et39_forest_gardenia_not_scared");
            ReadTo(null); Frames(20); Shot("et40_forest_gardenia_gone");
        }
        else
        {
            // Only the city: and a look at two of the rooms beside it
            At("TeamGalacticEternaBuilding3F", 13, 11, Direction.Up); Frames(30); Shot("et22_building_3f");
            At("EternaUndergroundManHouse", 4, 7, Direction.Up); Frames(30); Shot("et34_underground_mans_house");
        }
    }
}
