using System.Text.Json;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// The menu screens rebuilt in plan 04 · G10 (style guide, "Menu screens" and "The new-game introduction"):
/// what their cursors, buttons and lists do, driven without a window as the game's keys would drive them.
/// </summary>
public class MenuScreenTests
{
    private const float Frame = 1f / 60f;

    private static ItemData Item(string name) => ItemDatabase.Get(name) ?? throw new InvalidOperationException(name + " is not an item");

    private static Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    // ------------------------------------------------------------------ lists

    [Fact]
    public void AListsWindowFollowsTheCursorWithContextAhead()
    {
        // Twenty rows, eight showing: nothing moves until the cursor comes within two rows of the bottom
        int first = 0;
        for (int cursor = 0; cursor <= 5; cursor++) Assert.Equal(0, first = UiNav.Window(first, cursor, 20, 8));
        Assert.Equal(1, first = UiNav.Window(first, 6, 20, 8));
        Assert.Equal(2, first = UiNav.Window(first, 7, 20, 8));
        // Coming back up, it stays until the cursor is two rows from the top
        Assert.Equal(2, first = UiNav.Window(first, 5, 20, 8));
        Assert.Equal(2, first = UiNav.Window(first, 4, 20, 8));
        Assert.Equal(1, first = UiNav.Window(first, 3, 20, 8));
        // It never shows past either end, wherever the cursor jumps to
        Assert.Equal(12, UiNav.Window(first, 19, 20, 8));
        Assert.Equal(0, UiNav.Window(12, 0, 20, 8));
        Assert.Equal(12, UiNav.Window(0, 17, 20, 8));
        // The cursor is always inside the window
        for (int cursor = 0; cursor < 20; cursor++)
        {
            int at = UiNav.Window(7, cursor, 20, 8);
            Assert.InRange(cursor, at, at + 7);
        }
        // A list that fits doesn't scroll
        Assert.Equal(0, UiNav.Window(3, 4, 6, 8));
        Assert.Equal(0, UiNav.Window(0, 0, 0, 8));

        Assert.Equal(0, UiNav.Wrap(4, 1, 5));
        Assert.Equal(4, UiNav.Wrap(0, -1, 5));
        Assert.Equal(0, UiNav.Wrap(0, 1, 0));
    }

    [Fact]
    public void TheMenuFrameHasTheStyleGuidesNumbers()
    {
        // Content between rows 132 and 1032, 64 margins, 32 gutters; rows 84 tall and 8 apart
        Assert.Equal((132f, 1032f, 64f, 32f), (ModernUi.ContentTop, ModernUi.ContentBottom, ModernUi.Margin, ModernUi.Gutter));
        Assert.Equal((84f, 8f), (ModernUi.RowHeight, ModernUi.RowPitch - ModernUi.RowHeight));
        // The rows of a panel lie inside it, one under another
        var panel = new Rectangle(ModernUi.Margin, ModernUi.ContentTop, 900, ModernUi.ContentBottom - ModernUi.ContentTop);
        int rows = ModernUi.RowsIn(panel.Height);
        for (int row = 0; row < rows; row++)
        {
            var r = ModernUi.RowRect(panel, row);
            Assert.True(r.X >= panel.X && r.X + r.Width <= panel.X + panel.Width);
            Assert.True(r.Y >= panel.Y && r.Y + r.Height <= panel.Y + panel.Height, $"row {row} leaves its panel");
            if (row > 0) Assert.Equal(ModernUi.RowPitch, r.Y - ModernUi.RowRect(panel, row - 1).Y, 3);
        }
        // A list that scrolls leaves room for its bar
        Assert.True(ModernUi.RowRect(panel, 0, scrolls: true).Width < ModernUi.RowRect(panel, 0).Width);
        // The save's summary is as tall on the title's card as over the field
        Assert.Equal(460f, ModernUi.SaveSummaryHeight);
        Assert.Equal(99, ShopScreen.MostAtOnce);
    }

    [Fact]
    public void TheListsShowAsManyRowsAsTheirPanelsHold()
    {
        float height = ModernUi.ContentBottom - ModernUi.ContentTop;
        Assert.Equal(ShopScreen.VisibleRows, ModernUi.RowsIn(height));
        // The bag's list sits under its tabs; the Pokédex's under its counts
        Assert.Equal(BagScreen.VisibleRows, ModernUi.RowsIn(height - 100));
        Assert.Equal(PokedexScreen.VisibleRows, ModernUi.RowsIn(height - 132 + 24));
        Assert.True(ModernUi.RowPitch > ModernUi.RowHeight);
    }

    // ------------------------------------------------------------------ the bag

    private static Inventory Bag(params (string Name, int Count)[] items)
    {
        var bag = new Inventory();
        foreach (var (name, count) in items) bag.AddItem(Item(name), count);
        return bag;
    }

    [Fact]
    public void TheBagHasPlatinumsEightPocketsAndEachRemembersItsCursor()
    {
        Assert.Equal(new[]
        {
            ItemPocket.Items, ItemPocket.Medicine, ItemPocket.PokeBalls, ItemPocket.TMsAndHMs,
            ItemPocket.Berries, ItemPocket.Mail, ItemPocket.BattleItems, ItemPocket.KeyItems
        }, BagScreen.Pockets);
        Assert.Equal(BagScreen.Pockets.Length, ModernUi.PocketTabs.Length);
        Assert.Equal(BagScreen.Pockets.Length, ModernUi.PocketTabs.Select(t => (t.Color.R, t.Color.G, t.Color.B)).Distinct().Count());
        Assert.Equal(BagScreen.Pockets.Length, ModernUi.PocketTabs.Select(t => t.Icon).Distinct().Count());
        // Every item of the game has a pocket the bag shows
        Assert.All(ItemDatabase.GetAll(), item => Assert.Contains(item.Pocket, BagScreen.Pockets));

        var bag = new BagScreen();
        bag.Open();
        Assert.Equal(ItemPocket.Items, bag.CurrentPocket);
        bag.MovePocket(1);
        Assert.Equal(ItemPocket.Medicine, bag.CurrentPocket);
        bag.MoveCursor(1, 5);
        bag.MoveCursor(1, 5);
        Assert.Equal(2, bag.SelectedIndex);

        // Away and back: the cursor is where it was; the other pockets start at their top
        bag.MovePocket(1);
        Assert.Equal((ItemPocket.PokeBalls, 0), (bag.CurrentPocket, bag.SelectedIndex));
        bag.MovePocket(-1);
        Assert.Equal((ItemPocket.Medicine, 2), (bag.CurrentPocket, bag.SelectedIndex));

        // Round the ends
        bag.MovePocket(-2);
        Assert.Equal(ItemPocket.KeyItems, bag.CurrentPocket);
        bag.MovePocket(1);
        Assert.Equal(ItemPocket.Items, bag.CurrentPocket);

        // Opening the bag again starts afresh
        bag.Open();
        bag.MovePocket(1);
        Assert.Equal(0, bag.SelectedIndex);
    }

    [Fact]
    public void TheBagsCursorWrapsAndItsListScrollsWithIt()
    {
        var bag = new BagScreen();
        bag.Open();
        bag.MoveCursor(-1, 12);
        Assert.Equal((11, 4), (bag.SelectedIndex, bag.FirstRow));
        bag.MoveCursor(1, 12);
        Assert.Equal((0, 0), (bag.SelectedIndex, bag.FirstRow));
        for (int i = 0; i < 7; i++) bag.MoveCursor(1, 12);
        Assert.Equal((7, 2), (bag.SelectedIndex, bag.FirstRow));

        // A pocket that has shrunk under the cursor (its last item was used up) pulls the cursor back in
        bag.Follow(3);
        Assert.Equal((2, 0), (bag.SelectedIndex, bag.FirstRow));
        // An empty pocket has nothing to move over
        bag.MoveCursor(1, 0);
        Assert.Equal(2, bag.SelectedIndex);
    }

