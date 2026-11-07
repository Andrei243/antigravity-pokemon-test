using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q6 and Q7: the pointer. The window's point turned into the layout's through a letterboxed and a
/// pillarboxed picture; each screen's hit test on the rectangle it draws and beside it; the arrow shown and hidden;
/// the battle's commands, cards and targets; and the game itself played by the mouse alone with no window.
/// </summary>
[Collection("Shared clocks and dice")]
public class PointerTests
{
    private const float Frame = 1f / 60f;

    private static Vector2 Middle(Rectangle r) => new(r.X + r.Width / 2f, r.Y + r.Height / 2f);
    private static Vector2 Beside(Rectangle r) => new(r.X - 6f, r.Y + r.Height / 2f);

    // ---------------------------------------------------------------- the window and the layout

    [Fact]
    public void AWindowsPointIsReadInTheLayoutsUnitsThroughTheBars()
    {
        // A window wider than 16:9 is pillarboxed: bars left and right
        var pillar = Viewport.Fit(2560, 1080);
        Assert.Equal(1f, pillar.Scale);
        Assert.Equal(320f, pillar.X);
        Assert.Equal(0f, pillar.Y);
        Assert.Equal(new Vector2(0, 0), UiPointer.ToLayout(pillar, new Vector2(320, 0)));
        Assert.Equal(new Vector2(960, 540), UiPointer.ToLayout(pillar, new Vector2(1280, 540)));
        Assert.False(Viewport.OnThePicture(UiPointer.ToLayout(pillar, new Vector2(100, 540))));

        // A taller one is letterboxed: bars above and below, and the picture scaled to the width
        var letter = Viewport.Fit(1280, 1024);
        Assert.Equal(1280f / 1920f, letter.Scale, 4);
        Assert.Equal(0f, letter.X);
        Assert.Equal((1024 - 720) / 2, letter.Y);
        var corner = UiPointer.ToLayout(letter, new Vector2(1279, 152 + 719));
        Assert.True(Math.Abs(corner.X - 1918.5f) < 1f && Math.Abs(corner.Y - 1078.5f) < 1f, $"{corner}");
        Assert.False(Viewport.OnThePicture(UiPointer.ToLayout(letter, new Vector2(640, 40))));

        // And a 4K window shows the layout at twice its size
        var big = Viewport.Fit(3840, 2160);
        Assert.Equal(new Vector2(960, 540), UiPointer.ToLayout(big, new Vector2(1920, 1080)));
    }

    [Fact]
    public void TheArrowShowsOnlyAfterTheMouseMovedAndHidesWhenStillOrAKeyIsPressed()
    {
        var pointer = new UiPointer();
        pointer.Update(default, false, Frame);
        Assert.False(pointer.Shown);
        pointer.Update(PointerFrame.Over(new Vector2(400, 300)), false, Frame);
        Assert.True(pointer.Shown);
        Assert.Equal(new Vector2(400, 300), pointer.At);
        for (float t = 0; t < UiPointer.HideAfter - 0.1f; t += Frame) pointer.Update(default, false, Frame);
        Assert.True(pointer.Shown);
        for (int i = 0; i < 12; i++) pointer.Update(default, false, Frame);
        Assert.False(pointer.Shown);

        pointer.Update(PointerFrame.Over(new Vector2(10, 10)), false, Frame);
        pointer.Update(default, true, Frame);
        Assert.False(pointer.Shown);

        // On the bars round the picture there is no arrow
        pointer.Update(new PointerFrame(-40, 300, PointerButtons.Moved, 0f), false, Frame);
        Assert.False(pointer.Shown);
    }

    [Fact]
    public void ARecordingKeepsTheMouseFrameByFrame()
    {
        var recording = new InputRecording { DataVersion = GameDataFiles.Version };
        var pointed = new PointerFrame(812.5f, 431f, PointerButtons.Moved | PointerButtons.Click | PointerButtons.Inside, -1f);
        recording.Frames.Add(new InputFrame(Frame, 0, ActionFrame.None, WindowEvents.None, pointed));
        recording.Frames.Add(new InputFrame(Frame, 0, ActionFrame.Press(GameAction.Confirm)));
        using var stream = new MemoryStream();
        recording.Write(stream);
        stream.Position = 0;
        var back = InputRecording.Read(stream);
        Assert.Equal(pointed, back.Frames[0].Pointer);
        Assert.Equal(default, back.Frames[1].Pointer);
    }

