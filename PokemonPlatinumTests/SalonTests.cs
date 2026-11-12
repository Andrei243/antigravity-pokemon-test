using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 43 · LF3: the salon and the dye house. The outfit's name grows its salon part only once the salon is used; a
/// cut, a colour, eyes and a tone change only what they name; a dye changes only its garment's colours and costs its
/// berry and fee; a save without the new fields loads as before; every cut is one body with a face on both kits.
/// </summary>
[Collection("Shared clocks and dice")]
public class SalonTests
{
    [Fact]
    public void NothingChosenKeepsTodaysNames()
    {
        Assert.Equal("PLAYER@red_cap.-.-.-.-", Outfit.Dress("PLAYER", Outfit.Own.With(ClothingSlot.Hat, "red_cap")));
        Assert.Equal("PLAYER", Outfit.Dress("PLAYER", Outfit.Own));
        Assert.Equal(Outfit.Own.With(ClothingSlot.Hat, "red_cap"), Outfit.FromKey("red_cap.-.-.-.-"));
        // An older save's outfit has no salon fields, and reads as before
        var old = System.Text.Json.JsonSerializer.Deserialize<Outfit>("""{"Hat":"red_cap","Top":null,"Bottoms":null,"Shoes":null,"Bag":null}""")!;
        Assert.False(old.IsRestyled);
        Assert.Equal("red_cap.-.-.-.-", old.Key);
        // ...and an outfit with nothing of the salon writes none of it
        Assert.DoesNotContain("Hair", System.Text.Json.JsonSerializer.Serialize(Outfit.Own.With(ClothingSlot.Hat, "red_cap")));
    }

    [Fact]
    public void TheSalonsPartRidesInTheKeyAndBack()
    {
        var restyled = Outfit.Own with { Hair = "Bun", HairColour = "copper" };
        Assert.False(restyled.IsOwn);
        Assert.Equal("-.-.-.-.-.Bun,copper,-,-", restyled.Key);
        Assert.Equal(restyled, Outfit.FromKey(restyled.Key));
        string name = Outfit.Dress("DAWN", restyled.With(ClothingSlot.Top, "striped_tee~mint"));
        var (look, outfit) = Outfit.Undress(name);
        Assert.Equal("DAWN", look);
        Assert.Equal("striped_tee~mint", outfit!.Top);
        Assert.Equal("Bun", outfit.Hair);
    }

    [Fact]
    public void ACutAndAColourChangeOnlyTheHair()
    {
        var plain = CharacterStyle.For("PLAYER");
        var styled = CharacterStyle.For(Outfit.Dress("PLAYER", Outfit.Own with { Hair = "Ponytail", HairColour = "teal", Eyes = "green", Skin = "deep" }));
        Assert.Equal(HairCut.Ponytail, styled.Hair);
        Assert.NotEqual(plain.HairColor, styled.HairColor);
        Assert.NotEqual(plain.Eyes, styled.Eyes);
        Assert.NotEqual(plain.Skin, styled.Skin);
        Assert.Equal(plain.Top, styled.Top);
        Assert.Equal(plain.Bottom, styled.Bottom);
        Assert.Equal(plain.Hat, styled.Hat);
        // A part the data doesn't know is the look's own
        var unknown = CharacterStyle.For(Outfit.Dress("PLAYER", Outfit.Own with { Hair = "Mohawk", HairColour = "gold" }));
        Assert.Equal(plain.Hair, unknown.Hair);
        Assert.Equal(plain.HairColor, unknown.HairColor);
    }

