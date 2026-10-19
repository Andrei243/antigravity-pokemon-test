partial class Harness
{
    /// <summary>
    /// The west and the sea as plan 20 · J8 opens them, after Ilex Forest to Ecruteak City in the <c>johto</c> mode (so
    /// nothing before it moves; <c>JOHTO_FROM=west</c> runs it alone in a new game of its own): the five places with
    /// ground by day and after dark from the open tile nearest each one's middle (<c>jw48</c> to <c>jw57</c>), a few
    /// places close up (<c>jw58</c> to <c>jw61</c>), every new room from its own middle (<c>jr76</c> on), a whirlpool on
    /// Route 41 faced from the water without the move, its question and the surfer carried across (<c>jw62</c> to
    /// <c>jw65</c>), and a walk from Route 38's gate to Olivine's port, timed, with what streaming cost.
    /// </summary>
    void JohtoWestShots()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);
        string[] areas = { "route_38", "route_39", "olivine", "route_40", "cianwood" };
        void Areas(string time)
        {
            for (int i = 0; i < areas.Length; i++)
            {
                var (map, x, y) = AreaSpot(areas[i]);
                At(map.Name, x, y, Direction.Up); Frames(2);
                Shot($"jw{48 + 2 * i + (time == "night" ? 1 : 0)}_{areas[i]}_{time}");
                if (time == "day") Console.WriteLine($"{areas[i]}: {map.Name} ({x}, {y}), {map.DisplayNameAt(x, y)}, music {map.BgmTrackAt(x, y)}");
            }
        }
        Areas("day");
        // Close up: the Glitter Lighthouse on its point, Olivine's harbour with its yachts, the Moomoo Farm and Cianwood's
        // pharmacy
        At("Johto", 301, 266, Direction.Up); Frames(2); Shot("jw58_olivine_lighthouse"); ShotCrop("jw58_olivine_lighthouse_native", 640, 300, 640, 360, 2);
        At("Johto", 281, 268, Direction.Down); Frames(2); Shot("jw59_olivine_harbour");
        At("Johto", 265, 166, Direction.Up); Frames(2); Shot("jw60_moomoo_farm");
        At("Johto", 179, 372, Direction.Up); Frames(2); Shot("jw61_cianwood_pharmacy"); ShotCrop("jw61_cianwood_native", 640, 300, 640, 360, 2);

        string[] rooms =
        {
            "route_38_ecruteak_gatehouse", "route_39_moomoo_farm_house", "route_39_moomoo_farm_stable",
            "olivine_pokecenter_1f", "olivine_pokecenter_b1f", "olivine_pokemart", "olivine_cafe", "olivine_gym",
            "olivine_north_house", "olivine_northeast_house", "olivine_northwest_house", "ss_aqua_olivine_port_interior",
            "olivine_lighthouse_1f", "olivine_lighthouse_2f", "olivine_lighthouse_3f", "olivine_lighthouse_4f",
            "olivine_lighthouse_5f", "olivine_lighthouse_exterior", "olivine_lighthouse_light_room",
            "route_40_battle_frontier_gatehouse", "cianwood_pokecenter_1f", "cianwood_gym", "cianwood_pharmacy",
            "cianwood_kirks_house", "cianwood_central_house", "cianwood_north_house", "cianwood_camerons_house",
            "whirl_islands_1f", "whirl_islands_b1f", "whirl_islands_b2f"
        };
        for (int i = 0; i < rooms.Length; i++)
        {
            var (room, x, y) = AreaSpot(rooms[i]);
            At(room.Name, x, y, Direction.Up); Frames(2);
            Shot($"jr{76 + i}_{rooms[i]}");
            Console.WriteLine($"{rooms[i]}: {room.Name} {room.Width}×{room.Height}, {room.NPCs.Count} people, {room.Props.Count} pieces, ways out {string.Join(", ", room.Warps.Select(w => $"({w.SourceX}, {w.SourceY}) to {w.TargetMap}"))}");
        }

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Areas("night");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        WhirlpoolShots();
        WalkToOlivinesPort();
    }

    /// <summary>
    /// A whirlpool of Route 41 faced from the water south of it: refused without the move (<c>jw62</c>), its question with
    /// the move and the Glacier Badge (<c>jw63</c>), the surfer half way across (<c>jw64</c>) and on the far side (<c>jw65</c>).
    /// </summary>
    void WhirlpoolShots()
    {
        var johto = MapDatabase.Get("Johto");
        var ring = johto.Props.Where(p => p.Type == PropType.Whirlpool).OrderBy(p => p.X).FirstOrDefault();
        if (ring == null) { Console.WriteLine("!! no whirlpool on the map of Johto"); return; }
        // The block of whirlpool tiles under the ring, and the open water south of its middle column
        var tiles = Enumerable.Range(ring.X, ring.Width).SelectMany(x => Enumerable.Range(ring.Y, ring.Depth).Select(y => (x, y)))
            .Where(t => johto.BehaviourAt(t.x, t.y) == TileBehavior.Whirlpool).ToList();
        int mid = (tiles.Min(t => t.x) + tiles.Max(t => t.x)) / 2, south = tiles.Max(t => t.y) + 1;
        Console.WriteLine($"whirlpool at ({ring.X}, {ring.Y}) {ring.Width}×{ring.Depth}, {tiles.Count} whirlpool tiles; the surfer at ({mid}, {south})");

        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }

        var party = game.Party;
        int badgesBefore = game.Story.BadgeMask("Johto");
        At("Johto", mid, south, Direction.Up);
        game.Player.SetMode(TravelMode.Surfing);
        Frames(4);
        game.Interact(); Frames(3); Whole(); Frames(10);
        Shot("jw62_whirlpool_refused");
        Console.WriteLine($"  refused: the text box {(Box().IsActive ? "is up" : "!! never came up")}");
        for (int i = 0; i < 6 && Box().IsActive; i++) { Box().Advance(); Frames(3); }
        Until(() => !engine.ScriptRunning, "the refusal's end", 300);

        // A Pokémon that knows the move, and the Glacier Badge
        var tentacruel = new Pokemon(PokemonDatabase.Get("Tentacruel")!, 40);
        tentacruel.Moves.Clear();
        tentacruel.Moves.Add(new Move(MoveDatabase.Get("Whirlpool")!));
        tentacruel.Moves.Add(new Move(MoveDatabase.Get("Surf")!));
        party.Add(tentacruel);
        game.Story.GiveBadge("Johto", "Glacier");
        game.Player.Moves = FieldMovement.MovesOf(party);
        game.Interact(); Frames(3); Whole(); Until(() => engine.Choice.IsOpen, "the whirlpool's question"); Frames(20);
        Shot("jw63_whirlpool_asks");
        engine.Choice.Confirm(); Frames(4);
        for (int i = 0; i < 300 && engine.ScriptRunning; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete) { Box().Advance(); Frames(2); }
            else Frames(1);
            if (game.Player.IsMoving && game.Player.GridY < south - 1) break;
        }
        Shot("jw64_whirlpool_crossing");
        Until(() => !engine.ScriptRunning && !game.Player.IsMoving, "the crossing's end", 600);
        Frames(10);
        Shot("jw65_whirlpool_crossed");
        Console.WriteLine($"  crossed: the player at ({game.Player.GridX}, {game.Player.GridY}), {game.Player.Mode}");

        // Put the team and the Badges back as they were
        game.Story.SetBadges("Johto", badgesBefore);
        party.Members.Remove(tentacruel);
        game.Player.SetMode(TravelMode.OnFoot);
        game.Player.Moves = FieldMovement.MovesOf(party);
    }

    void WalkToOlivinesPort()
    {
        // The walk: from Route 38's gate west and down Route 39 to Olivine City's port, tile by tile at running pace, every
        // frame held to a sixtieth of a second
        if (filter.Length > 0 && Environment.GetEnvironmentVariable("SHOTS_TIMING") != "1") return;
        var johto = MapDatabase.Get("Johto");
        List<(int X, int Y)> path;
        try { path = WalkingPath(johto, (350, 167), (279, 277)); }
        catch (InvalidOperationException e) { Console.WriteLine($"!! {e.Message}"); return; }
        var renderer = game.World;
        At("Johto", 350, 167, Direction.Left); Frames(30);
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
        Console.WriteLine($"walk from Route 38's gate to Olivine's port: {path.Count} tiles, {total / Math.Max(1, frames):F2} ms/frame, worst frame {worst:F1} ms");
        Console.WriteLine($"  streaming: {renderer.Streaming}");
    }
}
