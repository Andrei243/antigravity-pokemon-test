using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    /// <summary>
    /// The places of the last waves that had few shots or none (not part of <c>all</c>; <c>leftovers</c>; a shots pass of
    /// 2026-11-12): Kanto's and Hoenn's pastures, a Pokémon Center's PC in each and Brendan's bedroom PC, the Seagallop at
    /// two harbours and the Indigo Plateau's Champion's room (<c>lo0*</c>); Unova's Route 23 with its holes, Join Avenue,
    /// the League's hall and its statue, the League's plaza after dark, Pokéstar Studios' lot and the five sports fields
    /// of Nimbasa City (<c>lo1*</c> to <c>lo3*</c>). Each room is shot from a little south of whoever or whatever it is
    /// about, found by the map's own people; it prints where each shot stands.
    /// </summary>
    public void LeftoversMode()
    {
        engine.NewGameRegion = "Kanto";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Bulbasaur")!, 30));
        Frames(10);

        // Kanto and Hoenn
        Middle("lo00_kanto_pasture", "KantoPasture");
        Middle("lo01_hoenn_pasture", "HoennPasture");
        Before("lo02_kanto_center_pc", "ViridianCity_PokemonCenter_1F", n => n.IsPCTerminal);
        Before("lo03_hoenn_center_pc", "OldaleTown_PokemonCenter_1F", n => n.IsPCTerminal);
        Before("lo04_brendans_bedroom_pc", "LittlerootTown_BrendansHouse_2F", n => n.IsPCTerminal);
        foreach (var (shot, map) in new[] { ("lo05_seagallop_one_island", "OneIsland_Harbor"), ("lo06_seagallop_four_island", "FourIsland_Harbor"),
                     ("lo06b_seagallop_seven_island", "SevenIsland_Harbor") })
            Before(shot, map, n => n.NpcType == NPC.SeagallopType, show: true);
        Middle("lo07_champions_room", "PokemonLeague_ChampionsRoom");

        // Unova
        foreach (var (shot, x, y) in new[] { ("lo10_route23_holes", 582, 98), ("lo11_route23_holes_east", 592, 99) })
            if (Wanted(shot)) { At("Unova", x, y, Direction.Up); Frames(20); Shot(shot); Tell(shot); }
        Middle("lo12_join_avenue", "JoinAvenue");
        if (Wanted("lo13_league_statue")) { At("UnovaPokemonLeague_Hall", 31, 48, Direction.Up); Frames(20); Shot("lo13_league_statue"); Tell("lo13_league_statue"); }
        Middle("lo14_league_plaza_day", "UnovaPokemonLeague");
        Middle("lo20_pokestar_lot", "PokestarStudios");
        Middle("lo21_pokestar_gate", "PokestarGate");
        foreach (var (shot, map) in new[]
                 {
                     ("lo30_gridiron_field", "NimbasaCity_GridironField"), ("lo31_baseball_field", "NimbasaCity_BaseballField"),
                     ("lo32_football_field", "NimbasaCity_FootballField"), ("lo33_tennis_court", "NimbasaCity_TennisCourt"),
                     ("lo34_basketball_court", "NimbasaCity_BasketballCourt"), ("lo35_small_court", "NimbasaCity_SmallCourt"),
                 })
            Middle(shot, map);

        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        Middle("lo15_league_plaza_night", "UnovaPokemonLeague");
        Middle("lo22_pokestar_lot_night", "PokestarStudios");
        Middle("lo08_kanto_pasture_night", "KantoPasture");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
    }

    void Tell(string shot) =>
        Console.WriteLine($"  {shot}: {game.Map.Name} ({game.Player.GridX}, {game.Player.GridY}), {game.Map.AreaAt(game.Player.GridX, game.Player.GridY)?.DisplayName ?? "no area"}");

    /// <summary>The player on the open tile nearest the map's middle, facing north.</summary>
    void Middle(string shot, string mapName)
    {
        if (!Wanted(shot)) return;
        var map = MapDatabase.Get(mapName);
        if (map.Name != mapName) { Console.WriteLine($"!! no map called {mapName}"); return; }
        var (x, y) = OpenTileNear(map, map.Width / 2, map.Height / 2 + 2);
        At(mapName, x, y, Direction.Up);
        Frames(20);
        Shot(shot);
        Tell(shot);
    }

    /// <summary>The player a few tiles south of the first of the map's people <paramref name="which"/> picks, facing it.</summary>
    void Before(string shot, string mapName, Func<NPC, bool> which, bool show = false)
    {
        if (!Wanted(shot)) return;
        var map = MapDatabase.Get(mapName);
        if (map.Name != mapName) { Console.WriteLine($"!! no map called {mapName}"); return; }
        var who = map.Everyone.FirstOrDefault(which);
        if (who == null) { Console.WriteLine($"!! {mapName} has nobody the shot is about"); Middle(shot, mapName); return; }
        if (show && who.HiddenBy is { } flag) game.Story.Unset(flag);
        var (x, y) = OpenTileNear(map, who.GridX, who.GridY + 3);
        At(mapName, x, y, Direction.Up);
        Frames(20);
        Shot(shot);
        Console.WriteLine($"  {shot}: {mapName} ({x}, {y}), before {who.Name} at ({who.GridX}, {who.GridY}){(map.NPCs.Contains(who) ? "" : ", not on the map")}");
    }

    /// <summary>The open tile nearest a tile, in rings round it.</summary>
    static (int X, int Y) OpenTileNear(Map map, int x, int y)
    {
        for (int r = 0; r < 24; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    int tx = x + dx, ty = y + dy;
                    if (tx >= 0 && ty >= 0 && tx < map.Width && ty < map.Height && map.IsWalkable(tx, ty)) return (tx, ty);
                }
        return (x, y);
    }
}