    // ---------------------------------------------------------------- the menus (plan 12 · Q6)

    [Fact]
    public void TheStartMenuAndItsQuitQuestionAreFoundUnderThePointer()
    {
        var menu = new StartMenu();
        menu.Open();
        var rows = ModernUi.StartMenuRows(GameEngine.VirtualWidth, menu.EntryCount);
        for (int i = 0; i < rows.Length; i++)
        {
            Assert.Equal(i, menu.HitTest(Middle(rows[i])));
            Assert.Null(menu.HitTest(Beside(rows[i])));
        }
        // The rows don't overlap, and the last stands under its rule
        for (int i = 1; i < rows.Length; i++) Assert.True(rows[i].Y >= rows[i - 1].Y + rows[i - 1].Height);
        menu.PointAt(3);
        Assert.Equal(3, menu.SelectedIndex);

        menu.SelectedIndex = menu.EntryCount - 1;
        menu.Confirm();
        Assert.True(menu.AskingToQuit);
        var buttons = ModernUi.PromptButtons(GameEngine.VirtualWidth, GameEngine.VirtualHeight, StartMenu.QuitAnswers.Length);
        Assert.Equal(2, menu.HitTest(Middle(buttons[2])));
        menu.PointAt(2);
        Assert.Equal(2, menu.QuitIndex);
    }

    [Fact]
    public void AQuestionsAnswersAreFoundUnderThePointer()
    {
        var box = new ChoiceBox();
        box.Open(new[] { "YES", "NO", "MAYBE" });
        var rows = ModernUi.ChoiceRows(GameEngine.VirtualWidth, GameEngine.VirtualHeight, box.Options, box.Cursor);
        Assert.Equal(3, rows.Count);
        foreach (var (index, rect) in rows)
        {
            Assert.Equal(index, ModernUi.HitRow(rows, Middle(rect)));
            Assert.Null(ModernUi.HitRow(rows, Beside(rect)));
        }
        box.PointAt(2);
        Assert.Equal(2, box.Cursor);
    }

    [Fact]
    public void TheTitlesCardsRowsAndQuestionsAreFoundUnderThePointer()
    {
        var title = new TitleScreen(new SaveData { PlayerName = "Lucas" });
        // The opening: a click anywhere is the A button
        Assert.Equal(UiPointer.Anywhere, title.HitTest(new Vector2(100, 100)));
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        var rects = title.EntryRects();
        for (int i = 0; i < rects.Count; i++)
        {
            Assert.Equal(i, title.HitTest(Middle(rects[i])));
            Assert.Null(title.HitTest(Beside(rects[i])));
        }
        int delete = title.Entries.ToList().IndexOf(TitleChoice.Delete);
        title.PointAt(delete);
        Assert.Equal(delete, title.SelectedIndex);
        title.PressConfirm();
        // With one save DELETE A SAVE asks of it at once
        Assert.Equal(TitlePhase.ConfirmDelete, title.Phase);
        var buttons = ModernUi.PromptButtons(GameEngine.VirtualWidth, GameEngine.VirtualHeight, 2);
        Assert.Equal(1, title.HitTest(Middle(buttons[1])));
        title.PointAt(1);
        Assert.True(title.ConfirmYes);
        title.PointAt(0);
        Assert.False(title.ConfirmYes);

        // With two, it asks which: only the cards are pointed at
        var two = TitleScreen.WithSlots(new[] { new TitleSlot(1, new SaveData { PlayerName = "Lucas" }), new TitleSlot(2, new SaveData { PlayerName = "Dawn" }) });
        two.PressConfirm();
        two.PressConfirm();
        int deleteOfTwo = two.Entries.ToList().IndexOf(TitleChoice.Delete);
        var rectsOfTwo = two.EntryRects();
        two.PointAt(deleteOfTwo);
        two.PressConfirm();
        Assert.Equal(TitlePhase.ChooseDelete, two.Phase);
        Assert.Null(two.HitTest(Middle(rectsOfTwo[deleteOfTwo])));
        Assert.Equal(1, two.HitTest(Middle(rectsOfTwo[1])));
        two.PointAt(1);
        Assert.Equal(1, two.SelectedIndex);
    }

