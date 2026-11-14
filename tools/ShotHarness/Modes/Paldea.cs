using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Core.Autoplay;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;

partial class Harness
{
    /// <summary>
    /// Paldea's first places and chapter on the open world (plan 34 · PD1, its second session; not part of <c>all</c>;
    /// <c>paldea</c>): a new game in Paldea, the player put down on the markers of Cabo Poco (<c>pd00</c> to <c>pd06</c>:
    /// the lane from four yaws, the player's house, the president's house and his terrace over the sea), Poco Path
    /// (<c>pd10</c> to <c>pd13</c>: its south end, the lighthouse, the beach and the grotto's mouth), Los Platos
    /// (<c>pd30</c>, <c>pd31</c>), the South Province's fields with their Pokémon (<c>pd40</c>, <c>pd41</c>) and Mesagoza
    /// (<c>pd50</c> to <c>pd54</c>: the gate, the plaza, the stairs' foot, up the stairs and the academy's door), the six
    /// rooms (<c>pd20</c> Inlet Grotto, <c>pd60a</c> to <c>pd60f</c>), the same after dark (<c>pd*n</c>), and last the
    /// chapter played by the playthrough bot on <see cref="Itineraries.PaldeaToTheAcademy"/> in a new game of its own,
    /// shot at each scene (<c>pds00</c> the bedroom to <c>pds10</c> the director's welcome). It prints the area each shot
    /// stands in, the wild Pokémon out in the field, and the bot's legs.
    /// </summary>
    public void PaldeaMode()
    {
        engine.NewGameRegion = "Paldea";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Sprigatito")!, 12));
        FieldRigs.Patient = true;
        Frames(10);
        var paldea = engine.OpenMapNamed("Paldea");
        if (paldea == null) { Console.WriteLine("!! no open map called Paldea"); return; }

        PaldeaPlaces(paldea, "");

        // After dark: the same places at night, a few of them
        var hour = engine.Settings.TimeOfDay;
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        PaldeaPlaces(paldea, "n");
        engine.Settings.TimeOfDay = hour;
        engine.ApplySettings(window: false);

        // The rooms, each from inside its way in
        foreach (var (shot, room) in new[]
        {
            ("pd20_inlet_grotto", "InletGrotto"),
            ("pd60a_players_house_1f", "CaboPoco_PlayersHouse_1F"),
            ("pd60b_players_house_2f", "CaboPoco_PlayersHouse_2F"),
            ("pd60c_nemonas_house", "CaboPoco_NemonasHouse"),
            ("pd60d_los_platos_center", "LosPlatos_PokemonCenter"),
            ("pd60e_academy_hall", "Mesagoza_AcademyHall"),
        })
            ShootRoom(shot, room);
        if (Wanted("pd60f_bedroom"))
        {
            var start = RegionDatabase.Get(RegionDatabase.Paldea)!.Start!;
            game.Place(MapDatabase.Get(start.Map), start.X, start.Y, Direction.Down);
            game.LocationSign.Hide();
            Frames(40);
            Shot("pd60f_bedroom");
            Console.WriteLine($"  pd60f_bedroom: {start.Map} at {start.X},{start.Y}");
        }

        PaldeaProvinces(paldea);
        PaldeaLegends(paldea);
        PaldeaStarfall(paldea);

        PaldeaChapter();
        PaldeaLegendsPlayed();
        PaldeaLetsGoPlayed();
    }

