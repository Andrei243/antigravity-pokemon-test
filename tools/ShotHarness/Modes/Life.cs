partial class Harness
{
    // ---------------------------------------------------------------- life (plan 04 · G9)

    // What moves in the field: prints in sand and snow, dust behind a run and under a hop, leaves out of tall grass,
    // rings behind a swimmer, a door opening and shutting, the bubbles over heads, every weather, the camera sent
    // to look elsewhere, and the three ways into a battle caught as they close and open.
    public void LifeMode()
    {
        var walker = game.Player;
        var sinnoh = MapDatabase.Get("Sinnoh");
        void Put(int x, int y, Direction facing, TravelMode travel = TravelMode.OnFoot)
        {
            game.Map = sinnoh;
            walker.SetPosition(x, y, facing);
            walker.SetMode(travel);
            game.State = GameState.Overworld;
            engine.Steering = (null, false);
            Frames(3);
        }
        // Walks the player with the game's own steps, so each one leaves what a step leaves
        void Walk(Direction way, int tiles, bool run = false)
        {
            int fromX = walker.GridX, fromY = walker.GridY;
            engine.Steering = (way, run);
            for (int guard = 0; guard < 900 && (Math.Abs(walker.GridX - fromX) + Math.Abs(walker.GridY - fromY) < tiles || walker.IsMoving); guard++) Frames(1);
            engine.Steering = (null, false);
        }
        (int X, int Y) Nearest(int x, int y, Func<int, int, bool> wanted)
        {
            for (int reach = 0; reach < 24; reach++)
                for (int dy = -reach; dy <= reach; dy++)
                    for (int dx = -reach; dx <= reach; dx++)
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == reach && sinnoh.InBounds(x + dx, y + dy) && wanted(x + dx, y + dy)) return (x + dx, y + dy);
            throw new InvalidOperationException($"Nothing of the kind near ({x}, {y})");
        }

        // Prints along Route 219's beach, and in the snow before the rival's door
        var (sandX, sandY) = Nearest(184, 853, (x, y) => Enumerable.Range(0, 6).All(i => sinnoh.BehaviourAt(x + i, y) == TileBehavior.Sand && sinnoh.IsWalkable(x + i, y)));
        Put(sandX, sandY, Direction.Right); Walk(Direction.Right, 5); Shot("l01_prints_in_sand");
        ShotCrop("l01b_prints_native", 560, 380, 640, 360, 2);
        var (snowX, snowY) = Nearest(105, 877, (x, y) => Enumerable.Range(0, 4).All(i => TileBehaviors.KeepsFootprints(sinnoh.BehaviourAt(x + i, y)) && sinnoh.BehaviourAt(x + i, y) != TileBehavior.Sand && sinnoh.IsWalkable(x + i, y)));
        Put(snowX, snowY, Direction.Right); Walk(Direction.Right, 3); Shot("l02_prints_in_snow");

        // Dust behind a run up Twinleaf's road, caught two frames after a step
        Put(112, 884, Direction.Up); Walk(Direction.Up, 4, run: true); Frames(2); Shot("l03_dust_running");
        ShotCrop("l03b_dust_native", 640, 400, 640, 360, 2);

        // Leaves out of the tall grass of Route 201
        var (grassX, grassY) = Nearest(110, 850, (x, y) => sinnoh.IsTallGrass(x, y) && sinnoh.IsTallGrass(x + 1, y) && sinnoh.IsTallGrass(x + 2, y));
        Put(grassX, grassY, Direction.Right);
        engine.Steering = (Direction.Right, false);
        for (int guard = 0; guard < 60 && walker.GridX == grassX; guard++) Frames(1);
        engine.Steering = (null, false);
        Frames(5);
        if (game.State == GameState.Overworld) { Shot("l04_leaves_from_grass"); ShotCrop("l04b_leaves_native", 640, 380, 640, 360, 2); }
        game.State = GameState.Overworld;

        // A hop over a ledge of Route 201 onto bare ground: the dust where it lands
        Frames(40);
        var (ledgeX, ledgeY) = Nearest(120, 852, (x, y) => sinnoh.GetGroundTile(x, y) == TileType.LedgeDown && sinnoh.IsWalkable(x, y - 1) && sinnoh.IsWalkable(x, y + 1) && !sinnoh.IsTallGrass(x, y + 1));
        Put(ledgeX, ledgeY - 1, Direction.Down); Walk(Direction.Down, 2); Frames(2); Shot("l05_dust_under_a_hop");
        ShotCrop("l05b_hop_native", 640, 380, 640, 360, 2);

        // Rings behind a swimmer on Twinleaf's pond
        var (pondX, pondY) = Nearest(111, 892, (x, y) => Enumerable.Range(0, 4).All(i => sinnoh.IsDeepWater(x + i, y) && !sinnoh.IsSolid(x + i, y)));
        Put(pondX, pondY, Direction.Right, TravelMode.Surfing); Walk(Direction.Right, 3); Frames(4); Shot("l06_rings_behind_a_swimmer");
        ShotCrop("l06b_rings_native", 560, 380, 640, 360, 2);
        walker.SetMode(TravelMode.OnFoot);

        // Riding out onto the pond from its bank: the splash where the Pokémon lands. The ride begins as the game's
        // does (plan 02 · S2): the water is faced and looked at, Surf's question (asked only of someone with the Fen
        // Badge) is answered yes, and the move's line and cut-in are seen through
        var swimmer = game.Party.Members[0];
        var surf = new Move(MoveDatabase.Get("Surf")!);
        swimmer.Moves.Add(surf);
        var story = game.Story;
        int badges = story.BadgeMask("Sinnoh");
        story.GiveBadge("Sinnoh", "Fen");
        var (bankX, bankY) = Nearest(111, 892, (x, y) => sinnoh.IsWalkable(x, y) && sinnoh.IsDeepWater(x + 1, y) && sinnoh.IsDeepWater(x + 2, y) && !sinnoh.IsSolid(x + 1, y));
        Put(bankX, bankY, Direction.Right);
        game.Interact();
        var text = game.Dialogue;
        for (int guard = 0; guard < 120 && !engine.Choice.IsOpen; guard++) { text.FinishLine(); Frames(1); }
        engine.Choice.Confirm();
        for (int guard = 0; guard < 600 && (walker.Mode != TravelMode.Surfing || walker.IsMoving); guard++)
        {
            text.FinishLine(); text.Advance();
            Frames(1);
        }
        if (walker.Mode != TravelMode.Surfing || engine.ScriptRunning) Console.WriteLine("  !! never happened: the ride out onto the pond");
        Frames(5); Shot("l06c_splash_riding_out"); ShotCrop("l06d_splash_native", 640, 380, 640, 360, 2);
        // On the water, the player rides the Pokémon whose Surf it was (plan 10 · F1)
        Walk(Direction.Right, 1); Frames(4); Shot("l06e_surfer_on_its_own_pokemon"); ShotCrop("l06f_surfer_native", 640, 380, 640, 360, 2);
        swimmer.Moves.Remove(surf);
        story.SetBadges("Sinnoh", badges);
        walker.SetMode(TravelMode.OnFoot);

        // A door: ajar as the step toward it begins, open as it ends, then from inside out again and shut behind
        void ThroughDoor(string name, int warpX, int warpY)
        {
            Put(warpX, warpY + 2, Direction.Up); Walk(Direction.Up, 1);
            engine.Steering = (Direction.Up, false);
            Frames(3); ShotCrop($"{name}_1_ajar", 640, 240, 640, 360, 2);
            Frames(6); ShotCrop($"{name}_2_open", 640, 240, 640, 360, 2);
            for (int guard = 0; guard < 120 && game.Map.Name == "Sinnoh"; guard++) Frames(1);
            engine.Steering = (null, false);
            Frames(40);
            Console.WriteLine($"{name}: through the door into " + game.Map.Name);
            engine.Steering = (Direction.Down, false);
            for (int guard = 0; guard < 400 && game.Map.Name != "Sinnoh"; guard++) Frames(1);
            engine.Steering = (null, false);
            Frames(26); ShotCrop($"{name}_3_open_behind", 640, 240, 640, 360, 2);
            Frames(60); ShotCrop($"{name}_4_shut_again", 640, 240, 640, 360, 2);
        }
        ThroughDoor("l07_house_door", 116, 885);
        // The glass doors of Sandgem's Pokémon Center slide apart
        var center = MapStructures.BuildingsOf(sinnoh).First(b => b.Kind == BuildingKind.PokemonCenter && !b.Annex && sinnoh.AreaAt(b.X0, b.Y0)?.Key == "sandgem_town"
            && b.Doors.Exists(d => sinnoh.GetWarpAt(d.X, b.Y1) != null || sinnoh.GetWarpAt(d.X, b.Y1 + 1) != null));
        int centerDoor = center.Doors[0].X;
        ThroughDoor("l08_center_door", centerDoor, sinnoh.GetWarpAt(centerDoor, center.Y1) != null ? center.Y1 : center.Y1 + 1);
        // The player's door after dark: the room's light in the doorway
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        ThroughDoor("l09_house_door_at_night", 116, 885);
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // Every bubble, over a row of people and the player
        game.Map = BuildFocusLineup();
        walker.SetPosition(10, 7, Direction.Down);
        Frames(3);
        var row = game.Map.NPCs;
        var bubbles = new[] { EmoteBubble.Exclaim, EmoteBubble.Question, EmoteBubble.Dots, EmoteBubble.Note, EmoteBubble.Heart, EmoteBubble.Sleep, EmoteBubble.Sweat };
        for (int i = 0; i < bubbles.Length; i++) engine.ShowEmote(row[i].Name, bubbles[i], 5f);
        engine.ShowEmote(null, EmoteBubble.Exclaim, 5f);
        Frames(1); ShotCrop("l10_bubble_popping", 560, 240, 800, 360, 2);
        Frames(12); Shot("l11_bubbles"); ShotCrop("l11b_bubbles_native", 420, 250, 1000, 300, 2);
        engine.ShowEmote(null, EmoteBubble.None, 0f);

        // Every weather over Twinleaf Town, and rain after dark
        Put(112, 880, Direction.Down);
        var town = sinnoh.AreaAt(112, 880)!;
        foreach (var (name, kind) in new[]
                 {
                     ("l12_rain", FieldWeather.Rain), ("l13_snow", FieldWeather.Snow), ("l14_heavy_snow", FieldWeather.HeavySnow),
                     ("l15_fog", FieldWeather.Fog), ("l16_sandstorm", FieldWeather.Sandstorm), ("l17_ash", FieldWeather.Ash),
                     ("l17b_cloudy", FieldWeather.Cloudy), ("l17c_heavy_rain", FieldWeather.HeavyRain), ("l17d_hail", FieldWeather.Hail),
                     ("l17e_blizzard", FieldWeather.Blizzard)
                 })
        {
            town.Weather = kind;
            Frames(20); Shot(name);
        }
        // A thunderstorm, caught in the first flash of its lightning (the harness's frames are a sixtieth of a second)
        town.Weather = FieldWeather.Thunderstorm;
        for (int guard = 0; guard < 720 && !engine.LightningNow; guard++) Frames(1);
        Shot("l17f_thunderstorm_lightning");
        Frames(30); Shot("l17g_thunderstorm");
        town.Weather = FieldWeather.Rain;
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Frames(4); Shot("l18_rain_at_night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        // Rain on the pond, seen from its north bank: rings where it lands on water, flecks on the ground
        var (shoreX, shoreY) = Nearest(111, 889, (x, y) => sinnoh.IsWalkable(x, y) && sinnoh.IsDeepWater(x, y + 1) && sinnoh.IsDeepWater(x, y + 2) && sinnoh.IsDeepWater(x + 1, y + 1));
        Put(shoreX, shoreY, Direction.Down);
        Frames(40); Shot("l19_rain_on_the_pond"); ShotCrop("l19b_rain_native", 640, 420, 640, 360, 2);
        Put(112, 880, Direction.Down);
        Timing("rain");
        town.Weather = FieldWeather.HeavySnow;
        Timing("heavy snow");
        town.Weather = FieldWeather.Fog;
        Timing("fog");
        town.Weather = FieldWeather.Clear;

        // The camera sent to look at the rival's house, half way and there, and back
        Put(112, 880, Direction.Down);
        engine.PanCamera(105, 876, 1f);
        Frames(30); Shot("l20_camera_half_way");
        Frames(40); Shot("l21_camera_there");
        engine.ReleaseCamera(0.5f);
        Frames(40);

        // Into battle: each way of closing caught in its flash, half closed and nearly shut, and the opening on the battle
        foreach (var (name, kind) in new[]
                 {
                     ("wild", TransitionKind.Wild), ("wild_strong", TransitionKind.WildStrong), ("trainer", TransitionKind.Trainer),
                     ("trainer_strong", TransitionKind.TrainerStrong)
                 })
        {
            Put(112, 880, Direction.Down);
            game.StartTransition(GameState.Overworld, null, kind);
            Frames(2); Shot($"l30_{name}_1_flash");
            Frames(36); Shot($"l30_{name}_2_closing");
            Frames(10); Shot($"l30_{name}_3_nearly_shut");
            Frames(18); Shot($"l30_{name}_4_opening");
            Frames(40);
        }
        // (The diamonds that stood in for a Gym Leader's way went with the bands of plan 09 · L15, which are shot at
        // the mode's end. The frames their four shots took are still spent here, on a trainer's way, so that the clock
        // of every later shot stands where it did.)
        Put(112, 880, Direction.Down);
        game.StartTransition(GameState.Overworld, null, TransitionKind.Trainer);
        Frames(2 + 36 + 10 + 18 + 40);
        // And the real thing: a wild Pokémon in the grass, from the flash to the battle's first frame
        Put(grassX, grassY, Direction.Right);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 3, MaxLevel = 3, Weight = 1 });
        Frames(40); Shot("l31_into_battle_closing");
        Frames(26); Shot("l32_into_battle_opening");
        Frames(40); Shot("l33_battle_begins");
        game.State = GameState.Overworld;

        // A waterfall of the terrain lab, twice an eighth of a second apart: the sheet has moved four texels down
        game.Map = BuildTerrainLab();
        walker.SetPosition(20, 15, Direction.Up);
        walker.SetMode(TravelMode.OnFoot);
        Frames(4); ShotCrop("l40_waterfall_a", 760, 100, 640, 360, 2);
        Frames(8); ShotCrop("l40_waterfall_b", 760, 100, 640, 360, 2);

        // A fountain playing and a turbine turning, on a lawn made for them: their four frames, a sixth of a second apart
        var yard = new Map(22, 14) { Name = "Yard", DisplayName = "Yard" };
        for (int x = 0; x < 22; x++) { yard.SetGroundTile(x, 0, TileType.Tree, true); yard.SetGroundTile(x, 13, TileType.Tree, true); }
        for (int y = 0; y < 14; y++) { yard.SetGroundTile(0, y, TileType.Tree, true); yard.SetGroundTile(21, y, TileType.Tree, true); }
        yard.Props.Add(new Prop { Type = PropType.Fountain, X = 5, Y = 5, Width = 4, Depth = 3 });
        yard.Props.Add(new Prop { Type = PropType.WindTurbine, X = 13, Y = 6, Width = 2, Depth = 2, Height = 5f });
        game.Map = yard;
        walker.SetPosition(10, 8, Direction.Up);
        Frames(5);
        for (int frame = 0; frame < 4; frame++)
        {
            Shot($"l41_fountain_and_turbine_{frame}");
            Frames(10);
        }

        Put(112, 880, Direction.Down);
        engine.Steering = null;
        Timing("twinleaf with life");

        // The camera kit's lens (plan 09 · L4), last so that nothing before it moves: Twinleaf Town zoomed in and out,
        // where houses, lamps and trees stand at the picture's sides (a zoom changes the field of view alone, so they
        // must stand as straight as at the place's own lens, and the wider view must be whole to its edges, shadows
        // and all); the same on a row of people; and a room, where a zoom in lets the camera leave the room's middle
        var rig = game.World.Camera;
        game.World.ResetStreamingStats();
        rig.ZoomTo(1.6f, 0.5f);
        Frames(40); Shot("l50_zoom_in"); ShotCrop("l50b_zoom_in_native", 700, 300, 520, 420, 2);
        rig.ZoomTo(0.5f, 0.5f);
        Frames(40); Shot("l51_zoom_out");
        // (Sent to the town's west side as well, so the view's edge is somewhere the player's own block of chunks ends)
        rig.Pan(96.5f, 870.5f, 0.5f);
        Frames(40); Shot("l52_zoom_out_sent_west");
        Console.WriteLine($"zoomed out: zoom {rig.Zoom:0.00}, {game.World.Streaming}, {game.World.LoadedChunks} chunks loaded");
        rig.Release(0.3f);
        Frames(30);
        Console.WriteLine($"lens released: zoom {rig.Zoom:0.00}, at rest {rig.AtRest}");

        game.Map = BuildFocusLineup();
        walker.SetPosition(10, 7, Direction.Down);
        Frames(3);
        rig.ZoomTo(2f, 0f);
        rig.Pan(6.5f, 7.5f, 0f);
        Frames(3); Shot("l53_zoom_in_on_people");
        rig.ZoomTo(0.6f, 0f);
        Frames(3); Shot("l54_zoom_out_on_people");
        rig.Reset();

        var room = MapDatabase.Get("PokemonCenter");
        var pc = room.NPCs.First(n => n.IsPCTerminal);
        game.Map = room;
        walker.SetPosition(pc.GridX, pc.GridY + 1, Direction.Up);
        Frames(3); Shot("l55_room_at_its_own_lens");
        rig.ZoomTo(1.8f, 0.4f);
        Frames(30); Shot("l56_room_zoomed_in");
        rig.Release(0.2f);
        Frames(20);
        Console.WriteLine($"room: zoom {rig.Zoom:0.00}, at rest {rig.AtRest}");
        Put(112, 880, Direction.Down);

        // The ways into a battle by place and by whom (plan 09 · L13), last so that nothing before them moves: each
        // caught in its flash, in its lead where it has one, three times as its dark closes, and as it opens. Those of
        // the water are shot from the pond, those of the caves in Oreburgh Gate, the Frontier's in the Battle Tower's
        // pale lobby and the boss's on Team Galactic's fourth floor, where their marks must still be read
        void WayIn(string name, TransitionKind kind, Action stand)
        {
            stand();
            game.StartTransition(GameState.Overworld, null, kind);
            float flash = SceneTransition.FlashSeconds, lead = SceneTransition.LeadSeconds(kind), shut = SceneTransition.OutSeconds(kind) - flash - lead;
            int at = 0;
            void To(float seconds)
            {
                int frame = (int)MathF.Round(seconds * 60f);
                Frames(frame - at);
                at = frame;
            }
            To(2f / 60f); Shot($"l30_{name}_1_flash");
            if (lead > 0f) { To(flash + lead * 0.7f); Shot($"l30_{name}_2_lead"); }
            To(flash + lead + shut * 0.35f); Shot($"l30_{name}_3_closing");
            To(flash + lead + shut * 0.6f); Shot($"l30_{name}_4_closing_on");
            To(flash + lead + shut * 0.85f); Shot($"l30_{name}_5_nearly_shut");
            To(flash + lead + shut + 0.2f); Shot($"l30_{name}_6_opening");
            Frames(40);
        }
        // The open tile of a room nearest its middle
        void InRoom(string name)
        {
            var hall = MapDatabase.Get(name);
            var (x, y) = Enumerable.Range(0, hall.Width * hall.Height).Select(i => (X: i % hall.Width, Y: i / hall.Width))
                .Where(t => hall.IsWalkable(t.X, t.Y) && hall.GetWarpAt(t.X, t.Y) == null)
                .OrderBy(t => Math.Abs(t.X - hall.Width / 2) + Math.Abs(t.Y - hall.Height / 2)).First();
            At(name, x, y, Direction.Up);
            walker.SetMode(TravelMode.OnFoot);
            Frames(30);
        }
        void OnThePond() { Put(pondX, pondY, Direction.Right, TravelMode.Surfing); Frames(10); }
        void InTheCave() { At("OreburghGate1F", 16, 22, Direction.Right); walker.SetMode(TravelMode.OnFoot); Frames(40); }
        void InTheStreet() => Put(112, 880, Direction.Down);
        foreach (var (name, kind, stand) in new (string, TransitionKind, Action)[]
                 {
                     ("wild_water", TransitionKind.WildWater, OnThePond), ("wild_water_strong", TransitionKind.WildWaterStrong, OnThePond),
                     ("wild_cave", TransitionKind.WildCave, InTheCave), ("wild_cave_strong", TransitionKind.WildCaveStrong, InTheCave),
                     ("trainer_water", TransitionKind.TrainerWater, OnThePond), ("trainer_water_strong", TransitionKind.TrainerWaterStrong, OnThePond),
                     ("trainer_cave", TransitionKind.TrainerCave, InTheCave), ("trainer_cave_strong", TransitionKind.TrainerCaveStrong, InTheCave),
                     ("double", TransitionKind.Double, InTheStreet), ("frontier", TransitionKind.Frontier, () => InRoom("BattleTower")),
                     ("grunt", TransitionKind.Grunt, InTheStreet), ("galactic_boss", TransitionKind.GalacticBoss, () => InRoom("GalacticHQ4F"))
                 })
            WayIn(name, kind, stand);

        // A cave's ring closes on the player wherever the picture shows them: in a room the camera looks at the room's
        // middle, and the player stands by its PC
        game.Map = room;
        walker.SetPosition(pc.GridX, pc.GridY + 1, Direction.Up);
        walker.SetMode(TravelMode.OnFoot);
        game.State = GameState.Overworld;
        Frames(3);
        game.StartTransition(GameState.Overworld, null, TransitionKind.WildCave);
        Frames(50); Shot("l30_wild_cave_7_on_the_player_off_the_middle");
        Frames(80);

        // The real thing on water: a wild Pokémon met surfing, as l31 to l33 are in the grass
        OnThePond();
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Magikarp", MinLevel = 3, MaxLevel = 3, Weight = 1 });
        Console.WriteLine($"way in: a wild Pokémon met surfing -> {game.TransitionKind}");
        Frames(54); Shot("l34_into_battle_on_the_water_1_closing");
        Frames(30); Shot("l34_into_battle_on_the_water_2_opening");
        Frames(40); Shot("l34_into_battle_on_the_water_3_battle_begins");
        game.State = GameState.Overworld;

        // And whom the game's own battles are met as: each begun as the game begins it, and given up at once
        void Met(string who, Action begin)
        {
            begin();
            Console.WriteLine($"way in: {who} -> {game.TransitionKind}");
            game.State = GameState.Overworld;
            Frames(2);
        }
        // Someone who carries a trainer of Platinum's data, as a script's battle makes them
        NPC Carrying(string id)
        {
            var record = TrainerDatabase.Get(id) ?? throw new InvalidOperationException($"No trainer {id}");
            var trainer = new Trainer { Id = record.Id };
            TrainerDatabase.Fill(trainer, record);
            return new NPC { Name = trainer.Name, NpcType = "Trainer", TrainerData = trainer };
        }
        void Versus(string where, string id, string? second = null)
        {
            var first = Carrying(id);
            Met($"{first.TrainerData!.FullTitle}{(second != null ? " with " + Carrying(second).TrainerData!.FullTitle : "")} {where}",
                () => game.StartTrainerBattle(first, second != null ? Carrying(second) : null));
        }
        InTheCave();
        Met("a wild Pokémon in Oreburgh Gate", () => game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Geodude", MinLevel = 2, MaxLevel = 2, Weight = 1 }));
        Met("a stronger one there", () => game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Geodude", MinLevel = 60, MaxLevel = 60, Weight = 1 }));
        Versus("in Oreburgh Gate", "hiker_daniel");
        OnThePond();
        Versus("on Twinleaf's pond", "swimmer_evan");
        InTheStreet();
        Versus("in the street", "youngster_tristan");
        Versus("in the street", "galactic_grunt_floaroma_meadow_1");
        Versus("in the street", "galactic_grunt_jubilife_city_1", "galactic_grunt_jubilife_city_2");
        Versus("in the street", "twins_liv_and_liz");
        Versus("in the street", "leader_volkner_fight_area", "elite_four_flint_fight_area");
        Versus("in the street", "rival_route_203_piplup");
        InRoom("ValleyWindworksBuilding");
        Versus("in the Valley Windworks", "commander_mars_valley_windworks");
        InRoom("GalacticHQ4F");
        Versus("on his fourth floor", "galactic_boss_cyrus_galactic_hq");
        InRoom("OreburghGym");
        Versus("in his Gym", "leader_roark");
        InRoom("PokemonLeagueAaronRoom");
        Versus("in his room", "elite_four_aaron");

        // A battle with a grunt, from the flash to the battle's first frame
        var grunt = Carrying("galactic_grunt_floaroma_meadow_1");
        var (meadow, meadowX, meadowY) = AreaSpot("floaroma_meadow");
        At(meadow.Name, meadowX, meadowY, Direction.Down);
        walker.SetMode(TravelMode.OnFoot);
        Frames(30);
        game.StartTrainerBattle(grunt);
        Frames(50); Shot("l35_into_battle_with_a_grunt_1_shards");
        Frames(58); Shot("l35_into_battle_with_a_grunt_2_the_mark");
        Frames(24); Shot("l35_into_battle_with_a_grunt_3_opening");
        Frames(40); Shot("l35_into_battle_with_a_grunt_4_battle_begins");
        game.State = GameState.Overworld;
        Put(112, 880, Direction.Down);

        // The bands (plan 09 · L15), last of all so that nothing before them moves: Roark's in his Gym and Cynthia's in
        // her room, each begun as the game begins it, the person of the room carrying their team as their script's
        // battle makes them. Five moments of the band, by its own beats, and the battle opening out of its white
        void Band(string shots, string room, string who, string id, (string Name, float Since)[] moments)
        {
            var hall = MapDatabase.Get(room);
            var person = hall.Everyone.First(n => n.Name.Equals(who, StringComparison.OrdinalIgnoreCase));
            At(room, person.GridX, person.GridY + 1, Direction.Up);
            walker.SetMode(TravelMode.OnFoot);
            Frames(30);
            var met = Carrying(id);
            met.NpcType = person.NpcType;
            game.StartTrainerBattle(met);
            var kind = game.TransitionKind;
            var whose = game.Band?.Band;
            Console.WriteLine($"way in: {met.TrainerData!.FullTitle} in {room} -> {kind}; the band of {whose?.Class} {whose?.Name} in {whose?.Base.R},{whose?.Base.G},{whose?.Base.B}, "
                + $"their portrait of {game.BandTheirLook}, the player's of {game.BandOwnLook ?? "nobody"}; {SceneTransition.OutSeconds(kind):0.00} s to the white");
            int at = 0, n = 0, wanted = game.BandOwnLook != null ? 2 : 1;
            void To(float seconds)
            {
                int frame = (int)MathF.Round(seconds * 60f);
                Frames(frame - at);
                at = frame;
            }
            // The portraits are rendered in the first frame that finds their rigs ready: the very first, where the
            // person stands in the room and the player's rig is the field's own
            To(1f / 60f);
            Console.WriteLine($"  after the first frame: {game.BandRendered} of {wanted} portraits rendered");
            foreach (var (name, since) in moments)
            {
                To(SceneTransition.FlashSeconds + since);
                Shot($"{shots}_{++n}_{name}");
                Console.WriteLine($"  {name}: {game.BandRendered} of {wanted} portraits rendered");
            }
            To(SceneTransition.OutSeconds(kind) + 0.2f); Shot($"{shots}_{++n}_opening");
            Frames(40);
            Console.WriteLine($"  the band over: {(game.Band == null ? "its portraits freed" : "!! its portraits still held")}");
            game.State = GameState.Overworld;
        }
        Band("l38_band_roark", "OreburghGym", "Roark", "leader_roark", new[]
        {
            ("wiping_in", 0.12f), ("the_mark", 0.9f), ("silhouette", 1.45f), ("lit", 2.25f), ("the_white", 2.95f)
        });
        Band("l39_band_cynthia", "PokemonLeagueChampionRoom", "Cynthia", "champion_cynthia", new[]
        {
            ("halves", 0.1f), ("silhouettes", 0.75f), ("lit", 1.42f), ("trembling", 2.5f), ("the_white", 2.75f)
        });
        Put(112, 880, Direction.Down);

        // The legendary's and the mythical's (plan 09 · L14), after everything so that nothing before them moves: a
        // Dialga met in the street and a Giratina in Oreburgh Gate, each begun as the game begins it. Four moments of
        // the camera's closing and the white, and the battle opening, with the lens the rig holds at each
        void Legendary(string shots, string species, string where, Action stand, (string Name, float Since)[] moments)
        {
            stand();
            game.StartWildBattle(new WildEncounterEntry { SpeciesName = species, MinLevel = 70, MaxLevel = 70, Weight = 1 });
            var kind = game.TransitionKind;
            var rig = game.World.Camera;
            Console.WriteLine($"way in: a wild {species} {where} -> {kind}; {SceneTransition.OutSeconds(kind):0.00} s to the white");
            int at = 0, n = 0;
            void To(float seconds)
            {
                int frame = (int)MathF.Round(seconds * 60f);
                Frames(frame - at);
                at = frame;
            }
            foreach (var (name, since) in moments)
            {
                To(SceneTransition.FlashSeconds + since);
                Shot($"{shots}_{++n}_{name}");
                Console.WriteLine($"  {name}: {(rig.Holding ? "held" : "!! not held")}, lens {rig.Lens:0.000} (the way's {SceneTransition.LensAt(kind, at / 60f).Zoom:0.000}), "
                    + $"step {rig.HeldStep.X:0.000},{rig.HeldStep.Y:0.000}, widest ahead {rig.WidestAhead:0.000}");
            }
            To(SceneTransition.OutSeconds(kind) + 0.2f); Shot($"{shots}_{++n}_opening");
            Console.WriteLine($"  the battle opening: {(rig.Holding ? "!! the lens still held" : "the camera let go")}, lens {rig.Lens:0.000}, at rest {rig.AtRest}");
            Frames(40);
            game.State = GameState.Overworld;
        }
        Legendary("l36_legendary", "Dialga", "in the street", InTheStreet, new[]
        {
            ("widening", 0.8f), ("the_rush", 1.7f), ("the_white", 2.4f), ("nearly_white", 3.6f)
        });
        Legendary("l37_mythical", "Giratina", "in Oreburgh Gate", InTheCave, new[]
        {
            ("first_lenses", 0.3f), ("widest", 1.2f), ("last_lenses", 1.75f), ("the_white", 2.08f)
        });
        Put(112, 880, Direction.Down);
    }
}
