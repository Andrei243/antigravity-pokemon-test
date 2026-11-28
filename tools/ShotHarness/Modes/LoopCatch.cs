using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // Plan 44 · SD10 (lp*; not part of `all`): the warden at her station west of Floaroma's flowerbeds (lp01), then the
    // first task's try on a fixed seed with the autopilot's hand on the disc: the brief (lp02), a line half drawn (lp03),
    // a strike winding up (lp04), a first loop counted (lp05) and the Pokemon calmed (lp06). It prints how the try stood
    // at each shot.
    public void LoopMode()
    {
        At("Sinnoh", 169, 662, Direction.Left);
        game.LocationSign.Hide();
        Frames(30);
        Shot("lp01_station");

        if (!game.OpenLoop("fallen_trunk", 7)) { Console.WriteLine("!! the loop catch didn't open"); return; }
        var screen = game.LoopScreen;
        // The sprite is baked behind the brief
        Frames(60);
        Shot("lp02_brief");
        screen.Confirm();
        screen.Autopilot = true;
        var run = screen.Game!;
        void Tell(string name)
        {
            Shot(name);
            Console.WriteLine($"{name}: tick {run.Ticks}, calm {run.Calm}, disc {run.Strength}, {run.Task.Species} at {run.At.X:F0},{run.At.Y:F0}, line {run.Line.Count}, {run.Outcome}");
        }
        bool Until(Func<bool> condition)
        {
            for (int i = 0; i < 60 * 180 && game.State == GameState.Loop && !condition(); i++) Frames(1);
            return condition();
        }
        if (Until(() => run.Line.Count > 24)) Tell("lp03_drawing");
        if (Until(() => run.WindingUp && run.WindUpShare > 0.6f)) Tell("lp04_strike");
        if (Until(() => screen.SinceLoop < 0.1f)) Tell("lp05_first_loop");
        if (Until(() => screen.Phase == LoopPhase.Over && screen.PhaseTime > LoopScreen.EndHold + 0.2f)) Tell("lp06_calmed");
        if (!screen.Calmed) Console.WriteLine("!! the Pokemon was not calmed");
        screen.Confirm();
        Frames(10);
        Console.WriteLine($"loop: left in {game.State}");
    }
}
