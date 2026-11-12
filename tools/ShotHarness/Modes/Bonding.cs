using System;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- bonding (plan 35)

    // The camp and the care close-up (bn*; not part of "all"; BONDING_FROM=care runs it alone, the only part so far):
    // the camp's clearing (bn01, plan 10 · F6's owed shot), a Turtwig close up (bn02), the hand over its liked spot with
    // its head turning (bn03) and a pat's lean (bn04), a sand battle's dust and the brush at work (bn05, bn06), poison and
    // the medicine (bn07, bn08), a berry eaten (bn09) and the close-up after dark (bn10). It prints the book's page at each.
    public void BondingMode()
    {
        string from = Environment.GetEnvironmentVariable("BONDING_FROM") ?? "";
        if (from is "" or "care") BondingCare();
        if (from is "" or "pasture") BondingPasture();
        if (from is "" or "stroll") BondingStroll();
    }

    // The pasture (bn40 on; plan 35 · BN5, BN6): a box of thirty laid out from Jubilife's Pokémon Center, the herd from the
    // gate and from the pond, one spoken to, the board's box picker, the pickers and bathers at work, after dark
    private void BondingPasture()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        string[] kinds = { "Bidoof", "Starly", "Shinx", "Budew", "Kricketot", "Ponyta", "Buneary", "Pachirisu", "Buizel", "Shellos" };
        for (int i = 0; i < PcBoxes.BoxSize; i++)
            game.Boxes.Boxes[0].Slots[i] = new Pokemon(PokemonDatabase.Get(kinds[i % kinds.Length])!, 8 + i % 7);
        for (int i = 0; i < 6; i++)
            game.Boxes.Boxes[1].Slots[i] = new Pokemon(PokemonDatabase.Get(kinds[(i + 3) % kinds.Length])!, 12);
        game.Boxes.CurrentBox = 0;
        game.GiveIds();
        // Two away in the orchard and two in the hot spring, seen at their work
        foreach (var (slot, kind) in new[] { (6, JobKind.Orchard), (7, JobKind.Orchard), (8, JobKind.HotSpring), (9, JobKind.HotSpring) })
        {
            game.JobBoard.Post(new Pokemon(PokemonDatabase.Get(kinds[slot % kinds.Length])!, 14), kind, 8, WallClock.UtcNow);
        }
        foreach (string k in kinds) { PokemonModels.Request(k); }
        FieldSprites.Patient = true;
        game.Place(MapDatabase.Get("JubilifePokemonCenter"), 12, 4, Direction.Up);
        game.ArriveOnMap();
        Frames(10);
        Console.WriteLine($"pasture here: {game.PastureHere}");
        game.Pasture("enter");
        for (int i = 0; i < 240 && game.Map.Name != "SinnohPasture"; i++) Frames(1);
        Frames(90);
        Shot("bn40_pasture_gate");
        Console.WriteLine($"herd {game.Herd.Count}, workers at work, map {game.Map.Name}");
        game.Place(game.Map, 18, 5, Direction.Left);
        Frames(60);
        Shot("bn41_pasture_from_the_pond");
        game.Place(game.Map, 6, 10, Direction.Up);
        Frames(30);
        Shot("bn42_pasture_orchard_and_spring");
        // The board's box picker
        game.Pasture("pick");
        Frames(20);
        Shot("bn43_pasture_pick_box");
        game.PcScreen.Move(1, 0, game.Party.Count);
        Frames(10);
        Shot("bn44_pasture_pick_box_next");
        game.PcScreen.Confirm(game.Party, game.Boxes, _ => { });
        Frames(60);
        Console.WriteLine($"picked: {game.Pasture("picked").Result}, herd {game.Herd.Count}");
        Shot("bn45_pasture_other_box");
        game.Pasture("box", -1);
        Frames(30);
        engine.Settings.TimeOfDay = TimeOfDay.Night;
        engine.ApplySettings(window: false);
        game.Place(game.Map, 13, 12, Direction.Up);
        Frames(60);
        Shot("bn46_pasture_after_dark");
        engine.Settings.TimeOfDay = TimeOfDay.Day;
        engine.ApplySettings(window: false);
        game.Pasture("leave");
        for (int i = 0; i < 240 && game.Map.Name == "SinnohPasture"; i++) Frames(1);
        Frames(30);
    }

    // The Stroll Counter (bn70 on; plan 35 · BN7): the boxes opened to pick its rider, its screen, a seek and a meet
    private void BondingStroll()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        game.Bag.AddItem(ItemDatabase.Get(StrollCounter.Item)!, 1);
        if (game.Boxes.Boxes[2].Slots[0] == null) game.Boxes.Boxes[2].Slots[0] = new Pokemon(PokemonDatabase.Get("Shinx")!, 14);
        game.Boxes.CurrentBox = 2;
        game.GiveIds();
        PokemonSprites.Request("Shinx");
        game.StrollAct("pick");
        Frames(20);
        Shot("bn70_stroll_pick");
        game.PcScreen.Confirm(game.Party, game.Boxes, _ => { });
        Frames(4);
        Console.WriteLine($"put: {game.StrollAct("put").Result}");
        for (int i = 0; i < 20 * 120; i++) StrollCounter.Step(game.Stroll);
        game.OpenStroll();
        Frames(30);
        Shot("bn71_stroll_home");
        var screen = game.StrollScreen;
        screen.Confirm(); // SEEK
        Frames(10);
        Shot("bn72_stroll_seek");
        screen.Confirm();
        Frames(10);
        Shot("bn73_stroll_seek_looked");
        while (screen.Page != StrollPage.Home) screen.Confirm();
        screen.Move(0, 1);
        screen.Confirm(); // MEET
        Frames(20);
        Shot("bn74_stroll_meet");
        screen.Confirm();
        Frames(10);
        Shot("bn75_stroll_meet_round");
        Console.WriteLine($"meet: {screen.Meet?.Species} {screen.Meet?.End}, held {game.Stroll.Caught.Count}/{game.Stroll.Found.Count}");
        screen.Cancel();
        screen.Cancel();
        Frames(4);
    }

    // The camp and the care close-up (bn01 to bn10)
    private void BondingCare()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        game.Party.Clear();
        var turtwig = new Pokemon(PokemonDatabase.Get("Turtwig")!, 18) { Friendship = 90 };
        game.Party.Add(turtwig);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Starly")!, 15));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Shinx")!, 14));
        game.BuildSave();
        foreach (var p in game.Party.Members) { PokemonModels.Get(p.LookName); PokemonSprites.Request(p.LookName); }
        void Tell(string at)
        {
            var page = game.Care.Of(turtwig);
            Console.WriteLine($"{at}: marks [{string.Join(",", page.Marks)}], mood {page.Mood} ({CareRules.MoodOf(page.Mood)}), full {CareRules.FullnessOn(page, game.CareDay)}, friendship {turtwig.Friendship}, status {turtwig.Status}, note \"{game.CareCloseUp.Note}\"");
        }

        game.ChooseFromStartMenu(StartMenuChoice.Camp);
        Frames(30);
        Shot("bn01_camp_clearing");

        game.Camp.AskToCare();
        Frames(4);
        Frames(20);
        var care = game.CareCloseUp;
        Shot("bn02_closeup");
        Console.WriteLine($"layout: head {care.Layout.Head}, body {care.Layout.Body}, liked {care.Layout.Liked}, laid out {care.LaidOut}");

        // The hand over the liked spot, strokes back and forth
        care.MoveHand(care.Layout.Liked + new Vector2(0.06f, -0.05f) - care.Hand);
        Frames(6);
        Shot("bn03_hand_liked_spot");
        care.Hold();
        for (int i = 0; i < 80 && turtwig.Friendship == 90; i++) { care.MoveHand(new Vector2(i % 2 == 0 ? 0.03f : -0.03f, 0f)); Frames(1); }
        care.Release();
        Frames(30);
        Shot("bn04_pat_lean");
        Tell("pat");

        // A sand battle's dust, and the brush
        CareRules.AfterBattle(game.Care, new CareRules.BattleOutcome(new[] { turtwig }, true, false, Array.Empty<Pokemon>(),
            PokemonPlatinumEngine.Battle.Sim.BattleTerrain.Sand, PokemonPlatinumEngine.Battle.Sim.BattleWeather.None));
        CareRules.Soil(game.Care.Of(turtwig), SoilMark.Mud);
        care.ChooseTool(CareTool.Brush);
        care.MoveHand(care.Layout.Mark(0) - care.Hand);
        Frames(6);
        Shot("bn05_dust_and_mud");
        care.Hold();
        for (int i = 0; i < 20; i++) { care.MoveHand(new Vector2(i % 2 == 0 ? 0.025f : -0.025f, 0f)); Frames(1); }
        Shot("bn06_brushing");
        for (int i = 0; i < 40 && game.Care.Of(turtwig).Marks.Contains(SoilMark.Dust); i++) { care.MoveHand(new Vector2(i % 2 == 0 ? 0.025f : -0.025f, 0f)); Frames(1); }
        care.Release();
        Frames(10);
        Tell("brushed");

        // Poison, and the medicine
        turtwig.Status = StatusCondition.Poison;
        care.ChooseTool(CareTool.Medicine);
        care.MoveHand(care.Layout.Body - care.Hand);
        Frames(6);
        Shot("bn07_poisoned");
        care.Hold();
        Frames(20);
        Shot("bn08_cured");
        Tell("cured");

        // A berry eaten
        var berry = ItemDatabase.Get("Oran Berry")!;
        game.Bag.AddItem(berry, 3);
        care.ChooseTool(CareTool.Treat);
        care.Feed(berry, game.Bag);
        Frames(40);
        Shot("bn09_eating");
        Tell("fed");

        // After dark: sleepy
        care.Open(turtwig, game.Care, game.CareDay, night: true);
        Frames(90);
        Shot("bn10_sleepy");
        care.Cancel();
        Frames(4);
    }
}
