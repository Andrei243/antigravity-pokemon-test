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
