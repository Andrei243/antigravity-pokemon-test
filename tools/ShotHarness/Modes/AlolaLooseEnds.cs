using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    /// <summary>
    /// Alola's loose ends (plan 23, wave 91; not part of <c>all</c>): the region's Town Map with its four islands on one
    /// grid (<c>wl00</c> to <c>wl04</c>: out of the bag on Melemele, the cursor on Akala, Ula'ula and Poni, Fly's choice
    /// landing on Ula'ula), the Pokédex's area page lighting places of every island (<c>wl05</c>, <c>wl06</c>), and the
    /// Ride Pager on land (<c>wl10</c> to <c>wl17</c>: the pager's question, Tauros under the player facing four ways and
    /// galloping, Stoutland and Mudsdale, and Tauros run into the cracked rock before Ten Carat Hill, its cut-in and the
    /// way open).
    /// </summary>
    public void AlolaLooseEndsMode()
    {
        engine.NewGameRegion = "Alola";
        engine.StartNewGame();
        game.State = GameState.Overworld;
        game.SaveSlot = 1;
        game.LocationSign.Hide();
        Frames(30);
        var story = game.Story;
        var alola = RegionDatabase.Get(RegionDatabase.Alola)!;
        var map = RegionMap.Of(RegionDatabase.Alola)!;
        var screen = game.TownMapScreen;
        Console.WriteLine($"alola: the map is {map.Columns} by {map.Rows} cells over {string.Join(", ", map.Overworlds.Select(o => $"{o.Key} at {o.Value.X},{o.Value.Y}"))}; {map.Blocks.Count} blocks, Fly's places {string.Join(' ', map.FlySpots.Select(f => f.Spawn))}");

        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        // Reads every line on until the script and its text are done
        void ReadOn(int most = 900)
        {
            for (int i = 0; i < most && (engine.ScriptRunning || game.Dialogue.IsActive || game.State == GameState.Dialogue); i++)
            {
                if (game.Dialogue.IsActive) { game.Dialogue.FinishLine(); Frames(2); game.Dialogue.Advance(); }
                Frames(2);
            }
        }
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
            Console.WriteLine($"{shot}: {screen.Mode} cursor {screen.CursorX},{screen.CursorY}: {told}{spot}; here {state.Here} ({state.HereName})");
        }

        // ---- The Town Map out of the bag, the player on Route 1 with Iki Town, Heahea, Malie and Seafolk come to
        foreach (int id in new[] { 2, 4, 30, 32 }) story.Set(SpawnLocations.FlyFlag(SpawnLocations.In(RegionDatabase.Alola).Single(s => s.Id == id)));
        story.Set("FLAG_FIRST_ARRIVAL_IKI_TOWN");
        At("Melemele", 238, 150, Direction.Up);
        game.OpenTownMap(TownMapMode.Item);
        Frames(30); Shot("wl00_alola_town_map"); Report("wl00_alola_town_map");
        To(21, 21); Shot("wl01_akala_heahea"); Report("wl01_akala_heahea");
        To(18, 10); Shot("wl02_ulaula_malie"); Report("wl02_ulaula_malie");
        To(2, 24); Shot("wl03_poni_seafolk"); Report("wl03_poni_seafolk");
        screen.Cancel(); Frames(4);
        game.State = GameState.Overworld;

        // ---- Fly's choice from Melemele to Malie City on Ula'ula
        screen.Open(new TownMapState(map, story, map.CellOn("Melemele", 238, 150)), TownMapMode.Fly, new Pokemon(PokemonDatabase.Get("Charizard")!, 40));
        game.State = GameState.FlyMap;
        Frames(30);
        To(18, 10); Shot("wl04_fly_to_malie"); Report("wl04_fly_to_malie");
        screen.Cancel(); Frames(4);
        game.State = GameState.Overworld;

        // ---- The Pokédex's AREA page: Wingull lives on several islands; Jangmo-o in Vast Poni Canyon on Poni
        var dex = game.Pokedex;
        var dexScreen = game.PokedexScreen;
        dex.Enter(alola);
        foreach (string caught in new[] { "Wingull", "Jangmo-o", "Yungoos" }) dex.RegisterCaught(PokemonDatabase.Get(caught)!.DexNumber);
        foreach (int seen in dex.SeenSpecies) PokemonPlatinumEngine.Graphics.PokemonSprites.Request(PokemonDatabase.GetByDex(seen)!.Name);
        PokemonPlatinumEngine.Graphics.PokemonSprites.Flush(game.RenderContext);
        game.State = GameState.PokedexMenu;
        dexScreen.Open(dex, game.World.Portrait(PlayerIdentity.Character), alola);
        if (dexScreen.Mode != alola.Dex)
        {
            dexScreen.OpenSearch();
            for (int i = 0; i < 6 && dexScreen.Mode != alola.Dex; i++) { dexScreen.Sideways(1); dexScreen.Search(); dexScreen.OpenSearch(); }
            dexScreen.Search();
        }
        Console.WriteLine($"alola: the Pokédex lists {dexScreen.Mode}, its area page draws {dexScreen.AreaRegion.Id}");
        foreach (var (species, shot) in new[] { ("Wingull", "wl05_area_wingull"), ("Jangmo-o", "wl06_area_jangmoo") })
        {
            int at = dexScreen.Rows.ToList().FindIndex(r => r.Species.Name == species);
            if (at < 0) { Console.WriteLine($"alola: {species} isn't listed"); continue; }
            dexScreen.SelectedIndex = at;
            dexScreen.Confirm();
            for (int i = 0; i < 4 && dexScreen.Page != PokedexPage.Area; i++) dexScreen.Sideways(1);
            Frames(20); Shot(shot);
            dexScreen.Cancel(); Frames(4);
        }
        game.State = GameState.Overworld;

        // ---- The Ride Pager on land
        story.Set("FLAG_ALOLA_RIDE_TAUROS");
        story.Set(RidePager.StoutlandFlag);
        story.Set(RidePager.MudsdaleFlag);
        game.Bag.AddItem(ItemDatabase.Get(RidePager.Item)!, 1);
        At("Melemele", 290, 167, Direction.Down);
        game.UseFieldItem(ItemDatabase.Get(RidePager.Item)!);
        Until(() => engine.Choice.IsOpen, "the pager's question", 240);
        Frames(10); Shot("wl10_ride_pager_question");
        engine.Choice.Confirm();
        ReadOn();
        Frames(30);
        Console.WriteLine($"alola: riding {game.Player.Steed} ({game.Player.Mode})");
        foreach (var (facing, suffix) in new[] { (Direction.Down, "down"), (Direction.Left, "left"), (Direction.Up, "up"), (Direction.Right, "right") })
        {
            game.Player.Facing = facing; Frames(20);
            Shot($"wl11_tauros_{suffix}");
            ShotCrop($"wl11_tauros_{suffix}_close", 800, 380, 320, 240, 2);
        }
        engine.Steering = (Direction.Left, false);
        Frames(7); Shot("wl12_tauros_galloping");
        engine.Steering = null;
        Frames(30);
        foreach (string ride in new[] { "Stoutland", "Mudsdale" })
        {
            engine.RideOn(null);
            engine.RideOn(ride);
            game.Player.Facing = Direction.Left;
            Frames(40);
            Shot($"wl13_{ride.ToLowerInvariant()}");
            ShotCrop($"wl13_{ride.ToLowerInvariant()}_close", 800, 380, 320, 240, 2);
        }

        // ---- Tauros charges the cracked rock before Ten Carat Hill
        engine.RideOn(null);
        engine.RideOn("Tauros");
        At("Melemele", 217, 176, Direction.Left);
        engine.RideOn("Tauros");
        Frames(40);
        var rock = game.Map.Everyone.Single(n => n.Key == "rock_ten_carat");
        Console.WriteLine($"alola: the rock stands {(game.Map.NPCs.Contains(rock) ? "in the way" : "gone")}, riding {game.Player.Steed}");
        Shot("wl14_tauros_before_the_rock");
        engine.Steering = (Direction.Left, false);
        for (int f = 0; f < 60 && !engine.ScriptRunning; f++) Frames(1);
        engine.Steering = null;
        Frames(20); Shot("wl15_tauros_charges");
        ReadOn();
        Frames(30);
        Console.WriteLine($"alola: after the charge the rock is {(game.Map.NPCs.Contains(rock) ? "still there" : "gone")}, its flag {story.Has(rock.HiddenBy!)}");
        Shot("wl16_rock_broken");
        engine.RideOn(null);
        Frames(20); Shot("wl17_off_the_ride");
    }
}
