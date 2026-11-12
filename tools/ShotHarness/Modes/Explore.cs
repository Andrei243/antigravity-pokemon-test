using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- the field beyond the tables (plan 38; not part of "all")

    // `explore` shoots what plan 38 adds to the field, a part to a session (EXPLORE_PART=<part> runs one; none runs them
    // all). EX10 is `unova`: Unova's Town Map, which it never had (ex00*): the map out of the bag in a new game there, Fly's
    // choice with Unova's tab beside Sinnoh's, and the Pokédex's area page on Unova's own picture.
    public void ExploreMode()
    {
        string part = Environment.GetEnvironmentVariable("EXPLORE_PART") ?? "";
        if (part.Length == 0 || part == "unova") ExploreUnovaMap();
        if (part.Length == 0 || part == "outbreaks") ExploreOutbreaks();
        if (part.Length == 0 || part == "report") ExploreReport();
        if (part.Length == 0 || part == "hideaways") ExploreHideaways();
        if (part.Length == 0 || part == "headbutt") ExploreHeadbutt();
        if (part.Length == 0 || part == "statues") ExploreStatues();
        if (part.Length == 0 || part == "soaring") ExploreSoaring();
        if (part.Length == 0 || part == "skyplaces") ExploreSkyPlaces();
        if (part.Length == 0 || part == "feebas") ExploreFeebas();
    }

    private void ExploreUnovaMap()
    {
        engine.NewGameRegion = "Unova";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        var story = game.Story;
        var unova = RegionDatabase.Get(RegionDatabase.Unova)!;
        var map = RegionMap.Of(RegionDatabase.Unova)!;
        var screen = game.TownMapScreen;
        Console.WriteLine($"explore: Unova's map is {map.Columns} by {map.Rows} cells, {map.Blocks.Count} blocks, {map.Areas.Count} places, view {map.View}, Fly's places {string.Join(' ', map.FlySpots.Select(f => f.Spawn))}");

        void To(int x, int y)
        {
            for (int i = 0; i < 80 && (screen.CursorX, screen.CursorY) != (x, y); i++) screen.Move(Math.Sign(x - screen.CursorX), Math.Sign(y - screen.CursorY));
            Frames(14);
        }
        void Report(string shot)
        {
            var state = screen.State!;
            var block = screen.Hovered;
            string told = block == null ? "nothing here" : $"{block.Kind} '{block.Name}' area {block.Area.Key} told {state.Tells(block.Area)}";
            string spot = screen.HoveredSpot is { } s ? $", Fly's place {s.Town.Area} open {s.Open}" : "";
            Console.WriteLine($"{shot}: {screen.Mode} tab {screen.Tab} cursor {screen.CursorX},{screen.CursorY}: {told}{spot}; here {state.Here} ({state.HereName})");
        }

        // ---- The Town Map out of the bag: Aspertia City, Floccesy Town and Virbank City come to, the player in Floccesy Town
        foreach (string key in new[] { "aspertia_city", "floccesy_town" }) story.Set(SpawnLocations.ArrivedIn(RegionDatabase.Unova, key)!.ArrivalFlag);
        story.Set("FLAG_FIRST_ARRIVAL_VIRBANK_CITY");
        At("Unova", 113, 678, Direction.Up);
        game.OpenTownMap(TownMapMode.Item);
        Frames(30); Shot("ex00_unova_town_map"); Report("ex00_unova_town_map");
        To(3, 20); Frames(10); Shot("ex00a_unova_floccesy"); Report("ex00a_unova_floccesy");
        To(12, 14); Shot("ex00a2_unova_nimbasa_not_visited"); Report("ex00a2_unova_nimbasa_not_visited");
        screen.Cancel(); Frames(4);
        game.State = GameState.Overworld;

        // ---- Fly's choice, a Champion of Sinnoh who has been to Unova: two tabs, Unova's with Aspertia City chosen
        var sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        story.Set(sinnoh.StoryCompleteFlag);
        game.Pokedex.Enter(unova);
        var tabs = new[]
        {
            new TownMapState(RegionMap.Of(RegionDatabase.Sinnoh)!, story),
            new TownMapState(map, story, RegionMap.CellOf(113, 678)),
        };
        screen.Open(tabs, 1, TownMapMode.Fly, new Pokemon(PokemonDatabase.Get("Staraptor")!, 40));
        game.State = GameState.FlyMap;
        Frames(30);
        To(1, 22); Shot("ex00b_unova_fly_tab"); Report("ex00b_unova_fly_tab");
        screen.ChangeTab(-1); Frames(20); Shot("ex00b2_sinnoh_tab"); Report("ex00b2_sinnoh_tab");
        screen.Cancel(); Frames(4);
        story.Unset(sinnoh.StoryCompleteFlag);
        game.State = GameState.Overworld;

        // ---- The Pokédex's AREA page on Unova's picture
        var dex = game.Pokedex;
        var dexScreen = game.PokedexScreen;
        foreach (string caught in new[] { "Patrat", "Lillipup", "Purrloin" }) dex.RegisterCaught(PokemonDatabase.Get(caught)!.DexNumber);
        foreach (int seen in dex.SeenSpecies) PokemonPlatinumEngine.Graphics.PokemonSprites.Request(PokemonDatabase.GetByDex(seen)!.Name);
        PokemonPlatinumEngine.Graphics.PokemonSprites.Flush(game.RenderContext);
        game.State = GameState.PokedexMenu;
        dexScreen.Open(dex, game.World.Portrait(PlayerIdentity.Character), unova);
        if (dexScreen.Mode != unova.Dex)
        {
            dexScreen.OpenSearch();
            for (int i = 0; i < 4 && dexScreen.Mode != unova.Dex; i++) { dexScreen.Sideways(1); dexScreen.Search(); dexScreen.OpenSearch(); }
            dexScreen.Search();
        }
        Console.WriteLine($"explore: the Pokédex lists {dexScreen.Mode}, its area page draws {dexScreen.AreaRegion.Id}");
        dexScreen.SelectedIndex = dexScreen.Rows.ToList().FindIndex(r => r.Species.Name == "Patrat");
        dexScreen.Confirm();
        while (dexScreen.Page != PokedexPage.Area) dexScreen.Sideways(1);
        Frames(20); Shot("ex00c_unova_area_page");
        dexScreen.Cancel();
        dexScreen.SelectedIndex = dexScreen.Rows.ToList().FindIndex(r => r.Species.Name == "Purrloin");
        dexScreen.Confirm();
        Frames(20); Shot("ex00d_unova_area_page_purrloin");
        dexScreen.Cancel();
        game.State = GameState.Overworld;
    }
}
