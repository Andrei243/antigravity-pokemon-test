using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

partial class Harness
{
    // ---------------------------------------------------------------- the rooms of plan 01 · M11

    // Every room of plan 01 · M11 rebuilt to the original's plan, a town at a time (not part of "all"): a shot of each
    // from its way in (`in_<town>_<nn>_<room>`; a wide room from two or three places) and a sheet of the town's rooms
    // (`interiors_<town>`); then a few of them at work through the game's own TryInteract and question boxes
    // (`in_x*`): a gift given once, the flower shop's swap of berries for an accessory, the Berry Master's berry of the
    // day, a rest on Route 205, a floor reached by its stairs and by a lift, the Old Chateau's ghost at its table. It
    // prints what each left behind. Name towns after the mode to do only those: twinleaf, sandgem, jubilife, oreburgh,
    // floaroma, eterna, chateau, hearthome, solaceon, and part b's: veilstone, lakefront, pastoria, mansion, canalave.
    // `work` shows part a's rooms at work (`in_x*`) and `workb` part b's (`in_y*`): the café's milk, a massage, a
    // table of the Seven Stars Restaurant and its double battle, the Suite Key and the White Flute, the Safari Game
    // from the observatory's counter into the marsh and back out, the Matchup Checker, the binoculars, the Pokémon
    // Mansion's corridor and a door of it, the maids' challenge and a prodigy, Mr. Backlot and his statue.
    // Part c's towns (the north and the Battle Zone): celestic, snow, sunyshore, battlezone, syndicate; `workc` shows
    // part c's rooms at work (`in_z*`, Modes/InteriorsNorth.cs).
    // Part d's towns: gates, tutors, store, centers, terminal; `workd` shows part d's rooms at work (`in_w*`,
    // Modes/InteriorsLast.cs).
    // The last rooms' towns (the hand-made rooms of the first chapters, rebuilt): pokecenters, marts.
    public void InteriorsMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        (string Town, string[] Rooms)[] towns =
        {
            ("twinleaf", new[] { "TwinleafTownNortheastHouse", "TwinleafTownSouthwestHouse" }),
            ("sandgem", new[] { "SandgemTownCounterpartHouse1F", "SandgemTownCounterpartHouse2F", "SandgemTownHouse" }),
            ("jubilife", new[] { "JubilifeCityCondominiums1F", "JubilifeCityCondominiums2F", "JubilifeCitySouthHouse1F", "JubilifeCitySouthHouse2F" }),
            ("oreburgh", new[]
            {
                "OreburghCityNorthwestHouse1F", "OreburghCityNorthwestHouse2F", "OreburghNorthHouse1F", "OreburghNorthHouse2F", "OreburghCityWestHouse",
                "OreburghCityMiddleHouse", "OreburghCityEastHouse1F", "OreburghCityEastHouse2F", "OreburghCitySouthHouse"
            }),
            ("floaroma", new[] { "FlowerShop", "FloaromaTownSoutheastHouse", "FloaromaTownMiddleHouse", "FloaromaMeadowHouse", "Route205House" }),
            ("eterna", new[] { "EternaCitySouthHouse", "EternaCityEastHouse", "EternaCityHerbShop", "EternaCondominiums1F", "EternaCondominiums2F", "EternaCondominiums3F" }),
            ("chateau", new[]
            {
                "OldChateau", "OldChateauDiningArea", "OldChateauWestRoom", "OldChateauEastRoom", "OldChateauCorridor", "OldChateauBackWestRoom",
                "OldChateauBackMiddleWestRoom", "OldChateauBackMiddleRoom", "OldChateauBackMiddleEastRoom", "OldChateauBackEastRoom"
            }),
            ("hearthome", new[]
            {
                "Route208House", "HearthomeCityNorthwestHouse", "HearthomeCitySoutheastHouse1F", "HearthomeCitySoutheastHouse2F", "ForeignBuilding",
                "HearthomeCityNortheastHouse1F", "HearthomeCityNortheastHouse2F"
            }),
            ("solaceon", new[] { "SolaceonTownNortheastHouse", "SolaceonTownPokemonNewsPress", "SolaceonTownNorthHouse" }),
            // Part b: the east and the sea
            ("veilstone", new[] { "CafeCabin", "VeilstoneCitySouthwestHouse", "VeilstoneCityNorthwestHouse", "VeilstoneCityNortheastHouse" }),
            ("lakefront", new[]
            {
                "SevenStarsRestaurant", "GrandLakeValorLakefrontEastHouse", "GrandLakeValorLakefrontWestHouse", "GrandLakeRoute213Lobby",
                "GrandLakeRoute213EastHouse", "GrandLakeRoute213NorthwestHouse", "GrandLakeRoute213NortheastHouse"
            }),
            ("pastoria", new[]
            {
                "PastoriaCityMiddleHouse", "PastoriaCityNorthHouse", "PastoriaCityNortheastHouse", "PastoriaCitySouthwestHouse",
                "PastoriaCityObservatoryGate1F", "PastoriaCityObservatoryGate2F"
            }),
            ("mansion", new[] { "PokemonMansion", "PokemonMansionMaidsRoomWest", "PokemonMansionMaidsRoomMiddle", "PokemonMansionMaidsRoomEast", "PokemonMansionOffice" }),
            ("canalave", new[] { "Route221House", "CanalaveCitySoutheastHouse", "CanalaveCitySailorEldritchHouse", "CanalaveCityWestHouse", "IronIslandHouse", "PalParkLobby" }),
            // Part c: the north and the Battle Zone
            ("celestic", new[] { "CelesticTownNorthwestHouse", "CelesticTownNortheastHouse", "CelesticTownSouthwestHouse" }),
            ("snow", new[] { "Route216House", "Route217WestHouse", "Route217NortheastHouse" }),
            ("sunyshore", new[] { "Route222EastHouse", "Route222WestHouse", "SunyshoreCityWestHouse", "SunyshoreCityNorthwestHouse", "SunyshoreCityEastHouse" }),
            ("battlezone", new[]
            {
                "FightAreaMiddleHouse", "FightAreaSouthHouse", "Route225House", "SurvivalAreaSouthHouse", "Route226House", "Route227House",
                "Route228SouthHouse", "ResortAreaHouse"
            }),
            ("syndicate", new[] { "ResortAreaRibbonSyndicate1F", "ResortAreaRibbonSyndicate2F", "ResortAreaRibbonSyndicateElevator" }),
            // Part d: the gate houses, the tutors' and the move men's houses, the Department Store, the Pokémon
            // Centers' other floors and the Global Terminal
            ("gates", new[]
            {
                "Route218GateToJubilifeCity", "Route218GateToCanalaveCity", "Route208GateToHearthomeCity", "Route209GateToHearthomeCity",
                "Route215GateToVeilstoneCity", "Route213GateToPastoriaCity", "Route222GateToSunyshoreCity", "Route228GateToRoute226",
                "Route212GateToHearthomeCity", "Route225GateToFightArea", "Route214GateToVeilstoneCity",
                "Route206CyclingRoadNorthGate", "Route206CyclingRoadSouthGate",
                "HearthomeCityWestGateToAmitySquare", "HearthomeCityEastGateToAmitySquare", "BattleFrontierGateToFightArea",
            }),
            ("tutors", new[]
            {
                "Route210GrandmaWilmaHouse", "Route212House", "PastoriaCityEastHouse", "CanalaveCityEastHouse",
                "SnowpointCityEastHouse", "SurvivalAreaNorthHouse", "Route228NorthHouse", "EternaCondominiums1F",
            }),
            ("store", new[]
            {
                "VeilstoneStoreB1F", "VeilstoneStore1F", "VeilstoneStore2F", "VeilstoneStore3F", "VeilstoneStore4F", "VeilstoneStore5F",
                "VeilstoneStoreElevator",
            }),
            ("centers", new[]
            {
                "JubilifePokemonCenter", "JubilifePokemonCenter2F", "JubilifePokemonCenterB1F",
                "PokemonLeagueNorthPokemonCenter", "PokemonLeagueNorthPokemonCenter2F", "PokemonLeagueNorthPokemonCenterB1F",
            }),
            ("terminal", new[] { "GlobalTerminal1F", "GlobalTerminal2F", "GlobalTerminal3F" }),
            ("gallery", new[] { "PalParkLobby", "PalParkGallery" }),
            // The rooms of the first chapters, rebuilt to the original's plans last (plan 01 · M11): the seventeen Pokémon
            // Centers' ground floors and the thirteen Marts
            ("pokecenters", new[]
            {
                "PokemonCenter", "JubilifePokemonCenter", "OreburghPokemonCenter", "FloaromaPokemonCenter", "EternaPokemonCenter",
                "HearthomePokemonCenter", "SolaceonPokemonCenter", "VeilstonePokemonCenter", "PastoriaPokemonCenter", "CanalavePokemonCenter",
                "CelesticPokemonCenter", "SnowpointPokemonCenter", "SunyshorePokemonCenter", "PokemonLeagueSouthPokemonCenter",
                "FightAreaPokemonCenter", "SurvivalAreaPokemonCenter", "ResortAreaPokemonCenter",
            }),
            ("marts", new[]
            {
                "PokeMart", "JubilifePokeMart", "OreburghPokeMart", "FloaromaPokeMart", "EternaPokeMart", "HearthomePokeMart", "SolaceonPokeMart",
                "PastoriaPokeMart", "CanalavePokeMart", "SnowpointPokeMart", "SunyshorePokeMart", "FightAreaPokeMart", "SurvivalAreaPokeMart",
            }),
        };
        var asked = args.Length > 2 ? args[2..].ToHashSet(StringComparer.OrdinalIgnoreCase) : null;

