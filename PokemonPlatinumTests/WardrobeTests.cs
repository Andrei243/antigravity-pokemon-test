using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>The player's clothes (plan 11 · C9 and C10, first part): the garments, the outfit, the wardrobe and its screen.</summary>
public class WardrobeTests
{
    private readonly ITestOutputHelper output;

    public WardrobeTests(ITestOutputHelper output) => this.output = output;

    // ------------------------------------------------------------------ the data

    [Fact]
    public void EveryGarmentIsWellMade()
    {
        Assert.NotEmpty(ClothingDatabase.All);
        Assert.Equal(ClothingDatabase.All.Count, ClothingDatabase.All.Select(g => g.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var g in ClothingDatabase.All)
        {
            // An outfit's key joins ids with dots and a dressed character's name follows an @
            Assert.Matches(new Regex("^[a-z][a-z0-9_]*$"), g.Id);
            Assert.False(string.IsNullOrWhiteSpace(g.Name), g.Id);
            Assert.False(string.IsNullOrWhiteSpace(g.Description), g.Id);
            foreach (var hex in new[] { g.Color, g.Accent }.OfType<string>()) Garment.Rgb(hex);
            if (g.IsNothing)
            {
                // Only a hat or a bag can be left off, and leaving it off costs nothing
                Assert.Contains(g.Slot, new[] { ClothingSlot.Hat, ClothingSlot.Bag });
                Assert.True(g.IsFree, g.Id);
            }
            else Assert.True(g.Price > 0, $"{g.Id} is free but is something to wear");
            if (g.Slot == ClothingSlot.Hat && !g.IsNothing)
                Assert.True(Enum.TryParse<Headwear>(g.Hat, out var hat) && hat != Headwear.None, $"{g.Id} names no hat");
            Assert.False(g.Skirt && g.Shorts, g.Id);
        }
    }

    [Fact]
    public void EveryBoutiqueSellsGarmentsThatExist()
    {
        Assert.Contains("jubilife", ClothingDatabase.Boutiques.Keys);
        foreach (var (key, ids) in ClothingDatabase.Boutiques)
        {
            Assert.All(ids, id => Assert.NotNull(ClothingDatabase.Get(id)));
            Assert.Equal(ids.Count, ClothingDatabase.Stock(key).Count);
            // Something for every slot
            Assert.All(Outfit.Slots, slot => Assert.Contains(ClothingDatabase.Stock(key), g => g.Slot == slot && !g.IsFree));
        }
        Assert.Empty(ClothingDatabase.Stock("nowhere"));
    }

    // ------------------------------------------------------------------ the outfit and the character it dresses

    [Fact]
    public void AnOutfitWithNothingChosenIsThePlainLook()
    {
        Assert.True(Outfit.Own.IsOwn);
        Assert.Equal("PLAYER", Outfit.Dress("PLAYER", Outfit.Own));
        Assert.Equal("DAWN", Outfit.Dress("DAWN", null));
        Assert.Equal(("PLAYER", (Outfit?)null), Outfit.Undress("PLAYER"));
    }

    [Fact]
    public void AnOutfitsKeyReadsBackTheSame()
    {
        var outfit = new Outfit("red_cap", null, "denim_jeans", null, "no_bag");
        Assert.Equal("red_cap.-.denim_jeans.-.no_bag", outfit.Key);
        Assert.Equal(outfit, Outfit.FromKey(outfit.Key));
        string dressed = Outfit.Dress("DAWN", outfit);
        Assert.Equal("DAWN@red_cap.-.denim_jeans.-.no_bag", dressed);
        Assert.Equal(("DAWN", (Outfit?)outfit), Outfit.Undress(dressed));
        Assert.Equal(outfit with { Top = "winter_coat" }, outfit.With(ClothingSlot.Top, "winter_coat"));
    }

    [Theory]
    [InlineData("PLAYER")]
    [InlineData("DAWN")]
    public void TheLooksOwnClothesAreTodaysLook(string look)
    {
        var plain = CharacterStyle.For(look);
        var dressed = CharacterStyle.For(Outfit.Dress(look, Outfit.Own));
        Assert.Empty(Differences(plain, dressed));
        // The dressed name with nothing in it, as a save could write it, is the plain look too
        Assert.Empty(Differences(plain, CharacterStyle.For(look + "@-.-.-.-.-")));
    }

    public static IEnumerable<object[]> EveryGarment =>
        ClothingDatabase.All.SelectMany(g => new[] { new object[] { "PLAYER", g.Id }, new object[] { "DAWN", g.Id } });

    [Theory]
    [MemberData(nameof(EveryGarment))]
    public void AGarmentChangesOnlyWhatItsSlotCovers(string look, string id)
    {
        var g = ClothingDatabase.Get(id)!;
        var plain = CharacterStyle.For(look);
        var dressed = CharacterStyle.Dressed(plain, Outfit.Own.With(g.Slot, id));
        string[] mine = g.Slot switch
        {
            ClothingSlot.Hat => new[] { "Hat", "HatColor", "HatBand" },
            ClothingSlot.Top => new[] { "Top", "Accent", "Coat", "ShortSleeves", "Stripes", "Scarf" },
            ClothingSlot.Bottoms => new[] { "Bottom", "Skirt", "Shorts" },
            ClothingSlot.Shoes => new[] { "Shoes", "Sole" },
            _ => new[] { "Bag" }
        };
        var changed = Differences(plain, dressed);
        Assert.All(changed, field => Assert.Contains(field, mine));
        // The style asked for by the dressed name is the same as the one put together here, and the look's own is untouched
        Assert.Empty(Differences(dressed, CharacterStyle.For(Outfit.Dress(look, Outfit.Own.With(g.Slot, id)))));
        Assert.Empty(Differences(plain, CharacterStyle.For(look)));
        if (g.Slot == ClothingSlot.Hat) Assert.Equal(g.IsNothing, dressed.Hat == Headwear.None);
        if (g.Slot == ClothingSlot.Bag) Assert.Equal(g.IsNothing, dressed.Bag == null);
    }

    private static List<string> Differences(CharacterStyle a, CharacterStyle b) =>
        typeof(CharacterStyle).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => !Equals(f.GetValue(a), f.GetValue(b))).Select(f => f.Name).ToList();

