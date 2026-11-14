using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;

partial class Harness
{
    /// <summary>
    /// The West Province (plan 34 · PD6, its first session; the <c>paldea</c> mode, <c>SHOTS_FILTER=pdw</c>): Quaking
    /// Earth's basin and Asado Desert north of Cortondo, Cascarrafa and its falls, the West Province's second area and
    /// Porto Marinada on the sea with its auction, the first area and the Open Sky gully, Medali and its eatery, from their
    /// markers, each gym faced from its square, and the eight rooms (<c>pdw00</c> to <c>pdw31</c>). It prints the area
    /// each stands in.
    /// </summary>
    void PaldeaWest(OpenMap paldea)
    {
        ShootAt(paldea, "pdw00_quaking_earth", "quaking_earth_mouth");
        ShootAt(paldea, "pdw01_asado_desert", "desert_south");
        ShootAt(paldea, "pdw02_desert_crossroads", "desert_crossroads");
        ShootAt(paldea, "pdw03_cascarrafa", "cascarrafa_south");
        ShootToward(paldea, "pdw04_cascarrafa_gym", "cascarrafa_square", -496, -727, 22);
        ShootToward(paldea, "pdw05_cascarrafa_falls", "cascarrafa_falls", -390, -760, 62);
        ShootAt(paldea, "pdw06_west_province_two", "west_two_east");
        ShootAt(paldea, "pdw07_porto_marinada", "porto_east");
        ShootToward(paldea, "pdw08_porto_auction", "porto_quay", -711, -700, 10);
        ShootAt(paldea, "pdw09_west_province_one", "west_one_west");
        ShootAt(paldea, "pdw10_open_sky", "open_sky_mouth");
        ShootAt(paldea, "pdw11_medali", "medali_south");
        ShootToward(paldea, "pdw12_medali_gym", "medali_square", -182, -708, 20);
        ShootToward(paldea, "pdw13_medali_eatery", "medali_square", -160, -653, 14);
        foreach (var (shot, room) in new[]
        {
            ("pdw20_cascarrafa_center", "Cascarrafa_PokemonCenter"),
            ("pdw21_cascarrafa_gym", "Cascarrafa_Gym"),
            ("pdw22_cascarrafa_test", "Cascarrafa_GymTest"),
            ("pdw23_porto_center", "PortoMarinada_PokemonCenter"),
            ("pdw24_medali_center", "Medali_PokemonCenter"),
            ("pdw25_medali_gym", "Medali_Gym"),
            ("pdw26_medali_test", "Medali_GymTest"),
            ("pdw27_treasure_eatery", "Medali_TreasureEatery"),
        })
            ShootRoom(shot, room);
        foreach (var (shot, room) in new[] { ("pdw30_kofu", "Cascarrafa_GymTest"), ("pdw31_larry", "Medali_GymTest") })
        {
            if (!Wanted(shot)) continue;
            var map = MapDatabase.Get(room);
            game.Place(map, 6, 6, Direction.Up);
            game.LocationSign.Hide();
            Frames(40);
            Shot(shot);
            Console.WriteLine($"  {shot}: {room} at 6,6, {map.NPCs.Count} people: {string.Join(", ", map.NPCs.Select(n => n.Name))}");
        }
    }
}
