partial class Harness
{
    // ---- part c's rooms at work (`in_z*`; plan 01 · M11, part c: the north and the Battle Zone), through the game's
    // own TryInteract, steps, question boxes, team screen and battles: Celestic Town's shop over its counter and the
    // man in glasses, a rest in the Snowbound Lodge's bed, the hiker's Icicle Plate, the woman of the snow and her
    // house empty afterwards, the fisherman measuring a Remoraid, the Pikachu Fan Club and the trainer among its
    // Pikachu, the inventor's app, the Item Expert's list, the Meister's trade, a pile of books read, and the Ribbon
    // Syndicate's lift ridden up to the spa, a treatment there, and the ride back down. It prints what each left behind.
    void InteriorsAtWorkC()
    {
        var team = game.Party;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // A battle won at once: every foe brought down to 1 HP and struck, the team kept well
        bool Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 120 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var mine in team.Members.Where(p => p.CurrentHP > 0)) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
                    foreach (var foe in fight.Trainers.SelectMany(t => t.Party.Members)) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    foreach (var foe in fight.Core.EnemySlots.Where(b => b.Pokemon != null)) foe.Pokemon!.CurrentHP = Math.Min(foe.Pokemon.CurrentHP, 1);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(0);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            if (!fight.IsBattleOver) { Console.WriteLine("  !! a battle that wouldn't end"); return false; }
            Until(() => game.State != GameState.Battle, "the field again", 1500);
            foreach (var mine in team.Members) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
            Frames(10);
            return game.State != GameState.Battle;
        }
        // Reads every line on to the end of the script, shooting the first line that holds each word given (a word to a
        // shot); each question takes the next of the answers (the first once they run out), the Pokémon at `choose` is
        // picked when one is asked for, and every battle is won (shot at its menu, if a shot is named for it)
        void ReadOn((string Word, string Shot)[] shots, int[]? answers = null, string? battleShot = null, int choose = 0)
        {
            var left = shots.ToList();
            int answered = 0;
            void Look()
            {
                var hit = left.FirstOrDefault(s => s.Word.Split('|').Any(word => Box().VisibleText.Contains(word, StringComparison.Ordinal)));
                if (hit.Shot == null) return;
                Shot(hit.Shot);
                left.Remove(hit);
            }
            for (int i = 0; i < 6000; i++)
            {
                bool busy = engine.ScriptRunning || Box().IsActive || engine.Choice.IsOpen || game.State is GameState.Dialogue or GameState.Battle or GameState.PartyMenu;
                if (!busy && i > 4) break;
                if (game.State == GameState.Battle)
                {
                    for (int k = 0; k < 40 && game.Battle.HUD.MenuState != BattleMenuState.Main && !game.Battle.IsBattleOver; k++) { Confirm(game.Battle); Skip(0.5); }
                    if (battleShot != null) { Skip(1.5); Shot(battleShot); battleShot = null; }
                    if (!Win()) return;
                    continue;
                }
                if (game.State == GameState.PartyMenu)
                {
                    Frames(10);
                    game.PartyScreen.SelectedIndex = choose;
                    game.PartyScreen.Confirm(team); Frames(3);
                    continue;
                }
                if (engine.Choice.IsOpen)
                {
                    Frames(24);
                    Look();
                    int answer = answers != null && answered < answers.Length ? answers[answered] : 0;
                    answered++;
                    for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                    engine.Choice.Confirm(); Frames(4);
                    continue;
                }
                if (!Box().IsActive) { Frames(1); continue; }
                Box().FinishLine(); Frames(2);
                if (engine.Choice.IsOpen) continue;
                Look();
                Box().Advance(); Frames(2);
            }
            foreach (var (word, shot) in left) Console.WriteLine($"  !! never said \"{word}\" for {shot}");
            Until(() => game.State == GameState.Overworld && !Box().IsActive, "the text gone");
        }
        void Talk(string map, int x, int y, Direction facing) { At(map, x, y, facing); game.Interact(); Frames(3); }
        void Arrive(string map, int x, int y, Direction facing) { At(map, x, y, facing); game.ArriveOnMap(); Frames(4); }
        int Have(string item) => inventory.GetQuantity(ItemDatabase.Get(item)!);
        Pokemon Strong(string species, string move)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, 55);
            p.Moves.Clear();
            p.Moves.Add(new Move(MoveDatabase.Get(move)!));
            return p;
        }
        // (whatever a room's arrival was still saying as the sheets were shot is read to its end first)
        ReadOn(Array.Empty<(string, string)>());
        // A lead whose first move strikes
        team.Clear();
        var lead = Strong("Torterra", "Razor Leaf");
        team.Add(lead);
        game.Money = Math.Max(game.Money, 20000);

        // Celestic Town's shop: the old man's greeting over the counter, and his counter of the town's own things
        var shop = MapDatabase.Get("CelesticTownNorthwestHouse");
        var keeper = shop.Everyone.First(n => n.Key == "expert_m");
        Arrive("CelesticTownNorthwestHouse", 4, 8, Direction.Up);
        Talk("CelesticTownNorthwestHouse", keeper.GridX, keeper.GridY + 2, Direction.Up);
        Until(() => Box().IsActive, "the shopkeeper's greeting", 240);
        Box().FinishLine(); Frames(2); Shot("in_z01_celestic_shopkeeper");
        Box().Advance();
        Until(() => game.State == GameState.Shop, "the counter", 240);
        Frames(30); Shot("in_z02_celestic_counter");
        Console.WriteLine($"  Celestic's shop: the state is {game.State}, the counter the old man's own: {keeper.Mart}");
        game.Shop.Close(); Frames(6);
        ReadOn(Array.Empty<(string, string)>());

        // The man in glasses: the pair of the hour (the harness's clock is noon: Black Glasses)
        Talk("CelesticTownNorthwestHouse", 7, 7, Direction.Up);
        ReadOn(new[] { ("glasses", "in_z03_glasses"), ("received", "in_z04_glasses_given") });
        Console.WriteLine($"  the man in glasses at {GameClock.Now}: Choice Specs {Have("Choice Specs")}, Black Glasses {Have("Black Glasses")}, Wise Glasses {Have("Wise Glasses")}");

        // The Snowbound Lodge: a rest in its bed, looked at from beside it
        lead.CurrentHP = 1;
        Arrive("Route216House", 4, 8, Direction.Up);
        Talk("Route216House", 7, 6, Direction.Right);
        ReadOn(new[] { ("bed", "in_z05_lodge_bed"), ("rested", "in_z06_lodge_rested") });
        Console.WriteLine($"  the lodge's bed: {lead.DisplayName} at {lead.CurrentHP} of {lead.MaxHP} HP");

        // Route 217: the hiker and his Hidden Machine
        if (Have("HM08") == 0) inventory.AddItem(ItemDatabase.Get("HM08")!, 1);
        Talk("Route217WestHouse", 4, 6, Direction.Up);
        ReadOn(new[] { ("Hidden Machine", "in_z07_hiker"), ("ancient", "in_z08_icicle_plate") });
        Console.WriteLine($"  the hiker: Icicle Plate {Have("Icicle Plate")}, HM08 {Have("HM08")}, his flag {game.Story.Has("FLAG_RECEIVED_ROUTE_217_WEST_HOUSE_ICICLE_PLATE")}");

        // Route 217: the woman of the snow, her gift, and the house when it is come to again
        Arrive("Route217NortheastHouse", 4, 8, Direction.Up);
        Frames(4); Shot("in_z09_woman_of_the_snow");
        Talk("Route217NortheastHouse", 4, 6, Direction.Up);
        ReadOn(new[] { ("rare", "in_z0a_spell_tag") });
        Console.WriteLine($"  the woman of the snow: Spell Tag {Have("Spell Tag")}, still there this visit: {game.Map.NPCs.Any(n => n.Key == "pokemon_breeder_f")}");
        Arrive("Sinnoh", 316, 286, Direction.Down);
        Arrive("Route217NortheastHouse", 4, 8, Direction.Up);
        Frames(4); Shot("in_z0b_house_empty");
        Console.WriteLine($"  the house come to again: {game.Map.NPCs.Count} people in it");

        // Route 222: the fisherman measures a Remoraid (the harness's chance is seeded, so its size is the same every run)
        team.Clear();
        var remoraid = new Pokemon(PokemonDatabase.Get("Remoraid")!, 30);
        team.Add(remoraid);
        team.Add(lead);
        Arrive("Route222EastHouse", 4, 8, Direction.Up);
        Talk("Route222EastHouse", 6, 5, Direction.Left);
        ReadOn(new[] { ("dream", "in_z10_fisherman"), ("measures|comes to|been here", "in_z11_remoraid_measured") });
        Console.WriteLine($"  the fisherman: this Remoraid is {SizeContest.Told(SizeContest.Millimetres(remoraid))} (factor {SizeContest.Factor(remoraid)}), the record now {SizeContest.Told(SizeContest.Millimetres(remoraid.Height, SizeContest.Record(game.Story)))}, Net Balls {Have("Net Ball")}");

        // A bigger one (its personality and IVs set as NorthInteriorTests works them out: 70.0 cm) beats the record
        remoraid.Personality = 0xABCD1234;
        (remoraid.IvHP, remoraid.IvAttack, remoraid.IvDefense, remoraid.IvSpeed, remoraid.IvSpAttack, remoraid.IvSpDefense) = (31, 10, 5, 20, 7, 12);
        Talk("Route222EastHouse", 6, 5, Direction.Left);
        ReadOn(new[] { ("measures", "in_z11b_remoraid_record"), ("received", "in_z11c_net_ball") });
        Console.WriteLine($"  a bigger one: {SizeContest.Told(SizeContest.Millimetres(remoraid))}, the record now {SizeContest.Told(SizeContest.Millimetres(remoraid.Height, SizeContest.Record(game.Story)))}, Net Balls {Have("Net Ball")}");

        // The Pikachu Fan Club, and the Poké Kid among its Pikachu (spoken to from beside her: someone that small is
        // behind the player's own head from the tile below)
        team.Clear();
        team.Add(lead);
        Arrive("Route222WestHouse", 4, 8, Direction.Up);
        Frames(20); Shot("in_z12_pikachu_fan_club");
        var janet = game.Map.Everyone.First(n => n.Key == "poke_kid_janet");
        Talk("Route222WestHouse", janet.GridX + 1, janet.GridY, Direction.Left);
        ReadOn(new[] { ("Trainer", "in_z13_janet") }, battleShot: "in_z14_janet_battle");
        Console.WriteLine($"  the Pikachu Fan Club: {game.Map.Everyone.Count(n => n.IsPokemon)} Pikachu, Janet beaten {game.Story.HasDefeated("poke_kid_janet")}");

        // Sunyshore City's inventor: his word, then an app for a Pokémon of the nature he asks for
        lead.Nature = Nature.Serious;
        Talk("SunyshoreCityEastHouse", 5, 6, Direction.Up);
        ReadOn(new[] { ("Pokétch", "in_z15_inventor") });
        Talk("SunyshoreCityEastHouse", 5, 6, Direction.Up);
        ReadOn(new[] { ("Serious", "in_z16_calendar_app") });
        Console.WriteLine($"  the inventor: the Calendar {game.Poketch.Has(PoketchApp.Calendar)}, the Dot Artist {game.Poketch.Has(PoketchApp.DotArt)}, the Roulette {game.Poketch.Has(PoketchApp.Roulette)}");

        // The Fight Area's Item Expert: his list, and one item told of
        Talk("FightAreaSouthHouse", 8, 7, Direction.Up);
        ReadOn(new[] { ("Which item", "in_z17_item_expert"), ("Life Orb", "in_z18_life_orb") }, answers: new[] { 3 });

        // The Meister: his welcome, the trade asked for, and his Magikarp for a Finneon
        team.Clear();
        team.Add(new Pokemon(PokemonDatabase.Get("Finneon")!, 30));
        team.Add(lead);
        Arrive("Route226House", 4, 8, Direction.Up);
        Talk("Route226House", 3, 5, Direction.Left);
        ReadOn(new[] { ("Meister.", "in_z19_meister"), ("precious Magikarp", "in_z1a_trade_asked"), ("Danke", "in_z1b_traded") });
        Console.WriteLine($"  the Meister: the team is {string.Join(", ", team.Members.Select(p => $"{p.Nickname} the {p.Species.Name}"))}; traded {game.Story.Has("FLAG_TRADED_FOR_FOPPA_MAGIKARP")}");

        // Route 228's old gentleman, and one of his piles of books read
        team.Clear();
        team.Add(lead);
        Arrive("Route228SouthHouse", 4, 8, Direction.Up);
        Talk("Route228SouthHouse", 5, 4, Direction.Up);
        ReadOn(new[] { ("books", "in_z1c_book_pile") });

        // The Ribbon Syndicate: a member walks into the lift's doors, asks for the floor above, and comes out in the spa
        bool member = game.Story.Has("FLAG_JOINED_RIBBON_SYNDICATE");
        game.Story.Set("FLAG_JOINED_RIBBON_SYNDICATE");
        Arrive("ResortAreaRibbonSyndicate1F", 5, 4, Direction.Up);
        Until(() => !engine.ScriptRunning && game.State == GameState.Overworld, "the hostess aside", 600);
        Frames(4); Shot("in_z20_lift_doors");
        game.Player.SetPosition(5, 3, Direction.Up);
        engine.Steering = (Direction.Up, false);
        Until(() => game.Player.IsMoving || game.State != GameState.Overworld, "a step into the lift", 120);
        engine.Steering = null;
        Until(() => game.Map.Name == "ResortAreaRibbonSyndicateElevator" && game.State == GameState.Overworld, "the lift's car", 400);
        Frames(20); Shot("in_z21_lift_car");
        Console.WriteLine($"  into the lift: {game.Map.Name} at {game.Player.GridX},{game.Player.GridY} facing {game.Player.Facing}; the lift upstairs: {game.Story.Has("FLAG_RIBBON_SYNDICATE_LIFT_UPSTAIRS")}");
        game.Player.SetPosition(2, 6, Direction.Left);
        game.Interact(); Frames(3);
        ReadOn(new[] { ("Which floor", "in_z22_lift_floor"), ("Going up", "in_z23_lift_going_up"), ("Here we are", "in_z24_lift_arrived") });
        game.Player.SetPosition(3, 6, Direction.Down);
        engine.Steering = (Direction.Down, false);
        Until(() => game.Map.Name != "ResortAreaRibbonSyndicateElevator" && game.State == GameState.Overworld, "the floor the lift came to", 400);
        engine.Steering = null;
        Frames(20); Shot("in_z25_spa_floor");
        Console.WriteLine($"  out of the lift: {game.Map.Name} at {game.Player.GridX},{game.Player.GridY} facing {game.Player.Facing}; the lift upstairs: {game.Story.Has("FLAG_RIBBON_SYNDICATE_LIFT_UPSTAIRS")}");

        // The spa: the offer, the walk to the chair, the attendants closed in, and the treatment's end
        At("ResortAreaRibbonSyndicate2F", 20, 10, Direction.Up); Frames(3); Shot("in_z26_spa_bays");
        lead.Friendship = 100;
        var who = game.Map.Everyone.First(n => n.Key == "receptionist_3");
        Talk("ResortAreaRibbonSyndicate2F", who.GridX, who.GridY + 1, Direction.Up);
        ReadOn(new[] { ("spa treatment", "in_z27_spa_offer"), ("ready to begin", "in_z28_spa_chair"), ("All done", "in_z29_spa_done") });
        Frames(6); Shot("in_z2a_spa_afterwards");
        Console.WriteLine($"  the spa: {lead.DisplayName}'s friendship 100 then {lead.Friendship}; the player at {game.Player.GridX},{game.Player.GridY}; the attendants at {string.Join(" ", game.Map.Everyone.OrderBy(n => n.Key).Select(n => $"{n.GridX},{n.GridY}"))}; the day's flag {game.Story.Has("FLAG_DAILY_GOT_SPA_TREATMENT_RESORT_AREA_RIBBON_SYNDICATE_2F")}");
        Talk("ResortAreaRibbonSyndicate2F", who.GridX - 1, who.GridY, Direction.Right);
        ReadOn(new[] { ("one spa session", "in_z2b_spa_tomorrow") });

        // And back down
        game.Player.SetPosition(5, 3, Direction.Up);
        engine.Steering = (Direction.Up, false);
        Until(() => game.Player.IsMoving || game.State != GameState.Overworld, "a step into the lift", 120);
        engine.Steering = null;
        Until(() => game.Map.Name == "ResortAreaRibbonSyndicateElevator" && game.State == GameState.Overworld, "the lift's car", 400);
        game.Player.SetPosition(2, 6, Direction.Left);
        game.Interact(); Frames(3);
        ReadOn(new[] { ("Going down", "in_z2c_lift_going_down") }, answers: new[] { 1 });
        game.Player.SetPosition(3, 6, Direction.Down);
        engine.Steering = (Direction.Down, false);
        Until(() => game.Map.Name != "ResortAreaRibbonSyndicateElevator" && game.State == GameState.Overworld, "the floor the lift came to", 400);
        engine.Steering = null;
        Until(() => !engine.ScriptRunning && game.State == GameState.Overworld, "the ground floor's own arrival", 600);
        Frames(20); Shot("in_z2d_ground_floor_again");
        Console.WriteLine($"  back down: {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}; the lift upstairs: {game.Story.Has("FLAG_RIBBON_SYNDICATE_LIFT_UPSTAIRS")}");
        if (!member) game.Story.Unset("FLAG_JOINED_RIBBON_SYNDICATE");
    }
}