        // Where a room is looked at from: the mat inside its door, or failing a door the open tile beside its first
        // way in; a room too wide for the picture from a tile toward each end as well
        static List<(int X, int Y, Direction Facing)> Spots(Map map)
        {
            var spots = new List<(int, int, Direction)>();
            int h = map.Height, w = map.Width;
            var (left, back) = map.RoomCorner();
            for (int x = 0; x < w; x++)
                if (map.GetGroundTile(x, h - 1) == TileType.Door && map.IsWalkable(x, h - 2)) spots.Add((x, h - 2, Direction.Up));
            if (spots.Count == 0)
                foreach (var warp in map.Warps)
                {
                    foreach (var (dx, dy, facing) in new[] { (0, 1, Direction.Down), (1, 0, Direction.Right), (-1, 0, Direction.Left), (0, -1, Direction.Up) })
                    {
                        int x = warp.SourceX + dx, y = warp.SourceY + dy;
                        if (!map.InBounds(x, y) || !map.IsWalkable(x, y) || map.GetWarpAt(x, y) != null) continue;
                        spots.Add((x, y, facing));
                        break;
                    }
                    if (spots.Count > 0) break;
                }
            if (w - 1 - left > 30)
                foreach (int x in new[] { left + 8, w - 10 })
                    for (int y = h - 2; y >= back; y--)
                        if (map.IsWalkable(x, y)) { spots.Add((x, y, Direction.Up)); break; }
            return spots;
        }