    /// <summary>Outfits that between them wear every garment, on both looks: each must still make a sound body.</summary>
    public static IEnumerable<object[]> OutfitsOfEveryGarment()
    {
        var bySlot = Outfit.Slots.ToDictionary(s => s, s => ClothingDatabase.All.Where(g => g.Slot == s).Select(g => g.Id).ToList());
        int count = bySlot.Values.Max(l => l.Count);
        for (int i = 0; i < count; i++)
        {
            var outfit = Outfit.Own;
            foreach (var slot in Outfit.Slots) outfit = outfit.With(slot, bySlot[slot][i % bySlot[slot].Count]);
            yield return new object[] { "PLAYER", outfit.Key };
            yield return new object[] { "DAWN", outfit.Key };
        }
    }

    [Theory]
    [MemberData(nameof(OutfitsOfEveryGarment))]
    public void EveryOutfitIsOneSmoothSkinnedBody(string look, string key)
    {
        string type = Outfit.Dress(look, Outfit.FromKey(key));
        var rig = CharacterModels.Build(type);
        var mesh = rig.Mesh;
        output.WriteLine($"{type}: {mesh.VertexCount} vertices");
        Assert.InRange(mesh.VertexCount, 4000, ushort.MaxValue);
        Assert.Equal(HumanBones.Count, rig.Skeleton.Count);
        var min = new System.Numerics.Vector3(float.MaxValue);
        var max = new System.Numerics.Vector3(float.MinValue);
        foreach (var p in mesh.Positions) { min = System.Numerics.Vector3.Min(min, p); max = System.Numerics.Vector3.Max(max, p); }
        Assert.InRange(min.Y, -0.01f, 0.01f);
        Assert.InRange(max.Y, 1.1f, 1.5f);
        Assert.NotNull(rig.FacePatch);
        Assert.True(rig.FacePatch!.TriangleCount > 150);
    }

    [Fact]
    public void AHatBoughtShowsOverTheHair()
    {
        foreach (var look in new[] { "PLAYER", "DAWN" })
            foreach (var hat in ClothingDatabase.All.Where(g => g.Slot == ClothingSlot.Hat && !g.IsNothing))
            {
                var rig = CharacterModels.Build(Outfit.Dress(look, Outfit.Own.With(ClothingSlot.Hat, hat.Id)));
                var mesh = rig.Mesh;
                static bool Near(Raylib_cs.Color a, Raylib_cs.Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) < 30;
                int shown = Enumerable.Range(0, mesh.VertexCount).Count(v => mesh.Positions[v].Y > rig.Face.Anchor.Y && Near(mesh.Colors[v], rig.Style.HatColor));
                Assert.True(shown > 400, $"{look} in the {hat.Name}: only {shown} vertices of the hat show");
            }
    }