    [Fact]
    public void AKeyHeldInTheBagRunsToTheEndOfThePocketAndStopsThere()
    {
        var bag = new BagScreen();
        bag.Open();

        // A held key's steps go down an item at a time, the list following, and stop at the last
        for (int i = 0; i < 5; i++) bag.MoveCursor(1, 12, held: true);
        Assert.Equal(5, bag.SelectedIndex);
        for (int i = 0; i < 20; i++) bag.MoveCursor(1, 12, held: true);
        Assert.Equal((11, 4), (bag.SelectedIndex, bag.FirstRow));
        // ...where a press goes round to the first
        bag.MoveCursor(1, 12);
        Assert.Equal((0, 0), (bag.SelectedIndex, bag.FirstRow));
        bag.MoveCursor(-1, 12, held: true);
        Assert.Equal(0, bag.SelectedIndex);

        // An item's actions are three rows: they go round whatever the key
        var inventory = new Inventory();
        inventory.AddItem(Item("Potion"), 1);
        bag.CurrentPocket = Item("Potion").Pocket;
        bag.Confirm(inventory, PartyOf(Mon("Turtwig", 5)), _ => { });
        Assert.NotNull(bag.Actions);
        bag.MoveCursor(-1, 1, held: true);
        Assert.Equal(bag.Actions!.Count - 1, bag.ActionIndex);
    }

    [Fact]
    public void WhatCanBeDoneWithAnItemFollowsWhatItIs()
    {
        Assert.Equal(new[] { BagAction.Use, BagAction.Give, BagAction.Cancel }, BagScreen.ActionsFor(Item("Potion")));
        Assert.Equal(new[] { BagAction.Use, BagAction.Give, BagAction.Cancel }, BagScreen.ActionsFor(Item("Fire Stone")));
        Assert.Equal(new[] { BagAction.Use, BagAction.Give, BagAction.Cancel }, BagScreen.ActionsFor(Item("Rare Candy")));
        // Something only held
        Assert.Equal(new[] { BagAction.Give, BagAction.Cancel }, BagScreen.ActionsFor(Item("Oran Berry")));
        Assert.Equal(new[] { BagAction.Give, BagAction.Cancel }, BagScreen.ActionsFor(Item("Poké Ball")));
        // As in Platinum, a Pokémon holds neither a Key Item nor a TM
        Assert.Equal(new[] { BagAction.Cancel }, BagScreen.ActionsFor(Item("Old Rod")));
        Assert.Equal(new[] { BagAction.Cancel }, BagScreen.ActionsFor(Item("TM01")));
        Assert.All(ItemDatabase.GetAll(), item =>
            Assert.Equal(item.Pocket is not (ItemPocket.KeyItems or ItemPocket.TMsAndHMs), BagScreen.CanGive(item)));
    }

    [Fact]
    public void TheAButtonOpensAnItemsActionsAndThenDoesTheOneChosen()
    {
        var hurt = Mon("Turtwig", 10);
        hurt.CurrentHP = 5;
        var party = PartyOf(hurt, Mon("Bidoof", 5));
        var inventory = Bag(("Potion", 2), ("Old Rod", 1));
        var notes = new List<string>();
        var bag = new BagScreen();
        bag.Open();

        // The Items pocket is empty: nothing happens
        bag.Confirm(inventory, party, notes.Add);
        Assert.Null(bag.Actions);

        bag.MovePocket(1);
        bag.Confirm(inventory, party, notes.Add);
        Assert.Equal(new[] { BagAction.Use, BagAction.Give, BagAction.Cancel }, bag.Actions);
        Assert.Equal(0, bag.ActionIndex);

        // While they are up the arrows move among them, and the pocket can't be changed
        bag.MoveCursor(-1, 1);
        Assert.Equal(2, bag.ActionIndex);
        bag.MovePocket(1);
        Assert.Equal(ItemPocket.Medicine, bag.CurrentPocket);

        // CANCEL puts them away; so does the B button, without leaving the bag
        bag.Confirm(inventory, party, notes.Add);
        Assert.Null(bag.Actions);
        bag.Confirm(inventory, party, notes.Add);
        bag.Cancel();
        Assert.Null(bag.Actions);
        Assert.True(bag.IsActive);

        // USE goes on to the party
        bag.Confirm(inventory, party, notes.Add);
        bag.Confirm(inventory, party, notes.Add);
        Assert.Null(bag.Actions);
        Assert.Equal("Potion", bag.ChoosingFor?.Name);
        Assert.False(bag.Giving);
        Assert.True(bag.WouldWorkOn(hurt, new EvolutionContext { Party = party, Bag = inventory }));
        Assert.False(bag.WouldWorkOn(party.Members[1], new EvolutionContext { Party = party, Bag = inventory }));

        // On the hurt one it works, and the party stays up while there is more of it
        bag.UseOnTarget(inventory, party, notes.Add, new EvolutionContext { Party = party, Bag = inventory });
        Assert.Equal(25, hurt.CurrentHP);
        Assert.Equal(1, inventory.GetQuantity(Item("Potion")));
        Assert.NotNull(bag.ChoosingFor);

        // On a healthy one it does nothing and nothing is spent
        bag.MoveTarget(1, 0, party.Count);
        bag.UseOnTarget(inventory, party, notes.Add, new EvolutionContext { Party = party, Bag = inventory });
        Assert.Equal("It won't have any effect.", notes.Last());
        Assert.Equal(1, inventory.GetQuantity(Item("Potion")));

        // The last one closes the choice
        hurt.CurrentHP = 1;
        bag.MoveTarget(-1, 0, party.Count);
        bag.UseOnTarget(inventory, party, notes.Add, new EvolutionContext { Party = party, Bag = inventory });
        Assert.Null(bag.ChoosingFor);
        Assert.Equal(0, inventory.GetQuantity(Item("Potion")));

        // A Key Item with nothing to do here says so
        bag.MovePocket(6);
        Assert.Equal(ItemPocket.KeyItems, bag.CurrentPocket);
        bag.Confirm(inventory, party, notes.Add);
        Assert.Null(bag.Actions);
        Assert.Equal("It can't be used here.", notes.Last());

        // The B button leaves the bag
        bag.Cancel();
        Assert.False(bag.IsActive);
    }

    [Fact]
    public void GivingFromTheBagHandsTheItemOverWhateverItIs()
    {
        var turtwig = Mon("Turtwig", 10);
        turtwig.CurrentHP = 5;
        var party = PartyOf(turtwig);
        var inventory = Bag(("Potion", 1), ("Oran Berry", 1));
        var bag = new BagScreen();
        bag.Open();
        bag.MovePocket(1);

        // GIVE, not USE: the Potion is held, not drunk
        bag.Confirm(inventory, party, _ => { });
        bag.MoveCursor(1, 1);
        Assert.Equal(BagAction.Give, bag.Actions![bag.ActionIndex]);
        bag.Confirm(inventory, party, _ => { });
        Assert.True(bag.Giving);
        Assert.Null(bag.WouldWorkOn(turtwig, new EvolutionContext { Party = party, Bag = inventory }));
        bag.UseOnTarget(inventory, party, _ => { }, new EvolutionContext { Party = party, Bag = inventory });
        Assert.Equal("Potion", turtwig.HeldItem?.Name);
        Assert.Equal(5, turtwig.CurrentHP);
        Assert.Equal(0, inventory.GetQuantity(Item("Potion")));

        // Giving something else brings the first back
        bag.BeginTargetChoice(Item("Oran Berry"));
        Assert.True(bag.Giving);
        bag.UseOnTarget(inventory, party, _ => { }, new EvolutionContext { Party = party, Bag = inventory });
        Assert.Equal("Oran Berry", turtwig.HeldItem?.Name);
        Assert.Equal(1, inventory.GetQuantity(Item("Potion")));

        // With nobody to give it to, the bag says so
        var notes = new List<string>();
        bag.Confirm(inventory, new Party(), notes.Add);
        bag.Confirm(inventory, new Party(), notes.Add);
        Assert.Null(bag.ChoosingFor);
        Assert.Single(notes);
    }

