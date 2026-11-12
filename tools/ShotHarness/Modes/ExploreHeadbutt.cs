using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- Headbutt trees (plan 38 · EX4; part of `explore`: EXPLORE_PART=headbutt)

    // A Johto game with a Slowpoke that knows Headbutt, facing Route 29's first tree from the south: the question (ex30),
    // the tree shaken (ex31) and, the first time something falls out, its battle at the menu (ex32). It prints each try.
    private void ExploreHeadbutt()
    {
        engine.NewGameRegion = RegionDatabase.Johto;
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.TrainersLookOnArrival = false;
        game.Party.Clear();
        var knower = new Pokemon(PokemonDatabase.Get("Slowpoke")!, 30);
        knower.Moves.Clear();
        knower.Moves.Add(new Move(MoveDatabase.Get("Headbutt")!));
        knower.Moves.Add(new Move(MoveDatabase.Get("Water Gun")!));
        game.Party.Add(knower);

        bool questionShot = false, shakeShot = false;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            At("Johto", 588, 398, Direction.Up);
            Frames(10);
            game.Interact();
            for (int i = 0; i < 400 && game.State != GameState.Battle && (engine.ScriptRunning || engine.Choice.IsOpen || game.State == GameState.Dialogue); i++)
            {
                var box = game.Dialogue;
                if (engine.Choice.IsOpen)
                {
                    Frames(14);
                    if (!questionShot) { Shot("ex30_headbutt_question"); questionShot = true; }
                    engine.Choice.Confirm(); Frames(4);
                    continue;
                }
                if (box.IsActive)
                {
                    box.FinishLine(); Frames(2);
                    if (box.IsQuestion) { Frames(4); continue; }
                    box.Advance(); Frames(2);
                    continue;
                }
                Frames(1);
                if (!shakeShot && questionShot && !box.IsActive && game.State == GameState.Overworld) { Frames(8); Shot("ex31_headbutt_shake"); shakeShot = true; }
            }
            for (int i = 0; i < 200 && game.State != GameState.Battle && engine.ScriptRunning; i++) Frames(1);
            if (game.State == GameState.Battle && game.Battle is { } battle)
            {
                ToMainMenu(battle);
                Shot("ex32_headbutt_battle");
                Console.WriteLine($"explore: try {attempt + 1}, {battle.EnemyPokemon.Species.Name} level {battle.EnemyPokemon.Level} fell out of the tree");
                return;
            }
            Console.WriteLine($"explore: try {attempt + 1}, nothing fell out");
        }
        Console.WriteLine("  !! nothing fell out of the tree in eight tries");
    }
}