        foreach (var (town, rooms) in towns)
        {
            if (asked != null && !asked.Contains(town)) continue;
            // The Ribbon Syndicate shows whoever hasn't ten ribbons out as they come in: its rooms are looked at as a member's
            bool joined = game.Story.Has("FLAG_JOINED_RIBBON_SYNDICATE");
            if (town == "syndicate") game.Story.Set("FLAG_JOINED_RIBBON_SYNDICATE");
            var shots = new List<string>();
            int n = 0;
            foreach (string room in rooms)
            {
                var map = MapDatabase.Get(room);
                n++;
                int k = 0;
                foreach (var (x, y, facing) in Spots(map))
                {
                    At(room, x, y, facing);
                    game.LocationSign.Hide();
                    Frames(3);
                    string name = $"in_{town}_{n:00}{(k == 0 ? "" : ((char)('a' + k)).ToString())}_{room}";
                    Shot(name);
                    shots.Add(name);
                    k++;
                }
                Console.WriteLine($"  {room}: {map.Width}x{map.Height}, {map.Everyone.Count(p => !p.IsThing)} people and things, {map.Props.Count} props, {map.Warps.Count} ways out");
            }
            // The sheet: three shots to a row at a third of their size
            const int cellW = 640, cellH = 360, gap = 6;
            int rows = (shots.Count + 2) / 3;
            var sheet = Raylib.GenImageColor(3 * cellW + 4 * gap, rows * (cellH + gap) + gap, new Color(24, 26, 34, 255));
            for (int i = 0; i < shots.Count; i++)
            {
                string path = Path.Combine(outDir, shots[i] + ".png");
                if (!File.Exists(path)) continue;
                var img = Raylib.LoadImage(path);
                Raylib.ImageResize(ref img, cellW, cellH);
                Raylib.ImageDraw(ref sheet, img, new Rectangle(0, 0, cellW, cellH),
                    new Rectangle(gap + i % 3 * (cellW + gap), gap + i / 3 * (cellH + gap), cellW, cellH), Color.White);
                Raylib.UnloadImage(img);
            }
            Save(sheet, "interiors_" + town);
            if (town == "syndicate" && !joined) game.Story.Unset("FLAG_JOINED_RIBBON_SYNDICATE");
        }
        bool workA = asked == null || asked.Contains("work"), workB = asked == null || asked.Contains("workb"), workC = asked == null || asked.Contains("workc");
        bool workD = asked == null || asked.Contains("workd");
        if (workD) InteriorsAtWorkD();
        if (workC) InteriorsAtWorkC();
        if (workB) InteriorsAtWorkB();
        if (!workA) return;

