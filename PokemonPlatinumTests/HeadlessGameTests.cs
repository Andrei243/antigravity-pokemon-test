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
    public void ARegionCrossedIntoMeetsThePlayerAtItsMappedLevels()
    {
        // Plan 18 · W4: a game begun in Sinnoh meets Kanto's wild Pokémon at Kanto's mapped levels (FireRed's 3 is 63:
        // 62 + 42 / 61, rounded), and one begun in Kanto at FireRed's own
        var paths = SavePaths.Current;
        try
        {
            Dice.Seed(12);
            foreach (var (began, expected) in new[] { ("Sinnoh", 63), ("Kanto", 3) })
            {
                var engine = Headless();
                var game = engine.Drive;
                engine.NewGameRegion = began;
                engine.StartNewGame();
                game.Party.Clear();
                game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 70));
                game.State = GameState.Overworld;
                Assert.Equal(began, game.BuildSave().BeganIn);
                // A save carries the build that wrote it (plan 16 · T2)
                Assert.Equal(PokemonPlatinumEngine.Core.Diagnostics.GameVersion.Shown, game.BuildSave().GameVersion);

                game.Place(MapDatabase.Get("Kanto"), 66, 268, Direction.Down);
                game.StartWildBattle(new WildEncounterEntry { SpeciesName = "Rattata", MinLevel = 3, MaxLevel = 3, Weight = 100 });
                Until(engine, () => game.State == GameState.Battle, "the battle");
                Assert.Equal(expected, game.Battle!.EnemyPokemon.Level);
            }
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
        }
    }

    [Fact]
    public void ATrainerOfARegionCrossedIntoIsMetAtTheStageItsLevelAllows()
    {
        // Plan 18 · W4b-2 with plan 03 · D15-A: Route 30's Joey's Rattata of 4 is met at 105 as a game begun in Sinnoh
        // meets Johto, a Raticate and then our Ratchisel; a game begun in Johto meets HeartGold's own Rattata
        var paths = SavePaths.Current;
        try
        {
            Dice.Seed(12);
            foreach (var (began, expected) in new[] { ("Sinnoh", "Ratchisel"), ("Johto", "Rattata") })
            {
                var engine = Headless();
                var game = engine.Drive;
                engine.NewGameRegion = began;
                engine.StartNewGame();
                game.Party.Clear();
                game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 70));
                game.State = GameState.Overworld;
                game.Place(MapDatabase.Get("Johto"), 553, 334, Direction.Up);
                var joey = game.Map.Everyone.First(n => n.Key == "r30_gsboy2");
                game.StartTrainerBattle(joey);
                Until(engine, () => game.State == GameState.Battle, "the battle");
                Assert.Equal(expected, game.Battle!.EnemyPokemon.Species.Name);
                // The trainer of the map keeps HeartGold's team
                Assert.Equal("Rattata", joey.TrainerData!.Party.Members[0].Species.Name);
            }
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
        }
    }

    [Fact]
    public void ARematchInARegionGonePastIsMetAtTheFurthestHallOfFamesLevels()
    {
        // Plan 18 · W4b, L9: Cynthia's rematch (her first Pokémon of 74) in Sinnoh is Platinum's until a Hall of Fame past
        // Sinnoh is entered, then Kanto's 119 and Johto's 149 (Models.RematchLevels; the numbers by the script of the
        // rulings' "Rematches of a region gone past"); a trainer of the place who is no rematch keeps Platinum's level
        var paths = SavePaths.Current;
        try
        {
            Dice.Seed(12);
            foreach (var (flags, cynthia) in new[] { (new string[0], 74), (new[] { "SinnohHallOfFame", "KantoHallOfFame" }, 119), (new[] { "SinnohHallOfFame", "KantoHallOfFame", "JohtoHallOfFame" }, 149) })
            {
                var engine = Headless();
                var game = engine.Drive;
                engine.NewGameRegion = "Sinnoh";
                engine.StartNewGame();
                foreach (var flag in flags) game.Story.Set(flag);
                game.Party.Clear();
                game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 100));
                game.State = GameState.Overworld;
                game.Place(MapDatabase.Get("Sinnoh"), 116, 886, Direction.Right);
                var someone = game.Map.Everyone.First(n => n.IsTrainer && n.TrainerData != null);
                var own = someone.TrainerData!;
                var team = new Trainer { Id = "champion_cynthia_rematch" };
                TrainerDatabase.Fill(team, TrainerDatabase.Get(team.Id)!);
                someone.TrainerData = team;
                game.StartTrainerBattle(someone);
                Until(engine, () => game.State == GameState.Battle, "the battle");
                Assert.Equal(cynthia, game.Battle!.EnemyPokemon.Level);
                // The trainer of the map keeps the data's team
                Assert.Equal(74, team.Party.Members[0].Level);
                someone.TrainerData = own;
            }
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
        }
    }

    [Fact]
    public void TheSignOverTheFieldNamesTheAreaInTheChosenLanguage()
    {
        // Plan 17 · N6b, slice 4: walking into an area showed its English name under every language
        string folder = Path.Combine(Path.GetTempPath(), "headless-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(12);
            var engine = Headless();
            var game = engine.Drive;
            engine.NewGameRegion = "Sinnoh";
            engine.StartNewGame();
            StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
            var sinnoh = MapDatabase.Get("Sinnoh");
            game.Place(sinnoh, 116, 886, Direction.Right);
            Text.Use("ro");
            game.EnterArea();
            Assert.Equal("Orășelul Twinleaf", game.LocationSign.Name);
        }
        finally
        {
            Text.Use(Text.English);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void RouteFortysGuideCrossesIntoTheBattleFrontierAndBackWithTheTeamAndOneSetOfRecords()
    {
        // Plan 20 · J13, part d: a Johto Champion spoken to by Route 40's guide comes out on Sinnoh's Battle Frontier by
        // the game's own crossing, and the guide on the grounds takes them back; the team, the region and the records
        // come through as they were
        string folder = Path.Combine(Path.GetTempPath(), "headless-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            Dice.Seed(12);
            var engine = Headless();
            var game = engine.Drive;
            engine.NewGameRegion = "Johto";
            engine.StartNewGame();
            game.Story.Set("JohtoHallOfFame");
            game.Story.SetVar(SpawnLocations.VariableOf(RegionDatabase.Johto), 2);
            game.Party.Clear();
            foreach (string species in new[] { "Typhlosion", "Ampharos", "Gyarados" }) game.Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 60));
            var team = game.Party.Members.ToList();
            var records = game.Frontier;
            records.GivePoints(12);
            game.State = GameState.Overworld;

            var gate = MapDatabase.Get("Route40_FrontierGate");
            Assert.Equal("Route40_FrontierGate", gate.Name);
            game.Place(gate, 5, 4, Direction.Up);
            game.TrainersLookOnArrival = false;
            game.Interact();
            Assert.True(engine.ScriptRunning);
            Until(engine, () => game.Map.Name == "BattleFrontier" && game.State == GameState.Overworld && !engine.ScriptRunning, "the Battle Frontier", press: true);
            Assert.Equal((47, 51), (game.Player.GridX, game.Player.GridY));
            Assert.Equal(RegionDatabase.Sinnoh, engine.Here.Id);
            Assert.Equal(team, game.Party.Members);
            Assert.All(game.Party.Members, p => Assert.Equal(RegionDatabase.Johto, p.Region));
            Assert.Same(records, game.Frontier);

            // The Johto guide stands beside the tile, and takes the player back to Route 40
            Assert.Contains(game.Map.NPCs, n => n.Name == "Guide" && (n.GridX, n.GridY) == (46, 51));
            game.Frontier.GivePoints(3);
            game.Place(game.Map, 47, 51, Direction.Left);
            game.Interact();
            Assert.True(engine.ScriptRunning);
            Until(engine, () => game.Map.Name == "Route40_FrontierGate" && game.State == GameState.Overworld && !engine.ScriptRunning, "Route 40's gate", press: true);
            Assert.Equal((5, 4), (game.Player.GridX, game.Player.GridY));
            Assert.Equal(RegionDatabase.Johto, engine.Here.Id);
            Assert.Equal(team, game.Party.Members);
            Assert.Same(records, game.Frontier);
            Assert.Equal(15, game.Frontier.BattlePoints);
            Assert.Equal(2, game.Story.Var(SpawnLocations.VariableOf(RegionDatabase.Johto)));
            // And the save holds the one set
            Assert.Equal(15, game.BuildSave().Frontier!.BattlePoints);
        }
        finally
        {
            InputManager.Feed(null);
            Dice.Seed(null);
            Ruleset.Use(RulesPreset.Platinum);
            PlayerIdentity.InRegion(RegionDatabase.First);
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
