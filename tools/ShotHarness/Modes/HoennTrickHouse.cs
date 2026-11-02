using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// The Trick House's later puzzles (plan 21 · H12, part j), after part i's shots at the end of the <c>hoenn</c> mode
    /// (<c>HOENN_FROM=postgame</c>), or alone (<c>HOENN_FROM=trickhouse</c>): the front room with the Trick Master hiding
    /// behind the potted tree and found there (<c>h2f0</c>, <c>h2f1</c>), the second puzzle's holes all open and two of them
    /// covered (<c>h2f2</c>, <c>h2f3</c>), the third's doors as it is come into and swapped (<c>h2f4</c>, <c>h2f5</c>), the
    /// fourth's boulders (<c>h2f6</c>), a Mechadoll's question (<c>h2f7</c>), the sixth's gates (<c>h2f8</c>), the seventh's
    /// arrows and statues before and after the green switch (<c>h2f9</c>, <c>h2fa</c>), the eighth's slippery floor
    /// (<c>h2fb</c>) and the last room's reward (<c>h2fc</c>). It prints the swaps laid and what each left.
    /// </summary>
    void HoennTrickHouseShots()
    {
        if (!HoennReady("the Trick House")) return;
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void ReadOn()
        {
            for (int i = 0; i < 1800 && (engine.ScriptRunning || game.State != GameState.Overworld); i++)
            {
                if (game.State == GameState.Battle) return;
                if (engine.Choice.IsOpen) { Frames(10); engine.Choice.Confirm(); Frames(4); }
                else if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
                else Frames(1);
            }
            Frames(6);
        }
        void Swapped(string map) =>
            Console.WriteLine($"  {map}: " + string.Join(", ", PokemonPlatinumEngine.Data.MapDatabase.Get(map).SwappedPlaces
                .Where(p => p.Swapped).Select(p => $"{p.Var}={p.Value} laid")));
        void Said(string label) =>
            Console.WriteLine($"  {label}: level {game.Story.Var("VAR_TRICK_HOUSE_LEVEL")}, state {game.Story.Var("VAR_TRICK_HOUSE_ENTRANCE_STATE")}, " +
                $"way {(game.Story.Has("FLAG_HOENN_TRICK_HOUSE_OPEN") ? "open" : "shut")}");

        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        Box().Close();

        // ---- The front room: the Trick Master behind the potted tree, waiting for the second puzzle's Badge
        game.Story.SetVar("VAR_TRICK_HOUSE_LEVEL", 1);
        game.Story.SetVar("VAR_TRICK_HOUSE_ENTRANCE_STATE", 3);
        game.Story.Unset("FLAG_HOENN_TRICK_HOUSE_OPEN");
        game.Story.GiveBadge("Hoenn", "Dynamo");
        At("Route110_TrickHouseEntrance", 10, 5, Direction.Right);
        game.ArriveOnMap(); Frames(20);
        ReadOn();
        Said("as the room is come into");
        Shot("h2f0_trick_house_hiding");
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "the find");
        for (int i = 0; i < 4; i++) { Frames(30); Box().FinishLine(); Frames(2); if (Box().IsActive) Box().Advance(); Frames(4); }
        Frames(30); Box().FinishLine(); Frames(4);
        Shot("h2f1_trick_house_found");
        ReadOn();
        Said("found");

        // ---- The second puzzle: the holes open, then two of them covered
        game.Story.SetVar("VAR_TEMP_5", 0);
        At("Route110_TrickHousePuzzle2", 6, 13, Direction.Up); Frames(20);
        Swapped("Route110_TrickHousePuzzle2");
        Shot("h2f2_trick_house_holes_open");
        game.Story.SetVar("VAR_TEMP_5", 5);
        At("Route110_TrickHousePuzzle2", 6, 14, Direction.Up); Frames(20);
        Swapped("Route110_TrickHousePuzzle2");
        Shot("h2f3_trick_house_holes_covered");

        // ---- The third: red doors shut, then a button pressed and the doors swapped
        game.Story.SetVar("VAR_TEMP_7", 0);
        At("Route110_TrickHousePuzzle3", 3, 10, Direction.Up); Frames(20);
        Shot("h2f4_trick_house_doors");
        game.Story.SetVar("VAR_TEMP_7", 7);
        At("Route110_TrickHousePuzzle3", 3, 11, Direction.Up); Frames(20);
        Swapped("Route110_TrickHousePuzzle3");
        Shot("h2f5_trick_house_doors_swapped");

        // ---- The fourth: the boulders
        At("Route110_TrickHousePuzzle4", 7, 10, Direction.Up); Frames(20);
        Shot("h2f6_trick_house_boulders");

        // ---- The fifth: a Mechadoll's question
        game.Story.SetVar("VAR_TEMP_1", 0);
        // (into the first doll's corridor, the row west of it)
        At("Route110_TrickHousePuzzle5", 4, 18, Direction.Up); Frames(10);
        engine.Steering = (Direction.Up, false); Frames(20); engine.Steering = null;
        // (its call first, then the question)
        for (int i = 0; i < 1200 && !engine.Choice.IsOpen; i++)
        {
            if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Frames(2); }
            else Frames(1);
        }
        Frames(30);
        Shot("h2f7_trick_house_mechadoll");
        ReadOn();
        Console.WriteLine($"  the quiz: VAR_TEMP_1 {game.Story.Var("VAR_TEMP_1")}, on {game.Map.Name} at ({game.Player.GridX}, {game.Player.GridY})");

        // ---- The sixth: the gates
        At("Route110_TrickHousePuzzle6", 9, 12, Direction.Up); Frames(20);
        Console.WriteLine($"  the gates: {game.Map.Puzzle?.Name}");
        Shot("h2f8_trick_house_gates");

        // ---- The seventh: the arrows and the statues, and the green switch
        At("Route110_TrickHousePuzzle7", 5, 9, Direction.Up); Frames(20);
        Shot("h2f9_trick_house_arrows");
        At("Route110_TrickHousePuzzle7", 6, 7, Direction.Up); Frames(4);
        engine.Steering = (Direction.Up, false); Frames(20); engine.Steering = null;
        ReadOn(); Frames(30);
        var statue = game.Map.NPCs.FirstOrDefault(n => n.Key == "trick_house_statue_2");
        Console.WriteLine($"  the green switch: the statue at ({statue?.GridX}, {statue?.GridY})");
        Shot("h2fa_trick_house_arrows_turned");

        // ---- The eighth: the slippery floor
        At("Route110_TrickHousePuzzle8", 7, 12, Direction.Up); Frames(20);
        Shot("h2fb_trick_house_ice");

        // ---- The last room: the reward
        game.Story.SetVar("VAR_TRICK_HOUSE_LEVEL", 2);
        game.Story.SetVar("VAR_TRICK_HOUSE_ENTRANCE_STATE", 1);
        At("Route110_TrickHouseEnd", 5, 5, Direction.Left);
        game.ArriveOnMap(); Frames(20);
        game.Interact(); Frames(3);
        Until(() => Box().IsActive, "the reward");
        Frames(30); Box().FinishLine(); Frames(4);
        Shot("h2fc_trick_house_reward");
        ReadOn();
        Said("the reward");
    }
}
