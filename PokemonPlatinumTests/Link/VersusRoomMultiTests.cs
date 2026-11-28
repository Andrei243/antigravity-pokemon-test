using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests.Link;

/// <summary>
/// VERSUS, TWO ON TWO in the game (plan 13 · V21): the battle room's row, four picks, the battle with its curtain
/// between the sides and each of the four asked by their own seat's pad, then the room again. Nothing of any file the
/// teams came from is written.
/// </summary>
[Collection("Shared clocks and dice")]
public class VersusRoomMultiTests
{
    [Fact]
    public void TheRowPicksFourTeamsAndBackStepsAPlayerAtATime()
    {
        var room = new BattleRoomScreen();
        var team = new BuiltTeam { Members = { new BuiltPokemon { Species = "Lucario", Level = 50, Moves = { "Close Combat" } } } };
        room.Open(new[] { team, team }, Array.Empty<PuzzleFile>(), new BattleRoomRecords());
        while (BattleRoomScreen.Entries[room.Cursor] != RoomEntry.Multi) room.Move(1);
        Assert.Equal(RoomEntry.Multi, BattleRoomScreen.Entries[^1]);
        room.Confirm();
        Assert.Equal(RoomChallenge.Multi, room.Picking);
        room.Confirm();
        room.Move(1);
        room.Confirm();
        Assert.Equal(new[] { 0, 1 }, room.MultiPicks);
        room.Cancel();
        Assert.Equal(new[] { 0 }, room.MultiPicks);
        Assert.Equal(1, room.Cursor);
        room.Cancel();
        room.Cancel();
        Assert.Equal(RoomPage.Menu, room.Page);
        Assert.Equal((int)RoomEntry.Multi, room.Cursor);
        room.Confirm();
        for (int i = 0; i < 3; i++) room.Confirm();
        Assert.Null(room.TakeRequest());
        room.Confirm();
        Assert.Equal(new[] { 0, 0, 0, 0 }, Assert.IsType<BeginMulti>(room.TakeRequest()).Teams);
        room.AfterVersus(new[] { team });
        Assert.Equal((int)RoomEntry.Multi, room.Cursor);
        Assert.Empty(room.MultiPicks);
    }

    [Fact]
    public void ASeatReadsItsOwnPadWhereOneIsPluggedInAndTheFirstOtherwise()
    {
        InputManager.Reset();
        for (int seat = 0; seat < 4; seat++)
        {
            Assert.Equal(seat, InputManager.PadFor(seat, _ => true));
            Assert.Equal(0, InputManager.PadFor(seat, _ => false));
        }
        // Two pads: the second seat has its own, the third and fourth pass the first round
        Assert.Equal(new[] { 0, 1, 0, 0 }, Enumerable.Range(0, 4).Select(s => InputManager.PadFor(s, p => p < 2)));
        // The seats go on from the pad the options chose, round to the first
        InputManager.Use(new GameSettings { GamepadIndex = 2 });
        Assert.Equal(new[] { 2, 3, 0, 1 }, Enumerable.Range(0, 4).Select(InputManager.PadOfSeat));
        InputManager.Reset();
    }

    private static BuiltTeam Team(string name, string species, params string[] moves)
    {
        var member = new BuiltPokemon { Species = species, Level = 50 };
        member.Moves.AddRange(moves);
        return new BuiltTeam { Name = name, Members = { member } };
    }

    [Fact]
    public void FourPlayAtOneKeyboardFromTheRoomAndNoFileIsWritten()
    {
        string folder = Path.Combine(Path.GetTempPath(), "multi-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(21);
            string teams = Path.Combine(folder, BuiltTeams.FileName);
            BuiltTeams.Write(teams, new BuiltTeamsFile
            {
                Teams =
                {
                    Team("ONE", "Garchomp", "Earthquake", "Dragon Claw"), Team("TWO", "Lucario", "Close Combat"),
                    Team("THREE", "Gengar", "Shadow Ball"), Team("FOUR", "Snorlax", "Body Slam")
                }
            });
            byte[] before = File.ReadAllBytes(teams);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            var game = engine.Drive;
            game.OpenBattleRoom();

            void Frame(params GameAction[] pressed)
            {
                InputManager.Feed(pressed.Length == 0 ? ActionFrame.None : ActionFrame.Press(pressed));
                engine.Update(1f / 60f);
            }
            void Until(Func<bool> done, string what)
            {
                for (int f = 0; f < 60 * 120; f++)
                {
                    if (done()) return;
                    // A is pressed every few frames: lines read on, the first command, move and target chosen
                    if (f % 6 == 5) Frame(GameAction.Confirm); else Frame();
                }
                Assert.Fail($"Waited for {what}; the game is in {game.State}");
            }

            while (BattleRoomScreen.Entries[game.BattleRoom.Cursor] != RoomEntry.Multi) Frame(GameAction.Down);
            Frame(GameAction.Confirm);
            for (int player = 0; player < 4; player++)
            {
                while (game.BattleRoom.Cursor != player) { Frame(GameAction.Down); }
                Frame(GameAction.Confirm);
            }
            Until(() => game.State == GameState.Battle && game.Versus != null, "the battle");
            var match = game.Versus!;
            Assert.True(match.Four);
            Assert.Equal("Garchomp", match.Server.Setup.TeamA[0].Species.Name);
            Assert.Equal("Lucario", match.Server.Setup.PartnerTeamA[0].Species.Name);
            Assert.Equal("Gengar", match.Server.Setup.TeamB[0].Species.Name);
            Assert.Equal("Snorlax", match.Server.Setup.PartnerTeamB[0].Species.Name);

            var seats = new List<int>();
            Until(() =>
            {
                if (game.Versus is { } now && (seats.Count == 0 || seats[^1] != InputManager.Seat) && game.State == GameState.Battle)
                    seats.Add(InputManager.Seat);
                return game.State == GameState.BattleRoom;
            }, "the room again");
            // Each of the four had their seat
            Assert.Equal(new[] { 0, 1, 2, 3 }, seats.Distinct().OrderBy(s => s));
            Frame();
            Assert.Equal(0, InputManager.Seat);
            Assert.Null(game.Versus);
            Assert.Equal((int)RoomEntry.Multi, game.BattleRoom.Cursor);
            Assert.Equal(before, File.ReadAllBytes(teams));
            Assert.Empty(Directory.GetFiles(folder, "save*"));
        }
        finally
        {
            InputManager.Feed(null);
            InputManager.Reset();
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
