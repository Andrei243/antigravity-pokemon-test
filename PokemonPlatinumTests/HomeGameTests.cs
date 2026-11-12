using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 43 · LF7 in the whole game with no window: the bedroom in Twinleaf Town keeps what was set down in it after a
/// save and a continue, laid in the way where it blocks, and the way into the home is drawn at the spot of Sinnoh its
/// door stands at and nowhere else. Shares the dice, the save folder and the databases, so it runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class HomeGameTests
{
    private const float Frame = 1f / 60f;

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    private static void Until(GameEngine engine, Func<bool> done, string what, int most = 60 * 120)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            InputManager.Feed(ActionFrame.None);
            engine.Update(Frame);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }

    [Fact]
    public void TheBedroomKeepsItsPiecesAndTheWayInStandsAtTheHomesSpot()
    {
        string folder = Path.Combine(Path.GetTempPath(), "home-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(43);
            var engine = Headless();
            var game = engine.Drive;
            engine.StartNewGame();
            SecretBases.Add(game.Decorations, DecorationDatabase.IdOf("Pikachu Doll"));
            SecretBases.Add(game.Decorations, DecorationDatabase.IdOf("Ball Poster"));
            game.State = GameState.Overworld;

            // The bedroom: a doll on the floor, laid in the way
            var bedroom = MapDatabase.Get("PlayerHouse2F");
            var floor = Bedrooms.FloorOf(bedroom, RegionDatabase.Sinnoh);
            var at = floor.Floor.Where(t => !floor.KeptClear.Contains(t)).OrderBy(t => t.Item2).ThenBy(t => t.Item1).First();
            game.Bedrooms[RegionDatabase.Sinnoh] = new List<PlacedDecoration> { new() { Slot = 0, X = at.Item1, Y = at.Item2 } };
            Bedrooms.MarkOut(game.Decorations, game.SecretBase, game.Bedrooms);
            game.Place(bedroom, 4, 6, Direction.Up);
            game.LayOutBedroom(bedroom);
            Assert.Contains(bedroom.Props, p => p.Type == PropType.Decoration && (p.X, p.Y) == at);
            Assert.True(bedroom.IsSolid(at.Item1, at.Item2));

            // The home's door on a spot of Sinnoh's: the way in is drawn there as the map is come to
            var sinnoh = MapDatabase.Get("Sinnoh");
            var spot = sinnoh.SecretBaseSpots.First();
            game.SecretBaseSet(new SecretBaseSave { Id = spot.Value, Map = "Sinnoh", X = spot.Key.X, Y = spot.Key.Y, OutX = spot.Key.X, OutY = spot.Key.Y + 1 });
            game.Place(sinnoh, spot.Key.X, spot.Key.Y + 1, Direction.Up);
            game.KeepTheBaseEntrance();
            var entrance = Assert.Single(sinnoh.Props, p => p.Type == PropType.BaseEntrance);
            Assert.Equal((spot.Key.X, spot.Key.Y), (entrance.X, entrance.Y));
            Assert.Equal(SecretBaseLayouts.Of(spot.Value)!.Kind.ToString(), entrance.Model);
            // ... and nowhere once the home has no door
            game.SecretBaseSet(null);
            game.KeepTheBaseEntrance();
            Assert.DoesNotContain(sinnoh.Props, p => p.Type == PropType.BaseEntrance);

            // Saved and continued: the bedroom as it was, its doll out of the PC
            game.Place(bedroom, 4, 6, Direction.Up);
            Assert.True(game.Save());
            int slot = game.SaveSlot;
            var again = Headless();
            again.ContinueGame(slot);
            Until(again, () => again.Drive.State == GameState.Overworld, "the continued game");
            var kept = Assert.Single(again.Drive.Bedrooms[RegionDatabase.Sinnoh]);
            Assert.Equal(at, (kept.X, kept.Y));
            Assert.True(again.Drive.Decorations[0].Out);
            Assert.False(again.Drive.Decorations[1].Out);
            Assert.Contains(again.Drive.Map.Props, p => p.Type == PropType.Decoration && (p.X, p.Y) == at);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
