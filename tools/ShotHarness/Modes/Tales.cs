using System;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

partial class Harness
{
    // ---------------------------------------------------------------- the Tales (plan 46 · CP11)

    // The Tales (tl*; not part of "all"): the Book of Tales in the bag and three tales learned by their flags, a wild
    // battle's move menu with the TALE button offered (tl01) and armed (tl02), the tale told and its sky over the stage
    // (tl03, its line; tl04, the next menu under it), and the Book's pages (tl05, tl06). It prints the tale armed, the
    // rules' tale and the screen's.
    public void TalesMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        game.Bag.AddItem(ItemDatabase.Get(TaleTable.BookItem)!, 1);
        foreach (var id in new[] { "zmeu", "varcolac", "ielele" }) game.Story.Set(TaleTable.FlagOf(id));

        At("Sinnoh", 116, 886, Direction.Right);
        game.LocationSign.Hide();
        game.Party.Clear();
        var lead = new Pokemon(PokemonDatabase.Get("Monferno")!, 40);
        game.Party.Add(lead);
        game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Bidoof", MinLevel = 30, MaxLevel = 30, Weight = 100 });
        for (int i = 0; i < 600 && game.State != GameState.Battle; i++) Frames(1);
        var battle = game.Battle!;
        ToMainMenu(battle);
        battle.SelectMainMenuOption(0);
        Frames(20);
        Console.WriteLine($"tales: can tell {battle.CanTellTaleNow}, offered {battle.TaleOffered}");
        Shot("tl01_battle_tale_offered");
        battle.ToggleTale();
        Frames(10);
        Shot("tl02_battle_tale_armed");
        Console.WriteLine($"tales: armed {battle.TaleArmed}");
        int hit = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status);
        battle.SelectMove(Math.Max(0, hit));
        for (int i = 0; i < 400 && !battle.CurrentMessage.Contains("tale of"); i++) Frames(1);
        Skip(0.8);
        Shot("tl03_tale_told");
        ToMainMenu(battle);
        Frames(20);
        Shot("tl04_menu_under_the_tale");
        Console.WriteLine($"tales: rules {battle.Core.Field.Tale?.Id} {battle.Core.Field.Tale?.Turns}, screen {battle.Anim.Tale}");

        // ---- the Book, after the battle is left behind
        game.State = GameState.Overworld;
        game.OpenBookOfTales();
        Frames(30);
        Shot("tl05_book_first_page");
        game.BookOfTalesScreen.Turn(1);
        Frames(10);
        Shot("tl06_book_second_page");
        Console.WriteLine($"book: {game.BookOfTalesScreen.Tales.Count} tales, page {game.BookOfTalesScreen.Page} {game.BookOfTalesScreen.Current}");
        game.BookOfTalesScreen.Close();
        Frames(2);
    }
}
