using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- the day's report, glints and rifts (plan 38 · EX2, EX3, EX5)

    // Part of `explore` (EXPLORE_PART=report). A Sinnoh game past its Hall of Fame with the National Pokédex, on a fixed Sunday:
    // the Town Map turned to its report (ex10), a scan made and the visitor on the report (ex12), a hidden item glinting two tiles
    // off (ex23), and an hour's rift on the report and in its ground (ex44, ex41). It prints what the report holds, the visitor
    // and the rift with their places, and the hidden items within reach.
    private void ExploreReport()
    {
        engine.NewGameRegion = RegionDatabase.Sinnoh;
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Story.Set("SinnohHallOfFame");
        game.Pokedex.UnlockNational();
        var before = (GameClock.FixedDate, GameClock.Fixed);
        GameClock.FixedDate = new DateTime(2026, 11, 8);
        engine.RiftsStayShut = false;
        try
        {
            // A day and a time of day whose hour has a rift
            Rift? rift = null;
            for (uint daily = 1; daily < 60 && rift == null; daily++)
                foreach (var time in new[] { TimeOfDay.Day, TimeOfDay.Morning })
                {
                    game.Encounters.DailyNumber = daily;
                    GameClock.Fixed = time;
                    if ((rift = engine.RiftNow()) != null) break;
                }
            Console.WriteLine($"explore: day {game.Encounters.DailyNumber}, hour {(int)GameClock.Hour}: outbreaks {string.Join(", ", Outbreaks.Today("Sinnoh", game.Encounters.DailyNumber).Select(o => $"{o.Species} at {o.Area}"))}; rift {(rift == null ? "none" : $"at {rift.Area} {rift.X},{rift.Y}, finds {string.Join(", ", rift.Finds.Select(f => $"{f.Item} {f.X},{f.Y}"))}")}");

            // ---- The report, then a scan
            At("Sinnoh", 647, 431, Direction.Up);
            game.OpenTownMap(TownMapMode.Item);
            Frames(20);
            game.TownMapScreen.ToggleReport();
            Frames(20);
            Shot("ex10_report");
            Console.WriteLine($"ex10_report: scan {game.TownMapScreen.Report?.Scan}, {game.TownMapScreen.Report?.Outbreaks.Count} outbreaks");
            game.TownMapScreen.Confirm();
            Frames(4);
            Frames(30);
            Shot("ex12_scan");
            var visitor = game.TownMapScreen.Report?.Visitor;
            Console.WriteLine($"ex12_scan: visitor {(visitor == null ? "none" : $"{visitor.Species} at {visitor.Area}, {visitor.SecondsLeft:0} s")}; rift on the report {game.TownMapScreen.Report?.Rift?.Area ?? "none"}");
            game.TownMapScreen.Cancel();
            Frames(6);
            game.State = GameState.Overworld;

            // ---- A rift's ground, and its finds glinting
            if (rift != null)
            {
                At("Sinnoh", rift.X, rift.Y + 2, Direction.Up);
                Frames(150);
                Shot("ex41_rift_ground");
                Console.WriteLine($"ex41_rift_ground: the moment's rift table {(engine.Drive.EncounterMomentNow().Rift != null ? "in force" : "not here")}; hidden items within reach {string.Join(", ", Glints.Near(game.Map, game.Player.GridX, game.Player.GridY, game.Story))}");
            }
        }
        finally
        {
            (GameClock.FixedDate, GameClock.Fixed) = before;
            engine.RiftsStayShut = true;
        }
    }
}
