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
    // floaroma, eterna, chateau, hearthome, solaceon.
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
        }
        if (asked != null && !asked.Contains("work")) return;

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
}
