using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 10 · F3 to F5 played by the whole game with no window: the lead out of its ball behind the player, through a
/// door and back in its ball on a Bicycle; spoken to; and wild Pokémon standing in Route 201's grass, met with the
/// Pokémon seen. It shares the dice and the databases with the whole process, so it runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class CompanionGameTests
{
    private const float Frame = 1f / 60f;

    private static (GameEngine Engine, GameEngine.Driver Game) NewGame(bool follower, bool wild)
    {
        Dice.Seed(31);
        var engine = new GameEngine(new GameSettings { Muted = true, Follower = follower, WildInSight = wild });
        engine.InitializeHeadless();
        engine.PeopleStayPut = true;
        var game = engine.Drive;
        engine.NewGameRegion = "Sinnoh";
        engine.StartNewGame();
        StoryMigration.PastTheOpening(game.Story, ScriptLibrary.Default);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Turtwig")!, 20));
        game.State = GameState.Overworld;
        game.TrainersLookOnArrival = false;
        return (engine, game);
    }

    private static void Run(GameEngine engine, int frames = 1)
    {
        for (int f = 0; f < frames; f++)
        {
            InputManager.Feed(ActionFrame.None);
            engine.Update(Frame);
        }
    }

    private static void Done()
    {
        InputManager.Feed(null);
        Dice.Seed(null);
        Ruleset.Use(RulesPreset.Platinum);
    }

    [Fact]
    public void TheLeadFollowsBehindThroughADoorAndGoesBackInItsBallOnABicycle()
    {
        try
        {
            var (engine, game) = NewGame(follower: true, wild: false);
            var sinnoh = MapDatabase.Get("Sinnoh");
            var door = sinnoh.Warps.First(w => w.TargetMap == "PokemonCenter");
            game.Place(sinnoh, door.SourceX, door.SourceY + 4, Direction.Up);
            Run(engine, 2);
            var lead = Assert.IsType<Follower>(game.Lead);
            Assert.Same(game.Party.Members[0], lead.Pokemon);
            Assert.Equal((door.SourceX, door.SourceY + 5), (lead.Who.GridX, lead.Who.GridY));
            Assert.Contains(lead.Who, game.Map.NPCs);

            // Walked north, it keeps a tile behind and never stands where the field refuses it
            for (int f = 0; f < 60 && game.State == GameState.Overworld; f++)
            {
                engine.Steering = (Direction.Up, false);
                Run(engine);
                if (!game.Player.IsMoving && lead.Who.StepOffsetX == 0f && lead.Who.StepOffsetY == 0f)
                    Assert.False(game.Map.IsSolid(lead.Who.GridX, lead.Who.GridY), $"the lead stands in a wall at {lead.Who.GridX},{lead.Who.GridY}");
            }
            // Through the door: it is in the room behind the player when the room opens
            for (int f = 0; f < 600 && game.Map.Name != "PokemonCenter"; f++)
            {
                engine.Steering = (Direction.Up, false);
                Run(engine);
            }
            engine.Steering = null;
            for (int f = 0; f < 600 && game.State != GameState.Overworld; f++) Run(engine);
            Run(engine, 2);
            Assert.Equal("PokemonCenter", game.Map.Name);
            Assert.NotNull(game.Lead);
            Assert.Contains(game.Lead!.Who, game.Map.NPCs);
            Assert.DoesNotContain(game.Lead.Who, sinnoh.NPCs);
            Assert.Equal(1, Math.Abs(game.Lead.Who.GridX - game.Player.GridX) + Math.Abs(game.Lead.Who.GridY - game.Player.GridY));

            // On a Bicycle (out of doors), it is back in its ball; on foot it comes out again
            game.Place(sinnoh, door.SourceX, door.SourceY + 4, Direction.Down);
            Run(engine, 2);
            Assert.NotNull(game.Lead);
            Assert.True(game.Player.SetCycling(true));
            Run(engine);
            Assert.Null(game.Lead);
            Assert.DoesNotContain(sinnoh.NPCs, n => n.Key == LeadFollower.BodyKey);
            game.Player.SetCycling(false);
            Run(engine);
            Assert.NotNull(game.Lead);

            // Swapped for another of the team, the new first walks
            var shinx = new Pokemon(PokemonDatabase.Get("Shinx")!, 12);
            game.Party.Members.Insert(0, shinx);
            Run(engine);
            Assert.Same(shinx, game.Lead!.Pokemon);
            Assert.Single(sinnoh.NPCs, n => n.Key == LeadFollower.BodyKey);

            // Spoken to, it says its band's line
            game.Lead.Who.FaceTowards(game.Player.GridX, game.Player.GridY);
            Assert.True(game.TalkToTheLead());
            Assert.True(engine.ScriptRunning);
        }
        finally
        {
            Done();
        }
    }

    [Fact]
    public void WithTheRowOffNoLeadAndNoWildPokemonAreOut()
    {
        try
        {
            var (engine, game) = NewGame(follower: false, wild: false);
            var sinnoh = MapDatabase.Get("Sinnoh");
            game.Place(sinnoh, 116, 870, Direction.Up);
            Run(engine, 300);
            Assert.Null(game.Lead);
            Assert.Null(game.Along);
            Assert.Empty(game.WildInSight.All);
            Assert.False(game.Player.WildInSight);
        }
        finally
        {
            Done();
        }
    }

    [Fact]
    public void WildPokemonStandInRoute201sGrassAndOneTouchedIsMet()
    {
        try
        {
            var (engine, game) = NewGame(follower: false, wild: true);
            var sinnoh = MapDatabase.Get("Sinnoh");
            // Beside Route 201's grass, in the open
            var (gx, gy) = NearGrass(sinnoh, 116, 886);
            game.Place(sinnoh, gx, gy, Direction.Down);
            var seen = game.WildInSight;
            for (int f = 0; f < 60 * 30 && seen.All.Count < 3; f++) Assert.False(game.KeepWildInSight(Frame), "one was met by itself");
            Assert.InRange(seen.All.Count, 1, FieldPokemon.MostInView);
            Assert.True(game.Player.WildInSight);
            var area = sinnoh.AreaAt(gx, gy)!;
            foreach (var w in seen.All)
            {
                Assert.True(sinnoh.IsTallGrass(w.Body.GridX, w.Body.GridY) || TileBehaviors.IsSurfable(sinnoh.BehaviourAt(w.Body.GridX, w.Body.GridY)));
                var table = sinnoh.AreaAt(w.HomeX, w.HomeY)!;
                var rows = w.Water ? table.WaterEncounters : table.WildEncounters;
                Assert.Contains(rows, r => r.SpeciesName == w.Entry.SpeciesName);
                Assert.InRange(w.Pokemon.Level, w.Entry.MinLevel, w.Entry.MaxLevel);
                Assert.Contains(w.Body, sinnoh.NPCs);
                Assert.Equal(w.Pokemon.LookName, w.Body.Species);
            }
            // Never more than six, however long
            for (int f = 0; f < 60 * 30; f++)
            {
                if (game.KeepWildInSight(Frame)) break;
                Assert.True(seen.All.Count <= FieldPokemon.MostInView);
            }

            // One touched is met, with the Pokémon seen
            var met = seen.All.First();
            var pokemon = met.Pokemon;
            game.State = GameState.Overworld;
            Assert.True(game.MeetSeen(met));
            Assert.DoesNotContain(met.Body, sinnoh.NPCs);
            for (int f = 0; f < 600 && game.State != GameState.Battle; f++) Run(engine);
            Assert.Equal(GameState.Battle, game.State);
            Assert.Same(pokemon, game.Battle!.EnemyPokemon);
        }
        finally
        {
            Done();
        }
    }

    // An open tile beside tall grass near a spot
    private static (int X, int Y) NearGrass(Map map, int x, int y)
    {
        for (int r = 1; r < 80; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    int gx = x + dx, gy = y + dy;
                    if (map.IsWalkable(gx, gy) && !map.IsTallGrass(gx, gy) && map.IsTallGrass(gx + 3, gy)) return (gx, gy);
                }
        throw new InvalidOperationException("No tall grass near Route 201.");
    }
}
