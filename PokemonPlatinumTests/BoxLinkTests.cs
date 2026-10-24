using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 12 · Q13: the start menu's BOXES in the engine — behind the options' row, refused where the map says so, and
/// opening the PC's box screen over the field. Runs alone: it begins a game, which sets the process's rules and save folder.
/// </summary>
[Collection("Shared clocks and dice")]
public class BoxLinkTests
{
    [Fact]
    public void BoxesOpenThePcWhereTheMapAllowsAndRefuseWhereItSaysNo()
    {
        string folder = Path.Combine(Path.GetTempPath(), "boxlink-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(5);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 5));
            game.State = GameState.Overworld;

            // The modern rules give the row; Platinum's keep the menu as it was
            Assert.Same(Ruleset.Modern, Ruleset.Current);
            Assert.True(engine.BoxLinkOn);
            engine.Settings.BoxesAnywhere = RulesDefault.Off;
            Assert.False(engine.BoxLinkOn);
            engine.Settings.BoxesAnywhere = RulesDefault.Rules;

            // Allowed: the PC opens over the field and the menu is gone
            game.StartMenu.HasBoxes = true;
            game.StartMenu.Open();
            game.ChooseFromStartMenu(StartMenuChoice.Boxes);
            Assert.Equal(GameState.PCStorage, game.State);
            Assert.False(game.StartMenu.IsActive);
            game.State = GameState.Overworld;

            // A room that says no: the state stays, and the menu stays up
            game.Map.NoBoxLink = true;
            game.StartMenu.Open();
            game.ChooseFromStartMenu(StartMenuChoice.Boxes);
            Assert.Equal(GameState.Overworld, game.State);
            Assert.True(game.StartMenu.IsActive);
            Assert.False(engine.CanReachBoxes);
            game.Map.NoBoxLink = false;
        }
        finally
        {
            SavePaths.Current = paths;
            Ruleset.Use(RulesPreset.Platinum);
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }
    }

    [Fact]
    public void AMapFileKeepsItsNoBoxLink()
    {
        var file = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "PlayerHouse.json"));
        Assert.False(file.ToMap().NoBoxLink);
        file.NoBoxLink = true;
        var map = file.ToMap();
        Assert.True(map.NoBoxLink);
        Assert.True(MapFile.FromMap(map).NoBoxLink);
    }
}
