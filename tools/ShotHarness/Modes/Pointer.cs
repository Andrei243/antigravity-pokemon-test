using System;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI;

partial class Harness
{
    // ---------------------------------------------------------------- the pointer (plan 12 · Q6, Q7)

    // The game played with the mouse alone (Modes/Pointer.cs, not part of `all`): the field's start menu opened by a
    // right click, its rows under the pointer, the bag's rows and a tab clicked, the options' rows, the save's buttons;
    // then a battle: the pointer over FIGHT, FIGHT clicked, a move card under the pointer, the move clicked and the
    // turn read on by clicks to the menu again. Each shot prints what lies under the pointer and where the cursor is.
    public void PointerMode()
    {
        // A frame of the mouse: moved to a place, and clicked (left or right) there if asked
        void Point(Vector2 at, bool click = false, bool right = false, int frames = 1)
        {
            var buttons = PointerButtons.Moved | (click ? PointerButtons.Click : 0) | (right ? PointerButtons.RightClick : 0);
            game.Point(PointerFrame.Over(at, buttons));
            Frames(frames);
        }
        Vector2 Middle(Raylib_cs.Rectangle r) => new(r.X + r.Width * 0.6f, r.Y + r.Height / 2f);
        void Tell(string shot, Vector2 at) =>
            Console.WriteLine($"pointer: {shot}: at {at.X:0},{at.Y:0}, over {game.Pointable?.HitTest(at)?.ToString() ?? "nothing"}, arrow {(game.Pointer.Shown ? "shown" : "hidden")}, state {game.State}");

        // ---- The field: a right click opens the start menu, and the pointer over its third row
        At("Sinnoh", 116, 886, Direction.Down);
        Frames(4);
        Point(new Vector2(700, 600), right: true);
        Frames(24);
        var rows = ModernUi.StartMenuRows(GameEngine.VirtualWidth, game.StartMenu.EntryCount);
        var overBag = Middle(rows[game.StartMenu.HasBoxes ? 3 : 2]);
        Point(overBag);
        Tell("28g_pointer_start_menu", overBag);
        Console.WriteLine($"pointer: the start menu's cursor on {game.StartMenu.SelectedIndex} of {game.StartMenu.EntryCount}");
        Shot("28g_pointer_start_menu");

        // ---- The bag: clicked open, the Medicine tab clicked, the pointer over its second row
        Point(overBag, click: true);
        Frames(30);
        Console.WriteLine($"pointer: the click opened {game.State}");
        var ballsTab = Middle(ModernUi.TabRects(ModernUi.BagTabStrip(GameEngine.VirtualWidth), PokemonPlatinumEngine.UI.BagScreen.Pockets.Length)[1]);
        Point(ballsTab, click: true);
        Frames(10);
        Tell("28g_pointer_bag_tab", ballsTab);
        Console.WriteLine($"pointer: the bag's pocket is {game.BagScreen.CurrentPocket}");
        Shot("28g_pointer_bag_tab");
        var second = Middle(ModernUi.RowRect(ModernUi.BagList(), 1, 26));
        Point(second);
        Tell("28g_pointer_bag_row", second);
        Console.WriteLine($"pointer: the bag's cursor on {game.BagScreen.SelectedIndex}");
        Shot("28g_pointer_bag_row");

        // ---- Back out by a right click, the options by clicks, the pointer over a row
        Point(ballsTab, right: true);
        Frames(20);
        Console.WriteLine($"pointer: the right click left {game.State}, the start menu {(game.StartMenu.IsActive ? "open" : "shut")}");
        if (!game.StartMenu.IsActive)
        {
            Point(new Vector2(700, 600), right: true);
            Frames(24);
        }
        rows = ModernUi.StartMenuRows(GameEngine.VirtualWidth, game.StartMenu.EntryCount);
        var overOptions = Middle(rows[game.StartMenu.EntryCount - 3]);
        Point(overOptions, click: true);
        Frames(24);
        Console.WriteLine($"pointer: the click on row {game.StartMenu.EntryCount - 3} opened {game.State}");
        var optionRow = Middle(game.Options.RowRect(3));
        Point(optionRow);
        Tell("28g_pointer_options_row", optionRow);
        Shot("28g_pointer_options_row");
        Point(optionRow, right: true);
        Frames(20);

        // ---- The save: its CANCEL under the pointer, then clicked
        if (!game.StartMenu.IsActive)
        {
            Point(new Vector2(700, 600), right: true);
            Frames(24);
        }
        rows = ModernUi.StartMenuRows(GameEngine.VirtualWidth, game.StartMenu.EntryCount);
        Point(Middle(rows[game.StartMenu.EntryCount - 4]), click: true);
        Frames(30);
        Console.WriteLine($"pointer: the click on row {game.StartMenu.EntryCount - 4} opened {game.State}");
        var cancel = Middle(ModernUi.SaveButtons(GameEngine.VirtualWidth, game.SaveScreen.Objective)[1]);
        Point(cancel);
        Tell("28g_pointer_save_button", cancel);
        Shot("28g_pointer_save_button");
        Point(cancel, click: true);
        Frames(30);
        Console.WriteLine($"pointer: CANCEL clicked, the game is in {game.State}");

        // ---- A battle fought with the mouse
        var b = StartBattle("Starly", 3);
        ToMainMenu(b);
        var fight = Middle(ModernUi.CommandRect(0));
        Point(fight);
        Tell("23p_pointer_fight", fight);
        Shot("23p_pointer_fight");
        Point(fight, click: true);
        Frames(6);
        Console.WriteLine($"pointer: FIGHT clicked, the menu is {b.HUD.MenuState}");
        var card = Middle(ModernUi.MoveCardRect(0));
        Point(card);
        Tell("23p_pointer_move_card", card);
        Console.WriteLine($"pointer: the move card under the pointer is {b.HUD.MoveMenuIndex}: {b.Fighter.Moves[b.HUD.MoveMenuIndex].DisplayName}");
        Shot("23p_pointer_move_card");
        Point(card, click: true);
        // The turn read on by clicks anywhere until the menu is open again
        for (int i = 0; i < 24 && b.HUD.MenuState != BattleMenuState.Main && !b.IsBattleOver; i++)
        {
            Skip(0.8);
            Point(new Vector2(960, 500), click: true);
        }
        Skip(0.5);
        Point(fight);
        Console.WriteLine($"pointer: after the turn the menu is {b.HUD.MenuState}, the foe at {b.EnemyPokemon.CurrentHP}/{b.EnemyPokemon.MaxHP}");
        Shot("23p_pointer_turn_read_on");
    }
}
