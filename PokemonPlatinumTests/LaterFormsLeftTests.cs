using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// What R29 left of the later forms (plan 06 · R29b): the Rotom Catalog's and the Zygarde Cube's lists, the Cube's
/// moves, a Giratina of the team in the Distortion World, Furfrou's trim in a box, and the names Showdown gives some
/// forms otherwise. One rule to a test; docs/mechanics/rulings.md, "What R29 left", has the rulings.
/// </summary>
public class LaterFormsLeftTests
{
    private static ItemData Item(string name) => ItemDatabase.Get(name) ?? throw new ArgumentException(name);

    private static Party Team(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    private static string[] MovesOf(Pokemon p) => p.Moves.Select(m => m.Name).ToArray();

    // ------------------------------------------------------------------ the Rotom Catalog

    [Fact]
    public void TheRotomCatalogListsTheFiveAppliancesAndTheBulb()
    {
        var rotom = Mon("Rotom", 30, "Thunder Shock");
        var rows = FormChanges.OptionsFor(Item("Rotom Catalog"), rotom);
        Assert.NotNull(rows);
        Assert.Equal(6, rows!.Count);
        Assert.Null(FormChanges.OptionsFor(Item("Rotom Catalog"), Mon("Pikachu", 30)));
    }

    [Fact]
    public void ARotomGoesStraightIntoTheApplianceChosenWithItsMove()
    {
        var rotom = Mon("Rotom", 30, "Thunder Shock", "Astonish");
        var catalog = Item("Rotom Catalog");

        // The refrigerator, the third row, without passing through the oven and the washing machine
        Assert.True(FormChanges.Choose(catalog, rotom, null, 2).Applied);
        Assert.Equal("Rotom-Frost", rotom.Form);
        Assert.Equal(new[] { "Thunder Shock", "Astonish", "Blizzard" }, MovesOf(rotom));

        // The lawn mower: Leaf Storm takes Blizzard's place
        FormChanges.Choose(catalog, rotom, null, 4);
        Assert.Equal("Rotom-Mow", rotom.Form);
        Assert.Equal(new[] { "Thunder Shock", "Astonish", "Leaf Storm" }, MovesOf(rotom));

        // The appliance it is in already changes nothing
        Assert.False(FormChanges.Choose(catalog, rotom, null, 4).Applied);

        // The bulb, the last row: its own form, the appliance's move forgotten
        FormChanges.Choose(catalog, rotom, null, 5);
        Assert.Null(rotom.Form);
        Assert.Equal(new[] { "Thunder Shock", "Astonish" }, MovesOf(rotom));
        Assert.False(FormChanges.Choose(catalog, rotom, null, 5).Applied);
    }

    [Fact]
    public void TheBagListsTheCatalogAndAsksAboutAMoveThatDoesntFit()
    {
        var rotom = Mon("Rotom", 30, "Thunder Shock", "Astonish", "Double Team", "Confuse Ray");
        var party = Team(rotom);
        var inventory = new Inventory();
        inventory.AddItem(Item("Rotom Catalog"), 1);
        var bag = new BagScreen();
        bag.Open();
        var notes = new List<string>();
        bag.BeginTargetChoice(Item("Rotom Catalog"), give: false);
        bag.UseOnTarget(inventory, party, notes.Add, new EvolutionContext { Party = party, Bag = inventory });

        // The list is up and nothing has changed yet
        Assert.NotNull(bag.Options);
        Assert.Null(rotom.Form);
        bag.MoveOption(1);
        bag.ChooseOption(party, notes.Add);

        // The washing machine; Hydro Pump wants a place among four moves, so the bag asks which to forget
        Assert.Equal("Rotom-Wash", rotom.Form);
        Assert.Null(bag.Options);
        Assert.Equal((rotom, "Hydro Pump"), (bag.Teaching!.Value.Pokemon, bag.Teaching!.Value.Move));
        Assert.Equal(1, inventory.GetQuantity(Item("Rotom Catalog")));
    }

    // ------------------------------------------------------------------ the Zygarde Cube

    [Fact]
    public void TheZygardeCubeOffersItsFormeAndItsFiveMoves()
    {
        var zygarde = Mon("Zygarde", 60, "Bite");
        var rows = FormChanges.OptionsFor(Item("Zygarde Cube"), zygarde);
        Assert.Equal(1 + FormChanges.CubeMoves.Count, rows!.Count);

        // A Zygarde whose form the Cube can't change (Complete, a shape of battle) is offered the moves alone
        var complete = Mon("Zygarde", 60, "Bite");
        complete.ChangeForm("Zygarde-Complete");
        Assert.Equal(FormChanges.CubeMoves.Count, FormChanges.OptionsFor(Item("Zygarde Cube"), complete)!.Count);
        Assert.True(FormChanges.WouldHelp(Item("Zygarde Cube"), complete, null));
    }

    [Fact]
    public void TheZygardeCubeChangesTheFormeOrTeachesAMove()
    {
        var zygarde = Mon("Zygarde", 60, "Bite");
        var cube = Item("Zygarde Cube");

        Assert.True(FormChanges.Choose(cube, zygarde, null, 0).Applied);
        Assert.Equal("Zygarde-10", zygarde.Form);

        // Thousand Arrows, the first move, learned into a free place; a second time it is known already
        Assert.True(FormChanges.Choose(cube, zygarde, null, 1).Applied);
        Assert.Contains("Thousand Arrows", MovesOf(zygarde));
        Assert.False(FormChanges.Choose(cube, zygarde, null, 1).Applied);
    }

    [Fact]
    public void ACubeMoveThatDoesntFitIsWanted()
    {
        var zygarde = Mon("Zygarde", 60, "Bite", "Dig", "Glare", "Land's Wrath");
        var result = FormChanges.Choose(Item("Zygarde Cube"), zygarde, null, 1 + 4);
        Assert.True(result.Applied);
        Assert.Equal(new[] { "Dragon Dance" }, result.Wanted);
        Assert.DoesNotContain("Dragon Dance", MovesOf(zygarde));
    }

    // ------------------------------------------------------------------ Giratina in the Distortion World

    [Fact]
    public void AGiratinaOfTheTeamTakesItsOriginFormeInTheDistortionWorldAndLeavesItOutside()
    {
        var giratina = Mon("Giratina", 50);
        var dialga = Mon("Dialga", 50);
        var team = new[] { giratina, dialga };

        Assert.True(FormRules.GiratinaHere(team, "DistortionWorldB3F"));
        Assert.Equal("Giratina-Origin", giratina.Form);
        Assert.Null(dialga.Form);
        // After a battle there it keeps it
        Assert.False(FormRules.ByHeldItem(giratina, distortionWorld: true));
        Assert.Equal("Giratina-Origin", giratina.Form);

        Assert.True(FormRules.GiratinaHere(team, "Sinnoh"));
        Assert.Null(giratina.Form);
    }

    [Fact]
    public void AGiratinaWithTheGriseousOrbStaysInItsOriginFormeOutside()
    {
        var giratina = Mon("Giratina", 50);
        giratina.HeldItem = Item(FormRules.GriseousOrb);
        FormRules.GiratinaHere(new[] { giratina }, "DistortionWorldGiratinaRoom");
        FormRules.GiratinaHere(new[] { giratina }, "SendoffSpring");
        Assert.Equal("Giratina-Origin", giratina.Form);
        Assert.True(FormRules.IsDistortionWorld("DistortionWorldTurnbackCaveRoom"));
        Assert.False(FormRules.IsDistortionWorld("TurnbackCave"));
    }

    // ------------------------------------------------------------------ Furfrou

    [Fact]
    public void FurfrousTrimGrowsOutInABox()
    {
        var furfrou = Mon("Furfrou", 30);
        furfrou.ChangeForm("Furfrou-Heart");
        var boxes = new PcBoxes();
        boxes.PutIn(0, 0, furfrou);
        Assert.Null(furfrou.Form);
    }

    // ------------------------------------------------------------------ Showdown's names

    [Fact]
    public void EveryNameShowdownGivesOtherwiseIsOneOfTheData()
    {
        foreach (string ours in ShowdownNames.OursThatDiffer)
            Assert.True(PokemonDatabase.Get(ours) != null || PokemonDatabase.SpeciesOfForm(ours) != null, ours);
    }

    [Theory]
    [InlineData("Meowstic-Female", "Meowstic-F")]
    [InlineData("Necrozma-Dusk", "Necrozma-Dusk-Mane")]
    [InlineData("Minior", "Minior-Meteor")]
    [InlineData("Minior-Blue-Meteor", "Minior-Meteor")]
    [InlineData("Minior-Red", "Minior")]
    [InlineData("Zygarde-10-Power-Construct", "Zygarde-10%")]
    [InlineData("Ogerpon-Wellspring-Mask", "Ogerpon-Wellspring")]
    [InlineData("Garchomp", "Garchomp")]
    public void AFormIsWrittenByShowdownsName(string ours, string theirs) => Assert.Equal(theirs, ShowdownNames.ToShowdown(ours));

    [Theory]
    [InlineData("Meowstic-F", "Meowstic-Female")]
    [InlineData("Necrozma-Dawn-Wings", "Necrozma-Dawn")]
    [InlineData("Minior", "Minior-Red")]
    [InlineData("Zygarde-10%", "Zygarde-10")]
    [InlineData("Pyroar", "Pyroar")]
    [InlineData("Zygarde", "Zygarde")]
    public void ShowdownsNameIsReadBackAsTheDatas(string theirs, string ours) => Assert.Equal(ours, ShowdownNames.FromShowdown(theirs));

    [Fact]
    public void TheExportWritesShowdownsName()
    {
        var necrozma = Mon("Necrozma", 70, "Sunsteel Strike");
        necrozma.ChangeForm("Necrozma-Dusk");
        Assert.StartsWith("Necrozma-Dusk-Mane", ShowdownText.Of(necrozma));
    }
}