    [Fact]
    public void MedicineHelpsOnlyWhoNeedsIt()
    {
        var p = Mon("Turtwig", 20);
        int full = p.MaxHP;
        var potion = Item("Potion");
        Assert.False(FieldItems.WouldHelp(potion, p));
        Assert.Null(FieldItems.Use(potion, p));

        p.CurrentHP = full - 30;
        Assert.True(FieldItems.WouldHelp(potion, p));
        Assert.NotNull(FieldItems.Use(potion, p));
        Assert.Equal(full - 10, p.CurrentHP);
        // Never past full
        FieldItems.Use(potion, p);
        Assert.Equal(full, p.CurrentHP);

        // A status: the right cure, or one that cures anything; an Antidote also stops bad poison
        p.Status = StatusCondition.Burn;
        Assert.False(FieldItems.WouldHelp(Item("Antidote"), p));
        Assert.True(FieldItems.WouldHelp(Item("Burn Heal"), p));
        Assert.True(FieldItems.WouldHelp(Item("Full Heal"), p));
        p.Status = StatusCondition.Toxic;
        p.ToxicCounter = 3;
        Assert.True(FieldItems.WouldHelp(Item("Antidote"), p));
        FieldItems.Use(Item("Antidote"), p);
        Assert.Equal((StatusCondition.None, 0), (p.Status, p.ToxicCounter));
        Assert.False(FieldItems.WouldHelp(Item("Full Heal"), p));

        // Fainted: only a Revive helps, with half its HP; a Max Revive with all of it
        p.CurrentHP = 0;
        p.Status = StatusCondition.Faint;
        Assert.False(FieldItems.WouldHelp(potion, p));
        Assert.False(FieldItems.WouldHelp(Item("Full Heal"), p));
        Assert.False(FieldItems.WouldHelp(Item("Full Restore"), p));
        Assert.True(FieldItems.WouldHelp(Item("Revive"), p));
        FieldItems.Use(Item("Revive"), p);
        Assert.Equal((full / 2, false), (p.CurrentHP, p.IsFainted));
        Assert.False(FieldItems.WouldHelp(Item("Revive"), p));
        p.CurrentHP = 0;
        FieldItems.Use(Item("Max Revive"), p);
        Assert.Equal(full, p.CurrentHP);

        // A Full Restore does both
        p.CurrentHP = 1;
        p.Status = StatusCondition.Paralyze;
        FieldItems.Use(Item("Full Restore"), p);
        Assert.Equal((full, StatusCondition.None), (p.CurrentHP, p.Status));
        Assert.False(FieldItems.WouldHelp(Item("Full Restore"), p));

        // Only medicine is medicine
        Assert.True(FieldItems.IsMedicine(potion));
        Assert.False(FieldItems.IsMedicine(Item("Poké Ball")));
        Assert.False(FieldItems.IsMedicine(Item("Rare Candy")));
        Assert.Null(FieldItems.Use(Item("Poké Ball"), p));
    }

    [Fact]
    public void ItemsOfAKindShareAShapeAndTellThemselvesApartByColour()
    {
        static string Key(ItemData item)
        {
            var icon = PixelArtGenerator.ItemIcon(item);
            var text = new System.Text.StringBuilder();
            for (int y = 0; y < icon.Height; y++)
                for (int x = 0; x < icon.Width; x++)
                {
                    var t = icon.Get(x, y);
                    text.Append(t.A == 0 ? 0 : t.R * 65536 + t.G * 256 + t.B).Append(';');
                }
            return text.ToString();
        }
        static string Shape(ItemData item)
        {
            var icon = PixelArtGenerator.ItemIcon(item);
            var text = new System.Text.StringBuilder();
            for (int y = 0; y < icon.Height; y++)
                for (int x = 0; x < icon.Width; x++)
                    text.Append(icon.IsOpaque(x, y) ? '#' : '.');
            return text.ToString();
        }

        string pouch = Shape(Item("Escape Rope"));
        // A kind has its own shape, not the pouch
        foreach (string name in new[] { "TM01", "Oran Berry", "Old Rod", "X Attack", "Fire Stone", "Rare Candy", "Potion", "Revive" })
            Assert.NotEqual(pouch, Shape(Item(name)));
        Assert.All(ItemDatabase.GetAll().Where(i => i.Pocket == ItemPocket.Mail), mail => Assert.NotEqual(pouch, Shape(mail)));

        // A TM's disc is the colour of its move's type: two of one type look alike, two of different types don't
        var byType = ItemDatabase.GetAll().Where(i => i.Name.StartsWith("TM") && !string.IsNullOrEmpty(i.TeachesMove))
            .GroupBy(i => MoveDatabase.Get(i.TeachesMove!).Type).Where(g => g.Count() >= 2).Take(4).ToList();
        Assert.True(byType.Count >= 2);
        foreach (var group in byType) Assert.Equal(Key(group.First()), Key(group.Last()));
        Assert.NotEqual(Key(byType[0].First()), Key(byType[1].First()));
        // An HM has its rim
        var hm = Item("HM01");
        var sameTypeTm = ItemDatabase.GetAll().FirstOrDefault(i => i.Name.StartsWith("TM") && !string.IsNullOrEmpty(i.TeachesMove)
            && MoveDatabase.Get(i.TeachesMove!).Type == MoveDatabase.Get(hm.TeachesMove!).Type);
        if (sameTypeTm != null) Assert.NotEqual(Key(hm), Key(sameTypeTm));

        // The stones are gems of their own colours
        Assert.Equal(Shape(Item("Fire Stone")), Shape(Item("Water Stone")));
        Assert.NotEqual(Key(Item("Fire Stone")), Key(Item("Water Stone")));
    }

    // ------------------------------------------------------------------ the shop

    [Fact]
    public void AShopAsksHowManyAndSellsWhatTheMoneyBuys()
    {
        var shop = new ShopScreen();
        shop.Open("Sandgem Poké Mart");
        Assert.Equal("SANDGEM POKÉ MART", shop.Name);
        Assert.True(shop.Stock.Count >= 4);
        var notes = new List<string>();
        var inventory = new Inventory();
        var potion = shop.Stock.First(i => i.Name == "Potion");
        shop.SelectedIndex = shop.Stock.ToList().IndexOf(potion);
        int money = potion.Price * 3 + 10;

        Assert.Equal(3, ShopScreen.MostAffordable(potion, money));
        Assert.Equal(0, ShopScreen.MostAffordable(potion, potion.Price - 1));
        Assert.Equal(ShopScreen.MostAtOnce, ShopScreen.MostAffordable(potion, potion.Price * 500));

        // The first press asks; nothing is bought yet
        Assert.Equal(0, shop.Confirm(inventory, money, notes.Add));
        Assert.True(shop.ChoosingQuantity);
        Assert.Equal(1, shop.Quantity);
        Assert.Empty(notes);

        // Left and right by one, wrapping between one and the most the money buys
        shop.Move(1, 0, money);
        Assert.Equal(2, shop.Quantity);
        shop.Move(1, 0, money);
        shop.Move(1, 0, money);
        Assert.Equal(1, shop.Quantity);
        shop.Move(-1, 0, money);
        Assert.Equal(3, shop.Quantity);
        // Up and down belong to the number too: the list doesn't move meanwhile
        int before = shop.SelectedIndex;
        shop.Move(0, 1, money);
        Assert.Equal((before, 1), (shop.SelectedIndex, shop.Quantity));

        // The second press buys that many and says what it cost
        shop.Move(1, 0, money);
        int spent = shop.Confirm(inventory, money, notes.Add);
        Assert.Equal(potion.Price * 2, spent);
        Assert.Equal(2, inventory.GetQuantity(potion));
        Assert.False(shop.ChoosingQuantity);
        Assert.Single(notes);

        // Up and down by ten, stopping at the ends before wrapping
        int rich = potion.Price * 25;
        shop.Confirm(inventory, rich, notes.Add);
        shop.Move(0, -1, rich);
        Assert.Equal(11, shop.Quantity);
        shop.Move(0, -1, rich);
        shop.Move(0, -1, rich);
        Assert.Equal(25, shop.Quantity);
        shop.Move(0, -1, rich);
        Assert.Equal(1, shop.Quantity);
        shop.Move(0, 1, rich);
        Assert.Equal(25, shop.Quantity);
        shop.Move(0, 1, rich);
        Assert.Equal(15, shop.Quantity);

        // The B button takes the question away, then leaves the shop
        shop.Cancel();
        Assert.False(shop.ChoosingQuantity);
        Assert.True(shop.IsActive);

        // Too poor for even one: it says so and doesn't ask
        Assert.Equal(0, shop.Confirm(inventory, potion.Price - 1, notes.Add));
        Assert.False(shop.ChoosingQuantity);
        Assert.Equal("You don't have enough money.", notes.Last());

        shop.Cancel();
        Assert.False(shop.IsActive);
    }

