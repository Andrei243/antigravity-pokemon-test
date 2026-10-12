partial class Harness
{
    // The time after the Hall of Fame, part a (plan 02 · S15; not part of `all`; pg*), played by its own scripts: the
    // professor's rating of the Pokédex, the assistant before the lab, Professor Oak's visit, the upgrade and the
    // Poké Radar; the assistant's sister and grandfather, her offer of a lesson and the lesson on Route 202; Pal Park's
    // doors without their workers, Professor Oak in its lobby and at the table in Eterna City, Byron in Iron Island's
    // cabin, Victory Road's way to its back rooms open; the ship: Snowpoint's sailor, Cynthia on the quay; the Fight
    // Area: the blockades, the rival on the pier, the two before the gate and their crowd, the offer refused and taken,
    // the tag battle, Palmer, the rival's leaving and Buck; Route 227 and the Survival Area; Marley in Victory Road's
    // back room, joining, sent back, a pair's tag battle beside her and her parting; Lucian in Canalave's library and
    // Looker at the Game Corner. It wins every battle a scene starts and prints what each left behind.
    // POSTGAME_FROM=radar starts with the National Pokédex before the lab, POSTGAME_FROM=palpark on Route 221,
    // POSTGAME_FROM=ship on Snowpoint's quay, POSTGAME_FROM=fight on the Fight Area's pier, POSTGAME_FROM=zone on
    // Route 227 with the tag battle won, POSTGAME_FROM=marley in Victory Road, POSTGAME_FROM=library in Canalave.
    public void PostgameMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var team = game.Party;
        team.Members.Clear();
        // A Garchomp leads: Flint's fire and Volkner's lightning do it little harm
        foreach (var (species, level) in new[] { ("Garchomp", 66), ("Torterra", 62), ("Staraptor", 60), ("Luxray", 60), ("Floatzel", 59), ("Lucario", 61) })
            team.Members.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        foreach (var badge in Enum.GetValues<Badge>()) story.GiveBadge(badge);

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
        // Reads on to a line, answering questions with their first answer (yes) unless told which
        void ReadTo(string? text, int most = 3000, int answer = 0)
        {
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Box().CurrentLine.Contains(text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld && i > 90) break;
                if (text == null && !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (engine.Choice.IsOpen)
                {
                    Quick();
                    for (int a = 0; a < answer; a++) engine.Choice.Move(1);
                    engine.Choice.Confirm();
                    Quick();
                }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        // Reads on to the next question and leaves it open, for a shot and an answer of the mode's own
        void ToQuestion(string what, int most = 3000)
        {
            for (int i = 0; i < most && !engine.Choice.IsOpen; i++)
            {
                if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Quick(); }
                else Quick();
            }
            if (!engine.Choice.IsOpen) Console.WriteLine($"  !! never asked: {what}");
            Frames(10);
        }
        void Answer(int answer)
        {
            for (int a = 0; a < answer; a++) engine.Choice.Move(1);
            engine.Choice.Confirm();
            Quick();
        }
        // A battle won: every foe brought down to 1 HP and struck, the player's own team kept well
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 600 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var mine in team.Members.Where(p => p.CurrentHP > 0))
                    {
                        mine.CurrentHP = mine.MaxHP;
                        mine.Status = StatusCondition.None;
                        foreach (var move in mine.Moves) move.CurrentPP = move.MaxPP;
                    }
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    foreach (var foe in fight.Core.EnemySlots.Where(b => b.Pokemon != null)) foe.Pokemon!.CurrentHP = Math.Min(foe.Pokemon.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    var target = fight.EnemyPokemon;
                    int pick = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status
                        && TypeChart.GetEffectiveness(m.Data.Type, target.PrimaryType, target.SecondaryType) > 0f);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(Math.Max(0, pick));
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else if (fight.HUD.MenuState == BattleMenuState.SwitchPokemon)
                {
                    int next = fight.PlayerParty.Members.FindIndex(p => !p.IsFainted && !fight.PlayerSlots.Any(s => s.Pokemon == p));
                    fight.SelectSwitch(Math.Max(0, next));
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
            for (int i = 0; i < 2400 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Quick(); engine.Choice.Confirm(); Quick(); }
                else if (Box().IsActive && !Box().IsCurrentLineComplete) { Box().FinishLine(); Quick(); }
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
            var npc = MapDatabase.Get(map).NPCs.FirstOrDefault(n => n.Key == key && (place == null || n.ScriptFile == place));
            if (npc == null) { Console.WriteLine($"  !! {key} isn't on {map}"); return; }
            var facing = (dx, dy) switch { (0, 1) => Direction.Up, (0, -1) => Direction.Down, (1, 0) => Direction.Left, _ => Direction.Right };
            At(map, npc.GridX + dx, npc.GridY + dy, facing);
            game.Interact();
        }
        // Someone on their way: the shot is taken once they have come within a tile of where a walk passes
        void Walking(string map, string key, string? place, Func<NPC, bool> there, string what)
        {
            Until(() => Someone(map, key, place) is { } who && there(who), what, 600);
            Frames(2);
        }
        string Flags(params string[] flags) => string.Join(", ", flags.Select(f => $"{f.Replace("FLAG_", "")} {story.Has(f)}"));
        string Vars(params string[] vars) => string.Join(", ", vars.Select(v => $"{v.Replace("VAR_", "")} {story.Var(v)}"));
        int Held(string item) => game.Bag.GetQuantity(ItemDatabase.Get(item)!);
        void SeeOfSinnoh(int count)
        {
            foreach (var entry in Pokedex.Entries(PokedexMode.Sinnoh).Take(count)) game.Pokedex.RegisterSeen(entry.Species.DexNumber);
        }
        const string Hall = "PokemonLeagueHallOfFame", Lab = "RowanLab";

        Frames(5);
        ReadTo(null, 600);

        // A Champion, as the Hall of Fame's scene leaves one (plan 02 · S14), who has been to Mom's
        engine.StartScript(ScriptParser.Parse(Hall, "script Test\nsetflag FLAG_UNLOCKED_VS_SEEKER_LVL_4\ncall Victory\nhalloffame\nsetflag FLAG_GAME_COMPLETED\nsetflag SinnohHallOfFame\nheal\nsetvar VAR_PLAYER_HOUSE_POSTGAME_STATE 2")[0]);
        ReadTo(null, 300);

        string from = Environment.GetEnvironmentVariable("POSTGAME_FROM") ?? "";
        int stage = from switch { "" => 0, "radar" => 1, "palpark" => 2, "ship" => 3, "fight" => 4, "zone" => 5, "marley" => 6, "library" => 7, _ => 0 };

        if (stage == 0)
        {
            // ---- the professor rates a Pokédex that isn't full yet
            SeeOfSinnoh(120);
            ComeThrough(Lab, 7, 15, Direction.Up);
            At(Lab, 7, 6, Direction.Up);
            Frames(20); Shot("pg01_lab_before_the_desk");
            game.Interact();
            ToQuestion("the professor's question"); Shot("pg02_lab_show_me_your_pokedex");
            Answer(0);
            ReadTo("kinds of Pokémon seen"); Frames(4); Shot("pg03_lab_the_count");
            ReadTo("real eye"); Frames(4); Shot("pg04_lab_the_rating");
            ReadTo(null);
            Console.WriteLine($"the rating: {game.Pokedex.SeenIn(PokedexMode.Sinnoh)} seen in Sinnoh, national {game.Pokedex.NationalUnlocked}");

            // ---- every Pokémon of Sinnoh seen: the assistant sends the Champion in
            SeeOfSinnoh(210);
            ComeThrough("Sinnoh", 167, 847, Direction.Up);
            TalkTo("Sinnoh", "sandgem_counterpart", "sandgem_town", -1, 0);
            ReadTo("show it to Professor Rowan"); Frames(4); Shot("pg05_sandgem_show_it_to_the_professor");
            ReadTo(null);

            // ---- Professor Oak's visit
            ComeThrough(Lab, 7, 15, Direction.Up);
            At(Lab, 7, 6, Direction.Up);
            game.Interact();
            ToQuestion("the professor's other question"); Shot("pg06_lab_something_to_show_me");
            Answer(0);
            ReadTo("every one of them"); Frames(4); Shot("pg07_lab_every_one_of_them");
            ReadTo("In sixty years"); Frames(4);
            Box().Advance();
            Walking(Lab, "prof_oak", null, oak => oak.GridY <= 10, "Professor Oak coming in");
            Shot("pg08_lab_professor_oak_comes_in");
            ReadTo("Good day to you"); Frames(4); Shot("pg09_lab_good_day_professor_rowan");
            ReadTo("old colleague"); Frames(4); Shot("pg0a_lab_my_old_colleague");
            ReadTo("upgrade that Pokédex"); Frames(4); Shot("pg0b_lab_let_me_upgrade_it");
            ReadTo("National Mode"); Frames(4); Shot("pg0c_lab_the_national_mode");
            ReadTo("Pal Park"); Frames(4); Shot("pg0d_lab_pal_park_is_open");
            ReadTo("must be going"); Frames(4);
            Box().Advance();
            Walking(Lab, "prof_oak", null, oak => oak.GridY >= 11, "Professor Oak going out");
            Shot("pg0e_lab_professor_oak_goes");
            ReadTo("This is for you"); Frames(4); Shot("pg0f_lab_this_is_for_you");
            ReadTo("Poké Radar"); Frames(4); Shot("pg0g_lab_the_poke_radar");
            ReadTo(null);
            Console.WriteLine($"the upgrade: national {game.Pokedex.NationalUnlocked}, the PC's boxes {game.Boxes.Boxes.Count}, Poké Radar {Held("Poké Radar")}, Professor Oak there {Someone(Lab, "prof_oak") != null}, "
                + Flags("FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_OAK", "FLAG_HIDE_FIGHT_AREA_BLOCKADE") + $", player at {Me().GridX},{Me().GridY}");
            game.Interact();
            ReadTo("many more"); Frames(4); Shot("pg0h_lab_many_more_pokemon");
            ReadTo(null);
        }
        else
        {
            SeeOfSinnoh(210);
            engine.StartScript(ScriptParser.Parse(Lab, "script Test\nnationaldex\nadditem \"Poké Radar\"")[0]);
            ReadTo(null, 300);
        }

        if (stage <= 1)
        {
            // ---- the assistant's family, who ask for the National Pokédex
            ComeThrough("SandgemTownCounterpartHouse1F", 4, 8, Direction.Up);
            Frames(30); Shot("pg10_the_assistants_house");
            TalkTo("SandgemTownCounterpartHouse1F", "expert_m");
            ReadTo("far more than that"); Frames(4); Shot("pg11_the_grandfather");
            ReadTo(null);
            // ---- the assistant: her sister, Bebe, the lesson
            ComeThrough("Sinnoh", 167, 847, Direction.Up);
            TalkTo("Sinnoh", "sandgem_counterpart", "sandgem_town", -1, 0);
            ReadTo("little sister"); Frames(4); Shot("pg12_sandgem_have_you_seen_my_sister");
            ReadTo(null);
            TalkTo("SandgemTownCounterpartHouse1F", "twin");
            ReadTo("crowds of one kind"); Frames(4); Shot("pg13_the_sisters_news");
            ReadTo(null);
            Console.WriteLine("the sister: " + Flags("FLAG_TALKED_TO_COUNTERPART_SISTER_WITH_NATIONAL_DEX") + $", swarms on {game.Encounters.SwarmsOn}");
            TalkTo("Sinnoh", "sandgem_counterpart", "sandgem_town", -1, 0);
            ReadTo("Bebe"); Frames(4); Shot("pg14_sandgem_have_you_met_bebe");
            ReadTo(null);
            story.Set("FLAG_RECEIVED_HEARTHOME_CITY_NORTHWEST_HOUSE_EEVEE");
            TalkTo("Sinnoh", "sandgem_counterpart", "sandgem_town", -1, 0);
            ReadTo("Route 202"); Frames(4); Shot("pg15_sandgem_ill_show_you");
            Box().Advance();
            Walking("Sinnoh", "sandgem_counterpart", "sandgem_town", her => her.GridX >= 172, "the assistant going off");
            Shot("pg16_sandgem_she_goes_off_up_the_road");
            ReadTo(null);
            Console.WriteLine("the lesson offered: " + Vars("VAR_ROUTE_202_STATE") + ", " + Flags("FLAG_STARTED_COUNTERPART_POKE_RADAR_TUTORIAL", "FLAG_HIDE_SANDGEM_TOWN_COUNTERPART"));

            // ---- Route 202: the lesson
            ComeThrough("Sinnoh", 186, 821, Direction.Up);
            Frames(30); Shot("pg17_route_202_she_waits_in_the_grass");
            TalkTo("Sinnoh", "counterpart", "route_202");
            ReadTo("how it works"); Frames(4); Shot("pg18_route_202_let_me_show_you");
            ReadTo("grass shakes"); Frames(4); Shot("pg19_route_202_the_patch_to_the_east");
            ReadTo("strange part"); Frames(4); Shot("pg1a_route_202_the_strange_part");
            ReadTo("chain"); Frames(4); Shot("pg1b_route_202_a_chain");
            ReadTo("Fifty steps"); Frames(4);
            Box().Advance();
            Walking("Sinnoh", "counterpart", "route_202", her => her.GridY >= 819, "the assistant leaving the grass");
            Shot("pg1c_route_202_she_goes_back");
            ReadTo(null);
            Console.WriteLine("the lesson: " + Vars("VAR_ROUTE_202_STATE") + ", " + Flags("FLAG_HIDE_ROUTE_202_COUNTERPART", "FLAG_HIDE_SANDGEM_TOWN_COUNTERPART"));
            ComeThrough("Sinnoh", 167, 847, Direction.Up);
            TalkTo("Sinnoh", "sandgem_counterpart", "sandgem_town", -1, 0);
            Whole(); Frames(4); Shot("pg1d_sandgem_afterwards");
            ReadTo(null);
        }

        if (stage <= 2)
        {
            // ---- Route 221: the workers have gone from Pal Park's doors
            ComeThrough("Sinnoh", 306, 913, Direction.Up);
            Frames(30); Shot("pg20_route_221_pal_parks_doors");
            Console.WriteLine($"route 221: workers there {Someone("Sinnoh", "worker_west") != null} {Someone("Sinnoh", "worker_east") != null}, " + Flags("FLAG_HIDE_ROUTE_221_WORKER_WEST", "FLAG_HIDE_ROUTE_221_WORKER_EAST"));

            // ---- the lobby: Professor Oak
            At("PalParkLobby", 7, 19, Direction.Up);
            Frames(20); Shot("pg21_lobby_professor_oak_waits");
            game.ArriveOnMap();
            game.LocationSign.Hide();
            Until(() => Me().GridY <= 17, "the player going up to Professor Oak", 300);
            Frames(2); Shot("pg22_lobby_up_to_the_professor");
            ReadTo("Welcome to Pal Park"); Frames(4); Shot("pg23_lobby_welcome_to_pal_park");
            ReadTo("Hold out your Pokétch"); Frames(4); Shot("pg24_lobby_hold_out_your_poketch");
            ReadTo("Trainer Counter"); Frames(4); Shot("pg25_lobby_the_trainer_counter");
            ReadTo("Eterna City"); Frames(4); Shot("pg26_lobby_i_shall_be_in_eterna");
            Box().Advance();
            Walking("PalParkLobby", "prof_oak", null, oak => oak.GridY >= 17, "Professor Oak going out");
            Shot("pg27_lobby_he_goes_out");
            ReadTo(null);
            Console.WriteLine($"the lobby: Trainer Counter {game.Poketch.Has(PoketchApp.TrainerCounter)}, " + Vars("VAR_PAL_PARK_LOBBY_STATE", "VAR_CATCHING_SHOW_RECORD")
                + ", " + Flags("FLAG_HIDE_PAL_PARK_LOBBY_PROF_OAK", "FLAG_HIDE_ETERNA_CITY_SOUTH_HOUSE_PROF_OAK"));
            TalkTo("PalParkLobby", "pokemon_breeder_m");
            ReadTo("2000 points"); Frames(4); Shot("pg28_lobby_the_rivals_record");
            ReadTo(null);

            // ---- Eterna City's south house: the Upgrade and the three birds
            ComeThrough("EternaCitySouthHouse", 4, 8, Direction.Up);
            Frames(30); Shot("pg29_eterna_professor_oak_at_the_table");
            TalkTo("EternaCitySouthHouse", "prof_oak", null, 1, 0);
            ReadTo("Good of you to call"); Frames(4); Shot("pg2a_eterna_good_of_you_to_call");
            ReadTo("Upgrade"); Frames(4); Shot("pg2b_eterna_the_upgrade");
            ReadTo("Moltres, Zapdos and Articuno"); Frames(4); Shot("pg2c_eterna_three_birds_of_legend");
            ReadTo(null);
            Console.WriteLine($"eterna: Upgrade {Held("Upgrade")}, " + Flags("FLAG_RECEIVED_ETERNA_CITY_SOUTH_HOUSE_UPGRADE", "FLAG_ACTIVATED_ROAMING_LEGENDARY_BIRDS")
                + ", roaming " + string.Join(", ", game.Encounters.Roamers.Where(r => r.Active).Select(r => r.Species)));
            TalkTo("EternaCitySouthHouse", "prof_oak", null, 1, 0);
            ReadTo("rare in the extreme"); Frames(4); Shot("pg2d_eterna_afterwards");
            ReadTo(null);

            // ---- Iron Island's cabin: Byron
            ComeThrough("IronIslandHouse", 4, 8, Direction.Up);
            Frames(30); Shot("pg2e_iron_island_byron_in_the_cabin");
            TalkTo("IronIslandHouse", "byron");
            ReadTo("my hideaway"); Frames(4); Shot("pg2f_iron_island_my_hideaway");
            ReadTo("Metal Coat"); Frames(4); Shot("pg2g_iron_island_the_metal_coat");
            ReadTo("Steel suits them"); Frames(4);
            Box().Advance();
            Walking("IronIslandHouse", "byron", null, he => he.GridY >= 7, "Byron going out");
            Shot("pg2h_iron_island_he_goes_out");
            ReadTo(null);
            Console.WriteLine($"iron island: Metal Coat {Held("Metal Coat")}, Byron there {Someone("IronIslandHouse", "byron") != null}, " + Flags("FLAG_RECEIVED_IRON_ISLAND_HOUSE_METAL_COAT", "FLAG_HIDE_IRON_ISLAND_HOUSE_BYRON"));

            // ---- Victory Road: the collector has stood aside, and Mr. Backlot has his question
            ComeThrough("VictoryRoad1F", 44, 33, Direction.Right);
            Frames(30); Shot("pg2i_victory_road_the_way_to_the_back_rooms");
            Console.WriteLine($"victory road: the collector there {Someone("VictoryRoad1F", "collector") != null}, " + Flags("FLAG_HIDE_VICTORY_ROAD_1F_COLLECTOR"));
        }

        if (stage <= 3)
        {
            // ---- Snowpoint City's quay: the sailor's offer, refused and taken, and Cynthia
            ComeThrough("Sinnoh", 356, 246, Direction.Down);
            Frames(30); Shot("pg30_snowpoint_the_quay");
            game.Interact();
            ToQuestion("the sailor's offer"); Shot("pg31_snowpoint_shall_we_cast_off");
            Answer(1);
            ReadTo("no surfing"); Frames(4); Shot("pg32_snowpoint_no_surfing_across");
            ReadTo(null);
            Console.WriteLine("the ship refused: " + Flags("FLAG_SAILED_TO_BATTLE_ZONE") + $", at {Me().GridX},{Me().GridY}");
            game.Interact();
            ToQuestion("the sailor's offer again");
            Answer(0);
            Walking("Sinnoh", "cynthia", "snowpoint_city", she => she.GridX <= 358, "Cynthia coming down to the quay");
            Shot("pg33_snowpoint_cynthia_comes_down");
            ReadTo("glad I waited"); Frames(4); Shot("pg34_snowpoint_im_glad_i_waited");
            ReadTo("keeps changing"); Frames(4); Shot("pg35_snowpoint_our_world_keeps_changing");
            ReadTo("far-off places"); Frames(4); Shot("pg36_snowpoint_the_far_off_places");
            ReadTo("Safe travels"); Frames(4);
            Box().Advance();
            Walking("Sinnoh", "cynthia", "snowpoint_city", she => she.GridY <= 243 && she.GridX >= 358, "Cynthia going back up the town");
            Shot("pg37_snowpoint_she_goes");
            ReadTo("All aboard"); Frames(4); Shot("pg38_snowpoint_all_aboard");
            Console.WriteLine("the ship: " + Flags("FLAG_SAILED_TO_BATTLE_ZONE", "FLAG_HIDE_SNOWPOINT_CITY_CYNTHIA"));
            Box().Advance();
            Until(() => Me().GridX == 623 && Me().GridY == 434 && engine.ScriptRunning, "the landing on the Fight Area's pier", 900);
        }
        else if (stage == 4) ComeThrough("Sinnoh", 623, 434, Direction.Right);

        if (stage <= 4)
        {
            // ---- the Fight Area: the rival on the pier
            Console.WriteLine($"ashore: {game.Map.Name} at {Me().GridX},{Me().GridY} facing {Me().Facing}; " + Vars("VAR_FIGHT_AREA_STATE"));
            Until(() => engine.ScriptRunning, "the rival's scene", 120);
            for (int i = 0; i < 24; i++) Quick();
            Frames(2); Shot("pg40_fight_area_the_rival_on_the_pier");
            Until(() => Me().GridX == 627, "the walk up to the rival", 600);
            ReadTo("every plank"); Frames(4); Shot("pg41_fight_area_there_you_are");
            ReadTo("come with me"); Frames(4); Shot("pg42_fight_area_come_with_me");
            Box().Advance();
            Until(() => Me().GridX == 654 && Me().GridY == 426 && Box().IsActive, "the two before the gate", 900);
            Whole(); Frames(4); Shot("pg43_fight_area_before_the_gate");
            ToQuestion("the rival's offer"); Shot("pg44_fight_area_you_and_me_together");
            // Refused first: he waits, and the two and their crowd have their say
            Answer(1);
            ReadTo("alone"); Frames(4); Shot("pg45_fight_area_i_need_your_help");
            ReadTo(null);
            Console.WriteLine("refused: " + Vars("VAR_FIGHT_AREA_STATE") + $", the rival at {Someone("Sinnoh", "rival", "fight_area")?.GridX},{Someone("Sinnoh", "rival", "fight_area")?.GridY}");
            TalkTo("Sinnoh", "flint", "fight_area");
            ReadTo("partner"); Frames(4); Shot("pg46_fight_area_flint");
            ReadTo(null);
            TalkTo("Sinnoh", "volkner", "fight_area");
            ReadTo("my friend"); Frames(4); Shot("pg47_fight_area_volkner");
            ReadTo(null);
            TalkTo("Sinnoh", "battle_girl_2", "fight_area", -1, 0);
            Whole(); Frames(4); Shot("pg48_fight_area_an_onlooker");
            ReadTo(null);
            TalkTo("Sinnoh", "buck", "fight_area", -1, 0);
            Whole(); Frames(4); Shot("pg49_fight_area_buck_watches");
            ReadTo(null);
            // The two blockades, standing
            At("Sinnoh", 629, 431, Direction.Up);
            Frames(30); Shot("pg4a_fight_area_the_way_north_kept");
            TalkTo("Sinnoh", "ace_trainer_m_2", "fight_area");
            Whole(); Frames(4); Shot("pg4b_fight_area_the_blockade_speaks");
            ReadTo(null);
            At("Sinnoh", 680, 430, Direction.Right);
            Frames(30); Shot("pg4c_route_230_the_waters_edge_kept");
            TalkTo("Sinnoh", "fisherman", "route_230", -1, 0);
            Whole(); Frames(4); Shot("pg4d_route_230_the_fisherman");
            ReadTo(null);

            // ---- the rival again, from his other side, and the tag battle
            TalkTo("Sinnoh", "rival", "fight_area", 1, 0);
            ToQuestion("the rival's offer again"); Shot("pg4e_fight_area_ready_now");
            Console.WriteLine($"asked again: the player at {Me().GridX},{Me().GridY} facing {Me().Facing}");
            Answer(0);
            ReadTo("flatten them"); Frames(4);
            ReadTo("Burn as bright"); Frames(4); Shot("pg4f_fight_area_burn_as_bright_as_you_can");
            ReadTo("Show me what you can do"); Frames(4); Shot("pg4g_fight_area_show_me");
            IntoBattle("the tag battle");
            Skip(3.0); Shot("pg4h_fight_area_the_tag_battle");
            for (int i = 0; i < 40 && game.Battle is { } b && b.HUD.MenuState == BattleMenuState.Message && !b.IsBattleOver; i++) { Confirm(b); Skip(0.5); }
            Frames(4); Shot("pg4i_fight_area_the_tag_battles_menu");
            Console.WriteLine($"the tag battle: {string.Join(", ", game.Battle!.Core.PlayerSlots.Where(s => s.Pokemon != null).Select(s => s.Pokemon!.Species.Name))} against "
                + string.Join(", ", game.Battle.Core.EnemySlots.Where(s => s.Pokemon != null).Select(s => s.Pokemon!.Species.Name)));
            Win();
            ReadTo("burn yourself out"); Frames(4); Shot("pg4j_fight_area_dont_burn_yourself_out");
            ReadTo("That was a blast"); Frames(4); Shot("pg4k_fight_area_that_was_a_blast");
            ReadTo("Battle Fronti"); Frames(4); Shot("pg4l_fight_area_im_off_to_the");
            Box().Advance();
            Until(() => Box().IsActive && Box().CurrentLine.Contains("Thud"), "the thud", 300);
            Whole(); Frames(2); Shot("pg4m_fight_area_thud");
            Box().Advance();
            Until(() => Someone("Sinnoh", "rival", "fight_area") is { GridY: 427 }, "the rival bowled over", 300);
            Frames(8); Shot("pg4n_fight_area_palmer_out_of_the_gate");
            ReadTo("What's all this"); Frames(4); Shot("pg4o_fight_area_whats_all_this");
            ReadTo("D-Dad"); Frames(4); Shot("pg4p_fight_area_dad");
            ReadTo("If it isn't"); Frames(4); Shot("pg4q_fight_area_if_it_isnt");
            ReadTo("where to find me"); Frames(4); Shot("pg4r_fight_area_you_know_where_to_find_me");
            ReadTo("Tower Tycoon"); Frames(4); Shot("pg4s_fight_area_the_tower_tycoon");
            ReadTo("Way tougher"); Frames(4);
            Box().Advance();
            Until(() => Someone("Sinnoh", "rival", "fight_area") is { GridY: >= 429 }, "the rival running off", 300);
            Frames(2); Shot("pg4t_fight_area_the_rival_runs_off");
            ReadTo("The name's Buck"); Frames(4); Shot("pg4u_fight_area_buck");
            ReadTo("Survival Area"); Frames(4); Shot("pg4v_fight_area_come_to_the_survival_area");
            ReadTo(null);
            Console.WriteLine("the tag battle won: " + Vars("VAR_FIGHT_AREA_STATE") + ", " + Flags("FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS", "FLAG_HIDE_FIGHT_AREA_PALMER",
                "FLAG_HIDE_FIGHT_AREA_RIVAL", "FLAG_HIDE_FIGHT_AREA_BUCK", "FLAG_HIDE_FIGHT_AREA_BLOCKADE") + $", player at {Me().GridX},{Me().GridY}");
            ComeThrough("Sinnoh", 654, 427, Direction.Up);
            Frames(30); Shot("pg4w_fight_area_the_gate_afterwards");
            At("Sinnoh", 629, 431, Direction.Up);
            Frames(30); Shot("pg4x_fight_area_the_way_north_open");
            Console.WriteLine($"afterwards: blockade there {Someone("Sinnoh", "ace_trainer_m_2", "fight_area") != null} {Someone("Sinnoh", "rich_boy", "route_230") != null}, "
                + $"the gate's people there {Someone("Sinnoh", "flint", "fight_area") != null} {Someone("Sinnoh", "rival", "fight_area") != null} {Someone("Sinnoh", "buck", "fight_area") != null}");
        }
        else
        {
            engine.StartScript(ScriptParser.Parse("fight_area", "script Test\nsetvar VAR_FIGHT_AREA_STATE 2\nsetflag FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS\nsetflag FLAG_HIDE_FIGHT_AREA_RIVAL\n"
                + "setflag FLAG_HIDE_FIGHT_AREA_BUCK\nsetflag FLAG_HIDE_FIGHT_AREA_BLOCKADE\nsetflag FLAG_SAILED_TO_BATTLE_ZONE")[0]);
            ReadTo(null, 300);
        }

        if (stage <= 5)
        {
            // ---- Route 227: Crasher Wake and the rival at the foot of the road
            ComeThrough("Sinnoh", 739, 309, Direction.Up);
            Frames(30); Shot("pg50_route_227_the_rival_and_his_master");
            StepOnto("Sinnoh", 739, 306, Direction.Up);
            ReadTo("if it isn't"); Frames(4); Shot("pg51_route_227_if_it_isnt_the_champion");
            ReadTo("my master"); Frames(4); Shot("pg52_route_227_meet_my_master");
            ReadTo("head or tail"); Frames(4); Shot("pg53_route_227_master_he_says");
            ReadTo("fully prepared"); Frames(4);
            Box().Advance();
            Walking("Sinnoh", "crasher_wake", "route_227", he => he.GridY >= 308, "Crasher Wake going down the road");
            Shot("pg54_route_227_crasher_wake_goes");
            ReadTo("Battle Frontier"); Frames(4); Shot("pg55_route_227_off_to_the_battle_frontier");
            ReadTo("don't make me wait"); Frames(4);
            Box().Advance();
            Walking("Sinnoh", "rival", "route_227", he => he.GridY >= 308, "the rival running off");
            Shot("pg56_route_227_the_rival_runs_off");
            ReadTo(null);
            Console.WriteLine("route 227: " + Vars("VAR_ROUTE_227_WAKE_RIVAL_STATE") + ", " + Flags("FLAG_HIDE_ROUTE_227_RIVAL", "FLAG_HIDE_ROUTE_227_CRASHER_WAKE") + $", player at {Me().GridX},{Me().GridY}");

            // ---- further up: Buck
            ComeThrough("Sinnoh", 758, 279, Direction.Up);
            Frames(30); Shot("pg57_route_227_buck_up_the_road");
            StepOnto("Sinnoh", 758, 277, Direction.Up);
            ReadTo("It is you"); Frames(4); Shot("pg58_route_227_it_is_you");
            ToQuestion("Buck's question"); Shot("pg59_route_227_will_you_look_round");
            Answer(1);
            ReadTo("go on asking"); Frames(4); Shot("pg5a_route_227_ill_go_on_asking");
            ToQuestion("Buck's question again");
            Answer(0);
            ReadTo("goons"); Frames(4); Shot("pg5b_route_227_goons_in_weird_outfits");
            ReadTo("right behind you"); Frames(4); Shot("pg5c_route_227_ill_be_right_behind_you");
            ReadTo(null);
            Console.WriteLine("buck: " + Vars("VAR_ROUTE_227_BUCK_STATE") + $", at {Someone("Sinnoh", "buck", "route_227")?.GridX},{Someone("Sinnoh", "buck", "route_227")?.GridY}");

            // ---- the Survival Area: the Battleground's step, empty until Stark Mountain is done with
            ComeThrough("Sinnoh", 652, 341, Direction.Up);
            Frames(30); Shot("pg5d_survival_area_the_battlegrounds_step");
            Console.WriteLine($"survival area: Buck there {Someone("Sinnoh", "buck", "survival_area") != null}, the rival there {Someone("Sinnoh", "rival", "survival_area") != null}");
            // ...and as part b's last scene will leave it: both before the door
            engine.StartScript(ScriptParser.Parse("survival_area", "script Test\nclearflag FLAG_HIDE_SURVIVAL_AREA_BUCK\nclearflag FLAG_HIDE_SURVIVAL_AREA_RIVAL")[0]);
            ReadTo(null, 300);
            ComeThrough("Sinnoh", 652, 341, Direction.Up);
            Frames(30); Shot("pg5e_survival_area_buck_and_the_rival");
            TalkTo("Sinnoh", "rival", "survival_area");
            Whole(); Frames(4); Shot("pg5f_survival_area_the_rival_on_a_weekday");
            ReadTo(null);
            TalkTo("Sinnoh", "buck", "survival_area");
            ReadTo("my place"); Frames(4); Shot("pg5g_survival_area_this_is_my_place");
            ReadTo(null); Frames(20); Shot("pg5h_survival_area_buck_gone_in");
            // A Saturday: the rival battles
            var day = GameClock.FixedDate;
            GameClock.FixedDate = new DateTime(2026, 6, 6);
            TalkTo("Sinnoh", "rival", "survival_area");
            ReadTo("which of us is tougher"); Frames(4); Shot("pg5i_survival_area_the_weekend");
            IntoBattle("the rival's weekend battle");
            Skip(2.0); Shot("pg5j_survival_area_the_rivals_battle");
            Console.WriteLine($"the weekend: {GameClock.Today.DayOfWeek}, the rival's {string.Join(", ", game.Battle?.EnemyParty?.Members.Select(p => $"{p.Species.Name} {p.Level}") ?? Array.Empty<string>())}");
            Win();
            ReadTo("next week"); Frames(4); Shot("pg5k_survival_area_same_time_next_week");
            ReadTo(null);
            Console.WriteLine("the weekend's battle: " + Flags("FLAG_DAILY_DEFEATED_SURVIVAL_AREA_RIVAL", "FLAG_HIDE_SURVIVAL_AREA_BUCK"));
            GameClock.FixedDate = day;
            Frames(4);
        }

        if (stage <= 6)
        {
            // ---- Victory Road's back room: Marley
            const string Room = "VictoryRoad1FRoom2";
            ComeThrough(Room, 4, 58, Direction.Right);
            Frames(30); Shot("pg60_victory_road_marley_waits");
            // The room lies in fog: the rest is shot with it lifted, as Defog lifts it, so that the scenes can be seen
            story.Set(FieldMoveRules.DefogFlag);
            Frames(30); Shot("pg60b_victory_road_the_fog_lifted");
            StepOnto(Room, 5, 58, Direction.Right);
            Until(() => Someone(Room, "marley") is { GridX: 6 }, "Marley coming up", 300);
            Frames(6); Shot("pg61_victory_road_she_comes_up");
            ReadTo("Let me go with you"); Frames(4); Shot("pg62_victory_road_let_me_go_with_you");
            ReadTo("decided to go with Marley"); Frames(4); Shot("pg63_victory_road_decided_to_go_with_marley");
            ReadTo(null);
            Console.WriteLine("marley: " + Vars("VAR_VICTORY_ROAD_1F_ROOM_2_FOLLOWER_MARLEY_STATE") + $", travelling {game.PartnerAlong}");
            // A few steps with her behind
            engine.Steering = (Direction.Up, false);
            Until(() => Me().GridY <= 56, "two steps north", 240);
            engine.Steering = (Direction.Right, false);
            Until(() => Me().GridX >= 7, "two steps east", 240);
            engine.Steering = null;
            Frames(30); Shot("pg64_victory_road_she_walks_behind");
            Console.WriteLine($"walking: the player at {Me().GridX},{Me().GridY}, Marley at {Someone(Room, "marley")?.GridX},{Someone(Room, "marley")?.GridY}");
            game.Interact();
            // (she is behind the player: turn to her)
            if (!Box().IsActive)
            {
                var her = Someone(Room, "marley")!;
                var way = (her.GridX - Me().GridX, her.GridY - Me().GridY) switch { (1, 0) => Direction.Right, (-1, 0) => Direction.Left, (0, 1) => Direction.Down, _ => Direction.Up };
                At(Room, Me().GridX, Me().GridY, way);
                game.Interact();
            }
            Whole(); Frames(4); Shot("pg65_victory_road_marley_spoken_to");
            ReadTo(null);

            // ---- a pair of the room's trainers, who see the two together
            At(Room, 8, 49, Direction.Up);
            game.KeepPartnerAlong();
            Frames(10);
            engine.Steering = (Direction.Up, false);
            Until(() => engine.ScriptRunning || State() == GameState.Battle || Me().GridY <= 46, "the pair seeing the player", 400);
            engine.Steering = null;
            IntoBattle("the pair's tag battle");
            if (State() == GameState.Battle)
            {
                Skip(3.0); Shot("pg66_victory_road_a_tag_battle_beside_marley");
                Console.WriteLine($"beside Marley: {string.Join(", ", game.Battle!.Core.PlayerSlots.Where(s => s.Pokemon != null).Select(s => s.Pokemon!.Species.Name))} against "
                    + string.Join(", ", game.Battle.Core.EnemySlots.Where(s => s.Pokemon != null).Select(s => s.Pokemon!.Species.Name)));
                Win();
            }
            ReadTo(null);

            // ---- turning back sends her to wait; and she has other words for whoever comes back
            At(Room, 6, 58, Direction.Left);
            game.KeepPartnerAlong();
            Frames(10);
            engine.Steering = (Direction.Left, false);
            Until(() => engine.ScriptRunning && Me().GridX == 4, "the step back towards the way in", 300);
            engine.Steering = null;
            ReadTo("going back"); Frames(4); Shot("pg67_victory_road_youre_going_back");
            ReadTo(null); Frames(10); Shot("pg68_victory_road_she_waits_again");
            Console.WriteLine("turned back: " + Vars("VAR_VICTORY_ROAD_1F_ROOM_2_FOLLOWER_MARLEY_STATE") + $", Marley at {Someone(Room, "marley")?.GridX},{Someone(Room, "marley")?.GridY}, travelling {game.PartnerAlong}");
            StepOnto(Room, 5, 58, Direction.Right);
            ReadTo("took so long"); Frames(4); Shot("pg69_victory_road_you_took_so_long");
            ReadTo(null);

            // ---- the far end: she thanks the player and goes on
            At(Room, 52, 21, Direction.Up);
            game.KeepPartnerAlong();
            Frames(10);
            engine.Steering = (Direction.Up, false);
            Until(() => engine.ScriptRunning && Me().GridY == 19, "the step onto the room's last stretch", 400);
            engine.Steering = null;
            ReadTo("far enough"); Frames(4); Shot("pg6a_victory_road_this_is_far_enough");
            ReadTo("Thank you"); Frames(4); Shot("pg6b_victory_road_thank_you");
            Box().Advance();
            Walking(Room, "marley", null, she => she.GridY <= 16, "Marley going on ahead");
            Shot("pg6c_victory_road_she_goes_on_ahead");
            ReadTo(null); Frames(10); Shot("pg6d_victory_road_the_way_out");
            Console.WriteLine("parted: " + Vars("VAR_VICTORY_ROAD_1F_ROOM_2_FOLLOWER_MARLEY_STATE") + ", " + Flags("FLAG_TRAVELED_WITH_MARLEY", "FLAG_HIDE_VICTORY_ROAD_1F_ROOM_2_MARLEY")
                + $", Marley there {Someone(Room, "marley") != null}, travelling {game.PartnerAlong}");
        }

        // ---- Canalave's library: Lucian at the shelves
        ComeThrough("CanalaveLibrary1F", 11, 11, Direction.Up);
        At("CanalaveLibrary1F", 15, 6, Direction.Up);
        Frames(30); Shot("pg70_library_lucian_at_the_shelves");
        TalkTo("CanalaveLibrary1F", "lucian");
        ReadTo("books wonderful"); Frames(4); Shot("pg71_library_arent_books_wonderful");
        ReadTo("This one"); Frames(4); Shot("pg72_library_the_book_in_hand");
        ReadTo("Pokémon League"); Frames(4); Shot("pg73_library_see_you_at_the_league");
        Box().Advance();
        Walking("CanalaveLibrary1F", "lucian", null, he => he.GridX <= 12, "Lucian leaving");
        Shot("pg74_library_he_goes");
        ReadTo(null);
        Console.WriteLine($"lucian: there {Someone("CanalaveLibrary1F", "lucian") != null}, " + Flags("FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN"));

        // ---- the Game Corner: Looker on his holiday, as Stark Mountain's last scene will leave him (part b)
        engine.StartScript(ScriptParser.Parse("VeilstoneGameCorner", "script Test\nsetflag FLAG_ARRESTED_CHARON_STARK_MOUNTAIN\nclearflag FLAG_HIDE_GAME_CORNER_LOOKER")[0]);
        ReadTo(null, 300);
        ComeThrough("VeilstoneGameCorner", 9, 13, Direction.Up);
        var looker = Someone("VeilstoneGameCorner", "looker");
        if (looker == null) Console.WriteLine("  !! Looker isn't at the Game Corner");
        else
        {
            bool below = !game.Map.IsSolid(looker.GridX, looker.GridY + 1);
            TalkTo("VeilstoneGameCorner", "looker", null, below ? 0 : 1, below ? 1 : 0);
            ReadTo("holiday"); Frames(4); Shot("pg75_game_corner_looker_on_holiday");
            ReadTo(null);
        }
    }
}
