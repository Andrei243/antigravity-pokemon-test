using System;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- materials, crafting and cooking (plan 39 · CR1)

    // Sunyshore City's workshop and the kit (cr*; not part of "all"): the way in at the dead end of the alley shut before
    // the Hall of Fame (cr01) and open after it (cr02), the workshop from its door (cr03), the craftswoman giving the kit
    // through her own script (cr04), a Bidoof felled on Route 201 and the toast of what it left (cr05) and the kit looked
    // into (cr06). Everything goes through the game's own steps, scripts and screens; it prints what each left behind
    // (the kit and its flag, the pouch, the toast).
    public void CraftingMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }

        // ---- the way in, shut: the player stands before the dead end with the Hall of Fame not yet entered
        At("Sinnoh", 870, 763, Direction.Right);
        game.LocationSign.Hide();
        Frames(20);
        Shot("cr01_workshop_door_shut");
        var door = game.Map.Warps.First(w => w.TargetMap == "SunyshoreWorkshop");
        Console.WriteLine($"door: {door.SourceX},{door.SourceY} opened by {door.OpenedBy}, solid {game.Map.IsSolid(door.SourceX, door.SourceY)}");

        // ---- and open, once the Hall of Fame has been entered (the story's flag, as its scene sets it)
        game.Story.Set("FLAG_GAME_COMPLETED");
        Frames(20);
        Shot("cr02_workshop_door_open");
        Console.WriteLine($"door: solid {game.Map.IsSolid(door.SourceX, door.SourceY)}");

        // ---- inside
        At("SunyshoreWorkshop", 6, 9, Direction.Up);
        game.LocationSign.Hide();
        Frames(20);
        Shot("cr03_workshop");

        // ---- the craftswoman: her lines read to the one that gives the kit
        At("SunyshoreWorkshop", 6, 5, Direction.Up);
        game.LocationSign.Hide();
        game.Interact(); Frames(3);
        bool shot = false;
        for (int i = 0; i < 40 && (Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue); i++)
        {
            if (engine.Choice.IsOpen) { engine.Choice.Confirm(); Frames(4); continue; }
            Until(() => Box().IsActive, "text on the screen");
            Box().FinishLine(); Frames(2);
            if (!shot && Box().VisibleText.Contains("received")) { Shot("cr04_kit_given"); shot = true; }
            Box().Advance(); Frames(2);
        }
        Until(() => game.State == GameState.Overworld && !Box().IsActive, "the field again");
        var kit = ItemDatabase.Get(GameEngine.CraftingKitItem)!;
        Console.WriteLine($"kit: {game.Bag.GetQuantity(kit)} in the bag, flag {game.Story.Has(GameEngine.CraftingKitFlag)}");

        // ---- a Bidoof felled on Route 201 leaves its material, and the field says so once it is back
        game.Crafting.Pouch.Clear();
        At("Sinnoh", 116, 886, Direction.Right);
        game.LocationSign.Hide();
        game.Party.Clear();
        var lead = new Pokemon(PokemonDatabase.Get("Turtwig")!, 60);
        game.Party.Add(lead);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 3, MaxLevel = 3, Weight = 100 });
        Until(() => game.State == GameState.Battle, "the battle");
        var battle = game.Battle!;
        int hit = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status);
        for (int guard = 0; guard < 400 && !battle.IsBattleOver; guard++)
        {
            if (battle.HUD.MenuState == BattleMenuState.Main) { battle.SelectMove(hit); Frames(1); }
            else Confirm(battle);
        }
        Until(() => game.State == GameState.Overworld, "the field after the battle");
        Frames(40);
        Shot("cr05_left_behind");
        Console.WriteLine($"toast: {(game.Toast.Visible ? game.Toast.Message : "(none)")}; pouch: {string.Join(", ", game.Crafting.Pouch.Held.Select(m => $"{m.Id} {game.Crafting.Pouch.Count(m.Id)}"))}");

        // ---- the kit, with a few things in it
        game.Crafting.Pouch.Add("emberCore", 2);
        game.Crafting.Pouch.Add("tidePearl", 1);
        game.Crafting.Pouch.Add("quarryStone", 14);
        game.Crafting.Pouch.Add("looseFeather", 6);
        game.OpenCrafting();
        Frames(30);
        Shot("cr06_pouch");
        Console.WriteLine($"kit screen: {game.CraftingScreen.Selected?.Id}, {game.CraftingScreen.Pouch.Kinds} kinds");
        game.CraftingScreen.Cancel();
        Frames(4);
    }
}
