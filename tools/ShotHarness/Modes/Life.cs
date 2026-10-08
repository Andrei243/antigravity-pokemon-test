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
        int badges = story.BadgeMask;
        story.GiveBadge(Badge.Fen);
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
        story.SetBadges(badges);
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
                     ("trainer_strong", TransitionKind.TrainerStrong), ("leader", TransitionKind.Leader)
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
    }
}
