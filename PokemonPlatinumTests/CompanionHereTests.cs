using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 10 · F11, what the Pokédex's HERE panel does without a window: it opens only with the DexNav's list, walks it,
/// buzzes on a species not seen and opens the entry of one that is; and the National Pokédex's script gives the DexNav.
/// </summary>
[Collection("Shared clocks and dice")]
public class CompanionHereTests
{
    private static (PokedexScreen Screen, IReadOnlyList<DexNavEntry> Here) OnRoute201()
    {
        var habitats = Habitats.Sinnoh!;
        var (cx, cy) = habitats.Of("Starly").First(h => h.Name == "Route 201").Cells[0];
        var dex = new Pokedex();
        dex.RegisterSeen(PokemonDatabase.Get("Starly")!.DexNumber);
        dex.RegisterCaught(Mon("Bidoof", 3));
        var here = DexNavRules.Here(habitats, cx * Habitats.CellTiles, cy * Habitats.CellTiles, TimeOfDay.Morning, dex, new DexNavState());
        var screen = new PokedexScreen();
        screen.Open(dex);
        return (screen, here);
    }

    [Fact]
    public void TheHerePanelOpensOnlyWithTheDexNavsList()
    {
        var (screen, here) = OnRoute201();
        screen.ToggleHere();
        Assert.Equal(PokedexFocus.List, screen.Focus);
        screen.SetHere(here, "Route 201");
        screen.ToggleHere();
        Assert.Equal(PokedexFocus.Here, screen.Focus);
        Assert.Equal("Route 201", screen.HerePlace);
        screen.Cancel();
        Assert.Equal(PokedexFocus.List, screen.Focus);
        // Without the DexNav (told null) nothing opens
        screen.SetHere(null);
        screen.ToggleHere();
        Assert.Equal(PokedexFocus.List, screen.Focus);
    }

    [Fact]
    public void ThePanelWalksTheListAndAnUnseenSpeciesOnlyBuzzes()
    {
        var (screen, here) = OnRoute201();
        screen.SetHere(here, "Route 201");
        screen.ToggleHere();
        int kricketot = here.ToList().FindIndex(e => e.Species == "Kricketot");
        Assert.True(kricketot > 0 && !here[kricketot].Seen);
        for (int i = 0; i < kricketot; i++) screen.Move(1);
        Assert.Equal(kricketot, screen.HereIndex);
        screen.Confirm();
        Assert.Equal(PokedexFocus.Here, screen.Focus);
        // Up wraps from the first to the last
        screen.SetHere(here, "Route 201");
        screen.Move(-1);
        Assert.Equal(here.Count - 1, screen.HereIndex);
    }

    [Fact]
    public void ASeenSpeciesOpensItsEntryAndBackComesToTheList()
    {
        var (screen, here) = OnRoute201();
        screen.SetHere(here, "Route 201");
        screen.ToggleHere();
        int starly = here.ToList().FindIndex(e => e.Species == "Starly");
        Assert.True(here[starly].Seen);
        for (int i = 0; i < starly; i++) screen.Move(1);
        screen.Confirm();
        Assert.Equal(PokedexFocus.Entry, screen.Focus);
        Assert.Equal("Starly", screen.Selected!.Value.Species.Name);
        screen.Cancel();
        Assert.Equal(PokedexFocus.List, screen.Focus);
    }

    [Fact]
    public void TheNationalPokedexsScriptGivesTheDexNavOnce()
    {
        var host = new HeadlessScriptHost();
        var item = ItemDatabase.Get(DexNavRules.Item)!;
        Assert.Equal(0, host.Bag.GetQuantity(item));
        host.GiveNationalDex();
        host.GiveNationalDex();
        Assert.Equal(1, host.Bag.GetQuantity(item));
        Assert.Equal("DexNav", item.FieldUse);
        Assert.Equal(ItemPocket.KeyItems, item.Pocket);
    }
}
