using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- outbreaks (plan 38 · EX1; part of `explore`: EXPLORE_PART=outbreaks)

    // A Sinnoh game past its Hall of Fame on a day whose outbreak is in a place with tall grass, with the options' WILD
    // POKéMON row at VISIBLE: ten stand in the grass and about eight in ten are the outbreak's one species (ex01), one touched
    // is met and the battle is its (ex02); then Hoenn on a day of Surskit (ex03). It prints the day's number, the outbreaks, who
    // stands in sight and how many of them are the outbreak's, and the frame's time with the crowd standing.
    private void ExploreOutbreaks()
    {
        OutbreakShots("Sinnoh", RegionDatabase.Sinnoh, o => true, "ex01_outbreak_crowd", "ex02_outbreak_battle");
        OutbreakShots("Hoenn", RegionDatabase.Hoenn, o => o.Species == "Surskit", "ex03_hoenn_outbreak", null);
    }

    // The tall-grass tile of an area with the most tall grass round it, where a crowd has room to stand
    private static (int X, int Y)? ThickGrass(Map map, string key)
    {
        if (map.AreaBounds(key) is not { } b) return null;
        var area = map.FindArea(key);
        (int X, int Y)? best = null;
        int most = 0;
        for (int y = b.Y; y < b.Y + b.Height; y++)
            for (int x = b.X; x < b.X + b.Width; x++)
            {
                if (map.AreaAt(x, y) != area || !map.IsWalkable(x, y) || map.IsTallGrass(x, y)) continue;
                int grass = 0;
                for (int dy = -8; dy <= 8; dy++)
                    for (int dx = -11; dx <= 11; dx++)
                        if (map.AreaAt(x + dx, y + dy) == area && map.IsTallGrass(x + dx, y + dy) && map.IsWalkable(x + dx, y + dy)) grass++;
                if (grass > most) { most = grass; best = (x, y); }
            }
        return best;
    }

    private void OutbreakShots(string mapName, string regionId, Func<Outbreak, bool> pick, string crowdShot, string? battleShot)
    {
        engine.NewGameRegion = regionId;
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Party.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get(regionId == RegionDatabase.Sinnoh ? "Turtwig" : "Mudkip")!, 30));
        game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
        game.Story.Set(RegionDatabase.Get(regionId)!.StoryCompleteFlag);
        var map = MapDatabase.Get(mapName);

        // The first day whose outbreak is the one asked for and has tall grass to stand in
        uint daily = 0;
        Outbreak? outbreak = null;
        (int X, int Y)? stand = null;
        for (uint n = 1; n < 400 && stand == null; n++)
            foreach (var o in Outbreaks.Today(regionId, n).Where(pick))
                if (ThickGrass(map, o.Area) is { } spot) { daily = n; outbreak = o; stand = spot; break; }
        if (outbreak == null || stand is not { } at)
        {
            Console.WriteLine($"  !! {regionId}: no day within 400 has an outbreak to stand in");
            return;
        }
        game.Encounters.DailyNumber = daily;
        Console.WriteLine($"explore: {regionId} day {daily}: {string.Join(", ", Outbreaks.Today(regionId, daily).Select(o => $"{o.Species} at {o.Area} ({o.Share} in a hundred{(o.Level > 0 ? $", level {o.Level}" : "")})"))}; standing at {at.X},{at.Y}");

        engine.Settings.WildInSight = true;
        // Nobody's eyes on the player as they arrive: the shot is of the crowd, not of a trainer's challenge
        game.TrainersLookOnArrival = false;
        At(mapName, at.X, at.Y, Direction.Up);
        Console.WriteLine($"explore: in the outbreak's place {engine.OutbreakHere()?.Area ?? "(none)"}, the moment holds {engine.Drive.EncounterMomentNow().Outbreaks?.Count ?? 0} outbreaks");
        // Stood still, so that nothing is walked into, while they come out
        for (int f = 0; f < 60 * 40 && game.WildInSight.All.Count < FieldPokemon.MostInOutbreak; f++) { game.KeepWildInSight(1f / 60f); Frames(1); }
        Frames(30);
        int ofIt = game.WildInSight.All.Count(w => w.Pokemon.Species.Name == outbreak.Species);
        Console.WriteLine($"explore: {game.WildInSight.All.Count} stand in sight, {ofIt} of them {outbreak.Species}: "
            + string.Join(", ", game.WildInSight.All.Select(w => $"{w.Pokemon.DisplayName} Lv.{w.Pokemon.Level}")));
        if (game.WildInSight.All.Count > 0 && game.WildInSight.All[0].Pokemon.Moves is { Count: > 0 } moves && outbreak.Moves != null)
            Console.WriteLine($"explore: the first knows {string.Join(", ", moves.Select(m => m.Data.Name))}");
        Shot(crowdShot);
        Timing($"{regionId} outbreak crowd of {game.WildInSight.All.Count}", 240);

        if (battleShot != null && game.WildInSight.All.FirstOrDefault(w => w.Pokemon.Species.Name == outbreak.Species) is { } met)
        {
            Console.WriteLine($"explore: met {met.Pokemon.DisplayName} Lv.{met.Pokemon.Level}");
            game.MeetSeen(met);
            Frames(8);
            for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Frames(1);
            if (game.Battle is { } battle) ToMainMenu(battle);
            Shot(battleShot);
        }
        engine.Settings.WildInSight = false;
        game.State = GameState.Overworld;
    }
}