    [Fact]
    public void AShopsCursorWrapsRoundItsStock()
    {
        var shop = new ShopScreen();
        shop.Open();
        Assert.Equal("POKÉ MART", shop.Name);
        shop.Move(0, -1, 0);
        Assert.Equal(shop.Stock.Count - 1, shop.SelectedIndex);
        shop.Move(0, 1, 0);
        Assert.Equal(0, shop.SelectedIndex);
        // Sideways does nothing in the list
        shop.Move(1, 0, 0);
        Assert.Equal(0, shop.SelectedIndex);
        Assert.Equal(shop.Stock[0], shop.Selected);

        // A key held down runs to the end of the stock and stops there, where a press goes round
        int last = shop.Stock.Count - 1;
        shop.Move(0, -1, 0, held: true);
        Assert.Equal(0, shop.SelectedIndex);
        shop.Move(0, 1, 0, held: true);
        Assert.Equal(1, shop.SelectedIndex);
        for (int i = 0; i < shop.Stock.Count + 3; i++) shop.Move(0, 1, 0, held: true);
        Assert.Equal(last, shop.SelectedIndex);
        shop.Move(0, 1, 0);
        Assert.Equal(0, shop.SelectedIndex);
    }

    // ------------------------------------------------------------------ the Pokédex

    [Fact]
    public void ThePokedexOpensOnTheFirstSpeciesSeenAndJumpsByTen()
    {
        var pokedex = new Pokedex();
        var dex = new PokedexScreen();
        dex.Open(pokedex);
        Assert.Equal(0, dex.SelectedIndex);
        // Platinum's own Sinnoh Pokédex, in its order
        Assert.Equal(PokedexMode.Sinnoh, dex.Mode);
        Assert.Equal(Enumerable.Range(1, 210), dex.Rows.Select(r => r.Number));
        Assert.Equal("Turtwig", dex.Species[0].Name);

        pokedex.RegisterSeen(396);   // Starly, No. 10 in Sinnoh
        pokedex.RegisterCaught(390); // Chimchar, No. 4
        dex.Open(pokedex);
        Assert.Equal("Chimchar", dex.Species[dex.SelectedIndex].Name);
        Assert.InRange(dex.SelectedIndex, dex.FirstRow, dex.FirstRow + PokedexScreen.VisibleRows - 1);

        dex.Move(1);
        Assert.Equal(5, dex.Rows[dex.SelectedIndex].Number);
        dex.Move(PokedexScreen.Jump);
        Assert.Equal(15, dex.Rows[dex.SelectedIndex].Number);
        dex.Move(-PokedexScreen.Jump);
        dex.Move(-1);
        Assert.Equal("Chimchar", dex.Species[dex.SelectedIndex].Name);
        Assert.InRange(dex.SelectedIndex, dex.FirstRow, dex.FirstRow + PokedexScreen.VisibleRows - 1);

        // A step wraps round the ends; a jump stops at them
        dex.SelectedIndex = 0;
        dex.Move(-PokedexScreen.Jump);
        Assert.Equal(0, dex.SelectedIndex);
        dex.Move(-1);
        Assert.Equal(dex.Rows.Count - 1, dex.SelectedIndex);
        Assert.Equal(dex.Rows.Count - PokedexScreen.VisibleRows, dex.FirstRow);
        dex.Move(PokedexScreen.Jump);
        Assert.Equal(dex.Rows.Count - 1, dex.SelectedIndex);
        dex.Move(1);
        Assert.Equal((0, 0), (dex.SelectedIndex, dex.FirstRow));
    }

    [Fact]
    public void APokedexEntryHasThreePagesAndStepsThroughTheSpeciesSeen()
    {
        var pokedex = new Pokedex();
        foreach (int n in new[] { 387, 396, 399 }) pokedex.RegisterSeen(n);   // Turtwig, Starly, Bidoof
        var dex = new PokedexScreen();
        dex.Open(pokedex);
        Assert.Equal("Turtwig", dex.Selected!.Value.Species.Name);

        // An unseen species has no entry to open
        dex.Move(1);
        dex.Confirm();
        Assert.Equal(PokedexFocus.List, dex.Focus);
        dex.Move(-1);

        dex.Confirm();
        Assert.Equal((PokedexFocus.Entry, PokedexPage.Info), (dex.Focus, dex.Page));
        dex.Sideways(1);
        Assert.Equal(PokedexPage.Area, dex.Page);
        dex.Sideways(1);
        Assert.Equal(PokedexPage.Size, dex.Page);
        dex.Sideways(1);
        Assert.Equal(PokedexPage.Info, dex.Page);
        dex.Sideways(-1);
        Assert.Equal(PokedexPage.Size, dex.Page);

        // Up and down skip the species not seen, and wrap
        dex.Move(1);
        Assert.Equal("Starly", dex.Selected!.Value.Species.Name);
        dex.Move(1);
        Assert.Equal("Bidoof", dex.Selected!.Value.Species.Name);
        dex.Move(1);
        Assert.Equal("Turtwig", dex.Selected!.Value.Species.Name);
        Assert.Equal(PokedexPage.Size, dex.Page);

        // B goes back to the list, which shows the first page again
        dex.Cancel();
        Assert.Equal((PokedexFocus.List, PokedexPage.Info), (dex.Focus, dex.Page));
        dex.Cancel();
        Assert.False(dex.IsActive);
    }

    [Fact]
    public void AKeyHeldInThePokedexRunsToTheEndAndStopsThere()
    {
        var pokedex = new Pokedex();
        foreach (int n in new[] { 387, 396, 399 }) pokedex.RegisterSeen(n);   // Turtwig, Starly, Bidoof
        var dex = new PokedexScreen();
        dex.Open(pokedex);
        int last = dex.Rows.Count - 1;

        // A held key's steps go on a species at a time and stop at the last, where a press wraps round
        for (int i = 0; i < 3; i++) dex.Move(1, held: true);
        Assert.Equal(3, dex.SelectedIndex);
        for (int i = 0; i < dex.Rows.Count; i++) dex.Move(1, held: true);
        Assert.Equal(last, dex.SelectedIndex);
        Assert.InRange(dex.SelectedIndex, dex.FirstRow, dex.FirstRow + PokedexScreen.VisibleRows - 1);
        dex.Move(1);
        Assert.Equal(0, dex.SelectedIndex);
        dex.Move(-1, held: true);
        Assert.Equal(0, dex.SelectedIndex);

        // Left and right held go ten at a time, and stop at the ends as they always did
        dex.Sideways(1, held: true);
        dex.Sideways(1, held: true);
        Assert.Equal(2 * PokedexScreen.Jump, dex.SelectedIndex);
        for (int i = 0; i < dex.Rows.Count; i++) dex.Sideways(-1, held: true);
        Assert.Equal(0, dex.SelectedIndex);

        // In an entry a held key stops at the last species seen, where a press goes round to the first
        dex.Confirm();
        Assert.Equal(PokedexFocus.Entry, dex.Focus);
        dex.Move(1, held: true);
        Assert.Equal("Starly", dex.Selected!.Value.Species.Name);
        dex.Move(1, held: true);
        dex.Move(1, held: true);
        Assert.Equal("Bidoof", dex.Selected!.Value.Species.Name);
        dex.Move(1);
        Assert.Equal("Turtwig", dex.Selected!.Value.Species.Name);
        dex.Move(-1, held: true);
        Assert.Equal("Turtwig", dex.Selected!.Value.Species.Name);
    }

    [Fact]
    public void ThePokedexSearchFindsAndSortsAmongTheSpeciesSeen()
    {
        var pokedex = new Pokedex();
        foreach (string name in new[] { "Turtwig", "Starly", "Staraptor", "Bidoof", "Shinx", "Kricketot" }) pokedex.RegisterCaught(PokemonDatabase.Get(name)!.DexNumber);
        var dex = new PokedexScreen();
        dex.Open(pokedex);

        dex.OpenSearch();
        Assert.Equal(PokedexFocus.Search, dex.Focus);
        // No choice of Pokédex until the National one is open
        Assert.Equal(PokedexSearchRow.Order, dex.SearchRows[0]);
        Assert.DoesNotContain(PokedexSearchButton.Diploma, dex.Buttons);

        dex.Sideways(1);
        Assert.Equal("A to Z", dex.ValueOf(PokedexSearchRow.Order));
        dex.Move(1);   // Name
        dex.Move(1);   // Type
        Assert.Equal(PokedexSearchRow.Type1, dex.SearchRows[dex.SearchIndex]);
        for (int i = 0; i <= Array.IndexOf(PokedexScreen.SearchTypes, PokemonType.Flying); i++) dex.Sideways(1);
        Assert.Equal("Flying", dex.ValueOf(PokedexSearchRow.Type1));
        dex.Sideways(-1);
        dex.Sideways(1);
        Assert.Equal(PokemonType.Flying, dex.Draft.Type1);

        // The buttons: SEARCH runs it
        while (dex.SearchRows[dex.SearchIndex] != PokedexSearchRow.Buttons) dex.Move(1);
        dex.Confirm();
        Assert.Equal(PokedexFocus.List, dex.Focus);
        Assert.NotNull(dex.Results);
        Assert.Equal(new[] { "Staraptor", "Starly" }, dex.Species.Select(s => s.Name));

        // B goes back to the whole Pokédex, on the species chosen there
        dex.Cancel();
        Assert.Null(dex.Results);
        Assert.Equal(210, dex.Rows.Count);
        Assert.Equal("Staraptor", dex.Selected!.Value.Species.Name);
        Assert.True(dex.IsActive);

        // RESET clears the search; a plain search is the whole list
        dex.OpenSearch();
        while (dex.SearchRows[dex.SearchIndex] != PokedexSearchRow.Buttons) dex.Move(1);
        dex.Sideways(1);
        dex.Confirm();   // RESET
        Assert.True(dex.Draft.IsPlain);
        dex.Search();
        Assert.Null(dex.Results);
        Assert.Equal(210, dex.Rows.Count);
    }

