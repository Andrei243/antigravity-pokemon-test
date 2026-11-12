using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models.Cabinets;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- the arcade's cabinets (plan 42 · PS6)

    // The Lumiose Arcade (pz*; not part of "all"): the room from its door (pz01), a match-three stage's board (pz02),
    // a swap's cascade (pz03), the stage cleared (pz04), a picture's blank grid (pz05), half filled (pz06) and solved
    // (pz07), and the clerk's prize counter (pz09). The cabinets are opened as their scripts open them
    // (`Driver.OpenCabinet`) and played with the screens' own steps; it prints each board, each swap's clears and what
    // the records kept.
    public void CabinetsMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();

        At("LumioseCity_Arcade", 7, 9, Direction.Up);
        game.LocationSign.Hide();
        Frames(20);
        Shot("pz01_room");

        // ---- the match-three: the first stage on the team's species
        game.OpenCabinet(GameEngine.MatchCabinet);
        var match = game.MatchThree;
        Frames(20);
        match.Confirm();
        Frames(20);
        Console.WriteLine($"stage {match.Board!.Stage.Id}: {string.Join(", ", match.Board.Species)} against {match.Board.Stage.Foe}; {match.Board.Rows()}");
        Shot("pz02_match_board");
        bool cascadeShot = false;
        for (int guard = 0; guard < 60 && match.Phase == MatchPhase.Board; guard++)
        {
            var swap = match.Board.FirstSwap()!.Value;
            match.PointAt(swap.A);
            match.Confirm();
            match.PointAt(swap.B);
            match.Confirm();
            Console.WriteLine($"  swap {swap.A}-{swap.B}: {string.Join(" ", match.Board.LastClears.Select(c => $"[{c.Piece}x{c.Length} c{c.Cascade} {c.Damage}]"))} hp {match.Board.Hp}");
            Frames(2);
            if (!cascadeShot && match.Board.LastClears.Any(c => c.Cascade > 1))
            {
                Shot("pz03_cascade");
                cascadeShot = true;
            }
            Frames(14);
        }
        if (!cascadeShot) Console.WriteLine("  !! no cascade fell in the stage");
        Frames(10);
        Shot("pz04_stage_cleared");
        Console.WriteLine($"result: cleared {match.Board.Cleared}, swaps left {match.Board.SwapsLeft}, pays {match.Outcome}");
        match.Confirm();
        Frames(5);
        Console.WriteLine($"records: cleared {string.Join(",", game.Cabinets.StagesCleared)}; coins {game.Pastimes.Coins.Coins}");

        // ---- the picture-logic puzzle: the first species seen with a picture
        game.State = GameState.Overworld;
        foreach (var species in new[] { "Bidoof", "Starly", "Shinx" }) game.Pokedex.RegisterSeen(PokemonDatabase.Get(species)!.DexNumber);
        game.OpenCabinet(GameEngine.PictureCabinet);
        var picture = game.PictureLogic;
        for (int i = 0; i < 30 && picture.Phase == PicturePhase.Finding; i++) Frames(1);
        Frames(20);
        var puzzle = picture.Puzzle;
        if (puzzle == null)
        {
            Console.WriteLine("  !! no seen species gave a picture");
            return;
        }
        Console.WriteLine($"picture: {puzzle.Species} at {puzzle.Size}, {puzzle.PictureCount} cells");
        Shot("pz05_picture_blank");
        int size = puzzle.Size, filled = 0;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (!puzzle.InPicture(x, y)) continue;
                if (filled == puzzle.PictureCount / 2)
                {
                    // One mistake and a cross, then the half shot
                    for (int k = 0; k < size * size; k++)
                        if (!puzzle.InPicture(k % size, k / size)) { picture.PointAt(k); picture.Fill(); break; }
                    picture.PointAt(y * size + x);
                    Frames(10);
                    Shot("pz06_picture_half");
                }
                picture.PointAt(y * size + x);
                picture.Fill();
                filled++;
            }
        Frames(40);
        Shot("pz07_picture_solved");
        Console.WriteLine($"solved {puzzle.Solved}, mistakes {puzzle.Mistakes}, pays {picture.Outcome}");
        picture.Fill();
        Frames(5);
        Console.WriteLine($"records: pictures {string.Join(",", game.Cabinets.Pictures.Select(p => $"{p.Key}@{p.Value}"))}; coins {game.Pastimes.Coins.Coins}");
        game.State = GameState.Overworld;

        // ---- the clerk's prize counter
        At("LumioseCity_Arcade", 7, 5, Direction.Up);
        game.LocationSign.Hide();
        game.Interact(); Frames(3);
        for (int i = 0; i < 30; i++)
        {
            if (engine.Choice.IsOpen)
            {
                // PRIZES at the menu, then the list stands: the shot
                if (game.Dialogue.VisibleText.Contains("help")) { engine.Choice.Move(1); Frames(2); engine.Choice.Confirm(); Frames(8); continue; }
                Frames(10);
                Shot("pz09_prize_counter");
                break;
            }
            game.Dialogue.FinishLine(); Frames(2);
            game.Dialogue.Advance(); Frames(3);
        }
    }
}
