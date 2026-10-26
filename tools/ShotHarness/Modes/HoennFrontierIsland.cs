partial class Harness
{
    /// <summary>
    /// The Battle Frontier island of Hoenn's post-game (plan 21 · H12, part b), after part a's shots at the end of the
    /// <c>hoenn</c> mode (<c>HOENN_FROM=postgame</c>): Lilycove's ferry desk offering the island with the S.S. Ticket and
    /// the quay it sails to (<c>h270</c>, <c>h271</c>), the reception gate and Scott's pass (<c>h272</c>, <c>h273</c>),
    /// the halls' plazas of both halves (<c>h274</c> to <c>h277</c>), the Pokémon Center, the Mart, the Exchange Service
    /// Corner, the Ranking Hall and a lounge (<c>h278</c> to <c>h27c</c>), Scott in his house with his Battle Points
    /// (<c>h27d</c>), and the strange tree watered and battled (<c>h27e</c>, <c>h27f</c>). It prints where the ferry put
    /// the player and the flags each scene left. Written, not run, by the session that built it.
    /// </summary>
    void HoennFrontierIslandShots()
    {
        if (!HoennReady("the Battle Frontier island")) return;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // Reads on through what is said; a question takes the answer given (0 the first), then the first
        void ReadOn(int answer = 0)
        {
            for (int i = 0; i < 1800 && (engine.ScriptRunning || game.State != GameState.Overworld); i++)
            {
                if (game.State == GameState.Battle) return;
                if (engine.Choice.IsOpen)
                {
                    Frames(10);
                    for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                    answer = 0;
                    engine.Choice.Confirm(); Frames(4);
                }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(6);
        }
        void Flags(string label, params string[] flags) =>
            Console.WriteLine($"  {label}: " + string.Join(", ", flags.Select(f => $"{f[5..]} {(game.Story.Has(f) ? "set" : "clear")}")));

        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        game.Story.Set("HoennHallOfFame");
        game.Story.Set("FLAG_RECEIVED_SS_TICKET");
        game.Story.Set("FLAG_ENABLE_SHIP_SOUTHERN_ISLAND");

        // ---- Lilycove's ferry desk: the island is the second choice, and the quay it sails to
        At("LilycoveCity_Harbor", 8, 11, Direction.Up);
        game.Interact(); Frames(3);
        Until(() => engine.Choice.IsOpen, "the ferry's choice");
        Frames(12); Shot("h270_ferry_choice");
        ReadOn(1);
        Frames(60);
        game.LocationSign.Hide();
        Console.WriteLine($"  ferry: {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY})");
        if (game.Map.Name != "HoennBattleFrontier") Console.WriteLine("  !! the ferry never reached the island");
        Shot("h271_frontier_quay");

        // ---- The reception gate and Scott's pass
        At("BattleFrontier_ReceptionGate", 4, 12, Direction.Up);
        Frames(20); Shot("h272_reception_gate");
        At("BattleFrontier_ReceptionGate", 4, 6, Direction.Up);
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "Scott's words"); Box().FinishLine(); Frames(2);
        Shot("h273_scott_pass");
        ReadOn();
        Flags("the pass", "FLAG_RECEIVED_FRONTIER_PASS");

        // ---- The grounds: the plazas before the halls of both halves (their doors shut)
        At("HoennBattleFrontier", 26, 58, Direction.Up); Frames(20); Shot("h274_west_plaza");
        At("HoennBattleFrontier", 19, 20, Direction.Up); Frames(20); Shot("h275_battle_dome");
        At("HoennBattleFrontier", 72, 17, Direction.Up); Frames(20); Shot("h276_battle_tower");
        At("HoennBattleFrontier", 95, 33, Direction.Up); Frames(20); Shot("h277_east_plaza");

        // ---- The rooms of the grounds
        At("BattleFrontier_PokemonCenter_1F", 7, 7, Direction.Up); Frames(20); Shot("h278_pokemon_center");
        At("BattleFrontier_Mart", 3, 6, Direction.Up); Frames(20); Shot("h279_mart");
        At("BattleFrontier_ExchangeServiceCorner", 7, 9, Direction.Up); Frames(20); Shot("h27a_exchange_corner");
        At("BattleFrontier_RankingHall", 26, 13, Direction.Up); Frames(20); Shot("h27b_ranking_hall");
        At("BattleFrontier_Lounge1", 4, 8, Direction.Up); Frames(20); Shot("h27c_lounge");

        // ---- Scott's house: his Battle Points, and his crossing refused
        At("BattleFrontier_ScottsHouse", 2, 4, Direction.Up);
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "Scott's welcome"); Box().FinishLine(); Frames(2);
        Shot("h27d_scotts_house");
        ReadOn(1);
        Flags("Scott", "FLAG_RECEIVED_SCOTTS_BATTLE_POINTS");

        // ---- The strange tree by the water, watered with the Wailmer Pail
        if (PokemonPlatinumEngine.Data.ItemDatabase.Get("Wailmer Pail") is { } pail) game.Bag.AddItem(pail, 1);
        At("HoennBattleFrontier", 110, 63, Direction.Up);
        Frames(20); Shot("h27e_strange_tree");
        game.Interact(); Frames(3);
        ReadOn();
        Until(() => game.State == GameState.Battle, "the Sudowoodo's battle", 600);
        if (game.State == GameState.Battle) { ToMainMenu(game.Battle); Shot("h27f_sudowoodo_battle"); }
        Flags("the tree", "FLAG_HIDE_BATTLE_FRONTIER_SUDOWOODO");
        HoennRayquazaAndStevenShots();
        HoennNewMauvilleShots();
        HoennWeatherCavesShots();
        HoennEventIslandsShots();
    }
}
