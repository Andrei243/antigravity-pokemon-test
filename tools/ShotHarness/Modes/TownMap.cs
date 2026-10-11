using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- the map of the region (plan 01 · M12)

    // The Town Map, Fly's map and a wall's map, which are one screen (tm*; not part of "all"): out of the bag as the
    // second chapter gives it, the cursor over a town come to, a route, a town not yet come to and the open sea, a
    // landmark before and after it has been visited, the way come, the map from near and its locator, the hidden
    // places before and after the story reveals them, Fly's mode over a place that is open and one that isn't, the
    // map looked at from inside a room and from a cave, a wall's map, the Pokédex's area page on the same picture,
    // and the picture itself at the screen's own resolution. It prints what each shot stands on (the cursor, the block,
    // what is told, the player's cell, the way come).
    public void RegionMapMode()
    {
        engine.StartNewGame();
        PastTheOpening();
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var story = game.Story;
        var screen = game.TownMapScreen;
        var region = RegionMap.Sinnoh!;
        var bag = game.Bag;
        bag.AddItem(ItemDatabase.Get("Town Map")!, 1);

        void Report(string shot)
        {
            var state = screen.State!;
            var block = screen.Hovered;
            string told = block == null ? "nothing here"
                : $"{block.Kind} '{block.Name}' area {block.Area.Key} told {state.Tells(block.Area)}"
                    + (block.Landmark is { } l ? $", landmark {l.Key} told {state.TellsLandmark(block)}" : "");
            string spot = screen.HoveredSpot is { } s ? $", Fly's place {s.Town.Area} open {s.Open}" : "";
            Console.WriteLine($"{shot}: {screen.Mode} cursor {screen.CursorX},{screen.CursorY} zoomed {screen.Zoomed}: {told}{spot}; here {state.Here} ({state.HereName}); "
                + $"way come {string.Join(' ', state.Trail.Select(t => $"{t.X},{t.Y}{(t.Facing is { } f ? f.ToString()[0] : '-')}"))}; revealed {string.Join(',', state.RevealedPlaces)}");
        }
        void To(int x, int y)
        {
            for (int i = 0; i < 60 && (screen.CursorX, screen.CursorY) != (x, y); i++) screen.Move(Math.Sign(x - screen.CursorX), Math.Sign(y - screen.CursorY));
            Frames(14);
        }
        void Take(string shot)
        {
            Frames(2);
            Shot(shot);
            Report(shot);
        }

        // ---- Out of the bag, as the rival's parcel gives it: only Twinleaf Town and Sandgem Town come to yet
        At("Sinnoh", 116, 888, Direction.Down);
        foreach (string key in new[] { "twinleaf_town", "sandgem_town" }) story.Set(SpawnLocations.ArrivedIn(key)!.ArrivalFlag);
        var bagScreen = game.BagScreen;
        var townMap = ItemDatabase.Get("Town Map")!;
        game.State = GameState.BagMenu;
        bagScreen.Open();
        bagScreen.CurrentPocket = townMap.Pocket;
        bagScreen.SelectedIndex = bag.GetPocketItems(townMap.Pocket).FindIndex(st => st.Data.Name == townMap.Name);
        Frames(20);
        bagScreen.Confirm(bag, game.Party, engine.ShowNotification);
        Frames(6); Shot("tm01_bag_town_map");
        while (bagScreen.Actions != null && bagScreen.Actions[bagScreen.ActionIndex] != BagAction.Use) bagScreen.MoveCursor(1, bagScreen.Actions.Count);
        bagScreen.Confirm(bag, game.Party, engine.ShowNotification);
        Frames(4);
        Console.WriteLine($"town map: out of the bag, the game is in {game.State} with the map {(screen.IsActive ? "open" : "shut")} in {screen.Mode}");
        Frames(30); Take("tm02_home");
        To(4, 26); Take("tm03_route");
        To(5, 23); Take("tm04_town_not_visited");
        To(2, 12); Take("tm05_sea");
        screen.Cancel(); Frames(4);
        Console.WriteLine($"town map: put away, the game is back in {game.State}");
        bagScreen.Close(); game.State = GameState.Overworld; Frames(2);

        // ---- Later: towns come to, a walk east through Hearthome City, landmarks seen and not
        foreach (string key in new[] { "jubilife_city", "oreburgh_city", "floaroma_town", "eterna_city", "hearthome_city", "solaceon_town", "veilstone_city", "pastoria_city" })
            story.Set(SpawnLocations.ArrivedIn(key)!.ArrivalFlag);
        foreach (string flag in new[] { "FLAG_FIRST_ARRIVAL_OREBURGH_GATE", "FLAG_FIRST_ARRIVAL_OREBURGH_MINE", "FLAG_FIRST_ARRIVAL_VALLEY_WINDWORKS", "FLAG_FIRST_ARRIVAL_AMITY_SQUARE" })
            story.Set(flag);
        // The way come: the game's own trail, written as the player's steps write it
        var trail = game.MapTrail;
        trail.Clear();
        foreach (var (cx, cy, way) in new[] { (11, 22, Direction.Right), (12, 22, Direction.Right), (13, 22, Direction.Right), (14, 22, Direction.Right), (15, 22, Direction.Right), (15, 21, Direction.Up) })
            trail.Push(cx, cy, way);
        At("Sinnoh", 492, 690, Direction.Up);
        game.UseFieldItem(townMap);
        Frames(30); Take("tm06_city_with_the_way_come");
        To(14, 22); Take("tm07_landmark_visited");
        To(5, 22); Take("tm08_landmark_not_visited");
        To(12, 19); Take("tm09_mountain");
        To(15, 21);
        screen.ToggleZoom(); Frames(30); Take("tm10_zoomed_city");
        To(9, 23); Take("tm11_zoomed_mine");
        To(1, 6); Take("tm12_zoomed_corner");
        screen.ToggleZoom(); Frames(20);

        // ---- The hidden places: off the map, and on it once the story reveals them
        To(1, 8); Take("tm13_hidden_island");
        screen.Cancel(); Frames(4);
        foreach (var hidden in region.Hidden) story.SetVar(hidden.Variable, hidden.Value);
        Frames(4);
        game.UseFieldItem(townMap);
        Frames(30);
        To(1, 8); Take("tm14_island_revealed");
        To(28, 9); Take("tm15_path_revealed");
        screen.Cancel(); Frames(4);

        // ---- From inside: a room shows where its door is, a cave where the way in was
        At("JubilifePokemonCenter", 8, 8, Direction.Up);
        game.UseFieldItem(townMap);
        Frames(30); Take("tm16_from_a_room");
        screen.Cancel(); Frames(4);

        // ---- Fly's mode: a place that is open, one that isn't, and from near
        At("Sinnoh", 116, 888, Direction.Down);
        game.OpenTownMap(TownMapMode.Fly);
        Frames(30); Take("tm20_fly");
        To(14, 21); Take("tm21_fly_open");
        To(26, 23); Take("tm22_fly_not_visited");
        To(9, 28); Take("tm23_fly_pal_park_unseen");
        screen.Confirm(); Frames(4);
        Console.WriteLine($"fly: the A button over a place not open leaves the map {(screen.IsActive ? "open" : "shut")}");
        story.Set("FLAG_FIRST_ARRIVAL_POKE_PARK_FRONT_GATE");
        screen.Cancel(); Frames(4);
        game.State = GameState.Overworld;
        game.OpenTownMap(TownMapMode.Fly);
        Frames(20);
        To(9, 28); Take("tm24_fly_pal_park");
        To(21, 18); screen.ToggleZoom(); Frames(30); Take("tm25_fly_zoomed");
        screen.Cancel(); Frames(4);
        game.State = GameState.Overworld;

        // ---- A map on a wall: no way come
        game.OpenTownMap(TownMapMode.Wall);
        Frames(30); To(5, 26); Take("tm30_wall_map");
        screen.Cancel(); Frames(4);
        game.State = GameState.Overworld;

        // ---- The picture itself, at the screen's own resolution: the whole of it, and a town from near
        game.UseFieldItem(townMap);
        Frames(30);
        To(2, 12);
        if (Wanted("tm40_picture")) { ShotCrop("tm40_picture", 64 + 32, 132 + 32, 960, 768, 2); }
        To(22, 19); screen.ToggleZoom(); Frames(30);
        if (Wanted("tm41_picture_near")) { ShotCrop("tm41_picture_near", 64 + 32, 132 + 32, 960, 768, 2); }
        screen.Cancel(); Frames(4);
        game.State = GameState.Overworld;

        // ---- The Pokédex's area page draws the same picture, smaller
        var dexScreen = game.PokedexScreen;
        var dex = game.Pokedex;
        foreach (string caught in new[] { "Starly", "Psyduck", "Gyarados" }) dex.RegisterCaught(PokemonDatabase.Get(caught)!.DexNumber);
        foreach (int seen in dex.SeenSpecies) PokemonPlatinumEngine.Graphics.PokemonSprites.Request(PokemonDatabase.GetByDex(seen)!.Name);
        PokemonPlatinumEngine.Graphics.PokemonSprites.Flush(game.RenderContext);
        game.State = GameState.PokedexMenu;
        dexScreen.Open(dex, game.World.Portrait(PlayerIdentity.Character));
        dexScreen.SelectedIndex = dexScreen.Rows.ToList().FindIndex(r => r.Species.Name == "Starly");
        dexScreen.Confirm();
        while (dexScreen.Page != PokedexPage.Area) dexScreen.Sideways(1);
        Frames(20); Shot("tm50_pokedex_area_starly");
        dexScreen.Cancel();
        dexScreen.SelectedIndex = dexScreen.Rows.ToList().FindIndex(r => r.Species.Name == "Psyduck");
        dexScreen.Confirm();
        Frames(20); Shot("tm51_pokedex_area_psyduck");
        dexScreen.Cancel();
        game.State = GameState.Overworld;
    }
}
