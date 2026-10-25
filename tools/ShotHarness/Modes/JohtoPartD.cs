partial class Harness
{
    /// <summary>
    /// Plan 20 · J13, part d, after part c in the <c>johto</c> mode (so nothing before it moves; <c>JOHTO_FROM=partd</c>
    /// runs it alone in a new game of its own): Route 40's gate to the Battle Frontier. The gate with its guide before
    /// the shut door, his word before Johto's Hall of Fame (<c>jpd01</c>, <c>jpd02</c>), his question once it is entered,
    /// the road over the dark and the arrival on Sinnoh's grounds (<c>jpd03</c> to <c>jpd05</c>), the Johto guide on the
    /// grounds and the way back to Route 40 (<c>jpd06</c>, <c>jpd07</c>). It prints the region, the tile, the team and the
    /// Battle Points at each end. Written, not run.
    /// </summary>
    void JohtoPartDShots()
    {
        game.State = GameState.Overworld;
        game.Player.SetMode(TravelMode.OnFoot);
        var box = game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadTo(string? text, int most = 2400)
        {
            int idle = 0;
            for (int i = 0; i < most; i++)
            {
                if (text != null && box.IsActive && Translated.Saying(box.CurrentLine, text)) { Until(() => box.IsActive, "text"); box.FinishLine(); Frames(2); Console.WriteLine($"  said: {text}"); return; }
                if (text != null && !engine.ScriptRunning && !box.IsActive && game.State == GameState.Overworld) { if (++idle > 60) break; }
                else idle = 0;
                if (text == null && !engine.ScriptRunning && !box.IsActive && game.State == GameState.Overworld) return;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (box.IsActive && box.IsCurrentLineComplete && !box.IsQuestion) { box.Advance(); Frames(2); }
                else Frames(1);
            }
            Console.WriteLine($"  !! never said: {text ?? "the script's end"}");
        }
        void Tell(string where) => Console.WriteLine($"  {where}: {game.Map.Name} {game.Player.GridX},{game.Player.GridY} in {engine.Here.Id}, "
            + $"team {string.Join(", ", game.Party.Members.Select(p => $"{p.Species.Name} ({p.Region})"))}, {game.Frontier.BattlePoints} BP");

        game.Party.Members.Clear();
        foreach (string species in new[] { "Typhlosion", "Ampharos", "Gyarados" }) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 60));
        bool champion = game.Story.Has("JohtoHallOfFame");

        // ---- The gate, and the guide's word before the Hall of Fame
        game.Story.Unset("JohtoHallOfFame");
        At("Route40_FrontierGate", 5, 6, Direction.Up); Frames(10); Shot("jpd01_the_frontier_gate");
        At("Route40_FrontierGate", 5, 4, Direction.Up); game.Interact();
        ReadTo("Only Trainers who have entered the Hall of Fame"); Frames(10); Shot("jpd02_not_yet");
        ReadTo(null);

        // ---- A Champion: the question, the road over the dark, the grounds
        game.Story.Set("JohtoHallOfFame");
        game.Frontier.GivePoints(10);
        Tell("before the road");
        At("Route40_FrontierGate", 5, 4, Direction.Up); game.Interact();
        ReadTo("Its five halls keep one record");
        Until(() => engine.Choice.IsOpen, "the guide's question", 300);
        Frames(10); Shot("jpd03_the_guides_question");
        ReadTo("followed the guide along the road"); Frames(10); Shot("jpd04_the_road_over_the_dark");
        Until(() => game.Map.Name == "BattleFrontier" && game.State == GameState.Overworld && !engine.ScriptRunning, "the grounds", 900);
        Frames(30); Shot("jpd05_on_the_frontiers_grounds");
        Tell("on the grounds");

        // ---- The Johto guide on the grounds, and back to Route 40
        At("BattleFrontier", 47, 51, Direction.Left); game.Interact();
        ReadTo("Shall I take you back"); Frames(10); Shot("jpd06_the_johto_guide");
        ReadTo("followed the guide back along the road");
        Until(() => game.Map.Name == "Route40_FrontierGate" && game.State == GameState.Overworld && !engine.ScriptRunning, "Route 40's gate", 900);
        Frames(30); Shot("jpd07_back_in_the_gate");
        Tell("back on Route 40");
        if (!champion) game.Story.Unset("JohtoHallOfFame");
    }
}
