using System;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- every item the rules run (plan 40 · GE11)

    // Items the finished regions owed, each handed over in a new game of its region through the game's own scripts and
    // Interact (gf100 to gf103):
    //   gf100_unova_splicers: the Giant Chasm's cavern as Ghetsis's scene leaves it (N, Zekrom and Kyurem there, Ghetsis
    //     taken away), its own beat played (giant_chasm_6.DnaSplicers): the DNA Splicers found where he stood;
    //   gf101_driftveil_drive: Driftveil's scientist in the flats by the market handing the Burn Drive to a Trainer with a
    //     Genesect, the first of three;
    //   gf102_professor_stone: Prof. Rowan, Sinnoh's Hall of Fame entered and a Key Stone in the bag, handing over the
    //     Torterrite, the first of the three stones of Sinnoh's Megas of our own;
    //   gf103_hoenn_stone: Mom in Littleroot Town after Hoenn's Hall of Fame handing over the Latiasite, the first of the
    //     eon Pokémon's two stones (Omega Ruby's and Alpha Sapphire's, decision 11).
    // It prints what each left in the bag (or on the team) beside what it should have.
    void GiftsItems()
    {
        DialogueManager Box() => game.Dialogue;
        void Tick()
        {
            FrameClock.Fixed = ++tick / 60.0;
            engine.Update(1f / 60f);
            engine.Draw();
        }
        // Reads on until the text box says a line holding the words, leaving it on screen
        bool ReadTo(string words, int most = 3000)
        {
            for (int i = 0; i < most; i++)
            {
                if (Box().IsActive && Translated.Saying(Box().CurrentLine, words)) return true;
                if (Box().IsActive && Box().IsCurrentLineComplete && !Box().IsQuestion) { Box().Advance(); Tick(); Tick(); }
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
        void NewGame(string region, params string[] team)
        {
            engine.NewGameRegion = region;
            engine.StartNewGame();
            game.State = GameState.Overworld;
            game.SaveSlot = 1;
            game.LocationSign.Hide();
            if (region == RegionDatabase.Sinnoh) PastTheOpening();
            foreach (string species in team) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 70, new Random(game.Party.Members.Count)));
        }
        // Beside someone of a map, facing them (from below the player's head would hide them): left or right first
        bool Beside(Map map, string key, string? area)
        {
            if (map.NPCs.FirstOrDefault(n => n.Key == key && (area == null || n.ScriptFile == area)) is not { } npc)
            {
                Console.WriteLine($"  !! nobody called {key} stands in {map.Name} ({(map.Everyone.Any(n => n.Key == key) ? "hidden" : "not on the map")})");
                return false;
            }
            (int dx, int dy, Direction facing)[] sides = { (-1, 0, Direction.Right), (1, 0, Direction.Left), (0, 1, Direction.Up), (0, -1, Direction.Down) };
            var side = sides.First(s => map.IsWalkable(npc.GridX + s.dx, npc.GridY + s.dy) && map.GetNpcAt(npc.GridX + s.dx, npc.GridY + s.dy) == null);
            At(map.Name, npc.GridX + side.dx, npc.GridY + side.dy, side.facing);
            game.LocationSign.Hide();
            Frames(30);
            return true;
        }
        int Has(string item) => game.Bag.GetQuantity(ItemDatabase.Get(item)!);
        void Print(string what, params string[] items) =>
            Console.WriteLine($"  {what}: {string.Join(", ", items.Select(i => $"{i} {Has(i)}"))}");

        // ---- Unova: the DNA Splicers in the Giant Chasm's cavern, where Ghetsis stood
        NewGame(RegionDatabase.Unova, "Haxorus");
        foreach (string shown in new[] { "FLAG_UNOVA_HIDE_N_CAVERN", "FLAG_UNOVA_HIDE_ZEKROM_CAVERN", "FLAG_UNOVA_HIDE_KYUREM_CAVERN" })
            game.Story.Unset(shown);
        foreach (string hidden in new[] { "FLAG_UNOVA_HIDE_GHETSIS_CAVERN", "FLAG_UNOVA_HIDE_BLACK_KYUREM", "FLAG_UNOVA_HIDE_TRIAD_CAVERN" })
            game.Story.Set(hidden);
        var cavern = AreaSpot("giant_chasm_6").Map;
        At(cavern.Name, 15, 23, Direction.Up);
        game.LocationSign.Hide();
        Frames(40);
        engine.StartScript(ScriptLibrary.Default.Find("DnaSplicers", "giant_chasm_6")!);
        if (ReadTo("found the DNA Splicers")) { Box().FinishLine(); Frames(24); Shot("gf100_unova_splicers"); }
        ReadOn();
        Print("the cavern", "DNA Splicers");

        // ---- Unova: three of Genesect's drives from Driftveil's scientist
        NewGame(RegionDatabase.Unova, "Genesect");
        var flats = AreaSpot("driftveil_city_3").Map;
        if (Beside(flats, "3", "driftveil_city_3"))
        {
            game.Interact();
            Frames(3);
            if (ReadTo("received the Burn Drive")) { Box().FinishLine(); Frames(24); Shot("gf101_driftveil_drive"); }
            ReadOn();
            Print("the scientist", "Burn Drive", "Chill Drive", "Douse Drive");
        }

        // ---- Sinnoh: Prof. Rowan's stones once the Hall of Fame is entered and the bag holds a Key Stone
        NewGame(RegionDatabase.Sinnoh);
        game.Story.Set("SinnohHallOfFame");
        game.Story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", Math.Max(1, game.Story.Var("VAR_SANDGEM_TOWN_LAB_STATE")));
        game.Bag.AddItem(ItemDatabase.Get(BattleCore.KeyStoneItem)!, 1);
        Frames(4);
        var lab = MapDatabase.Get("RowanLab");
        if (Beside(lab, "prof_rowan", null))
        {
            game.Interact();
            Frames(3);
            if (ReadTo("received the Torterrite")) { Box().FinishLine(); Frames(24); Shot("gf102_professor_stone"); }
            ReadOn();
            Print("Prof. Rowan", "Torterrite", "Infernapite", "Empoleonite");
        }

        // ---- Hoenn: Mom's two stones of the eon Pokémon after the Hall of Fame
        NewGame(RegionDatabase.Hoenn);
        game.Story.SetVar("VAR_LITTLEROOT_INTRO_STATE", 7);
        game.Story.SetVar("VAR_HOENN_CHAMPION_HOME", 1);
        game.Story.Set("HoennHallOfFame");
        // (the movers' Vigoroth, which the opening's scenes send off, gone as a Champion's house has them)
        game.Story.Set("FLAG_HIDE_LITTLEROOT_TOWN_PLAYERS_HOUSE_VIGOROTH_1");
        game.Story.Set("FLAG_HIDE_LITTLEROOT_TOWN_PLAYERS_HOUSE_VIGOROTH_2");
        Frames(4);
        var house = AreaSpot("littleroot_town_brendans_house_1f").Map;
        if (Beside(house, "players_house_1f_mom", "littleroot_town_brendans_house_1f"))
        {
            game.Interact();
            Frames(3);
            if (ReadTo("received the Latiasite")) { Box().FinishLine(); Frames(24); Shot("gf103_hoenn_stone"); }
            ReadOn();
            Print("Mom", "Latiasite", "Latiosite");
        }
    }
}