    // ------------------------------------------------------------------ the wardrobe

    [Fact]
    public void AGarmentBoughtOnceIsWornForNothingAfter()
    {
        var wardrobe = new Wardrobe();
        var cap = ClothingDatabase.Get("red_cap")!;
        Assert.False(wardrobe.Owns(cap.Id));
        Assert.False(wardrobe.Wear(ClothingSlot.Hat, cap.Id));
        Assert.Equal(-1, wardrobe.Buy(cap, cap.Price - 1));
        Assert.Equal(cap.Price, wardrobe.Buy(cap, cap.Price));
        Assert.Equal(cap.Id, wardrobe.Worn.Hat);
        Assert.True(wardrobe.Wear(ClothingSlot.Hat, null));
        Assert.True(wardrobe.Worn.IsOwn);
        Assert.Equal(0, wardrobe.Buy(cap, 0));
        Assert.Equal(cap.Id, wardrobe.Worn.Hat);
        // A garment is only worn in its own slot; the free ones are everyone's
        Assert.False(wardrobe.Wear(ClothingSlot.Top, cap.Id));
        Assert.True(wardrobe.Owns("no_hat"));
        Assert.True(wardrobe.Wear(ClothingSlot.Bag, "no_bag"));
        Assert.Equal(new[] { "red_cap" }, wardrobe.Owned);
    }

    [Fact]
    public void ASavedWardrobeComesBackAndForgetsWhatTheDataLost()
    {
        var wardrobe = new Wardrobe();
        wardrobe.Restore(new Outfit("red_cap", "gone_shirt", null, "black_boots", "no_bag"), new[] { "red_cap", "gone_shirt" });
        Assert.Equal(new[] { "red_cap" }, wardrobe.Owned);
        // The boots were never bought, the shirt is no more: both slots go back to the look's own
        Assert.Equal(new Outfit("red_cap", null, null, null, "no_bag"), wardrobe.Worn);
        wardrobe.Restore(null, null);
        Assert.True(wardrobe.Worn.IsOwn);
        Assert.Empty(wardrobe.Owned);
    }

    [Fact]
    public void ASaveKeepsTheOutfitAndAnOlderSaveIsTheLooksOwn()
    {
        var save = new SaveData { Outfit = new Outfit("blue_cap", "winter_coat", null, null, "no_bag"), Wardrobe = new() { "blue_cap", "winter_coat" } };
        var back = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        Assert.Equal(save.Outfit, back.Outfit);
        Assert.Equal(save.Wardrobe, back.Wardrobe);
        Assert.DoesNotContain("IsOwn", JsonSerializer.Serialize(save.Outfit));

        var old = JsonSerializer.Deserialize<SaveData>("""{ "PlayerName": "Lucas", "Money": 500 }""")!;
        Assert.Null(old.Outfit);
        Assert.Null(old.Wardrobe);
        var wardrobe = new Wardrobe();
        wardrobe.Restore(old.Outfit, old.Wardrobe);
        Assert.True(wardrobe.Worn.IsOwn);
    }

    // ------------------------------------------------------------------ the screen

    private static (WardrobeScreen Screen, Wardrobe Wardrobe) Boutique()
    {
        var wardrobe = new Wardrobe();
        var screen = new WardrobeScreen();
        screen.Open(wardrobe, ClothingDatabase.Stock("jubilife"), "Jubilife Boutique");
        return (screen, wardrobe);
    }

    [Fact]
    public void TheBoutiqueListsItsStockAndTheLooksOwnFirst()
    {
        var (screen, _) = Boutique();
        Assert.True(screen.IsBoutique);
        Assert.Equal("JUBILIFE BOUTIQUE", screen.Name);
        Assert.Equal(ClothingSlot.Hat, screen.Slot);
        Assert.Null(screen.Rows[0]);
        Assert.Equal(0, screen.SelectedIndex);
        Assert.Equal(ClothingDatabase.All.Where(g => g.Slot == ClothingSlot.Hat).Select(g => g.Id), screen.Rows.Skip(1));
        Assert.True(screen.Trial.IsOwn);

        screen.Move(1);
        Assert.Equal(screen.Rows[1], screen.Trial.Hat);
        // Left and right go through the slots, wrapping
        screen.MoveTab(-1);
        Assert.Equal(ClothingSlot.Bag, screen.Slot);
        screen.MoveTab(1);
        screen.MoveTab(1);
        Assert.Equal(ClothingSlot.Top, screen.Slot);
        Assert.True(screen.Trial.IsOwn);
    }