    /// <summary>
    /// The South and East Provinces (plan 34 · PD3): Mesagoza's two gates, the three ways and the three towns from
    /// their markers, each gym faced from its square, and the nine rooms: the Pokémon Centers, the gyms' lobbies and their
    /// tests, and each leader at the far end of the test with the three in the way gone (<c>pd70</c> to <c>pd85</c>). It
    /// prints the area each stands in.
    /// </summary>
    void PaldeaProvinces(OpenMap paldea)
    {
        ShootAt(paldea, "pd70_mesagoza_west_gate", "area_two_east");
        ShootAt(paldea, "pd71_area_two", "area_two_west");
        ShootAt(paldea, "pd72_cortondo", "cortondo_east");
        ShootToward(paldea, "pd73_cortondo_gym", "cortondo_square", -416, -374, 20);
        ShootAt(paldea, "pd74_mesagoza_east_gate", "area_three_west");
        ShootAt(paldea, "pd75_artazon", "artazon_west");
        ShootToward(paldea, "pd76_artazon_gym", "artazon_square", 416, -374, 20);
        ShootAt(paldea, "pd77_east_province", "east_one_south");
        ShootAt(paldea, "pd78_levincia", "levincia_south");
        ShootToward(paldea, "pd79_levincia_gym", "levincia_plaza", 496, -788, 24);
        foreach (var (shot, room) in new[]
        {
            ("pd80a_cortondo_center", "Cortondo_PokemonCenter"),
            ("pd80b_cortondo_gym", "Cortondo_Gym"),
            ("pd80c_cortondo_test", "Cortondo_GymTest"),
            ("pd81a_artazon_center", "Artazon_PokemonCenter"),
            ("pd81b_artazon_gym", "Artazon_Gym"),
            ("pd81c_artazon_test", "Artazon_GymTest"),
            ("pd82a_levincia_center", "Levincia_PokemonCenter"),
            ("pd82b_levincia_gym", "Levincia_Gym"),
            ("pd82c_levincia_test", "Levincia_GymTest"),
        })
            ShootRoom(shot, room);
        foreach (var (shot, room, town) in new[] { ("pd83_katy", "Cortondo_GymTest", "CORTONDO"), ("pd84_brassius", "Artazon_GymTest", "ARTAZON"), ("pd85_iono", "Levincia_GymTest", "LEVINCIA") })
        {
            if (!Wanted(shot)) continue;
            for (int n = 1; n <= 3; n++) game.Story.Set($"FLAG_PALDEA_{town}_TEST_{n}");
            var map = MapDatabase.Get(room);
            game.Place(map, 6, 6, Direction.Up);
            game.LocationSign.Hide();
            Frames(40);
            Shot(shot);
            Console.WriteLine($"  {shot}: {room} at 6,6, {map.NPCs.Count} people: {string.Join(", ", map.NPCs.Select(n => n.Name))}");
        }
    }

    /// <summary>Paldea's places by their markers (<paramref name="night"/> "n" for the shots after dark, which take only some of them).</summary>
    void PaldeaPlaces(OpenMap paldea, string night)
    {
        bool dark = night.Length > 0;
        // Cabo Poco's lane from four yaws
        string[] ways = { "south", "east", "north", "west" };
        if (paldea.Marker("cabo_poco_lane") is { } lane)
            for (int k = 0; k < 4; k++)
            {
                if (dark && k > 0) break;
                string shot = $"pd0{k}_cabo_poco_{ways[k]}{night}";
                if (!Wanted(shot)) continue;
                PutInLab(paldea, lane.Pos, k * 90, k * 90);
                game.LocationSign.Hide();
                Frames(40);
                Shot(shot);
                Console.WriteLine($"  {shot}: cabo_poco_lane at {lane.Pos}, the camera {k * 90}°, {paldea.AreaAt(lane.Pos)?.Key ?? "no area"}");
            }
        // The houses and the terrace, each faced from its marker's side
        if (!dark)
        {
            ShootToward(paldea, "pd04_players_house", "players_door", -18, 1, 14);
            ShootToward(paldea, "pd05_presidents_house", "nemona_door", 31, 9, 15);
        }
        // The terrace looking south over the cliff to the sea (its marker faces the house)
        if (Wanted($"pd06_presidents_terrace{night}") && paldea.Marker("terrace") is { } terrace)
        {
            string shot = $"pd06_presidents_terrace{night}";
            PutInLab(paldea, terrace.Pos, 0, 0);
            game.LocationSign.Hide();
            Frames(40);
            Shot(shot);
            Console.WriteLine($"  {shot}: terrace at {terrace.Pos}, facing south over the sea, {paldea.AreaAt(terrace.Pos)?.Key ?? "no area"}");
        }
        if (!dark)
        {
            ShootAt(paldea, "pd10_poco_path", "poco_path_south");
            ShootToward(paldea, "pd11_lighthouse", "lighthouse_foot", 60, -92, 22);
            ShootAt(paldea, "pd12_beach", "beach_foot");
            // The grotto's door in the foot of the cliff, faced from the beach
            ShootToward(paldea, "pd13_grotto_mouth", "grotto_door", -55, -80, 12);
        }
        ShootAt(paldea, $"pd30_los_platos{night}", "los_platos_south");
        if (!dark)
        {
            ShootToward(paldea, "pd31_los_platos_center", "center_door", 14, -185, 16);
            // The fields, with time for their Pokémon to come out round the player
            foreach (var (shot, marker) in new[] { ("pd40_south_province", "area_one_south"), ("pd41_south_province_north", "area_one_north") })
            {
                if (!Wanted(shot) || paldea.Marker(marker) is not { } m) continue;
                PutInOpenMap(paldea, m.Pos, m.Yaw);
                game.LocationSign.Hide();
                Frames(360);
                // (they come out where the player isn't looking, so the camera is turned to the nearest)
                var wild = game.OpenField?.Wild;
                if (wild?.All.OrderBy(w => Math.Pow(w.Pos.X - m.Pos.X, 2) + Math.Pow(w.Pos.Z - m.Pos.Z, 2)).FirstOrDefault() is { } near)
                {
                    game.OpenCamera.Face(Math.Atan2(near.Pos.X - m.Pos.X, near.Pos.Z - m.Pos.Z));
                    game.SettleOpenView();
                    Frames(20);
                }
                Shot(shot);
                Console.WriteLine($"  {shot}: {marker} at {m.Pos}, {paldea.AreaAt(m.Pos)?.Key ?? "no area"}, {(paldea.ZoneAt(m.Pos)?.Name ?? "no zone")}, "
                    + $"wild out {wild?.All.Count ?? 0}: {string.Join(", ", wild?.All.Select(w => w.Species) ?? Array.Empty<string>())}");
            }
            ShootAt(paldea, "pd50_mesagoza_gate", "mesagoza_south");
        }
        ShootAt(paldea, $"pd51_mesagoza_plaza{night}", "mesagoza_plaza");
        if (!dark)
        {
            ShootAt(paldea, "pd52_stairs_foot", "stairs_foot");
            // Up the great stairs from the plaza, and the academy from before its door
            if (Wanted("pd53_up_the_stairs"))
            {
                var half = OnLabGround(paldea, 0, -340, 40, 0);
                PutInLab(paldea, half, 180, 180);
                game.LocationSign.Hide();
                Frames(40);
                Shot("pd53_up_the_stairs");
                Console.WriteLine($"  pd53_up_the_stairs: half way up at {half}, {paldea.AreaAt(half)?.Key ?? "no area"}");
            }
            ShootToward(paldea, "pd54_academy_door", "academy_door", 0, -367, 22);
        }
        else ShootToward(paldea, "pd54_academy_door_n", "academy_door", 0, -367, 22);
    }

