using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- fossils, the Game Corner, the Villa, Pal Park (plan 06 · R16b)

    // The Oreburgh Mining Museum and a Fossil brought back to life; the clown's Coin Case; the Game Corner's hall, its
    // counter, a slot machine through a game, a win, the bonus waiting for its sevens, the jackpot and the bonus game
    // under each moon; the prize exchange; the Villa given, its first table, the catalogue, the room full, its
    // visitors; Pal Park's lobby, a Catching Show from the countdown to the score (pt*; not part of "all"). Everything
    // goes through the game's own TryInteract, scripts, steps and the screens' own buttons; it prints what each left behind.
    public void PastimesMode()
    {
        var only = args.Length > 2 ? args[2..] : Array.Empty<string>();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var team = game.Party;
        var story = game.Story;
        var pastimes = game.Pastimes;
        bool Wants(string part) => only.Length == 0 || only.Contains(part);
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        // Reads every line on to the end of the script, shooting the line that holds a word; a question is answered with
        // the answers given, in turn (the first when they run out)
        void ReadOn(string word = null, string shot = null, params int[] answers)
        {
            int asked = 0;
            for (int i = 0; i < 80 && (Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue || engine.ScriptRunning); i++)
            {
                if (game.State is not (GameState.Overworld or GameState.Dialogue)) break;
                if (engine.Choice.IsOpen)
                {
                    Frames(14);
                    int answer = asked < answers.Length ? answers[asked] : 0;
                    asked++;
                    for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                    Frames(2);
                    if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                    engine.Choice.Confirm(); Frames(4);
                    continue;
                }
                if (!Box().IsActive) { Frames(6); continue; }
                // A question's line is shot with its answers beside it, once they have slid in
                if (Box().IsQuestion) { Whole(); Frames(4); continue; }
                Whole();
                if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                Next();
            }
            Until(() => game.State != GameState.Dialogue && !Box().IsActive, "the text gone");
        }
        // Comes to a place as the game does: its furniture or its show laid out, its own script run
        void Enter(string map, int x, int y, Direction facing)
        {
            At(map, x, y, facing);
            game.ArriveOnMap();
            Frames(8);
        }
        // A script's warp: the fade over, and the place's own script begun if it has anything to say
        void Through(string map, bool speaks)
        {
            Until(() => game.Map.Name == map && game.State is GameState.Overworld or GameState.Dialogue, $"the way to {map}", 900);
            if (speaks) Until(() => Box().IsActive, "the place's own words", 300);
            else Frames(12);
        }
        void Talk(int x, int y, Direction facing, string word = null, string shot = null, params int[] answers)
        {
            game.Player.SetPosition(x, y, facing);
            Frames(2);
            game.Interact(); Frames(3);
            ReadOn(word, shot, answers);
        }

        team.Clear();
        team.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 50));
        inventory.AddItem(ItemDatabase.Get("Explorer Kit")!);
        story.Set("FLAG_RECEIVED_EXPLORER_KIT");

        // ---- The Oreburgh Mining Museum: its hall, its cases, and a Skull Fossil brought back to life
        if (Wants("museum"))
        {
            At("Sinnoh", 305, 748, Direction.Up);
            game.LocationSign.Hide();
            Frames(4);
            Shot("pt01_museum_door");
            At("OreburghMiningMuseum", 5, 13, Direction.Up);
            Frames(6);
            Shot("pt02_museum");
            At("OreburghMiningMuseum", 12, 8, Direction.Right);
            Frames(6);
            Shot("pt03_museum_cases");
            At("OreburghMiningMuseum", 17, 15, Direction.Up);
            Frames(6);
            Shot("pt04_museum_east_wing");
            Talk(19, 8, Direction.Up, "lamps", "pt05_museum_exhibit");
            inventory.AddItem(ItemDatabase.Get("Skull Fossil")!);
            At("OreburghMiningMuseum", 6, 5, Direction.Up);
            Frames(4);
            game.Interact(); Frames(3);
            ReadOn("back to life?", "pt06_fossil_question");
            Console.WriteLine($"  the Fossil handed over: {Fossils.Count(inventory)} left, species {story.Var(Fossils.RevivedVariable)} waiting, the machine at work: {story.Has("FLAG_MAP_LOCAL_MUSEUM_REVIVING")}");
            Talk(6, 5, Direction.Up, "unwatched", "pt07_fossil_working");
            // Out of the door and in again
            At("Sinnoh", 305, 748, Direction.Up);
            game.ArriveOnMap();
            Frames(4);
            At("OreburghMiningMuseum", 6, 5, Direction.Up);
            game.ArriveOnMap();
            Frames(4);
            game.Interact(); Frames(3);
            ReadOn("turned out to be", "pt08_fossil_revived");
            Console.WriteLine($"  the team: {string.Join(", ", team.Members.Select(p => $"{p.Species.Name} Lv. {p.Level}"))}; revived so far: {pastimes.FossilsRevived}");
        }

        // ---- Veilstone City: the clown's Coin Case, the Game Corner, a machine, the prize exchange
        if (Wants("corner"))
        {
            At("VeilstoneSoutheastHouse", 4, 8, Direction.Up);
            Frames(6);
            Shot("pt10_clown_house");
            for (int tries = 0; tries < 12 && inventory.GetQuantity(ItemDatabase.Get("Coin Case")!) == 0; tries++)
                Talk(6, 7, Direction.Up, tries == 0 ? "Which hand" : "Coin Case", tries == 0 ? "pt11_clown_hands" : "pt12_coin_case", tries % 2);
            Console.WriteLine($"  the Coin Case: {inventory.GetQuantity(ItemDatabase.Get("Coin Case")!)}");

            At("Sinnoh", 706, 624, Direction.Up);
            game.LocationSign.Hide();
            Frames(4);
            Shot("pt20_game_corner_door");
            At("VeilstoneGameCorner", 5, 11, Direction.Up);
            Frames(6);
            Shot("pt21_game_corner");
            At("VeilstoneGameCorner", 10, 8, Direction.Up);
            Frames(6);
            Shot("pt22_game_corner_machines");
            game.Money = 20000;
            Talk(2, 6, Direction.Up, "How many", "pt23_coin_counter", 1);
            Console.WriteLine($"  bought at the counter: {pastimes.Coins.Coins} Coins, ${game.Money} left");
            Talk(4, 7, Direction.Down);
            Talk(5, 6, Direction.Right);
            Console.WriteLine($"  with the old man's 20 and the farmer's 50: {pastimes.Coins.Coins} Coins");

            // A machine: the fourth of the original's numbers, its mood the day's deal
            game.Player.SetPosition(6, 8, Direction.Right);
            Frames(2);
            game.Interact();
            Until(() => game.State == GameState.SlotMachine, "the machine on the screen");
            var slots = game.Slots;
            var m = slots.Machine!;
            Console.WriteLine($"  machine {slots.Number} of mood {m.Mood}, {m.Coins} Coins");
            void Play(int frames) => Frames(Math.Max(1, frames * 2));   // the harness's frames are sixtieths, the machine's thirtieths
            void Press(SlotKeys keys) { slots.Press(keys); Play(1); }
            void StopReels(int[] order, string afterFirst = null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Press(order[i] == 0 ? SlotKeys.Stop1 : order[i] == 1 ? SlotKeys.Stop2 : SlotKeys.Stop3);
                    Play(i == 0 && afterFirst != null ? 14 : 6);
                    if (i == 0 && afterFirst != null) Shot(afterFirst);
                }
                Until(() => m.Stopped(0) && m.Stopped(1) && m.Stopped(2), "the reels still", 400);
            }
            Play(20);
            Shot("pt30_slots_bet");
            Press(SlotKeys.Bet);
            Play(2);
            m.Rig(SlotMachine.Draw.Ball);
            Play(14);
            Shot("pt31_slots_turning");
            StopReels(SlotMachine.Orders[0], "pt32_slots_one_stopped");
            Play(10);
            Shot("pt33_slots_win");
            Console.WriteLine($"  a Ball drawn and stopped left to right: {m.Lined} on lines {m.Lines}, {m.Payout} to count");
            slots.Held = true;
            Until(() => m.WaitsForBet, "the next game", 1200);
            slots.Held = false;
            Console.WriteLine($"  after the win: {m.Coins} Coins");

            // The bonus is won and waits for three sevens
            bool lined = false;
            for (int tries = 0; tries < 40 && !lined; tries++)
            {
                m.RigBonus(2);
                m.Rig(SlotMachine.Draw.RedSevens);
                Play(11 + tries);
                if (tries == 0) Shot("pt34_slots_sevens_wanted");
                StopReels(SlotMachine.Orders[0]);
                Play(4);
                lined = m.Lined == SlotSymbol.RedSeven;
                if (!lined) Until(() => m.WaitsForBet, "the next try", 600);
            }
            Play(12);
            Shot("pt35_slots_jackpot");
            Console.WriteLine($"  three sevens: {m.Lined} on lines {m.Lines}, {m.Payout} to count");
            slots.Held = true;
            Until(() => m.InBonus && m.WaitsForBet, "the bonus game", 3000);
            slots.Held = false;
            Console.WriteLine($"  the bonus game: round {m.Rounds}, {m.GamesLeft} games, {m.Coins} Coins, stamina {m.StaminaLeft}");
            Play(10);
            Shot("pt36_bonus_round");
            // A game as Clefairy points
            Press(SlotKeys.Bet);
            m.Rig(SlotMachine.Draw.Replay, bonusGame: true, pointed: 3);
            Play(14);
            Shot("pt37_bonus_pointing");
            StopReels(SlotMachine.Orders[3], "pt38_bonus_second_reel");
            Play(10);
            Shot("pt39_bonus_win");
            Console.WriteLine($"  stopped as pointed: {m.Lined}, {m.Payout} to count");
            slots.Held = true;
            Until(() => m.WaitsForBet, "the next bonus game", 1200);
            slots.Held = false;
            // Under a red moon, and stopped out of order
            Press(SlotKeys.Bet);
            m.Rig(SlotMachine.Draw.Replay, bonusGame: true, pointed: 1, moon: SlotMoon.Red);
            Play(14);
            Shot("pt3a_bonus_red_moon");
            StopReels(SlotMachine.Orders[4]);
            Play(8);
            Console.WriteLine($"  stopped out of order under a red moon: {m.Lined}");
            Until(() => m.WaitsForBet, "the next bonus game", 1200);
            Press(SlotKeys.Bet);
            m.Rig(SlotMachine.Draw.Replay, bonusGame: true, pointed: 5, moon: SlotMoon.Bright);
            Play(14);
            Shot("pt3b_bonus_bright_moon");
            StopReels(SlotMachine.Orders[5]);
            slots.Held = true;
            Until(() => m.WaitsForBet, "the next bonus game", 1200);
            slots.Held = false;
            Console.WriteLine($"  the bonus so far: {m.BonusCoins} Coins won, {m.GamesLeft} games left of round {m.Rounds}");
            Press(SlotKeys.Quit);
            Until(() => game.State == GameState.Overworld, "the hall again", 600);
            Frames(40);
            Console.WriteLine($"  back in the hall with {pastimes.Coins.Coins} Coins; the best run of bonus rounds: {story.Var(GameCornerPrizes.StreakVariable)}");

            // The prize exchange
            pastimes.Coins.Set(Math.Max(pastimes.Coins.Coins, 6500));
            At("VeilstonePrizeExchange", 4, 8, Direction.Up);
            Frames(6);
            Shot("pt40_prize_exchange");
            Talk(4, 6, Direction.Up, "Which prize", "pt41_prize_list", 9, 0);
            Console.WriteLine($"  exchanged: TM89 ×{inventory.GetQuantity(ItemDatabase.Get("TM89")!)}, {pastimes.Coins.Coins} Coins left");
        }

        // ---- The Villa: given, its first table, the catalogue, the room full, its visitors
        if (Wants("villa"))
        {
            At("Sinnoh", 814, 473, Direction.Right);
            game.LocationSign.Hide();
            Frames(6);
            Shot("pt50_resort_boy");
            game.Money = 999999;
            engine.Steering = (Direction.Right, false);
            Until(() => engine.ScriptRunning || Box().IsActive, "the boy's offer", 200);
            engine.Steering = null;
            ReadOn("Will you have it", "pt51_villa_offered", 0, 0);
            Through(GameEngine.VillaMap, speaks: true);
            ReadOn("sold the furniture", "pt52_villa_empty", 0);
            Frames(20);
            Shot("pt55_villa_first_table");
            Console.WriteLine($"  the Villa: state {story.Var(VillaRules.StateVariable)}, {VillaRules.Owned(story)} piece(s), the boy gone: {story.Has("FLAG_HIDE_VILLA_SCHOOL_KID_M")}, ${game.Money}");

            // The catalogue on the table: a Small Sofa, which brings the assistant
            Talk(10, 7, Direction.Right, "Which piece", "pt56_villa_catalogue", 2, 0);
            Frames(20);
            Shot("pt57_villa_sofa_and_caller");
            Console.WriteLine($"  after the catalogue: {VillaRules.Owned(story)} pieces, ${game.Money}, the day's visitor {story.Var(VillaRules.VisitorVariable)}");

            // Everything the catalogue has, in its place
            foreach (var piece in VillaRules.Furniture) story.Set(piece.Flag);
            story.SetVar(VillaRules.VisitorVariable, 12);
            story.SetVar(VillaRules.MessageVariable, 2);
            story.Unset(VillaRules.OutsideFlag);
            Enter(GameEngine.VillaMap, 11, 11, Direction.Up);
            Frames(12);
            Shot("pt58_villa_furnished");
            game.Player.SetPosition(9, 7, Direction.Up);
            Frames(12);
            Shot("pt59_villa_west");
            game.Player.SetPosition(16, 8, Direction.Up);
            Frames(12);
            Shot("pt5a_villa_east");
            Console.WriteLine($"  the room: {game.Map.Props.Count} things standing, the caller expected: {story.Var("VAR_MAP_LOCAL_VILLA_CALLER")}");
            // Cynthia walks in once the player has stepped up to the table
            game.Player.SetPosition(10, 8, Direction.Up);
            Frames(8);
            engine.Steering = (Direction.Up, false);
            Until(() => engine.ScriptRunning, "Cynthia at the door", 200);
            engine.Steering = null;
            Until(() => !engine.ScriptRunning, "Cynthia by the piano", 900);
            Frames(10);
            Shot("pt5b_villa_cynthia");
            Talk(4, 5, Direction.Left, "old story", "pt5c_cynthia_says");
            Talk(3, 4, Direction.Left, "piano", "pt5d_villa_piano");
            // Flint and Volkner by the television
            story.Unset(VillaRules.OutsideFlag);
            story.SetVar(VillaRules.VisitorVariable, 13);
            Enter(GameEngine.VillaMap, 13, 5, Direction.Up);
            Frames(12);
            game.Interact(); Frames(3);
            ReadOn("Flint talks", "pt5e_flint_and_volkner");

            // The professor waits at the door on another day
            story.Unset(VillaRules.InsideFlag);
            story.Unset(VillaRules.OutsideFlag);
            story.SetVar(VillaRules.VisitorVariable, 2);
            Enter("Sinnoh", 822, 470, Direction.Down);
            game.LocationSign.Hide();
            Frames(12);
            Shot("pt5f_rowan_at_the_door");
            Talk(821, 472, Direction.Down, "rest his feet", "pt5g_rowan_asks", 0);
            Through(GameEngine.VillaMap, speaks: false);
            Frames(12);
            Shot("pt5h_rowan_inside");
        }

        // ---- Pal Park: the lobby, a Catching Show from the countdown to the score
        if (Wants("park"))
        {
            At("Sinnoh", 306, 911, Direction.Up);
            game.LocationSign.Hide();
            Frames(6);
            Shot("pt60_pal_park_door");
            Enter("PalParkLobby", 7, 19, Direction.Up);
            Shot("pt61_pal_park_lobby");
            Talk(7, 12, Direction.Up, "no Pokémon of yours", "pt62_nothing_brought", 0);

            // Six brought over, as this game's migration will bring them
            var six = new[] { "Bulbasaur", "Pikachu", "Geodude", "Magikarp", "Oddish", "Zubat" }.Select(name => Meet(name, 30)).ToList();
            pastimes.PalPark.Stock(six);
            Console.WriteLine($"  six wait in the park: {string.Join(", ", six.Select(p => $"{p.Species.Name} (corner {CatchingShow.AreaOf(p.Species)})"))}");
            game.Player.SetPosition(7, 12, Direction.Up);
            Frames(2);
            game.Interact(); Frames(3);
            ReadOn("Park Balls", "pt63_park_balls", 0);
            Through(GameEngine.PalParkMap, speaks: true);
            ReadOn("about to begin", "pt64_countdown");
            Frames(10);
            Shot("pt65_park_gate");
            var show = pastimes.PalPark.Show!;
            Console.WriteLine($"  the show: {show.ParkBalls} Park Balls, the clock running: {pastimes.PalPark.Running}");

            // Walk the grass and the water of each corner until its Pokémon come out, and throw a Park Ball at each
            var park = game.Map;
            (int X, int Y)? Tile(int area)
            {
                for (int y = 0; y < park.Height; y++)
                    for (int x = 0; x < park.Width; x++)
                        if (CatchingShow.AreaAt(x, y, park.IsTallGrass(x, y), park.IsDeepWater(x, y)) == area && (area > 4 || park.IsWalkable(x, y))) return (x, y);
                return null;
            }
            int caught = 0;
            for (int guard = 0; guard < 6 && show.Caught < 6; guard++)
            {
                var next = Enumerable.Range(0, 6).First(i => !show.IsCaught(i));
                int area = CatchingShow.AreaOf(show.Pokemon[next].Species);
                if (Tile(area) is not { } at) { Console.WriteLine($"  !! no tile of corner {area}"); break; }
                game.Player.SetPosition(at.X, at.Y, Direction.Down);
                game.Player.SetMode(area > 4 ? TravelMode.Surfing : TravelMode.OnFoot);
                Frames(2);
                if (caught == 0) { Frames(20); Shot("pt66_park_grass"); }
                for (int step = 0; step < 4000 && game.State == GameState.Overworld && !game.ParkStep(); step++) { }
                Until(() => game.State == GameState.Battle && game.Battle != null, "a battle in the park", 600);
                if (game.Battle is not { } fight) break;
                ToMainMenu(fight);
                if (caught == 0) Shot("pt67_park_battle");
                fight.SelectMainMenuOption(0);
                for (int i = 0; i < 80 && !fight.IsBattleOver; i++)
                {
                    Skip(0.5);
                    if (caught == 0 && i == 3) Shot("pt68_park_ball");
                    Confirm(fight);
                }
                Until(() => game.State is GameState.Overworld or GameState.Dialogue, "the park again", 1500);
                caught++;
                Console.WriteLine($"  caught {show.Pokemon[show.Current].Species.Name} in corner {area}: {show.Caught} of 6, {(int)pastimes.PalPark.Seconds} s");
                if (caught == 1) { Frames(20); Shot("pt69_park_count"); }
            }
            // The sixth catch ends the show
            Until(() => Box().IsActive, "the show's end", 600);
            ReadOn("Ding-dong", "pt6a_all_caught");
            Through(GameEngine.PalParkLobbyMap, speaks: true);
            ReadOn("Total", "pt6b_score", 0);
            Frames(20);
            Shot("pt6c_lobby_after");
            Console.WriteLine($"  the score: {show.CatchingPoints} catching + {show.TimePoints} time + {show.TypePoints} type = {show.Score}; the record {story.Var(PalParkState.RecordVariable)}");
            Console.WriteLine($"  the boxes: {string.Join(", ", game.Boxes.All.Select(p => p.Species.Name))}; six still waiting: {pastimes.PalPark.Stocked}; the bag's berries: {string.Join(", ", inventory.AllItems.Where(s => s.Data.Berry != null).Select(s => s.Name))}");
        }
    }
}
