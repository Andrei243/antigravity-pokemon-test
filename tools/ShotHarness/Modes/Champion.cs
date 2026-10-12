partial class Harness
{
    // The eleventh chapter (plan 02 · S14; not part of `all`; ch*), played by its own scripts: Victory Road from its
    // mouth, its boulders, the collector who keeps its back rooms and the League's statues; the League's Pokémon
    // Center, the rival running in at the first step before the guard, his words, his battle, his leaving, and the
    // guard counting the Badges off; the Hall of Fame: the professor's and Cynthia's words, the balls laid on the
    // machine one by one, the scene of the team's entry (each Pokémon and its card, the Champion among the team, the
    // paper falling), the save, the credits (the title card, the roll with the team beside it, the last card), and
    // waking at home; then Mom's news downstairs and the assistant in Sandgem Town. It wins the rival's battle and
    // prints what each left behind. CHAMPION_FROM=center starts in the League's Pokémon Center, CHAMPION_FROM=hall in
    // the Hall of Fame, CHAMPION_FROM=credits at the credits with the victory recorded, CHAMPION_FROM=home in the
    // player's room afterwards.
    public void ChampionMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var team = game.Party;
        // A Champion's team of six, each with a place it was met
        team.Members.Clear();
        foreach (var (species, level, name, place) in new[] { ("Torterra", 58, "Atlas", "Route 201"), ("Garchomp", 62, "Fang", "Wayward Cave"),
            ("Staraptor", 57, "", "Route 201"), ("Luxray", 56, "", "Route 202"), ("Floatzel", 55, "Otto", "Valley Windworks"), ("Lucario", 59, "", "Iron Island") })
        {
            var pokemon = new Pokemon(PokemonDatabase.Get(species)!, level);
            if (name.Length > 0) pokemon.Nickname = name;
            pokemon.Met(place, GameClock.Today);
            if (species == "Lucario") pokemon.MetLevel = 0;
            team.Members.Add(pokemon);
        }
        foreach (var badge in Enum.GetValues<Badge>()) story.GiveBadge(badge);
        story.SetVar("VAR_SUNYSHORE_CITY_STATE", 3);

        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        Player Me() => game.Player;
        int quick = 0;
        void Quick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            if (++quick % 6 == 0) engine.Draw();
        }
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Quick();
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void ReadTo(string? text, int most = 3000)
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
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 600 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    // The rival brings six: the lead never tires, and strikes with a move that does something to what it faces
                    foreach (var mine in team.Members.Where(p => p.CurrentHP > 0))
                    {
                        mine.CurrentHP = mine.MaxHP;
                        mine.Status = StatusCondition.None;
                        foreach (var move in mine.Moves) move.CurrentPP = move.MaxPP;
                    }
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
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
            foreach (var mine in team.Members.Where(p => !p.IsEgg)) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
            Frames(10);
        }
        void IntoBattle(string what)
        {
            for (int i = 0; i < 2000 && State() != GameState.Battle; i++)
            {
                if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            if (State() != GameState.Battle) Console.WriteLine($"  !! never happened: {what}");
            else Skip(1.0);
        }
        void StepOnto(string map, int x, int y, Direction way)
        {
            var (dx, dy) = FieldMovement.Delta(way);
            At(map, x - dx, y - dy, way);
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning, $"the scene at {x},{y}", 120);
            engine.Steering = null;
        }
        void ComeThrough(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            game.SteppedOutOfWarp = true;
            game.LocationSign.Hide();
            Frames(2);
        }
        NPC? Someone(string map, string key, string? place = null) => MapDatabase.Get(map).NPCs.FirstOrDefault(n => n.Key == key && (place == null || n.ScriptFile == place));
        void TalkTo(string map, string key, string? place = null, int dx = 0, int dy = 1)
        {
            var npc = MapDatabase.Get(map).NPCs.First(n => n.Key == key && (place == null || n.ScriptFile == place));
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        string Flags(params string[] flags) => string.Join(", ", flags.Select(f => $"{f.Replace("FLAG_", "")} {story.Has(f)}"));
        string Vars(params string[] vars) => string.Join(", ", vars.Select(v => $"{v.Replace("VAR_", "")} {story.Var(v)}"));
        const string Center = "PokemonLeagueNorthPokemonCenter", Hall = "PokemonLeagueHallOfFame";

        Frames(5);
        ReadTo(null, 600);

        string from = Environment.GetEnvironmentVariable("CHAMPION_FROM") ?? "";
        int stage = from switch { "" => 0, "center" => 1, "hall" => 2, "credits" => 3, "home" => 4, _ => 0 };

        if (stage == 0)
        {
            // ---- Victory Road: its mouth, the boulders of its upper floor, and the collector before its back rooms
            ComeThrough("VictoryRoad1F", 15, 77, Direction.Up);
            Frames(30); Shot("ch01_victory_road_from_its_mouth");
            Console.WriteLine("victory road: " + Flags("FLAG_FIRST_ARRIVAL_VICTORY_ROAD", "FLAG_HIDE_VICTORY_ROAD_1F_COLLECTOR"));
            TalkTo("VictoryRoad1F", "collector", null, -1, 0);
            ReadTo("Pokémon League"); Frames(4); Shot("ch02_victory_road_the_collector");
            ReadTo(null);
            ComeThrough("VictoryRoad2F", 19, 4, Direction.Left);
            Frames(30); Shot("ch03_victory_road_the_boulders");

            // ---- the League's grounds: a statue read from before it
            ComeThrough("Sinnoh", 842, 578, Direction.Up);
            Frames(30); Shot("ch04_league_the_steps");
            game.Interact();
            ReadTo("POKéMON LEAGUE"); Frames(4); Shot("ch05_league_a_statue_read");
            ReadTo(null);
        }

        if (stage <= 1)
        {
            // ---- the League's Pokémon Center: the rival comes running in at the first step onto the row before the guard
            ComeThrough(Center, 11, 11, Direction.Up);
            Frames(30); Shot("ch10_center_before_the_scene");
            Console.WriteLine($"center before: rival there {Someone(Center, "rival") != null}, {Vars("VAR_RIVAL_BEAT_SUNYSHORE_GYM")}, {Flags("FLAG_HIDE_POKEMON_LEAGUE_NORTH_POKECENTER_1F_RIVAL")}");
            StepOnto(Center, 11, 4, Direction.Up);
            for (int i = 0; i < 26; i++) Quick();
            Frames(2); Shot("ch11_center_the_rival_runs_in");
            ReadTo("Hold it right there"); Frames(4); Shot("ch12_center_hold_it");
            ReadTo("Elite Four"); Frames(4); Shot("ch13_center_who_is_ready");
            IntoBattle("the rival's battle");
            Skip(2.0); Shot("ch14_center_the_rivals_battle");
            Win();
            ReadTo("not ready"); Frames(4); Shot("ch15_center_not_ready_yet");
            ReadTo("don't you dare lose"); Frames(4); Shot("ch16_center_dont_you_dare_lose");
            if (Box().IsActive) Box().Advance();
            for (int i = 0; i < 20; i++) Quick();
            Frames(2); Shot("ch17_center_he_runs_out");
            ReadTo(null);
            ComeThrough(Center, 11, 5, Direction.Up);
            Console.WriteLine($"center after: rival there {Someone(Center, "rival") != null}, {Vars("VAR_RIVAL_BEAT_SUNYSHORE_GYM")}, {Flags("FLAG_HIDE_POKEMON_LEAGUE_NORTH_POKECENTER_1F_RIVAL")}");

            // ---- the guard counts the Badges off, town by town
            TalkTo(Center, "door_guard");
            ReadTo("Coal Badge"); Frames(4); Shot("ch18_center_the_guard_counts");
            ReadTo("Beacon Badge"); Frames(4); Shot("ch19_center_and_the_beacon_badge");
            ReadTo(null); Frames(20); Shot("ch1a_center_the_guard_aside");
            Console.WriteLine("the guard: " + Flags("FLAG_POKEMON_LEAGUE_DOOR_GUARD_MOVED_AWAY"));
        }
        else story.SetVar("VAR_RIVAL_BEAT_SUNYSHORE_GYM", 1);

        var ceremony = game.Ceremony;
        var credits = game.Credits;
        if (stage <= 2)
        {
            // ---- the Hall of Fame: the walk up, the professor's and Cynthia's words, the machine
            ComeThrough(Hall, 8, 15, Direction.Up);
            for (int i = 0; i < 60; i++) Quick();
            Frames(2); Shot("ch20_hall_the_walk_up");
            ReadTo("long time since"); Frames(4); Shot("ch21_hall_the_professor");
            ReadTo("became Champion"); Frames(4); Shot("ch22_hall_cynthia");
            ReadTo("Welcome to the Hall of Fame"); Frames(4); Shot("ch23_hall_welcome");
            ReadTo("kept for good"); Frames(4); Shot("ch24_hall_your_names_are_kept");
            ReadTo("This machine"); Frames(4); Shot("ch25_hall_this_machine");

            // ---- the balls laid on the machine, one for each of the team, and lit: the machine's own set piece (plan
            // 09 · L12), with the machine close up at the screen's own resolution while they are plain and while lit
            Box().Advance();
            Until(() => game.Map.MachineBalls == 1, "the first ball", 300);
            Frames(6); Shot("ch26_hall_the_first_ball");
            Until(() => game.Map.MachineBalls == 3, "three balls", 300);
            Frames(6); Shot("ch27_hall_three_balls");
            ShotCrop("ch27b_hall_three_balls_close", 840, 250, 240, 150, 8);
            Until(() => game.Map.MachineBalls == team.Members.Count, "every ball", 300);
            Frames(30); Shot("ch28_hall_every_ball_and_the_chime");
            Until(() => game.Map.MachineLit, "the balls lit", 120);
            Frames(2); ShotCrop("ch28b_hall_the_balls_lit_close", 840, 250, 240, 150, 8);
            Console.WriteLine($"the machine: {game.Map.MachineBalls} balls for a team of {team.Members.Count}, lit {game.Map.MachineLit}");

            // ---- the scene of the team's entry: each Pokémon and its card
            Until(() => State() == GameState.HallOfFameCeremony, "the ceremony", 1200);
            Console.WriteLine("the victory: " + Flags("FLAG_GAME_COMPLETED", "SinnohHallOfFame", "FLAG_UNLOCKED_VS_SEEKER_LVL_4", "FLAG_HIDE_SENDOFF_SPRING_CYNTHIA",
                "FLAG_HIDE_SANDGEM_TOWN_COUNTERPART", "FLAG_HIDE_DAY_CARE_GYM_GUIDE", "FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN") + ", " + Vars("VAR_PLAYER_HOUSE_POSTGAME_STATE"));
            Console.WriteLine($"the record: {game.HallOfFame.Total} entered, {string.Join(", ", game.HallOfFame.Entries[^1].Team.Select(m => m.Nickname))}; ribbons {team.Members.Count(p => p.Ribbons.Contains(RibbonRules.SinnohChamp))}");
            for (int member = 0; member < ceremony.Team.Count; member++)
            {
                int now = member;
                if (member == 0)
                {
                    Until(() => ceremony.Phase == CeremonyPhase.Enter && ceremony.Appearance().Slide > 0.45f, "the first Pokémon on its way in", 300);
                    Frames(1); Shot("ch30_ceremony_coming_in");
                    Until(() => ceremony.Phase == CeremonyPhase.Show && ceremony.PartsShown == 1, "the card's first part", 300);
                    Frames(4); Shot("ch31_ceremony_the_card_begins");
                }
                Until(() => ceremony.Member == now && ceremony.Phase == CeremonyPhase.Show && ceremony.PartsShown == HallOfFameCeremony.CardParts, $"the card of {member}", 600);
                Frames(20); Shot($"ch32_ceremony_{member + 1}_{ceremony.Team[member].Species.Name.ToLowerInvariant()}");
                var card = ceremony.CardOf(ceremony.Team[member]);
                Console.WriteLine($"  card {member + 1}: {card.Name} ({card.Species}) Lv. {card.Level}, {card.Trainer} {card.TrainerId}, {card.Met}");
                ceremony.PressConfirm();
                if (member == 0)
                {
                    Until(() => ceremony.Phase == CeremonyPhase.Leave && ceremony.Appearance().Slide < 0.6f, "the first Pokémon leaving", 120);
                    Frames(1); Shot("ch33_ceremony_going_out");
                }
            }

            // ---- the Champion among the team, and the paper
            Until(() => ceremony.Phase == CeremonyPhase.Champion && ceremony.Appearance().Figure > 0.5f, "the Champion stepping up", 300);
            Frames(2); Shot("ch40_ceremony_the_champion_steps_up");
            Until(() => ceremony.TeamSlide(2) > 0.5f, "the team coming in", 300);
            Frames(1); Shot("ch41_ceremony_the_team_comes_in");
            Until(() => ceremony.Appearance().Arrow, "the arrow", 600);
            Frames(60); Shot("ch42_ceremony_league_champion");
            Frames(90); Shot("ch43_ceremony_the_paper_falls");
            Console.WriteLine($"the Champion: {ceremony.Champion}, ID No. {ceremony.ChampionId}, time {ceremony.TimePlayed}");
            ceremony.PressConfirm();
            Frames(30); Shot("ch44_ceremony_closing");
            Until(() => State() != GameState.HallOfFameCeremony, "the ceremony's end", 300);

            // ---- the save, in the player's own room
            ReadTo("saved the game"); Frames(4); Shot("ch45_the_game_is_saved");
            var saved = SaveManager.LoadGame();
            Console.WriteLine(saved == null ? "  !! no save was written" : $"the save: {saved.CurrentMapName} at {saved.PlayerGridX},{saved.PlayerGridY}, story version {saved.StoryVersion}, completed {saved.StoryFlags.Contains("FLAG_GAME_COMPLETED")}");
            Box().Advance();
        }
        else
        {
            // The victory as the Hall of Fame's scene leaves it
            engine.StartScript(ScriptParser.Parse(Hall, "script Test\nsetflag FLAG_UNLOCKED_VS_SEEKER_LVL_4\ncall Victory\nhalloffame\nsetflag FLAG_GAME_COMPLETED\nsetflag SinnohHallOfFame\nheal")[0]);
            ReadTo(null, 300);
            ComeThrough("PlayerHouse2F", 4, 4, Direction.Up);
            ReadTo(null, 300);
        }

        if (stage <= 3)
        {
            // ---- the credits: the title card, the roll with the team beside it, the last card
            if (stage == 3) engine.StartScript(ScriptParser.Parse("PlayerHouse2F", "script Test\nfade out 0.4\ncredits")[0]);
            Until(() => State() == GameState.Credits && credits.Phase == CreditsPhase.Title && credits.Appearance().Title >= 1f, "the credits' title card", 1200);
            Frames(20); Shot("ch50_credits_the_title_card");
            Console.WriteLine($"the credits: {credits.Lines.Count} lines, {credits.Height:0} tall, {credits.Length / CreditsScreen.Pace:0} seconds of roll, beside it {string.Join(", ", credits.Team.Select(p => p.DisplayName))}");
            int shot = 1;
            void RollTo(float scroll, string name)
            {
                credits.Held = true;
                Until(() => credits.Phase != CreditsPhase.Roll && credits.Phase != CreditsPhase.Title || credits.Scroll >= scroll, name, 6000);
                credits.Held = false;
                Frames(8); Shot($"ch5{shot++}_credits_{name}");
            }
            // Five of its sections, each by where its heading stands, brought to the upper part of the picture
            float At(string heading) => credits.Lines.First(l => l.Kind == CreditLineKind.Heading && l.Text == heading).Y + 760f;
            RollTo(At("A FAN REMAKE") - 140f, "a_fan_remake");
            RollTo(At("MADE ANEW"), "made_anew");
            RollTo(At("DATA"), "the_data");
            RollTo(At("THE CHAMPION"), "the_champions_team");
            RollTo(At("AND YOU") - 200f, "and_you");
            credits.Held = true;
            Until(() => credits.Phase == CreditsPhase.End, "the last card", 6000);
            credits.Held = false;
            Until(() => credits.Appearance().Arrow, "the last card's arrow", 300);
            Frames(20); Shot("ch56_credits_the_end");
            credits.PressConfirm();
            Until(() => State() == GameState.Overworld && !engine.ScriptRunning, "the game going on at home", 900);
            Frames(60);
        }
        File.Delete("savegame.json");
        File.Delete(SaveManager.BackupOf("savegame.json"));

        // ---- waking at home, where the game goes on
        ReadTo(null, 300);
        Frames(30); Shot("ch60_home_awake_in_the_bedroom");
        Console.WriteLine($"home: {game.Map.Name} at {Me().GridX},{Me().GridY} facing {Me().Facing}; " + Vars("VAR_PLAYER_HOUSE_POSTGAME_STATE"));

        // ---- downstairs: Mom's news
        ComeThrough("PlayerHouse", 8, 4, Direction.Down);
        ReadTo("looking for you"); Frames(4); Shot("ch61_home_mom_has_news");
        ReadTo("Snowpoint City"); Frames(4); Shot("ch62_home_a_ship_at_snowpoint");
        ReadTo("Pokédex"); Frames(4); Shot("ch63_home_the_professors_pokedex");
        ReadTo(null);
        Console.WriteLine("mom: " + Vars("VAR_PLAYER_HOUSE_POSTGAME_STATE") + ", " + Flags("FLAG_TALKED_TO_MOM_ABOUT_NATIONAL_DEX_PROGRESS", "FLAG_TALKED_TO_MOM_ABOUT_RIVAL_SNOWPOINT_CITY"));

        // ---- Sandgem Town: the assistant stands before the lab again
        ComeThrough("Sinnoh", 167, 847, Direction.Up);
        Frames(30); Shot("ch64_sandgem_the_assistant_is_back");
        TalkTo("Sinnoh", "sandgem_counterpart", "sandgem_town", -1, 0);
        // (her first word to a new Champion is the elder's message since plan 02 · S15, part c, as the original's is;
        // the congratulations come once the elder has been heard)
        ReadTo("elder of Celestic Town"); Frames(4); Shot("ch65_sandgem_the_elders_message");
        ReadTo(null);

        // ---- and who else the victory brought on or sent away
        ComeThrough("PokemonDayCare", 5, 9, Direction.Left);
        Frames(30); Shot("ch66_day_care_the_fan");
        ComeThrough("VictoryRoad1F", 44, 33, Direction.Right);
        Frames(30); Shot("ch67_victory_road_the_way_to_the_back_rooms");
        Console.WriteLine($"afterwards: the fan there {Someone("PokemonDayCare", "gym_guide") != null}, the collector there {Someone("VictoryRoad1F", "collector") != null}, "
            + Flags("FLAG_HIDE_SENDOFF_SPRING_CYNTHIA", "FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN", "FLAG_HIDE_VEILSTONE_STORE_B1F_PROF_ROWAN"));
    }
}
