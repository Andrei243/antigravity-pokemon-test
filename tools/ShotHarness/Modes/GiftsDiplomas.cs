using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

partial class Harness
{
    // ---------------------------------------------------------------- diplomas for every Pokédex (plan 40 · GE4)

    // The Pokédex of our own and the diplomas of every region (gf30 to gf34), through the game's own Pokédex screen and
    // Interact:
    //   gf30_pokedex_list_ours: the Pokédex of our own's list, its species numbered from 1, some seen and some caught,
    //     the longest names of ours among the rows shown (gf30b_national_list_ours: the National list at D14's heading,
    //     the teal OURS pill on each row of ours, the longest name's row selected and not);
    //   gf31_pokedex_ours_entry: an entry of ours open on its INFO page;
    //   gf32_diploma_johto: the diploma a Johto Pokédex seen whole is handed as the Pokédex opens;
    //   gf33_diploma_ours: the Pokédex of our own's diploma, its line the longest of the diplomas' (three rows);
    //   gf34_designer_lilycove: Hoenn's game designer in the Cove Lily Motel reading the Hoenn diploma (gf34b: the one
    //     of the Pokédex of our own).
    // It prints the species of ours with the longest names, the Pokédex's counts, the rows the diploma line wraps to and
    // what the designer said.
    void GiftsDiplomas()
    {
        DialogueManager Box() => game.Dialogue;
        void Tick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            engine.Draw();
        }
        bool ReadTo(string words, int most = 3000)
        {
            for (int i = 0; i < most; i++)
            {
                if (Box().IsActive && Translated.Saying(Box().CurrentLine, words)) return true;
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Console.WriteLine($"    said: {Box().CurrentLine}"); Box().Advance(); Tick(); Tick(); }
                else Tick();
            }
            Console.WriteLine($"  !! never said: {words}");
            return false;
        }
        void ReadOn(int most = 3000)
        {
            for (int i = 0; i < most && (engine.ScriptRunning || Box().IsActive); i++)
            {
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Tick(); Tick(); }
                else Tick();
            }
        }
        void NewGame(string region)
        {
            engine.NewGameRegion = region;
            engine.StartNewGame();
            game.State = GameState.Overworld;
            game.SaveSlot = 1;
            game.LocationSign.Hide();
            if (region == RegionDatabase.Sinnoh) PastTheOpening();
        }
        var dexScreen = game.PokedexScreen;
        // Menu sprites are baked the first time a menu asks for them: ask for the rows a shot shows and bake them
        void Bake(params string[] names)
        {
            foreach (string name in names) PokemonSprites.Request(name);
            PokemonSprites.Flush(game.RenderContext);
        }
        // The Pokédex of the list's own kind, chosen through the search panel as a player does
        void ShowMode(string dex)
        {
            dexScreen.OpenSearch();
            for (int i = 0; i < 20 && dexScreen.ValueOf(PokedexSearchRow.Mode) != PokedexScreen.ModeName(dex); i++) dexScreen.Sideways(1);
            dexScreen.Search();
        }
        void Select(string species) => dexScreen.SelectedIndex = dexScreen.Rows.ToList().FindIndex(r => r.Species.Name == species);

        // ---- The longest names of the species of ours: the pill and the row's name must not meet
        var ours = Pokedex.Entries(Pokedex.Ours);
        var longest = ours.OrderByDescending(e => e.Species.DisplayName.Length).Take(4).Select(e => e.Species).ToList();
        Console.WriteLine($"  the Pokédex of our own: {ours.Count} species; longest names {string.Join(", ", longest.Select(s => $"{s.DisplayName} ({s.DisplayName.Length})"))}");

        // ---- The list of ours and an entry of it (a National Pokédex, one of ours met, the first twelve seen)
        NewGame(RegionDatabase.Sinnoh);
        var dex = game.Pokedex;
        dex.UnlockNational();
        foreach (var s in longest) { dex.RegisterSeen(s.DexNumber); dex.RegisterCaught(s.DexNumber); }
        for (int i = 0; i < ours.Count && i < 14; i++)
        {
            dex.RegisterSeen(ours[i].Species.DexNumber);
            if (i % 3 == 0) dex.RegisterCaught(ours[i].Species.DexNumber);
        }
        Console.WriteLine($"  ours offered {dex.OursOpen}; Pokédexes {string.Join(", ", dex.Dexes)}; ours seen {dex.SeenIn(Pokedex.Ours)} caught {dex.CaughtIn(Pokedex.Ours)} of {ours.Count}");
        Bake(ours.Take(14).Select(e => e.Species.Name).Concat(longest.Select(s => s.Name)).ToArray());
        game.State = GameState.PokedexMenu;
        dexScreen.Open(dex, game.World.Portrait(PlayerIdentity.Character));
        ShowMode(Pokedex.Ours);
        dexScreen.SelectedIndex = 2;
        Frames(30); Shot("gf30_pokedex_list_ours");
        // A row of the longest names (rows further down the list, past the first page)
        dexScreen.SelectedIndex = Math.Max(0, dexScreen.Rows.ToList().FindIndex(r => r.Species == longest[0]));
        Frames(30); Shot("gf30c_pokedex_list_ours_long_names");
        Console.WriteLine($"  the list of ours: mode {dexScreen.Mode}, {dexScreen.Rows.Count} rows, the cursor on {dexScreen.Selected?.Species.DisplayName} (No. {dexScreen.Selected?.Number})");
        dexScreen.SelectedIndex = 3;   // Sandspire, one caught: its entry written out
        Frames(4);
        dexScreen.Confirm();
        Frames(40); Shot("gf31_pokedex_ours_entry");
        Console.WriteLine($"  the entry: {dexScreen.Selected?.Species.DisplayName}, page {dexScreen.Page}, focus {dexScreen.Focus}");
        dexScreen.Cancel();

        // ---- The National list at D14's heading: a pill on each row of ours
        ShowMode(Pokedex.National);
        int firstOurs = dexScreen.Rows.ToList().FindIndex(r => r.Species.IsOurs);
        int[] pills = { firstOurs, Math.Max(0, dexScreen.Rows.ToList().FindIndex(r => r.Species == longest[0])) };
        Bake(dexScreen.Rows.Skip(firstOurs).Take(8).Select(r => r.Species.Name).ToArray());
        dexScreen.SelectedIndex = firstOurs + 1;
        Frames(30); Shot("gf30b_national_list_ours");
        dexScreen.SelectedIndex = pills[1];
        Frames(30); Shot("gf30d_national_list_ours_long_names");
        Console.WriteLine($"  the National list: {dexScreen.Rows.Count} rows, ours from row {firstOurs}");
        dexScreen.Close();
        game.State = GameState.Overworld;

        // ---- Johto's diploma: the Pokédex of a Johto game seen whole (Entered, every species of the list seen)
        NewGame(RegionDatabase.Johto);
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        dex = game.Pokedex;
        dex.Enter(johto);
        foreach (string id in johto.Dexes)
            foreach (var entry in Pokedex.Entries(id)) dex.RegisterSeen(entry.Species.DexNumber);
        game.State = GameState.PokedexMenu;
        dexScreen.Open(dex, game.World.Portrait(PlayerIdentity.Character), johto);
        Frames(30); Shot("gf32_diploma_johto");
        Console.WriteLine($"  Johto: focus {dexScreen.Focus}, shown {dexScreen.ShownDiploma}, diplomas {string.Join(", ", dex.Diplomas)}, complete {string.Join(", ", johto.Dexes.Select(d => dex.IsComplete(d)))}");
        dexScreen.Close();
        game.State = GameState.Overworld;

        // ---- The diploma of the Pokédex of our own: every species of ours seen, the National Pokédex open
        NewGame(RegionDatabase.Sinnoh);
        dex = game.Pokedex;
        dex.UnlockNational();
        foreach (var entry in ours) dex.RegisterSeen(entry.Species.DexNumber);
        game.State = GameState.PokedexMenu;
        dexScreen.Open(dex, game.World.Portrait(PlayerIdentity.Character));
        Frames(30); Shot("gf33_diploma_ours");
        Console.WriteLine($"  ours: focus {dexScreen.Focus}, shown {dexScreen.ShownDiploma}, diplomas {string.Join(", ", dex.Diplomas)}, progress {dex.Progress(Pokedex.Ours)}");
        var wrapped = UiFonts.Measure(Text.Of("pokedex.diploma.line.ours", ("count", ours.Count)), 32, UiWeight.ExtraBold);
        Console.WriteLine($"  the ours line is {wrapped:0} units wide in one row; the diploma wraps it at 900");
        dexScreen.Close();
        game.State = GameState.Overworld;

        // ---- Hoenn's game designer in the Cove Lily Motel reading the diplomas out
        NewGame(RegionDatabase.Hoenn);
        Frames(10);
        ReadOn(40000);                  // the moving van's scene, played through (its script starts a few frames in)
        var hoenn = RegionDatabase.Get(RegionDatabase.Hoenn)!;
        dex = game.Pokedex;
        dex.Enter(hoenn);
        dex.Diplomas.Add(hoenn.Dex!);
        game.Story.Set("HoennHallOfFame");
        game.Story.Unset("FLAG_HIDE_LILYCOVE_MOTEL_GAME_DESIGNERS");   // the designers who come to stay after the Hall of Fame
        game.Bag.AddItem(ItemDatabase.Get(Charms.Oval)!, 1);
        Frames(4);
        Console.WriteLine($"  the designer's check: charms due {string.Join(", ", Charms.Due(dex, game.Bag).DefaultIfEmpty("nothing"))}, DIPLOMAS held {dex.Diplomas.Count}");
        var motel = AreaSpot("lilycove_city_cove_lily_motel_2f").Map;
        if (BesideDesigner(motel))
        {
            game.EnterArea();           // the place's own arrival script comes first, as a first step into it would run it
            ReadOn(40000);
            Frames(500);                // the toast of the van scene's last script fades
            game.LocationSign.Hide();
            Frames(20);
            game.Interact();
            Frames(3);
            Console.WriteLine($"  spoken to: script running {engine.ScriptRunning}, text {Box().IsActive}, state {game.State}, line \"{Box().CurrentLine}\", complete {Box().IsCurrentLineComplete}, question {Box().IsQuestion}");
            if (ReadTo("A diploma for all of Hoenn")) { Box().FinishLine(); Frames(24); Shot("gf34_designer_lilycove"); }
            ReadOn();
            dex.Diplomas.Add(Pokedex.Ours);
            game.Interact();
            Frames(3);
            if (ReadTo("no region lists")) { Box().FinishLine(); Frames(24); Shot("gf34b_designer_lilycove_ours"); }
            ReadOn();
        }
    }

    // Beside the game designer of the motel's upper floor, facing him
    bool BesideDesigner(Map map)
    {
        if (map.NPCs.FirstOrDefault(n => n.Key == "man_3") is not { } npc)
        {
            Console.WriteLine($"  !! the game designer is not on the map ({(map.Everyone.Any(n => n.Key == "man_3") ? "hidden" : "not placed")})");
            return false;
        }
        (int dx, int dy, Direction facing)[] sides = { (-1, 0, Direction.Right), (1, 0, Direction.Left), (0, 1, Direction.Up), (0, -1, Direction.Down) };
        var side = sides.First(s => map.IsWalkable(npc.GridX + s.dx, npc.GridY + s.dy) && map.GetNpcAt(npc.GridX + s.dx, npc.GridY + s.dy) == null);
        At(map.Name, npc.GridX + side.dx, npc.GridY + side.dy, side.facing);
        game.LocationSign.Hide();
        Frames(30);
        Console.WriteLine($"  beside the designer: he stands at {npc.GridX},{npc.GridY} in {map.Name}; the player at {game.Player.GridX},{game.Player.GridY} on {game.Map.Name} facing {game.Player.Facing}");
        return true;
    }
}
