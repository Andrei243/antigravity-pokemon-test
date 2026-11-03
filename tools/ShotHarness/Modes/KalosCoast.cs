partial class Harness
{
    /// <summary>
    /// Plan 23 · Z3's shots (the last of the <c>kalos</c> mode, and the whole of it with <c>KALOS_FROM=coast</c>): Route 10's
    /// standing stones, Geosenge Town, Route 11 and Shalour City by day and some after dark (<c>wk106</c> on), the new rooms,
    /// and the story from the Cliff Badge to the Key Stone in a new game of its own (<c>wks42</c> on: Trevor in Geosenge, the
    /// tower's first visit, Korrina's battle and Badge, the Key Stone, her Mega Lucario in battle and the Lucario received),
    /// and last a battle of the player's own Lucario Mega Evolving (<c>wkm01</c> on).
    /// </summary>
    public void KalosCoast()
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
            for (int guard = 0; guard < 400 && !fight.IsBattleOver; guard++)
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
        void StepOnto(string map, int x, int y, Direction way)
        {
            var (dx, dy) = way switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            At(map, x - dx, y - dy, way);
            game.EnterArea();
            Frames(2);
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning, $"the scene at {x},{y}", 120);
            engine.Steering = null;
        }
        void Left(string what) =>
            Console.WriteLine($"  {what}: story {story.Var("VAR_KALOS_STORY")}, team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"badges {story.BadgeCount(PokemonPlatinumEngine.Data.RegionDatabase.Kalos)}, key stone {game.Bag.GetQuantity(PokemonPlatinumEngine.Data.ItemDatabase.Get("Key Stone")!)}, next {PokemonPlatinumEngine.Story.Objectives.Current(story, PokemonPlatinumEngine.Data.RegionDatabase.Kalos)?.Name}");

        // A new game of Kalos brought at once to where the Cyllage Gym leaves it: the Cliff Badge won
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        story.ChooseStarter("Froakie");
        foreach (var (species, level) in new[] { ("Frogadier", 30), ("Fletchinder", 28), ("Ivysaur", 28) })
            game.Party.Add(new Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get(species)!, level));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        story.GiveBadge(PokemonPlatinumEngine.Data.RegionDatabase.Kalos, "Bug");
        story.GiveBadge(PokemonPlatinumEngine.Data.RegionDatabase.Kalos, "Cliff");
        story.SetVar("VAR_KALOS_STORY", 18);
        story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 1);
        Frames(2);
        Left("brought past Grant");

        (string Day, string? Night, int X, int Y, Direction Facing)[] places =
        {
            ("wk106_route10_stones", "wk107_route10_stones_night", 119, 270, Direction.Up),
            ("wk108_route10_north", null, 119, 250, Direction.Up),
            ("wk109_geosenge", "wk110_geosenge_night", 120, 212, Direction.Up),
            ("wk111_geosenge_center", null, 127, 218, Direction.Up),
            ("wk112_route11_east", null, 150, 205, Direction.Right),
            ("wk113_route11", "wk114_route11_night", 165, 190, Direction.Up),
            ("wk115_route11_north", null, 165, 160, Direction.Up),
            ("wk116_shalour_square", "wk117_shalour_square_night", 165, 130, Direction.Up),
            ("wk118_shalour_tower", null, 165, 112, Direction.Up),
            ("wk119_shalour_gym", null, 181, 128, Direction.Up),
            // Plan 23 · Z3, second part: Route 8 south, Ambrette Town, Route 9 and the two caves' mouths
            ("wk130_route8_south", null, 103, 432, Direction.Up),
            ("wk131_route8_beach", null, 96, 448, Direction.Left),
            ("wk132_ambrette_avenue", "wk133_ambrette_avenue_night", 103, 476, Direction.Up),
            ("wk134_ambrette_street", null, 100, 480, Direction.Up),
            ("wk135_ambrette_pier", null, 103, 496, Direction.Down),
            ("wk136_route9_west", null, 140, 483, Direction.Right),
            ("wk137_route9_cave_mouth", "wk138_route9_cave_mouth_night", 168, 478, Direction.Up),
            ("wk139_route9_east", null, 195, 483, Direction.Right),
            ("wk140_route11_cave_mouth", null, 172, 178, Direction.Up),
            // Route 12 and Coumarine City
            ("wk150_route12_flowers", "wk151_route12_flowers_night", 218, 483, Direction.Right),
            ("wk152_coumarine_gate", null, 233, 483, Direction.Right),
            ("wk153_coumarine_street", "wk154_coumarine_street_night", 250, 480, Direction.Up),
            ("wk155_coumarine_gym_front", null, 268, 478, Direction.Up),
            ("wk156_coumarine_garden", null, 262, 492, Direction.Down),
        };
        foreach (var (day, _, x, y, facing) in places)
        {
            At("Kalos", x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(day);
        }
        ShotCrop("wk118_shalour_tower_native", 640, 200, 640, 360, 2);
        // The tower from further off, the lens zoomed out, to see its stages whole
        var rig = game.World.Camera;
        At("Kalos", 172, 108, Direction.Up); Frames(2);
        rig.ZoomTo(0.5f, 0f);
        Frames(60); game.LocationSign.Hide(); Shot("wk118b_shalour_tower_wide");
        rig.Reset();
        Frames(2);

        (string Name, string Map, int X, int Y, Direction Facing)[] rooms =
        {
            ("wk120_geosenge_center_inside", "GeosengeTown_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk121_geosenge_nw_house", "GeosengeTown_NorthwestHouse", 4, 7, Direction.Up),
            ("wk122_geosenge_east_house", "GeosengeTown_EastHouse", 4, 7, Direction.Up),
            ("wk123_shalour_center_inside", "ShalourCity_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk124_shalour_gym_inside", "ShalourCity_Gym", 6, 13, Direction.Up),
            ("wk125_shalour_tower_inside", "ShalourCity_TowerOfMastery", 7, 14, Direction.Up),
            ("wk126_shalour_sw_house", "ShalourCity_SouthwestHouse", 4, 7, Direction.Up),
            ("wk127_shalour_se_house", "ShalourCity_SoutheastHouse", 4, 7, Direction.Up),
            ("wk146_ambrette_center_inside", "AmbretteTown_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk147_ambrette_west_house", "AmbretteTown_WestHouse", 4, 7, Direction.Up),
            ("wk148_ambrette_aquarium", "AmbretteTown_Aquarium", 4, 7, Direction.Up),
            ("wk149_ambrette_cottage", "AmbretteTown_Cottage", 4, 7, Direction.Up),
            ("wk157_coumarine_center_inside", "CoumarineCity_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk158_coumarine_garden_house", "CoumarineCity_GardenHouse", 4, 7, Direction.Up),
            ("wk159_coumarine_east_house", "CoumarineCity_EastHouse", 4, 7, Direction.Up),
            // The caves, maps of their own
            ("wk141_glittering_corridor", "GlitteringCave", 23, 32, Direction.Up),
            ("wk142_glittering_chamber", "GlitteringCave", 20, 22, Direction.Up),
            ("wk143_glittering_north", "GlitteringCave", 28, 10, Direction.Up),
            ("wk144_reflection_corridor", "ReflectionCave", 21, 28, Direction.Up),
            ("wk145_reflection_hall", "ReflectionCave", 14, 22, Direction.Up),
            ("wk145b_reflection_hall_east", "ReflectionCave", 28, 16, Direction.Up),
        };
        foreach (var (name, map, x, y, facing) in rooms) { At(map, x, y, facing); Frames(2); Shot(name); }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (_, night, x, y, facing) in places)
        {
            if (night == null) continue;
            At("Kalos", x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(night);
        }
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // ---- Geosenge: Trevor
        StepOnto("Kalos", 119, 227, Direction.Up);
        ReadTo("I've been reading about these stones"); Frames(10); Shot("wks42_trevor_in_geosenge");
        ReadTo(null);
        Left("Geosenge");

        // ---- the tower's first visit
        At("ShalourCity_TowerOfMastery", 7, 7, Direction.Up); Frames(2);
        game.Interact();
        ReadTo("I am Gurkinn"); Frames(10); Shot("wks43_gurkinn");
        ReadTo("Grandpa only gives Key Stones"); Frames(10); Shot("wks44_korrina_at_the_tower");
        ReadTo(null);
        Left("the tower, first visit");

        // ---- the Gym
        At("ShalourCity_Gym", 6, 5, Direction.Up); Frames(2);
        game.Interact();
        ReadTo("I'm Korrina"); Frames(10); Shot("wks45_korrina_in_her_gym");
        ToBattle();
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks46_korrinas_battle"); Win(); }
        else Console.WriteLine("  !! never happened: Korrina's battle");
        ReadTo("This is the Rumble Badge"); Frames(10); Shot("wks47_the_rumble_badge");
        ReadTo(null);
        Left("the Gym");

        // ---- the Key Stone, her Mega Lucario and the Lucario received
        At("ShalourCity_TowerOfMastery", 7, 7, Direction.Up); Frames(2);
        game.Interact();
        ReadTo("This Key Stone is yours"); Frames(10); Shot("wks48_the_key_stone");
        ReadTo("My turn! Watch closely"); Frames(10); Shot("wks49_korrina_shows_mega");
        ToBattle();
        if (State() == GameState.Battle)
        {
            Skip(1.0); ToMainMenu(game.Battle); Shot("wks50_korrinas_mega_battle");
            // Her Lucario's Mega Evolution, shot as it plays through the first move
            var fight = game.Battle;
            fight.SelectMainMenuOption(0); Frames(5);
            fight.SelectMove(0);
            for (int i = 0; i < 24; i++) { Skip(0.25); if (i % 3 == 0) Shot($"wks51_korrinas_mega_{i / 3}"); if (fight.HUD.MenuState == BattleMenuState.Main) break; Confirm(fight); }
            Win();
        }
        else Console.WriteLine("  !! never happened: Korrina's demonstration");
        ReadTo("Here, take this Lucario"); Frames(10); Shot("wks52_the_lucario_received");
        ReadTo(null);
        Left("the tower, the Key Stone");

        // ---- the player's own Lucario Mega Evolving in a wild battle
        var lucario = game.Party.Members.FirstOrDefault(p => p.Species.Name == "Lucario");
        if (lucario == null) Console.WriteLine("  !! no Lucario in the team");
        else
        {
            game.Party.Members.Remove(lucario);
            game.Party.Members.Insert(0, lucario);
            lucario.CurrentHP = lucario.MaxHP;
            game.Map = MapDatabase.Get("Kalos");
            game.Player.SetPosition(165, 190, Direction.Up);
            game.BattleRenderer.SetArena(game.Map, 165, 190);
            var bag = new Inventory();
            bag.AddItem(ItemDatabase.Get(PokemonPlatinumEngine.Battle.Sim.BattleCore.KeyStoneItem)!);
            var b = new BattleEngine(new BattleSetup
            {
                PlayerParty = game.Party, Inventory = bag, Pokedex = pokedex, WildPokemon = new List<Pokemon> { Meet("Snorlax", 40) },
                Random = new BattleRandom(77).Force(RollKind.Critical, 1).Force(RollKind.Accuracy, 0), Rules = Ruleset.Modern
            });
            game.Battle = b;
            game.State = GameState.Battle;
            ToMainMenu(b);
            Shot("wkm01_lucario_menu");
            b.SelectMainMenuOption(0); Frames(5);
            Console.WriteLine($"  Mega armed: {b.ToggleLaterMechanic()}");
            Shot("wkm02_mega_armed");
            b.SelectMove(0); Frames(1);
            for (int i = 0; i < 14; i++) { Skip(0.2); Shot($"wkm03_mega_evolving_{i}"); }
            for (int i = 0; i < 60 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver; i++) { if (b.IsWaitingForConfirm) Confirm(b); else Skip(0.1); }
            Skip(0.8); Shot("wkm04_mega_lucario_menu");
            Console.WriteLine($"  after: form {b.PlayerPokemon?.Form}, shown {b.Anim.Player.Shown?.Form}");
        }
        game.DeleteSave();
    }
}
