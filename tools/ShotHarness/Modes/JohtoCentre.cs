partial class Harness
{
    /// <summary>
    /// The centre of Johto as plan 20 · J5 opens it, at the end of the <c>johto</c> mode (after the chapter and the
    /// Pokégear, so nothing before it moves): the Ruins of Alph, Route 33 and Azalea Town by day and after dark from the
    /// open tile nearest each one's middle (<c>jw17</c> to <c>jw22</c>), a few places close up (<c>jw23</c> to <c>jw26</c>),
    /// every new room from its own middle (<c>jr32</c> on), and a walk across Azalea Town from Route 33 to the gate house
    /// to Ilex Forest, timed, with what streaming cost.
    /// </summary>
    void JohtoCentreShots()
    {
        game.State = GameState.Overworld;
        string[] areas = { "ruins_of_alph", "route_33", "azalea" };
        void Areas(string time)
        {
            for (int i = 0; i < areas.Length; i++)
            {
                var (map, x, y) = AreaSpot(areas[i]);
                At(map.Name, x, y, Direction.Up); Frames(2);
                Shot($"jw{17 + 2 * i + (time == "night" ? 1 : 0)}_{areas[i]}_{time}");
                if (time == "day") Console.WriteLine($"{areas[i]}: {map.Name} ({x}, {y}), {map.DisplayNameAt(x, y)}, music {map.BgmTrackAt(x, y)}");
            }
        }
        Areas("day");
        // Close up: the research centre and the chambers' huts, Azalea's houses and Gym, Kurt's house and the well
        At("Johto", 441, 288, Direction.Up); Frames(2); Shot("jw23_ruins_research_center");
        At("Johto", 431, 288, Direction.Up); Frames(2); Shot("jw24_ruins_hall");
        At("Johto", 405, 474, Direction.Up); Frames(2); Shot("jw25_azalea_gym"); ShotCrop("jw25_azalea_gym_native", 640, 300, 640, 360, 2);
        At("Johto", 400, 457, Direction.Up); Frames(2); Shot("jw26_azalea_kurt_and_well");

        string[] rooms =
        {
            "ruins_of_alph_research_center", "ruins_of_alph_northeast_entrance_first_room", "ruins_of_alph_southwest_entrance_first_room",
            "route_36_ruins_of_alph_gatehouse", "union_cave_1f", "union_cave_b1f", "union_cave_b2f",
            "azalea_pokecenter_1f", "azalea_pokecenter_b1f", "azalea_pokemart", "azalea_kurt_house", "azalea_charcoal_kiln",
            "azalea_ilex_forest_gatehouse"
        };
        for (int i = 0; i < rooms.Length; i++)
        {
            var (room, x, y) = AreaSpot(rooms[i]);
            At(room.Name, x, y, Direction.Up); Frames(2);
            Shot($"jr{32 + i}_{rooms[i]}");
            Console.WriteLine($"{rooms[i]}: {room.Name} {room.Width}×{room.Height}, {room.NPCs.Count} people, {room.Props.Count} pieces, ways out {string.Join(", ", room.Warps.Select(w => $"({w.SourceX}, {w.SourceY}) to {w.TargetMap}"))}");
        }

        // The Kabuto chamber's panel (plan 20 · J5): read through the room's own script, the panel as it begins, a piece
        // in hand and turned, half the picture in place, and the whole picture glowing
        {
            var (chamber, cx, cy) = AreaSpot("ruins_of_alph_northeast_entrance_first_room");
            At(chamber.Name, cx, cy, Direction.Up); Frames(2);
            var panelTile = chamber.TileScripts.First(kv => kv.Value == "Panel").Key;
            At(chamber.Name, panelTile.X, panelTile.Y + 1, Direction.Up); Frames(2);
            // Read on through the lines to the question, and say yes
            void On(Func<bool> until, int most = 900)
            {
                for (int i = 0; i < most && !until(); i++)
                {
                    var box = game.Dialogue;
                    if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
                    else Frames(1);
                }
            }
            game.Interact(); Frames(30);
            Shot("jp00_panel_read");
            On(() => engine.Choice.IsOpen);
            Frames(10);
            Shot("jp00b_panel_question");
            engine.Choice.Confirm();
            On(() => game.State == GameState.AlphPuzzle, 120);
            var screen = game.AlphPuzzle;
            for (int f = 0; f < 120 && PokemonSprites.GetBaked(screen.Puzzle!.Species, SpriteView.Front) == null; f++) Frames(1);
            Frames(20);
            Shot("jp01_panel");
            var puzzle = screen.Puzzle!;
            (int X, int Y) Where(int piece)
            {
                for (int y = 0; y < AlphPuzzle.Side; y++)
                    for (int x = 0; x < AlphPuzzle.Side; x++)
                        if (puzzle.PieceAt(x, y) == piece) return (x, y);
                return (-1, -1);
            }
            void Place(int piece, bool upright)
            {
                var (x, y) = Where(piece);
                puzzle.Point(x, y); screen.Confirm();
                puzzle.Point(1 + piece % AlphPuzzle.Frame, 1 + piece / AlphPuzzle.Frame); screen.Confirm();
                if (upright) while (puzzle.TurnsOf(piece) != 0) screen.Turn(1);
            }
            for (int piece = 0; piece < 8; piece++) Place(piece, true);
            var (hx, hy) = Where(8);
            puzzle.Point(hx, hy); screen.Confirm(); screen.Turn(1);
            puzzle.Point(1, 3);
            Frames(10);
            Shot("jp02_panel_half_and_in_hand");
            puzzle.Point(hx, hy); screen.Confirm();
            for (int piece = 8; piece < AlphPuzzle.Pieces; piece++) Place(piece, true);
            Frames(20);
            Console.WriteLine($"the Kabuto panel: solved {puzzle.Solved} in {puzzle.Moves} moves, screen open {screen.IsActive}");
            Shot("jp03_panel_whole");
            Frames(110);
            On(() => game.Dialogue.IsActive, 120);
            Frames(60);
            Shot("jp04_panel_after");
            Console.WriteLine($"  after it: {(game.Story.Has("FLAG_ALPH_KABUTO_PANEL_SOLVED") ? "the flag is set" : "!! no flag")}, state {game.State}");
            On(() => !engine.ScriptRunning && !game.Dialogue.IsActive && game.State == GameState.Overworld);
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Areas("night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // The walk: from Union Cave's mouth on Route 33 across Azalea Town to the gate house to Ilex Forest, tile by tile at
        // running pace, every frame held to a sixtieth of a second
        if (filter.Length == 0 || Environment.GetEnvironmentVariable("SHOTS_TIMING") == "1")
        {
            var johto = MapDatabase.Get("Johto");
            var path = WalkingPath(johto, (467, 462), (393, 462));
            var renderer = game.World;
            At("Johto", 467, 462, Direction.Left); Frames(30);
            renderer.ResetStreamingStats();
            double total = 0, worst = 0;
            int frames = 0;
            var watch = new System.Diagnostics.Stopwatch();
            foreach (var (x, y) in path)
            {
                game.Player.SetPosition(x, y, Direction.Left);
                for (int f = 0; f < 8; f++)
                {
                    watch.Restart();
                    FrameClock.Fixed = ++tick / 60.0;
                    engine.Update(1f / 60f);
                    engine.Draw();
                    double ms = watch.Elapsed.TotalMilliseconds;
                    total += ms; frames++;
                    worst = Math.Max(worst, ms);
                    while (watch.Elapsed.TotalMilliseconds < 1000.0 / 60.0) Thread.Sleep(1);
                }
            }
            Console.WriteLine($"walk from Union Cave's mouth on Route 33 across Azalea Town to the Ilex Forest gate: {path.Count} tiles, {total / Math.Max(1, frames):F2} ms/frame, worst frame {worst:F1} ms");
            Console.WriteLine($"  streaming: {renderer.Streaming}");
            foreach (var key in areas)
            {
                var (map, x, y) = AreaSpot(key);
                At(map.Name, x, y, Direction.Up);
                Timing("johto " + key);
            }
        }
    }
}
