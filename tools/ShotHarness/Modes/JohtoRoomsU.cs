using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    /// <summary>
    /// Plan 20 · J15, part u (<c>JOHTO_FROM=roomsu</c>, not part of <c>all</c>): the imported rooms whose inner walls were
    /// still the room's wall cut to the wainscot (<c>jru_*</c>; <c>JOHTO_ROOMS=a,b</c> shoots only those), the S.S. Aqua's
    /// hold with its table, and part t's people and pictures: Steven in the museum and in Pewter City with his friend, the
    /// museum's second Steven, the night aboard the S.S. Aqua, an album page of each scene and Cameron at some of his places.
    /// </summary>
    void JohtoRoomShotsU()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);

        (int X, int Y) Open(Map map, int x, int y) => map.IsWalkable(x, y) ? (x, y)
            : Enumerable.Range(0, map.Width * map.Height).Select(i => (X: i % map.Width, Y: i / map.Width))
                .Where(t => map.IsWalkable(t.X, t.Y)).OrderBy(t => Math.Abs(t.X - x) + Math.Abs(t.Y - y)).First();
        void Spot(string name, int x, int y, Direction way, string shot, int frames = 20)
        {
            var map = MapDatabase.Get(name);
            (x, y) = Open(map, x, y);
            At(name, x, y, way); game.ArriveOnMap(); Frames(frames);
            ReadOn(() => false, 300); Frames(4);
            Shot(shot);
            Console.WriteLine($"  {shot}: {name} {x},{y} ({map.Width}x{map.Height}, {map.Interior}, {map.Partitions})");
        }
        void Person(string name, string key, string shot)
        {
            var map = MapDatabase.Get(name);
            var npc = map.Everyone.FirstOrDefault(n => n.Key == key);
            if (npc == null) { Console.WriteLine($"  !! nobody {key} on {name}"); return; }
            var (x, y) = Open(map, npc.GridX, npc.GridY + 2);
            At(name, x, y, Direction.Up); game.ArriveOnMap(); Frames(10);
            ReadOn(() => false, 300); Frames(4);
            bool there = game.Map.NPCs.Contains(npc);
            if (!there) { npc.Forced = true; game.Map.ApplyPresence(game.Story.Has); }
            Frames(10);
            Shot(shot);
            Console.WriteLine($"  {shot}: {key} at {npc.GridX},{npc.GridY} {(there ? "there by its flags" : "FORCED")}, look {npc.NpcType}; player {x},{y}; script {game.ScriptNow}; state {game.State}");
        }
        void ReadOn(Func<bool> until, int guard = 900)
        {
            for (int i = 0; i < guard && !until() && (engine.ScriptRunning || game.Dialogue.IsActive); i++)
            {
                if (engine.Choice.IsOpen) { Frames(6); engine.Choice.Confirm(); Frames(4); }
                else if (game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete && !game.Dialogue.IsQuestion) { game.Dialogue.Advance(); Frames(2); }
                else Frames(1);
            }
            // (a line left on the box by a script that has ended, which nothing reads on, is taken down)
            if (!engine.ScriptRunning && game.Dialogue.IsActive && game.State != GameState.Dialogue) game.Dialogue.Close();
        }

        // (the hideout's welcome is a scene of its own, which would hold every shot after it: it is had already)
        game.Story.SetVar("VAR_UNK_40A9", 1);
        string only = Environment.GetEnvironmentVariable("JOHTO_ROOMS") ?? "";
        string[] rooms = only == "none" ? Array.Empty<string>() : only.Length > 0 ? only.Split(',', StringSplitOptions.RemoveEmptyEntries) : RoomsToLookAt;
        // (the rooms come last, since a room's own scene may still be running when the next shot is taken; JOHTO_ROOMS
        // names some and shoots only those, JOHTO_ROOMS=none shoots only the hold and part t's)
        void TheRooms()
        {
            foreach (var name in rooms)
            {
                var map = MapDatabase.Get(name);
                Spot(name, map.Width / 2, (int)(map.Height * 0.6f), Direction.Up, $"jru_{name}");
            }
        }
        if (only.Length > 0 && only != "none") { TheRooms(); return; }

        // ---- The S.S. Aqua's hold, its middle table
        Spot("SSAqua_B1F", 33, 12, Direction.Up, "jru2_ss_aqua_hold_table");

        // ---- Part t: Steven's stone, his friend, and the museum after
        game.Story.Set("FLAG_JOHTO_GOT_HOENN_STARTER");
        Person("Pewter_Museum", "t03r0101_daigo", "jru3_steven_in_the_museum");
        game.Story.Set("FLAG_JOHTO_STEVEN_SHOWED_STONE");
        Person("Johto", "t03_daigo", "jru4_steven_in_pewter");
        Person("Johto", "t03_tsure_poke_static_latias", "jru5_stevens_friend_in_pewter");
        game.Story.Set("FLAG_JOHTO_MET_STEVENS_EON");
        Person("Pewter_Museum", "t03r0101_daigo_2", "jru6_the_museums_second_steven");

        // ---- The night aboard the S.S. Aqua, played by its crewman
        game.Story.Unset("FLAG_JOHTO_SS_AQUA_AT_VERMILION");
        var deck = MapDatabase.Get("SSAqua_1F");
        var crewman = deck.Everyone.First(n => n.Key == "p01r0301_seaman_2_2");
        // (a tile beside him, facing him: the one south of him is the gangway's)
        var beside = new[] { (0, 1, Direction.Up), (-1, 0, Direction.Right), (1, 0, Direction.Left), (0, -1, Direction.Down) }
            .First(d => deck.IsWalkable(crewman.GridX + d.Item1, crewman.GridY + d.Item2));
        int cx = crewman.GridX + beside.Item1, cy = crewman.GridY + beside.Item2;
        At("SSAqua_1F", cx, cy, beside.Item3); game.ArriveOnMap(); Frames(10);
        ReadOn(() => false, 300);
        game.Player.Facing = beside.Item3; Frames(2);
        Console.WriteLine($"  crewman at {crewman.GridX},{crewman.GridY}; player {cx},{cy} facing {beside.Item3}; script {game.ScriptNow}; state {game.State}; dialogue {game.Dialogue.IsActive}");
        game.Interact();
        ReadOn(() => game.Map.Name == "SSAqua_1F_SouthwestRooms" && game.Dialogue.IsActive && game.Dialogue.IsCurrentLineComplete);
        Shot("jru7_the_cabin_at_night");
        Console.WriteLine($"  night: {game.Map.Name} {game.Player.GridX},{game.Player.GridY}");
        ReadOn(() => game.Map.Name == "SSAqua_1F" && !engine.ScriptRunning);
        Frames(30);
        Shot("jru8_the_deck_in_the_morning");
        Console.WriteLine($"  morning: {game.Map.Name} {game.Player.GridX},{game.Player.GridY}, at Vermilion {game.Story.Has("FLAG_JOHTO_SS_AQUA_AT_VERMILION")}");

        // ---- An album page of each scene
        game.Photos.Clear();
        var lead = game.Party.Members.FirstOrDefault(p => !p.IsEgg);
        string[] places = { "pallet", "johto_route_1", "ilex_forest", "route_21", "mount_silver", "johto_seafoam_islands_b1f", "celadon_department_store_2f" };
        foreach (var place in places) game.Photos.Add(PhotoAlbum.Of(place, lead, "Boy", GameClock.FixedDate ?? new DateTime(2026, 6, 1)));
        var studio = MapDatabase.Get("Cianwood_PhotoStudio");
        At("Cianwood_PhotoStudio", studio.Width / 2, studio.Height - 3, Direction.Up); game.ArriveOnMap(); Frames(10);
        game.OpenPhotoAlbum();
        Frames(240);
        for (int i = 0; i < places.Length; i++)
        {
            Shot($"jru9{i}_album_{game.PhotoAlbumScreen.Scene?.Setting}");
            Console.WriteLine($"  album page {i}: {places[i]} -> {game.PhotoAlbumScreen.Scene?.Setting}");
            game.PhotoAlbumScreen.Turn(1); Frames(40);
        }
        game.PhotoAlbumScreen.Close(); Frames(4);
        game.State = GameState.Overworld;

        // ---- Cameron at some of his places
        game.Story.Set("FLAG_JOHTO_MET_CAMERONS_FATHER");
        Person("Johto", "t20_gsmiddleman1", "jrua_cameron_new_bark");
        Person("Johto", "t31_gsmiddleman1", "jrub_cameron_mount_silver");
        Person("Johto", "w21_gsmiddleman1", "jruc_cameron_route_21");
        Person("IlexForest", "d36r0101_gsmiddleman1", "jrud_cameron_ilex_forest");

        TheRooms();
    }

    /// <summary>The imported rooms of Johto part u looks at (plan 20 · J15, part u).</summary>
    static readonly string[] RoomsToLookAt =
    {
        "Route22_LeagueReceptionGate", "Saffron_MagnetTrainStation", "Goldenrod_MagnetTrainStation", "Goldenrod_DeptStore_Basement",
        "Celadon_DeptStore_1F", "Celadon_DeptStore_2F", "Celadon_DeptStore_3F", "Celadon_DeptStore_4F", "Celadon_DeptStore_5F",
        "Celadon_DeptStore_Roof", "Goldenrod_DeptStore_1F", "Goldenrod_DeptStore_2F", "Goldenrod_DeptStore_6F",
        "Celadon_Condominiums_1F", "Celadon_Condominiums_3F", "Celadon_Condominiums_Roof",
        "Goldenrod_RadioTower_1F", "Goldenrod_RadioTower_3F", "Goldenrod_RadioTower_4F", "Goldenrod_RadioTower_5F",
        "Goldenrod_RadioTower_ObservationDeck", "Lavender_RadioTower", "Lavender_HouseOfMemories",
        "IndigoLeague_PokemonCenter_1F", "Azalea_PokemonCenter_1F", "Azalea_KurtsHouse", "Azalea_GymEntrance",
        "Pewter_Museum", "PokeathlonDome_1F", "PokeathlonDome_2F", "PokeathlonDome_B1F_Trust", "Route10_PowerPlant",
        "SafariZone_Gate", "Fuchsia_PalParkGate", "Route5_UndergroundPath", "TeamRocketHQ_B1F", "TeamRocketHQ_B2F", "TeamRocketHQ_B3F",
        "SSAqua_1F_SouthwestRooms", "SSAqua_1F_NortheastRooms", "Viridian_TrainerHouse_1F", "Viridian_TrainerHouse_B1F",
        "Vermilion_FanClub", "Olivine_PortHall", "OlivineLighthouse_LightRoom", "OlivineLighthouse_Ledge",
        "Ecruteak_BellTowerGate", "Ecruteak_BellTowerCorridor", "Ecruteak_DanceTheater", "Route39_MoomooFarmStable",
        "Goldenrod_BikeShop", "Route35_NationalParkGate", "Route26_WeekSiblingsHouse", "Saffron_SilphCo_RotomRoom",
        "CeruleanCape_SeaCottage", "Goldenrod_FlowerShop", "Route2_ForestNorthGate",
    };
}