        // ---- a few of them at work
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // Reads every line on to the end of the script, shooting the line that holds a word; each question takes the
        // next of the answers given (the first answer once they run out). What a script does between its lines (a
        // fade, a walk) is waited for
        void ReadOn(string? word = null, string? shot = null, params int[] answers)
        {
            int asked = 0;
            for (int i = 0; i < 2400 && (engine.ScriptRunning || Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue); i++)
            {
                if (engine.Choice.IsOpen)
                {
                    Frames(24);
                    if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                    int answer = asked < answers.Length ? answers[asked] : 0;
                    asked++;
                    for (int k = 0; k < answer; k++) engine.Choice.Move(1);
                    engine.Choice.Confirm(); Frames(4);
                    continue;
                }
                if (!Box().IsActive) { Frames(1); continue; }
                Box().FinishLine(); Frames(2);
                // A question's answers come up as its line ends: the question is shot with them
                if (engine.Choice.IsOpen) continue;
                if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                Box().Advance(); Frames(2);
            }
            Until(() => game.State == GameState.Overworld && !Box().IsActive, "the text gone");
        }
        void Talk(string map, int x, int y, Direction facing) { At(map, x, y, facing); game.Interact(); Frames(3); }
        int Have(string item) => inventory.GetQuantity(ItemDatabase.Get(item)!);

        // A gift given once: the Quick Claw of the Jubilife Condominiums
        Talk("JubilifeCityCondominiums1F", 8, 7, Direction.Up);
        ReadOn("hold", "in_x01_quick_claw");
        Talk("JubilifeCityCondominiums1F", 8, 7, Direction.Up);
        ReadOn("slowcoach", "in_x02_quick_claw_after");
        Console.WriteLine($"  the Quick Claw: {Have("Quick Claw")}, its flag {game.Story.Has("FLAG_RECEIVED_JUBILIFE_CITY_CONDOMINIUMS_1F_QUICK_CLAW")}");

        // The stairs: up from the ground floor, and what is found there
        At("JubilifeCityCondominiums1F", 16, 3, Direction.Left);
        Frames(2); Shot("in_x03_stairs_foot");
        engine.Steering = (Direction.Left, false);
        Until(() => game.Map.Name == "JubilifeCityCondominiums2F" && game.State == GameState.Overworld, "the upper floor", 400);
        engine.Steering = null;
        Frames(20); Shot("in_x04_stairs_head");
        Console.WriteLine($"  up the stairs: {game.Map.Name} at {game.Player.GridX},{game.Player.GridY} facing {game.Player.Facing}");

        // The flower shop: the Sprayduck, the day's berry, and berries swapped for an accessory
        Talk("FlowerShop", 4, 8, Direction.Up);
        ReadOn("Sprayduck", "in_x05_sprayduck");
        Talk("FlowerShop", 14, 9, Direction.Up);
        ReadOn("spread", "in_x06_berry_of_the_day");
        inventory.AddItem(ItemDatabase.Get("Cheri Berry")!, 3);
        int cheri = Have("Cheri Berry");
        Talk("FlowerShop", 14, 5, Direction.Up);
        ReadOn("Which flower", "in_x07_accessories", 0, 0, 0, 0);
        Console.WriteLine($"  the flower shop: Sprayduck {Have("Sprayduck")}, Cheri Berries {cheri} then {Have("Cheri Berry")}, Red Flowers {game.FashionCase.Count(SuperContestDatabase.Accessory("Red Flower"))}");

        // The Berry Master, his granddaughter's app and the mulch
        Talk("Route208House", 4, 4, Direction.Right);
        game.Player.SetPosition(6, 5, Direction.Left);
        game.Interact(); Frames(3);
        ReadOn("Berry Master", "in_x08_berry_master");
        game.Player.SetPosition(8, 6, Direction.Up);
        int money = game.Money;
        game.Interact(); Frames(3);
        ReadOn("mulch", "in_x09_mulch", 1);
        Console.WriteLine($"  the Berry Master's house: Damp Mulch {Have("Damp Mulch")}, money {money} then {game.Money}, the day's flag {game.Story.Has("FLAG_DAILY_RECEIVED_ROUTE_208_HOUSE_RANDOM_BERRY")}");

