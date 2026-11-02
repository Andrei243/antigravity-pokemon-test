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
            if (game.Map.DeckAt(x, y) is { } deck) game.Player.SetHeight(deck);
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
        Look("h3e4_ss_tidal_corridor", "SSTidalCorridor", 4, 8, Direction.Up);
        Look("h3e5_ss_tidal_cabins", "SSTidalRooms", 6, 13, Direction.Up);
        Look("h3e6_ss_tidal_hold", "SSTidalLowerDeck", 15, 6, Direction.Up);

        // ---- The Seaside Cycling Road
        Look("h3f0_cycling_road", "Hoenn", 214, 214, Direction.Up);

        // ---- The Abandoned Ship, the Mechadolls
        Look("h3g0_route108_wreck", "Hoenn", 169, 369, Direction.Up, TravelMode.Surfing);
        Look("h3g1_ship_deck", "AbandonedShip_Deck", 13, 13, Direction.Up);
        Look("h3g2_mechadolls", "Route110_TrickHousePuzzle5", 4, 20, Direction.Up);

        // ---- The bridges, the salt, cave mouths, deep water and the triangle
        Look("h3h0_route120_kecleon_bridge", "Hoenn", 373, 17, Direction.Up);
        Look("h3h1_fortree_bridge", "Hoenn", 330, 12, Direction.Up);
        Look("h3h2_shoal_cave_salt", "ShoalCave_LowTideInnerRoom", 31, 22, Direction.Up);
        Look("h3i0_granite_cave_mouth", "Hoenn", 117, 330, Direction.Up);
        Look("h3i1_route124_deep_water", "Hoenn", 500, 140, Direction.Up, TravelMode.Surfing);
        Look("h3j0_birth_island_triangle", "BirthIsland_Exterior", 15, 17, Direction.Up);
    }
}
