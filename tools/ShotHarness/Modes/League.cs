partial class Harness
{
    // ---------------------------------------------------------------- the Pokémon League's rooms (plan 01 · M9, part 2d)

    // The League from its Pokémon Center to the Hall of Fame (not part of `all`; lg*), through the game's own
    // TryInteract, ArriveOnMap, steps and scripts: the door's guard counting the Badges and stepping aside, a passage
    // and its platform rising, each room of the Elite Four from its way in with the door shut behind, its master's
    // greeting and battle and the door on open, the Champion's room, her battle and the platform up to the gallery,
    // the hallway with Cynthia and the professor, the Hall of Fame and its record, and home. It wins every battle and
    // prints what each room left behind. Name rooms to shoot only those: `league aaron flint champion`.
    public void LeagueMode()
    {
        var story = game.Story;
        var team = game.Party;
        team.Members.Insert(0, new Pokemon(PokemonDatabase.Get("Garchomp")!, 75));
        game.LocationSign.Hide();

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        Map Here() => game.Map;
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
            for (int guard = 0; guard < 300 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty.Members) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    // The League is five battles in a row with no nurse between: the lead never tires
                    var lead = fight.PlayerPokemon;
                    lead.CurrentHP = lead.MaxHP;
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
            Until(() => State() != GameState.Battle && State() != GameState.Transition, "the field again", 1500);
            Frames(10);
        }
        // Reads on until the script has ended, winning its battles; or, given a test, until it holds
        void ReadOn(Func<bool>? until = null, int most = 4000)
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
        bool Says(string word) => Box().IsActive && Box().CurrentLine.Contains(word);
        void Line(string word, string shot)
        {
            ReadOn(() => Says(word));
            Box().FinishLine(); Frames(4); Shot(shot);
        }
        void IntoBattle(string what)
        {
            ReadOn(() => State() == GameState.Battle, 1500);
            if (State() != GameState.Battle) Console.WriteLine($"  !! never happened: {what}");
            else Skip(1.6);
        }
        // Comes in by a door, as the game does: the room's lift is laid out and its own script runs
        void Enter(string map, int x, int y, bool read = true)
        {
            At(map, x, y, Direction.Up);
            game.ArriveOnMap();
            Frames(2);
            if (read) ReadOn();
        }
        void TalkFrom(string map, string key, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.Interact();
        }
        var only = args.Length > 2 ? args[2..] : null;
        bool Want(string room) => only == null || only.Contains(room);

        const string Center = "PokemonLeagueNorthPokemonCenter";
        string Passage(string to) => $"PokemonLeagueElevatorTo{to}Room";
        string RoomOf(string who) => $"PokemonLeague{who}Room";

        // ---- the Pokémon Center: the door and its guard
        story.SetBadges(0x7F);
        Enter(Center, 11, 11);
        if (Want("center"))
        {
            Frames(20); Shot("lg01_center_from_its_door");
            At(Center, 11, 6, Direction.Up); Frames(4); Shot("lg02_center_the_guard_at_the_door");
            // Seven Badges: he stays where he is
            TalkFrom(Center, "door_guard", 11, 4, Direction.Up);
            Line("eight", "lg03_center_the_guard_asks");
            ReadOn();
            var guard = Here().Everyone.Single(n => n.Key == "door_guard");
            Console.WriteLine($"league: with seven Badges the guard stands at {guard.GridX},{guard.GridY}, moved away {story.Has("FLAG_POKEMON_LEAGUE_DOOR_GUARD_MOVED_AWAY")}");
            story.SetBadges(0xFF);
            TalkFrom(Center, "door_guard", 11, 4, Direction.Up);
            ReadOn();
            Frames(10); Shot("lg04_center_the_guard_steps_aside");
            Console.WriteLine($"league: with eight the guard stands at {guard.GridX},{guard.GridY} facing {guard.Facing}, moved away {story.Has("FLAG_POKEMON_LEAGUE_DOOR_GUARD_MOVED_AWAY")}");
            At(Center, 4, 3, Direction.Up); Frames(4); Shot("lg05_center_the_nurse");
            At(Center, 16, 3, Direction.Up); Frames(4); Shot("lg06_center_the_counters");
        }
        story.SetBadges(0xFF);

        // A passage: its foot, the platform rising with the player on it, and the top
        void Ride(string who, string? prefix)
        {
            string map = Passage(who);
            var lift = (LeagueLift)MapDatabase.Get(map).Puzzle!;
            Enter(map, 4, lift.Where.Z + 4);
            if (prefix != null) { Frames(20); Shot($"{prefix}_the_foot_of_the_shaft"); }
            At(map, 4, lift.Where.Z + 1, Direction.Up); Frames(2);
            engine.Steering = (Direction.Up, false);
            Until(() => lift.Moving, "the platform setting off", 120);
            engine.Steering = null;
            if (prefix != null) { Skip(0.7); Shot($"{prefix}_the_platform_rising"); }
            Until(() => !lift.Moving && !engine.ScriptRunning, "the platform at the top", 600);
            Frames(10);
            if (prefix != null) Shot($"{prefix}_the_platform_at_the_top");
            Console.WriteLine($"league: {map}: the platform at {lift.Height}, the player at height {game.Player.HeightOn(Here())}");
            if (prefix != null) { At(map, 4, 5, Direction.Up); Frames(4); Shot($"{prefix}_the_door_on"); }
        }

        // A room of the Four: from its way in with the door shut behind, its master, the battle, the door on open
        void Challenge(string who, string number, string word)
        {
            string map = RoomOf(who), id = who.ToLowerInvariant();
            bool shoot = Want(id);
            Ride(who, shoot && who == "Aaron" ? "lg10_passage" : null);
            team.HealAll();
            Enter(map, 8, 11);
            if (shoot) { Frames(20); Shot($"lg{number}0_{id}_the_door_shuts_behind"); }
            Console.WriteLine($"league: {map}: in at {game.Player.GridX},{game.Player.GridY}, the door behind {(Here().NPCs.Any(n => n.Key == "entrance_door") ? "shut" : "open")}, " +
                $"the door on {(Here().NPCs.Any(n => n.Key == "exit_door") ? "shut" : "open")}");
            if (shoot) { At(map, 8, 8, Direction.Up); Frames(4); Shot($"lg{number}1_{id}_the_room"); }
            TalkFrom(map, id, 8, 6, Direction.Up);
            if (shoot) Line(word, $"lg{number}2_{id}_greets");
            IntoBattle($"{who}'s battle");
            if (shoot) Shot($"lg{number}3_{id}_battle");
            ReadOn();
            if (shoot) { At(map, 8, 6, Direction.Up); Frames(6); Shot($"lg{number}4_{id}_the_door_on_open"); }
            Console.WriteLine($"league: {map}: FLAG_DEFEATED_{who.ToUpperInvariant()} {story.Has($"FLAG_DEFEATED_{who.ToUpperInvariant()}")}, " +
                $"the door on {(Here().NPCs.Any(n => n.Key == "exit_door") ? "shut" : "open")}");
        }
        Challenge("Aaron", "2", "Aaron");
        Challenge("Bertha", "3", "Bertha");
        Challenge("Flint", "4", "Flint");
        Challenge("Lucian", "5", "Lucian");

        // ---- the Champion's room, the hallway and the Hall of Fame: one scene after another
        bool end = Want("champion");
        Ride("Champion", end ? "lg60_champion_passage" : null);
        team.HealAll();
        const string Champion = "PokemonLeagueChampionRoom", Hallway = "PokemonLeagueHallwayToHallOfFame", Hall = "PokemonLeagueHallOfFame";
        Enter(Champion, 8, 18, read: false);
        if (end) Line("Cynthia", "lg70_champion_cynthia");
        IntoBattle("Cynthia's battle");
        if (end) Shot("lg71_champion_battle");
        var own = (LeagueLift)MapDatabase.Get(Champion).Puzzle!;
        if (end) Line("lift", "lg72_champion_come_with_me");
        ReadOn(() => own.Moving);
        if (end) { Skip(0.25); Shot("lg73_champion_the_platform_sets_off"); Skip(1.6); Shot("lg74_champion_the_platform_rising"); }
        ReadOn(() => !own.Moving && game.Player.GridY <= 5);
        if (end) { Frames(2); Shot("lg75_champion_the_gallery"); }
        ReadOn(() => Here().Name == Hallway && State() == GameState.Overworld);
        if (end) { Skip(1.0); Shot("lg80_hallway_walking_in"); }
        if (end) Line("Hall of Fame", "lg81_hallway_cynthia");
        if (end) Line("splendid", "lg82_hallway_the_professor");
        ReadOn(() => Here().Name == Hall && State() == GameState.Overworld);
        if (end) { Skip(0.6); Shot("lg90_hall_walking_in"); }
        if (end) Line("Welcome", "lg91_hall_of_fame");
        if (end) Line("machine", "lg92_hall_the_machine");
        ReadOn(() => State() == GameState.HallOfFame);
        if (end) { Frames(40); Shot("lg93_hall_the_record"); }
        game.HallOfFameScreen.Close();
        ReadOn(() => Here().Name == "PlayerHouse2F" && State() == GameState.Overworld && !engine.ScriptRunning && !Box().IsActive);
        Frames(20);
        if (end) Shot("lg94_home");
        Console.WriteLine($"league: FLAG_DEFEATED_CYNTHIA {story.Has("FLAG_DEFEATED_CYNTHIA")}, the Hall of Fame holds {game.HallOfFame.Total}, " +
            $"SinnohHallOfFame {story.Has("SinnohHallOfFame")}, FLAG_GAME_COMPLETED {story.Has("FLAG_GAME_COMPLETED")}, " +
            $"the player in {Here().Name} at {game.Player.GridX},{game.Player.GridY}, the lead's HP {team.Members[0].CurrentHP}/{team.Members[0].MaxHP}");
    }
}
