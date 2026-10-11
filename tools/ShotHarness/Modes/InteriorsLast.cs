using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---- part d's rooms at work (`in_w*`; plan 01 · M11, part d: the gate houses, the tutors, the Department Store,
    // the Pokémon Centers' other floors and the Global Terminal), through the game's own TryInteract and question
    // boxes: the Old Rod in Jubilife's gate, the Pokédex's upgrade in Canalave's, the television crew in the gate to
    // Route 212, a keeper of the Cycling Road's gate over his counter, the Battle Frontier's attendant and her topics,
    // a shard tutor's list and the move taught, Grandma Wilma's Draco Meteor, the Move Deleter's question, the
    // store's counter, its vending machine, its Poffin seller and its lift ridden to the roof, an attendant upstairs
    // in a Pokémon Center and the Global Terminal's receptionist. It prints what each left behind.
    void InteriorsAtWorkD()
    {
        var team = game.Party;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // Reads every line on to the end of the script, shooting the first line or question that holds each word
        // given; each question takes the next of the answers (the first once they run out), and the Pokémon at
        // `choose` is picked when the team is asked for
        void ReadOn((string Word, string Shot)[] shots, int[]? answers = null, int choose = 0)
        {
            var left = shots.ToList();
            int answered = 0;
            void Look()
            {
                var hit = left.FirstOrDefault(s => s.Word.Split('|').Any(word => Box().VisibleText.Contains(word, StringComparison.Ordinal)));
                if (hit.Shot == null) return;
                Shot(hit.Shot);
                left.Remove(hit);
            }
            for (int i = 0; i < 6000; i++)
            {
                bool busy = engine.ScriptRunning || Box().IsActive || engine.Choice.IsOpen || game.State is GameState.Dialogue or GameState.PartyMenu;
                if (!busy && i > 4) break;
                if (game.State == GameState.PartyMenu)
                {
                    Frames(10);
                    game.PartyScreen.SelectedIndex = choose;
                    game.PartyScreen.Confirm(team); Frames(3);
                    continue;
                }
                if (engine.Choice.IsOpen)
                {
                    Frames(24);
                    Look();
                    int answer = answers != null && answered < answers.Length ? answers[answered] : 0;
                    answered++;
                    for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                    engine.Choice.Confirm(); Frames(4);
                    continue;
                }
                if (!Box().IsActive) { Frames(1); continue; }
                Box().FinishLine(); Frames(2);
                if (engine.Choice.IsOpen) continue;
                Look();
                Box().Advance(); Frames(2);
            }
            foreach (var (word, shot) in left) Console.WriteLine($"  !! never said \"{word}\" for {shot}");
            Until(() => game.State == GameState.Overworld && !Box().IsActive, "the text gone");
        }
        void Talk(string map, int x, int y, Direction facing) { At(map, x, y, facing); game.Interact(); Frames(3); }
        int Have(string item) => inventory.GetQuantity(ItemDatabase.Get(item)!);
        Pokemon With(string species, int level, params string[] moves)
        {
            var p = new Pokemon(PokemonDatabase.Get(species)!, level);
            p.Moves.Clear();
            foreach (string move in moves) p.Moves.Add(new Move(MoveDatabase.Get(move)!));
            return p;
        }
        ReadOn(Array.Empty<(string, string)>());
        team.Clear();
        var bulbasaur = With("Bulbasaur", 30, "Tackle", "Vine Whip");
        var gible = With("Gible", 30, "Tackle");
        gible.Friendship = 255;
        team.Add(bulbasaur);
        team.Add(gible);
        game.Money = Math.Max(game.Money, 30000);
        foreach (string shard in new[] { "Red Shard", "Blue Shard", "Yellow Shard", "Green Shard" }) inventory.AddItem(ItemDatabase.Get(shard)!, 20);

        // The gates
        Talk("Route218GateToJubilifeCity", 4, 9, Direction.Up);
        ReadOn(new[] { ("Old Rod! You", "in_w01_old_rod_question"), ("Old Rod", "in_w02_old_rod_given") });
        Console.WriteLine($"  Jubilife's gate: {Have("Old Rod")} Old Rod, its flag {game.Story.Has("FLAG_RECEIVED_OLD_ROD")}");
        Talk("Route218GateToCanalaveCity", 6, 7, Direction.Left);
        ReadOn(new[] { ("father", "in_w03_assistants_father"), ("upgraded", "in_w04_pokedex_upgraded") });
        Console.WriteLine($"  Canalave's gate: the upgrade's flag {game.Story.Has("FLAG_ENABLED_POKEDEX_FORM_DETECTION")}, the gate's state {game.Story.Var("VAR_ROUTE_218_GATE_TO_CANALAVE_CITY_STATE")}, " +
            $"he is {(MapDatabase.Get("Route218GateToCanalaveCity").NPCs.Any(n => n.Key == "scientist_m") ? "still there" : "gone")}");
        At("Route212GateToHearthomeCity", 5, 9, Direction.Down); Frames(4); Shot("in_w05_television_crew");
        Talk("Route206CyclingRoadNorthGate", 6, 7, Direction.Left);
        ReadOn(new[] { ("gears", "in_w06_gate_keeper") });
        Talk("BattleFrontierGateToFightArea", 7, 17, Direction.Left);
        ReadOn(new[] { ("What would you like", "in_w07_frontier_topics"), ("Battle Points", "in_w08_frontier_points") }, new[] { 2 });

        // The tutors
        Talk("Route212House", 5, 7, Direction.Up);
        ReadOn(new[] { ("Which move should", "in_w10_tutor_moves"), ("learned", "in_w11_tutor_taught") }, new[] { 0, 0 });
        Console.WriteLine($"  Route 212's tutor: {bulbasaur.DisplayName} knows {string.Join(", ", bulbasaur.Moves.Select(m => m.Name))}; {Have("Blue Shard")} Blue Shards left of 20");
        Talk("Route210GrandmaWilmaHouse", 5, 6, Direction.Up);
        ReadOn(new[] { ("Dragon tamers", "in_w12_wilma"), ("learned", "in_w13_draco_meteor") }, new[] { 0 }, choose: 1);
        Console.WriteLine($"  Grandma Wilma: {gible.DisplayName} knows {string.Join(", ", gible.Moves.Select(m => m.Name))}");
        Talk("CanalaveCityEastHouse", 2, 7, Direction.Up);
        ReadOn(new[] { ("Which move should be forgotten", "in_w14_deleter_moves"), ("forgotten", "in_w15_deleter_done") }, new[] { 0, 1, 0 }, choose: 1);
        Console.WriteLine($"  The Move Deleter: {gible.DisplayName} knows {string.Join(", ", gible.Moves.Select(m => m.Name))}");

        // The Department Store
        Talk("VeilstoneStore1F", 2, 7, Direction.Up);
        Until(() => Box().IsActive || game.State == GameState.Shop, "the clerk's greeting", 240);
        for (int i = 0; i < 6 && game.State != GameState.Shop; i++) { Box().FinishLine(); Frames(2); Box().Advance(); Frames(20); }
        Frames(30); Shot("in_w20_store_counter");
        Console.WriteLine($"  The store's ground floor: the state is {game.State}");
        if (game.State == GameState.Shop) { game.Shop.Close(); Frames(6); }
        ReadOn(Array.Empty<(string, string)>());
        Talk("VeilstoneStore5F", 5, 4, Direction.Up);
        ReadOn(new[] { ("vending machine", "in_w21_vending_machine"), ("Clang", "in_w22_vending_can") }, new[] { 2 });
        Console.WriteLine($"  The roof: {Have("Lemonade")} Lemonade");
        inventory.AddItem(ItemDatabase.Get("Poffin Case")!, 1);
        Talk("VeilstoneStoreB1F", 15, 13, Direction.Up);
        ReadOn(new[] { ("Which Poffin", "in_w23_poffin_kinds"), ("Thank you", "in_w24_poffin_sold") }, new[] { 1 });
        Console.WriteLine($"  The basement: {game.Poffins.Count} Poffin in the case");
        At("VeilstoneStore1F", 15, 3, Direction.Up); game.ArriveOnMap(); Frames(4); Shot("in_w25_lift_doors");
        Talk("VeilstoneStoreElevator", 2, 6, Direction.Left);
        ReadOn(new[] { ("To which floor", "in_w26_lift_floors"), ("Going up", "in_w27_lift_going_up") }, new[] { 0 });
        Until(() => game.Map.Name == "VeilstoneStore5F" && game.State == GameState.Overworld, "the roof", 600);
        Frames(20); Shot("in_w28_lift_arrived");
        Console.WriteLine($"  The lift: on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}, the car at floor {game.Story.Var("VAR_VEILSTONE_STORE_LIFT_FLOOR")}");

        // A Pokémon Center's floor above and the Global Terminal
        Talk("JubilifePokemonCenter2F", 7, 6, Direction.Up);
        ReadOn(new[] { ("Union Room", "in_w30_union_room_desk") });
        Talk("GlobalTerminal1F", 7, 5, Direction.Up);
        ReadOn(new[] { ("Global Trade Station", "in_w31_gts_desk") });
    }
}
