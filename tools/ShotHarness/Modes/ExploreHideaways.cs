using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Underground;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- hideaways (plan 38 · EX6; part of `explore`: EXPLORE_PART=hideaways)

    // A Sinnoh Champion below ground before the Fern Grotto's way down (ex50), then each hideaway named in HIDEAWAYS (by
    // number, "1,2" unless set; "all" shoots the twelve) from just inside its way out, its Pokémon of the day standing about
    // in it (ex51 on, a shot a hideaway). It prints each hideaway's layout, its day's twelve and who stands in sight.
    private void ExploreHideaways()
    {
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Party.Clear();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Infernape")!, 60));
        game.Story.Set(Hideaways.OpenedBy);
        // (Roark has met this Champion below ground already)
        game.Story.SetVar("VAR_HAS_SEEN_UNDERGROUND_ROARK_INTRO", 1);
        game.TrainersLookOnArrival = false;

        var first = Hideaways.All[0];
        At("Underground", first.DownX, first.DownZ + 2, Direction.Up);
        game.ArriveOnMap();
        Frames(40);
        Shot("ex50_way_down");
        Console.WriteLine($"explore: the way down to the {first.Name} at {first.DownX},{first.DownZ}: {MapDatabase.Get("Underground").GetGroundTile(first.DownX, first.DownZ)}, solid {MapDatabase.Get("Underground").IsSolid(first.DownX, first.DownZ)}");

        string asked = Environment.GetEnvironmentVariable("HIDEAWAYS") ?? "1,2";
        var which = asked == "all" ? Hideaways.All : Hideaways.All.Where(h => asked.Split(',').Contains(h.Number.ToString())).ToList();
        foreach (var h in which)
        {
            At(h.Map, Hideaways.ArriveX, Hideaways.ArriveY, Direction.Down);
            game.ArriveOnMap();
            for (int f = 0; f < 60 * 30 && game.WildInSight.All.Count < FieldPokemon.MostInView; f++) { game.KeepWildInSight(1f / 60f); Frames(1); }
            Frames(30);
            string shot = $"ex{50 + h.Number}_hideaway_{h.Map.Replace("Hideaway", "").ToLowerInvariant()}";
            Shot(shot);
            var map = MapDatabase.Get(h.Map);
            Console.WriteLine($"{shot}: layout {string.Join(' ', Hideaways.Lay(h).Layout)}, the day's twelve {string.Join(", ", map.WildEncounters.Select(e => e.SpeciesName))}");
            Console.WriteLine($"   in sight: {string.Join(", ", game.WildInSight.All.Select(w => $"{w.Pokemon.Species.Name} at {w.Body.GridX},{w.Body.GridY}"))}");
        }
    }
}
