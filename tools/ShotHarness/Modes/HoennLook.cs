using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    /// <summary>
    /// A look at Hoenn's last places and pictures (plan 21 Â· H12, part p), at the end of nothing: <c>HOENN_FROM=look</c> runs
    /// it alone in a new game. The Trainer Hill from Route 111, its lobby, a floor and its roof (<c>h3a0</c> to <c>h3a3</c>),
    /// the Frontier island's halls' doors (<c>h3b*</c>), Mauville's slot machine and the roulette's room (<c>h3c0</c>,
    /// <c>h3c1</c>), the two basements (<c>h3d0</c>, <c>h3d1</c>), Lilycove's PokÃ©mon Center with its three ladies and the
    /// Department Store's lift (<c>h3e0</c>, <c>h3e1</c>), Altering Cave (<c>h3e2</c>), Faraway Island's grass with Mew
    /// unseen (<c>h3e3</c>), the S.S. Tidal's rooms (<c>h3e4</c> to <c>h3e6</c>), the Seaside Cycling Road (<c>h3f0</c>),
    /// the Abandoned Ship and its deck (<c>h3g0</c>, <c>h3g1</c>), the Trick House's Mechadolls (<c>h3g2</c>), the
    /// Kecleon's and Fortree's bridges (<c>h3h0</c>, <c>h3h1</c>), Shoal Cave's salt (<c>h3h2</c>), cave mouths and deep
    /// water (<c>h3i*</c>) and Birth Island's triangle (<c>h3j0</c>).
    /// </summary>
    void HoennLookShots()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        var party = game.Party;
        party.Clear();
        var diver = new Pokemon(PokemonDatabase.Get("Swampert")!, 50);
        diver.Moves.Clear();
        foreach (string move in new[] { "Surf", "Dive", "Waterfall", "Strength" }) diver.Moves.Add(new Move(MoveDatabase.Get(move)!));
        party.Add(diver);
        party.Add(new Pokemon(PokemonDatabase.Get("Manectric")!, 50));
        game.Player.Moves = FieldMovement.MovesOf(party);
        game.Story.SetBadges("Hoenn", 0xFF);
        game.Story.Set("HoennHallOfFame");
        game.Story.Set("FLAG_SYS_POKENAV_GET");
        string only = Environment.GetEnvironmentVariable("LOOK_ONLY") ?? "";
        bool Wants(string name) => only.Length == 0 || only.Split(',').Any(name.Contains);

        void Look(string name, string map, int x, int y, Direction facing, TravelMode mode = TravelMode.OnFoot)
        {
            if (!Wants(name)) return;
            At(map, x, y, facing);
            if (mode != TravelMode.OnFoot) game.Player.SetMode(mode);
            game.ArriveOnMap();
            // On a bridge the player stands on its deck, as one who came onto it would
            if (mode != TravelMode.Surfing && game.Map.DeckAt(x, y) is { } deck) game.Player.SetHeight(deck);
            game.LocationSign.Hide();
            Frames(30);
            Console.WriteLine($"{name}: {game.Map.Name} ({game.Player.GridX}, {game.Player.GridY}), {game.Map.NPCs.Count} people, "
                + $"{game.Map.Props.Count} props, deck {game.Map.DeckAt(x, y)?.ToString() ?? "none"}");
            foreach (var near in game.Map.NPCs.Where(n => Math.Abs(n.GridX - x) <= 6 && Math.Abs(n.GridY - y) <= 6 && !n.IsThing))
                Console.WriteLine($"  {near.Key} {near.NpcType} at ({near.GridX}, {near.GridY}) level {near.Level?.ToString() ?? "ground"}, deck there {game.Map.DeckAt(near.GridX, near.GridY)?.ToString() ?? "none"}");
            Shot(name);
        }

        // ---- The Trainer Hill
        Look("h3a0_route111_trainer_hill", "Hoenn", 231, 117, Direction.Up);
        Look("h3a1_trainer_hill_lobby", "TrainerHillLobby", 5, 8, Direction.Up);
        if (Wants("h3a2"))
        {
            TrainerHill.Begin(game.Story, TrainerHillMode.Normal, 0);
            if (TrainerHillFloor.Make(game.Story) is { } floor)
            {
                game.Place(floor, TrainerHillFloor.Arrival.X, TrainerHillFloor.Arrival.Y + 4, Direction.Down);
                game.LocationSign.Hide();
                Frames(30);
                Shot("h3a2_trainer_hill_floor");
            }
            else Console.WriteLine("  !! no floor made");
        }
        Look("h3a3_trainer_hill_roof", "TrainerHillRoof", 5, 8, Direction.Up);

        // ---- The Frontier island's halls
        foreach (var (name, x, y) in new[]
                 {
                     ("h3b0_frontier_dome_door", 19, 20), ("h3b1_frontier_tower_door", 72, 17),
                     ("h3b2_frontier_pike_door", 42, 30), ("h3b5_frontier_factory_door", 11, 41), ("h3b6_frontier_arena_door", 95, 32), ("h3b7_frontier_palace_door", 101, 59), ("h3b8_frontier_pyramid_door", 114, 17), ("h3b9_frontier_ranking_hall", 91, 15), ("h3b4_frontier_south", 60, 50)
                 })
            Look(name, "HoennBattleFrontier", x, y, Direction.Up);

        // ---- Mauville's machine and the roulette's room
        if (Wants("h3c0"))
        {
            At("MauvilleCity_GameCorner", 11, 9, Direction.Up);
            game.Reels.Open(0, 500, ReelMachine.LuckOf(0, 1), new Random(7));
            game.State = GameState.ReelMachine;
            Frames(40);
            Shot("h3c0_mauville_reel_machine");
            game.State = GameState.Overworld;
        }
        Look("h3c1_mauville_game_corner", "MauvilleCity_GameCorner", 4, 9, Direction.Up);

        // ---- The two basements
        Look("h3d0_mossdeep_basement", "MossdeepCity_GameCorner_B1F", 5, 7, Direction.Up);
        Look("h3d1_sootopolis_basement", "SootopolisCity_MysteryEventsHouse_B1F", 5, 7, Direction.Up);

        // ---- Lilycove, Altering Cave, Faraway Island, the S.S. Tidal
        Look("h3e0_lilycove_center_ladies", "LilycoveCity_PokemonCenter_1F", 7, 7, Direction.Up);
        Look("h3e1_department_store_lift", "LilycoveCity_DepartmentStoreElevator", 2, 3, Direction.Up);
        game.Story.Set("FLAG_HOENN_ALTERING_CAVE_OPEN");
        Look("h3e2_altering_cave", "AlteringCave", 9, 16, Direction.Up);
        Look("h3e3_faraway_island_grass", "FarawayIsland_Interior", 13, 16, Direction.Up);
        Look("h3e4_ss_tidal_corridor", "SSTidalCorridor", 6, 11, Direction.Up);
        Look("h3e5_ss_tidal_cabins", "SSTidalRooms", 6, 13, Direction.Up);
        Look("h3e6_ss_tidal_hold", "SSTidalLowerDeck", 15, 6, Direction.Up);

        // ---- The Seaside Cycling Road
        Look("h3f0_cycling_road", "Hoenn", 214, 214, Direction.Up);

        // ---- The Abandoned Ship, the Mechadolls
        Look("h3g0_route108_wreck", "Hoenn", 169, 369, Direction.Up, TravelMode.Surfing);
        Look("h3g1_ship_deck", "AbandonedShip_Deck", 13, 13, Direction.Up);
        Look("h3g2_mechadolls", "Route110_TrickHousePuzzle5", 4, 20, Direction.Up);

        // ---- The bridges, the salt, cave mouths, deep water and the triangle
        // Plan 21 · part r: the player on Route 120's bridge beside the Kecleon nobody sees, whose plank is pressed down,
        // and the bridge once it is gone; Fortree's walkway with the player's own plank pressed down
        Look("h3h0_route120_kecleon_bridge", "Hoenn", 369, 16, Direction.Right);
        if (Wants("h3h0"))
        {
            game.Story.Set("FLAG_HIDE_ROUTE_120_KECLEON_BRIDGE");
            Look("h3h0b_route120_kecleon_gone", "Hoenn", 369, 16, Direction.Right);
            game.Story.Unset("FLAG_HIDE_ROUTE_120_KECLEON_BRIDGE");
        }
        Look("h3h1_fortree_bridge", "Hoenn", 331, 4, Direction.Right);
        // Shoal Cave's first pile of salt, whole and then with the day's salt taken
        Look("h3h2_shoal_cave_salt", "ShoalCave_LowTideInnerRoom", 31, 10, Direction.Up);
        if (Wants("h3h2"))
        {
            game.Story.Set("FLAG_DAILY_RECEIVED_SHOAL_SALT_1");
            Look("h3h2b_shoal_cave_salt_taken", "ShoalCave_LowTideInnerRoom", 31, 10, Direction.Up);
            game.Story.Unset("FLAG_DAILY_RECEIVED_SHOAL_SALT_1");
        }
        Look("h3i0_granite_cave_mouth", "Hoenn", 117, 330, Direction.Up);
        Look("h3i1_route124_deep_water", "Hoenn", 500, 140, Direction.Up, TravelMode.Surfing);
        // The abnormal weather's cave mouth on Route 114 and its deep water on Route 105, before and while it is there
        if (Wants("h3i2"))
        {
            Look("h3i2_route114_terra_cave_shut", "Hoenn", 46, 50, Direction.Up);
            game.Story.Set("FLAG_HOENN_ABNORMAL_WEATHER_ROUTE_114_SOUTH");
            Look("h3i2b_route114_terra_cave_mouth", "Hoenn", 46, 50, Direction.Up);
            game.Story.Unset("FLAG_HOENN_ABNORMAL_WEATHER_ROUTE_114_SOUTH");
        }
        if (Wants("h3i3"))
        {
            Look("h3i3_route105_sea", "Hoenn", 11, 295, Direction.Up, TravelMode.Surfing);
            game.Story.Set("FLAG_HOENN_ABNORMAL_WEATHER_ROUTE_105_NORTH");
            Look("h3i3b_route105_deep_water", "Hoenn", 11, 295, Direction.Up, TravelMode.Surfing);
            game.Story.Unset("FLAG_HOENN_ABNORMAL_WEATHER_ROUTE_105_NORTH");
        }
        // A surfer under the Seaside Cycling Road, where Emerald's sea runs under its deck
        Look("h3i4_cycling_road_surfer_under", "Hoenn", 210, 196, Direction.Right, TravelMode.Surfing);
        Look("h3j0_birth_island_triangle", "BirthIsland_Exterior", 15, 17, Direction.Up);

        // ---- Plan 21 · part q: what parts k, n and o built that nobody had seen. The Seaside Cycling Road ridden (its
        // deck under the wheels), Mauville's roulette wheel, the contest's stage behind the panels and the museum's first scene
        if (Wants("h3k0"))
        {
            Look("h3k0_cycling_road_ridden", "Hoenn", 217, 222, Direction.Up);
            game.Player.SetMode(TravelMode.Cycling);
            if (game.Map.DeckAt(217, 222) is { } deck) game.Player.SetHeight(deck);
            Frames(20);
            Shot("h3k0_cycling_road_ridden");
        }
        Look("h3k1_cycling_road_north", "Hoenn", 212, 200, Direction.Down);
        Look("h3k8_route119_small_bridge", "Hoenn", 334, 85, Direction.Up);
        Look("h3k9_route119_bridge_west", "Hoenn", 302, 83, Direction.Up);
        // ---- The Safari Zone's Pokéblock feeders (plan 21 · H11, part d): the south part's, two steps before it, and the
        // north-west part's by its pond
        Look("h3l0_safari_feeder_south", "HoennSafariZone", 64, 72, Direction.Up);
        Look("h3l1_safari_feeder_northwest", "HoennSafariZone", 33, 17, Direction.Up);
        if (Wants("h3l"))
            foreach (var (fx, fy) in new[] { (64, 70), (33, 15) })
            {
                var zone = PokemonPlatinumEngine.Data.MapDatabase.Get("HoennSafariZone");
                Console.WriteLine($"h3l*: feeder ({fx}, {fy}): behaviour {zone.BehaviourAt(fx, fy)}, walkable {zone.IsWalkable(fx, fy)}, script {zone.TileScripts.GetValueOrDefault((fx, fy))}");
            }
        if (Wants("h3k2"))
        {
            At("MauvilleCity_GameCorner", 4, 9, Direction.Up);
            party.Add(new Pokemon(PokemonDatabase.Get("Shroomish")!, 20));
            game.Roulette.Open(1, 500, 14, hasShroomish: true, hasTaillow: false, new Random(11));
            game.State = GameState.RouletteTable;
            Frames(40);
            Shot("h3k2_roulette_board");
            game.Roulette.Press(RouletteKeys.Right);
            game.Roulette.Frames(8);
            game.Roulette.Press(RouletteKeys.Confirm);
            game.Roulette.Frames(4);
            Frames(30);
            Shot("h3k3_roulette_wager");
            for (int i = 0; i < 12 && game.Roulette.IsActive; i++) { game.Roulette.Press(RouletteKeys.Confirm); game.Roulette.Frames(20); }
            Frames(60);
            Shot("h3k4_roulette_rolling");
            game.Roulette.Frames(240);
            Frames(10);
            Shot("h3k5_roulette_landed");
            Console.WriteLine($"h3k*: the wheel's phase {game.Roulette.Wheel?.Phase}, coins {game.Roulette.Wheel?.Coins}");
            game.State = GameState.Overworld;
            party.Remove(party.Members[^1]);
        }
        if (Wants("h3k6"))
        {
            var screen = game.AppealScreen;
            game.State = GameState.Appeals;
            foreach (var quality in new[] { ContestStat.Cool, ContestStat.Smart })
            {
                screen.Begin(party.Members[0], "Brendan", quality, PokemonPlatinumEngine.Models.Contest.AppealRank.Master, afterTheGame: true, new Random(5));
                foreach (var a in screen.Round!.Appealers) PokemonPlatinumEngine.Graphics.PokemonSprites.Request(a.Pokemon.ModelName);
                Frames(60);
                Shot($"h3k6_contest_stage_{quality.ToString().ToLowerInvariant()}");
            }
            game.State = GameState.Overworld;
        }
        if (Wants("h3k7"))
        {
            game.Story.SetVar("VAR_LILYCOVE_MUSEUM_2F_STATE", 0);
            At("LilycoveCity_LilycoveMuseum_2F", 11, 7, Direction.Up);
            game.ArriveOnMap();
            game.LocationSign.Hide();
            Frames(60);
            Shot("h3k7_museum_first_scene");
            Console.WriteLine($"h3k7: the script {game.ScriptNow ?? "none"}, the museum's state {game.Story.Var("VAR_LILYCOVE_MUSEUM_2F_STATE")}");
        }
    }
}
