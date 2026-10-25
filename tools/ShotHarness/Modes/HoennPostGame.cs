partial class Harness
{
    /// <summary>
    /// The post-game of Hoenn's Hall of Fame, part a (plan 21 · H12), last of the <c>hoenn</c> mode so nothing before it
    /// moves (<c>HOENN_FROM=postgame</c> runs it alone, in a new game): Route 134's dark water and the floor under it
    /// (<c>h240</c>, <c>h241</c>), the Sealed Chamber's corridor and its end (<c>h242</c>), the chamber's pool, hall and wall
    /// (<c>h243</c> to <c>h245</c>), Dig used before the wall as the party menu's choice does and the wall dug (<c>h246</c>,
    /// <c>h247</c>), the inner room and its back wall and the riddle read with a Wailord first and a Relicanth last, the
    /// Regis' doors opening far off (<c>h248</c> to <c>h24b</c>); the Island Cave's wall, a lap run to its four corners
    /// and the wall opened, Regice in its chamber (<c>h250</c> to <c>h255</c>), the Desert Ruins' wall broken by Rock Smash
    /// and Regirock battled (<c>h256</c> to <c>h25b</c>), the Ancient Tomb's wall lit by Flash and Registeel (<c>h25c</c> to
    /// <c>h261</c>); and the ferry desk's attendant giving Southern Island's pass, the beach, the garden and the stone with
    /// the eon Pokémon that isn't roaming coming down to it (<c>h262</c> to <c>h268</c>). It prints what each left behind
    /// (the flags, the walls and ways, who is where). Written, not run, by the session that built it.
    /// </summary>
    void HoennPostGameShots()
    {
        if (!HoennReady("the post-game's places")) return;
        string shotName = "";
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // Reads on through what is said and asked (the first answer, yes) until the script is over and the field is the player's
        void ReadOn()
        {
            for (int i = 0; i < 1800 && (engine.ScriptRunning || State() != GameState.Overworld); i++)
            {
                if (State() == GameState.Battle) return;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(6);
        }
        void StepOnto(string map, int x, int y, Direction way)
        {
            var (dx, dy) = way switch { Direction.Up => (0, -1), Direction.Down => (0, 1), Direction.Left => (-1, 0), _ => (1, 0) };
            At(map, x - dx, y - dy, way);
            game.EnterArea();
            Frames(2);
            engine.Steering = (way, false);
            Until(() => engine.ScriptRunning || (game.Player.GridX, game.Player.GridY) == (x, y), $"the step onto {x},{y}", 120);
            engine.Steering = null;
            Frames(4);
        }
        // A battle won as the other modes win theirs: the foe left a single HP at each menu
        void FightOn()
        {
            Until(() => State() == GameState.Battle, "the battle", 600);
            if (State() != GameState.Battle) return;
            var fight = game.Battle;
            ToMainMenu(fight);
            Shot($"{shotName}_battle");
            for (int guard = 0; guard < 300 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    if (fight.EnemyPokemon is { } foe) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(Math.Max(0, fight.PlayerPokemon?.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status) ?? 0));
                }
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => State() == GameState.Overworld, "the field again", 1800);
            Frames(10);
        }
        void Flags(string label, params string[] flags) =>
            Console.WriteLine($"  {label}: " + string.Join(", ", flags.Select(f => $"{f[5..]} {(game.Story.Has(f) ? "set" : "clear")}")));

        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The team: a Wailord first and a Relicanth last, between them a Pokémon that knows Dig, Rock Smash, Flash and Dive;
        // every badge of Hoenn, the Hall of Fame entered and the pass for the ferry given
        var party = game.Party;
        party.Clear();
        Pokemon Knowing(string species, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, 50);
            p.Moves.Clear();
            foreach (string move in moves) p.Moves.Add(new Move(MoveDatabase.Get(move)!));
            return p;
        }
        party.Add(Knowing("Wailord", "Surf", "Splash"));
        party.Add(Knowing("Swampert", "Dig", "Rock Smash", "Flash", "Dive"));
        party.Add(Knowing("Relicanth", "Rock Slide", "Headbutt"));
        game.Story.SetBadges("Hoenn", 0xFF);
        game.Story.Set("HoennHallOfFame");
        game.Story.Set("FLAG_HOENN_CHAPTER_FOURTEEN");
        foreach (string hidden in new[] { "FLAG_HIDE_REGIROCK", "FLAG_HIDE_REGICE", "FLAG_HIDE_REGISTEEL" }) game.Story.Set(hidden);

        // ---- Route 134's dark water, down to the floor under it
        var sea = MapDatabase.Get("Hoenn");
        var dive = Enumerable.Range(240, 100).SelectMany(x => Enumerable.Range(255, 70).Select(y => (X: x, Y: y)))
            .FirstOrDefault(t => sea.AreaAt(t.X, t.Y)?.Key == "route_134" && sea.BehaviourAt(t.X, t.Y) == TileBehavior.DiveWater);
        if (dive == default) { Console.WriteLine("  !! no dark water on Route 134"); return; }
        At("Hoenn", dive.X, dive.Y, Direction.Up);
        game.Player.SetMode(TravelMode.Surfing);
        game.Player.Moves = FieldMovement.MovesOf(party);
        Frames(30);
        Shot("h240_route134_dark_water");
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "Dive's words"); Box().FinishLine(); Frames(2);
        Until(() => engine.Choice.IsOpen, "Dive's question"); Frames(10);
        engine.Choice.Confirm(); Frames(4);
        ReadOn();
        Frames(30);
        game.LocationSign.Hide();
        Console.WriteLine($"  dived from Route 134 at ({dive.X}, {dive.Y}): {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), {game.Player.Mode}");
        if (game.Map.Name != "Underwater_Route134") Console.WriteLine("  !! the dive never reached the floor under Route 134");
        Shot("h241_floor_under_route134");

        // ---- The long corridor, and the tile at its end that comes up in the chamber
        At("Underwater_SealedChamber", 7, 4, Direction.Down);
        game.Player.SetMode(TravelMode.Diving);
        Frames(20); Shot("h242_sealed_chamber_corridor_top");
        At("Underwater_SealedChamber", 12, 43, Direction.Down);
        game.Player.SetMode(TravelMode.Diving);
        Frames(20); Shot("h242b_sealed_chamber_corridor_end");
        ShotCrop("h242b_sealed_chamber_corridor_end_native", 640, 300, 640, 360, 2);

        // ---- The chamber's pool where the corridor comes up, its hall of dots and the wall at the north end
        At("SealedChamber_OuterRoom", 10, 19, Direction.Up);
        game.Player.SetMode(TravelMode.Surfing);
        Frames(20); Shot("h243_sealed_chamber_pool");
        At("SealedChamber_OuterRoom", 10, 12, Direction.Up);
        Frames(20); Shot("h244_sealed_chamber_hall");
        At("SealedChamber_OuterRoom", 10, 3, Direction.Up);
        Frames(20); Shot("h245_sealed_chamber_wall");
        ShotCrop("h245_sealed_chamber_wall_native", 640, 300, 640, 360, 2);
        game.Interact(); Frames(3);
        ReadOn();
        Console.WriteLine($"  the wall read: the way through shut {game.Map.IsSolid(10, 2)}");

        // ---- Dig, chosen as the party menu chooses it, standing before the wall
        game.UseFieldMove(FieldMove.Dig, 1);
        Frames(24); Shot("h246_dig_cut_in");
        ReadOn();
        Frames(20); Shot("h247_wall_dug");
        Flags("the wall", "FLAG_SYS_BRAILLE_DIG");
        Console.WriteLine($"  the way through shut {game.Map.IsSolid(10, 2)}");

        // ---- The inner room, and its back wall's riddle with a Wailord first and a Relicanth last
        At("SealedChamber_InnerRoom", 10, 18, Direction.Up);
        game.Player.SetMode(TravelMode.OnFoot);
        Frames(20); Shot("h248_inner_room");
        At("SealedChamber_InnerRoom", 10, 5, Direction.Up);
        Frames(20); Shot("h249_inner_back_wall");
        game.Interact(); Frames(3);
        for (int i = 0; i < 12 && Box().IsActive && !Box().IsQuestion; i++)
        {
            Until(() => Box().IsActive, "the dots' words"); Box().FinishLine(); Frames(2);
            if (i == 0) Shot("h24a_back_wall_read");
            Box().Advance(); Frames(2);
        }
        ReadOn();
        Flags("the riddle", "FLAG_REGI_DOORS_OPENED");

        // ---- The Island Cave: the wall's dots, the lap by its four corners and back, the way open and Regice
        At("IslandCave", 8, 21, Direction.Up);
        Frames(20); Shot("h250_island_cave_wall");
        game.Interact(); Frames(3);
        ReadOn();
        Console.WriteLine($"  the lap begun: {game.Story.Var("VAR_ISLAND_CAVE_LAP")}");
        foreach (var (x, y, way) in new[] { (4, 22, Direction.Left), (4, 27, Direction.Down), (12, 27, Direction.Right), (12, 22, Direction.Up) })
        {
            StepOnto("IslandCave", x, y, way);
            Console.WriteLine($"  at {x}, {y}: the lap {game.Story.Var("VAR_ISLAND_CAVE_LAP")}");
            if ((x, y) == (4, 27)) Shot("h251_island_cave_lap");
        }
        StepOnto("IslandCave", 8, 21, Direction.Left);
        ReadOn();
        Frames(20); Shot("h252_island_cave_open");
        Flags("the wall", "FLAG_SYS_BRAILLE_REGICE_COMPLETED");
        At("IslandCave", 8, 10, Direction.Up);
        game.ArriveOnMap(); Frames(20); Shot("h253_island_cave_chamber");
        shotName = "h254_regice";
        game.Interact(); Frames(3);
        ReadOn();
        FightOn();
        Flags("Regice", "FLAG_DEFEATED_REGICE", "FLAG_HIDE_REGICE");

        // ---- The Desert Ruins: Rock Smash before the wall, and Regirock
        At("DesertRuins", 8, 21, Direction.Up);
        Frames(20); Shot("h256_desert_ruins_wall");
        game.UseFieldMove(FieldMove.RockSmash, 1);
        Frames(24); Shot("h257_rock_smash_cut_in");
        ReadOn();
        Frames(20); Shot("h258_desert_ruins_open");
        Flags("the wall", "FLAG_SYS_REGIROCK_PUZZLE_COMPLETED");
        At("DesertRuins", 8, 10, Direction.Up);
        game.ArriveOnMap(); Frames(20); Shot("h259_desert_ruins_chamber");
        shotName = "h25a_regirock";
        game.Interact(); Frames(3);
        ReadOn();
        FightOn();
        Flags("Regirock", "FLAG_DEFEATED_REGIROCK", "FLAG_HIDE_REGIROCK");

        // ---- The Ancient Tomb: Flash before the wall, and Registeel
        At("AncientTomb", 8, 21, Direction.Up);
        Frames(20); Shot("h25c_ancient_tomb_wall");
        game.UseFieldMove(FieldMove.Flash, 1);
        Frames(24); Shot("h25d_flash_cut_in");
        ReadOn();
        Frames(20); Shot("h25e_ancient_tomb_open");
        Flags("the wall", "FLAG_SYS_REGISTEEL_PUZZLE_COMPLETED");
        At("AncientTomb", 8, 10, Direction.Up);
        game.ArriveOnMap(); Frames(20); Shot("h25f_ancient_tomb_chamber");
        shotName = "h260_registeel";
        game.Interact(); Frames(3);
        ReadOn();
        FightOn();
        Flags("Registeel", "FLAG_DEFEATED_REGISTEEL", "FLAG_HIDE_REGISTEEL");

        // ---- Lilycove's harbour: the attendant gives the pass and sails the Champion to Southern Island's beach
        At("LilycoveCity_Harbor", 8, 11, Direction.Up);
        Frames(20); Shot("h262_ferry_desk");
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "the attendant's words"); Box().FinishLine(); Frames(2);
        Shot("h263_ferry_pass");
        ReadOn();
        Frames(60);
        game.LocationSign.Hide();
        Console.WriteLine($"  ferry: {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), the pass {game.Story.Has("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND")}");
        if (game.Map.Name != "SouthernIsland_Exterior") Console.WriteLine("  !! the ferry never reached Southern Island");
        Shot("h264_southern_island_beach");
        ShotCrop("h264_southern_island_beach_native", 640, 300, 640, 360, 2);

        // ---- The garden: the pond with the stone on its island, and the eon Pokémon coming down to it (the one not roaming)
        game.Story.SetVar("VAR_ROAMER_POKEMON", 0);
        At("SouthernIsland_Interior", 13, 17, Direction.Up);
        Frames(20); Shot("h265_southern_island_garden");
        At("SouthernIsland_Interior", 13, 12, Direction.Up);
        game.Player.SetMode(TravelMode.OnFoot);
        Frames(20); Shot("h266_southern_island_stone");
        shotName = "h267_latios";
        game.Interact(); Frames(3);
        Until(() => Box().IsActive || State() == GameState.Battle, "the stone's scene", 600);
        Shot("h267_lati_comes_down");
        ReadOn();
        FightOn();
        Flags("the eon Pokémon", "FLAG_ENCOUNTERED_LATIAS_OR_LATIOS", "FLAG_DEFEATED_LATIAS_OR_LATIOS", "FLAG_CAUGHT_LATIAS_OR_LATIOS");
        Frames(20); Shot("h268_southern_island_after");
        HoennFrontierIslandShots();
    }
}
