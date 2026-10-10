partial class Harness
{
    // The fifth chapter (plan 02 · S8; not part of `all`), played by its own scripts: Route 215's black belt, the
    // television crew at Hearthome's gate to Route 212, the rival in Pastoria's Gym doorway, the assistant and Crasher
    // Wake before the Veilstone Gym, the warehouse's guards, Maylene's Badge and the assistant's call for help, the tag
    // battle by the warehouse, Looker and HM02 inside it, the rival's battle in Pastoria, the Fen Badge and Crasher Wake
    // coming out, the bomb at the Great Marsh, the grunt's flight along Route 213, Looker, the grunt cornered at Valor
    // Lakefront, Cynthia and the Secret Potion, and the road to Sunyshore shut. The Badges are won in their Gyms, by the
    // Gyms' own scripts (plan 01 · M9's). VEILSTONE_FROM=pastoria starts at the rival's battle in Pastoria, the story set
    // as the scenes before would have left it.
    public void VeilstoneMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var bag = game.Bag;
        var team = game.Party;
        var torterra = new Pokemon(PokemonDatabase.Get("Torterra")!, 45);
        torterra.Moves.Clear();
        foreach (string move in new[] { "Razor Leaf", "Earthquake", "Crunch", "Bite" }) torterra.Moves.Add(new Move(MoveDatabase.Get(move)!));
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
        // A battle won: every foe brought down to 1 HP and struck, the player's own team kept well (the Gyms follow one
        // another with no Pokémon Center between, and a fainted lead would leave the battle asking for another)
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
        void Where(string what) => Console.WriteLine($"{what}: player at {Me().GridX},{Me().GridY} of {game.Map.Name}");

        Frames(5);
        ReadTo(null, 600);

        string from = Environment.GetEnvironmentVariable("VEILSTONE_FROM") ?? "";
        if (from == "")
        {
            // ---- Route 215's black belt and his TM, in the rain
            TalkTo("Sinnoh", "black_belt", "route_215");
            ReadTo("Payback"); Frames(10); Shot("vp01_route215_black_belt");
            ReadTo(null);
            Console.WriteLine($"route 215: TM66 {bag.GetQuantity(ItemDatabase.Get("TM66")!)}");

            // ---- the television crew in the gate to Route 212, until the player has been to Pastoria
            At("Sinnoh", 459, 726, Direction.Down); Frames(30); Shot("vp02_hearthome_tv_crew_at_the_gate");

            // ---- the rival in Pastoria's Gym doorway while Crasher Wake is away
            At("Sinnoh", 591, 831, Direction.Up); Frames(30); Shot("vp03_pastoria_rival_in_the_doorway");
            TalkTo("Sinnoh", "rival", "pastoria_city", 1, 0);
            ReadTo("Veilstone"); Frames(10); Shot("vp04_pastoria_wake_is_away");
            ReadTo(null);

            // ---- Veilstone: the assistant before the Gym, and Crasher Wake
            At("Sinnoh", 683, 619, Direction.Up); Frames(30); Shot("vp05_veilstone_assistant_at_the_gym");
            StepOnto("Sinnoh", 682, 616, Direction.Up);
            ReadTo("try the Gym"); Frames(10); Shot("vp06_veilstone_the_assistant");
            ReadTo("swell"); Frames(10); Shot("vp07_veilstone_crasher_wake_sings");
            ReadTo("Pastoria Gym"); Frames(10); Shot("vp08_veilstone_crasher_wake");
            ReadTo(null); Frames(20); Shot("vp09_veilstone_both_gone");
            Console.WriteLine($"veilstone: crasher wake state {story.Var("VAR_VEILSTONE_CITY_CRASHER_WAKE_STATE")}");

            // ---- the warehouse's guards push the player back
            StepOnto("Sinnoh", 697, 596, Direction.Right);
            ReadTo("No kids allowed"); Frames(10); Shot("vp10_veilstone_the_guards");
            ReadTo(null); Frames(20); Where("pushed back");

            // ---- Maylene's Badge, won in her Gym, and the assistant's call for help outside its door
            TalkTo("VeilstoneGym", "maylene", null);
            IntoBattle("Maylene");
            Win();
            ReadTo(null);
            Console.WriteLine($"veilstone gym: cobble {story.HasBadge(Badge.Cobble)}");
            ComeThrough("Sinnoh", 684, 612, Direction.Down);
            ReadTo("snatched my Pokédex"); Frames(10); Shot("vp11_veilstone_the_assistant_needs_help");
            ReadTo(null); Frames(20);
            At("Sinnoh", 694, 598, Direction.Up); Frames(30); Shot("vp12_veilstone_by_the_warehouse");

            // ---- the tag battle beside the assistant, Looker, and in after the guards
            TalkTo("Sinnoh", "counterpart", "veilstone_city", -1, 0);
            ReadTo("how could we possibly lose"); Frames(10); Shot("vp13_veilstone_together");
            IntoBattle("the guards");
            ToMainMenu(game.Battle); Shot("vp14_veilstone_the_tag_battle");
            Win();
            ReadTo("take the stupid thing"); Frames(10); Shot("vp15_veilstone_the_pokedex_back");
            ReadTo("Word reached me"); Frames(10); Shot("vp16_veilstone_looker");
            ReadTo("Follow me"); Frames(10); Shot("vp17_veilstone_looker_and_the_warehouse");
            ReadTo(null);
            Until(() => game.Map.Name == "VeilstoneGalacticWarehouse", "into the warehouse", 600);
            Until(() => engine.ScriptRunning, "Looker's look round", 120);
            ReadTo("does not budge"); Frames(10); Shot("vp18_warehouse_the_locked_door");
            ReadTo("for the move Fly"); Frames(10); Shot("vp19_warehouse_hm02");
            ReadTo(null); Frames(20); Shot("vp20_warehouse");
            At("VeilstoneGalacticWarehouse", 13, 9, Direction.Up);
            game.Interact();
            ReadTo(null);
            Console.WriteLine($"warehouse: HM02 {bag.GetQuantity(ItemDatabase.Get("HM02")!)}, pastoria state {story.Var("VAR_PASTORIA_CITY_STATE")}");
            At("VeilstoneGalacticWarehouse", 9, 10, Direction.Up); Frames(30); Shot("vp21_warehouse_after");
        }
        else
        {
            // As the Veilstone scenes would have left it
            story.GiveBadge(Badge.Cobble);
            story.SetVar("VAR_VEILSTONE_CITY_CRASHER_WAKE_STATE", 1);
            story.SetVar("VAR_VEILSTONE_CITY_COUNTERPART_NEEDS_HELP_STATE", 2);
            story.SetVar("VAR_VEILSTONE_CITY_GALACTIC_WAREHOUSE_STATE", 2);
            story.SetVar("VAR_PASTORIA_CITY_STATE", 1);
            foreach (string flag in new[] { "FLAG_HIDE_PASTORIA_CITY_RIVAL", "FLAG_HIDE_VEILSTONE_COUNTERPART", "FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS" }) story.Set(flag);
            story.Unset("FLAG_HIDE_VEILSTONE_CITY_GALACTIC_WAREHOUSE_LOOKER");
            bag.AddItem(ItemDatabase.Get("HM02")!, 1);
        }

        // ---- Pastoria: the rival's battle at the Gym's door
        StepOnto("Sinnoh", 589, 828, Direction.Up);
        ReadTo("barrelling into you"); Frames(10); Shot("vp22_pastoria_the_rival");
        IntoBattle("the rival");
        Win();
        ReadTo(null);
        Console.WriteLine($"pastoria: state {story.Var("VAR_PASTORIA_CITY_STATE")}");

        // ---- the Fen Badge, won in Crasher Wake's Gym, and the two of them outside its door
        TalkTo("PastoriaGym", "crasher_wake", null);
        IntoBattle("Crasher Wake");
        Win();
        ReadTo(null);
        ComeThrough("Sinnoh", 589, 828, Direction.Down);
        ReadTo("Got the Badge"); Frames(10); Shot("vp23_pastoria_the_rival_waits");
        ReadTo("noise on my doorstep"); Frames(10); Shot("vp24_pastoria_crasher_wake_comes_out");
        ReadTo("A BOMB"); Frames(10); Shot("vp25_pastoria_a_bomb");
        ReadTo(null); Frames(20);
        At("Sinnoh", 609, 813, Direction.Up); Frames(30); Shot("vp26_pastoria_by_the_observatory");

        // ---- the bomb
        StepOnto("Sinnoh", 610, 810, Direction.Up);
        ReadTo("KA-BOOOOOM"); Frames(10); Shot("vp27_pastoria_the_explosion");
        ReadTo("Galactic Bomb"); Frames(10); Shot("vp28_pastoria_the_grunt");
        ReadTo("done it any harm"); Frames(10); Shot("vp29_pastoria_the_grunt_runs");
        ReadTo("before he gets away"); Frames(10); Shot("vp30_pastoria_the_rival_keeps_the_door");
        ReadTo(null); Frames(20);
        StepOnto("Sinnoh", 610, 810, Direction.Up);
        ReadTo("still standing about"); Frames(10); Shot("vp31_pastoria_turned_back");
        ReadTo(null);
        Console.WriteLine($"pastoria: state {story.Var("VAR_PASTORIA_CITY_STATE")}");

        // ---- the grunt runs from Pastoria, and along Route 213
        TalkTo("Sinnoh", "grunt_m", "pastoria_city", -1, 0);
        ReadTo("the lake"); Frames(10); Shot("vp32_pastoria_the_grunt_by_the_gate");
        ReadTo(null);
        ComeThrough("Sinnoh", 647, 812, Direction.Right);
        Frames(30); Shot("vp33_route213_the_grunt");
        TalkTo("Sinnoh", "grunt_m", "route_213", -1, 0);
        ReadTo("How long have you been standing there"); Frames(10); Shot("vp34_route213_caught_listening");
        ReadTo(null);
        TalkTo("Sinnoh", "grunt_m", "route_213", -1, 0);
        ReadTo("blunder"); Frames(10); Shot("vp35_route213_looker");
        ReadTo(null); Frames(20);
        Console.WriteLine($"route 213: grunt gone {story.Has("FLAG_ROUTE_213_GRUNT_M_LEFT")}");

        // ---- Valor Lakefront: the grunt cornered, Cynthia and the Secret Potion
        ComeThrough("Sinnoh", 719, 794, Direction.Up);
        Frames(30); Shot("vp36_valor_the_grunt");
        TalkTo("Sinnoh", "grunt_m", "valor_lakefront");
        ReadTo(null);
        TalkTo("Sinnoh", "grunt_m", "valor_lakefront");
        ReadTo("knock you flat"); Frames(10); Shot("vp37_valor_the_grunt_cornered");
        IntoBattle("the grunt");
        Win();
        ReadTo("We meet again"); Frames(10); Shot("vp38_valor_cynthia");
        ReadTo("relative of yours"); Frames(10); Shot("vp39_valor_the_rival");
        ReadTo("this medicine"); Frames(10); Shot("vp40_valor_the_secret_potion");
        ReadTo(null); Frames(20); Shot("vp41_valor_after");
        Console.WriteLine($"valor: secret potion {bag.GetQuantity(ItemDatabase.Get("Secret Potion")!)}, pastoria state {story.Var("VAR_PASTORIA_CITY_STATE")}");

        // ---- the road to Sunyshore, closed
        StepOnto("Sinnoh", 724, 790, Direction.Right);
        ReadTo("blackout"); Frames(10); Shot("vp42_valor_the_road_closed");
        ReadTo(null); Frames(10);
    }
}
