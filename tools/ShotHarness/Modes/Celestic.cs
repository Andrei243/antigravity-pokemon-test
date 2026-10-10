partial class Harness
{
    // The sixth chapter (plan 02 · S9; not part of `all`), played by its own scripts: Route 218's blockade before the
    // Canalave gate, the Psyduck on Route 210 cured and Cynthia's Old Charm, northern Route 210 in its fog, Celestic
    // Town's elder and Team Galactic's grunt, the ruins' painting, the elder, Cyrus and his battle, HM03, Cynthia
    // outside the ruins, the elder's house, the Fuego Ironworks and Mr. Fuego, the sea south of Sandgem Town, the
    // blockade gone and the rival's battle on Canalave City's bridge. CELESTIC_FROM=ruins starts at the ruins' painting
    // and CELESTIC_FROM=sea at the Fuego Ironworks, the story set as the scenes before would have left it.
    public void CelesticMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        var empoleon = new Pokemon(PokemonDatabase.Get("Empoleon")!, 45);
        empoleon.Moves.Clear();
        foreach (string move in new[] { "Surf", "Drill Peck", "Metal Claw", "Brine" }) empoleon.Moves.Add(new Move(MoveDatabase.Get(move)!));
        team.Members.Insert(0, empoleon);
        // The Badges of the chapters before: the Relic Badge lets Defog be used, the Fen Badge Surf
        foreach (var badge in new[] { Badge.Coal, Badge.Forest, Badge.Relic, Badge.Cobble, Badge.Fen }) story.GiveBadge(badge);
        bag.AddItem(ItemDatabase.Get("Secret Potion")!, 1);

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        Player Me() => game.Player;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Quick();
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        // Llvmpipe fills the 4K screen slowly: between shots the game is moved on a frame at a time but drawn only every
        // sixth frame, and a line of text is shown whole at once (the shots' frames are drawn in full before each one)
        int quick = 0;
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            if (++quick % 6 == 0) engine.Draw();
        }
        void ReadTo(string? text, int most = 2400)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Box().CurrentLine.Contains(text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld && i > 60) break;
                if (text == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (engine.Choice.IsOpen) { Quick(); engine.Choice.Confirm(); Quick(); }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        // A battle won: every foe brought down to 1 HP and struck, the player's own team kept well (the battles follow
        // one another with no Pokémon Center between, and a fainted lead would leave the battle asking for another)
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 300 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var mine in team.Members.Where(p => p.CurrentHP > 0)) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
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
            foreach (var mine in team.Members) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
            Frames(10);
        }
        void IntoBattle(string what)
        {
            for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Quick(); engine.Choice.Confirm(); Quick(); }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
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
        void Where(string what) => Console.WriteLine($"{what}: player at {Me().GridX},{Me().GridY} of {game.Map.Name}");

        Frames(5);
        ReadTo(null, 600);

        string from = Environment.GetEnvironmentVariable("CELESTIC_FROM") ?? "";
        if (from == "")
        {
            // ---- Route 218: the blockade before the Canalave gate, rehearsing its show
            At("Sinnoh", 76, 755, Direction.Left); Frames(30); Shot("cs01_route218_the_blockade");

            // ---- Route 210: the Psyduck cured, and Cynthia up the road after them with the Old Charm
            TalkTo("Sinnoh", "psyduck_1", "route_210_south");
            ReadTo("used the Secret Potion"); Frames(4); Shot("cs02_route210_the_secret_potion");
            ReadTo("big favour"); Frames(4); Shot("cs03_route210_cynthia");
            ReadTo(null);
            Console.WriteLine($"route 210: old charm {bag.GetQuantity(ItemDatabase.Get("Old Charm")!)}, secret potion used {story.Has("FLAG_USED_SECRETPOTION")}");

            // ---- northern Route 210 in its fog
            At("Sinnoh", 540, 527, Direction.Down); Frames(30); Shot("cs04_route210_north_in_the_fog");

            // ---- Celestic Town: the elder's warning, the grunt before the ruins and the Old Charm handed over
            StepOnto("Sinnoh", 463, 538, Direction.Up);
            ReadTo("spaceman"); Frames(4); Shot("cs05_celestic_the_elder_warns");
            ReadTo(null);
            TalkTo("Sinnoh", "grunt_m", "celestic_town");
            ReadTo("Galactic Bomb"); Frames(4); Shot("cs06_celestic_the_grunt");
            IntoBattle("the grunt");
            Win();
            ReadTo("granddaughter"); Frames(4); Shot("cs07_celestic_the_elder_and_the_old_charm");
            ReadTo(null); Frames(20);
            Console.WriteLine($"celestic: old charm delivered {story.Has("FLAG_DELIVERED_OLD_CHARM")}, grunt gone {story.Has("FLAG_HIDE_CELESTIC_TOWN_GRUNT_M")}");
        }
        else
        {
            // As Route 210 and Celestic Town's scenes would have left it
            foreach (string flag in new[] { "FLAG_HIDE_ROUTE_210_SOUTH_PSYDUCK", "FLAG_USED_SECRETPOTION", "FLAG_DELIVERED_OLD_CHARM", "FLAG_HIDE_CELESTIC_TOWN_GRUNT_M" })
                story.Set(flag);
            story.SetVar("VAR_CELESTIC_TOWN_ELDER_STATE", 1);
        }

        if (from != "sea")
        {
            // ---- the ruins: the painting, the elder and Cyrus, his battle and HM03
            ComeThrough("CelesticTownCave", 9, 3, Direction.Up);
            Frames(30); Shot("cs08_ruins_the_painting");
            game.Interact();
            ReadTo("What could it all mean"); Frames(4); Shot("cs09_ruins_looking_closely");
            ReadTo("Sinnoh's ancient legend"); Frames(4); Shot("cs10_ruins_the_elder");
            ReadTo("My name is Cyrus"); Frames(4); Shot("cs11_ruins_cyrus");
            IntoBattle("Cyrus");
            ToMainMenu(game.Battle); Shot("cs12_ruins_cyrus_battle");
            Win();
            ReadTo("no need of it now"); Frames(4); Shot("cs13_ruins_hm03_from_the_elder");
            ReadTo(null); Frames(20);
            Console.WriteLine($"ruins: HM03 {bag.GetQuantity(ItemDatabase.Get("HM03")!)}, celestic state {story.Var("VAR_CELESTIC_TOWN_STATE")}");

            // ---- out of the ruins: Cynthia, and the way to Canalave City
            ComeThrough("Sinnoh", 463, 522, Direction.Down);
            ReadTo("Galactic Bomb"); Frames(4); Shot("cs14_celestic_cynthia_outside_the_ruins");
            ReadTo(null); Frames(10);
            Console.WriteLine($"celestic: state {story.Var("VAR_CELESTIC_TOWN_STATE")}, route 218 blockade lifted {story.Has("FLAG_HIDE_ROUTE_218_BLOCKADE")}");

            // ---- the elder at home
            ComeThrough("CelesticTownNorthHouse", 5, 8, Direction.Up);
            Frames(30); Shot("cs15_celestic_the_elders_house");
        }
        else
        {
            // As the ruins and Cynthia's scene would have left it
            foreach (string flag in new[] { "FLAG_EXAMINED_CELESTIC_TOWN_CAVE_PAINTING", "FLAG_HIDE_CELESTIC_TOWN_ELDER", "FLAG_HIDE_ROUTE_218_BLOCKADE" })
                story.Set(flag);
            story.Unset("FLAG_HIDE_CELESTIC_TOWN_NORTH_HOUSE_ELDER");
            story.Unset("FLAG_HIDE_CELESTIC_TOWN_CYNTHIA");
            story.SetVar("VAR_CELESTIC_TOWN_STATE", 2);
            bag.AddItem(ItemDatabase.Get("HM03")!, 1);
        }

        // ---- the Fuego Ironworks: its floor, and Mr. Fuego's Star Piece
        ComeThrough("FuegoIronworksBuilding", 26, 25, Direction.Up);
        Frames(30); Shot("cs16_ironworks_the_floor");
        TalkTo("FuegoIronworksBuilding", "mr_fuego", null);
        ReadTo("moving floors"); Frames(4); Shot("cs17_ironworks_mr_fuego");
        ReadTo(null);
        Console.WriteLine($"ironworks: shards {bag.GetQuantity(ItemDatabase.Get("Red Shard")!)}, star pieces {bag.GetQuantity(ItemDatabase.Get("Star Piece")!)}");

        // ---- by sea south of Sandgem Town: the tubers' beach and the swimmers of Route 220
        At("Sinnoh", 174, 894, Direction.Down);
        Me().SetMode(TravelMode.Surfing);
        Frames(30); Shot("cs18_route219_by_sea");
        Me().SetMode(TravelMode.OnFoot);

        // ---- Route 218: the blockade gone from before the Canalave gate
        At("Sinnoh", 76, 755, Direction.Left); Frames(30); Shot("cs19_route218_the_way_open");

        // ---- Canalave City: the rival on the bridge
        StepOnto("Sinnoh", 47, 724, Direction.Left);
        ReadTo("brand-new Badge"); Frames(4); Shot("cs20_canalave_the_rival_on_the_bridge");
        IntoBattle("the rival");
        ToMainMenu(game.Battle); Shot("cs21_canalave_the_rivals_battle");
        Win();
        ReadTo("Iron Island"); Frames(4); Shot("cs22_canalave_off_to_iron_island");
        ReadTo(null); Frames(10);
        Where("canalave");
        Console.WriteLine($"canalave: state {story.Var("VAR_CANALAVE_CITY_STATE")}, cynthia gone from celestic {story.Has("FLAG_HIDE_CELESTIC_TOWN_CYNTHIA")}");
    }
}
