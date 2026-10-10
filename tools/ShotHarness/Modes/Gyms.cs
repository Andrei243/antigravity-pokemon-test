partial class Harness
{
    // ---------------------------------------------------------------- the Gyms and their puzzles (plan 01 · M9)

    // The Gyms rebuilt to the original's plans with their puzzles (not part of `all`), each from its door, its puzzle
    // in its states, a trainer's battle starting and the Leader: the Eterna Gym's flower clock turning and its
    // fountains draining, the Hearthome Gym's dark rooms and their doors, the Veilstone Gym's punching bags, the
    // Pastoria Gym's water rising and falling. It wins every battle and prints what each puzzle left behind.
    public void GymsMode()
    {
        var story = game.Story;
        var team = game.Party;
        team.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Infernape")!, 60));
        game.LocationSign.Hide();

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        // A frame of the game, drawn only every sixth time: the puzzles' motions are functions of time, so the frames
        // between only cost time under software rendering
        int quiet = 0;
        void Tick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            if (++quiet % 6 == 0) engine.Draw();
        }
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Tick();
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 200 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty.Members) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    // A Gym is a long run of battles: the lead's moves never run dry, and it uses the first that touches
                    // the foe (Close Combat does nothing to a Ghost)
                    var lead = fight.PlayerPokemon;
                    foreach (var move in lead.Moves) move.CurrentPP = move.MaxPP;
                    var target = fight.EnemyPokemon;
                    int pick = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status
                        && TypeChart.GetEffectiveness(m.Data.Type, target.PrimaryType, target.SecondaryType) > 0f);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(Math.Max(0, pick));
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => State() == GameState.Overworld, "the field again", 1500);
            Frames(10);
        }
        // Reads on until the script has ended, winning its battles; or, given a test, until it holds
        void ReadOn(Func<bool>? until = null, int most = 3000)
        {
            for (int i = 0; i < most; i++)
            {
                if (until != null && until()) return;
                if (until == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (engine.Choice.IsOpen) { Skip(10 / 60.0); engine.Choice.Confirm(); Skip(4 / 60.0); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Tick(); Tick(); }
                else Tick();
            }
            Console.WriteLine("  !! the script never got there");
        }
        void IntoBattle(string what)
        {
            ReadOn(() => State() == GameState.Battle, 1500);
            if (State() != GameState.Battle) Console.WriteLine($"  !! never happened: {what}");
            else Skip(1.6);
        }
        // Comes in by the door, as the game does: the room's puzzle is laid out
        void Enter(string map, int x, int y)
        {
            At(map, x, y, Direction.Up);
            game.ArriveOnMap();
            Frames(2);
            ReadOn();
        }
        void TalkFrom(string map, string key, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.Interact();
        }
        // `gyms oreburgh eterna veilstone hearthome pastoria`: only the Gyms named
        var only = args.Length > 2 ? args[2..] : null;
        bool Want(string gym) => only == null || only.Contains(gym);

        // ---- the Oreburgh Gym: its tiers of rock, rebuilt to the original's heights
        if (Want("oreburgh"))
        {
        Enter("OreburghGym", 5, 24);
        Frames(20); Shot("g50_oreburgh_door");
        At("OreburghGym", 5, 19, Direction.Up); Frames(4); Shot("g51_oreburgh_the_pit_and_the_bridge");
        TalkFrom("OreburghGym", "youngster_jonathon", 5, 18, Direction.Left);
        IntoBattle("Jonathon's battle"); Shot("g52_oreburgh_battle_jonathon");
        ReadOn();
        At("OreburghGym", 2, 18, Direction.Up); Frames(4); Shot("g53_oreburgh_up_the_west_stairs");
        // Onto the bridge from the west tier, by the field's own steps (placed on its tile, the player would stand in the pit under it)
        At("OreburghGym", 3, 17, Direction.Right); Frames(2);
        engine.Steering = (Direction.Right, false); Frames(30); engine.Steering = null; Frames(10);
        Console.WriteLine($"oreburgh: on the bridge at {game.Player.GridX},{game.Player.GridY}, height {game.Player.HeightOn(game.Map)}");
        Shot("g54_oreburgh_on_the_bridge");
        TalkFrom("OreburghGym", "youngster_darius", 6, 11, Direction.Right);
        IntoBattle("Darius's battle");
        ReadOn();
        At("OreburghGym", 5, 9, Direction.Up); Frames(4); Shot("g55_oreburgh_below_the_dais");
        var rock = game.Map;
        Console.WriteLine($"oreburgh: heights at the door {rock.HeightAt(5, 24)}, the pit {rock.HeightAt(3, 16)}, the bridge {rock.DeckAt(5, 17)}, " +
            $"the second tier {rock.HeightAt(5, 10)}, the dais {rock.HeightAt(5, 4)}");
        TalkFrom("OreburghGym", "roark", 5, 4, Direction.Up);
        ReadOn(() => Box().IsActive && Box().CurrentLine.Contains("Roark")); Box().FinishLine(); Frames(4); Shot("g56_oreburgh_roark");
        IntoBattle("Roark's battle"); Shot("g57_oreburgh_battle_roark");
        ReadOn();
        At("OreburghGym", 5, 21, Direction.Up); Frames(4); Shot("g58_oreburgh_from_the_mat");
        Console.WriteLine($"oreburgh: Coal Badge {story.HasBadge(Badge.Coal)}");
        }

        // ---- the Eterna Gym: the flower clock
        if (Want("eterna"))
        {
        Enter("EternaGym", 11, 27);
        Frames(20); Shot("g01_eterna_door");
        At("EternaGym", 11, 19, Direction.Up); Frames(4); Shot("g02_eterna_clock_at_twenty_five_past_seven");
        TalkFrom("EternaGym", "lass_caroline", 13, 22, Direction.Right);
        IntoBattle("Caroline's battle"); Shot("g03_eterna_battle_caroline");
        ReadOn(() => game.ClockTurn != null);
        Skip(1.5); Shot("g04_eterna_clock_turning");
        ReadOn();
        Shot("g05_eterna_quarter_past_six");
        Console.WriteLine($"eterna: clock {story.Var(EternaClock.StateVar)}, trainers {story.Var(EternaClock.TrainersVar)}");
        // Up the hour hand: the hop over its tip
        At("EternaGym", 11, 19, Direction.Up); Frames(2);
        engine.Steering = (Direction.Up, false); Frames(6); engine.Steering = null; Shot("g06_eterna_hop_over_the_tip"); Frames(30);
        var walker = game.Player;
        Console.WriteLine($"eterna: after the hop the player stands at {walker.GridX},{walker.GridY}");
        TalkFrom("EternaGym", "aroma_lady_jenna", 20, 16, Direction.Down);
        ReadOn(() => game.ClockTurn != null);
        ReadOn(() => game.ClockTurn is EternaClock.Turn { Draining: true });
        Skip(1.0); Shot("g07_eterna_right_fountain_drains");
        ReadOn();
        Shot("g08_eterna_quarter_past_nine");
        TalkFrom("EternaGym", "aroma_lady_angela", 2, 8, Direction.Up);
        ReadOn();
        At("EternaGym", 11, 12, Direction.Up); Frames(4); Shot("g09_eterna_quarter_to_one");
        TalkFrom("EternaGym", "gardenia", 11, 4, Direction.Up);
        ReadOn(() => Box().IsActive && Box().CurrentLine.Contains("Gardenia")); Box().FinishLine(); Frames(4); Shot("g10_eterna_gardenia");
        IntoBattle("Gardenia's battle"); Shot("g11_eterna_battle_gardenia");
        ReadOn();
        At("EternaGym", 11, 20, Direction.Up); Frames(4); Shot("g12_eterna_half_past_twelve");
        Console.WriteLine($"eterna: Forest Badge {story.HasBadge(Badge.Forest)}, clock {story.Var(EternaClock.StateVar)}, TM86 {game.Bag.GetQuantity(ItemDatabase.Get("TM86")!)}");
        }

        Map Here() => game.Map;
        // Walks one way until the player is somewhere else (through a door) and the fade has opened again
        void WalkInto(Direction way)
        {
            var from = Here();
            engine.Steering = (way, false);
            Until(() => Here() != from, "a way through", 240);
            engine.Steering = null;
            Until(() => State() == GameState.Overworld, "the field after the door", 240);
            Frames(30);
            ReadOn();
        }

        // ---- the Veilstone Gym: the punching bags
        if (Want("veilstone"))
        {
        Enter("VeilstoneGym", 12, 30);
        Frames(20); Shot("g21_veilstone_door");
        // A black belt's battle, then the rest kept from walking up while the bags are kicked
        TalkFrom("VeilstoneGym", "black_belt_colby", 16, 23, Direction.Left);
        IntoBattle("Colby's battle"); Shot("g22_veilstone_battle_black_belt");
        ReadOn();
        foreach (var npc in Here().Everyone.Where(n => n.IsTrainer)) { npc.HasBattled = true; story.Defeat(npc.TrainerData?.Id ?? npc.Key); }
        // The kicks that open the way to Maylene (GymTests.SolveVeilstone found them), the first and the last shown
        // before, while the bag runs and after its stack has fallen
        var kicks = new (int X, int Y, Direction Way)[]
        {
            (15, 26, Direction.Right), (3, 10, Direction.Down), (3, 14, Direction.Right), (8, 20, Direction.Up), (8, 17, Direction.Left),
            (3, 17, Direction.Down), (4, 22, Direction.Up), (4, 12, Direction.Right), (20, 17, Direction.Up), (16, 10, Direction.Left),
            (13, 10, Direction.Left), (8, 7, Direction.Right)
        };
        for (int k = 0; k < kicks.Length; k++)
        {
            var (bx, by, way) = kicks[k];
            var (dx, dy) = FieldMovement.Delta(way);
            bool show = k == 0 || k == kicks.Length - 1;
            string n = k == 0 ? "first" : "last";
            At("VeilstoneGym", bx - dx, by - dy, way);
            if (show) { Frames(4); Shot($"g23_veilstone_{n}_bag_before"); }
            game.Interact();
            if (show) { Skip(1.4); Shot($"g24_veilstone_{n}_bag_running"); }
            Until(() => !game.BagRunning, "the bag's run");
            if (show) { Frames(20); Shot($"g25_veilstone_{n}_bag_after"); }
            ReadOn();
        }
        var dojo = Here();
        Console.WriteLine($"veilstone: {dojo.NPCs.Count(VeilstoneBags.IsTireStack)} stacks standing, bags at " +
            string.Join(" ", dojo.NPCs.Where(VeilstoneBags.IsBag).Select(b => $"{b.GridX},{b.GridY}")));
        At("VeilstoneGym", 12, 8, Direction.Up); Frames(4); Shot("g26_veilstone_the_way_open");
        TalkFrom("VeilstoneGym", "maylene", 12, 5, Direction.Up);
        ReadOn(() => Box().IsActive && Box().CurrentLine.Contains("Maylene")); Box().FinishLine(); Frames(4); Shot("g27_veilstone_maylene");
        IntoBattle("Maylene's battle"); Shot("g28_veilstone_battle_maylene");
        ReadOn();
        Console.WriteLine($"veilstone: Cobble Badge {story.HasBadge(Badge.Cobble)}, TM60 {game.Bag.GetQuantity(ItemDatabase.Get("TM60")!)}");
        }

        // ---- the Pastoria Gym: the water and its buttons
        if (Want("pastoria"))
        {
        Enter("PastoriaGym", 13, 41);
        Frames(20); Shot("g60_pastoria_door");
        PastoriaWater Pool() => (PastoriaWater)Here().Puzzle!;
        // Onto a button by the field's own step, from whichever side a step reaches it; the room's trigger presses it
        void StepOnto(int x, int y)
        {
            foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var (dx, dy) = FieldMovement.Delta(way);
                int fx = x - dx, fy = y - dy;
                if (!Here().IsWalkable(fx, fy)) continue;
                var step = FieldMovement.Step(Here(), fx, fy, way, new Walker(TravelMode.OnFoot, Here().HeightAt(fx, fy)));
                if (!step.Moves || (step.X, step.Y) != (x, y)) continue;
                At("PastoriaGym", fx, fy, way); Frames(2);
                engine.Steering = (way, false);
                Until(() => game.Player.GridX == x && game.Player.GridY == y, "the step onto the button", 120);
                engine.Steering = null;
                return;
            }
            Console.WriteLine($"  !! no step onto the button at {x},{y}");
        }
        At("PastoriaGym", 13, 36, Direction.Up); Frames(4); Shot("g61_pastoria_the_water_at_two");
        // Up onto the floating floor: the middle ground, then a raft carried at the water's height
        engine.Steering = (Direction.Up, false); Frames(40); engine.Steering = null; Frames(6);
        Console.WriteLine($"pastoria: afloat at {game.Player.GridX},{game.Player.GridY}, height {game.Player.HeightOn(Here())}");
        Shot("g62_pastoria_on_a_raft");
        // The orange button lowers it: the water on its way down, and the pool drained
        StepOnto(3, 34);
        Skip(1.0); Shot("g63_pastoria_the_water_falling");
        Until(() => !Pool().Moving, "the water at its level");
        Frames(10); Shot("g64_pastoria_the_water_at_nought");
        Console.WriteLine($"pastoria: after the orange button the water is at {Pool().Level}");
        // The blue raises it to the top
        StepOnto(9, 24);
        Until(() => !Pool().Moving, "the water at its level");
        Frames(10); Shot("g65_pastoria_the_water_at_four");
        Console.WriteLine($"pastoria: after the blue button the water is at {Pool().Level}");
        TalkFrom("PastoriaGym", "sailor_damian", 7, 23, Direction.Up);
        IntoBattle("Damian's battle"); Shot("g66_pastoria_battle_sailor");
        ReadOn();
        foreach (var npc in Here().Everyone.Where(n => n.IsTrainer)) { npc.HasBattled = true; story.Defeat(npc.TrainerData?.Id ?? npc.Key); }
        Pool().Settle(PastoriaWater.Button.Blue);
        At("PastoriaGym", 12, 5, Direction.Up); Frames(4); Shot("g67_pastoria_by_wake");
        TalkFrom("PastoriaGym", "crasher_wake", 13, 5, Direction.Up);
        ReadOn(() => Box().IsActive && Box().CurrentLine.Contains("Wake")); Box().FinishLine(); Frames(4); Shot("g68_pastoria_wake");
        IntoBattle("Wake's battle"); Shot("g69_pastoria_battle_wake");
        ReadOn();
        Console.WriteLine($"pastoria: Fen Badge {story.HasBadge(Badge.Fen)}, TM55 {game.Bag.GetQuantity(ItemDatabase.Get("TM55")!)}");
        }

        // ---- the Hearthome Gym: the dark rooms and their doors
        if (Want("hearthome"))
        {
        At("HearthomeGym", 4, 8, Direction.Up);
        game.ArriveOnMap();
        ReadOn(() => Box().IsActive, 600); Box().FinishLine(); Frames(4); Shot("g31_hearthome_guide");
        ReadOn();
        At("HearthomeGym", 4, 6, Direction.Up); Frames(4); Shot("g32_hearthome_entrance");
        At("HearthomeGym", 4, 3, Direction.Up);
        WalkInto(Direction.Up);
        Shot("g33_hearthome_first_room");
        HearthomeDoors Doors() => (HearthomeDoors)Here().Puzzle!;
        Console.WriteLine($"hearthome: first room {Here().Name}, the way on is the {Doors().Correct}, its sign at {Doors().Clue}");
        // Seen from beside it: something small right behind the player is behind their head
        void Beside((int X, int Y) spot)
        {
            bool west = Here().IsWalkable(spot.X - 1, spot.Y);
            At(Here().Name, spot.X + (west ? -1 : 1), spot.Y, west ? Direction.Right : Direction.Left);
        }
        var clue = Doors().Clue;
        Beside(clue); Frames(4); Shot("g34_hearthome_the_sign_on_the_floor");
        TalkFrom(Here().Name, "lass_molly", 5, 7, Direction.Left);
        IntoBattle("Molly's battle"); Shot("g35_hearthome_battle");
        ReadOn();
        // A wrong door, and back at the entrance
        var wrong = Doors().Doors.First(d => d.Sign != Doors().Correct);
        At(Here().Name, wrong.X, 3, Direction.Up); Frames(4); Shot("g36_hearthome_before_a_wrong_door");
        WalkInto(Direction.Up);
        Shot("g37_hearthome_back_at_the_entrance");
        Console.WriteLine($"hearthome: the {wrong.Sign} door led to {Here().Name} {game.Player.GridX},{game.Player.GridY}");
        // In again (another door is chosen), through the right one, and on through the second room
        At("HearthomeGym", 4, 3, Direction.Up);
        WalkInto(Direction.Up);
        var right = Doors().Doors.First(d => d.Sign == Doors().Correct);
        At(Here().Name, right.X, 3, Direction.Up);
        WalkInto(Direction.Up);
        Shot("g38_hearthome_second_room");
        clue = Doors().Clue;
        Beside(clue); Frames(4); Shot("g39_hearthome_second_sign");
        Console.WriteLine($"hearthome: second room, the way on is the {Doors().Correct}, its sign at {clue}");
        right = Doors().Doors.First(d => d.Sign == Doors().Correct);
        At(Here().Name, right.X, 3, Direction.Up);
        WalkInto(Direction.Up);
        Shot("g40_hearthome_fantinas_room");
        TalkFrom("HearthomeGymLeaderRoom", "fantina", 4, 11, Direction.Up);
        Skip(0.3); Shot("g41_hearthome_fantina_twirls");
        ReadOn(() => Box().IsActive && Box().CurrentLine.Contains("Fantina")); Box().FinishLine(); Frames(4); Shot("g42_hearthome_fantina");
        IntoBattle("Fantina's battle"); Shot("g43_hearthome_battle_fantina");
        ReadOn();
        At("HearthomeGymLeaderRoom", 7, 10, Direction.Right); Frames(4); Shot("g44_hearthome_bollards_gone");
        Console.WriteLine($"hearthome: Relic Badge {story.HasBadge(Badge.Relic)}, TM65 {game.Bag.GetQuantity(ItemDatabase.Get("TM65")!)}, bollards {Here().NPCs.Count(n => n.Key.StartsWith("bollard"))}");
        Enter("HearthomeGymRoom1", 8, 10);
        Frames(4); Shot("g45_hearthome_first_room_after_the_badge");
        }
    }
}