    [Fact]
    public void TheSalonsListsAreWholeAndNamedInBothLanguages()
    {
        Assert.Equal(Enum.GetValues<HairCut>().Select(c => c.ToString()), ClothingDatabase.Cuts);
        Assert.Equal(8, ClothingDatabase.HairColours.Count);
        Assert.Equal(6, ClothingDatabase.EyeColours.Count);
        Assert.Equal(6, ClothingDatabase.SkinTones.Count);
        Assert.Equal(16, ClothingDatabase.Dyes.Count);
        Assert.False(ClothingDatabase.DyeOf("teal")!.Sold);
        foreach (var dye in ClothingDatabase.Dyes) Assert.NotNull(ItemDatabase.Get(dye.Berry));
        string language = Text.Current;
        try
        {
            foreach (string tag in new[] { "en", "ro" })
            {
                Text.Use(tag);
                foreach (string c in ClothingDatabase.Cuts) Assert.False(Text.Of($"salon.cut.{c}").StartsWith("salon.", StringComparison.Ordinal), $"{tag} {c}");
                foreach (var c in ClothingDatabase.HairColours) Assert.False(Text.Of($"salon.hairColour.{c.Id}").StartsWith("salon.", StringComparison.Ordinal));
                foreach (var c in ClothingDatabase.EyeColours) Assert.False(Text.Of($"salon.eyeColour.{c.Id}").StartsWith("salon.", StringComparison.Ordinal));
                foreach (var c in ClothingDatabase.SkinTones) Assert.False(Text.Of($"salon.skinTone.{c.Id}").StartsWith("salon.", StringComparison.Ordinal));
                foreach (var d in ClothingDatabase.Dyes) Assert.False(Text.Of($"salon.dye.{d.Id}").StartsWith("salon.", StringComparison.Ordinal));
            }
        }
        finally { Text.Use(language); }
    }

    [Fact]
    public void ADyedGarmentIsItsGarmentInTheDyesColours()
    {
        var plain = ClothingDatabase.Get("striped_tee")!;
        var dyed = ClothingDatabase.Get("striped_tee~mint")!;
        Assert.Equal(plain.Slot, dyed.Slot);
        Assert.Equal(plain.Stripes, dyed.Stripes);
        Assert.Equal(ClothingDatabase.DyeOf("mint")!.Color, dyed.Color);
        Assert.Null(ClothingDatabase.Get("no_hat~mint"));
        Assert.Null(ClothingDatabase.Get("striped_tee~gold"));
        // No dye's name is a build or a held thing, which a character's name could read it as
        foreach (var d in ClothingDatabase.Dyes)
        {
            string name = Outfit.Dress("PLAYER", Outfit.Own.With(ClothingSlot.Bag, "blue_backpack~" + d.Id));
            var asked = CharacterStyle.For(CharacterStyle.Holding(name + "~Teen", HeldProp.Rod));
            Assert.Equal(BodyBuild.Teen, asked.Build);
            Assert.Equal(HeldProp.Rod, asked.Holds);
            Assert.NotNull(asked.Bag);
        }
    }

    [Fact]
    public void TheDyeHouseTakesTheBerryAndTheFeeAndTheDyedOneIsOwnedAndWorn()
    {
        var wardrobe = new Wardrobe();
        Assert.Null(wardrobe.Dye("striped_tee", "mint"));
        wardrobe.Give("striped_tee");
        var screen = new SalonScreen();
        var bag = new HashSet<string> { "Lum Berry" };
        screen.OpenDye(wardrobe, bag.Contains);
        Assert.Contains("striped_tee", screen.Rows);
        while (screen.Selected != "striped_tee") screen.Move(1);
        Assert.Equal(0, screen.Confirm(5000, _ => { }));
        Assert.Equal(DyeStep.Dye, screen.Step);
        while (screen.Selected != "mint") screen.Move(1);
        Assert.Equal(ClothingDatabase.DyeOf("mint")!.Color, ClothingDatabase.Get(screen.Trial.Top)!.Color);
        screen.Confirm(5000, _ => { });
        Assert.True(screen.Asking);
        int paid = screen.Confirm(5000, _ => { });
        Assert.Equal(-ClothingDatabase.Prices.Dye, paid);
        Assert.Equal("Lum Berry", screen.TakeBerry());
        Assert.True(wardrobe.Owns("striped_tee~mint"));
        Assert.True(wardrobe.Owns("striped_tee"));
        Assert.Equal("striped_tee~mint", wardrobe.Worn.Top);
        // With no berry, nothing is asked or paid
        while (screen.Selected != "striped_tee") screen.Move(1);
        screen.Confirm(5000, _ => { });
        while (screen.Selected != "cherry") screen.Move(1);
        string? said = null;
        Assert.Equal(0, screen.Confirm(5000, s => said = s));
        Assert.False(screen.Asking);
        Assert.NotNull(said);
        // The wardrobe lists the dyed one after its plain one
        var w = new WardrobeScreen();
        w.Open(wardrobe);
        w.MoveTab(1);
        int at = w.Rows.ToList().IndexOf("striped_tee");
        Assert.Equal("striped_tee~mint", w.Rows[at + 1]);
    }

