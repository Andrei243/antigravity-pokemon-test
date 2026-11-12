partial class Harness
{
    /// <summary>
    /// The places Z5's second part drew and left unshot (the <c>melemele</c> mode, not part of <c>all</c>; plan 23 · Z5):
    /// a new game in Alola, then the rest of Melemele and Akala as far as Brooklet Hill by day (<c>wm01</c> to <c>wm16</c>:
    /// Hau'oli City's east end and marina, Route 2 by its Pokémon Center and before Verdant Cavern, Route 3, Melemele
    /// Meadow, the shore before Ten Carat Hill, inside Ten Carat Hill and Verdant Cavern, Heahea City's plaza and pier,
    /// Route 4, Paniola Town, Paniola Ranch, Route 5 by its Pokémon Center and Brooklet Hill), Route 2 and the meadow
    /// after dark (<c>wm17</c>, <c>wm18</c>) the part's rooms (<c>wm20</c> to <c>wm29</c>), and the Trainers' School built after it, from its street and inside (<c>wm19</c>, <c>wm30</c>), and Sandy Cave's mouth, Hau'oli's beach on its widened sea, the cave inside and its boulder (<c>wm31</c> to <c>wm34</c>). Z5 had named them
    /// <c>wa30</c> to <c>wa63</c>, but the <c>ultraspace</c> mode took <c>wa50</c> on. Nothing is placed by a typed tile:
    /// an area's spot is the open ground nearest its middle (<see cref="AreaSpot"/>), a pier's or a street's is beside
    /// the person of the overlay who stands there, a door's is two tiles before it, and a room's is the tile it is come
    /// into by; each tile used is printed. The chapter's scenes are played headless by <c>AlolaIslandsTests</c>.
    /// </summary>
    public void MelemeleMode()
    {
        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);

        var spots = new List<(string Name, Map Map, int X, int Y)>
        {
            BesidePerson("wm01_hauoli_east_end", "Melemele", "shopper", "hauoli_city"),
            BesidePerson("wm02_hauoli_marina", "Melemele", "sailor", "hauoli_city"),
            BeforeDoor("wm03_route2_center", "Melemele", "AlolaRoute2_PokemonCenter_1F"),
            BeforeDoor("wm04_route2_verdant_cavern", "Melemele", AreaSpot("verdant_cavern").Map.Name),
            InArea("wm05_route3", "alola_route_3"),
            InArea("wm06_melemele_meadow", "melemele_meadow"),
            BeforeDoor("wm07_ten_carat_shore", "Melemele", AreaSpot("ten_carat_hill").Map.Name),
            InArea("wm08_ten_carat_hill", "ten_carat_hill"),
            InArea("wm09_verdant_cavern", "verdant_cavern"),
            InArea("wm10_heahea_plaza", "heahea_city"),
            BesidePerson("wm11_heahea_pier", "Akala", "sailor", "heahea_city"),
            InArea("wm12_route4", "akala_route_4"),
            InArea("wm13_paniola_town", "paniola_town"),
            InArea("wm14_paniola_ranch", "paniola_ranch"),
            BeforeDoor("wm15_route5_center", "Akala", "AkalaRoute5_PokemonCenter_1F"),
            InArea("wm16_brooklet_hill", "brooklet_hill"),
            BeforeDoor("wm19_hauoli_trainers_school", "Melemele", "HauoliCity_TrainersSchool"),
        };
        foreach (var (name, map, x, y) in spots) ShootAt(name, map, x, y, Direction.Up);

        string[] rooms =
        {
            "AlolaRoute2_PokemonCenter_1F", "AlolaRoute2_BerryFieldsHouse", "HauoliCity_EastHouse", "HauoliCity_IlimasHouse",
            "HauoliCity_MalasadaShop", "HeaheaCity_PokemonCenter_1F", "HeaheaCity_House", "PaniolaTown_PokemonCenter_1F",
            "PaniolaTown_House", "AkalaRoute5_PokemonCenter_1F", "HauoliCity_TrainersSchool",
        };
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = MapDatabase.Get(rooms[i]);
            var (x, y) = SaveData.ArrivalIn(room, MapDatabase.Get) is { } spot ? (spot.X, spot.Y)
                : (room.Width / 2, room.Height - 3);
            ShootAt($"wm{20 + i}_{Snake(rooms[i])}", room, x, y, Direction.Up);
        }

        // Sandy Cave (plan 23, Melemele's last places): its rock off Big Wave Beach from the spit of sand before its mouth,
        // Hau'oli's sea from the beach west of the marina, and inside the cave from its way in and before the boulder
        var melemele = MapDatabase.Get("Melemele");
        var mouth = melemele.Warps.Single(w => w.TargetMap == "SandyCave");
        ShootAt("wm31_sandy_cave_mouth", melemele, mouth.SourceX, mouth.SourceY - 1, Direction.Down);
        var hauoli = BesidePerson("wm32_hauoli_beach", "Melemele", "sailor", "hauoli_city");
        ShootAt(hauoli.Name, hauoli.Map, hauoli.X - 30, OpenNear(hauoli.Map, hauoli.X - 30, 177).Y, Direction.Down);
        var cave = MapDatabase.Get("SandyCave");
        var (inX, inY) = SaveData.ArrivalIn(cave, MapDatabase.Get) is { } c ? (c.X, c.Y) : (22, 6);
        ShootAt("wm33_sandy_cave", cave, inX, inY, Direction.Down);
        var boulder = cave.Everyone.Single(n => n.Obstacle == PropType.StrengthBoulder);
        ShootAt("wm34_sandy_cave_boulder", cave, boulder.GridX - 3, boulder.GridY + 1, Direction.Right);

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        foreach (var (name, map, x, y) in spots.Where(s => s.Name.StartsWith("wm03", StringComparison.Ordinal) || s.Name.StartsWith("wm06", StringComparison.Ordinal)))
            ShootAt(name.StartsWith("wm03", StringComparison.Ordinal) ? "wm17_route2_night" : "wm18_melemele_meadow_night", map, x, y, Direction.Up);
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
    }

    void ShootAt(string name, Map map, int x, int y, Direction facing)
    {
        if (!Wanted(name)) return;
        game.Place(map, x, y, facing);
        Settle();
        game.LocationSign.Hide();
        Console.WriteLine($"  {name}: {map.Name} ({x}, {y}), {map.DisplayNameAt(x, y)}");
        Shot(name);
    }

    static (string Name, Map Map, int X, int Y) InArea(string name, string key)
    {
        var (map, x, y) = AreaSpot(key);
        return (name, map, x, y);
    }

    // Two tiles south of someone of the overlay, or the open tile nearest that
    static (string Name, Map Map, int X, int Y) BesidePerson(string name, string mapName, string key, string place)
    {
        var map = MapDatabase.Get(mapName);
        var person = map.FindPerson(key, place) ?? throw new InvalidOperationException($"{place} has nobody called {key}");
        var (x, y) = OpenNear(map, person.GridX, person.GridY + 2);
        return (name, map, x, y);
    }

    // Two tiles before the door that leads into a map
    static (string Name, Map Map, int X, int Y) BeforeDoor(string name, string mapName, string target)
    {
        var map = MapDatabase.Get(mapName);
        var door = map.Warps.FirstOrDefault(w => string.Equals(w.TargetMap, target, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"{mapName} has no way into {target}");
        var (x, y) = OpenNear(map, door.SourceX, door.SourceY + 2);
        return (name, map, x, y);
    }

    // The walkable tile nearest a tile, within eight
    static (int X, int Y) OpenNear(Map map, int x, int y)
    {
        for (int r = 0; r <= 8; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    int tx = x + dx, ty = y + dy;
                    if (map.InBounds(tx, ty) && map.IsWalkable(tx, ty)) return (tx, ty);
                }
        return (x, y);
    }

    // AlolaRoute2_PokemonCenter_1F -> alolaroute2_pokemoncenter_1f
    static string Snake(string name) => name.ToLowerInvariant();
}
