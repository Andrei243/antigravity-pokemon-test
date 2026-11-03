partial class Harness
{
    /// <summary>
    /// Plan 23 · Z2's second part, the last of the <c>kalos</c> mode (and the whole of it with <c>KALOS_FROM=central</c>):
    /// Lumiose's tower of light redrawn, seen from its foot and from further off (<c>wk74</c> to <c>wk76</c>), the west gate,
    /// Route 5, Camphrier Town and its castle, Route 6 and the palace gate, Route 7 with its sleeping Snorlax and its river,
    /// Route 8's cliff path and Cyllage City's beach, street and Gym, by day and some after dark (<c>wk77</c> to <c>wk99</c>), the
    /// new rooms (<c>wk100</c> to <c>wk105</c>), and the story from Lysandre's café to the Cliff Badge in a new game of its own,
    /// brought at once to where the café left it (<c>wks30</c> to <c>wks40</c>: Tierno on Route 5, the castle's keeper, the
    /// palace's flute, Snorlax woken, the neighbour in Cyllage, Grant and the Cliff Badge, and Lumiose's boutique).
    /// </summary>
    public void KalosCentral()
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
                    // The strongest move that hurts, its PP made whole (a Ghost move does nothing to Snorlax, and a long battle runs a move dry)
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
            Console.WriteLine($"  {what}: story {story.Var("VAR_KALOS_STORY")}, roads {story.Var("VAR_KALOS_LUMIOSE_ROADS")}, team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}, "
                + $"badges {story.BadgeCount(PokemonPlatinumEngine.Data.RegionDatabase.Kalos)}, flute {game.Bag.GetQuantity(PokemonPlatinumEngine.Data.ItemDatabase.Get("Poké Flute")!)}, next {PokemonPlatinumEngine.Story.Objectives.Current(story, PokemonPlatinumEngine.Data.RegionDatabase.Kalos)?.Name}");

        // A new game of Kalos brought at once to where Lysandre's café leaves it: the Bug Badge, Kanto's three met, the road west open
        engine.NewGameRegion = "Kalos";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        story.ChooseStarter("Froakie");
        foreach (var (species, level) in new[] { ("Frogadier", 26), ("Fletchinder", 24), ("Ivysaur", 24) })
            game.Party.Add(new Pokemon(PokemonPlatinumEngine.Data.PokemonDatabase.Get(species)!, level));
        story.Set(StoryState.PokedexFlag);
        story.Set(StoryState.RunningShoesFlag);
        story.GiveBadge(PokemonPlatinumEngine.Data.RegionDatabase.Kalos, "Bug");
        story.SetVar("VAR_KALOS_STORY", 12);
        story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 1);
        Frames(2);
        Left("brought past the café");

        // ---- the places, by day
        (string Day, string? Night, int X, int Y, Direction Facing)[] places =
        {
            ("wk74_prism_tower_foot", "wk75_prism_tower_foot_night", 359, 364, Direction.Up),
            ("wk76_prism_tower_south", null, 359, 374, Direction.Up),
            ("wk77_lumiose_west_gate", null, 306, 359, Direction.Left),
            ("wk78_route5_square", "wk79_route5_square_night", 262, 360, Direction.Left),
            ("wk80_route5_west", null, 238, 359, Direction.Left),
            ("wk81_camphrier", "wk82_camphrier_night", 202, 364, Direction.Up),
            ("wk83_camphrier_castle", null, 214, 350, Direction.Up),
            ("wk84_camphrier_center", null, 190, 360, Direction.Up),
            ("wk85_route6", null, 201, 300, Direction.Up),
            ("wk86_route6_gate", null, 201, 274, Direction.Up),
            ("wk87_route7_snorlax", null, 202, 397, Direction.Up),
            ("wk88_route7_river", "wk89_route7_river_night", 160, 399, Direction.Left),
            ("wk90_route8", null, 103, 395, Direction.Up),
            ("wk91_route8_cliffs", null, 103, 372, Direction.Up),
            ("wk92_cyllage_beach", "wk93_cyllage_beach_night", 92, 352, Direction.Up),
            ("wk94_cyllage_street", "wk95_cyllage_street_night", 103, 342, Direction.Up),
            ("wk96_cyllage_middle", null, 103, 330, Direction.Up),
            ("wk97_cyllage_gym", "wk98_cyllage_gym_night", 103, 318, Direction.Up),
        };
        foreach (var (day, _, x, y, facing) in places)
        {
            At("Kalos", x, y, facing); Frames(2); game.LocationSign.Hide(); Shot(day);
        }
        ShotCrop("wk97_cyllage_gym_native", 640, 300, 640, 360, 2);

        // The tower from further off, the lens zoomed out, the way wk68 had it
        var rig = game.World.Camera;
        At("Kalos", 359, 372, Direction.Up); Frames(2);
        rig.ZoomTo(0.6f, 0f);
        Frames(30); game.LocationSign.Hide(); Shot("wk99_prism_tower_wide");
        rig.Reset();
        Frames(2);

        (string Name, string Map, int X, int Y, Direction Facing)[] rooms =
        {
            ("wk100_camphrier_center_inside", "CamphrierTown_PokemonCenter_1F", 8, 9, Direction.Up),
            ("wk101_camphrier_castle_inside", "CamphrierTown_Castle", 7, 9, Direction.Up),
            ("wk102_cyllage_gym_inside", "CyllageCity_Gym", 6, 13, Direction.Up),
            ("wk103_lumiose_boutique", "LumioseCity_Boutique", 4, 8, Direction.Up),
            ("wk104_lumiose_stone_shop", "LumioseCity_StoneEmporium", 4, 8, Direction.Up),
            ("wk105_cyllage_bike_shop", "CyllageCity_BikeShop", 4, 8, Direction.Up),
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

        // ---- Lumiose's boutique: its clerk and the wardrobe on its own stock
        game.Money = 20000;
        At("LumioseCity_Boutique", 7, 7, Direction.Up); Frames(2);
        game.Interact(); Frames(3);
        for (int i = 0; i < 40 && State() != GameState.Wardrobe; i++)
        {
            if (Box().IsActive && !Box().IsQuestion) { Whole(); Box().Advance(); Frames(2); }
            else Frames(4);
        }
        Until(() => State() == GameState.Wardrobe, "the boutique's wardrobe", 300);
        if (State() == GameState.Wardrobe)
        {
            var screen = game.WardrobeScreen;
            Until(() => !screen.Dressing, "the outfit made", 3000);
            Frames(30); Shot("wks30_boutique_lumiose");
            screen.Cancel(); Frames(4);
        }
        for (int i = 0; i < 40 && (Box().IsActive || engine.ScriptRunning); i++)
        {
            if (Box().IsActive && !Box().IsQuestion) { Whole(); Box().Advance(); Frames(2); }
            else Frames(4);
        }
        Console.WriteLine($"  after the boutique: state {State()}, script {engine.ScriptRunning}");

        // ---- Route 5: Tierno and Trevor, and Tierno's battle
        StepOnto("Kalos", 278, 359, Direction.Left);
        ReadTo("We skated all the way from Lumiose"); Frames(10); Shot("wks31_tierno_on_route5");
        ToBattle();
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks32_tiernos_battle"); Win(); }
        else Console.WriteLine("  !! never happened: Tierno's battle");
        ReadTo(null);
        Left("Route 5");

        // ---- Camphrier Town: the castle's keeper
        StepOnto("Kalos", 222, 359, Direction.Left);
        ReadTo("I am Aubert"); Frames(10); Shot("wks33_the_castles_keeper");
        ReadTo(null);
        Left("Camphrier Town");

        // ---- Route 6: the palace's butler lends the flute
        At("Kalos", 201, 269, Direction.Up); Frames(2);
        game.Interact();
        ReadTo("Monsieur Aubert sent you"); Frames(10); Shot("wks34_the_palace_butler");
        ReadTo(null);
        Left("the palace gate");

        // ---- Route 7: the flute played and Snorlax woken
        At("Kalos", 202, 393, Direction.Up); Frames(2);
        Shot("wks35_snorlax_asleep");
        game.Interact();
        ReadTo("Snorlax woke up"); Frames(10); Shot("wks36_snorlax_wakes");
        ToBattle();
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks37_snorlax_battle"); Win(); }
        else Console.WriteLine("  !! never happened: Snorlax's battle");
        ReadTo(null);
        Left("Route 7");

        // ---- Cyllage City: the neighbour at the foot of the street
        StepOnto("Kalos", 103, 358, Direction.Up);
        ReadTo("You woke that Snorlax"); Frames(10); Shot("wks38_the_neighbour_in_cyllage");
        ReadTo(null);
        Left("Cyllage City");

        // ---- the Gym: Grant, his battle and the Cliff Badge
        At("CyllageCity_Gym", 6, 5, Direction.Up); Frames(2);
        game.Interact();
        ReadTo("I'm Grant"); Frames(10); Shot("wks39_grant");
        ToBattle();
        if (State() == GameState.Battle) { Skip(1.0); ToMainMenu(game.Battle); Shot("wks40_grants_battle"); Win(); }
        else Console.WriteLine("  !! never happened: Grant's battle");
        ReadTo("The Cliff Badge is yours"); Frames(10); Shot("wks41_the_cliff_badge");
        ReadTo(null);
        Left("the Gym");
        game.DeleteSave();
    }
}
