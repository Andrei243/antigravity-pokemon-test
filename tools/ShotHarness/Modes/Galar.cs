using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

partial class Harness
{
    /// <summary>
    /// Galar on the open world (plan 33; not part of <c>all</c>; <c>galar</c>), begun by GL5's second session: a new game in
    /// Galar, the player put down on a marker of each place GL5 built and shot as the field draws it, from Hammerlocke's
    /// south gate to Spikemuth (<c>ga70</c> on). It prints the area each stands in. The places of GL1 to GL4 (<c>ga00</c> to
    /// <c>ga6*</c>, the story's <c>gas*</c>) are owed to a later session of the mode.
    /// </summary>
    public void GalarMode()
    {
        engine.NewGameRegion = "Galar";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Grookey")!, 40));
        FieldRigs.Patient = true;
        Frames(10);
        var galar = engine.OpenMapNamed("Galar");
        if (galar == null) { Console.WriteLine("!! no open map called Galar"); return; }
        foreach (var (shot, marker) in new[]
        {
            ("ga70_hammerlocke", "hammerlocke_south_gate"),
            ("ga71_route_6", "route6_east"),
            ("ga72_stow_on_side_mural", "stow_mural_back"),
            ("ga73_glimwood_mouth", "glimwood_west"),
            ("ga74_ballonlea", "ballonlea_center_door"),
            ("ga75_hammerlocke_stadium", "hammerlocke_stadium_door"),
            ("ga76_route_7", "route7_south"),
            ("ga77_route_8_ruins", "route8_ruins"),
            ("ga78_circhester", "circhester_center_door"),
            ("ga79_heros_bath", "heros_bath"),
            ("ga80_route_9_east_shore", "route9_east"),
            ("ga81_spikemuth", "spikemuth_gate"),
            ("ga82_dusty_bowl", "dusty_bowl"),
            ("ga83_lake_of_outrage", "lake_of_outrage"),
        })
        {
            if (!Wanted(shot)) continue;
            if (galar.Marker(marker) is not { } m) { Console.WriteLine($"!! Galar has no marker {marker}"); continue; }
            PutInOpenMap(galar, m.Pos, m.Yaw);
            game.LocationSign.Hide();
            Frames(40);
            Shot(shot);
            Console.WriteLine($"  {shot}: {marker} at {m.Pos}, {galar.AreaAt(m.Pos)?.Key ?? "no area"}");
        }
    }
}
