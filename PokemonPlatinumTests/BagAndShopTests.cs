using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The bag, items and shops (plan 06 · R11): an item used on a Pokémon by the item table, as Platinum's
/// <c>Pokemon_CheckItemEffects</c> and <c>Pokemon_ApplyItemEffects</c> (src/item_use_pokemon.c) have it; PP Ups;
/// TMs and HMs; the bag's TOSS, Sacred Ash, Repels and flutes; the marts' stock and selling; and the battle's bag.
/// </summary>
public class BagAndShopTests
{
    private static ItemData Item(string name) => ItemDatabase.Get(name) ?? throw new InvalidOperationException(name + " is not an item");

    private static Pokemon Mon(string species, int level, params string[] moves)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);
        if (moves.Length > 0)
        {
            p.Moves.Clear();
            foreach (string m in moves) p.Moves.Add(MoveDatabase.Create(m));
        }
        return p;
    }

    // ------------------------------------------------------------------ an item used on a Pokémon

    [Fact]
    public void HitPointsComeBackByTheTablesAmountsAndShares()
    {
        var p = Mon("Bidoof", 30);
        int max = p.MaxHP;

        // RestorePokemonHP: so many points, never past the most
        p.CurrentHP = 1;
        Assert.True(ItemUse.Apply(Item("Potion"), p).Applied);
        Assert.Equal(1 + 20, p.CurrentHP);
        p.CurrentHP = 1;
        ItemUse.Apply(Item("Hyper Potion"), p);
        Assert.Equal(Math.Min(max, 1 + 200), p.CurrentHP);

        // A Sitrus Berry used from the bag: the table's -3, a quarter of the most, rounded down
        p.CurrentHP = 1;
        ItemUse.Apply(Item("Sitrus Berry"), p);
        Assert.Equal(1 + max / 4, p.CurrentHP);

        // A Revive brings it round with half, a Max Revive with all; neither helps one that is up
        p.CurrentHP = 0;
        p.Status = StatusCondition.Faint;
        Assert.False(ItemUse.WouldHelp(Item("Potion"), p));
        ItemUse.Apply(Item("Revive"), p);
        Assert.Equal((max / 2, false), (p.CurrentHP, p.IsFainted));
        Assert.False(ItemUse.WouldHelp(Item("Max Revive"), p));

        // A Pokémon whose most is 1 is given 1, whatever the item says (Shedinja)
        Assert.Equal(1, ItemUse.Restored(0, 1, 200));
        Assert.Equal(1, ItemUse.Restored(0, 1, -1));
    }

    [Fact]
    public void ARareCandyRaisesTheLevelAndBringsAFaintedPokemonRoundWithWhatItGained()
    {
        var p = Mon("Bidoof", 10);
        int oldMax = p.MaxHP;
        p.CurrentHP = 0;
        p.Status = StatusCondition.Faint;

        var result = ItemUse.Apply(Item("Rare Candy"), p);

        // Pokemon_ApplyItemEffects: RestorePokemonHP of the difference between the new most and the old
        Assert.True(result.Applied);
        Assert.Equal(11, p.Level);
        Assert.Equal(11, result.NewLevel);
        Assert.False(p.IsFainted);
        Assert.Equal(Math.Max(1, p.MaxHP - oldMax), p.CurrentHP);

        var top = Mon("Bidoof", 100);
        Assert.False(ItemUse.WouldHelp(Item("Rare Candy"), top));
    }

    [Fact]
    public void PpUpsAddAFifthOfAMovesPpEachUpToThree()
    {
        var p = Mon("Bidoof", 20, "Tackle", "Sketch");
        var tackle = p.Moves[0];
        Assert.Equal(35, tackle.MaxPP);

        // MoveTable_CalcMaxPP: 35 + 35 * 20 * 1 / 100 = 42, and the PP gained comes with it
        Assert.True(ItemUse.Apply(Item("PP Up"), p, 0).Applied);
        Assert.Equal((1, 42, 42), (tackle.PPUps, tackle.MaxPP, tackle.CurrentPP));
        // A PP Max gives all three: 35 + 35 * 60 / 100 = 56
        ItemUse.Apply(Item("PP Max"), p, 0);
        Assert.Equal((3, 56, 56), (tackle.PPUps, tackle.MaxPP, tackle.CurrentPP));
        Assert.False(ItemUse.WouldHelp(Item("PP Up"), p, 0));

        // A move with fewer than five PP to begin with takes none (PP_UP_REQUIREMENT: Sketch's 1)
        Assert.False(ItemUse.WouldHelp(Item("PP Up"), p, 1));

        // An Ether gives one move 10 PP back, a Max Ether all of them; an Elixir all four moves
        tackle.CurrentPP = 10;
        ItemUse.Apply(Item("Ether"), p, 0);
        Assert.Equal(20, tackle.CurrentPP);
        ItemUse.Apply(Item("Max Ether"), p, 0);
        Assert.Equal(56, tackle.CurrentPP);
        Assert.False(ItemUse.WouldHelp(Item("Ether"), p, 0));
        Assert.True(ItemUse.NeedsMove(Item("Leppa Berry")));
        Assert.False(ItemUse.NeedsMove(Item("Elixir")));
    }

    [Fact]
    public void FriendshipComesWithTheItemOnlyOnceItHasDoneSomething()
    {
        var p = Mon("Bidoof", 20);
        p.Friendship = 50;
        p.CurrentHP = p.MaxHP;

        // A Rare Candy's +5 below 100 (the table's friendshipLow), half again with a Soothe Bell: 5 * 150 / 100 = 7
        ItemUse.Apply(Item("Rare Candy"), p);
        Assert.Equal(55, p.Friendship);
        p.HeldItem = Item("Soothe Bell");
        ItemUse.Apply(Item("Rare Candy"), p);
        Assert.Equal(62, p.Friendship);
        p.HeldItem = null;

        // The bitter herbs cost friendship, but only when they heal: one that does nothing costs nothing
        p.Status = StatusCondition.Burn;
        ItemUse.Apply(Item("Heal Powder"), p);
        Assert.Equal((StatusCondition.None, 57), (p.Status, p.Friendship));
        Assert.False(ItemUse.Apply(Item("Heal Powder"), p).Applied);
        Assert.Equal(57, p.Friendship);

        // Confusion is a battle's alone: a Persim Berry in the field has nothing to cure
        Assert.False(ItemUse.WouldHelp(Item("Persim Berry"), p));
    }

    [Fact]
    public void PpUpsAreSavedAndCarriedIntoABattlesCopies()
    {
        var p = Mon("Bidoof", 20, "Tackle");
        ItemUse.Apply(Item("PP Up"), p, 0);
        var saved = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal((1, 42), (saved.Moves[0].PPUps, saved.Moves[0].MaxPP));
        var copy = p.Clone();
        Assert.Equal(1, copy.Moves[0].PPUps);
    }

    // ------------------------------------------------------------------ TMs and HMs

    [Fact]
    public void AMachineIsLearnedByTheSpeciesItsListNames()
    {
        var turtwig = Mon("Turtwig", 20, "Tackle");
        Assert.Equal(TeachAnswer.Able, MoveTeaching.Answer(turtwig, Item("TM06")));
        Assert.Equal(TeachAnswer.NotAble, MoveTeaching.Answer(turtwig, Item("TM24")));
        Assert.True(MoveTeaching.CanLearn(Mon("Bidoof", 20), Item("TM24")));
        turtwig.Moves.Add(MoveDatabase.Create("Toxic"));
        Assert.Equal(TeachAnswer.Learned, MoveTeaching.Answer(turtwig, Item("TM06")));

        // Every species of Platinum's has its list, and every machine it names is one of Platinum's TMs and HMs
        Assert.All(PokemonDatabase.GetAll().Where(s => s.Generation <= 4 && s.TmMoves != null), s =>
            Assert.All(s.TmMoves!, m => Assert.True(MoveTeaching.IsMachine(Item(m)), $"{s.Name}: {m}")));
        Assert.True(MoveTeaching.IsHmMove("Rock Smash"));
        Assert.False(MoveTeaching.IsHmMove("Toxic"));
    }

    private static (BagScreen Bag, Inventory Inventory, Party Party, List<string> Notes) Bag(Pokemon p, params (string Item, int Count)[] items)
    {
        var inventory = new Inventory();
        foreach (var (item, count) in items) inventory.AddItem(Item(item), count);
        var party = new Party();
        party.Add(p);
        var bag = new BagScreen();
        bag.Open();
        return (bag, inventory, party, new List<string>());
    }

    private static void OpenItem(BagScreen bag, Inventory inventory, Party party, List<string> notes, ItemData item)
    {
        bag.CurrentPocket = item.Pocket;
        bag.SelectedIndex = inventory.GetPocketItems(item.Pocket).FindIndex(s => s.Data == item);
        bag.Confirm(inventory, party, notes.Add);
    }

    private static void Choose(BagScreen bag, BagAction action, Inventory inventory, Party party, List<string> notes)
    {
        while (bag.Actions![bag.ActionIndex] != action) bag.MoveCursor(1, 0);
        bag.Confirm(inventory, party, notes.Add);
    }

    [Fact]
    public void ATmIsUsedUpAndAPokemonWithFourMovesForgetsOneForIt()
    {
        var turtwig = Mon("Turtwig", 20, "Tackle", "Withdraw", "Absorb", "Bite");
        var (bag, inventory, party, notes) = Bag(turtwig, ("TM06", 1));
        OpenItem(bag, inventory, party, notes, Item("TM06"));
        Assert.Equal(new[] { BagAction.Use, BagAction.Toss, BagAction.Cancel }, bag.Actions);
        Choose(bag, BagAction.Use, inventory, party, notes);
        Assert.Equal("ABLE", bag.TagFor(turtwig, new EvolutionContext { Party = party, Bag = inventory }));

        bag.UseOnTarget(inventory, party, notes.Add, new EvolutionContext { Party = party, Bag = inventory });
        Assert.Equal((turtwig, "Toxic"), (bag.Teaching!.Value.Pokemon, bag.Teaching!.Value.Move));
        bag.MoveForget(1);
        bag.MoveForget(1);
        bag.ConfirmForget(inventory, notes.Add);

        Assert.Equal(new[] { "Tackle", "Withdraw", "Toxic", "Bite" }, turtwig.Moves.Select(m => m.Name));
        Assert.Equal(0, inventory.GetQuantity(Item("TM06")));
        Assert.Contains("forgot Absorb", notes[^1]);
    }

    [Fact]
    public void AnHmStaysInTheBagAndItsMoveCannotBeForgottenToMakeRoom()
    {
        var bidoof = Mon("Bidoof", 20, "Rock Smash", "Tackle", "Growl", "Headbutt");
        var (bag, inventory, party, notes) = Bag(bidoof, ("HM01", 1));
        Assert.False(BagScreen.CanToss(Item("HM01")));
        OpenItem(bag, inventory, party, notes, Item("HM01"));
        Assert.Equal(new[] { BagAction.Use, BagAction.Cancel }, bag.Actions);
        Choose(bag, BagAction.Use, inventory, party, notes);
        bag.UseOnTarget(inventory, party, notes.Add, new EvolutionContext { Party = party, Bag = inventory });

        // Rock Smash came from an HM: it stays
        bag.ConfirmForget(inventory, notes.Add);
        Assert.Equal("HM moves can't be forgotten now.", notes[^1]);
        Assert.NotNull(bag.Teaching);
        bag.MoveForget(1);
        bag.ConfirmForget(inventory, notes.Add);
        Assert.Equal(new[] { "Rock Smash", "Cut", "Growl", "Headbutt" }, bidoof.Moves.Select(m => m.Name));
        Assert.Equal(1, inventory.GetQuantity(Item("HM01")));
    }

    [Fact]
    public void ARareCandysNewMoveAsksForARoomAndCanBeGivenUp()
    {
        // Turtwig learns Absorb at 9
        var turtwig = Mon("Turtwig", 8, "Tackle", "Withdraw", "Bite", "Growl");
        var (bag, inventory, party, notes) = Bag(turtwig, ("Rare Candy", 1));
        OpenItem(bag, inventory, party, notes, Item("Rare Candy"));
        Choose(bag, BagAction.Use, inventory, party, notes);
        bag.UseOnTarget(inventory, party, notes.Add, new EvolutionContext { Party = party, Bag = inventory });

        Assert.Equal(9, turtwig.Level);
        Assert.Equal("Absorb", bag.Teaching!.Value.Move);
        for (int i = 0; i < 4; i++) bag.MoveForget(1);
        bag.ConfirmForget(inventory, notes.Add);
        Assert.Contains("did not learn Absorb", notes[^1]);
        Assert.DoesNotContain(turtwig.Moves, m => m.Name == "Absorb");
        Assert.Null(bag.ChoosingFor);
    }

    [Fact]
    public void AnEtherGoesOnToTheMoveItRestores()
    {
        var p = Mon("Bidoof", 20, "Tackle", "Growl");
        p.Moves[1].CurrentPP = 5;
        var (bag, inventory, party, notes) = Bag(p, ("Ether", 2));
        OpenItem(bag, inventory, party, notes, Item("Ether"));
        Choose(bag, BagAction.Use, inventory, party, notes);
        bag.UseOnTarget(inventory, party, notes.Add, new EvolutionContext { Party = party, Bag = inventory });
        Assert.True(bag.ChoosingMove);

        // Tackle's PP is full: nothing happens and the Ether stays
        bag.UseOnMove(inventory, party, notes.Add);
        Assert.Equal(2, inventory.GetQuantity(Item("Ether")));
        bag.MoveMove(1, party);
        bag.UseOnMove(inventory, party, notes.Add);
        Assert.Equal((15, 1), (p.Moves[1].CurrentPP, inventory.GetQuantity(Item("Ether"))));
    }

    // ------------------------------------------------------------------ the bag's own uses

    [Fact]
    public void ItemsAreThrownAwayAsManyAsAreChosen()
    {
        var (bag, inventory, party, notes) = Bag(Mon("Bidoof", 5), ("Potion", 5), ("Bicycle", 1));
        Assert.False(BagScreen.CanToss(Item("Bicycle")));
        OpenItem(bag, inventory, party, notes, Item("Potion"));
        Assert.Equal(new[] { BagAction.Use, BagAction.Give, BagAction.Toss, BagAction.Cancel }, bag.Actions);
        Choose(bag, BagAction.Toss, inventory, party, notes);
        Assert.True(bag.Tossing);
        bag.MoveToss(1, 0, 5);
        bag.MoveToss(0, -1, 5);
        Assert.Equal(5, bag.TossCount);
        bag.MoveToss(-1, 0, 5);
        bag.MoveToss(-1, 0, 5);
        bag.Confirm(inventory, party, notes.Add);
        Assert.Equal(2, inventory.GetQuantity(Item("Potion")));
        Assert.False(bag.Tossing);
    }

    [Fact]
    public void SacredAshBringsTheWholeTeamRound()
    {
        var a = Mon("Bidoof", 20);
        var b = Mon("Starly", 20);
        foreach (var p in new[] { a, b })
        {
            p.CurrentHP = 0;
            p.Status = StatusCondition.Faint;
        }
        var (bag, inventory, party, notes) = Bag(a, ("Sacred Ash", 1));
        party.Add(b);
        OpenItem(bag, inventory, party, notes, Item("Sacred Ash"));
        Choose(bag, BagAction.Use, inventory, party, notes);
        Assert.All(party.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
        Assert.Equal(0, inventory.GetQuantity(Item("Sacred Ash")));
    }

    [Fact]
    public void ARepelTurnsAwayWeakerPokemonForItsStepsAndAFluteChangesTheRate()
    {
        var aids = new EncounterAids();
        var (said, usedUp) = aids.Use(Item("Repel"), "Lucas");
        Assert.True(usedUp);
        Assert.Equal(100, aids.RepelSteps);
        // UseItemInBag: another can't be used while one is working
        Assert.False(aids.Use(Item("Super Repel"), "Lucas").UsedUp);
        Assert.Equal(100, aids.RepelSteps);

        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Bidoof")!, 1) { CurrentHP = 0, Status = StatusCondition.Faint });
        party.Add(Mon("Starly", 12));
        var lead = WildLead.Of(party, aids);
        // The level is the first Pokémon's that can fight (Party_FindFirstEligibleBattler)
        Assert.Equal(12, lead!.Value.RepelLevel);
        Assert.True(WildEncounterRules.RepelTurnsAway(lead, 11));
        Assert.False(WildEncounterRules.RepelTurnsAway(lead, 12));
        var table = new List<WildEncounterEntry> { new() { SpeciesName = "Bidoof", MinLevel = 4, MaxLevel = 4, Weight = 100 } };
        Assert.Null(WildEncounterRules.Meet(table, false, lead, new Random(1)));

        for (int i = 0; i < 99; i++) Assert.False(aids.Step());
        Assert.True(aids.Step());
        Assert.False(aids.RepelActive);

        // ModifyEncounterRateWithFlute: the Black Flute halves, the White one adds half again
        aids.Use(Item("Black Flute"), "Lucas");
        Assert.Equal(15, WildEncounterRules.Rate(30, WildLead.Of(party, aids), FieldWeather.Clear));
        aids.Use(Item("White Flute"), "Lucas");
        Assert.Equal(45, WildEncounterRules.Rate(30, WildLead.Of(party, aids), FieldWeather.Clear));
        aids.ChangePlace();
        Assert.Equal(30, WildEncounterRules.Rate(30, WildLead.Of(party, aids), FieldWeather.Clear));
    }

    [Fact]
    public void TheRepelsStepsAreSaved()
    {
        var save = System.Text.Json.JsonSerializer.Deserialize<SaveData>(System.Text.Json.JsonSerializer.Serialize(new SaveData { RepelSteps = 42 }))!;
        Assert.Equal(42, save.RepelSteps);
        Assert.Equal(0, new SaveData().RepelSteps);
    }

    // ------------------------------------------------------------------ the marts

    [Fact]
    public void TheCommonCounterSellsMoreWithEveryFewBadges()
    {
        // ScrCmd_PokeMartCommon's steps
        Assert.Equal(new[] { 1, 2, 2, 3, 3, 4, 4, 5, 6 }, Enumerable.Range(0, 9).Select(MartDatabase.StepFor));
        Assert.Equal(new[] { "Poké Ball", "Potion", "Antidote", "Paralyze Heal" }, MartDatabase.Stock(null, 0).Select(i => i.Name));
        Assert.Equal(19, MartDatabase.Stock(null, 8).Count);
        Assert.Contains(MartDatabase.Stock(null, 3), i => i.Name == "Great Ball");
        Assert.DoesNotContain(MartDatabase.Stock(null, 2), i => i.Name == "Great Ball");
        Assert.Equal(new[] { "Air Mail", "Heal Ball" }, MartDatabase.Stock("jubilife", 0).Select(i => i.Name));
        Assert.Empty(MartDatabase.Stock("nowhere", 8));
        // Every item a counter names is one of the game's
        Assert.All(MartDatabase.Specialties, s => Assert.Equal(s.Value.Count, MartDatabase.Stock(s.Key, 0).Count));
    }

    [Fact]
    public void EveryTownsMartHasItsCommonCounterAndItsOwn()
    {
        var marts = Directory.GetFiles(GameDataFiles.PathOf(MapDatabase.Folder), "*PokeMart.json").Select(Path.GetFileNameWithoutExtension).ToList();
        Assert.NotEmpty(marts);
        foreach (string name in marts)
        {
            var map = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();
            var clerks = map.Everyone.Where(n => n.IsPokeMartClerk).ToList();
            Assert.Single(clerks, c => c.Mart == null);
            // Sandgem Town's Mart has the common counter alone, as in Platinum
            if (name == "PokeMart") Assert.Single(clerks);
            else Assert.Single(clerks, c => c.Mart != null && MartDatabase.Specialties.ContainsKey(c.Mart));
        }
    }

    [Fact]
    public void AShopBuysAndSellsAtHalfPrice()
    {
        var inventory = new Inventory();
        inventory.AddItem(Item("Bicycle"), 1);
        var notes = new List<string>();
        var shop = new ShopScreen();
        shop.Open("Jubilife", MartDatabase.Stock(null, 0));
        Assert.Equal(ShopMode.Choosing, shop.Mode);

        // BUY: two Potions
        shop.Confirm(inventory, 3000, notes.Add);
        Assert.Equal(ShopMode.Buying, shop.Mode);
        shop.Move(0, 1, 3000);
        Assert.Equal("Potion", shop.Selected!.Name);
        shop.Confirm(inventory, 3000, notes.Add);
        shop.Move(1, 0, 3000);
        Assert.Equal(-600, shop.Confirm(inventory, 3000, notes.Add));
        Assert.Equal(2, inventory.GetQuantity(Item("Potion")));

        // SELL: the Bicycle can't be sold; a Potion fetches half its price
        shop.Cancel();
        shop.Move(0, 1, 2400);
        shop.Confirm(inventory, 2400, notes.Add);
        Assert.Equal(ShopMode.Selling, shop.Mode);
        Assert.Equal(new[] { "Potion" }, shop.Listed.Select(i => i.Name));
        shop.Confirm(inventory, 2400, notes.Add);
        Assert.Equal(150, shop.Confirm(inventory, 2400, notes.Add));
        Assert.Equal(1, inventory.GetQuantity(Item("Potion")));
        Assert.Equal(0, ShopScreen.SellPrice(Item("Bicycle")));
    }

    [Fact]
    public void AClerksCounterIsOpenedByTheShopCommand()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Clerk)!, new NPC { IsPokeMartClerk = true, Mart = "oreburgh" });
        runner.RunToEnd();
        Assert.Contains("open Shop oreburgh", host.Log);

        var wrong = ScriptLibrary.FromSources(("common", "script S\n shop \"nowhere\"\n end"));
        Assert.Contains(wrong.Problems(), p => p.Contains("there is no counter"));
    }

    // ------------------------------------------------------------------ the battle's bag

    [Fact]
    public void TheBattlesPocketsAreFilledByEachItemsBattlePocket()
    {
        Assert.True(BattleBag.InPocket(Item("Potion"), 0));
        Assert.True(BattleBag.InPocket(Item("Ether"), 0));
        Assert.True(BattleBag.InPocket(Item("Oran Berry"), 0));
        Assert.True(BattleBag.InPocket(Item("Full Heal"), 1));
        Assert.True(BattleBag.InPocket(Item("Lum Berry"), 1));
        Assert.True(BattleBag.InPocket(Item("Full Restore"), 0) && BattleBag.InPocket(Item("Full Restore"), 1));
        Assert.True(BattleBag.InPocket(Item("Poké Ball"), 2));
        Assert.True(BattleBag.InPocket(Item("X Attack"), 3));
        Assert.False(BattleBag.InPocket(Item("Bicycle"), 0));
        // Berries that heal are used from the battle's bag too
        Assert.True(BattleCore.CanUseInBattle(Item("Oran Berry")));
    }

    private static BattleEngine Battle(Party party, Inventory inventory)
    {
        var battle = new BattleEngine(party, new Pokemon(PokemonDatabase.Get("Bidoof")!, 3), inventory, new Pokedex(), null, new List<Pokemon>());
        for (int i = 0; i < 20 && battle.HUD.MenuState == BattleMenuState.Message; i++) battle.ConfirmMessage();
        return battle;
    }

    private static void ToMenu(BattleEngine battle)
    {
        for (int i = 0; i < 40 && battle.HUD.MenuState is BattleMenuState.Message or BattleMenuState.LearnMove && !battle.IsBattleOver; i++) battle.ConfirmMessage();
    }

    [Fact]
    public void AnItemFromTheBattlesBagGoesToThePokemonAndTheMoveItIsFor()
    {
        var lead = Mon("Turtwig", 30, "Tackle");
        var bench = Mon("Starly", 30, "Tackle", "Growl");
        bench.CurrentHP = 10;
        bench.Moves[1].CurrentPP = 0;
        var party = new Party();
        party.Add(lead);
        party.Add(bench);
        var inventory = new Inventory();
        inventory.AddItem(Item("Potion"), 2);
        inventory.AddItem(Item("Ether"), 2);
        var battle = Battle(party, inventory);

        battle.SelectMainMenuOption(1);
        Assert.Equal(BattleMenuState.SelectBagPocket, battle.HUD.MenuState);
        battle.SelectBagPocket(0);
        Assert.Equal(new[] { "Potion", "Ether" }, battle.BagListed.Select(s => s.Name));
        battle.SelectBagItem(0);
        Assert.Equal(BattleMenuState.SelectBagTarget, battle.HUD.MenuState);
        battle.SelectBagTarget(1);
        ToMenu(battle);
        Assert.Equal(30, bench.CurrentHP);
        Assert.Equal((1, "Potion"), (inventory.GetQuantity(Item("Potion")), battle.LastUsedItem));

        // The Ether goes on to the bench Pokémon's moves
        battle.SelectMainMenuOption(1);
        battle.SelectBagPocket(0);
        battle.SelectBagItem(1);
        battle.SelectBagTarget(1);
        Assert.Equal(BattleMenuState.SelectBagMove, battle.HUD.MenuState);
        battle.SelectBagMove(1);
        ToMenu(battle);
        Assert.Equal(10, bench.Moves[1].CurrentPP);

        // The last item used is the fifth button, while there is any of it left
        battle.SelectMainMenuOption(1);
        battle.SelectBagPocket(BattleBag.LastUsed);
        Assert.Equal((BattleMenuState.SelectBagTarget, "Ether"), (battle.HUD.MenuState, battle.BagItemChosen?.Name));
    }
}
