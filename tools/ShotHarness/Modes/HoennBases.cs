partial class Harness
{
    /// <summary>
    /// Hoenn's secret bases (plan 21 · H10), last of the <c>hoenn</c> mode so nothing before it moves: a tree on Route 118
    /// that might drop a vine, with Secret Power's question over it (<c>h90_secret_power_tree</c>); the base opened and
    /// gone into, bare before its PC (<c>h91_base_empty</c>); the decorating screen with a desk in hand over the floor
    /// (<c>h93_decoration_screen</c>); the base furnished (<c>h92_base_decorated</c>), and found again so from a save
    /// written and continued (<c>h92b_base_from_the_save</c>). It prints where the base is, what stands in it and what the
    /// save brought back.
    /// </summary>
    void HoennBaseShots()
    {
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // Reads on through what is said until the script is over and the field is the player's
        void ReadOn()
        {
            for (int i = 0; i < 1200 && (engine.ScriptRunning || game.State != GameState.Overworld); i++)
            {
                if (game.State == GameState.Decoration) return;
                if (engine.Choice.IsOpen) return;
                if (Box().IsActive && Box().IsCurrentLineComplete) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
        }

        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);

        // A Pokémon that knows Secret Power, and decorations in the PC
        var party = game.Party;
        var digger = new Pokemon(PokemonDatabase.Get("Zigzagoon")!, 30);
        digger.Moves.Clear();
        digger.Moves.Add(new Move(MoveDatabase.Get("Secret Power")!));
        while (party.IsFull) party.Members.RemoveAt(party.Members.Count - 1);
        party.Add(digger);
        foreach (string name in new[] { "Heavy Desk", "Pikachu Doll", "Red Brick", "Surf Mat", "Ball Poster", "Pretty Flowers", "Round Cushion", "Red Balloon" })
            SecretBases.Add(game.Decorations, DecorationDatabase.IdOf(name));

        // TREE2_1, Route 118's tree, faced from the tile under it
        var spot = MapDatabase.Get("Hoenn").SecretBaseSpots.FirstOrDefault(s => s.Value == 171);
        if (spot.Value != 171) { Console.WriteLine("!! no spot SECRET_BASE_TREE2_1 on Hoenn's map"); return; }
        At("Hoenn", spot.Key.X, spot.Key.Y + 1, Direction.Up);
        Frames(20);
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "the spot's words"); Box().FinishLine(); Frames(2);
        Until(() => engine.Choice.IsOpen, "Secret Power's question"); Frames(20);
        Shot("h90_secret_power_tree");
        engine.Choice.Confirm(); Frames(4);
        ReadOn();
        Frames(30);
        game.LocationSign.Hide();
        Console.WriteLine($"secret base: made at ({spot.Key.X}, {spot.Key.Y}) as {game.SecretBase?.Layout?.Map ?? "nothing"}, "
            + $"the player on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY})");
        if (game.Map.Name != "SecretBase_Tree2") Console.WriteLine("  !! the base was never gone into");
        Shot("h91_base_empty");

        // The PC, its DECORATE, and each decoration set down by the screen's own buttons
        game.Interact(); Frames(3);
        ReadOn();
        Until(() => engine.Choice.IsOpen, "the PC's menu"); Frames(10);
        engine.Choice.Confirm(); Frames(4);
        ReadOn();
        Until(() => game.State == GameState.Decoration, "the decorating screen");
        var screen = game.Decoration;
        // (Tree2's floor: x 1 to 4 from row 1 to 10, x 5 from row 6, the wall over row 0; the PC at 5,5)
        var places = new (int X, int Y)[] { (1, 2), (4, 3), (1, 6), (2, 6), (2, 0), (4, 0), (5, 7), (5, 9) };
        for (int slot = 0; slot < game.Decorations.Count && slot < places.Length; slot++)
        {
            var session = screen.Session;
            while (session.MenuCursor != 0) screen.Move(0, -1);
            screen.PressConfirm();
            while (session.StoreCursor < slot) screen.Move(0, 1);
            while (session.StoreCursor > slot) screen.Move(0, -1);
            screen.PressConfirm();
            var (tx, ty) = places[slot];
            for (int i = 0; i < 20 && session.X != tx; i++) screen.Move(Math.Sign(tx - session.X), 0);
            for (int i = 0; i < 20 && session.Y != ty; i++) screen.Move(0, Math.Sign(ty - session.Y));
            if (slot == 0) { Frames(10); Shot("h93_decoration_screen"); }
            screen.PressConfirm();
            Console.WriteLine($"  {session.KindOf(game.Decorations[slot].Kind).Name} at ({tx}, {ty}): {(session.Refused ? "refused, " + session.Message : "set down")}");
            if (session.Step != DecoratingStep.Menu) screen.PressCancel();
            if (session.Step != DecoratingStep.Menu) screen.PressCancel();
        }
        while (screen.Session.MenuCursor != DecoratingSession.MenuRowCount - 1) screen.Move(0, 1);
        screen.PressConfirm();
        Frames(4);
        ReadOn();
        Frames(20);
        Console.WriteLine($"  standing in the base: {string.Join(", ", SecretBases.Standing(game.SecretBase!, game.Decorations).Select(d => $"{d.Decoration.Name} ({d.X}, {d.Y})"))}");
        // Back down to the door's side of the room, to see it whole
        At(game.Map.Name, game.Player.GridX, game.Player.GridY, Direction.Down);
        Frames(20);
        Shot("h92_base_decorated");

        // Saved, and the game continued from the save: the base found again as it was left
        if (!game.Save()) Console.WriteLine("  !! the game wasn't saved");
        engine.ContinueGame(game.SaveSlot);
        Until(() => game.State == GameState.Overworld, "the continued game");
        Frames(40);
        game.LocationSign.Hide();
        Console.WriteLine($"  from the save: {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY}), the base {game.SecretBase?.Id}, "
            + $"{game.SecretBase?.Placed.Count ?? 0} standing, {game.Decorations.Count} in the PC");
        Shot("h92b_base_from_the_save");
        game.DeleteSave();
    }
}