    [Fact]
    public void TheNationalPokedexIsChosenInTheSearchOnceItIsOpen()
    {
        var pokedex = new Pokedex();
        pokedex.RegisterCaught(1);
        pokedex.UnlockNational();
        var dex = new PokedexScreen();
        dex.Open(pokedex);
        dex.OpenSearch();
        Assert.Equal(PokedexSearchRow.Mode, dex.SearchRows[0]);
        Assert.Equal("Sinnoh", dex.ValueOf(PokedexSearchRow.Mode));
        dex.Sideways(1);
        Assert.Equal("National", dex.ValueOf(PokedexSearchRow.Mode));
        dex.Search();
        Assert.Equal((PokedexMode.National, 1025), (dex.Mode, dex.Rows.Count));
        Assert.Equal("Bulbasaur", dex.Species[0].Name);

        // The screen remembers the Pokédex it showed, but not one the player can no longer open
        dex.Open(pokedex);
        Assert.Equal(PokedexMode.National, dex.Mode);
        dex.Open(new Pokedex());
        Assert.Equal(PokedexMode.Sinnoh, dex.Mode);
    }

    [Fact]
    public void ADiplomaIsShownOnceThePokedexIsCompleteAndAgainFromTheSearch()
    {
        var pokedex = new Pokedex();
        foreach (var e in Pokedex.Entries(PokedexMode.Sinnoh)) pokedex.RegisterSeen(e.Species.DexNumber);
        var dex = new PokedexScreen();
        dex.Open(pokedex);
        Assert.Equal((PokedexFocus.Diploma, PokedexMode.Sinnoh), (dex.Focus, dex.ShownDiploma));
        dex.Confirm();
        Assert.Equal(PokedexFocus.List, dex.Focus);

        // Given once
        dex.Open(pokedex);
        Assert.Equal(PokedexFocus.List, dex.Focus);

        // Shown again from the search panel
        dex.OpenSearch();
        Assert.Contains(PokedexSearchButton.Diploma, dex.Buttons);
        while (dex.SearchRows[dex.SearchIndex] != PokedexSearchRow.Buttons) dex.Move(1);
        dex.Sideways(-1);   // from SEARCH round to DIPLOMA
        dex.Confirm();
        Assert.Equal(PokedexFocus.Diploma, dex.Focus);
        dex.Cancel();
        Assert.Equal(PokedexFocus.Search, dex.Focus);
    }

    // ------------------------------------------------------------------ the Trainer Card and saving

    [Theory]
    [InlineData(0, "00000")]
    [InlineData(7, "00007")]
    [InlineData(24391, "24391")]
    [InlineData(65535, "65535")]
    public void ATrainersNumberIsPrintedInFiveDigits(int id, string printed) => Assert.Equal(printed, TrainerCardScreen.FormatId(id));

    [Fact]
    public void ASaveKeepsWhoThePlayerIs()
    {
        var save = new SaveData { PlayerName = "Maya", Look = PlayerLook.Girl, TrainerId = 41350, Started = new DateTime(2026, 10, 4) };
        var read = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        Assert.Equal(("Maya", PlayerLook.Girl, 41350, new DateTime(2026, 10, 4)), (read.PlayerName, read.Look, read.TrainerId, read.Started!.Value));

        // A save from before any of it was recorded is the boy, with no number or day yet
        var old = JsonSerializer.Deserialize<SaveData>("{\"PlayerName\":\"Lucas\"}")!;
        Assert.Equal((PlayerLook.Boy, 0, (DateTime?)null), (old.Look, old.TrainerId, old.Started));
    }

    [Fact]
    public void SavingAsksFirstAndSaysSoAfterwards()
    {
        var screen = new SaveScreen();
        var summary = new SaveData { PlayerName = "Lucas" };
        screen.Open(summary);
        Assert.True(screen.IsActive);
        Assert.Same(summary, screen.Summary);
        Assert.Equal("SAVE", SaveScreen.Answers[screen.AnswerIndex]);
        Assert.False(screen.TakeRequest());

        // CANCEL leaves without saving
        screen.Move(1);
        screen.Move(1);
        Assert.Equal("CANCEL", SaveScreen.Answers[screen.AnswerIndex]);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.False(screen.TakeRequest());

        // SAVE asks the game to save, once
        screen.Open(summary);
        screen.Confirm();
        Assert.True(screen.IsActive);
        Assert.True(screen.TakeRequest());
        Assert.False(screen.TakeRequest());
        Assert.False(screen.Saved);

        // Once the game has saved, the panel says so for a moment and goes by itself
        screen.MarkSaved();
        Assert.True(screen.Saved);
        screen.Move(1);
        Assert.Equal(0, screen.AnswerIndex);
        for (float t = 0; t < SaveScreen.SavedTime - 0.1f; t += Frame) screen.Advance(Frame);
        Assert.True(screen.IsActive);
        for (float t = 0; t < 0.2f; t += Frame) screen.Advance(Frame);
        Assert.False(screen.IsActive);
        // It slides away before it is gone
        Assert.True(screen.Visible);
        for (float t = 0; t < 0.3f; t += Frame) screen.Advance(Frame);
        Assert.False(screen.Visible);

        // Or any button sends it away at once, without saving twice
        screen.Open(summary);
        screen.Confirm();
        screen.TakeRequest();
        screen.MarkSaved();
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.False(screen.TakeRequest());

        // The B button backs out before saving
        screen.Open(summary);
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.False(screen.TakeRequest());
    }

    // ------------------------------------------------------------------ PC boxes

    [Fact]
    public void TheStorageCursorGoesFromThePartyIntoTheBoxAndUpToItsName()
    {
        var pc = new PCScreen();
        pc.Open();
        Assert.Equal((StorageZone.Party, 0), (pc.Zone, pc.PartyIndex));

        // Up and down the party, wrapping
        pc.Move(0, -1, 3);
        Assert.Equal(2, pc.PartyIndex);
        pc.Move(0, 1, 3);
        pc.Move(0, 1, 3);
        Assert.Equal(1, pc.PartyIndex);

        // Right: into the box at the row beside that card, in its first column
        pc.Move(1, 0, 3);
        Assert.Equal((StorageZone.Box, PCScreen.Columns), (pc.Zone, pc.Cell));
        pc.Move(1, 0, 3);
        pc.Move(0, 1, 3);
        Assert.Equal(2 * PCScreen.Columns + 1, pc.Cell);

        // Back off its left edge to the party, to the card beside that row (or the last there is)
        pc.Move(-1, 0, 3);
        pc.Move(-1, 0, 3);
        Assert.Equal((StorageZone.Party, 2), (pc.Zone, pc.PartyIndex));
        // Left from the party comes in at the box's far side
        pc.Move(-1, 0, 3);
        Assert.Equal((StorageZone.Box, 2 * PCScreen.Columns + PCScreen.Columns - 1), (pc.Zone, pc.Cell));
        // And off its right edge is the party again
        pc.Move(1, 0, 3);
        Assert.Equal(StorageZone.Party, pc.Zone);

        // Up from the top row to the box's name, where left and right change box, round and round
        pc.Move(1, 0, 3);
        pc.Move(0, -1, 3);
        pc.Move(0, -1, 3);
        Assert.Equal(0, pc.Cell);
        pc.Move(0, -1, 3);
        Assert.Equal((StorageZone.BoxName, 0), (pc.Zone, pc.Box));
        pc.Move(-1, 0, 3);
        Assert.Equal(PCScreen.BoxCount - 1, pc.Box);
        pc.Move(1, 0, 3);
        pc.Move(1, 0, 3);
        Assert.Equal(1, pc.Box);
        // Down again into the top row; up from the name into the last row
        pc.Move(0, 1, 3);
        Assert.Equal((StorageZone.Box, 0), (pc.Zone, pc.Cell));
        pc.Move(0, -1, 3);
        pc.Move(0, -1, 3);
        Assert.Equal((StorageZone.Box, (PCScreen.Rows - 1) * PCScreen.Columns), (pc.Zone, pc.Cell));
        // Down from the last row is the name too
        pc.Move(0, 1, 3);
        Assert.Equal(StorageZone.BoxName, pc.Zone);

        Assert.Equal(30, PCScreen.BoxSize);
        Assert.Equal(18, PCScreen.BoxCount);
    }