    /// <summary>A grid room of the region shot from the tile inside its first way out, facing in.</summary>
    void ShootRoom(string shot, string room)
    {
        if (!Wanted(shot)) return;
        var map = MapDatabase.Get(room);
        if (map.Name != room) { Console.WriteLine($"!! no room called {room}"); return; }
        var warp = map.Warps.FirstOrDefault();
        int x = warp?.SourceX ?? map.Width / 2, y = warp?.SourceY ?? map.Height / 2;
        // the tile beside the way out that is open, the way in from the door
        foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (1, 0), (-1, 0) })
            if (map.IsWalkable(x + dx, y + dy)) { x += dx; y += dy; break; }
        game.Place(map, x, y, Direction.Up);
        game.LocationSign.Hide();
        Frames(40);
        Shot(shot);
        Console.WriteLine($"  {shot}: {room} at {x},{y} ({map.Width}×{map.Height}), {map.NPCs.Count} people");
    }

    /// <summary>
    /// The chapter in a new game of its own, played by the bot on <see cref="Itineraries.PaldeaToTheAcademy"/>, a shot at
    /// each of its scenes as the bot comes to it: the first line of each scene, the starters' briefcase, Nemona's battle
    /// at its menu and the grotto's question.
    /// </summary>
    void PaldeaChapter()
    {
        if (!Enumerable.Range(0, 11).Any(n => Wanted($"pds{n:00}"))) return;
        engine.NewGameRegion = "Paldea";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(40);
        if (Wanted("pds00")) { Shot("pds00_bedroom"); Console.WriteLine($"  pds00_bedroom: {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}"); }
        game.Flow.EncounterAids.RepelSteps = 1_000_000;

        // Each scene's shot by the scene it comes in (its name as the script's `scene` gives it; an arrival's leg is done as
        // the door is come through and a student may see the player on the way, so the leg alone can't say): (scene, what
        // is on screen, the shot)
        var wanted = new List<(string Scene, string What, string Shot)>
        {
            ("The director comes to call", "line", "pds01_mom_and_the_director"),
            ("The three on the terrace", "line", "pds02_the_three_on_the_terrace"),
            ("The three on the terrace", "starters", "pds03_the_briefcase"),
            ("The three on the terrace", "battle", "pds04_nemonas_battle"),
            ("A roar on the beach", "line", "pds05_a_roar_on_the_beach"),
            ("The two in the grotto", "line", "pds06_the_two_in_the_grotto"),
            ("The two in the grotto", "question", "pds07_the_grottos_question"),
            ("Nemona at the lighthouse", "line", "pds08_nemonas_lesson"),
            ("Nemona at Mesagoza's gate", "line", "pds09_nemona_at_the_gate"),
            ("The director's welcome", "line", "pds10_the_directors_welcome"),
        };
        var taken = new HashSet<string>();
        var bot = new Autoplayer(engine, seed: 1) { Itinerary = new Itinerary(Itineraries.PaldeaToTheAcademy) };
        bool strengthened = false;
        int battleFrames = 0;
        for (int f = 0; f < 60 * 60 * 30 && !bot.Itinerary.Finished && !bot.Stopped; f++)
        {
            bot.Frame(1f / 60f);
            if (f % 6 == 0) Frames(1);
            int leg = bot.Itinerary.Index;
            // A strong Pokémon behind the starter once it is chosen, so Nemona's lesson and the students are the story's to be got through
            if (!strengthened && leg >= 2 && game.State == GameState.Overworld)
            {
                strengthened = true;
                game.Party.Add(new Pokemon(PokemonDatabase.Get("Pawmot")!, 40));
            }
            battleFrames = game.State == GameState.Battle ? battleFrames + 1 : 0;
            foreach (var (scene, what, shot) in wanted)
            {
                if (taken.Contains(shot)) continue;
                // (a battle's scene isn't the running scene while the battle has the screen: Nemona's is the first battle, on the terrace's leg)
                if (what == "battle" ? leg != 1 : game.Scene is not { } running || !running.Contains(scene, StringComparison.Ordinal)) continue;
                bool now = what switch
                {
                    "line" => game.Dialogue.IsActive && !game.Dialogue.IsQuestion && game.State != GameState.Battle,
                    "question" => game.Dialogue.IsActive && game.Dialogue.IsQuestion,
                    "starters" => game.State == GameState.StarterSelect,
                    "battle" => battleFrames == 1,
                    _ => false
                };
                if (!now) continue;
                taken.Add(shot);
                InputManager.Feed(null);
                // (a battle is left to come in by itself, the bot's hands off, to its menu)
                if (what == "battle") { if (game.Battle is { } b) ToMainMenu(b); }
                else Frames(what == "line" ? 50 : what == "question" ? 90 : 20);
                if (what == "line" && game.Dialogue.IsActive) game.Dialogue.FinishLine();
                Frames(2);
                if (Wanted(shot))
                {
                    Shot(shot);
                    Console.WriteLine($"  {shot}: leg {leg} ({bot.Itinerary.Current}), {game.Map.Name}{(engine.InOpenField ? $", open at {game.OpenPlayer?.Pos}" : $" at {game.Player.GridX},{game.Player.GridY}")}, "
                        + $"state {game.State}, VAR_PALDEA_STORY {game.Story.Var("VAR_PALDEA_STORY")}");
                }
            }
        }
        for (int f = 0; f < 600 && engine.ScriptRunning; f++) bot.Frame(1f / 60f);
        bot.Release();
        Console.WriteLine($"  the bot: legs {string.Join(", ", bot.Report.Legs)}{(bot.Report.LegsGivenUp.Count > 0 ? $"; !! given up {string.Join(", ", bot.Report.LegsGivenUp)}" : "")}, "
            + $"{bot.Report.Seconds:0.0} s; VAR_PALDEA_STORY {game.Story.Var("VAR_PALDEA_STORY")}, the Tera Orb {game.Bag.GetQuantity(ItemDatabase.Get("Tera Orb")!)}");
        foreach (var (_, _, shot) in wanted)
            if (!taken.Contains(shot)) Console.WriteLine($"  !! {shot} never came");
    }

    /// <summary>
    /// `profile`'s Mesagoza scene (plan 34 · PD1): the plaza of Paldea's open map from its marker, the great stairs and the
    /// academy ahead and the plaza's shops round, by day, the busiest of PD1's places. Run with every preset unless
    /// SHOTS_ONLY names other scenes, as the open lab's are (SHOTS_ONLY=mesagoza for it alone); the player is put back
    /// where they stood afterwards.
    /// </summary>
    void MesagozaProfile()
    {
        const string label = "mesagoza_plaza";
        var only = (Environment.GetEnvironmentVariable("SHOTS_ONLY") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        bool look = Environment.GetEnvironmentVariable("SHOTS_LOOK") == "1";
        if (only.Length > 0 && !only.Any(word => label.Contains(word, StringComparison.OrdinalIgnoreCase)) && !look) return;
        // Paldea's open map read from the game's own bake, added beside whatever the profile's game has
        var paldea = engine.OpenMapNamed("Paldea") ?? OpenRegions.Load(RegionDatabase.Get(RegionDatabase.Paldea)!).SingleOrDefault();
        if (paldea == null || paldea.Marker("mesagoza_plaza") is not { } plaza) { Console.WriteLine("!! no Mesagoza plaza to time"); return; }
        if (engine.OpenMapNamed("Paldea") == null) game.AddOpenMap(paldea);
        var (stood, x, y, facing) = (game.Map, game.Player.GridX, game.Player.GridY, game.Player.Facing);
        game.OpenWeather = null;
        PutInLab(paldea, plaza.Pos, 180, 180);
        Profile("mesagoza_plaza: Mesagoza's plaza toward the academy, by day");
        game.Place(stood, x, y, facing);
    }
}