    [Fact]
    public void TheOptionsRowsAndTheSavesButtonsAreFoundUnderThePointer()
    {
        var options = new OptionsScreen();
        options.Open();
        for (int k = 0; k < OptionsScreen.VisibleRows; k++)
        {
            Assert.Equal(options.FirstRow + k, options.HitTest(Middle(options.RowRect(k))));
            Assert.Null(options.HitTest(Beside(options.RowRect(k))));
        }
        options.PointAt(4);
        Assert.Equal(4, options.SelectedIndex);
        // A tab under the pointer: a hover leaves the cursor, a click puts the tab on show
        for (int i = 0; i < OptionsScreen.Categories.Count; i++)
            Assert.Equal(OptionsScreen.TabHit + i, options.HitTest(Middle(OptionsScreen.TabRect(i))));
        options.PointAt(OptionsScreen.TabHit + 2);
        Assert.Equal((OptionCategory.Game, 4), (options.Category, options.SelectedIndex));
        Assert.True(options.Press(OptionsScreen.TabHit + 2));
        Assert.Equal((OptionCategory.Battle, 0), (options.Category, options.SelectedIndex));
        Assert.False(options.Press(1));

        var save = new SaveScreen();
        save.Open(new SaveData { PlayerName = "Lucas" });
        var buttons = ModernUi.SaveButtons(GameEngine.VirtualWidth);
        Assert.Equal(0, save.HitTest(Middle(buttons[0])));
        Assert.Equal(1, save.HitTest(Middle(buttons[1])));
        Assert.Null(save.HitTest(Beside(buttons[0])));
        save.PointAt(1);
        Assert.Equal(1, save.AnswerIndex);
    }

    [Fact]
    public void TheBagsRowsTabsAndTheTeamsCardsAreFoundUnderThePointer()
    {
        var bag = new BagScreen();
        var inventory = new Inventory();
        inventory.AddItem(ItemDatabase.Get("Potion")!, 3);
        inventory.AddItem(ItemDatabase.Get("Antidote")!, 1);
        inventory.AddItem(ItemDatabase.Get("Revive")!, 1);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
        bag.Open();
        bag.CurrentPocket = ItemPocket.Medicine;
        var rows = ModernUi.ListRows(ModernUi.BagList(), 0, BagScreen.VisibleRows, 3, 26);
        foreach (var (index, rect) in rows)
        {
            Assert.Equal(index, bag.HitTest(Middle(rect), inventory, party));
            Assert.Null(bag.HitTest(Beside(rect), inventory, party));
        }
        bag.PointAt(2, inventory, party);
        Assert.Equal(2, bag.SelectedIndex);

        // A tab opens its pocket on a click, and a hover leaves it be
        var tabs = ModernUi.TabRects(ModernUi.BagTabStrip(GameEngine.VirtualWidth), BagScreen.Pockets.Length);
        int balls = Array.IndexOf(BagScreen.Pockets, ItemPocket.PokeBalls);
        Assert.Equal(BagScreen.TabHits + balls, bag.HitTest(Middle(tabs[balls]), inventory, party));
        bag.PointAt(BagScreen.TabHits + balls, inventory, party);
        Assert.Equal(ItemPocket.Medicine, bag.CurrentPocket);
        Assert.True(bag.Press(BagScreen.TabHits + balls));
        Assert.Equal(ItemPocket.PokeBalls, bag.CurrentPocket);

        // The party's cards, two to a row, as both of its screens draw them
        var screen = new PartyScreen();
        screen.Open();
        for (int i = 0; i < Party.MaxSize; i++)
            Assert.Equal(i < party.Count ? i : null, screen.HitTest(Middle(ModernUi.PartyCardRect(i)), party));
    }

    [Fact]
    public void AShopsQuestionAndListAreFoundUnderThePointer()
    {
        var shop = new ShopScreen();
        shop.Open("Mart", new[] { ItemDatabase.Get("Potion")!, ItemDatabase.Get("Poké Ball")!, ItemDatabase.Get("Antidote")! });
        var buttons = ModernUi.ShopQuestionButtons(GameEngine.VirtualWidth, GameEngine.VirtualHeight);
        for (int i = 0; i < buttons.Length; i++) Assert.Equal(i, shop.HitTest(Middle(buttons[i])));
        shop.PointAt(1);
        Assert.Equal(1, shop.ChoiceIndex);
        shop.PointAt(0);
        shop.Confirm(new Inventory(), 1000, _ => { });
        Assert.Equal(ShopMode.Buying, shop.Mode);
        var rows = ModernUi.ListRows(ModernUi.ShopList(), 0, ShopScreen.VisibleRows, 3, 40);
        foreach (var (index, rect) in rows)
        {
            Assert.Equal(index, shop.HitTest(Middle(rect)));
            Assert.Null(shop.HitTest(Beside(rect)));
        }
        shop.PointAt(2);
        Assert.Equal(2, shop.SelectedIndex);
    }