        // A rest on Route 205
        var lead = game.Party.Members[0];
        lead.CurrentHP = 1;
        Talk("Route205House", 6, 6, Direction.Up);
        ReadOn("rest", "in_x10_rest");
        Console.WriteLine($"  a rest on Route 205: {lead.DisplayName} at {lead.CurrentHP} of {lead.MaxHP} HP");

        // A lift's doors, and the floor above
        At("HearthomeCitySoutheastHouse1F", 18, 3, Direction.Up);
        Frames(2); Shot("in_x11_lift_doors");
        // One step into the doors and no more: whoever keeps walking north upstairs walks into the lift again
        engine.Steering = (Direction.Up, false);
        Until(() => game.Player.IsMoving || game.State != GameState.Overworld, "a step into the lift", 120);
        engine.Steering = null;
        Until(() => game.Map.Name == "HearthomeCitySoutheastHouse2F" && game.State == GameState.Overworld, "the floor above", 400);
        Frames(20); Shot("in_x12_lift_above");
        Console.WriteLine($"  the lift: {game.Map.Name} at {game.Player.GridX},{game.Player.GridY} facing {game.Player.Facing}");

        // Bebe's Eevee
        Talk("HearthomeCityNorthwestHouse", 5, 6, Direction.Up);
        ReadOn("Eevee", "in_x13_bebe");
        Console.WriteLine($"  Bebe: the team is {string.Join(", ", game.Party.Members.Select(p => p.Species.Name))}");

        // The Old Chateau: its hall from the stairs' foot and its gallery, the way into the dining room, and the old
        // man at the head of the table on a day the die shows him (the die is the game's seeded one: the mode comes in
        // until it does, and says how many times that took)
        At("OldChateau", 9, 10, Direction.Up); Frames(3); Shot("in_x20_chateau_hall");
        At("OldChateau", 9, 4, Direction.Down); Frames(3); Shot("in_x21_chateau_gallery");
        At("OldChateau", 2, 6, Direction.Left); Frames(3); Shot("in_x22_chateau_gallery_door");
        int tries = 0;
        for (; tries < 60; tries++)
        {
            At("OldChateau", 9, 6, Direction.Up);
            engine.Steering = (Direction.Up, false);
            Until(() => game.Map.Name == "OldChateauDiningArea" && game.State == GameState.Overworld, "the dining room", 400);
            engine.Steering = null;
            Frames(4);
            if (game.Map.NPCs.Any(p => p.Key == "old_man")) { Shot("in_x23_chateau_ghost"); break; }
        }
        Console.WriteLine($"  the Old Chateau's old man: seen on visit {tries + 1}; then gone: {Wait(() => game.Map.NPCs.All(p => p.Key != "old_man"), 600)}");
        Frames(10); Shot("in_x24_chateau_dining_room");
        At("OldChateauBackMiddleWestRoom", 11, 5, Direction.Up); Frames(3); Shot("in_x25_chateau_television");
        game.Interact(); Frames(6);
        Console.WriteLine($"  the television: {(game.State == GameState.Dialogue ? "says something" : "says nothing yet")}");

