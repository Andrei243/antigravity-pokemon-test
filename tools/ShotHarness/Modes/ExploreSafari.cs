using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- the Johto Safari Zone's objects (plan 38 · EX8; EXPLORE_PART=safari)

    // A Johto game whose player is the Safari Zone's Owner: the Area Customizer's screen over the Peak (ex80), one of each
    // object set down in the Peak twenty days ago and seen in the zone (ex81), and the grass's slot 0 the objects opened met
    // at its battle's menu (ex82). It prints the objects, the points and the slots each way opened.
    private void ExploreSafari()
    {
        engine.NewGameRegion = RegionDatabase.Johto;
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.TrainersLookOnArrival = false;
        game.Story.SetVar(SafariObjects.OwnerVariable, 6);
        game.Party.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Quagsire")!, 50));

        const string peak = "safari_zone_04";
        var zone = MapDatabase.Get(SafariObjects.ZoneMap);
        var bounds = zone.AreaBounds(peak)!.Value;
        int today = ApricornBox.DayOf(GameClock.Today);

        // One of each object, spaced over the Peak's floor near its middle
        var placed = new List<PlacedSafariObject>();
        var taken = new HashSet<(int, int)>();
        int cx = bounds.X + bounds.Width / 2, cy = bounds.Y + bounds.Height / 2;
        var tiles = Enumerable.Range(bounds.Y, bounds.Height).SelectMany(y => Enumerable.Range(bounds.X, bounds.Width).Select(x => (x, y)))
            .OrderBy(t => Math.Abs(t.x - cx) + Math.Abs(t.y - cy)).ThenBy(t => t.y).ThenBy(t => t.x).ToList();
        foreach (var type in SafariObjects.Catalogue)
            foreach (var (x, y) in tiles)
            {
                bool fits = true;
                for (int dy = -1; dy <= type.Depth && fits; dy++)
                    for (int dx = -1; dx <= type.Width && fits; dx++)
                        if (taken.Contains((x + dx, y + dy))) fits = false;
                for (int dy = 0; dy < type.Depth && fits; dy++)
                    for (int dx = 0; dx < type.Width && fits; dx++)
                        if (!SafariObjects.FloorAt(zone, bounds, x + dx, y + dy)) fits = false;
                if (!fits) continue;
                for (int dy = 0; dy < type.Depth; dy++)
                    for (int dx = 0; dx < type.Width; dx++) taken.Add((x + dx, y + dy));
                placed.Add(new PlacedSafariObject { Type = type.Id, X = x, Y = y, Day = today - 20 });
                break;
            }
        // and five haystacks more, so the plains reach Zangoose's twelve
        foreach (var (x, y) in tiles.Where(t => !taken.Contains(t) && SafariObjects.FloorAt(zone, bounds, t.x, t.y)).Take(5))
            placed.Add(new PlacedSafariObject { Type = SafariObjects.Catalogue.Single(t => t.Key == "haystack").Id, X = x, Y = y, Day = today - 20 });
        game.SafariObjects.Set(peak, placed);
        foreach (var kind in Enum.GetValues<SafariObjectKind>())
            Console.WriteLine($"explore: the Peak's {kind} points on day {today}: {SafariObjects.Points(placed, kind, today)}");

        // ex80: the Area Customizer over the Peak
        At(SafariObjects.ZoneMap, cx, cy + 3, Direction.Up);
        Frames(20);
        game.ArrangeSafari(3);
        Frames(20);
        Shot("ex80_customizer");
        game.Decoration.Close();
        Frames(4);
        game.State = GameState.Overworld;

        // ex81: the objects standing in the Peak
        game.LayOutSafariObjects();
        At(SafariObjects.ZoneMap, cx, cy + 4, Direction.Up);
        Frames(40);
        Shot("ex81_objects_placed");

        // ex82: the grass's table with the open slots, its slot 0 met
        var moment = game.EncounterMomentNow();
        var area = zone.Areas.Single(a => a.Key == peak);
        var land = EncounterSlots.Grass(area, moment, area.WildEncounters);
        Console.WriteLine($"explore: the Peak's grass now: {string.Join(", ", land.Select(e => $"{e.SpeciesName} {e.MinLevel}"))}");
        Console.WriteLine($"explore: open slots: {string.Join(", ", area.SafariSlots.Where(s => SafariObjects.Opens(s, placed, today)).Select(s => $"{s.Way} {s.Species}"))}");
        game.StartWildBattle(land[0]);
        for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Frames(1);
        if (game.Battle is { } battle)
        {
            ToMainMenu(battle);
            Shot("ex82_slot_met");
            Console.WriteLine($"explore: met {battle.EnemyPokemon.Species.Name} level {battle.EnemyPokemon.Level}");
        }
        else Console.WriteLine("  !! no battle began");
    }
}