    [Fact]
    public void PokemonArePutInStorageAndTakenOut()
    {
        var turtwig = Mon("Turtwig", 5);
        var starly = Mon("Starly", 4);
        var party = PartyOf(turtwig, starly);
        var stored = new List<Pokemon>();
        var notes = new List<string>();
        var pc = new PCScreen();
        pc.Open();
        Assert.Same(turtwig, pc.Under(party, stored));

        // A party Pokémon goes into the first box
        pc.Confirm(party, stored, notes.Add);
        Assert.Equal(new[] { starly }, party.Members);
        Assert.Equal(new[] { turtwig }, stored);
        Assert.Contains("Box 1", notes.Last());
        Assert.Same(starly, pc.Under(party, stored));

        // Never the last one
        pc.Confirm(party, stored, notes.Add);
        Assert.Single(party.Members);
        Assert.Equal("That's your last Pokémon!", notes.Last());

        // In the box: the first slot holds it, the next is empty
        pc.Move(1, 0, party.Count);
        Assert.Same(turtwig, pc.Under(party, stored));
        pc.Move(1, 0, party.Count);
        Assert.Null(pc.Under(party, stored));
        pc.Confirm(party, stored, notes.Add);
        Assert.Single(stored);

        // Taken out again, it joins the party
        pc.Move(-1, 0, party.Count);
        pc.Confirm(party, stored, notes.Add);
        Assert.Equal(new[] { starly, turtwig }, party.Members);
        Assert.Empty(stored);

        // A full party takes nobody
        var full = PartyOf(Enumerable.Range(0, Party.MaxSize).Select(_ => Mon("Bidoof", 3)).ToArray());
        stored.Add(Mon("Shinx", 3));
        pc.Confirm(full, stored, notes.Add);
        Assert.Single(stored);
        Assert.Equal("Your party is full.", notes.Last());

        // The thirty-first Pokémon lands in the second box, and the screen turns to it
        stored.Clear();
        for (int i = 0; i < PCScreen.BoxSize; i++) stored.Add(Mon("Bidoof", 2));
        pc.Open();
        pc.Confirm(full, stored, notes.Add);
        Assert.Equal(1, pc.Box);
        Assert.Contains("Box 2", notes.Last());
        pc.Zone = StorageZone.Box;
        pc.Cell = 0;
        Assert.Equal(PCScreen.BoxSize, pc.StoredIndex);
        Assert.Same(stored[^1], pc.Under(full, stored));

        // The name of a box is nothing to press
        pc.Zone = StorageZone.BoxName;
        int count = stored.Count;
        pc.Confirm(full, stored, notes.Add);
        Assert.Equal(count, stored.Count);
        Assert.Null(pc.Under(full, stored));
    }

    // ------------------------------------------------------------------ the choice of a partner

    [Fact]
    public void APartnerIsChosenOnlyAfterTheQuestionIsAnsweredYes()
    {
        Assert.Equal(new[] { "Turtwig", "Chimchar", "Piplup" }, StarterSelectScreen.Starters);
        var screen = new StarterSelectScreen();
        screen.Open();
        screen.Move(-1);
        Assert.Equal(2, screen.SelectedIndex);
        screen.Move(1);
        screen.Move(1);
        Assert.Equal(1, screen.SelectedIndex);

        // The B button does nothing until a question is up: the briefcase can't be left without choosing
        screen.Cancel();
        Assert.True(screen.IsActive);

        // The first press asks, with going back as the answer under the cursor
        Assert.Null(screen.Confirm());
        Assert.True(screen.ConfirmingSelection);
        Assert.False(screen.AnswerYes);
        // No: back to looking, on the same one
        Assert.Null(screen.Confirm());
        Assert.False(screen.ConfirmingSelection);
        Assert.Equal(1, screen.SelectedIndex);
        Assert.True(screen.IsActive);

        // While the question is up the arrows move between its answers, not between the three
        screen.Confirm();
        screen.Move(1);
        Assert.True(screen.AnswerYes);
        Assert.Equal(1, screen.SelectedIndex);
        screen.Move(-1);
        Assert.False(screen.AnswerYes);
        screen.Cancel();
        Assert.False(screen.ConfirmingSelection);

        // Yes: the Pokémon is given, at the level the games give it
        screen.Confirm();
        screen.Move(1);
        var chosen = screen.Confirm();
        Assert.NotNull(chosen);
        Assert.Equal(("Chimchar", StarterSelectScreen.Level), (chosen!.Species.Name, chosen.Level));
        Assert.Equal(5, chosen.Level);
        Assert.False(screen.IsActive);
    }

    // ------------------------------------------------------------------ options and written lines

    [Fact]
    public void TextSpeedIsTheFirstOptionAndLinesAreWrittenAtItWhateverTheFrameRate()
    {
        Assert.Equal(OptionRow.TextSpeed, Enum.GetValues<OptionRow>()[0]);
        var settings = new GameSettings();
        Assert.Equal(TextSpeed.Normal, settings.TextSpeed);
        OptionsScreen.Change(settings, OptionRow.TextSpeed, 1);
        Assert.Equal(TextSpeed.Fast, settings.TextSpeed);
        OptionsScreen.Change(settings, OptionRow.TextSpeed, 1);
        Assert.Equal(TextSpeed.Slow, settings.TextSpeed);
        OptionsScreen.Change(settings, OptionRow.TextSpeed, -1);
        Assert.Equal(TextSpeed.Fast, settings.TextSpeed);

        Assert.Equal((24f, 45f, 120f), (GameSettings.CharactersPerSecond(TextSpeed.Slow), GameSettings.CharactersPerSecond(TextSpeed.Normal),
            GameSettings.CharactersPerSecond(TextSpeed.Fast)));
        var read = JsonSerializer.Deserialize<GameSettings>(JsonSerializer.Serialize(settings))!;
        Assert.Equal(TextSpeed.Fast, read.TextSpeed);

        // A second of typing writes as much at thirty frames a second as at a hundred and forty-four
        string line = new('x', 200);
        int Written(float speed, int fps)
        {
            var talk = new DialogueManager { CharactersPerSecond = speed };
            talk.ShowDialogue("", line);
            for (int i = 0; i < fps; i++) talk.Type(1f / fps);
            return talk.VisibleText.Length;
        }
        foreach (float speed in new[] { 24f, 45f, 120f })
        {
            Assert.InRange(Written(speed, 30), speed - 1, speed + 1);
            Assert.InRange(Written(speed, 144), speed - 1, speed + 1);
        }

        // The A button finishes a line at once, and goes on only once it is all there
        var said = new DialogueManager();
        bool done = false;
        said.ShowDialogue("Barry", new[] { "Barry: One.", "Two." }, () => done = true);
        Assert.Equal(("Barry", "One."), (said.Speaker, said.CurrentLine));
        said.Advance();
        Assert.Equal("One.", said.CurrentLine);
        said.FinishLine();
        Assert.Equal("One.", said.VisibleText);
        said.Advance();
        Assert.Equal("Two.", said.CurrentLine);
        said.FinishLine();
        said.Advance();
        Assert.True(done);
        Assert.False(said.IsActive);
    }

    // ------------------------------------------------------------------ who the player is

