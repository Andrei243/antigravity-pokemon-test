using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 43 · LF7: a home that goes with the player. The room goes with its door (a spot of another region opens into the
/// room as it was), MOVE IN HERE takes the spot's own room and keeps the room left as it was, the spots of ours read from
/// the overlays, and Johto's Mom keeping a share of a prize and spending it.
/// </summary>
[Collection("Shared clocks and dice")]
public class HomeAcrossRegionsTests
{
    private static List<StoredDecoration> Store(params string[] names)
    {
        var store = new List<StoredDecoration>();
        foreach (string n in names) Assert.True(SecretBases.Add(store, DecorationDatabase.Get(n)!.Id), n);
        return store;
    }

    [Fact]
    public void TheRoomGoesWithItsDoorAndEveryPieceStaysWhereItWas()
    {
        var store = Store("Small Desk", "Pikachu Doll");
        var home = SecretBases.Open(null, store, 161, "Hoenn", 10, 10, 10, 11);
        home.Placed.Add(new PlacedDecoration { Slot = 0, X = 2, Y = 3 });
        home.Placed.Add(new PlacedDecoration { Slot = 1, X = 4, Y = 3 });
        store[0].Out = store[1].Out = true;
        // A tree in Sinnoh: the door moves, the room and its pieces don't
        SecretBases.MoveDoor(home, 169 + 10, "Sinnoh", 120, 880, 120, 881);
        Assert.Equal(161, home.Id);
        Assert.Equal("Sinnoh", home.Map);
        Assert.Equal((120, 880), (home.X, home.Y));
        Assert.Equal(2, home.Placed.Count);
        Assert.Equal(179, home.SpotRoom);
        // Through a save
        var back = SaveManager.Read(SaveManager.Write(new SaveData { SecretBase = home, Decorations = store })!)!;
        Assert.Equal(161, back.SecretBase!.Id);
        Assert.Equal(179, back.SecretBase.DoorRoom);
        Assert.Equal(2, back.SecretBase.Placed.Count);
        // An older save, written before the door could move, opens at its own room
        Assert.Equal(161, new SecretBaseSave { Id = 161 }.SpotRoom);
    }

    [Fact]
    public void MoveInHereTakesTheSpotsRoomAndKeepsTheRoomLeftAsItWas()
    {
        var store = Store("Small Desk", "Pikachu Doll");
        var home = SecretBases.Open(null, store, 161, "Hoenn", 10, 10, 10, 11);
        home.Placed.Add(new PlacedDecoration { Slot = 0, X = 2, Y = 3 });
        store[0].Out = true;
        // Nothing to move into while the door's room is the home's own
        Assert.False(SecretBases.MoveIn(home, store));
        SecretBases.MoveDoor(home, 9, "Kalos", 50, 50, 50, 51);
        Assert.True(SecretBases.MoveIn(home, store));
        Assert.Equal(9, home.Id);
        Assert.Empty(home.Placed);
        Assert.False(store[0].Out);
        Assert.Single(home.Layouts![161]);
        // Back at a spot of the first room, and moved in again: laid as it was left
        SecretBases.MoveDoor(home, 161, "Hoenn", 10, 10, 10, 11);
        Assert.True(SecretBases.MoveIn(home, store));
        Assert.Equal(161, home.Id);
        var desk = Assert.Single(home.Placed);
        Assert.Equal((2, 3), (desk.X, desk.Y));
        Assert.True(store[0].Out);
    }

    [Fact]
    public void ASpotOfOursNamesARoomOfItsKind()
    {
        Assert.Equal(9, new PokemonPlatinumEngine.Data.OverlayBaseSpot { Kind = "RedCave", Room = 1 }.Id);
        Assert.Equal(SecretBaseKind.Tree, SecretBaseLayouts.Of(new PokemonPlatinumEngine.Data.OverlayBaseSpot { Kind = "Tree", Room = 2 }.Id)!.Kind);
        Assert.Equal(0, new PokemonPlatinumEngine.Data.OverlayBaseSpot { Kind = "Igloo" }.Id);
        Assert.Equal(0, new PokemonPlatinumEngine.Data.OverlayBaseSpot { Kind = "Shrub", Room = 5 }.Id);
        Assert.True(SecretBases.HasSpots(RegionDatabase.Kalos));
        Assert.False(SecretBases.HasSpots(RegionDatabase.Galar));
    }

    [Fact]
    public void MomKeepsAQuarterOfAPrizeInJohtoOnlyWhenAsked()
    {
        var story = new StoryState();
        Assert.Equal(0, MomSavings.ShareOf(1000, RegionDatabase.Johto, story));
        story.Set(MomSavings.SavingFlag);
        Assert.Equal(250, MomSavings.ShareOf(1000, RegionDatabase.Johto, story));
        Assert.Equal(0, MomSavings.ShareOf(1000, RegionDatabase.Kanto, story));
        MomSavings.Keep(story, 250);
        MomSavings.Keep(story, 250);
        Assert.Equal(500, story.Var(MomSavings.Var));
        story.SetVar(MomSavings.Var, MomSavings.Most - 10);
        Assert.Equal(10, MomSavings.ShareOf(1000, RegionDatabase.Johto, story));
    }

    [Fact]
    public void MomAsksGivesTheSavingsBackWholeAndSaysWhatSheBought()
    {
        var library = ScriptLibrary.Default;
        // Asked once: yes
        var host = new HeadlessScriptHost { Region = RegionDatabase.Johto };
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find("MomSavings", "new_bark_player_house_1f")!);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.True(host.Story.Has(MomSavings.SavingFlag));
        // With savings: back whole
        host.Story.SetVar(MomSavings.Var, 1234);
        host.Money = 100;
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        runner = new ScriptRunner(library, host);
        runner.Start(library.Find("MomSavings", "new_bark_player_house_1f")!);
        runner.RunToEnd();
        Assert.Equal(1334, host.Money);
        Assert.Equal(0, host.Story.Var(MomSavings.Var));
        // Her buy leaves the savings short by its price, and the call says what
        for (int roll = 0; roll < 4; roll++)
        {
            var call = new HeadlessScriptHost { Region = RegionDatabase.Johto };
            call.Story.SetVar(MomSavings.Var, 1500);
            call.Rolls.Enqueue(roll);
            var shops = new ScriptRunner(library, call);
            shops.Start(library.Find("MomShops", GameEngine.PhoneScripts)!);
            shops.RunToEnd();
            Assert.Empty(call.Problems);
            int left = call.Story.Var(MomSavings.Var);
            Assert.Equal(roll switch { 0 => 800, 1 => 500, 2 => 1150, _ => 1500 }, left);
            if (roll < 3) Assert.NotEmpty(call.Transcript);
        }
        // Too little saved: nothing bought and nothing drawn
        var poor = new HeadlessScriptHost { Region = RegionDatabase.Johto };
        poor.Story.SetVar(MomSavings.Var, 100);
        var none = new ScriptRunner(library, poor);
        none.Start(library.Find("MomShops", GameEngine.PhoneScripts)!);
        none.RunToEnd();
        Assert.Equal(100, poor.Story.Var(MomSavings.Var));
        Assert.Empty(poor.Transcript);
    }
}
