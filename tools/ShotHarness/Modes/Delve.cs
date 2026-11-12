using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Delve;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // Plan 44 · SD8, SD9 (dv*; not part of `all`): the delve camp's keeper in the Survival Area and her board (dv10, dv11),
    // then a run down Brinecave played by the plain policy through the game's own screen: the first floor (dv01), the team
    // trailing the leader (dv02), a strike (dv03), the moves' list (dv04), a resident down (dv05), a recruit asking (dv06),
    // the belly low (dv07), the stairs taken and the next floor laid from its seed (dv08), and Heartpeak's dark (dv09); then
    // each hollow's first floor (dv20 to dv23). It prints each action and what the rules said.
    public void DelveMode()
    {
        party.Clear();
        party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 60));
        party.Add(new Pokemon(PokemonDatabase.Get("Lucario")!, 60));
        game.Story.Set("SinnohHallOfFame");
        // The camp: the keeper beside Looker, and the board
        At("Sinnoh", 662, 343, Direction.Up);
        game.LocationSign.Hide();
        Frames(30);
        Shot("dv10_camp");
        game.OpenDelveBoard();
        Frames(20);
        Shot("dv11_board");
        foreach (var r in game.DelveBoardScreen.Requests) Console.WriteLine($"board: {r}");
        game.DelveBoardScreen.Close();
        game.State = GameState.Overworld;

        // ---- a run down Brinecave
        if (!game.OpenDelve("brinecave", 4242)) { Console.WriteLine("!! the delve didn't open"); return; }
        Frames(60);
        var run = game.Delve!;
        run.FixedRecruitRoll = 0;
        var screen = game.DelveScreen;
        Shot("dv01_first_floor");
        Console.WriteLine($"delve: seed {run.Seed}, floor {run.Floor.Index + 1}, leader {run.Leader.Name} at {run.Leader.X},{run.Leader.Y}, stairs {run.Floor.Stairs}");

        bool trail = false, strike = false, reach = false, down = false, asked = false, hunger = false, stairs = false;
        for (int n = 0; n < 4000 && run.Outcome == DelveOutcome.Going && !stairs; n++)
        {
            while (screen.Playing) Frames(1);
            var action = DelvePolicy.Next(run);
            if (!reach && action.Kind is DelveActionKind.Strike or DelveActionKind.Move)
            {
                screen.OpenMoves();
                Frames(10);
                Shot("dv04_reach");
                screen.Cancel();
                reach = true;
            }
            int events = run.Events.Count, floor = run.Floor.Index;
            var refused = game.DelveAct(action);
            Console.WriteLine($"turn {run.Turn}: {action.Kind} {action.Way} -> {refused?.ToString() ?? string.Join(", ", run.Events.Skip(events).Select(e => $"{e.Kind} {e.Who} {e.Target} {e.What} {e.Amount}"))}");
            var fresh = run.Events.Skip(events).ToList();
            if (!trail && run.Turn >= 3)
            {
                while (screen.Playing) Frames(1);
                Frames(6);
                Shot("dv02_trail");
                trail = true;
            }
            if (!strike && fresh.Any(e => e.Kind is DelveEventKind.Damaged && run.Team.Any(m => m.Name == e.Who)))
            {
                Frames(8);
                Shot("dv03_strike");
                strike = true;
            }
            if (!down && fresh.Any(e => e.Kind == DelveEventKind.Fainted && run.Team.All(m => m.Name != e.Who)))
            {
                Frames(14);
                Shot("dv05_foe_down");
                down = true;
            }
            if (!asked && run.Asking != null)
            {
                while (screen.Playing) Frames(1);
                Frames(6);
                Shot("dv06_recruit");
                asked = true;
            }
            if (!hunger && run.Turn > 12)
            {
                run.Hunger = 14;
                while (screen.Playing) Frames(1);
                Frames(4);
                Shot("dv07_hunger");
                hunger = true;
            }
            if (run.Floor.Index != floor)
            {
                Frames(70);
                Shot("dv08_stairs");
                Console.WriteLine($"delve: floor {run.Floor.Index + 1}, seed {run.Floor.Seed}, stairs {run.Floor.Stairs}");
                stairs = true;
            }
        }
        Console.WriteLine($"delve: trail {trail} strike {strike} reach {reach} down {down} asked {asked} hunger {hunger} stairs {stairs}");
        // ---- the dark of Heartpeak (the camp's rank raised by hand)
        LeaveDelve();
        game.DelveRecord.Clears["brinecave"] = 1;
        game.DelveRecord.Clears["rootwood"] = 1;
        foreach (var (key, shot) in new[] { ("brinecave", "dv20_brinecave"), ("rootwood", "dv21_rootwood"), ("heartpeak", "dv22_heartpeak"), ("old_tower", "dv23_old_tower") })
        {
            if (key == "old_tower") game.DelveRecord.Clears["heartpeak"] = 1;
            if (!game.OpenDelve(key, 77)) { Console.WriteLine($"!! {key} didn't open"); continue; }
            Frames(70);
            Shot(shot);
            if (key == "heartpeak") Shot("dv09_dark");
            LeaveDelve();
        }
    }

    void LeaveDelve()
    {
        if (game.Delve is { } run)
        {
            game.DelveAct(DelveAction.Wait);
            run.Escape();
        }
        for (int i = 0; i < 600 && game.State != GameState.Overworld; i++)
        {
            game.DelveScreen.Hurry();
            Frames(1);
        }
        Frames(10);
    }
}