    [Fact]
    public void WrittenLinesNameThePlayerAndTheAssistantWhoeverTheyAre()
    {
        Assert.Equal("Hi, Maya! Lucas is waiting.", PlayerIdentity.Fill("Hi, {player}! {assistant} is waiting.", "Maya", PlayerLook.Girl));
        Assert.Equal("Hi, Lucas! Dawn is waiting.", PlayerIdentity.Fill("Hi, {player}! {assistant} is waiting.", "Lucas", PlayerLook.Boy));
        Assert.Equal("Nothing to fill.", PlayerIdentity.Fill("Nothing to fill.", "Maya", PlayerLook.Girl));

        // The one the player isn't is the assistant; the player's own type follows their look
        Assert.Equal("DAWN", PlayerIdentity.CharacterFor("Assistant", PlayerLook.Boy));
        Assert.Equal("PLAYER", PlayerIdentity.CharacterFor("Assistant", PlayerLook.Girl));
        Assert.Equal("PLAYER", PlayerIdentity.CharacterFor("Player", PlayerLook.Boy));
        Assert.Equal("DAWN", PlayerIdentity.CharacterFor("Player", PlayerLook.Girl));
        Assert.Equal("Lass", PlayerIdentity.CharacterFor("Lass", PlayerLook.Girl));
        Assert.Equal(("Lucas", "Dawn"), (PlayerIdentity.DefaultName(PlayerLook.Boy), PlayerIdentity.DefaultName(PlayerLook.Girl)));
        Assert.Equal(PlayerLook.Boy, PlayerIdentity.Other(PlayerLook.Girl));

        // A name is kept at seven characters at most, trimmed, and can't smuggle a placeholder in
        Assert.Equal(7, PlayerIdentity.MaxNameLength);
        Assert.Equal("Maya", PlayerIdentity.Clean("  Maya "));
        Assert.Equal("Bartho", PlayerIdentity.Clean("Bartho lomew"));
        Assert.Equal("Barthol", PlayerIdentity.Clean("Bartholomew"));
        Assert.Equal("player", PlayerIdentity.Clean("{player}"));
        Assert.Equal("", PlayerIdentity.Clean("   "));
        Assert.Equal("", PlayerIdentity.Clean(null));

        // Both characters have a look of their own
        var boy = CharacterStyle.For(PlayerIdentity.CharacterOf(PlayerLook.Boy));
        var girl = CharacterStyle.For(PlayerIdentity.CharacterOf(PlayerLook.Girl));
        Assert.NotEqual((boy.Top.R, boy.Top.G, boy.Top.B), (girl.Top.R, girl.Top.G, girl.Top.B));
        Assert.NotEqual(boy.Hair, girl.Hair);
        Assert.True(girl.Skirt && !boy.Skirt);
    }

