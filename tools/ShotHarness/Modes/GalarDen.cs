using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

partial class Harness
{
    /// <summary>
    /// The Mega Max den's raid played through the game (plan 33 · GL7, part c; <c>GALAR_FROM=den</c> alone, not part of the
    /// default <c>galar</c> run): the Hall of Fame entered and a Wishing Piece thrown in (the two flags the den and its beam
    /// ask), the player put down on the Lake of Outrage's south shore and walked into the den's trigger by the stick, as a
    /// player does (<see cref="ActionFrame.Stick"/> fed to the game each frame; no second way of starting the script), its
    /// lines read, the call answered yes, the raid's battle brought to its menu and a turn played. Prints what the den
    /// holds, the stick's walk and the battle's sides.
    /// </summary>
    void GalarDenRaid(PokemonPlatinumEngine.OpenWorld.OpenMap galar)
    {
        var box = game.Dialogue;
        game.Story.Set("GalarHallOfFame");
        game.Story.Set(ScriptRunner.DenFlag("outrage_mega", "WISHED"));
        game.Story.SetVar("VAR_GALAR_VERSION", 1);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Corviknight")!, 75));
        var start = OnLabGround(galar, 132, -1146, 90, 0);
        PutInLab(galar, start, 0, 0);
        game.LocationSign.Hide();
        Frames(30);
        Shot("ga96_den_beam");
        // The walk: the stick held down (down the screen is south, +z, as InputManager.GetStick has it) until the den's script has the field
        int frames = 0;
        for (; frames < 1200 && !engine.ScriptRunning; frames++)
        {
            InputManager.Feed(ActionFrame.Stick(0, 127));
            Frames(1);
        }
        InputManager.Feed(null);
        var field = game.OpenField!;
        Console.WriteLine($"  the stick's walk: {frames} frames, the player at {field.Player.Pos}, script {game.ScriptNow ?? "none"}");
        if (!engine.ScriptRunning) { Console.WriteLine("!! the den's trigger never started its script"); return; }
        string? shot = "ga97_den_lines";
        for (int i = 0; i < 160 && engine.ScriptRunning && game.State is GameState.Overworld or GameState.Dialogue; i++)
        {
            if (engine.Choice.IsOpen)
            {
                Frames(14);
                Shot("ga98_den_call_question");
                engine.Choice.Confirm(); Frames(4);
                continue;
            }
            if (!box.IsActive) { Frames(6); continue; }
            box.FinishLine(); Frames(2);
            if (shot != null && box.VisibleText.Contains("stirs")) { Shot(shot); shot = null; }
            if (box.IsQuestion) { Frames(4); continue; }
            box.Advance(); Frames(2);
        }
        for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Frames(1);
        if (game.Battle is not { } battle) { Console.WriteLine("!! the raid's battle never began"); return; }
        Console.WriteLine($"  the raid: foe {battle.EnemyPokemon.Species.Name} level {battle.EnemyPokemon.Level}, {battle.EnemySlots.Count} foe place(s), {battle.PlayerSlots.Count} of ours");
        ToMainMenu(battle);
        Shot("ga99_den_raid_menu");
        battle.SelectMove(0);
        Skip(2.5);
        Shot("ga99b_den_raid_turn");
    }
}