    [Fact]
    public void AGarmentIsBoughtWithAYesAndPutOn()
    {
        var (screen, wardrobe) = Boutique();
        var notices = new List<string>();
        int money = 1000;
        while (screen.Selected?.Id != "red_cap") screen.Move(1);

        Assert.Equal(0, screen.Confirm(money, notices.Add));
        Assert.True(screen.Asking);
        // NO: nothing bought
        screen.Move(1);
        Assert.Equal(0, screen.Confirm(money, notices.Add));
        Assert.False(screen.Asking);
        Assert.False(wardrobe.Owns("red_cap"));

        screen.Confirm(money, notices.Add);
        money += screen.Confirm(money, notices.Add);
        Assert.Equal(1000 - ClothingDatabase.Get("red_cap")!.Price, money);
        Assert.Equal("red_cap", wardrobe.Worn.Hat);
        Assert.Equal(1, screen.Bought);

        // Owned now: chosen again, it is worn for nothing and nothing is asked
        int capRow = screen.SelectedIndex;
        while (screen.SelectedIndex != 0) screen.Move(-1);
        screen.Confirm(money, notices.Add);
        Assert.Null(wardrobe.Worn.Hat);
        while (screen.SelectedIndex != capRow) screen.Move(1);
        Assert.Equal(0, screen.Confirm(money, notices.Add));
        Assert.False(screen.Asking);
        Assert.Equal("red_cap", wardrobe.Worn.Hat);
    }

    [Fact]
    public void WhatTheMoneyDoesntReachIsntAsked()
    {
        var (screen, wardrobe) = Boutique();
        var notices = new List<string>();
        while (screen.Selected?.Id != "navy_beret") screen.Move(1);
        Assert.Equal(0, screen.Confirm(10, notices.Add));
        Assert.False(screen.Asking);
        Assert.Equal("You don't have enough money.", Assert.Single(notices));
        Assert.True(wardrobe.Worn.IsOwn);
    }

    [Fact]
    public void AtHomeTheWardrobeHoldsOnlyWhatIsOwned()
    {
        var wardrobe = new Wardrobe();
        wardrobe.Give("black_cap");
        var screen = new WardrobeScreen();
        screen.Open(wardrobe);
        Assert.False(screen.IsBoutique);
        Assert.Equal("WARDROBE", screen.Name);
        Assert.Equal(new[] { null, "no_hat", "black_cap" }, screen.Rows);
        screen.Move(-1);
        screen.Confirm(0, _ => { });
        Assert.Equal("black_cap", wardrobe.Worn.Hat);

        // The cursor opens on what is worn
        screen.Cancel();
        Assert.False(screen.IsActive);
        screen.Open(wardrobe);
        Assert.Equal("black_cap", screen.Rows[screen.SelectedIndex]);
    }

    [Fact]
    public void TheListShowsAsManyRowsAsItsPanelHolds()
    {
        // The slot's list sits under the tabs, as the bag's pocket does
        const float height = ModernUi.ContentBottom - (ModernUi.ContentTop + 76 + 24);
        Assert.Equal(WardrobeScreen.VisibleRows, ModernUi.RowsIn(height));
    }

    // ------------------------------------------------------------------ the scripts

    [Fact]
    public void TheBoutiquesClerkOpensItsStock()
    {
        var library = ScriptLibrary.Default;
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(library, host);
        runner.Start(library.In("JubilifeBoutique", "Clerk")!);
        runner.RunToEnd();
        Assert.Contains("open Wardrobe jubilife", host.Log);

        var home = new HeadlessScriptHost();
        var atHome = new ScriptRunner(library, home);
        atHome.Start(library.In("PlayerHouse2F", "Wardrobe")!, own: new[] { "It's your wardrobe." });
        atHome.RunToEnd();
        Assert.Contains("open Wardrobe", home.Log);
    }

    [Fact]
    public void AnUnknownBoutiqueIsAProblem()
    {
        var library = ScriptLibrary.FromSources(("test", """
            script Bad
              wardrobe "nowhere"
              end
            """));
        Assert.Contains(library.Problems(), p => p.Contains("no boutique 'nowhere'"));
    }
}
