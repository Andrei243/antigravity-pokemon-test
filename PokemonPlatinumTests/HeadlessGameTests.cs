using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T12: the whole game with no window. A new game, steps through the engine's own steering, a wild
/// battle on seeded dice, a warp's fade over frames, the nurse spoken to, a save in a folder of the test's own and
/// the game continued from it: every frame the engine's own <see cref="GameEngine.Update"/>, fed its actions as
/// frames, and nothing drawn. It shares the dice, the save folder and the databases with the whole process, so it
/// runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class HeadlessGameTests
{
    private const float Frame = 1f / 60f;

    [Fact]
    public void ANewGameIsPlayedSavedAndContinuedWithNoWindow()
    {
        string folder = Path.Combine(Path.GetTempPath(), "headless-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        var heard = AudioManager.Listen();
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(12);

            var engine = Headless();
            var game = engine.Drive;
            Assert.True(engine.IsHeadless);
            Assert.Equal(GameState.Title, game.State);

            // A new game begins in the player's room with nothing (plan 02 · S4); it is taken past the first chapter as
            // the harness takes it (OpeningTests plays the chapter itself)
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            Assert.Equal(0, game.Party.Count);
            Assert.Same(Ruleset.Modern, Ruleset.Current);
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            var lead = new Pokemon(PokemonDatabase.Get("Turtwig")!, 20);
            game.Party.Add(lead);
            game.Bag.AddItem(ItemDatabase.Get("Poké Ball")!, 10);
            game.State = GameState.Overworld;

            // Out on Route 201's grass, walked back and forth by the engine's own steps until something jumps out
            var sinnoh = MapDatabase.Get("Sinnoh");
            var (gx, gy) = GrassPair(sinnoh, 116, 886);
            game.Place(sinnoh, gx, gy, Direction.Right);
            game.TrainersLookOnArrival = false;
            int steps = 0;
            for (int f = 0; f < 60 * 600 && game.State == GameState.Overworld; f++)
            {
                engine.Steering = (game.Player.GridX == gx ? Direction.Right : Direction.Left, false);
                Run(engine, ActionFrame.None);
                if (game.Player.GridX != gx && game.Player.GridX != gx + 1) Assert.Fail($"The walk left the grass at {game.Player.GridX},{game.Player.GridY}.");
                steps++;
            }
            engine.Steering = null;
            Assert.NotEqual(GameState.Overworld, game.State);
            Until(engine, () => game.State == GameState.Battle, "the battle");

            // The battle, won with the lead's first damaging move (which the newest learnset puts where it puts it,
            // plan 06 · R19), its lines read on with the confirm button as frames
            var battle = game.Battle!;
            string foe = battle.EnemyPokemon.Species.Name;
            int hit = lead.Moves.FindIndex(m => m.Data.Category != MoveCategory.Status);
            Until(engine, () => battle.IsBattleOver || battle.HUD.MenuState == BattleMenuState.Main, "the battle's menu", press: true);
            for (int guard = 0; guard < 60 * 120 && !battle.IsBattleOver; guard++)
            {
                if (battle.HUD.MenuState == BattleMenuState.Main) battle.SelectMove(hit);
                Run(engine, guard % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
            }
            Assert.True(battle.IsBattleOver, $"The battle with {foe} never ended.");
            Until(engine, () => game.State == GameState.Overworld, "the field after the battle", press: true);
            Assert.False(lead.IsFainted);
            Assert.Contains(heard, s => s.StartsWith("cry "));

            // Into Sandgem Town's Pokémon Center by its door: a warp's fade closes, the map changes at its midpoint and
            // it opens on the room
            var door = sinnoh.Warps.FirstOrDefault(w => w.TargetMap == "PokemonCenter")
                ?? throw new InvalidOperationException("Sandgem Town's Pokémon Center has no door on the map of Sinnoh.");
            game.Place(sinnoh, door.SourceX, door.SourceY + 1, Direction.Up);
            engine.Steering = (Direction.Up, false);
            Until(engine, () => game.State == GameState.Transition, "the door's fade");
            engine.Steering = null;
            Until(engine, () => game.State == GameState.Overworld, "the room");
            Assert.Equal("PokemonCenter", game.Map.Name);

            // The nurse, spoken to from across her counter, heals the team: her lines and her question answered with
            // the confirm button
            var nurse = game.Map.NPCs.First(n => n.IsHealingNurse);
            game.Place(game.Map, nurse.GridX, nurse.GridY + 2, Direction.Up);
            lead.CurrentHP = 1;
            game.Interact();
            Until(engine, () => game.State == GameState.Overworld && !engine.ScriptRunning, "the nurse's end", press: true);
            Assert.Equal(lead.MaxHP, lead.CurrentHP);

            // Saved into the test's folder, and continued by a game started afresh on the same tile
            var (x, y) = (game.Player.GridX, game.Player.GridY);
            Assert.True(game.Save());
            Assert.True(File.Exists(Path.Combine(folder, SavePaths.SlotName(game.SaveSlot))));
            int slot = game.SaveSlot;

            var again = Headless();
            Assert.Equal(GameState.Title, again.Drive.State);
            again.ContinueGame(slot);
            Until(again, () => again.Drive.State == GameState.Overworld, "the continued game");
            Assert.Equal("PokemonCenter", again.Drive.Map.Name);
            Assert.Equal((x, y), (again.Drive.Player.GridX, again.Drive.Player.GridY));
            Assert.Equal("Turtwig", again.Drive.Party.Members[0].Species.Name);
            Assert.Equal(lead.Level, again.Drive.Party.Members[0].Level);
            Assert.True(steps > 0);
        }
        finally
        {
            InputManager.Feed(null);
            AudioManager.StopListening();
            Dice.Seed(null);
            // The game put the modern rules in force (plan 06 · R19); the rest of the run reads Platinum's baseline, as every
            // test does that hands its battle no rules of its own
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void AGameWithNoWindowRefusesToDraw()
    {
        var engine = new GameEngine(new GameSettings());
        engine.InitializeHeadless();
        Assert.Throws<InvalidOperationException>(engine.Draw);
        engine.Close();
    }

    private static GameEngine Headless()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        return engine;
    }

    // A frame of the game, its actions handed over as the keys would be
    private static void Run(GameEngine engine, ActionFrame frame)
    {
        InputManager.Feed(frame);
        engine.Update(Frame);
    }

    // Frames until the condition holds, pressing confirm every few if asked (a line read on, a question's first answer)
    private static void Until(GameEngine engine, Func<bool> done, string what, bool press = false, int most = 60 * 120)
    {
        for (int f = 0; f < most; f++)
        {
            if (done()) return;
            Run(engine, press && f % 8 == 0 ? ActionFrame.Press(GameAction.Confirm) : ActionFrame.None);
        }
        Assert.Fail($"Waited {most / 60} seconds of game time for {what}; the game is in {engine.Drive.State}.");
    }

    // The tall grass nearest a spot with grass on its east too, both open to walk on
    private static (int X, int Y) GrassPair(Map map, int x, int y)
    {
        for (int r = 1; r < 80; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    int gx = x + dx, gy = y + dy;
                    if (map.IsTallGrass(gx, gy) && map.IsTallGrass(gx + 1, gy) && map.IsWalkable(gx, gy) && map.IsWalkable(gx + 1, gy))
                        return (gx, gy);
                }
        throw new InvalidOperationException("No tall grass near Twinleaf Town.");
    }
}
