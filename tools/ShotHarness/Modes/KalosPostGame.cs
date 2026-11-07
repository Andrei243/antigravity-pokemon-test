using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

partial class Harness
{
    /// <summary>
    /// Plan 23 · Z4b's shots (the last of the <c>kalos</c> mode, and the whole of it with <c>KALOS_FROM=postgame</c>): Kalos
    /// after its Hall of Fame. The places by day and some after dark (<c>wk400</c> on: Lumiose's east gate and the station's
    /// square, Azure Bay's road, meadow, beach, sandbar and the den's mouth, the Pokémon Village's cave, Kiloude's hill,
    /// Anistar's sundial, two Mega Stones on Route 16), the rooms and caves (<c>wk420</c> on), and the scenes in a game of
    /// their own (<c>wks200</c> on: Mom, the bird over Vaniville, the professor at Lumiose Station and the TMV, the neighbour
    /// in Kiloude, the sundial, Looker's cases one by one, Zygarde, Mewtwo and the bird at rest in its den). It wins every
    /// battle a scene starts and prints what each scene left behind.
    /// </summary>
    public void KalosPostGame()
    {
        var story = game.Story;
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 600 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var foe in fight.EnemyParty?.Members ?? new List<Pokemon>()) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    if (fight.EnemyPokemon is { } met) met.CurrentHP = Math.Min(met.CurrentHP, 1);
                    var lead = fight.PlayerPokemon;
                    if (lead != null) lead.CurrentHP = lead.MaxHP;
                    fight.SelectMainMenuOption(0);
                    if (lead != null) foreach (var m in lead.Moves) m.CurrentPP = m.MaxPP;
                    int hurts = lead == null ? 0 : lead.Moves.Select((m, i) => (m, i)).Where(p => p.m.Data.Category != MoveCategory.Status && p.m.Data.Type != PokemonType.Ghost)
                        .OrderByDescending(p => p.m.Data.Power).Select(p => p.i).DefaultIfEmpty(0).First();
                    fight.SelectMove(hurts);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => FieldAgain(), "the field again", 1500);
            Frames(10);
        }
        void ReadTo(string? text, int most = 2400)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && Box().IsActive && Translated.Saying(Box().CurrentLine, text)) { Whole(); Console.WriteLine($"  said: {text}"); return; }
                idle = !engine.ScriptRunning && !Box().IsActive && State() == GameState.Overworld ? idle + 1 : 0;
                if (text != null && idle > 90) break;
                if (text == null && idle > 30) return;
                if (State() == GameState.Battle) { Win(); continue; }
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void ToBattle()
        {
            for (int i = 0; i < 1500 && State() != GameState.Battle; i++)
            {
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
        }
        void Battle(string scene, string shot)
        {
            ToBattle();
            if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot(shot); Win(); }
            else Console.WriteLine($"  !! never happened: {scene}");
        }
        void Talk(string map, int x, int y, Direction facing) { At(map, x, y, facing); Frames(2); game.Interact(); }
        void ComeThrough(string map, int x, int y, Direction facing) { At(map, x, y, facing); game.ArriveOnMap(); Frames(2); }
        void Left(string what) =>
            Console.WriteLine($"  {what}: post-game {story.Var("VAR_KALOS_POSTGAME")}, Looker {story.Var("VAR_KALOS_LOOKER")}, roads {story.Var("VAR_KALOS_LUMIOSE_ROADS")}, "
                + $"map {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), pass {story.Has("FLAG_KALOS_TMV_PASS")}, ring {story.Has("FLAG_KALOS_MEGA_RING_UPGRADED")}, "
                + $"bird {story.Has("FLAG_KALOS_BIRD_LOOSE")}, next {Objectives.Current(story, RegionDatabase.Kalos)?.Name}");

        // A game of Kalos brought to its Hall of Fame: eight Badges, the Hall of Fame entered, Lumiose's roads at 3
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        story.ChooseStarter("Froakie");
        foreach (var (species, level) in new[] { ("Greninja", 80), ("Talonflame", 78), ("Venusaur", 78), ("Lucario", 78) })
            game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, level));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        foreach (string badge in RegionDatabase.Get(RegionDatabase.Kalos)!.BadgeNames) story.GiveBadge(RegionDatabase.Kalos, badge);
        story.SetVar("VAR_KALOS_STORY", 31);
        story.Set("KalosHallOfFame");
        story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 3);
        // ... and what the story's own scenes left on the way there that the post-game's people meet: Lysandre gone from
        // his laboratory, where Xerosic stands later, and its two grunts beaten (LumioseCity_LysandreLabs.txt)
        story.Set("FLAG_KALOS_HIDE_LYSANDRE_LABS");
        story.Defeat("kalos_flare_grunt_labs_1");
        story.Defeat("kalos_flare_grunt_labs_2");
        Frames(2);
        Left("brought to the Hall of Fame");

        // ---- the scenes come first, in the order a game meets them, so the places after show the story as it ends up
        // Mom, home as the Champion
        Talk("VanivilleTown_PlayersHouse_1F", 7, 9, Direction.Up);
        ReadTo("There's my Champion"); Frames(10); Shot("wks200_mom_champion");
        ReadTo(null);
        // The bird over Vaniville, out of the front door
        // (its shadow crosses the street from the west in two seconds, over the player's column at the first: shot as it
        // comes, before the words)
        ComeThrough("Kalos", 242, 722, Direction.Down);
        Frames(34); Shot("wks201_bird_shadow");
        Until(() => Box().IsActive || !engine.ScriptRunning, "the bird's scene", 300);
        ReadTo("Moltres is loose in Kalos"); Frames(10); Shot("wks202_bird_loose");
        ReadTo(null);
        Left("the bird");
        // Lumiose Station: the professor and the TMV pass
        ComeThrough("LumioseCity_Station", 8, 10, Direction.Up);
        ReadTo("The Champion of Kalos"); Frames(10); Shot("wks203_station_assistants");
        ReadTo("With this pass you can ride"); Frames(10); Shot("wks204_station_pass");
        ReadTo(null);
        Left("Lumiose Station");
        Talk("LumioseCity_Station", 12, 5, Direction.Up);
        ReadTo("All aboard"); Frames(10); Shot("wks205_tmv_attendant");
        ReadTo(null); Frames(40);
        Shot("wks206_kiloude_by_train");
        Left("the TMV");
        // The neighbour on Kiloude's hill
        Talk("Kalos", 565, 337, Direction.Up);
        ReadTo("Battle me!"); Frames(10); Shot("wks207_kiloude_neighbour");
        Battle("the neighbour's battle", "wks208_kiloude_battle");
        ReadTo("a Mega Stone, for an Absol"); Frames(10); Shot("wks209_absolite");
        ReadTo(null);
        Left("Kiloude");
        // The sundial
        Talk("Kalos", 649, 220, Direction.Up);
        ReadTo("Hold your Key Stone up to it"); Frames(10); Shot("wks210_sundial");
        ReadTo("shone back at it"); Frames(10); Shot("wks211_sundial_glow");
        ReadTo(null);
        Left("the sundial");
        // Looker's first case
        ComeThrough("LumioseCity_LookerBureau", 5, 8, Direction.Up);
        ReadTo("I am called Looker"); Frames(10); Shot("wks212_looker_bureau");
        ReadTo(null);
        Left("the bureau");
        foreach (var (room, x, y, facing) in new[] { ("LumioseCity_Station", 15, 4, Direction.Up), ("LumioseCity_Museum", 3, 5, Direction.Up), ("LumioseCity_HotelRichissime", 15, 4, Direction.Up) })
        {
            Talk(room, x, y, facing);
            ReadTo(null);
        }
        Talk("LumioseCity_LookerBureau", 5, 6, Direction.Up);
        ReadTo("Then here is a real case"); Frames(10); Shot("wks213_looker_second_case");
        ReadTo(null);
        // The second: the park's children and Emma
        Talk("Kalos", 391, 379, Direction.Down);
        ReadTo("I'm guarding the park"); Frames(10); Shot("wks214_alley_children");
        Battle("Lou's battle", "wks215_alley_battle");
        ReadTo(null);
        foreach (var (x, y) in new[] { (387, 379), (395, 379), (399, 379) }) { Talk("Kalos", x, y, Direction.Down); ReadTo(null); }
        Talk("Kalos", 403, 379, Direction.Down);
        ReadTo("There are three alleys near here she likes"); Frames(10);
        ReadTo("Where would a frightened Espurr hide");
        Until(() => engine.Choice.IsOpen, "the alleys' question", 300);
        Frames(10); Shot("wks216_emma_alleys");
        ReadTo(null);
        Left("Emma and Mimi");
        Talk("LumioseCity_LookerBureau", 5, 6, Direction.Up);
        ReadTo(null);
        // The third: the gang at the station
        ComeThrough("LumioseCity_Station", 8, 10, Direction.Up);
        Frames(4); Shot("wks217_gang_station");
        foreach (var (x, y, facing) in new[] { (5, 5, Direction.Left), (5, 7, Direction.Left), (12, 7, Direction.Right) }) { Talk("LumioseCity_Station", x, y, facing); ReadTo(null); }
        Talk("LumioseCity_Station", 2, 7, Direction.Up);
        ReadTo("Give her Pokémon back"); Frames(10); Shot("wks218_emma_talks_them_round");
        ReadTo(null);
        Talk("LumioseCity_LookerBureau", 5, 6, Direction.Up);
        ReadTo(null);
        // The fourth: the museum, the sting and the butler
        Talk("LumioseCity_Museum", 8, 6, Direction.Up);
        ReadTo("someone broke in"); Frames(10); Shot("wks219_museum_break_in");
        ReadTo(null);
        Talk("Kalos", 402, 372, Direction.Down);
        Battle("the hooded woman's battle", "wks220_sting_battle");
        ReadTo(null);
        foreach (var (x, y, facing) in new[] { (373, 312, Direction.Down), (330, 376, Direction.Down) }) { Talk("Kalos", x, y, facing); ReadTo(null); }
        ComeThrough("LumioseCity_LookerBureau", 5, 8, Direction.Up);
        ReadTo("My mistress has heard"); Frames(10); Shot("wks221_the_butler");
        ReadTo(null);
        Talk("LumioseCity_LookerBureau", 4, 7, Direction.Left);
        ReadTo(null);
        // The fifth: Malva, the laboratory and Essentia
        Talk("LumioseCity_HotelRichissime", 11, 6, Direction.Right);
        ReadTo("Let me see for myself"); Frames(10); Shot("wks222_malva_hotel");
        ReadTo(null);
        // (come into the laboratory as through the café, so that the room takes the story as it is: its grunts beaten)
        ComeThrough("LumioseCity_LysandreLabs", 8, 14, Direction.Up);
        ReadTo(null);
        foreach (var (x, y, facing) in new[] { (4, 6, Direction.Left), (12, 6, Direction.Right) }) { Talk("LumioseCity_LysandreLabs", x, y, facing); ReadTo(null); }
        Talk("LumioseCity_LysandreLabs", 8, 7, Direction.Up);
        ReadTo("Intruder. Remove intruder"); Frames(10); Shot("wks223_essentia");
        Battle("Essentia's first battle", "wks224_essentia_battle");
        ReadTo("Mimi...?"); Frames(10); Shot("wks225_essentia_freed");
        ReadTo("I did not account for affection");
        ReadTo(null);
        Talk("LumioseCity_LysandreLabs", 8, 5, Direction.Up);
        ReadTo("It still fits"); Frames(10); Shot("wks226_xerosic");
        ReadTo(null);
        // The farewell
        Talk("LumioseCity_Museum", 9, 5, Direction.Up);
        ReadTo("you may call yourself Looker too"); Frames(10); Shot("wks227_looker_farewell");
        ReadTo(null);
        Left("Looker's episode");
        // Zygarde and Mewtwo
        Talk("TerminusCaveDepths", 31, 9, Direction.Up);
        ReadTo("It raised its head"); Frames(10); Shot("wks228_zygarde_wakes");
        Battle("Zygarde's battle", "wks229_zygarde_battle");
        ReadTo(null);
        Talk("UnknownDungeon", 25, 10, Direction.Left);
        ReadTo("Slowly, it turns its head"); Frames(10); Shot("wks230_mewtwo");
        Battle("Mewtwo's battle", "wks231_mewtwo_battle");
        ReadTo("two stones glimmer"); Frames(10); Shot("wks232_mewtwonites");
        ReadTo(null);
        // The bird at rest in Sea Spirit's Den, once it has fled a dozen times
        int slot = Roamers.SlotOf("Moltres", "kalos")!.Value;
        game.Encounters.Roamers[slot].Active = false;
        story.SetVar(Roamers.StateVariable(slot), Roamers.Resting);
        ComeThrough("SeaSpiritsDen", 15, 25, Direction.Up);
        At("SeaSpiritsDen", 15, 15, Direction.Up); Frames(4); Shot("wks233_den_bird_at_rest");
        Talk("SeaSpiritsDen", 14, 10, Direction.Right);
        ReadTo("has come to rest here"); Frames(10); Shot("wks234_den_bird_line");
        Battle("the bird's battle", "wks235_den_bird_battle");
        ReadTo(null);
        Left("the legendaries");
        // (the Pokédex's research notices the battles left are told one at a time: they are let run out first, so the
        // places show none)
        for (int quiet = 0, i = 0; quiet < 10 && i < 8000; i++) { Frames(1); quiet = game.Toast.Visible ? 0 : quiet + 1; }

        // ---- the places, as the story leaves them, by day
        (string Day, string? Night, string Map, int X, int Y, Direction Facing)[] places =
        {
            ("wk400_lumiose_east_gate", null, "Kalos", 414, 359, Direction.Right),
            ("wk401_station_square", "wk440_station_square_night", "Kalos", 433, 356, Direction.Up),
            ("wk402_lumiose_south_east_gate", null, "Kalos", 392, 416, Direction.Down),
            ("wk403_azure_road", null, "Kalos", 392, 430, Direction.Down),
            ("wk404_azure_meadow", null, "Kalos", 411, 434, Direction.Down),
            ("wk405_azure_beach", "wk441_azure_beach_night", "Kalos", 420, 448, Direction.Down),
            ("wk406_azure_sandbar", null, "Kalos", 420, 462, Direction.Down),
            ("wk407_den_mouth", null, "Kalos", 419, 474, Direction.Down),
            ("wk408_village_cave", null, "Kalos", 698, 110, Direction.Up),
            ("wk409_kiloude_hill", "wk442_kiloude_hill_night", "Kalos", 565, 339, Direction.Up),
            ("wk410_anistar_sundial", null, "Kalos", 648, 222, Direction.Up),
            ("wk411_route16_stones", null, "Kalos", 386, 243, Direction.Up),
            ("wk420_station", null, "LumioseCity_Station", 8, 9, Direction.Up),
            ("wk421_looker_bureau", null, "LumioseCity_LookerBureau", 5, 7, Direction.Up),
            ("wk422_museum", null, "LumioseCity_Museum", 8, 9, Direction.Up),
            ("wk423_hotel", null, "LumioseCity_HotelRichissime", 8, 9, Direction.Up),
            ("wk424_terminus_way_down", null, "TerminusCave", 31, 20, Direction.Up),
            ("wk425_depths_basement", null, "TerminusCaveDepths", 31, 12, Direction.Up),
            ("wk426_depths_chamber", null, "TerminusCaveDepths", 30, 27, Direction.Up),
            ("wk427_unknown_dungeon", null, "UnknownDungeon", 31, 44, Direction.Up),
            ("wk428_unknown_dungeon_end", null, "UnknownDungeon", 30, 11, Direction.Left),
            ("wk429_sea_spirits_den", null, "SeaSpiritsDen", 15, 16, Direction.Up),
            ("wk430_cyllage_gym_stones", null, "CyllageCity_Gym", 6, 14, Direction.Down),
        };
        foreach (var (day, _, map, x, y, facing) in places)
        {
            At(map, x, y, facing);
            // (a room or a cave is come into, as a door would bring the player, so its people stand as the story has them)
            if (map != "Kalos") { game.ArriveOnMap(); ReadTo(null); }
            Frames(2); game.LocationSign.Hide(); Shot(day);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (_, night, map, x, y, facing) in places)
        {
            if (night == null) continue;
            At(map, x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(night);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        Left("the end");
    }
}
