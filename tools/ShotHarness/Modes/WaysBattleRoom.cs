partial class Harness
{
    // V13 and V26, the battle room: wy_battle_room (its menu), wy_team_builder (a member's page with its picture, types,
    // moves and stats) and wy_team_builder_species (the species list), wy_gauntlet_cynthia (the gauntlet's last battle at
    // its menu, a run begun twelve wins in), wy_puzzles_list, wy_puzzle_goal (a puzzle's battle at its menu with the goal
    // over it) and wy_puzzle_solved (the room after its solution). Nothing here reads or writes a save: the room's teams are
    // handed to the screens, never written. It prints each battle's foe and how the puzzle's battle ended
    void BattleRoomMode()
    {
        var team = new BuiltTeam
        {
            Name = "SHOWCASE",
            Members =
            {
                new BuiltPokemon { Species = "Garchomp", Level = 62, Nature = Nature.Jolly, Ability = "Rough Skin", Item = "Life Orb", Moves = { "Earthquake", "Dragon Claw", "Fire Fang", "Swords Dance" } },
                new BuiltPokemon { Species = "Lucario", Level = 62, Nature = Nature.Adamant, Ability = "Inner Focus", Item = "Focus Sash", Moves = { "Close Combat", "Extreme Speed", "Crunch" } },
                new BuiltPokemon { Species = "Togekiss", Level = 62, Nature = Nature.Timid, Item = "Leftovers", Moves = { "Air Slash", "Aura Sphere", "Roost" } },
                new BuiltPokemon { Species = "Rotom", Form = "Rotom-Wash", Level = 62, Nature = Nature.Modest, Moves = { "Hydro Pump", "Thunderbolt", "Will-O-Wisp" } }
            }
        };
        // The game's data is the modern rules' (their learnsets), so a move one of these can't know there is dropped
        foreach (var m in team.Members)
        {
            var can = BuiltTeams.MovesOf(m);
            var dropped = m.Moves.Where(move => !can.Contains(move)).ToList();
            m.Moves.RemoveAll(dropped.Contains);
            Console.WriteLine($"battle room: {m.Species} {(BuiltTeams.WhyNot(m) is { } why ? why.Key + " " + why.Value : "can battle")}{(dropped.Count > 0 ? ", without " + string.Join(", ", dropped) : "")}");
        }

        game.OpenBattleRoom();
        game.BattleRoom.Open(new[] { team }, Puzzles.All, new BattleRoomRecords { GauntletBest = 9, StreakBest = 23, Puzzles = { "priority", "sunshine" } });
        Frames(30);
        Shot("wy_battle_room");

        // The builder, on the team, its first member's page
        game.State = GameState.TeamBuilder;
        game.TeamBuilder.Open(new BuiltTeamsFile { Teams = { team.Copy() } });
        game.TeamBuilder.Confirm();
        game.TeamBuilder.Confirm();
        Frames(30);
        Shot("wy_team_builder");
        game.TeamBuilder.Confirm();
        Frames(20);
        Shot("wy_team_builder_species");
        game.TeamBuilder.Close();

        // The gauntlet's last battle: Cynthia, twelve wins in
        var run = new RoomRun(RoomChallenge.Gauntlet, team, 2026);
        for (int i = 0; i < 12; i++) run.Won();
        game.State = GameState.BattleRoom;
        game.StartRoomRun(run);
        Skip(1.5);
        if (game.Battle is { } cynthia)
        {
            Console.WriteLine($"battle room: battle {run.Battle} of {run.Length} against {cynthia.Trainers[0].Name} with {string.Join(", ", cynthia.Trainers[0].Party.Members.Select(p => $"{p.Species.Name} {p.Level}"))}");
            ToMainMenu(cynthia);
            Shot("wy_gauntlet_cynthia");
        }

        // The puzzles: the list, then the first one played to its solution
        game.Battle = null;
        game.OpenBattleRoom();
        game.BattleRoom.Open(Array.Empty<BuiltTeam>(), Puzzles.All, new BattleRoomRecords { Puzzles = { "priority", "levitate" } });
        game.BattleRoom.Move(3);
        game.BattleRoom.Confirm();
        game.BattleRoom.Move(4);
        Frames(30);
        Shot("wy_puzzles_list");

        var puzzle = Puzzles.All.First(p => p.Id == "sunshine");
        game.PlayPuzzle(puzzle);
        Skip(1.5);
        if (game.Battle is { } b)
        {
            ToMainMenu(b);
            Shot("wy_puzzle_goal");
            b.SelectMainMenuOption(0);
            b.SelectMove(b.PlayerPokemon.Moves.FindIndex(m => m.Name == puzzle.Solution[0].Move));
            for (int i = 0; i < 40 && game.State == GameState.Battle; i++)
            {
                Skip(0.8);
                Confirm(b);
            }
            Skip(1.5);
            Console.WriteLine($"battle room: {puzzle.Id} ended {b.Result} after {b.Core.Turn} turns; the room shows {game.BattleRoom.Page}, solved {game.BattleRoom.PuzzleSolved}");
            Shot("wy_puzzle_solved");
        }
        game.State = GameState.Overworld;
    }
}
