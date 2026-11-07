partial class Harness
{
    // V12, VERSUS (two people at one machine): wy_versus_pick_second (the battle room's team list as the second player
    // picks), wy_versus_first_side (the first player's commands), wy_versus_curtain (the curtain once they have chosen),
    // wy_versus_second_side (the second player's commands, from their own side, after the curtain is lifted) and
    // wy_versus_turn (the turn playing on the second player's screen). Two teams of the room's, handed to its screen;
    // nothing reads or writes a save. WAYS_ONLY=versus runs it alone
    void WaysVersusMode()
    {
        BuiltTeam Team(string name, params (string Species, string[] Moves)[] members)
        {
            var team = new BuiltTeam { Name = name };
            foreach (var (species, moves) in members)
            {
                var m = new BuiltPokemon { Species = species, Level = 50, Nature = Nature.Hardy };
                m.Moves.AddRange(moves.Where(BuiltTeams.MovesOf(m).Contains));
                team.Members.Add(m);
            }
            return team;
        }
        var first = Team("LUCAS", ("Infernape", new[] { "Flamethrower", "Close Combat", "Mach Punch" }), ("Staraptor", new[] { "Brave Bird", "Close Combat" }), ("Luxray", new[] { "Spark", "Crunch" }));
        var second = Team("DAWN", ("Empoleon", new[] { "Surf", "Drill Peck" }), ("Roserade", new[] { "Energy Ball", "Sludge Bomb" }), ("Garchomp", new[] { "Earthquake", "Dragon Claw" }));

        game.OpenBattleRoom();
        game.BattleRoom.Open(new[] { first, second }, Puzzles.All, new BattleRoomRecords());
        while (BattleRoomScreen.Entries[game.BattleRoom.Cursor] != RoomEntry.Versus) game.BattleRoom.Move(1);
        game.BattleRoom.Confirm();
        game.BattleRoom.Confirm();
        game.BattleRoom.Move(1);
        Frames(30);
        Shot("wy_versus_pick_second");
        game.BattleRoom.Confirm();

        // The battle opens on the first player's screen
        for (int i = 0; i < 900 && !(game.Versus is { } m0 && game.Battle == m0.Face && m0.Face.AtItsMenu); i++)
        {
            if (game.Battle is { IsLink: true } reading && reading.IsWaitingForConfirm && !reading.IsLearningMove) reading.ConfirmMessage();
            Skip(0.1);
        }
        if (game.Versus is not { } match)
        {
            Console.WriteLine("!! VERSUS never began");
            return;
        }
        Skip(0.8);
        Shot("wy_versus_first_side");
        Console.WriteLine($"versus: {match.Names[0]} with {string.Join(", ", match.Faces[0].LinkClient!.Own.Select(p => p.DisplayName))}; {match.Names[1]} with {string.Join(", ", match.Faces[1].LinkClient!.Own.Select(p => p.DisplayName))}");

        // The first player chooses: the curtain comes down for the second
        match.Face.SelectMainMenuOption(0);
        match.Face.SelectMove(Strongest(match.Face, match.Face.LinkClient!.Asked!.Options[0].Moves));
        for (int i = 0; i < 120 && match.Curtain == null; i++) Frames(1);
        Skip(0.3);
        Shot("wy_versus_curtain");
        Console.WriteLine($"versus: the curtain waits for {(match.Curtain is { } c ? match.Names[c] : "nobody")}");

        // Lifted: the second player's own screen, from their own side
        game.PassTheVersus();
        Skip(0.5);
        Shot("wy_versus_second_side");
        Console.WriteLine($"versus: shown {match.Names[match.Shown]}, at its menu {match.Face.AtItsMenu}, its near Pokémon {match.Face.PlayerSlots[0].Pokemon?.DisplayName}, across {match.Face.EnemySlots[0].Pokemon?.DisplayName}");
        match.Face.SelectMainMenuOption(0);
        match.Face.SelectMove(Strongest(match.Face, match.Face.LinkClient!.Asked!.Options[0].Moves));
        Skip(0.9);
        Shot("wy_versus_turn");
        Console.WriteLine($"versus: the line '{match.Face.CurrentMessage}'");

        // Left there: the room taken back as a battle room's own end would
        game.Battle = null;
        game.State = GameState.BattleRoom;
    }
}