    // ---------------------------------------------------------------- the battle (plan 12 · Q7)

    private static readonly MoveData Idle = new() { Name = "Idle", Type = PokemonType.Normal, Category = MoveCategory.Status, MaxPP = 40, Target = MoveTarget.User };

    private static Pokemon Mon(string species, int level, params MoveData[] moves)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);
        if (moves.Length > 0)
        {
            p.Moves.Clear();
            foreach (var m in moves) p.Moves.Add(new Move(m));
        }
        return p;
    }

    private static void Settle(BattleEngine battle)
    {
        for (int i = 0; i < 400 && !battle.IsBattleOver && battle.HUD.MenuState == BattleMenuState.Message; i++)
        {
            battle.ConfirmMessage();
            battle.Update(Frame);
        }
    }

    // A click on a point: the cursor brought onto what is there, and the A button pressed, as the engine does
    private static void Click(BattleEngine battle, Vector2 at)
    {
        int? hit = battle.HitTest(at);
        if (hit is int i && i != UiPointer.Anywhere) battle.PointAtIndex(i);
        InputManager.Feed(hit != null ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        try { battle.Update(Frame); }
        finally { InputManager.Feed(null); }
    }

    [Fact]
    public void TheCommandsDontOverlapAndStandInTheCursorsOrder()
    {
        var rects = Enumerable.Range(0, 4).Select(ModernUi.CommandRect).ToArray();
        for (int a = 0; a < 4; a++)
            for (int b = a + 1; b < 4; b++)
                Assert.False(rects[a].X < rects[b].X + rects[b].Width && rects[b].X < rects[a].X + rects[a].Width
                    && rects[a].Y < rects[b].Y + rects[b].Height && rects[b].Y < rects[a].Y + rects[a].Height, $"commands {a} and {b} overlap");
        // FIGHT on the left of the stack; BAG, POKéMON and RUN down the stack as the cursor goes down it
        Assert.True(rects[0].X + rects[0].Width <= rects[1].X);
        Assert.True(rects[1].Y < rects[2].Y && rects[2].Y < rects[3].Y);
        Assert.False(UiPointer.Inside(ModernUi.AutoRect, Middle(rects[0])));

        var party = new Party();
        party.Add(Mon("Turtwig", 20, MoveDatabase.Get("Tackle")!));
        var battle = new BattleEngine(new BattleSetup { PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon("Bidoof", 3, Idle) }, Random = new Random(3) });
        Settle(battle);
        Assert.Equal(BattleMenuState.Main, battle.HUD.MenuState);
        for (int i = 0; i < 4; i++) Assert.Equal(i, battle.HitTest(Middle(rects[i])));
        // The cursor down from BAG goes where the pointer finds the next command
        battle.PointAtIndex(1);
        battle.MoveCursor(0, 1);
        Assert.Equal(battle.HitTest(Middle(rects[2])), battle.HUD.MainMenuIndex);
        Assert.Null(battle.HitTest(new Vector2(600, 400)));
    }

    [Fact]
    public void AWholeBattleIsFoughtWithTheMouse()
    {
        var party = new Party();
        party.Add(Mon("Turtwig", 30, MoveDatabase.Get("Tackle")!));
        var battle = new BattleEngine(new BattleSetup { PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { Mon("Bidoof", 2, Idle) }, Random = new Random(5) });
        for (int guard = 0; guard < 2000 && !battle.IsBattleOver; guard++)
        {
            var at = battle.HUD.MenuState switch
            {
                BattleMenuState.Main => Middle(ModernUi.CommandRect(0)),
                BattleMenuState.Moves => Middle(ModernUi.MoveCardRect(0)),
                // Lines are read on by a click anywhere
                _ => new Vector2(700, 500)
            };
            Click(battle, at);
            battle.Update(0.2f);
        }
        Assert.True(battle.IsBattleOver);
        Assert.Equal(BattleResult.PlayerVictory, battle.Result);
    }

    [Fact]
    public void AClickOnATargetThatCantBeAimedAtDoesNothing()
    {
        var party = new Party();
        party.Add(Mon("Turtwig", 20, MoveDatabase.Get("Tackle")!));
        party.Add(Mon("Piplup", 20, Idle));
        var twins = new Trainer { Name = "Liv & Liz", TrainerClass = "Twins", PrizeMoney = 400 };
        twins.Party.Add(Mon("Bidoof", 18, Idle));
        twins.Party.Add(Mon("Starly", 18, Idle));
        var battle = new BattleEngine(new BattleSetup { PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            Format = BattleFormat.Double, Trainers = new() { twins }, Random = new Random(1) });
        Settle(battle);
        battle.SelectMove(0);
        Assert.Equal(BattleMenuState.SelectTarget, battle.HUD.MenuState);

        // Turtwig's own card is greyed: nothing under the pointer there, and a click there does nothing
        var layout = battle.TargetLayout;
        int self = layout.ToList().IndexOf(battle.PlayerSlots[0]);
        var greyed = Middle(ModernUi.TargetCard(self, battle.TargetColumns));
        Assert.Null(battle.HitTest(greyed));
        int before = battle.HUD.TargetMenuIndex;
        Click(battle, greyed);
        Assert.Equal(BattleMenuState.SelectTarget, battle.HUD.MenuState);
        Assert.Equal(before, battle.HUD.TargetMenuIndex);

        // Each card that can be aimed at answers with its place among the choices
        for (int i = 0; i < layout.Count; i++)
            if (layout[i] is { } place && battle.TargetChoices.Contains(place))
                Assert.Equal(battle.TargetChoices.ToList().IndexOf(place), battle.HitTest(Middle(ModernUi.TargetCard(i, battle.TargetColumns))));

        // A click on the partner aims there
        int partner = layout.ToList().IndexOf(battle.PlayerSlots[1]);
        Click(battle, Middle(ModernUi.TargetCard(partner, battle.TargetColumns)));
        Assert.NotEqual(BattleMenuState.SelectTarget, battle.HUD.MenuState);
    }

    [Fact]
    public void TheEvolutionsChoiceOfAMoveToForgetIsFoundUnderThePointer()
    {
        var panel = new Rectangle(GameEngine.VirtualWidth - 64 - 820, 64, 820, 716);
        for (int i = 0; i <= 4; i++) Assert.Equal(i, ModernUi.HitMoveChoice(panel, 4, Middle(ModernUi.MoveChoiceRow(panel, i))));
        Assert.Null(ModernUi.HitMoveChoice(panel, 2, Middle(ModernUi.MoveChoiceRow(panel, 3))));
        Assert.Equal(4, ModernUi.HitMoveChoice(panel, 2, Middle(ModernUi.MoveChoiceRow(panel, 4))));
    }

    // ---------------------------------------------------------------- the game, by the mouse alone

    [Fact]
    public void TheFieldsMenuOpensOnARightClickAndAClickChoosesFromIt()
    {
        var heard = AudioManager.Listen();
        try
        {
            Dice.Seed(7);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
            game.State = GameState.Overworld;

            void Run(PointerFrame? mouse = null)
            {
                if (mouse is { } m) game.Point(m);
                InputManager.Feed(ActionFrame.None);
                engine.Update(Frame);
            }
            for (int i = 0; i < 10; i++) Run();

            // A right click in the field opens the start menu
            Run(PointerFrame.Over(new Vector2(600, 600), PointerButtons.Moved | PointerButtons.RightClick));
            Assert.True(game.StartMenu.IsActive);
            Assert.True(game.Pointer.Shown);

            // The pointer over a row moves the cursor there; a click on OPTIONS opens them
            var rows = ModernUi.StartMenuRows(GameEngine.VirtualWidth, game.StartMenu.EntryCount);
            Run(PointerFrame.Over(Middle(rows[1])));
            Assert.Equal(1, game.StartMenu.SelectedIndex);
            int options = game.StartMenu.EntryCount - 3;
            Run(PointerFrame.Over(Middle(rows[options]), PointerButtons.Moved | PointerButtons.Click));
            Assert.Equal(GameState.Options, game.State);

            // A right click backs out of them, and the keyboard owns the screen again once a key is pressed
            Run(PointerFrame.Over(new Vector2(600, 600), PointerButtons.RightClick));
            Assert.NotEqual(GameState.Options, game.State);
            InputManager.Feed(ActionFrame.Press(GameAction.Down));
            engine.Update(Frame);
            Assert.False(game.Pointer.Shown);
        }
        finally
        {
            InputManager.Feed(null);
            InputManager.FeedPointer(default);
            AudioManager.StopListening();
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
        }
    }
}
