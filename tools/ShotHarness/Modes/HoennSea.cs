partial class Harness
{
    /// <summary>
    /// Dive and the sea floor (plan 21 · H5), last of the <c>hoenn</c> mode so nothing before it moves: Route 124's sea
    /// with a surfer on its dark water (<c>h92</c>), Dive's question there (<c>h93</c>), the sea floor under it by day and
    /// after dark, whose light ignores the clock (<c>h94</c>, <c>h95</c>, each with a crop at the screen's own
    /// resolution), and a wild Pokémon met in its kelp (<c>h96</c>, on the sea floor's own stage since part b); then,
    /// where the dive came down, a breath's bubbles rising and bursting and the waves' light moved on between two breaths
    /// (<c>h97</c> to <c>h99</c>, with crops at the screen's own resolution, part b), and the way back up. It prints where
    /// each dive put the player and how they travel there, and when the breath fell.
    /// </summary>
    void HoennSeaShots()
    {
        DialogueManager Box() => game.Dialogue;
        GameState State() => game.State;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }

        var hoenn = MapDatabase.Get("Hoenn");
        var floor = MapDatabase.Get("HoennUnderwater");
        if (!floor.IsUnderwater) { Console.WriteLine("!! HoennUnderwater isn't the sea floor"); return; }
        // A tile of Route 124's dark water whose floor below is open sand, nearest the middle of the trenches
        var dives = new List<(int X, int Y)>();
        for (int y = 0; y < hoenn.Height; y++)
            for (int x = 0; x < hoenn.Width; x++)
                // (Route 124's alone: plan 21 · H8 opened the floors under Routes 125 to 127, which would move the diver)
                if (hoenn.BehaviourAt(x, y) == TileBehavior.DiveWater && hoenn.DiveAt(x, y) == "HoennUnderwater"
                    && hoenn.AreaAt(x, y)?.Key == "route_124" && !floor.IsSolid(x, y)
                    && FieldMovement.CanSurface(floor, x, y) && floor.GetGroundTile(x, y) != TileType.Kelp)
                    dives.Add((x, y));
        if (dives.Count == 0) { Console.WriteLine("!! no dark water over an open floor on Route 124"); return; }
        double mx = dives.Average(t => t.X), my = dives.Average(t => t.Y);
        var (dx, dy) = dives.OrderBy(t => (t.X - mx) * (t.X - mx) + (t.Y - my) * (t.Y - my)).First();
        Console.WriteLine($"sea floor: {dives.Count} tiles of dark water dive; the diver at ({dx}, {dy})");

        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // A Pokémon that knows Dive and Surf, and the Mind Badge
        var party = game.Party;
        var diver = new Pokemon(PokemonDatabase.Get("Wailmer")!, 40);
        diver.Moves.Clear();
        diver.Moves.Add(new Move(MoveDatabase.Get("Dive")!));
        diver.Moves.Add(new Move(MoveDatabase.Get("Surf")!));
        party.Add(diver);
        int badgesBefore = game.Story.BadgeMask("Hoenn");
        game.Story.GiveBadge("Hoenn", "Balance");
        game.Story.GiveBadge("Hoenn", "Mind");

        At("Hoenn", dx, dy, Direction.Up);
        game.Player.SetMode(TravelMode.Surfing);
        game.Player.Moves = FieldMovement.MovesOf(party);
        Frames(30);
        Shot("h92_route124_surface");

        game.Interact(); Frames(3); Whole(); Until(() => engine.Choice.IsOpen, "Dive's question"); Frames(20);
        Shot("h93_dive_down");
        engine.Choice.Confirm(); Frames(4);
        for (int i = 0; i < 900 && (engine.ScriptRunning || State() != GameState.Overworld); i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Frames(30);
        game.LocationSign.Hide();
        Console.WriteLine($"  dived: {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), {game.Player.Mode}, "
            + $"in {game.Map.AreaAt(game.Player.GridX, game.Player.GridY)?.Key ?? "nowhere"}");
        if (game.Map.Name != "HoennUnderwater") Console.WriteLine("  !! the dive never reached the sea floor");
        Shot("h94_sea_floor_day");
        ShotCrop("h94_sea_floor_day_native", 640, 300, 640, 360, 2);

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Frames(2);
        Shot("h95_sea_floor_night");
        ShotCrop("h95_sea_floor_night_native", 640, 300, 640, 360, 2);
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // A wild Pokémon of the kelp, as a step in it brings one, then run from
        var kelp = Enumerable.Range(0, floor.Height).SelectMany(y => Enumerable.Range(0, floor.Width).Select(x => (x, y)))
            .Where(t => floor.GetGroundTile(t.x, t.y) == TileType.Kelp && floor.AreaAt(t.x, t.y)?.Open == true)
            .OrderBy(t => Math.Abs(t.x - dx) + Math.Abs(t.y - dy)).FirstOrDefault();
        if (floor.GetGroundTile(kelp.x, kelp.y) == TileType.Kelp)
        {
            At("HoennUnderwater", kelp.x, kelp.y, Direction.Up);
            Frames(10);
            WildEncounterEntry? met = null;
            for (int i = 0; i < 50 && met == null; i++) met = floor.RollWildEncounter(kelp.x, kelp.y, game.Player.Encounters, water: true);
            met ??= new WildEncounterEntry { SpeciesName = "Clamperl", MinLevel = 30, MaxLevel = 30 };
            Console.WriteLine($"  kelp at ({kelp.x}, {kelp.y}): met {met.SpeciesName}, the battle on {floor.ArenaAt(kelp.x, kelp.y)}");
            game.StartWildBattle(met);
            Until(() => State() == GameState.Battle, "the kelp's battle", 300);
            var fight = game.Battle;
            ToMainMenu(fight);
            Shot("h96_seaweed_battle");
            for (int guard = 0; guard < 200 && !fight.IsBattleOver; guard++)
            {
                if (fight.HUD.MenuState == BattleMenuState.Main) fight.SelectMainMenuOption(3);
                else Confirm(fight);
                Skip(0.5);
            }
            Until(() => State() == GameState.Overworld, "the field again", 1500);
        }
        else Console.WriteLine("  !! no kelp on the open sea floor");

        // Where the dive came down: the diver's bubbles and the waves' light over the floor (plan 21 · H5, part b).
        // A breath falls on a whole multiple of FieldLife.BreathEvery on the field's clock: wait for the next and shoot
        // its bubbles grown and rising, the first of them bursting, and the floor between two breaths with the light moved on
        At("HoennUnderwater", dx, dy, Direction.Up);
        game.Player.SetMode(TravelMode.Diving);
        Frames(4);
        game.LocationSign.Hide();
        double breath = (Math.Floor(game.World.Life.Now / FieldLife.BreathEvery) + 1) * FieldLife.BreathEvery;
        void OnTheClock(double at) => Until(() => game.World.Life.Now >= at, $"the field's clock at {at:0.00}", 600);
        OnTheClock(breath + 0.55);
        Console.WriteLine($"  bubbles: a breath at {breath:0.00} s, shot {game.World.Life.Now - breath:0.00} s after it; "
            + $"{game.World.Life.Count} pieces of life about");
        Shot("h97_bubbles_rising");
        ShotCrop("h97_bubbles_rising_native", 640, 240, 640, 360, 2);
        OnTheClock(breath + 1.22);
        ShotCrop("h98_bubbles_bursting_native", 640, 240, 640, 360, 2);
        OnTheClock(breath + FieldLife.BreathEvery - 0.3);
        Shot("h99_caustics_later");
        ShotCrop("h99_caustics_later_native", 640, 240, 640, 360, 2);

        // And back up from where the dive came down
        game.Interact(); Frames(3); Whole(); Until(() => engine.Choice.IsOpen, "the question to swim up"); Frames(2);
        engine.Choice.Confirm(); Frames(4);
        for (int i = 0; i < 900 && (engine.ScriptRunning || State() != GameState.Overworld); i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Console.WriteLine($"  surfaced: {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), {game.Player.Mode}");

        // Put the team and the Badges back as they were
        game.Story.SetBadges("Hoenn", badgesBefore);
        party.Members.Remove(diver);
        game.Player.SetMode(TravelMode.OnFoot);
        game.Player.Moves = FieldMovement.MovesOf(party);
    }
}