    [Fact]
    public void NoWrittenLineNamesThePlayerOutright()
    {
        // Every line of every map goes through the names chosen in the introduction
        var known = new[] { "{player}", "{assistant}" };
        void Check(string where, string text)
        {
            Assert.DoesNotContain("Lucas", text);
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, "\\{[^}]*\\}"))
                Assert.True(known.Contains(m.Value), $"{where}: unknown placeholder {m.Value}");
        }

        foreach (string name in MapDatabase.MapNames)
        {
            var map = MapDatabase.Get(name);
            Check(name, map.DisplayName);
            foreach (var npc in map.NPCs)
            {
                Check(name, npc.Name);
                foreach (string line in npc.DialogLines) Check($"{name}, {npc.Name}", line);
            }
        }

        // The assistant stands in Sandgem Town, and is drawn as whichever character the player isn't
        var sinnoh = MapDatabase.Get("Sinnoh");
        var assistant = Assert.Single(sinnoh.NPCs, n => n.NpcType == "Assistant" && sinnoh.AreaAt(n.GridX, n.GridY)?.Key == "sandgem_town");
        Assert.Equal("{assistant}", assistant.Name);
        Assert.Contains(assistant.DialogLines, line => line.Contains("{player}"));

        // A place named after the player shows their name
        var home = MapDatabase.Get("PlayerHouse");
        Assert.Contains("{player}", home.DisplayName);
        Assert.Equal(PlayerIdentity.Fill(home.DisplayName), home.DisplayNameAt(1, 1));
        Assert.DoesNotContain("{", home.DisplayNameAt(1, 1));
    }

    // ------------------------------------------------------------------ the name keyboard

    [Fact]
    public void ANameIsEnteredKeyByKey()
    {
        var entry = new NameEntry();
        Assert.Equal(("", true, 0, 0), (entry.Text, entry.UpperCase, entry.Row, entry.Column));
        Assert.Equal("ABCDEFGHIJ", entry.RowKeys(0));

        // M is the third key of the second row; after the first capital the keys go to lower case by themselves
        entry.Move(0, 1);
        entry.Move(1, 0);
        entry.Move(1, 0);
        Assert.Equal(NameKey.Letter, entry.KeyUnderCursor);
        Assert.Equal(NameKey.Letter, entry.Press());
        Assert.Equal(("M", false), (entry.Text, entry.UpperCase));
        Assert.Equal("klmnopqrst", entry.RowKeys(1));

        // a: up a row and two to the left
        entry.Move(0, -1);
        entry.Move(-1, 0);
        entry.Move(-1, 0);
        entry.Press();
        Assert.Equal("Ma", entry.Text);

        // The cursor wraps round rows and columns
        entry.Move(-1, 0);
        Assert.Equal(NameEntry.Columns - 1, entry.Column);
        entry.Press();
        Assert.Equal("Maj", entry.Text);
        entry.Move(0, -1);
        Assert.Equal(NameEntry.LetterRows, entry.Row);
        Assert.Equal(NameKey.Done, entry.KeyUnderCursor);

        // The bottom row has three wide keys: a step goes from one to the next, wherever the cursor came down
        entry.Move(1, 0);
        Assert.Equal(NameKey.Case, entry.KeyUnderCursor);
        entry.Move(1, 0);
        Assert.Equal(NameKey.Delete, entry.KeyUnderCursor);
        Assert.Equal(NameKey.Delete, entry.Press());
        Assert.Equal("Ma", entry.Text);
        entry.Move(-1, 0);
        Assert.Equal(NameKey.Case, entry.Press());
        Assert.True(entry.UpperCase);
        entry.Press();
        Assert.False(entry.UpperCase);

        // The B button deletes too; with nothing left the keys are capitals again
        Assert.True(entry.Backspace());
        Assert.True(entry.Backspace());
        Assert.False(entry.Backspace());
        Assert.Equal(("", true), (entry.Text, entry.UpperCase));

        // A name doesn't begin with a space, takes seven characters, and then the cursor waits on OK
        Assert.False(entry.Type(' '));
        foreach (char c in "Bartholomew") entry.Type(c);
        Assert.Equal("Barthol", entry.Text);
        Assert.Equal(NameKey.Done, entry.KeyUnderCursor);
        Assert.False(entry.Type('x'));

        // OK ends it; nothing more can be changed until it is opened again
        Assert.False(entry.Done);
        Assert.Equal(NameKey.Done, entry.Press());
        Assert.True(entry.Done);
        Assert.False(entry.Backspace());
        entry.Move(1, 0);
        Assert.Equal(NameKey.Done, entry.KeyUnderCursor);
        Assert.Equal("Barthol", entry.Result("Lucas"));
        entry.Reopen();
        Assert.True(entry.Backspace());
        Assert.Equal("Bartho", entry.Text);

        // The Start button jumps to OK, and an empty name takes the character's own
        var empty = new NameEntry();
        empty.ToDone();
        Assert.Equal(NameKey.Done, empty.Press());
        Assert.Equal("Dawn", empty.Result("Dawn"));

        // Every key of the four rows is one character, and both cases have the same signs and digits
        var upper = new NameEntry();
        var lower = new NameEntry("a");
        Assert.False(lower.UpperCase);
        for (int row = 0; row < NameEntry.LetterRows; row++)
        {
            Assert.Equal(NameEntry.Columns, upper.RowKeys(row).Length);
            Assert.Equal(upper.RowKeys(row).ToLowerInvariant(), lower.RowKeys(row));
        }
        Assert.Equal(26, Enumerable.Range(0, 3).SelectMany(r => upper.RowKeys(r)).Count(char.IsLetter));
        Assert.Equal("0123456789", upper.RowKeys(3));
        Assert.Equal(NameEntry.WideKeys.Length, NameEntry.WideStarts.Length);
    }

    // ------------------------------------------------------------------ the new-game introduction

    /// <summary>Runs the introduction's clock for a while.</summary>
    private static void Run(IntroScreen intro, float seconds)
    {
        for (float t = 0; t < seconds; t += Frame) intro.Advance(Frame);
    }

    /// <summary>Presses the A button through whatever is being said until the introduction is somewhere else.</summary>
    private static void Through(IntroScreen intro, IntroPhase until)
    {
        for (int guard = 0; guard < 4000 && intro.Phase != until; guard++)
        {
            if (intro.Talking) intro.PressConfirm();
            intro.Advance(Frame);
        }
        Assert.Equal(until, intro.Phase);
    }

    [Fact]
    public void TheIntroductionGoesThroughItsStepsInOrder()
    {
        var intro = new IntroScreen();
        Assert.False(intro.IsActive);
        intro.Open();
        Assert.Equal(IntroPhase.FadeIn, intro.Phase);
        Assert.Equal(0f, intro.Appearance().Backdrop);
        Assert.Equal(0f, intro.Appearance().Professor);

        // Buttons do nothing while it fades in; then the professor speaks, unnamed until he says who he is
        intro.PressConfirm();
        Assert.Equal(IntroPhase.FadeIn, intro.Phase);
        Run(intro, IntroScreen.FadeInTime + 0.05f);
        Assert.Equal(IntroPhase.Greeting, intro.Phase);
        Assert.Equal((1f, 1f), (intro.Appearance().Backdrop, intro.Appearance().Professor));
        Assert.True(intro.Talking);
        Assert.Equal("", intro.Speaker);

        // The A button finishes a line being written, then goes on to the next
        Assert.False(intro.LineComplete);
        intro.PressConfirm();
        Assert.True(intro.LineComplete);
        string first = intro.SpokenText;
        intro.PressConfirm();
        Assert.NotEqual(first, intro.SpokenText);
        // The B button finishes a line but never skips one
        intro.PressCancel();
        Assert.True(intro.LineComplete);
        string second = intro.SpokenText;
        intro.PressCancel();
        Assert.Equal(second, intro.SpokenText);
        intro.PressConfirm();
        Assert.Equal(IntroScreen.Professor, intro.Speaker);

        // The world, then the ball: it opens by itself and a Pokémon is there
        Through(intro, IntroPhase.BallOpens);
        Assert.False(intro.Talking);
        Assert.Equal(0f, intro.Appearance().Pokemon);
        intro.PressConfirm();
        Assert.Equal(IntroPhase.BallOpens, intro.Phase);
        Run(intro, 0.45f);
        Assert.True(intro.Appearance().Ball > 0.5f);
        Assert.Equal(0f, intro.Appearance().BallOpen);
        Run(intro, 0.35f);
        Assert.True(intro.Appearance().Flash > 0.5f);
        Run(intro, IntroScreen.BallOpenTime);
        Assert.Equal(IntroPhase.Alongside, intro.Phase);
        var beside = intro.Appearance();
        Assert.Equal((1f, 0f, 0f, 1f), (beside.Pokemon, beside.Ball, beside.Flash, beside.ProfessorShift));

        // It goes back, and he asks who the player is
        Through(intro, IntroPhase.BallCloses);
        Run(intro, IntroScreen.BallCloseTime + 0.05f);
        Assert.Equal(IntroPhase.AboutYou, intro.Phase);
        Assert.Equal((0f, 0f), (intro.Appearance().Pokemon, intro.Appearance().ProfessorShift));
        Through(intro, IntroPhase.ChooseLook);
        Assert.Equal(PlayerLook.Boy, intro.Look);
    }

    [Fact]
    public void TheIntroductionAsksWhoThePlayerIsAndWhatTheyAreCalled()
    {
        var intro = new IntroScreen();
        intro.Open();
        Through(intro, IntroPhase.ChooseLook);
        Run(intro, 0.6f);
        Assert.Equal((1f, 0f), (intro.Appearance().Choice, intro.Appearance().Professor));

        // Left and right between the two; up and down do nothing
        intro.Move(1, 0);
        Assert.Equal(PlayerLook.Girl, intro.Look);
        intro.Move(1, 0);
        Assert.Equal(PlayerLook.Girl, intro.Look);
        intro.Move(0, 1);
        Assert.Equal(PlayerLook.Girl, intro.Look);
        intro.Move(-1, 0);
        Assert.Equal(PlayerLook.Boy, intro.Look);
        intro.Move(1, 0);

        // "So you're a girl?": yes is under the cursor; no goes back to the two
        intro.PressConfirm();
        Assert.Equal(IntroPhase.ConfirmLook, intro.Phase);
        Assert.True(intro.AnswerYes);
        intro.Move(1, 0);
        Assert.False(intro.AnswerYes);
        intro.PressConfirm();
        Assert.Equal((IntroPhase.ChooseLook, PlayerLook.Girl), (intro.Phase, intro.Look));
        intro.PressConfirm();
        intro.PressCancel();
        Assert.Equal(IntroPhase.ChooseLook, intro.Phase);
        intro.PressConfirm();
        Assert.True(intro.AnswerYes);
        intro.PressConfirm();
        Assert.Equal(IntroPhase.AskName, intro.Phase);

        // The keyboard: with nothing entered the name is the character's own
        Through(intro, IntroPhase.EnterName);
        Assert.NotNull(intro.Entry);
        Assert.Equal("Dawn", intro.Name);
        intro.Move(0, 1);
        intro.Move(1, 0);
        intro.Move(1, 0);
        intro.PressConfirm();
        Assert.Equal("M", intro.Entry!.Text);
        foreach (char c in "ayaa") intro.Entry.Type(c);
        // The B button deletes
        intro.PressCancel();
        Assert.Equal("Maya", intro.Name);
        // The Start button jumps to OK
        intro.PressStart();
        intro.PressConfirm();
        Assert.Equal(IntroPhase.ConfirmName, intro.Phase);

        // "So you're Maya?": no goes back to the keyboard with the name still there
        intro.Move(1, 0);
        intro.PressConfirm();
        Assert.Equal((IntroPhase.EnterName, "Maya"), (intro.Phase, intro.Entry.Text));
        Assert.False(intro.Entry.Done);
        intro.PressStart();
        intro.PressConfirm();
        intro.PressCancel();
        Assert.Equal(IntroPhase.EnterName, intro.Phase);
        intro.PressStart();
        intro.PressConfirm();
        intro.PressConfirm();
        Assert.Equal(IntroPhase.Farewell, intro.Phase);

        // He says the name, and sends the player off
        intro.PressConfirm();
        Assert.StartsWith("Maya!", intro.SpokenText);
        Assert.Equal(1f, intro.Appearance().Professor);
        Through(intro, IntroPhase.SendOff);
        Assert.False(intro.Talking);
        Run(intro, 1f);
        var leaving = intro.Appearance();
        Assert.Equal((1f, 0f), (leaving.Dark, leaving.Professor));
        Assert.True(leaving.SpriteScale > 0.9f);
        // The sprite shrinks away before the dark is all there is
        Run(intro, 1.4f);
        Assert.Equal(0f, intro.Appearance().SpriteScale);
        Assert.Equal(IntroPhase.SendOff, intro.Phase);
        Run(intro, IntroScreen.SendOffTime);
        Assert.Equal(IntroPhase.Done, intro.Phase);
        Assert.False(intro.IsActive);
        Assert.Equal(("Maya", PlayerLook.Girl), (intro.Name, intro.Look));
        Assert.Equal(1f, intro.Appearance().Dark);

        // The boy with nothing entered is Lucas
        var plain = new IntroScreen();
        plain.Open();
        Through(plain, IntroPhase.ChooseLook);
        plain.PressConfirm();
        plain.PressConfirm();
        Through(plain, IntroPhase.EnterName);
        plain.PressStart();
        plain.PressConfirm();
        plain.PressConfirm();
        Assert.Equal(("Lucas", PlayerLook.Boy), (plain.Name, plain.Look));
    }

    [Fact]
    public void TheIntroductionShowsPlatinumsPokemonAndItHasAModel()
    {
        Assert.Equal("Buneary", IntroScreen.ShownSpecies);
        Assert.True(PokemonModels.HasModel(IntroScreen.ShownSpecies));
        Assert.NotNull(PokemonDatabase.Get(IntroScreen.ShownSpecies));
    }
}