        bool Wait(Func<bool> holds, int most)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            return holds();
        }
    }

    // ---- part b's rooms at work (`in_y*`), through the game's own TryInteract, steps, question boxes and battles
    void InteriorsAtWorkB()
    {
        var team = game.Party;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // A battle won at once: every foe brought down to 1 HP and struck, the team kept well. Each trainer's team is
        // weakened, not the first's alone: a couple of the restaurant are two trainers with a team each
        bool Win()
        {
            var fight = game.Battle;
            for (int guard = 0; guard < 120 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main)
                {
                    foreach (var mine in team.Members.Where(p => p.CurrentHP > 0)) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
                    foreach (var foe in fight.Trainers.SelectMany(t => t.Party.Members)) foe.CurrentHP = Math.Min(foe.CurrentHP, 1);
                    foreach (var foe in fight.Core.EnemySlots.Where(b => b.Pokemon != null)) foe.Pokemon!.CurrentHP = Math.Min(foe.Pokemon.CurrentHP, 1);
                    fight.SelectMainMenuOption(0);
                    fight.SelectMove(0);
                    if (fight.HUD.MenuState == BattleMenuState.SelectTarget) fight.SelectTarget(0);
                }
                else Confirm(fight);
                Skip(0.5);
            }
            if (!fight.IsBattleOver) { Console.WriteLine("  !! a battle that wouldn't end"); return false; }
            Until(() => game.State != GameState.Battle, "the field again", 1500);
            foreach (var mine in team.Members) { mine.CurrentHP = mine.MaxHP; mine.Status = StatusCondition.None; }
            Frames(10);
            return game.State != GameState.Battle;
        }
        // Reads every line on to the end of the script, shooting the first line that holds each word given (a word to a
        // shot); each question takes the next of the answers (the first once they run out), the team's first Pokémon is
        // chosen when one is asked for, and every battle is won (shot at its menu, if a shot is named for it)
        // (a word may be one of several, "this|that"; `heard` is told every line as it stands whole)
        void ReadOn((string Word, string Shot)[] shots, int[]? answers = null, string? battleShot = null, Action<string>? heard = null)
        {
            var left = shots.ToList();
            int answered = 0;
            void Look()
            {
                heard?.Invoke(Box().VisibleText);
                var hit = left.FirstOrDefault(s => s.Word.Split('|').Any(word => Box().VisibleText.Contains(word, StringComparison.Ordinal)));
                if (hit.Shot == null) return;
                Shot(hit.Shot);
                left.Remove(hit);
            }
            for (int i = 0; i < 6000; i++)
            {
                bool busy = engine.ScriptRunning || Box().IsActive || engine.Choice.IsOpen || game.State is GameState.Dialogue or GameState.Battle or GameState.PartyMenu;
                if (!busy && i > 4) break;
                if (game.State == GameState.Battle)
                {
                    // Read on past the challenge, the Pokémon sent out and what their abilities say, to the menu
                    for (int k = 0; k < 40 && game.Battle.HUD.MenuState != BattleMenuState.Main && !game.Battle.IsBattleOver; k++) { Confirm(game.Battle); Skip(0.5); }
                    if (battleShot != null) { Skip(1.5); Shot(battleShot); battleShot = null; }
                    if (!Win()) return;
                    continue;
                }
                if (game.State == GameState.PartyMenu)
                {
                    Frames(10);
                    game.PartyScreen.SelectedIndex = 0;
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
        void Arrive(string map, int x, int y, Direction facing) { At(map, x, y, facing); game.ArriveOnMap(); Frames(4); }
        int Have(string item) => inventory.GetQuantity(ItemDatabase.Get(item)!);
        // A team of two that can stand at the restaurant's tables: the lead's first move is one that strikes
        team.Clear();
        team.Add(new Pokemon(PokemonDatabase.Get("Torterra")!, 45));
        team.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 45));
        foreach (var mine in team.Members)
        {
            mine.Moves.Clear();
            mine.Moves.Add(new Move(MoveDatabase.Get(mine.Species.Name == "Torterra" ? "Razor Leaf" : "Wing Attack")!));
        }
        var lead = team.Members[0];
        game.Money = Math.Max(game.Money, 20000);

        // Café Cabin: a bottle of Moomoo Milk over the bar
        int money = game.Money;
        Talk("CafeCabin", 6, 4, Direction.Right);
        ReadOn(new[] { ("road", "in_y01_cafe_milk"), ("Enjoy", "in_y02_cafe_bottle") });
        Console.WriteLine($"  the café: Moomoo Milk {Have("Moomoo Milk")}, money {money} then {game.Money}");

        // The Massage Girl: the first of the team, three points friendlier, and what she found
        int friendship = lead.Friendship;
        Talk("VeilstoneCitySouthwestHouse", 5, 6, Direction.Up);
        ReadOn(new[] { ("massage", "in_y03_massage"), ("content", "in_y04_massage_content"), ("received", "in_y05_massage_accessory") });
        Console.WriteLine($"  the massage: {lead.DisplayName}'s friendship {friendship} then {lead.Friendship}; the day's flag {game.Story.Has("FLAG_DAILY_GOT_MASSAGE_VEILSTONE_CITY_SOUTHWEST_HOUSE")}; accessories of hers in the case {ScriptRunner.MassageAccessories.Sum(a => game.FashionCase.Count(SuperContestDatabase.Accessory(a)))}");

        // The Seven Stars Restaurant: the day's five couples at their tables, one asked, and their double battle
        Arrive("SevenStarsRestaurant", 9, 12, Direction.Up);
        Frames(6); Shot("in_y06_restaurant_tables");
        var diner = game.Map.NPCs.Where(n => n.IsTrainer).OrderBy(n => Math.Abs(n.GridX - 9) + Math.Abs(n.GridY - 12)).First();
        Console.WriteLine($"  the restaurant: {game.Map.NPCs.Count(n => n.IsTrainer)} diners at their tables: {string.Join(", ", game.Map.NPCs.Where(n => n.IsTrainer).Select(n => n.Name))}");
        Talk("SevenStarsRestaurant", diner.GridX, diner.GridY + 1, Direction.Up);
        money = game.Money;
        ReadOn(new[] { ("two of us", "in_y07_restaurant_asked") }, battleShot: "in_y08_restaurant_battle");
        Console.WriteLine($"  {diner.Name}'s table: beaten today {game.Story.Flags.Count(f => f.StartsWith("FLAG_DAILY_BEAT_RESTAURANT_", StringComparison.Ordinal))}, money {money} then {game.Money}");

        // The Suite Key: the beauty before her cottage lets herself in, and gives the White Flute inside
        // (the cottage's door is the lakefront's warp 1, at 715, 781: she stands on the tile before it)
        inventory.AddItem(ItemDatabase.Get("Suite Key")!, 1);
        At("Sinnoh", 715, 783, Direction.Up);
        game.ArriveOnMap(); Frames(30);
        Shot("in_y09_suite_key_before");
        game.Interact(); Frames(3);
        ReadOn(new[] { ("my suite key!", "in_y10_suite_key") });
        Frames(20); Shot("in_y11_suite_key_gone");
        Arrive("GrandLakeValorLakefrontEastHouse", 6, 8, Direction.Up);
        var guest = game.Map.NPCs.FirstOrDefault(n => n.Script == "WhiteFlute");
        Console.WriteLine($"  the Suite Key: {Have("Suite Key")} left, the beauty {(guest != null ? $"in her cottage at {guest.GridX},{guest.GridY}" : "NOT in her cottage")}");
        if (guest != null)
        {
            At("GrandLakeValorLakefrontEastHouse", guest.GridX, guest.GridY + 1, Direction.Up);
            game.Interact(); Frames(3);
            ReadOn(new[] { ("trouble", "in_y12_white_flute") });
            Console.WriteLine($"  the White Flute: {Have("White Flute")}");
        }

        // The Hotel Grand Lake's lobby: a rest over the reception desk
        lead.CurrentHP = 1;
        Talk("GrandLakeRoute213Lobby", 9, 4, Direction.Right);
        ReadOn(new[] { ("reservation", "in_y13_lobby_rest") });
        Console.WriteLine($"  the lobby's rest: {lead.DisplayName} at {lead.CurrentHP} of {lead.MaxHP} HP");

        // The Safari Game: up to the counter, the fee, into the marsh by the back door; back out with the game on, the
        // attendant's question, and the cowgirl's app for five caught
        money = game.Money;
        At("PastoriaCityObservatoryGate1F", 5, 8, Direction.Up);
        Frames(3); Shot("in_y14_observatory");
        engine.Steering = (Direction.Up, false);
        Until(() => engine.ScriptRunning, "the counter's question", 240);
        engine.Steering = null;
        ReadOn(new[] { ("just $500", "in_y15_safari_counter"), ("Safari Balls.", "in_y16_safari_balls") });
        Until(() => game.Map.Name == "GreatMarsh" && game.State == GameState.Overworld, "the marsh", 600);
        Frames(30); Shot("in_y17_safari_marsh");
        Console.WriteLine($"  the Safari Game: on {game.Safari.Active}, {game.Safari.Balls} balls and {game.Safari.Steps} steps, money {money} then {game.Money}, in {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}");
        game.Safari.Caught = 5;
        engine.Steering = (Direction.Down, false);
        Until(() => game.Map.Name == "PastoriaCityObservatoryGate1F", "the observatory again", 600);
        engine.Steering = null;
        // (the room's arrival asks once the fade has opened on it)
        Until(() => game.State == GameState.Dialogue || engine.Choice.IsOpen || Box().IsActive, "the attendant's question", 900);
        ReadOn(new[] { ("still have time", "in_y18_safari_leave"), ("Five at least", "in_y19_matchup_checker") });
        Console.WriteLine($"  back at the counter: the game on {game.Safari.Active}, the player at {game.Player.GridX},{game.Player.GridY}, the Matchup Checker {game.Poketch.Has(PoketchApp.MatchupChecker)}");

        // The binoculars upstairs
        Talk("PastoriaCityObservatoryGate2F", 6, 4, Direction.Up);
        ReadOn(new[] { ("binoculars", "in_y20_binoculars"), ("swings", "in_y21_binoculars_view"), ("A ", "in_y22_binoculars_pokemon") });

        // The Pokémon Mansion: the hall, the west corridor with its doorways, and a door of it taken
        Arrive("PokemonMansion", 33, 12, Direction.Up);
        Frames(6); Shot("in_y23_mansion_hall");
        At("PokemonMansion", 13, 8, Direction.Up); Frames(3); Shot("in_y24_mansion_corridor");
        At("PokemonMansion", 13, 6, Direction.Up); Frames(2);
        engine.Steering = (Direction.Up, false);
        Until(() => game.Map.Name == "PokemonMansionMaidsRoomMiddle" && game.State == GameState.Overworld, "the maids' room", 400);
        engine.Steering = null;
        Frames(20); Shot("in_y25_maids_room");
        Console.WriteLine($"  through the corridor's door: {game.Map.Name} at {game.Player.GridX},{game.Player.GridY} facing {game.Player.Facing}");

        // The maids' challenge: five battles won at once come to no turns, which is not the day's number...
        Arrive("PokemonMansion", 2, 7, Direction.Left);
        game.Interact(); Frames(3);
        ReadOn(new[] { ("Exact-Turn", "in_y26_challenge"), ("First up", "in_y27_first_maid"), ("four maids", "in_y28_four_maids"), ("sweep", "in_y29_sweep") }, battleShot: "in_y2a_maid_battle");
        Console.WriteLine($"  the maids' challenge: target {game.Story.Var("VAR_FIVE_MAID_KNOCKOUT_TURN_TARGET")}, taken today {game.Story.Has("FLAG_DAILY_TOOK_FIVE_MAID_KNOCKOUT_CHALLENGE")}, the maids beaten {new[] { "maid_belinda", "maid_sophie", "maid_emily", "maid_elena", "maid_clare" }.Count(game.Story.HasDefeated)}");
        // ... and the maids' Clefairy hold on with Endure as they please, so for the prodigy the day's number is made the
        // number the sweep took, as the maid tells it (the mode's rigging, as a slot machine's draw is rigged)
        game.Story.ClearDaily();
        Arrive("PokemonMansion", 2, 7, Direction.Left);
        game.Interact(); Frames(3);
        ReadOn(new[] { ("prodigy", "in_y2b_prodigy_named"), ("pull me|called me out", "in_y2c_prodigy") }, heard: text =>
        {
            int at = text.IndexOf("maids was ", StringComparison.Ordinal);
            if (at < 0) return;
            string digits = new string(text[(at + 10)..].TakeWhile(char.IsDigit).ToArray());
            if (digits.Length > 0) game.Story.SetVar("VAR_FIVE_MAID_KNOCKOUT_TURN_TARGET", int.Parse(digits));
        });
        Console.WriteLine($"  the prodigy: Liam beaten {game.Story.HasDefeated("rich_boy_liam")}, Celeste beaten {game.Story.HasDefeated("lady_celeste")}");

        // Mr. Backlot's office: his boast once the Hall of Fame is behind the player, the butler sent out, the statue
        // guarded, and the book
        bool champion = game.Story.Has("FLAG_GAME_COMPLETED");
        game.Story.Set("FLAG_GAME_COMPLETED");
        Arrive("PokemonMansionOffice", 7, 10, Direction.Up);
        Frames(6); Shot("in_y30_office");
        Talk("PokemonMansionOffice", 7, 9, Direction.Up);
        ReadOn(new[] { ("envious", "in_y31_backlot"), ("kiss", "in_y32_backlot_story"), ("At once", "in_y33_butler") });
        Frames(10); Shot("in_y34_butler_gone");
        Console.WriteLine($"  Mr. Backlot: the Trophy Garden's newest is {TrophyGardenRules.SpeciesIn(game.Encounters.TrophyFirst, SpecialEncounterTables.Sinnoh.TrophyGarden)}, the day's flag {game.Story.Has("FLAG_DAILY_ADDED_TROPHY_GARDEN_MON")}");
        if (!champion) game.Story.Unset("FLAG_GAME_COMPLETED");
        At("PokemonMansionOffice", 7, 5, Direction.Up); Frames(2);
        engine.Steering = (Direction.Up, false);
        Until(() => engine.ScriptRunning, "the guard's word", 240);
        engine.Steering = null;
        ReadOn(new[] { ("hands off", "in_y35_statue_guarded") });
        Console.WriteLine($"  before the statue: the player at {game.Player.GridX},{game.Player.GridY}");

        // The old man of Route 221 and the day's level
        game.Story.SetVar(DailyEvents.RandomLevelVariable, lead.Level);
        Talk("Route221House", 3, 5, Direction.Left);
        ReadOn(new[] { ("number", "in_y36_level"), ("Remarkable", "in_y37_level_shown") });
        Console.WriteLine($"  Route 221's old man: Black Belt {Have("Black Belt")}, the day's flag {game.Story.Has("FLAG_DAILY_RECEIVED_ROUTE_221_HOUSE_REWARD")}");
    }

}
