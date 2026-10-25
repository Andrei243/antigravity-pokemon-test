partial class Harness
{
    /// <summary>
    /// Plan 20 · J13, part c, after part b in the <c>johto</c> mode (so nothing before it moves; <c>JOHTO_FROM=partc</c>
    /// runs it alone in a new game of its own): the Move Relearner of Blackthorn City asked for a move back for a Heart
    /// Scale (<c>jpc01</c> to <c>jpc06</c>), a swarm on Route 35 and one on the water of Route 27 met and told on Professor
    /// Oak's radio (<c>jpc10</c> to <c>jpc13</c>), and the Dragon's Den after the League: the red-haired boy on the island,
    /// Lance and Clair coming down behind the player, the double battle beside the boy and the boy's pledge (<c>jpc20</c>
    /// to <c>jpc29</c>), then Clair's number (<c>jpc30</c> to <c>jpc33</c>). It prints what each left behind (the moves, the
    /// Heart Scale, the swarm's slots, the variables). Written, not run.
    /// </summary>
    void JohtoPartCShots()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);
        var box = game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadTo(string? text, int most = 2400)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && box.IsActive && Translated.Saying(box.CurrentLine, text)) { Until(() => box.IsActive, "text"); box.FinishLine(); Frames(2); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !box.IsActive && game.State == GameState.Overworld) { if (++idle > 60) break; }
                else idle = 0;
                if (text == null && !engine.ScriptRunning && !box.IsActive && game.State == GameState.Overworld) return;
                if (game.State == GameState.Battle) { WinTheBattle(); continue; }
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void WinTheBattle()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    if (fight.EnemyPokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
                    // (the Den's double battle: the champions' and the foes' slots all left a single HP and only Struggle, and
                    // whoever stands for the player kept whole, as the other modes' helpers do)
                    if (fight.IsDouble)
                        foreach (var foe in (fight.EnemyParty?.Members ?? new List<Pokemon>()).Concat(fight.EnemySlots.Select(s => s.Pokemon)))
                            if (foe != null) { foe.CurrentHP = Math.Min(foe.CurrentHP, 1); foreach (var move in foe.Moves) move.CurrentPP = 0; }
                    var lead = fight.PlayerPokemon;
                    foreach (var mine in fight.PlayerSlots.Select(s => s.Pokemon).Append(lead))
                        if (mine != null && !mine.IsFainted) mine.CurrentHP = mine.MaxHP;
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(Math.Max(0, lead == null ? 0 : lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status)));
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
        void TalkTo(string map, string key, int dx = 0, int dy = 1)
        {
            var npc = MapDatabase.Get(map).NPCs.First(n => n.Key == key);
            At(map, npc.GridX + dx, npc.GridY + dy, dy > 0 ? Direction.Up : dy < 0 ? Direction.Down : dx > 0 ? Direction.Left : Direction.Right);
            game.Interact();
        }
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            game.SteppedOutOfWarp = true;
            game.LocationSign.Hide();
            Frames(2);
        }
        void StepOnto(string map, int x, int y, Direction way)
        {
            var (dx, dy) = FieldMovement.Delta(way);
            At(map, x - dx, y - dy, way);
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning, $"the scene at {x},{y}", 120);
            engine.Steering = null;
        }

        // ---- The Move Relearner of Blackthorn City: a Pidgey that has let moves slip, and a Heart Scale
        var pidgey = new Pokemon(PokemonDatabase.Get("Pidgey")!, 30);
        pidgey.Moves.Clear();
        foreach (string m in new[] { "Tackle", "Sand Attack", "Gust", "Quick Attack" }) pidgey.Moves.Add(MoveDatabase.Create(m));
        game.Party.Members.Clear();
        game.Party.Add(pidgey);
        game.Bag.AddItem(ItemDatabase.Get("Heart Scale")!, 1);
        Console.WriteLine($"  Pidgey could remember: {string.Join(", ", Relearning.MovesFor(pidgey))}");
        At("Blackthorn_MoveTutorHouse", 4, 8, Direction.Up); Frames(10); Shot("jpc01_the_relearners_house");
        TalkTo("Blackthorn_MoveTutorHouse", "t30r0601_gsbigman");
        ReadTo("Ah, a young Trainer"); Frames(10); Shot("jpc02_the_relearner");
        ReadTo("Shall I trade it"); Frames(10); Shot("jpc03_the_question");
        ReadTo("Which Pokémon is it to be"); Frames(10); box.Advance(); Frames(2);
        Until(() => game.State == GameState.PartyMenu, "the team screen", 300);
        Frames(20); Shot("jpc04_the_team_screen");
        game.PartyScreen.SelectedIndex = game.Party.Members.IndexOf(pidgey);
        game.PartyScreen.Confirm(game.Party); Frames(3);
        // The list of moves it let slip, and the full set asking which move is to go
        Until(() => engine.Choice.IsOpen, "the list of moves", 600);
        Frames(10); Shot("jpc05_the_moves_it_let_slip");
        ReadTo("remembered"); Frames(10); Shot("jpc06_a_move_remembered");
        ReadTo(null);
        Console.WriteLine($"  Pidgey: {string.Join(", ", pidgey.Moves.Select(m => m.Name))}; Heart Scales {game.Bag.GetQuantity(ItemDatabase.Get("Heart Scale")!)}");

        // ---- A swarm on the day's place: Route 35's grass and Route 27's water, and Professor Oak's bulletin
        game.Encounters.SwarmsOn = true;
        var places = Swarms.Places("Johto");
        foreach (string key in new[] { "route_35", "route_27" })
        {
            uint day = (uint)places.ToList().FindIndex(p => p.Area == key);
            game.Encounters.SwarmDaily = day;
            var moment = game.EncounterMomentNow();
            var (map, x, y) = AreaSpot(key);
            At(map.Name, x, y, Direction.Down); Frames(10);
            Shot(key == "route_35" ? "jpc10_a_swarm_on_route_35" : "jpc12_a_swarm_on_route_27");
            var area = map.AreaAt(x, y)!;
            var grass = EncounterSlots.Grass(area, moment);
            var water = EncounterSlots.Surf(area, area.WaterEncounters, moment);
            Console.WriteLine($"  swarm {places[(int)day].Species} on {key} ({places[(int)day].Way ?? "grass"}): grass {string.Join(", ", grass.Take(3).Select(e => e.SpeciesName))}; "
                + $"water {string.Join(", ", water.Take(2).Select(e => e.SpeciesName))}");
        }
        Console.WriteLine($"  Oak's bulletin: {RadioShows.SwarmLine(new Random(1), "Yanma", "Route 35")}");
        Frames(2); Shot("jpc13_after_the_swarms");

        // ---- The Dragon's Den after the League: the boy, Lance and Clair, the double battle
        game.Story.Set("JohtoHallOfFame");
        game.Party.Members.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Feraligatr")!, 60));
        ComeThrough("DragonsDen", 28, 12, Direction.Down);
        Console.WriteLine($"  the Den: VAR_UNK_40FC {game.Story.Var("VAR_UNK_40FC")}, the boy on the island {MapDatabase.Get("DragonsDen").NPCs.Any(n => n.Key == "d44r0102_gsrivel_2")}");
        At("DragonsDen", 25, 44, Direction.Up); Frames(10); Shot("jpc20_the_island_after_the_league");
        StepOnto("DragonsDen", 24, 43, Direction.Down);
        Frames(30); Shot("jpc21_the_boy_notices");
        ReadTo("laugh at my training"); Frames(10); Shot("jpc22_his_training");
        Frames(40); Shot("jpc23_lance_and_clair_come_down");
        ReadTo("We met at the Team Rocket hideout"); Frames(10); Shot("jpc24_lance");
        ReadTo("Me, with that show-off"); Frames(10); Shot("jpc25_the_boy_refuses_clair");
        ReadTo("Come to my side"); Frames(10); Shot("jpc26_beside_the_boy");
        Until(() => game.State == GameState.Battle, "the double battle", 900);
        Frames(30); Shot("jpc27_the_double_battle");
        ReadTo("See you another time"); Frames(10); Shot("jpc28_lances_farewell");
        ReadTo("toughest challenge"); Frames(10); Shot("jpc29_the_boys_pledge");
        ReadTo(null);
        Console.WriteLine($"  after the scene: VAR_UNK_40FC {game.Story.Var("VAR_UNK_40FC")}, team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}");

        // ---- From then on: the boy trains here on Tuesdays and Thursdays, and Clair on her mornings, her number to be had
        var fixedDay = GameClock.FixedDate;
        GameClock.FixedDate = new DateTime(2026, 6, 2);   // a Tuesday
        ComeThrough("DragonsDen", 28, 12, Direction.Down);
        Console.WriteLine($"  the boy on the bank {MapDatabase.Get("DragonsDen").NPCs.Any(n => n.Key == "d44r0102_gsrivel")}, Clair {MapDatabase.Get("DragonsDen").NPCs.Any(n => n.Key == "d44r0102_gsleader8")} "
            + $"({GameClock.Today.DayOfWeek}, {GameClock.Now})");
        if (MapDatabase.Get("DragonsDen").NPCs.Any(n => n.Key == "d44r0102_gsleader8"))
        {
            TalkTo("DragonsDen", "d44r0102_gsleader8");
            ReadTo("swap numbers"); Frames(10); Shot("jpc30_clairs_number");
            ReadTo("Registered Clair"); Frames(10); Shot("jpc31_clair_registered");
            ReadTo("Friday nights"); Frames(10); Shot("jpc32_friday_nights");
            ReadTo(null);
            Frames(30); Shot("jpc33_clair_gone_home");
        }
        if (MapDatabase.Get("DragonsDen").NPCs.Any(n => n.Key == "d44r0102_gsrivel"))
        {
            TalkTo("DragonsDen", "d44r0102_gsrivel");
            ReadTo("training my Pokémon here"); Frames(10); Shot("jpc34_the_boy_training");
            ReadTo(null);
        }
        Console.WriteLine($"  Clair's number {game.Story.Has(Pokegear.ContactFlag("clair"))}");
        GameClock.FixedDate = fixedDay;
    }
}