    [Fact]
    public void TheChairChangesTheLookAndAWardrobeRestoresIt()
    {
        var wardrobe = new Wardrobe();
        var screen = new SalonScreen();
        screen.OpenSalon(wardrobe);
        while (screen.Selected != "Bun") screen.Move(1);
        Assert.Equal("Bun", screen.Trial.Hair);
        screen.Confirm(10000, _ => { });
        Assert.Equal(-ClothingDatabase.Prices.Cut, screen.Confirm(10000, _ => { }));
        Assert.Equal("Bun", wardrobe.Worn.Hair);
        // What is worn is free to choose again: nothing is asked
        screen.Confirm(10000, _ => { });
        Assert.False(screen.Asking);
        screen.MoveTab(1);
        while (screen.Selected != "copper") screen.Move(1);
        screen.Confirm(10000, _ => { });
        screen.Confirm(10000, _ => { });
        Assert.Equal("copper", wardrobe.Worn.HairColour);
        // Not enough money: nothing
        screen.MoveTab(1);
        screen.Move(1);
        Assert.Equal(0, screen.Confirm(10, _ => { }));
        Assert.False(screen.Asking);

        var back = new Wardrobe();
        back.Restore(wardrobe.Worn, wardrobe.Owned);
        Assert.Equal(wardrobe.Worn, back.Worn);
    }

    [Fact]
    public void TheStylistAndTheDyersScriptsOpenTheirCounters()
    {
        foreach (var (file, script, counter) in new[] { ("JubilifeBoutique", "Stylist", "salon"), ("JubilifeBoutique", "Dyer", "dye:jubilife"), ("LumioseCity_Boutique", "Dyer", "dye:lumiose") })
        {
            var host = new HeadlessScriptHost();
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(script, file)!);
            runner.RunToEnd();
            Assert.Contains(host.Log, l => l == $"open Wardrobe {counter}");
        }
        Assert.DoesNotContain(ScriptLibrary.Default.Problems(), p => p.Contains("boutique", StringComparison.Ordinal));
    }

    public static IEnumerable<object[]> Cuts => Enum.GetValues<HairCut>().SelectMany(c => new[] { new object[] { c.ToString(), false }, new object[] { c.ToString(), true } });

    [Theory]
    [MemberData(nameof(Cuts))]
    public void EveryCutIsOneBodyWithAFaceOnBothKits(string cut, bool grown)
    {
        string name = Outfit.Dress("PLAYER", Outfit.Own with { Hair = cut, HairColour = "copper", Skin = "tan" });
        if (grown) name = CharacterStyle.InKit(name, CharacterKit.Grown);
        var rig = CharacterModels.Build(name);
        Assert.InRange(rig.Mesh.VertexCount, 3000, ushort.MaxValue);
        Assert.NotNull(rig.FacePatch);
        Assert.True(rig.FacePatch!.TriangleCount > 100, $"{name}: a face patch of {rig.FacePatch.TriangleCount} triangles");
        Assert.Equal(cut, rig.Style.Hair.ToString());
    }
}
