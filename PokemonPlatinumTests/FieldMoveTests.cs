using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 02 · S2: the field moves, their badges and the obstacles they clear, Surf, Waterfall and Rock Climb by the
/// question, Strength's boulders, Flash and Defog, the party menu's moves, the Bicycle and the Cycling Road's gates,
/// the rods, the places Fly and Teleport go, and the Pokétch. Each gate is held shut without the move or the badge
/// and open with both, by the original's rules (<c>src/field_move_tasks.c</c>, <c>scripts_field_moves.s</c>).
/// </summary>
public class FieldMoveTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static Map Overworld => BuiltMaps.Value["Sinnoh"];

    private static Pokemon Knowing(params string[] moves)
    {
        var pokemon = new Pokemon(PokemonDatabase.Get("Bibarel")!, 30);
        pokemon.Moves.Clear();
        foreach (string move in moves) pokemon.Moves.Add(new Move(MoveDatabase.Get(move)!));
        return pokemon;
    }

    // ------------------------------------------------------------------ the badges

    [Theory]
    [InlineData(FieldMove.Cut, Badge.Forest)]
    [InlineData(FieldMove.Fly, Badge.Cobble)]
    [InlineData(FieldMove.Surf, Badge.Fen)]
    [InlineData(FieldMove.Strength, Badge.Mine)]
    [InlineData(FieldMove.Defog, Badge.Relic)]
    [InlineData(FieldMove.RockSmash, Badge.Coal)]
    [InlineData(FieldMove.Waterfall, Badge.Beacon)]
    [InlineData(FieldMove.RockClimb, Badge.Icicle)]
    public void EachHiddenMoveNeedsTheOriginalsBadge(FieldMove move, Badge badge)
    {
        Assert.Equal(badge, FieldMoveRules.BadgeFor(move));
        // With everything it needs but the badge, it can't be used; with the badge too, it can
        var spot = new FieldSpot
        {
            Obstacle = move switch { FieldMove.Cut => PropType.CutTree, FieldMove.RockSmash => PropType.CrackedRock, FieldMove.Strength => PropType.StrengthBoulder, _ => null },
            Water = true, Waterfall = true, RockFace = true, Fog = true, FlyAllowed = true
        };
        var story = new StoryState();
        Assert.Equal(FieldMoveError.Badge, FieldMoveRules.Check(move, spot, story));
        story.GiveBadge(badge);
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(move, spot, story));
    }

    [Fact]
    public void FlashTeleportDigAndSweetScentNeedNoBadge()
    {
        foreach (var move in new[] { FieldMove.Flash, FieldMove.Teleport, FieldMove.Dig, FieldMove.SweetScent, FieldMove.MilkDrink, FieldMove.Softboiled })
            Assert.Null(FieldMoveRules.BadgeFor(move));
        var story = new StoryState();
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Flash, new FieldSpot { Dark = true }, story));
        Assert.Equal(FieldMoveError.Location, FieldMoveRules.Check(FieldMove.Flash, new FieldSpot(), story));
        // Teleport flies, but never from a town; Dig leads out of a cave the header lets one leave
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Teleport, new FieldSpot { FlyAllowed = true }, story));
        Assert.Equal(FieldMoveError.Location, FieldMoveRules.Check(FieldMove.Teleport, new FieldSpot { FlyAllowed = true, Town = true }, story));
        Assert.Equal(FieldMoveError.Partner, FieldMoveRules.Check(FieldMove.Teleport, new FieldSpot { FlyAllowed = true, Partner = true }, story));
        Assert.Equal(FieldMoveError.Location, FieldMoveRules.Check(FieldMove.Dig, new FieldSpot(), story));
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Dig, new FieldSpot { CaveWithAWayOut = true }, story));
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.SweetScent, new FieldSpot(), story));
        // Surf can't start on the water, and Rock Smash isn't used from it
        story.SetBadges(0xFF);
        Assert.Equal(FieldMoveError.State, FieldMoveRules.Check(FieldMove.Surf, new FieldSpot { Water = true, Surfing = true }, story));
        Assert.Equal(FieldMoveError.Location, FieldMoveRules.Check(FieldMove.RockSmash, new FieldSpot { Obstacle = PropType.CrackedRock, Surfing = true }, story));
        // Every error has something to say
        foreach (var error in Enum.GetValues<FieldMoveError>().Where(e => e != FieldMoveError.None)) Assert.False(string.IsNullOrWhiteSpace(FieldMoveRules.Why(error)));
    }

    [Fact]
    public void AMoveIsAFieldMoveByItsName()
    {
        foreach (var move in Enum.GetValues<FieldMove>().Where(m => m != FieldMove.Chatter))
        {
            Assert.NotNull(MoveDatabase.Get(FieldMoveRules.MoveName(move)));
            Assert.Equal(move, FieldMoveRules.Of(FieldMoveRules.MoveName(move)));
        }
        // Chatter records the player's voice in the original: there is nothing for it to do here
        Assert.Null(FieldMoveRules.Of("Chatter"));
        Assert.Null(FieldMoveRules.Of("Tackle"));
    }

    // ------------------------------------------------------------------ the obstacles

    /// <summary>An obstacle faced, its script played with the team given, and the map's people put where its flags say.</summary>
    private static (HeadlessScriptHost Host, Map Map, NPC Thing) Face(PropType kind, Pokemon? pokemon, Badge? badge, int answer = 0)
    {
        var map = new Map(5, 5) { Name = "Obstacle" };
        var thing = map.AddObstacle(kind, 2, 1);
        thing.HiddenBy = "FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_TEST";
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (2, 2), PlayerFacing = Direction.Up };
        host.Party.Add(pokemon ?? Knowing("Tackle"));
        if (badge is { } b) host.Story.GiveBadge(b);
        host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.For(thing)!)!, thing);
        runner.RunToEnd();
        map.ApplyPresence(host.Story.Has);
        return (host, map, thing);
    }

    [Theory]
    [InlineData(PropType.CutTree, "Cut", Badge.Forest)]
    [InlineData(PropType.CrackedRock, "Rock Smash", Badge.Coal)]
    public void AnObstacleGivesWayToItsMoveAndBadgeOnly(PropType kind, string move, Badge badge)
    {
        // Not without the move, not without the badge: it says what it is and asks nothing
        foreach (var (pokemon, has) in new[] { (Knowing("Tackle"), (Badge?)badge), (Knowing(move), (Badge?)null) })
        {
            var (host, map, thing) = Face(kind, pokemon, has);
            Assert.Empty(host.Asked);
            Assert.Single(host.Transcript);
            Assert.Contains(thing, map.NPCs);
            Assert.DoesNotContain(host.Log, l => l.StartsWith("usemove"));
        }

        // With both, asked; no leaves it standing
        var (keep, kept, standing) = Face(kind, Knowing(move), badge, answer: 1);
        Assert.Single(keep.Asked);
        Assert.Contains(standing, kept.NPCs);

        // Yes: the move is used, and the obstacle is gone by its own flag
        var (cut, map2, gone) = Face(kind, Knowing(move), badge, answer: 0);
        Assert.Contains($"usemove {move} Bibarel on {gone.Name}", cut.Log);
        Assert.Contains(cut.Transcript, t => t.Text == $"Bibarel used {move}!");
        Assert.True(cut.Story.Has(gone.HiddenBy!));
        Assert.DoesNotContain(gone, map2.NPCs);
    }

    [Fact]
    public void StrengthLetsBouldersBePushedUntilThePlayerLeaves()
    {
        var (without, _, boulder) = Face(PropType.StrengthBoulder, Knowing("Strength"), null);
        Assert.False(without.Story.Has(FieldMoveRules.StrengthFlag));
        Assert.Empty(without.Asked);

        var (with, map, stays) = Face(PropType.StrengthBoulder, Knowing("Strength"), Badge.Mine);
        Assert.True(with.Story.Has(FieldMoveRules.StrengthFlag));
        // The boulder itself stays: Strength is what lets it be pushed
        Assert.Contains(stays, map.NPCs);
        Assert.Contains(with.Transcript, t => t.Text.Contains("boulders"));

        // Asked again once it is in force, it only says so
        var host = new HeadlessScriptHost { Map = map };
        host.Story.Set(FieldMoveRules.StrengthFlag);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.Boulder)!, stays);
        runner.RunToEnd();
        Assert.Empty(host.Asked);

        // Leaving the place forgets it, and the area's local flags with it
        with.Story.Set("FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_TEST");
        with.Story.SetVar("VAR_MAP_LOCAL_0X01", 3);
        FieldMoveRules.LeavePlace(with.Story, intoCave: false);
        Assert.False(with.Story.Has(FieldMoveRules.StrengthFlag));
        Assert.False(with.Story.Has("FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_TEST"));
        Assert.Equal(0, with.Story.Var("VAR_MAP_LOCAL_0X01"));
    }

    [Fact]
    public void ABoulderIsPushedATileOnWhereThereIsRoom()
    {
        var map = new Map(7, 7) { Name = "Boulders" };
        var boulder = map.AddObstacle(PropType.StrengthBoulder, 3, 2);
        var player = new Player(3, 3) { Facing = Direction.Up };

        // Without Strength it is a wall like any other
        player.Advance(1f / 60f, map, Direction.Up, false, null, null);
        Assert.Equal((3, 2), (boulder.GridX, boulder.GridY));
        Assert.Null(player.TakePush());

        // With it, the boulder goes a tile on and the player stays, leaning in while it slides
        player.PushesBoulders = true;
        player.Advance(1f / 60f, map, Direction.Up, false, null, null);
        Assert.Equal((3, 1), (boulder.GridX, boulder.GridY));
        Assert.Equal((3, 3), (player.GridX, player.GridY));
        Assert.True(player.IsPushing);
        Assert.Equal((boulder, Direction.Up), player.TakePush());

        // Against something solid, water or another boulder it doesn't move
        map.SetSolid(3, 0, true);
        Assert.False(FieldMovement.CanPush(map, boulder, Direction.Up));
        map.SetGroundTile(4, 1, TileType.Water);
        Assert.False(FieldMovement.CanPush(map, boulder, Direction.Right));
        map.AddObstacle(PropType.StrengthBoulder, 2, 1);
        Assert.False(FieldMovement.CanPush(map, boulder, Direction.Left));
        Assert.True(FieldMovement.CanPush(map, boulder, Direction.Down));

        // Coming back to the place puts it where it stood
        map.ResetObstacles();
        Assert.Equal((3, 2), (boulder.GridX, boulder.GridY));
        Assert.Equal(16f / 30f, FieldMovement.BoulderPushSeconds);
    }

    [Fact]
    public void TheWorldsObstaclesAreObjectsOfTheMapWithTheirAreasOwnFlags()
    {
        var things = BuiltMaps.Value.Values.SelectMany(m => m.Everyone.Where(n => n.IsObstacle).Select(n => (Map: m, Thing: n))).ToList();
        Assert.True(things.Count >= 40, $"only {things.Count} obstacles");
        foreach (var (map, thing) in things)
        {
            Assert.False(map.IsSolid(thing.GridX, thing.GridY), $"{thing.Name} at {thing.GridX},{thing.GridY} of {map.Name} stands in something solid");
            Assert.EndsWith("_" + thing.ScriptFile!.ToUpperInvariant(), thing.HiddenBy);
            Assert.StartsWith(StoryState.LocalFlagPrefix, thing.HiddenBy);
            Assert.NotNull(FieldScripts.For(thing));
        }
        // Eterna City's four trees and the forest's, which both number their flags from 1, are told apart
        var eterna = things.Single(t => t.Map.Name == "Sinnoh" && (t.Thing.GridX, t.Thing.GridY) == (304, 521)).Thing;
        Assert.Equal(PropType.CutTree, eterna.Obstacle);
        Assert.Equal("FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_ETERNA_CITY", eterna.HiddenBy);
        Assert.Equal(FieldScripts.CutTree, FieldScripts.For(eterna));
        Assert.Contains(things, t => t.Map.Name == "OreburghGateB1F" && t.Thing.Obstacle == PropType.StrengthBoulder);
        Assert.Equal("FLAG_MAP_LOCAL_HIDE_OBSTACLE_1_ETERNA_FOREST", WorldMapBuilder.LocalFlag("FLAG_MAP_LOCAL_HIDE_OBSTACLE_1", "eterna_forest"));
        Assert.Equal("FLAG_HIDE_SOMEONE", WorldMapBuilder.LocalFlag("FLAG_HIDE_SOMEONE", "eterna_city"));
    }

    // ------------------------------------------------------------------ water, waterfalls, rock faces

    [Fact]
    public void SurfIsOfferedOnlyWithTheMoveAndTheFenBadgeAndRidesOutOnYes()
    {
        var spot = new FieldSpot { Water = true };
        var story = new StoryState();
        Assert.Equal(FieldMoveError.Badge, FieldMoveRules.Check(FieldMove.Surf, spot, story));
        story.GiveBadge(Badge.Fen);
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Surf, spot, story));

        // The question at the water's edge, and the ride out
        var map = new Map(5, 5) { Name = "Shore" };
        for (int x = 0; x < 5; x++) map.SetGroundTile(x, 0, TileType.Water);
        foreach (int answer in new[] { 1, 0 })
        {
            var host = new HeadlessScriptHost { Map = map, PlayerTile = (2, 1), PlayerFacing = Direction.Up };
            host.Party.Add(Knowing("Surf"));
            host.Answers.Enqueue(answer);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(FieldScripts.Water)!);
            runner.RunToEnd();
            Assert.Equal(answer == 0 ? (2, 0) : (2, 1), host.PlayerTile);
            Assert.Equal(answer == 0 ? TravelMode.Surfing : TravelMode.OnFoot, host.PlayerMode);
        }
    }

    [Fact]
    public void AWaterfallAndARockFaceAreClimbedByTheQuestion()
    {
        // A river with a fall of three tiles, and beside it a rock face of three
        var map = new Map(7, 12) { Name = "Climbs" };
        for (int y = 0; y < 12; y++)
        {
            map.SetGroundTile(2, y, TileType.Water);
            map.SetHeight(2, y, y < 4 ? 3f : 0f);
            if (y is >= 4 and <= 6) map.SetBehaviour(2, y, TileBehavior.Waterfall);
            for (int x = 4; x < 7; x++) map.SetHeight(x, y, y <= 2 ? 4f : 0f);
            if (y is >= 3 and <= 5)
            {
                map.SetGroundTile(5, y, TileType.Rock, isSolid: true);
                map.SetBehaviour(5, y, TileBehavior.RockClimbNorthSouth);
            }
        }

        HeadlessScriptHost Play(string script, (int X, int Y) at, TravelMode mode, Pokemon pokemon, int badges)
        {
            var host = new HeadlessScriptHost { Map = map, PlayerTile = at, PlayerFacing = Direction.Up, PlayerMode = mode };
            host.Party.Add(pokemon);
            host.Story.SetBadges(badges);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(script)!);
            runner.RunToEnd();
            return host;
        }

        // Up the waterfall with the move and the Beacon Badge; not without either
        Assert.Equal((2, 3), Play(FieldScripts.Waterfall, (2, 7), TravelMode.Surfing, Knowing("Waterfall"), 1 << (int)Badge.Beacon).PlayerTile);
        Assert.Equal((2, 7), Play(FieldScripts.Waterfall, (2, 7), TravelMode.Surfing, Knowing("Waterfall"), 0).PlayerTile);
        Assert.Equal((2, 7), Play(FieldScripts.Waterfall, (2, 7), TravelMode.Surfing, Knowing("Surf"), 0xFF).PlayerTile);

        // Up the rock face with Rock Climb and the Icicle Badge; not without either
        Assert.Equal((5, 2), Play(FieldScripts.RockFace, (5, 6), TravelMode.OnFoot, Knowing("Rock Climb"), 1 << (int)Badge.Icicle).PlayerTile);
        Assert.Equal((5, 6), Play(FieldScripts.RockFace, (5, 6), TravelMode.OnFoot, Knowing("Rock Climb"), 0).PlayerTile);
        Assert.Equal((5, 6), Play(FieldScripts.RockFace, (5, 6), TravelMode.OnFoot, Knowing("Strength"), 0xFF).PlayerTile);

        // The player's own climb, once asked: up the face to the ground beyond it
        var player = new Player(5, 6) { Facing = Direction.Up, Moves = FieldMoves.RockClimb };
        Assert.True(player.Climb(map));
        Assert.True(player.IsMoving);
        Assert.Equal((5, 2), player.Heading);
        Assert.False(new Player(4, 6) { Facing = Direction.Up, Moves = FieldMoves.RockClimb }.Climb(map));
    }

    // ------------------------------------------------------------------ Flash and Defog

    [Fact]
    public void FlashLightsTheCavesUntilThePlayerLeavesThem()
    {
        var cave = BuiltMaps.Value["WaywardCave1F"];
        Assert.True(cave.IsDark);
        var (x, y) = (cave.Warps[0].SourceX, cave.Warps[0].SourceY);
        var spot = FieldMoveRules.SpotOf(cave, x, y, Direction.Up, new Walker());
        Assert.True(spot.Dark);
        Assert.True(spot.CaveWithAWayOut);
        Assert.False(spot.FlyAllowed);
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Flash, spot, new StoryState()));

        var host = new HeadlessScriptHost();
        host.Party.Add(Knowing("Flash"));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.FromMenu(FieldMove.Flash)!)!);
        runner.RunToEnd();
        Assert.True(host.Story.Has(FieldMoveRules.FlashFlag));

        // A cave's next floor stays lit; outside, it is forgotten
        FieldMoveRules.LeavePlace(host.Story, intoCave: true);
        Assert.True(host.Story.Has(FieldMoveRules.FlashFlag));
        FieldMoveRules.LeavePlace(host.Story, intoCave: false);
        Assert.False(host.Story.Has(FieldMoveRules.FlashFlag));
    }

    [Fact]
    public void DefogLiftsTheFogAndOnlyWhereThereIsFog()
    {
        var map = new Map(5, 5) { Name = "Foggy", Weather = FieldWeather.Fog };
        var spot = FieldMoveRules.SpotOf(map, 2, 2, Direction.Up, new Walker());
        Assert.True(spot.Fog);
        var story = new StoryState();
        Assert.Equal(FieldMoveError.Badge, FieldMoveRules.Check(FieldMove.Defog, spot, story));
        story.GiveBadge(Badge.Relic);
        Assert.Equal(FieldMoveError.None, FieldMoveRules.Check(FieldMove.Defog, spot, story));
        Assert.Equal(FieldMoveError.Location, FieldMoveRules.Check(FieldMove.Defog, FieldMoveRules.SpotOf(new Map(5, 5) { Name = "Clear" }, 2, 2, Direction.Up, new Walker()), story));

        // Once lifted the place's weather reads as clear
        map.FogLifted = true;
        Assert.Equal(FieldWeather.Clear, map.WeatherAt(2, 2));
        Assert.False(FieldMoveRules.SpotOf(map, 2, 2, Direction.Up, new Walker()).Fog);
    }

    // ------------------------------------------------------------------ the party menu

    [Fact]
    public void APokemonsMenuOffersItsFieldMovesInTheOrderOfItsMoves()
    {
        var pokemon = Knowing("Tackle", "Surf", "Cut", "Flash");
        var actions = PartyScreen.ActionsFor(pokemon);
        Assert.Equal(new[] { "SUMMARY", "SURF", "CUT", "FLASH", "SWITCH", "CANCEL" }, actions.Select(a => a.Label));
        Assert.Equal(new[] { "SUMMARY", "SWITCH", "CANCEL" }, PartyScreen.ActionsFor(Knowing("Tackle", "Chatter")).Select(a => a.Label));

        // Choosing one hands it to the game, with the Pokémon's place
        var party = new Party();
        party.Add(Knowing("Tackle"));
        party.Add(pokemon);
        var screen = new PartyScreen();
        screen.Open();
        screen.SelectedIndex = 1;
        screen.Confirm(party);
        Assert.NotNull(screen.Actions);
        screen.MoveAction(1);
        screen.MoveAction(1);
        screen.Confirm(party);
        Assert.Equal((FieldMove.Cut, 1), screen.TakeFieldMove());
        Assert.Null(screen.TakeFieldMove());
        Assert.Null(screen.Actions);
    }

    [Fact]
    public void SoftBoiledGivesAFifthOfItsHpToAnotherPokemon()
    {
        var chansey = new Pokemon(PokemonDatabase.Get("Chansey")!, 40);
        chansey.Moves.Clear();
        chansey.Moves.Add(new Move(MoveDatabase.Get("Soft-Boiled")!));
        var hurt = Knowing("Tackle");
        hurt.CurrentHP = hurt.MaxHP - 3;
        var low = Knowing("Tackle");
        low.CurrentHP = 1;
        var party = new Party();
        party.Add(chansey);
        party.Add(hurt);
        party.Add(low);

        // Only what the other is missing; never to itself, nor to one at full health or fainted
        Assert.Equal(3, FieldMoveRules.ShareHp(chansey, hurt));
        Assert.Equal(hurt.MaxHP, hurt.CurrentHP);
        Assert.Equal(chansey.MaxHP - 3, chansey.CurrentHP);
        Assert.Equal(0, FieldMoveRules.ShareHp(chansey, chansey));
        Assert.Equal(0, FieldMoveRules.ShareHp(chansey, hurt));
        var fainted = Knowing("Tackle");
        fainted.CurrentHP = 0;
        Assert.Equal(0, FieldMoveRules.ShareHp(chansey, fainted));

        // Through the menu: a fifth of its most HP
        chansey.CurrentHP = chansey.MaxHP;
        var screen = new PartyScreen();
        screen.Open();
        screen.Confirm(party);
        screen.MoveAction(1);
        screen.Confirm(party);
        Assert.Equal((0, FieldMove.Softboiled), screen.Sharing!.Value);
        screen.SelectedIndex = 2;
        screen.Confirm(party);
        Assert.Equal(1 + chansey.MaxHP / 5, low.CurrentHP);
        Assert.Equal(chansey.MaxHP - chansey.MaxHP / 5, chansey.CurrentHP);
        Assert.Null(screen.Sharing);
        Assert.Contains("recovered", screen.Message);

        // With a fifth or less left, it hasn't the HP to share
        chansey.CurrentHP = chansey.MaxHP / 5;
        Assert.False(FieldMoveRules.CanShareHp(chansey));
    }

    [Fact]
    public void EveryFieldMoveTheMenuOffersHasAScriptOrIsTheMenusOwn()
    {
        foreach (var move in Enum.GetValues<FieldMove>())
        {
            string? script = FieldScripts.FromMenu(move);
            if (move is FieldMove.MilkDrink or FieldMove.Softboiled or FieldMove.Chatter) Assert.Null(script);
            else Assert.NotNull(ScriptLibrary.Default.Find(script!));
        }
    }

    [Fact]
    public void AFieldMovesCutInOpensRunsThroughAndCloses()
    {
        var cutIn = new FieldCutIn("Bibarel", FieldMove.Cut);
        Assert.Equal((0f, 1f, 0f), (cutIn.Band, cutIn.Slide, cutIn.Label));
        cutIn.Advance(FieldCutIn.Opens + FieldCutIn.Enters + 0.1f);
        Assert.Equal((1f, 0f, 1f), (cutIn.Band, cutIn.Slide, cutIn.Label));
        cutIn.Advance(FieldCutIn.Holds + FieldCutIn.Leaves - 0.1f);
        Assert.Equal(-1f, cutIn.Slide, 3);
        Assert.False(cutIn.Done);
        cutIn.Advance(FieldCutIn.Closes);
        Assert.True(cutIn.Done);
        Assert.Equal(0f, cutIn.Band, 3);
        Assert.Equal("Cut", cutIn.Move);
    }

    // ------------------------------------------------------------------ Fly, Teleport, Dig

    [Fact]
    public void FlyAndTeleportLandInFrontOfEachTownsPokemonCenter()
    {
        var map = Overworld;
        // (Route 221's spot is in front of Pal Park, whose lobby comes after the Hall of Fame: plan 01 · M10)
        foreach (var town in SpawnLocations.All.Where(s => map.Areas.Any(a => a.Key == s.Area && a.Open) && MapDatabase.MapNames.Contains(s.Room)))
        {
            Assert.Equal(town.Area, map.AreaAt(town.X, town.Y)?.Key);
            Assert.True(map.IsWalkable(town.X, town.Y), $"{town.Area}'s landing at {town.X},{town.Y} is not open ground");
            // The tile above it is the Pokémon Center's door (Twinleaf's: the player's house)
            Assert.True(map.GetWarpAt(town.X, town.Y - 1) != null, $"{town.Area}: no door above its landing at {town.X},{town.Y}");
        }

        // The last Pokémon Center gone into is where Teleport goes; before any, Twinleaf Town
        var story = new StoryState();
        Assert.Equal("twinleaf_town", SpawnLocations.Respawn(story).Area);
        story.SetVar(SpawnLocations.Variable, SpawnLocations.OfRoom("EternaPokemonCenter")!.Id);
        Assert.Equal("eterna_city", SpawnLocations.Respawn(story).Area);

        // Fly reaches the towns arrived in, among those open
        Assert.Empty(SpawnLocations.FlyDestinations(story, _ => true));
        story.Set(SpawnLocations.ArrivedIn("eterna_city")!.ArrivalFlag);
        story.Set(SpawnLocations.ArrivedIn("snowpoint_city")!.ArrivalFlag);
        Assert.Equal(new[] { "eterna_city" }, SpawnLocations.FlyDestinations(story, key => key != "snowpoint_city").Select(s => s.Area));
        Assert.Null(SpawnLocations.ArrivedIn("route_206"));
    }

    [Fact]
    public void TheFlyScreenChoosesATownStartingNearestThePlayer()
    {
        var towns = SpawnLocations.All.Take(9).ToList();
        var screen = new FlyScreen();
        screen.Open(towns, (305, 540), flier: 2);
        Assert.Equal("eterna_city", towns[screen.Cursor].Area);
        screen.Move(1);
        Assert.Equal("twinleaf_town", towns[screen.Cursor].Area);
        screen.Confirm();
        Assert.False(screen.IsActive);
        Assert.Equal("twinleaf_town", screen.TakeChoice()!.Area);
        Assert.Null(screen.TakeChoice());
        Assert.Equal(2, screen.Flier);
    }

    [Fact]
    public void TeleportDigAndSweetScentScriptsDoWhatTheirMovesDo()
    {
        HeadlessScriptHost Run(FieldMove move, Action<HeadlessScriptHost>? setUp = null)
        {
            var host = new HeadlessScriptHost();
            host.Party.Add(Knowing(FieldMoveRules.MoveName(move)));
            setUp?.Invoke(host);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(FieldScripts.FromMenu(move)!)!, pokemon: host.Party.Members[0]);
            runner.RunToEnd();
            return host;
        }

        var teleport = Run(FieldMove.Teleport, h => h.Story.SetVar(SpawnLocations.Variable, 9));
        Assert.Contains("warp Sinnoh 305 531", teleport.Log);
        var dig = Run(FieldMove.Dig, h => h.Exit = new MapSpot("Sinnoh", 299, 612, Direction.Down));
        Assert.Contains("warp Sinnoh 299 612", dig.Log);
        var fly = Run(FieldMove.Fly, h => h.FlyTo = SpawnLocations.Get(3));
        Assert.Contains("warp Sinnoh 176 667", fly.Log);

        var nothing = Run(FieldMove.SweetScent);
        Assert.Contains(nothing.Transcript, t => t.Text.Contains("nothing comes out"));
        var scented = Run(FieldMove.SweetScent, h => h.Scented = ("Starly", 4));
        Assert.Contains("wildbattle Starly 4 Won", scented.Log);
        Assert.DoesNotContain(scented.Transcript, t => t.Text.Contains("nothing comes out"));
    }

    // ------------------------------------------------------------------ the Bicycle and the Cycling Road

    [Fact]
    public void TheBicycleGoesWhereTheOriginalLetsIt()
    {
        var map = new Map(6, 6) { Name = "Park" };
        map.SetBehaviour(1, 1, TileBehavior.VeryTallGrass);
        map.SetBehaviour(2, 1, TileBehavior.Mud);
        map.SetBehaviour(3, 1, TileBehavior.BikeBridgeNorthSouth);
        Assert.Equal(BicycleCheck.Ok, BicycleRules.Check(map, 0, 0, TravelMode.OnFoot, false));
        Assert.Equal(BicycleCheck.Ok, BicycleRules.Check(map, 0, 0, TravelMode.Cycling, false));
        Assert.Equal(BicycleCheck.NotHere, BicycleRules.Check(map, 1, 1, TravelMode.OnFoot, false));
        Assert.Equal(BicycleCheck.NotHere, BicycleRules.Check(map, 2, 1, TravelMode.Cycling, false));
        Assert.Equal(BicycleCheck.CannotDismount, BicycleRules.Check(map, 3, 1, TravelMode.Cycling, false));
        Assert.Equal(BicycleCheck.NotHere, BicycleRules.Check(map, 0, 0, TravelMode.Surfing, false));
        // On the Cycling Road the rider stays a rider
        Assert.Equal(BicycleCheck.CannotDismount, BicycleRules.Check(map, 0, 0, TravelMode.Cycling, onCyclingRoad: true));
        // Never in a room
        Assert.Equal(BicycleCheck.NotHere, BicycleRules.Check(new Map(4, 4) { Name = "Room", Interior = InteriorStyle.House }, 1, 1, TravelMode.OnFoot, false));

        // The header's word on it: Wayward Cave lets one ride, Amity Square doesn't
        var cave = BuiltMaps.Value["WaywardCave1F"];
        Assert.True(cave.BikeAllowedAt(cave.Warps[0].SourceX, cave.Warps[0].SourceY));
        var square = BuiltMaps.Value["AmitySquare"];
        Assert.False(square.BikeAllowedAt(square.Warps[0].SourceX, square.Warps[0].SourceY));
    }

    [Theory]
    // Into the Cycling Road from Eterna City and from the south: riders only
    [InlineData(304, 569, true)]
    [InlineData(305, 569, true)]
    [InlineData(302, 688, true)]
    [InlineData(301, 688, true)]
    // Out of it, either way: anyone
    [InlineData(304, 576, false)]
    [InlineData(302, 681, false)]
    [InlineData(300, 681, false)]
    public void OnlyRidersAreLetOntoTheCyclingRoad(int x, int y, bool ridersOnly)
    {
        var warp = Overworld.GetWarpAt(x, y);
        Assert.NotNull(warp);
        Assert.Equal(ridersOnly, warp!.CyclistsOnly);
        // Other gates are walked through by anyone
        Assert.False(Overworld.GetWarpAt(447, 726)!.CyclistsOnly);
    }

    [Fact]
    public void TheGateKeeperTurnsBackSomeoneOnFoot()
    {
        var host = new HeadlessScriptHost { PlayerTile = (304, 569), PlayerFacing = Direction.Down };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.CyclistsOnly)!);
        runner.RunToEnd();
        Assert.Equal((304, 568), host.PlayerTile);
        Assert.Contains(host.Transcript, t => t.Speaker == "Gate Keeper");
    }

    [Fact]
    public void TheBagUsesTheBicycleAndTheRodsInTheFieldAndKeepsOneOnTheItemButton()
    {
        var bicycle = ItemDatabase.Get("Bicycle")!;
        Assert.Equal(new[] { BagAction.Use, BagAction.Register, BagAction.Cancel }, BagScreen.ActionsFor(bicycle));
        Assert.Equal(new[] { BagAction.Use, BagAction.Deselect, BagAction.Cancel }, BagScreen.ActionsFor(bicycle, "Bicycle"));
        foreach (string rod in new[] { "Old Rod", "Good Rod", "Super Rod" }) Assert.True(BagScreen.UsedInField(ItemDatabase.Get(rod)!));
        Assert.Equal(new[] { BagAction.Use, BagAction.Give, BagAction.Toss, BagAction.Cancel }, BagScreen.ActionsFor(ItemDatabase.Get("Escape Rope")!));

        var bag = new Inventory();
        bag.AddItem(bicycle, 1);
        var screen = new BagScreen();
        screen.Open();
        screen.CurrentPocket = ItemPocket.KeyItems;
        var said = new List<string>();
        screen.Confirm(bag, new Party(), said.Add);
        screen.Confirm(bag, new Party(), said.Add);
        Assert.False(screen.IsActive);
        Assert.Equal(bicycle, screen.TakeFieldUse());
        Assert.Null(screen.TakeFieldUse());

        // Registering it
        screen.Open();
        screen.CurrentPocket = ItemPocket.KeyItems;
        screen.Confirm(bag, new Party(), said.Add);
        screen.MoveCursor(1, 1);
        screen.Confirm(bag, new Party(), said.Add);
        Assert.Equal("Bicycle", screen.Registered);
        Assert.Single(said);
    }

    // ------------------------------------------------------------------ fishing

    [Fact]
    public void ACastPlaysOutAsTheOriginalsFishingTask()
    {
        var magikarp = new WildEncounterEntry { SpeciesName = "Magikarp", MinLevel = 5, MaxLevel = 5 };

        // Something bites within four seconds, and pressed in time it is landed
        var cast = new FishingAttempt(FishingRod.Old, magikarp, new Random(1));
        cast.Update(FishingAttempt.CastSeconds - 0.01f, false);
        Assert.Equal(FishingStage.Casting, cast.Stage);
        cast.Update(0.02f, false);
        Assert.Equal(FishingStage.Waiting, cast.Stage);
        for (int i = 0; i < 41 && cast.Stage == FishingStage.Waiting; i++) cast.Update(0.1f, false);
        Assert.Equal(FishingStage.Hooked, cast.Stage);
        cast.Update(0.1f, true);
        Assert.Equal(FishingStage.Landed, cast.Stage);
        Assert.True(cast.Says);
        cast.Read();
        Assert.Equal(FishingStage.Done, cast.Stage);
        Assert.Same(magikarp, cast.Caught);

        // Pressed before the bite, the line comes up empty
        var early = new FishingAttempt(FishingRod.Good, magikarp, new Random(1));
        early.Update(FishingAttempt.CastSeconds, false);
        early.Update(0.1f, true);
        Assert.Equal(FishingStage.TooSoon, early.Stage);

        // Too slow, and it gets away: the Super Rod gives half a second
        var slow = new FishingAttempt(FishingRod.Super, magikarp, new Random(1));
        slow.Update(FishingAttempt.CastSeconds, false);
        slow.Update(4.01f, false);
        Assert.Equal(FishingStage.Hooked, slow.Stage);
        slow.Update(FishingAttempt.HookSeconds(FishingRod.Super), false);
        Assert.Equal(FishingStage.GotAway, slow.Stage);
        slow.Read();
        Assert.Null(slow.Caught);

        // Nothing at all after four seconds
        var nothing = new FishingAttempt(FishingRod.Old, null, new Random(1));
        nothing.Update(FishingAttempt.CastSeconds, false);
        nothing.Update(FishingAttempt.NothingSeconds, false);
        Assert.Equal(FishingStage.NoNibble, nothing.Stage);
        Assert.Equal((1.5f, 1f, 0.5f), (FishingAttempt.HookSeconds(FishingRod.Old), FishingAttempt.HookSeconds(FishingRod.Good), FishingAttempt.HookSeconds(FishingRod.Super)));
    }

    [Fact]
    public void EachRodHasTheAreasOwnTableAndRate()
    {
        var route = Overworld.Areas.Single(a => a.Key == "route_203");
        Assert.Equal(new[] { 25, 50, 75 }, route.RodRates);
        Assert.Equal(new[] { 60, 30, 5, 4, 1 }, route.RodEncounters[(int)FishingRod.Old].Select(e => e.Weight));
        Assert.Equal(new[] { 40, 40, 15, 4, 1 }, route.RodEncounters[(int)FishingRod.Good].Select(e => e.Weight));
        Assert.All(route.RodEncounters[(int)FishingRod.Old], e => Assert.Equal("Magikarp", e.SpeciesName));
        Assert.Contains(route.RodEncounters[(int)FishingRod.Super], e => e.SpeciesName == "Gyarados");

        // A cast on its water bites about as often as the rate says
        var (x, y) = TileOf(Overworld, "route_203");
        int bites = Enumerable.Range(0, 2000).Count(_ => Overworld.Fish(x, y, FishingRod.Old) != null);
        Assert.InRange(bites, 380, 620);

        // A rod is cast at water, and never from a deck over it
        var shore = new Map(4, 4) { Name = "Shore" };
        shore.SetGroundTile(1, 0, TileType.Water);
        Assert.True(FishingAttempt.CanCast(shore, 1, 1, Direction.Up, 0f));
        Assert.False(FishingAttempt.CanCast(shore, 1, 1, Direction.Down, 0f));
        Assert.Equal(FishingRod.Super, FishingAttempt.RodOf(ItemDatabase.Get("Super Rod")!));
        Assert.Null(FishingAttempt.RodOf(ItemDatabase.Get("Bicycle")!));
    }

    private static (int X, int Y) TileOf(Map map, string area)
    {
        for (int y = 0; y < map.Height; y += 4)
            for (int x = 0; x < map.Width; x += 4)
                if (map.AreaAt(x, y)?.Key == area) return (x, y);
        throw new InvalidOperationException($"no tile of {area}");
    }

    // ------------------------------------------------------------------ the Pokétch

    [Fact]
    public void ThePoketchShowsTheAppsItRunsAndKeepsThemInASave()
    {
        var poketch = new Poketch();
        Assert.Null(poketch.Current);
        poketch.Step();
        Assert.Equal(0, poketch.Steps);

        // What the original's president hands over: four apps, three of which run so far
        var host = new HeadlessScriptHost { Map = new Map(4, 4) { Name = "Test" } };
        var library = ScriptLibrary.FromSources(("test", """
            script Gift
              if poketch end
              poketch on
              poketchapp DigitalWatch
              poketchapp Calculator
              poketchapp Pedometer
              poketchapp PartyStatus
            """));
        var runner = new ScriptRunner(library, host);
        runner.Start(library.All.Single());
        runner.RunToEnd();
        var given = host.Poketch;
        Assert.True(given.Enabled);
        Assert.Equal(4, given.Apps.Count);
        Assert.Equal(new[] { PoketchApp.DigitalWatch, PoketchApp.Pedometer, PoketchApp.PartyStatus }, given.Shown);
        Assert.Equal(PoketchApp.DigitalWatch, given.Current);
        given.Next();
        Assert.Equal(PoketchApp.Pedometer, given.Current);
        given.Next();
        given.Next();
        Assert.Equal(PoketchApp.DigitalWatch, given.Current);
        Assert.False(given.Register(PoketchApp.Pedometer));

        for (int i = 0; i < 12; i++) given.Step();
        var restored = new Poketch();
        restored.Load(given.Save());
        Assert.Equal((true, 12, 4), (restored.Enabled, restored.Steps, restored.Apps.Count));
        restored.Load(null);
        Assert.False(restored.Enabled);

        // Five figures and no more
        for (int i = 0; i < 3; i++) given.Step();
        var full = new Poketch { Enabled = true };
        full.Load(new PoketchSave { Enabled = true, Steps = Poketch.MostSteps });
        full.Step();
        Assert.Equal(Poketch.MostSteps, full.Steps);
    }

    [Fact]
    public void AScriptNamesOnlyFieldMovesAndPoketchApps()
    {
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script S\n usemove \"Tackle\""));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script S\n poketchapp Clock"));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script S\n poketch off"));
    }
}
