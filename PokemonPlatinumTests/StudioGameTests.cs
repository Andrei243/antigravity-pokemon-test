using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 42 · PS4: a take shot in the whole game with no window, from Director Wren's desk in Jubilife TV's lobby to the
/// field again: the game's own <c>StartStudioFilm</c> and its end, the script hearing the stars, the team untouched.
/// </summary>
[Collection("Shared clocks and dice")]
public class StudioGameTests
{
    private const float Frame = 1f / 60f;

    [Fact]
    public void ATakeIsShotFromTheDeskAndTheFieldComesBack()
    {
        string folder = Path.Combine(Path.GetTempPath(), "studio-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(12);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.PeopleStayPut = true;
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            var lead = new Pokemon(PokemonDatabase.Get("Turtwig")!, 20);
            game.Party.Add(lead);
            game.State = GameState.Overworld;

            var lobby = MapDatabase.Get("JubilifeTV1F");
            Assert.Contains(lobby.NPCs, n => n.Script == "StudioDesk" && (n.GridX, n.GridY) == (3, 7));
            game.Place(lobby, 3, 8, Direction.Up);
            game.TrainersLookOnArrival = false;
            game.Interact();
            Assert.True(engine.ScriptRunning);
            bool fought = false;
            for (int f = 0; f < 60 * 240; f++)
            {
                if (game.State == GameState.Battle) fought = true;
                if (fought && game.State == GameState.Overworld && !engine.ScriptRunning) break;
                InputManager.Feed(f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
                engine.Update(Frame);
            }
            Assert.True(fought, "no take was shot");
            Assert.Equal(GameState.Overworld, game.State);
            Assert.False(engine.ScriptRunning);
            Assert.Equal("JubilifeTV1F", game.Map.Name);
            // Confirm all the way: the first film, the script's line, the first move: the ghost moved on, four stars
            var save = game.BuildSave().Studio;
            Assert.NotNull(save);
            Assert.Equal(1, save!.Shot["emptySeat"]);
            Assert.Equal(4, save.Best["emptySeat"]);
            // Nothing of the take touched the team
            Assert.Equal(new[] { lead }, game.Party.Members);
            Assert.Equal(20, lead.Level);
            Assert.Equal(lead.MaxHP, lead.CurrentHP);
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
