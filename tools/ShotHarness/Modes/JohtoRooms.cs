using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    /// <summary>
    /// Plan 20 · J15, part s (<c>JOHTO_FROM=rooms</c>, not part of <c>all</c>): the imported rooms whose inner walls have
    /// looks of their own (the Goldenrod Gym's beds, the tunnel's counters, the S.S. Aqua's hold, the Cianwood and Mahogany
    /// Gyms, Buena's floor, the lighthouse's ground floor), and part q's and part r's people and places through their
    /// arrival scripts (<c>jrs*</c>). Every shot comes through <c>ArriveOnMap</c>, so a place's own <c>OnEnter</c> has run.
    /// </summary>
    void JohtoRoomShots()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);

        (int X, int Y) Open(Map map, int x, int y) => game.Map == map && map.IsWalkable(x, y) ? (x, y)
            : Enumerable.Range(0, map.Width * map.Height).Select(i => (X: i % map.Width, Y: i / map.Width))
                .Where(t => map.IsWalkable(t.X, t.Y)).OrderBy(t => Math.Abs(t.X - x) + Math.Abs(t.Y - y)).First();
        void Spot(string name, int x, int y, Direction way, string shot, int frames = 20)
        {
            var map = MapDatabase.Get(name);
            At(name, x, y, way);
            (x, y) = Open(game.Map, x, y);
            At(name, x, y, way); game.ArriveOnMap(); Frames(frames);
            Shot(shot);
            Console.WriteLine($"  {shot}: {name} {x},{y} ({map.Width}x{map.Height}, {map.Interior})");
        }
        void Middle(string name, string shot, float fy = 0.5f)
        {
            var map = MapDatabase.Get(name);
            Spot(name, map.Width / 2, (int)(map.Height * fy), Direction.Up, shot);
        }
        // Someone of a place, shown whatever their flags say, from two tiles south of them
        void Person(string name, string key, string shot)
        {
            var map = MapDatabase.Get(name);
            var npc = map.Everyone.FirstOrDefault(n => n.Key == key);
            if (npc == null) { Console.WriteLine($"  !! nobody {key} on {name}"); return; }
            At(name, npc.GridX, npc.GridY + 2, Direction.Up);
            var (x, y) = Open(game.Map, npc.GridX, npc.GridY + 2);
            At(name, x, y, Direction.Up); game.ArriveOnMap(); Frames(10);
            npc.Forced = true; game.Map.ApplyPresence(game.Story.Has); Frames(10);
            Shot(shot);
            Console.WriteLine($"  {shot}: {key} at {npc.GridX},{npc.GridY} level {npc.Level}, facing {npc.Facing}, look {npc.NpcType}; player {x},{y}");
        }

        // ---- The rooms whose inner walls have looks of their own
        Middle("Goldenrod_Gym", "jrs01_goldenrod_gym", 0.7f);
        Middle("Goldenrod_Gym", "jrs02_goldenrod_gym_back", 0.3f);
        Middle("Goldenrod_Underground", "jrs03_goldenrod_underground");
        Middle("Goldenrod_Underground_B1F", "jrs04_goldenrod_underground_b1f");
        Middle("SSAqua_B1F", "jrs05_ss_aqua_hold");
        Spot("SSAqua_B1F", 30, 15, Direction.Up, "jrs06_ss_aqua_hold_east");
        Middle("Cianwood_Gym", "jrs07_cianwood_gym");
        Middle("Mahogany_Gym", "jrs08_mahogany_gym");
        Middle("Goldenrod_RadioTower_2F", "jrs09_buenas_floor");
        Middle("OlivineLighthouse_1F", "jrs10_lighthouse_ground_floor");
        Middle("OlivineLighthouse_1F", "jrs11_lighthouse_ground_floor_back", 0.3f);
        Middle("SSAqua_1F", "jrs12_ss_aqua_corridor");

        // ---- Part q's people
        Person("Johto", "route_40_baoba", "jrs20_baoba_on_route_40");
        Person("Route22_LeagueReceptionGate", "reception_gate_blue", "jrs21_blue_at_the_reception_gate");
        Person("Cianwood_PhotoStudio", "t24r0601_fishing_2", "jrs22_camerons_father");
        game.Story.Set("FLAG_JOHTO_MET_CAMERONS_FATHER");
        Person("Celadon_DeptStore_2F", "t07r0102_gsmiddleman1_2", "jrs23_cameron_in_celadons_store");
        var cameron = game.Map.NPCs.First(n => n.Key == "t07r0102_gsmiddleman1_2");
        At("Celadon_DeptStore_2F", cameron.GridX, cameron.GridY + 1, Direction.Up); Frames(4);
        game.Interact();
        for (int i = 0; i < 600 && (engine.ScriptRunning || game.Dialogue.IsActive); i++)
        {
            if (engine.Choice.IsOpen) { Frames(6); engine.Choice.Confirm(); Frames(4); }
            else if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) { game.Dialogue.Advance(); Frames(2); }
            else Frames(1);
        }
        var father = MapDatabase.Get("Cianwood_PhotoStudio").Everyone.First(n => n.Key == "t24r0601_fishing_2");
        At("Cianwood_PhotoStudio", father.GridX, father.GridY + 1, Direction.Up); game.ArriveOnMap(); Frames(10);
        game.Interact();
        for (int i = 0; i < 600 && game.State != GameState.PhotoAlbum; i++)
        {
            if (engine.Choice.IsOpen) { Frames(6); engine.Choice.Confirm(); Frames(4); }
            else if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) { game.Dialogue.Advance(); Frames(2); }
            else Frames(1);
        }
        Frames(240); Shot("jrs24_the_album_page");
        Console.WriteLine($"  album open {game.State == GameState.PhotoAlbum}");
        game.State = GameState.Overworld; game.Dialogue.Close();
        for (int i = 0; i < 200 && engine.ScriptRunning; i++) { if (game.Dialogue.IsActive) game.Dialogue.Advance(); Frames(1); }
        Person("CliffEdgeGate", "d48r0101_gsleader5", "jrs25_chuck_in_the_cliff_edge_gate");
        Person("NationalPark", "d22r0101_gsleader2", "jrs26_bugsy_in_the_national_park");
        Person("Johto", "r39_gsleader9", "jrs27_lt_surge_on_route_39");
        Person("Saffron_MagnetTrainStation", "t11r0601_gsleader9", "jrs28_lt_surge_at_saffrons_station");
        Person("Saffron_SilphCo", "t11r0701_daigo", "jrs29_steven_at_silph_co");

        // ---- Part r: the basement's crates on a Tuesday, and the Embedded Tower's rooms through their arrivals
        var day = GameClock.FixedDate;
        GameClock.FixedDate = new DateTime(2026, 6, 2);
        Middle("Goldenrod_DeptStore_Basement", "jrs30_basement_on_a_tuesday");
        GameClock.FixedDate = day;
        Middle("Goldenrod_DeptStore_Basement", "jrs31_basement_on_a_monday");
        game.Story.Set("FLAG_JOHTO_GOT_ORBS");
        game.Story.Set("FLAG_JOHTO_FOUGHT_GROUDON");
        game.Story.Set("FLAG_JOHTO_GOT_JADE_ORB");
        game.Story.Unset("FLAG_JOHTO_FOUGHT_GROUDON");
        Spot("EmbeddedTower_GroudonRoom", 16, 16, Direction.Up, "jrs32_embedded_tower_groudon");
        game.Story.Set("FLAG_JOHTO_FOUGHT_GROUDON");
        Spot("EmbeddedTower_KyogreRoom", 16, 16, Direction.Up, "jrs33_embedded_tower_kyogre");
        Spot("EmbeddedTower_RayquazaRoom", 16, 16, Direction.Up, "jrs34_embedded_tower_rayquaza");
        foreach (var name in new[] { "EmbeddedTower_GroudonRoom", "EmbeddedTower_KyogreRoom", "EmbeddedTower_RayquazaRoom" })
            Console.WriteLine($"  {name}: {string.Join(", ", MapDatabase.Get(name).NPCs.Select(n => $"{n.Key} {n.GridX},{n.GridY}"))}");
    }
}
