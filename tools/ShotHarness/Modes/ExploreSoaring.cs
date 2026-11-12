using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- soaring (plan 38 · EX7, part a; part of `explore`: EXPLORE_PART=soaring)

    // A Sinnoh Champion with a Staraptor that knows Fly, in Jubilife City with most towns come to: Fly's choice with SOAR on
    // the item button (ex70a), the take-off (ex70), over a town (ex71), over Snowpoint City's beacon (ex72), the same after
    // dark (ex73), and landed there through Fly's own way (ex74). It prints the flight's place, height and the beacon under it.
    private void ExploreSoaring()
    {
        engine.NewGameRegion = RegionDatabase.Sinnoh;
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Story.Set("SinnohHallOfFame");
        foreach (string town in new[] { "twinleaf_town", "sandgem_town", "jubilife_city", "oreburgh_city", "floaroma_town", "eterna_city", "hearthome_city", "solaceon_town", "snowpoint_city" })
            if (SpawnLocations.ArrivedIn(town) is { } spawn) game.Story.Set(spawn.ArrivalFlag);
        var flier = new Pokemon(PokemonDatabase.Get("Staraptor")!, 50);
        flier.Moves.Clear();
        flier.Moves.Add(new Move(MoveDatabase.Get("Fly")!));
        game.Party.Add(flier);
        var before = engine.Settings.TimeOfDay;
        engine.Settings.TimeOfDay = TimeOfDay.Day; engine.ApplySettings(window: false);
        try
        {
            At("Sinnoh", 647, 431, Direction.Up);
            Frames(10);
            game.OpenFlyChoice(flier);
            Frames(30);
            Shot("ex70a_fly_choice_soar");
            Console.WriteLine($"ex70a_fly_choice_soar: SOAR offered {game.TownMapScreen.CanSoar} on tab {game.TownMapScreen.Tab}");
            game.TownMapScreen.AskSoar();
            for (int i = 0; i < 240 && (game.State != GameState.Soaring || game.SoarScreen.Flight == null); i++) Frames(1);
            // The models made behind the fade, then a second of flight
            for (int i = 0; i < 600 && !PokemonPlatinumEngine.Graphics.PokemonModels.TryGet(flier.LookName, out _); i++) Frames(1);
            for (int i = 0; i < 600 && PokemonPlatinumEngine.Graphics.CharacterModels.TryGet(PlayerIdentity.Character, game.RenderContext.Shaders) == null; i++) Frames(1);
            Frames(60);
            var flight = game.SoarScreen.Flight!;
            void Tell(string shot) => Console.WriteLine($"{shot}: at {flight.X:0.00},{flight.Y:0.00} heading {flight.Heading:0} height {flight.Height:0.0}, beacon under {game.SoarScreen.Under?.Town.Area ?? "none"}, {flight.Beacons.Count()} beacons, in a cloud {game.SoaringInCloud}");
            Shot("ex70_soar_takeoff");
            Tell("ex70_soar_takeoff");

            // Over Hearthome City, low, turning east
            var map = flight.Map;
            var hearthome = map.Spots.First(s => s.Town.Area == "hearthome_city").Spot.Cells[0];
            flight.Place(hearthome.X + 0.5f, hearthome.Y + 2.6f, 20f);
            for (int i = 0; i < 40; i++) game.SoarScreen.Steer(0.6f, -1f, false, 1f / 30f);
            flight.Place(hearthome.X + 0.5f, hearthome.Y + 2.6f, 20f);
            Frames(2);
            Shot("ex71_soar_over_a_town");
            Tell("ex71_soar_over_a_town");

            // Over Snowpoint City's beacon, high
            var snowpoint = map.Spots.First(s => s.Town.Area == "snowpoint_city").Spot.Cells[0];
            for (int i = 0; i < 90; i++) game.SoarScreen.Steer(0f, 1f, false, 1f / 30f);
            flight.Place(snowpoint.X + 0.5f, snowpoint.Y + 0.5f, 180f);
            Frames(2);
            flight.Place(snowpoint.X + 0.5f, snowpoint.Y + 0.5f, 180f);
            Frames(1);
            Shot("ex72_soar_beacon");
            Tell("ex72_soar_beacon");

            engine.Settings.TimeOfDay = TimeOfDay.Night; engine.ApplySettings(window: false);
            flight.Place(snowpoint.X + 0.5f, snowpoint.Y + 0.5f, 180f);
            Frames(2);
            flight.Place(snowpoint.X + 0.5f, snowpoint.Y + 0.5f, 180f);
            Frames(1);
            Shot("ex73_soar_night");
            Tell("ex73_soar_night");
            engine.Settings.TimeOfDay = TimeOfDay.Day; engine.ApplySettings(window: false);

            // A lands over the beacon, through Fly's own way
            flight.Place(snowpoint.X + 0.5f, snowpoint.Y + 0.5f, 180f);
            game.SoarScreen.Confirm();
            for (int i = 0; i < 900 && (game.State != GameState.Overworld || engine.ScriptRunning); i++)
            {
                if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
                Frames(1);
            }
            Frames(30);
            Shot("ex74_soar_landed");
            Console.WriteLine($"ex74_soar_landed: on {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}, the area {game.Map.AreaAt(game.Player.GridX, game.Player.GridY)?.Key}");
        }
        finally
        {
            engine.Settings.TimeOfDay = before; engine.ApplySettings(window: false);
        }
    }

    // ---------------------------------------------------------------- the places of the day (plan 38 · EX7, part b; EXPLORE_PART=skyplaces)

    // A Sinnoh Champion soaring on a fixed day: a place of the day's beacon in the sky (ex75), then one place of each kind
    // landed on through the game's own landing (ex76 the islet, ex77 the grove, ex78 the hollow, ex79 the peak). It prints the
    // places lit, each place's table and finds.
    private void ExploreSkyPlaces()
    {
        engine.NewGameRegion = RegionDatabase.Sinnoh;
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Story.Set("SinnohHallOfFame");
        foreach (string town in new[] { "twinleaf_town", "jubilife_city", "hearthome_city", "snowpoint_city" })
            if (SpawnLocations.ArrivedIn(town) is { } spawn) game.Story.Set(spawn.ArrivalFlag);
        var flier = new Pokemon(PokemonDatabase.Get("Staraptor")!, 50);
        flier.Moves.Clear();
        flier.Moves.Add(new Move(MoveDatabase.Get("Fly")!));
        game.Party.Add(flier);
        var beforeDate = GameClock.FixedDate;
        GameClock.FixedDate = new DateTime(2026, 11, 8);
        var regionMap = RegionMap.Of("Sinnoh")!;
        try
        {
            var lit = SkyPlaces.Lit(regionMap, GameClock.Today);
            Console.WriteLine($"explore: {GameClock.Today:yyyy-MM-dd}, places lit: {string.Join(", ", lit.Select(p => $"{p.Kind} {p.Index} at {p.X},{p.Y}"))}");
            At("Sinnoh", 647, 431, Direction.Up);
            Frames(10);
            game.OpenFlyChoice(flier);
            Frames(4);
            game.TownMapScreen.AskSoar();
            for (int i = 0; i < 240 && (game.State != GameState.Soaring || game.SoarScreen.Flight == null); i++) Frames(1);
            for (int i = 0; i < 600 && !PokemonPlatinumEngine.Graphics.PokemonModels.TryGet(flier.LookName, out _); i++) Frames(1);
            var flight = game.SoarScreen.Flight!;
            var first = lit[0];
            flight.Place(first.X + 0.5f, first.Y + 2.8f, 0f);
            Frames(2);
            flight.Place(first.X + 0.5f, first.Y + 0.5f, 0f);
            Frames(1);
            Shot("ex75_place_beacon");
            Console.WriteLine($"ex75_place_beacon: over {flight.PlaceUnder?.Kind} {flight.PlaceUnder?.Index}, {flight.Places.Count} places lit");
            game.SoarScreen.Close();
            game.State = GameState.Overworld;

            string[] names = { "ex76_place_islet", "ex77_place_grove", "ex78_place_hollow", "ex79_place_peak" };
            foreach (var kind in Enum.GetValues<SkyPlaceKind>())
            {
                var place = regionMap.SkyPlaces.First(p => p.Kind == kind);
                game.LandOnASkyPlace(place);
                for (int i = 0; i < 240 && (game.State != GameState.Overworld || game.Map != game.SkyPlaceMap); i++) Frames(1);
                game.LocationSign.Hide();
                Frames(40);
                string shot = names[(int)kind];
                Shot(shot);
                var table = SkyPlaces.Table(place, GameClock.Today);
                Console.WriteLine($"{shot}: {game.Map.Name} at {game.Player.GridX},{game.Player.GridY}; table {string.Join(", ", table.Select(t => $"{t.SpeciesName} {t.MinLevel}-{t.MaxLevel}"))}; finds {string.Join(", ", SkyPlaces.FindsOf(place, GameClock.Today).Select(f => $"{f.Item} {f.X},{f.Y}"))}");
            }
        }
        finally
        {
            GameClock.FixedDate = beforeDate;
        }
    }
}
