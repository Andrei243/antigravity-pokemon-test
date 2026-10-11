using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.SuperContests;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Amity Square's walk (plan 06 · R16c). The numbers are the decompilation's (<c>scrcmd_amity_square.c</c>,
/// <c>scripts_amity_square.s</c>, <c>events_amity_square.json</c>, <c>system_vars.c</c>).
/// </summary>
[Collection("MapDatabase")]
public class AmitySquareTests
{
    private const string Place = "amity_square";

    private static Map Square() => MapDatabase.Fresh()(AmitySquare.Map);

    private static Pokemon Mon(string species, int level = 20, string? nickname = null)
    {
        var pokemon = new Pokemon(PokemonDatabase.Get(species)!, level);
        if (nickname != null) pokemon.Nickname = nickname;
        return pokemon;
    }

    private static Party Team(params string[] species)
    {
        var party = new Party();
        foreach (string name in species) party.Add(Mon(name));
        return party;
    }

    /// <summary>A die that gives the rolls it was handed, in order, and writes down how many sides each was asked for.</summary>
    private static Func<int, int> Dice(List<int> sides, params int[] rolls)
    {
        var left = new Queue<int>(rolls);
        return n =>
        {
            sides.Add(n);
            return left.Count > 0 ? left.Dequeue() % n : 0;
        };
    }

    private static HeadlessScriptHost Host(Map map, Party party, int x, int y)
    {
        var host = new HeadlessScriptHost { Map = map, PlayerTile = (x, y) };
        foreach (var pokemon in party.Members) host.Party.Add(pokemon);
        // (The square's arrival script puts the Pokémon nobody walks with off the map: a game is never in the square without it)
        host.Story.Set(AmitySquare.HideFlag);
        map.ApplyPresence(host.Story.Has);
        return host;
    }

    private static HeadlessScriptHost Run(string name, HeadlessScriptHost host, string? who = null)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(name, Place)!, who == null ? null : host.Map!.Everyone.First(n => n.Key == who));
        runner.RunToEnd();
        host.Map!.ApplyPresence(host.Story.Has);
        return host;
    }

    private static bool Said(HeadlessScriptHost host, string part) => host.Transcript.Any(l => l.Text.Contains(part));

    // ------------------------------------------------------------------ who may walk

    [Fact]
    public void TheSpeciesLetInAreTheOriginals()
    {
        // AmitySquare_CheckAllowedMon: sixteen, and four more with the National Pokédex
        Assert.Equal(16, AmitySquare.Permitted.Count);
        Assert.Equal(new[] { "Jigglypuff", "Torchic", "Skitty", "Shroomish" }, AmitySquare.WithNationalDex);
        Assert.All(AmitySquare.Permitted.Concat(AmitySquare.WithNationalDex), name => Assert.NotNull(PokemonDatabase.Get(name)));
        foreach (string name in new[] { "Pikachu", "Clefairy", "Psyduck", "Pachirisu", "Drifloon", "Buneary", "Happiny", "Turtwig", "Torterra", "Monferno", "Empoleon" })
            Assert.True(AmitySquare.MayWalk(Mon(name), nationalDex: false), name);
        foreach (string name in new[] { "Bidoof", "Raichu", "Clefable", "Golduck", "Lopunny", "Chansey", "Drifblim", "Skitty", "Torchic" })
            Assert.False(AmitySquare.MayWalk(Mon(name), nationalDex: false), name);
        Assert.True(AmitySquare.MayWalk(Mon("Skitty"), nationalDex: true));
        Assert.False(AmitySquare.MayWalk(Mon("Delcatty"), nationalDex: true));
    }

    [Fact]
    public void AnEggIsNotAskedAboutThoughItsSpeciesCounts()
    {
        // GetPartyMonSpecies gives an Egg SPECIES_NONE, but Party_HasSpecies reads the species under the shell: with
        // only an Egg of a Pikachu the receptionist doesn't list the species, and has nobody to ask about
        var party = Team("Bidoof", "Pikachu");
        party.Members[1].IsEgg = true;
        Assert.True(AmitySquare.HasPermitted(party, false));
        Assert.False(AmitySquare.MayWalk(party.Members[1], false));
        Assert.Equal(-1, AmitySquare.Next(party, false, 0));

        // Each place of the team in turn
        var team = Team("Bidoof", "Pikachu", "Starly", "Turtwig", "Skitty");
        Assert.Equal(1, AmitySquare.Next(team, false, 0));
        Assert.Equal(3, AmitySquare.Next(team, false, 2));
        Assert.Equal(-1, AmitySquare.Next(team, false, 4));
        Assert.Equal(4, AmitySquare.Next(team, true, 4));
        Assert.False(AmitySquare.HasPermitted(Team("Bidoof", "Starly"), true));
    }

    // ------------------------------------------------------------------ what it turns up

    [Fact]
    public void AnAccessoryIsFoundByTheSpeciesRowAndARollOfAHundred()
    {
        // CalcAmitySquareFoundAccessory: under 15, 30, 45 and 60 the row's first four, under 70 and 80 the next two,
        // under 88 the seventh, under 93 and 98 the next two, and the last on 98 and 99
        Assert.Equal("Orange Fluff", AmitySquare.AccessoryFor("Pikachu", 0));
        Assert.Equal("Orange Fluff", AmitySquare.AccessoryFor("Pikachu", 14));
        Assert.Equal("Brown Fluff", AmitySquare.AccessoryFor("Pikachu", 15));
        Assert.Equal("Red Feather", AmitySquare.AccessoryFor("Happiny", 59));
        Assert.Equal("Yellow Feather", AmitySquare.AccessoryFor("Happiny", 60));
        Assert.Equal("Yellow Fluff", AmitySquare.AccessoryFor("Clefairy", 79));
        Assert.Equal("Mini Pebble", AmitySquare.AccessoryFor("Pachirisu", 87));
        Assert.Equal("Big Scale", AmitySquare.AccessoryFor("Pachirisu", 88));
        Assert.Equal("Black Moustache", AmitySquare.AccessoryFor("Pikachu", 97));
        Assert.Equal("Purple Scale", AmitySquare.AccessoryFor("Pikachu", 98));
        Assert.Equal("Purple Scale", AmitySquare.AccessoryFor("Pikachu", 99));
        // The six rows, by their rarest
        Assert.Equal("Stump", AmitySquare.AccessoryFor("Infernape", 99));
        Assert.Equal("Purple Scale", AmitySquare.AccessoryFor("Prinplup", 99));
        Assert.Equal("Thick Mushroom", AmitySquare.AccessoryFor("Turtwig", 99));
        Assert.Equal("Glitter Boulder", AmitySquare.AccessoryFor("Drifloon", 99));
        Assert.Equal("Black Pebble", AmitySquare.AccessoryFor("Shroomish", 99));
        // Whoever is in no row takes the first, as the original's switch does
        Assert.Equal(0, AmitySquare.PoolOf("Bidoof"));
        Assert.Equal(4, AmitySquare.PoolOf("Happiny"));

        // Everything the rows and the man's list name is an accessory of the Fashion Case or an item of the bag
        foreach (string species in AmitySquare.Permitted.Concat(AmitySquare.WithNationalDex))
            for (int roll = 0; roll < 100; roll++)
                Assert.True(SuperContestDatabase.Accessory(AmitySquare.AccessoryFor(species, roll)) >= 0, AmitySquare.AccessoryFor(species, roll));
        Assert.All(AmitySquare.Berries, berry => Assert.NotNull(ItemDatabase.Get(berry)));
        Assert.All(AmitySquare.Gifts.Skip(9), gift => Assert.True(SuperContestDatabase.Accessory(gift) >= 0, gift));
    }

    [Fact]
    public void ABerryIsFoundByARollOfAHundred()
    {
        // AmitySquare_FollowerMon_SetPickupItemVar: 20, then 15 three times, then 7 five times
        Assert.Equal("Magost Berry", AmitySquare.BerryFor(0));
        Assert.Equal("Magost Berry", AmitySquare.BerryFor(19));
        Assert.Equal("Cornn Berry", AmitySquare.BerryFor(20));
        Assert.Equal("Rabuta Berry", AmitySquare.BerryFor(35));
        Assert.Equal("Nomel Berry", AmitySquare.BerryFor(64));
        Assert.Equal("Spelon Berry", AmitySquare.BerryFor(65));
        Assert.Equal("Pamtre Berry", AmitySquare.BerryFor(72));
        Assert.Equal("Watmel Berry", AmitySquare.BerryFor(79));
        Assert.Equal("Durin Berry", AmitySquare.BerryFor(92));
        Assert.Equal("Belue Berry", AmitySquare.BerryFor(93));
        Assert.Equal("Belue Berry", AmitySquare.BerryFor(99));
    }

    [Fact]
    public void TheNextFindIsDecidedAheadByThreeDice()
    {
        // SetNextPickUpType: GetRandom 5 (nought a berry), CalcAmitySquareFoundAccessory's roll, then GetRandom 100
        var story = new StoryState();
        var sides = new List<int>();
        AmitySquare.DrawNext(story, "Pikachu", Dice(sides, 0, 98, 93));
        Assert.Equal(new[] { 5, 100, 100 }, sides);
        Assert.Equal(0, story.Var(AmitySquare.NextKindVar));
        Assert.Equal(SuperContestDatabase.Accessory("Purple Scale"), story.Var(AmitySquare.NextAccessoryVar));
        Assert.Equal("Belue Berry", AmitySquare.Berries[story.Var(AmitySquare.NextBerryVar)]);
        AmitySquare.DrawNext(story, "Pikachu", Dice(sides, 4, 0, 0));
        Assert.Equal(1, story.Var(AmitySquare.NextKindVar));
    }

    [Fact]
    public void TwoHundredStepsTurnSomethingUp()
    {
        var story = new StoryState();
        var fashionCase = new FashionCase();
        var pikachu = Mon("Pikachu");
        // The walk begins: the count at nought, an accessory decided (a roll of 1 of five), the row's first
        AmitySquare.Begin(story, 2, pikachu, Dice(new List<int>(), 1, 0, 0));
        Assert.Equal(2, story.Var(AmitySquare.PartyVar));
        Assert.Equal(pikachu.Species.DexNumber, story.Var(AmitySquare.SpeciesVar));

        for (int i = 0; i < 199; i++) AmitySquare.CountStep(story);
        var sides = new List<int>();
        Assert.Equal(AmityFind.Nothing, AmitySquare.Find(story, "Pikachu", fashionCase, Dice(sides), out _));
        Assert.Empty(sides);
        Assert.Equal(199, AmitySquare.Steps(story));

        // The two hundredth: what was decided ahead, the count over again and the next find decided (a berry)
        AmitySquare.CountStep(story);
        Assert.Equal(AmityFind.Accessory, AmitySquare.Find(story, "Pikachu", fashionCase, Dice(sides, 0, 50, 20), out string found));
        Assert.Equal("Orange Fluff", found);
        Assert.Equal(new[] { 5, 100, 100 }, sides);
        Assert.Equal(0, AmitySquare.Steps(story));
        Assert.Equal(AmityFind.Nothing, AmitySquare.Find(story, "Pikachu", fashionCase, Dice(sides), out _));
        for (int i = 0; i < 200; i++) AmitySquare.CountStep(story);
        Assert.Equal(AmityFind.Berry, AmitySquare.Find(story, "Pikachu", fashionCase, Dice(sides, 0, 0, 0), out found));
        Assert.Equal("Cornn Berry", found);

        // SystemVars_IncrementAmitySquareStepCount stops at ten thousand
        for (int i = 0; i < 10_500; i++) AmitySquare.CountStep(story);
        Assert.Equal(10_000, AmitySquare.Steps(story));
    }

    [Fact]
    public void WhatDoesNotFitGivesWayToTheOtherKind()
    {
        // AmitySquare_FollowerMon_PickUp: the kind decided first, the other when it can't be put away, and with room
        // for neither only a mood
        StoryState Ready(int kind, string accessory)
        {
            var story = new StoryState();
            story.SetVar(AmitySquare.NextKindVar, kind);
            story.SetVar(AmitySquare.NextAccessoryVar, SuperContestDatabase.Accessory(accessory));
            story.SetVar(AmitySquare.NextBerryVar, 3);
            story.SetVar(AmitySquare.StepsVar, AmitySquare.StepsToFind);
            return story;
        }
        var full = new FashionCase();
        Assert.True(full.Add("Orange Fluff", FashionCase.MostOfAKind));
        Assert.False(full.CanFit(SuperContestDatabase.Accessory("Orange Fluff")));
        var die = Dice(new List<int>());

        Assert.Equal(AmityFind.Berry, AmitySquare.Find(Ready(1, "Orange Fluff"), "Pikachu", full, die, out string found));
        Assert.Equal("Nomel Berry", found);
        Assert.Equal(AmityFind.Accessory, AmitySquare.Find(Ready(0, "Brown Fluff"), "Pikachu", full, die, out found, bagHasRoom: false));
        Assert.Equal("Brown Fluff", found);
        Assert.Equal(AmityFind.Nothing, AmitySquare.Find(Ready(0, "Orange Fluff"), "Pikachu", full, die, out _, bagHasRoom: false));
        Assert.Equal(AmityFind.Nothing, AmitySquare.Find(Ready(1, "Orange Fluff"), "Pikachu", full, die, out _, bagHasRoom: false));
        // And the count starts again whichever it was
        var spent = Ready(1, "Orange Fluff");
        AmitySquare.Find(spent, "Pikachu", full, die, out _, bagHasRoom: false);
        Assert.Equal(0, AmitySquare.Steps(spent));
    }

    [Fact]
    public void TheManOfTheParkHasSixteenGiftsAndFivePlaces()
    {
        // sBerryAndAccessoryManOptions: nine berries, seven accessories; LCRNG % 16, then GetRandom 5 for his place
        Assert.Equal(16, AmitySquare.Gifts.Count);
        Assert.Equal("Magost Berry", AmitySquare.Gifts[0]);
        Assert.Equal("Belue Berry", AmitySquare.Gifts[8]);
        Assert.Equal("Mini Pebble", AmitySquare.Gifts[9]);
        Assert.Equal("Stump", AmitySquare.Gifts[15]);
        Assert.False(AmitySquare.GiftIsAccessory(8));
        Assert.True(AmitySquare.GiftIsAccessory(9));

        var story = new StoryState();
        var sides = new List<int>();
        AmitySquare.Arrive(story, Dice(sides, 15, 3));
        Assert.Equal(new[] { 16, 5 }, sides);
        Assert.Equal(15, story.Var(AmitySquare.GiftVar));
        Assert.Equal((48, 41), AmitySquare.GiftManPlaces[story.Var(AmitySquare.GiftManPlaceVar)]);

        // Every place he may stand is open ground of the square
        var square = Square();
        Assert.All(AmitySquare.GiftManPlaces, at => Assert.False(square.IsSolid(at.X, at.Y), $"{at}"));
    }

    // ------------------------------------------------------------------ the ruins' huts

    /// <summary>
    /// The original's twenty-seven warps: the tile of each coordinate event (events_amity_square.json, scripts 18 to
    /// 44) and where its movement walks the unseen player to (AmitySquare_Movement_PlayerGoToDestinationWarp1 to 27).
    /// </summary>
    private static readonly (int X, int Y, int ToX, int ToY)[] Warps =
    {
        (17, 17, 35, 17), (16, 18, 35, 17), (18, 18, 35, 17),   // east 18; east 19 north; east 17 north
        (35, 16, 17, 18), (34, 17, 17, 18), (36, 17, 17, 18),   // west 18 south 2; west 17 south; west 19 south
        (52, 26, 47, 10), (51, 27, 33, 9), (53, 27, 15, 12),    // west 5 north 16; west 18 north 18; west 38 north 15
        (47, 9, 52, 27), (46, 10, 15, 12), (48, 10, 33, 9),     // east 5 south 18; west 31 south 2; west 15 north
        (15, 11, 52, 27), (14, 12, 26, 11), (16, 12, 41, 18),   // east 37 south 16; east 12 north; east 25 south 6
        (33, 8, 52, 27), (32, 9, 41, 18), (34, 9, 26, 11),      // east 19 south 19; east 9 south 9; west 8 south 2
        (26, 10, 47, 10), (25, 11, 47, 36), (27, 11, 15, 12),   // east 21; east 22 south 25; west 12 south
        (41, 17, 47, 10), (40, 18, 33, 9), (42, 18, 47, 36),    // east 6 north 7; west 7 north 9; east 5 south 18
        (47, 35, 52, 27), (46, 36, 15, 12), (48, 36, 33, 9),    // east 5 north 8; west 31 north 24; west 15 north 27
    };

    [Fact]
    public void TheHutsLeadWhereTheOriginalsDo()
    {
        Assert.Equal(9, AmitySquare.Huts.Count);
        for (int warp = 0; warp < Warps.Length; warp++)
        {
            var (x, y, toX, toY) = Warps[warp];
            Assert.Equal(warp + 1, AmitySquare.WarpAt(x, y));
            var hut = AmitySquare.Leading(x, y);
            Assert.True(hut != null && (hut.Value.X, hut.Value.Y) == (toX, toY), $"warp {warp + 1} at ({x},{y}) leads to {hut}, not ({toX},{toY})");
        }
        // No other tile carries anybody: not a hut's middle, nor its door
        Assert.All(AmitySquare.Huts, hut =>
        {
            Assert.Null(AmitySquare.Leading(hut.X, hut.Y));
            Assert.Null(AmitySquare.Leading(hut.Door.X, hut.Door.Y));
            Assert.Equal(0, AmitySquare.WarpAt(hut.X, hut.Y));
        });
    }

    [Fact]
    public void TheSquaresTriggersAreTheOriginalsEachWithItsScript()
    {
        var square = Square();
        // The two gates: the question with no Pokémon out, the goodbye with one (scripts 2 to 5)
        foreach (var (x, y, value, script) in new[] { (12, 46, 0, "WestGate"), (12, 47, 1, "LeaveWest"), (51, 46, 0, "EastGate"), (51, 47, 1, "LeaveEast") })
        {
            var trigger = Assert.Single(square.TriggersAt(x, y));
            Assert.Equal((script, AmitySquare.ActiveVar, value), (trigger.Script, trigger.Variable, trigger.Value));
        }
        // The huts': each tile's script puts the player in the middle of the hut the tile leads to, facing out of its
        // door, and walks them out of it
        foreach (var (x, y, toX, toY) in Warps)
        {
            var trigger = Assert.Single(square.TriggersAt(x, y));
            Assert.Equal("Hut" + AmitySquare.WarpAt(x, y), trigger.Script);
            var script = ScriptLibrary.Default.Find(trigger.Script, Place);
            Assert.NotNull(script);
            var call = Assert.Single(script!.Everything(), i => i.Op == Op.Call);
            var carried = ScriptLibrary.Default.Find(call.Name, Place)!.Everything().ToList();
            var put = Assert.Single(carried, i => i.Op == Op.Place);
            Assert.Equal(("player", toX, toY), (put.Name, put.X, put.Y));
            Assert.Contains(carried, i => i.Op == Op.Walk && i.Name == "player");
        }
        Assert.Equal(4 + Warps.Length, square.Triggers.Count);
        // Every one of them has a script the square's file holds
        Assert.All(square.Triggers, t => Assert.NotNull(ScriptLibrary.Default.Find(t.Script, Place)));
    }

    [Fact]
    public void AHutIsWalkedIntoFromItsDoorAndOutOfItAgain()
    {
        var square = Square();
        foreach (var hut in AmitySquare.Huts)
        {
            // The door, the middle and the three tiles that carry are open; the door is come to from the south
            foreach (var (x, y) in new[] { hut.Door, (hut.X, hut.Y), (hut.X, hut.Y - 1), (hut.X - 1, hut.Y), (hut.X + 1, hut.Y), (hut.Door.X, hut.Door.Y + 1) })
                Assert.True(square.IsWalkable(x, y), $"hut {hut.Name}: ({x},{y}) is shut");
            // And the door is the only way out of the middle that carries nobody
            Assert.False(square.IsWalkable(hut.Door.X - 1, hut.Door.Y), $"hut {hut.Name} has a gap west of its door");
            Assert.False(square.IsWalkable(hut.Door.X + 1, hut.Door.Y), $"hut {hut.Name} has a gap east of its door");
            // A step out of the door is a step, not a cliff
            var step = FieldMovement.Step(square, hut.X, hut.Y, Direction.Down, new Walker(Height: square.HeightAt(hut.X, hut.Y)));
            Assert.Equal(StepKind.Walk, step.Kind);
        }
    }

    /// <summary>The tiles walked to from a tile on foot, never onto a tile that carries the walker away.</summary>
    private static HashSet<(int X, int Y)> Reach(Map map, int fromX, int fromY, Func<int, int, bool>? stop = null)
    {
        var seen = new HashSet<(int X, int Y)> { (fromX, fromY) };
        var queue = new Queue<(int X, int Y)>(seen);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (stop?.Invoke(x, y) == true) continue;
            foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var step = FieldMovement.Step(map, x, y, way, new Walker(Height: map.HeightAt(x, y)));
                // (An item's ball is picked up and a person walked round: neither shuts a way for good)
                bool passable = step.Moves || step.Obstacle == Obstacle.Person;
                var (dx, dy) = FieldMovement.Delta(way);
                var next = step.Moves ? (step.X, step.Y) : (x + dx, y + dy);
                if (passable && !map.IsSolid(next.Item1, next.Item2) && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return seen;
    }

    [Fact]
    public void NobodyComesPastTheCountersWithoutBeingAsked()
    {
        // The lane before each counter is the one way from a gate's door into the square, so the tile that asks is
        // stepped on by everyone who comes in
        var square = Square();
        foreach (var (doorX, lane) in new[] { (10, 12), (53, 51) })
        {
            var lobby = Reach(square, doorX, 50, (x, y) => (x, y) == (lane, 46));
            Assert.Contains((lane, 47), lobby);
            Assert.Contains((lane, 46), lobby);
            Assert.DoesNotContain((lane, 45), lobby);
            Assert.DoesNotContain((30, 44), lobby);
        }
    }

    [Fact]
    public void TheRaisedGroundIsComeToThroughTheHuts()
    {
        // The square is two halves. On foot from the west counter: the lawns and the two huts up the stairs, which
        // lead only to one another. On foot from the east counter: up the long slope to the two huts of the east, and
        // no further. Everything else is behind the huts' ways, and all of that is the east gate's
        var square = Square();
        (int X, int Y) Door(char name) => AmitySquare.Huts.First(h => h.Name == name).Door;
        bool Carries(int x, int y) => AmitySquare.WarpAt(x, y) > 0;
        var west = Reach(square, 12, 45, Carries);
        var east = Reach(square, 51, 45, Carries);
        foreach (char name in "AB") Assert.Contains(Door(name), west);
        foreach (char name in "CDEFGHI") Assert.DoesNotContain(Door(name), west);
        foreach (char name in "CD") Assert.Contains(Door(name), east);
        foreach (char name in "ABEFGHI") Assert.DoesNotContain(Door(name), east);
        Assert.DoesNotContain(east, tile => west.Contains(tile));

        // With them: every hut's door, the Spooky Plate, the woman and her Happiny, and wherever the man may stand
        var all = new HashSet<(int X, int Y)>();
        var doors = new Queue<(int X, int Y)>(new[] { (12, 45), (51, 45) });
        while (doors.Count > 0)
        {
            var from = doors.Dequeue();
            foreach (var tile in Reach(square, from.X, from.Y, Carries))
            {
                if (!all.Add(tile)) continue;
                if (AmitySquare.Leading(tile.X, tile.Y) is { } hut && !all.Contains((hut.X, hut.Y))) doors.Enqueue((hut.X, hut.Y));
            }
        }
        Assert.All(AmitySquare.Huts, hut => Assert.Contains(hut.Door, all));
        bool Beside(NPC who) => new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }.Any(d => all.Contains((who.GridX + d.Item1, who.GridY + d.Item2)));
        foreach (string key in new[] { "item_spooky_plate", "item_amulet_coin", "item_tm45", "item_tm43", "pokemon_breeder_f", "happiny", "gift_man" })
            Assert.True(Beside(square.Everyone.First(n => n.Key == key)), $"{key} can't be come to");
        Assert.All(AmitySquare.GiftManPlaces, at => Assert.Contains(at, all));
    }

    // ------------------------------------------------------------------ the follower

    [Fact]
    public void APokemonOutOfItsBallIsTheSameFollowerAPartnerIs()
    {
        // One kind of follower: a person who battles beside the player, or a Pokémon of the team, which only walks
        var square = Square();
        var body = square.Everyone.First(n => n.Key == AmitySquare.Body);
        Assert.True(body.IsPokemon);
        Assert.Equal(AmitySquare.HideFlag, body.HiddenBy);
        var infernape = Mon("Infernape", nickname: "Blaze");
        var walking = new Follower(body, infernape);
        Assert.Same(infernape, walking.Pokemon);
        Assert.Null(walking.TrainerId);
        Assert.Equal(("Infernape", "Blaze", NPC.PokemonType), (body.Species, body.Name, body.NpcType));

        var cheryl = new Follower(new NPC { Name = "Cheryl" }, "cheryl_eterna_forest");
        Assert.Null(cheryl.Pokemon);
        Assert.Equal("cheryl_eterna_forest", cheryl.TrainerId);
    }

    [Fact]
    public void ItKeepsAStepBehindThroughEveryTurnOfTheSquare()
    {
        // From the west counter up to the square, round a bed, up the stairs to a hut, through it to the next hut and
        // out: after every step the Pokémon stands on the tile the player has just left, which is open ground
        var square = Square();
        var body = square.Everyone.First(n => n.Key == AmitySquare.Body);
        var follower = new Follower(body, Mon("Pikachu"));
        square.Follower = body;
        var (x, y) = (12, 46);
        follower.Behind(x, y, Direction.Up);
        Assert.Equal((12, 47), (body.GridX, body.GridY));

        void Settle()
        {
            for (int i = 0; i < 600 && follower.Moving; i++) follower.Update(1f / 60f, NpcWalk.WalkPace);
            Assert.False(follower.Moving);
        }
        int steps = 0;
        void Walk(Direction way, int tiles)
        {
            for (int i = 0; i < tiles; i++)
            {
                var step = FieldMovement.Step(square, x, y, way, new Walker(Height: square.HeightAt(x, y)));
                Assert.True(step.Kind == StepKind.Walk, $"the step {way} from ({x},{y}) is {step.Kind} {step.Obstacle}");
                follower.PlayerLeft(x, y);
                var left = (x, y);
                (x, y) = (step.X, step.Y);
                Settle();
                Assert.Equal(left, (body.GridX, body.GridY));
                Assert.False(square.IsSolid(body.GridX, body.GridY));
                steps++;
            }
        }
        // To hut A: north out of the lane, east along the lawn, up the stairs onto its ground and in at its door
        var a = AmitySquare.Huts.First(h => h.Name == 'A');
        var route = Route(square, (x, y), a.Door);
        foreach (var way in route) Walk(way, 1);
        Walk(Direction.Up, 1);
        Assert.Equal((a.X, a.Y), (x, y));
        Assert.Equal(a.Door, (body.GridX, body.GridY));

        // The step north carries the player to hut B's middle, facing out: the Pokémon is behind them, on the tile
        // that carries (it carries nobody but the player), and follows them out of the door
        var b = AmitySquare.Leading(a.X, a.Y - 1)!.Value;
        (x, y) = (b.X, b.Y);
        follower.Behind(x, y, Direction.Down);
        Assert.Equal((b.X, b.Y - 1), (body.GridX, body.GridY));
        Assert.False(square.IsSolid(body.GridX, body.GridY));
        Walk(Direction.Down, 2);
        Assert.Equal(b.Door, (body.GridX, body.GridY));
        Assert.True(steps > 20, $"only {steps} steps were walked");
    }

    [Fact]
    public void ATallOneKeepsTwoTilesBehindAndNeverTakesThePlayersTile()
    {
        // A Pokémon that would hide the player from a tile behind them keeps a tile further back (Follower.Gap), and
        // still makes way when the player turns round and walks into it
        var square = Square();
        var body = square.Everyone.First(n => n.Key == AmitySquare.Body);
        var follower = new Follower(body, Mon("Torterra", 40)) { Gap = 2 };
        square.Follower = body;
        int x = 16;
        const int y = 41;
        follower.Behind(x, y, Direction.Right);
        Assert.Equal((14, y), (body.GridX, body.GridY));

        void Step(int dx)
        {
            follower.PlayerLeft(x, y, (x + dx, y));
            x += dx;
            for (int i = 0; i < 600 && follower.Moving; i++) follower.Update(1f / 60f, NpcWalk.WalkPace);
            Assert.NotEqual(x, body.GridX);
            Assert.False(square.IsSolid(body.GridX, body.GridY));
        }
        for (int i = 0; i < 3; i++)
        {
            Step(1);
            Assert.Equal(x - 2, body.GridX);
        }
        // Turned back: the player steps onto the tile between them and the Pokémon waits; onto its own tile, and the
        // two change places, after which it is two tiles behind again the other way
        Step(-1);
        Assert.Equal((18, 17), (x, body.GridX));
        Step(-1);
        Assert.Equal((17, 19), (x, body.GridX));
        Step(-1);
        Assert.Equal((16, 18), (x, body.GridX));
        Assert.Equal(y, body.GridY);

        // With no room for two tiles it is put one behind, as whoever places it decides
        follower.Behind(17, 18, Direction.Down, tiles: 1);
        Assert.Equal((17, 17), (body.GridX, body.GridY));
    }

    /// <summary>A way on foot between two tiles of a map, by the field's own steps.</summary>
    private static List<Direction> Route(Map map, (int X, int Y) from, (int X, int Y) to)
    {
        var came = new Dictionary<(int X, int Y), ((int X, int Y) From, Direction Way)>();
        var queue = new Queue<(int X, int Y)>(new[] { from });
        var seen = new HashSet<(int X, int Y)> { from };
        while (queue.Count > 0)
        {
            var at = queue.Dequeue();
            if (at == to) break;
            foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var step = FieldMovement.Step(map, at.X, at.Y, way, new Walker(Height: map.HeightAt(at.X, at.Y)));
                if (step.Kind != StepKind.Walk || AmitySquare.WarpAt(step.X, step.Y) > 0 || !seen.Add((step.X, step.Y))) continue;
                came[(step.X, step.Y)] = (at, way);
                queue.Enqueue((step.X, step.Y));
            }
        }
        Assert.True(came.ContainsKey(to), $"no way on foot from {from} to {to}");
        var route = new List<Direction>();
        for (var at = to; at != from; at = came[at].From) route.Add(came[at].Way);
        route.Reverse();
        return route;
    }

    // ------------------------------------------------------------------ the scripts

    [Fact]
    public void TheReceptionistLetsOutThePokemonThePlayerSaysYesTo()
    {
        var square = Square();
        var host = Host(square, Team("Bidoof", "Pikachu", "Turtwig"), 12, 46);
        host.Party.Members[0].CurrentHP = 1;
        // No to the Pikachu, yes to the Turtwig; then the walk's three dice (a berry, any accessory, a Magost Berry)
        host.Answers.Enqueue(1);
        host.Answers.Enqueue(0);
        foreach (int roll in new[] { 0, 0, 0 }) host.Rolls.Enqueue(roll);
        Run("WestGate", host);

        Assert.Equal(2, host.Asked.Count);
        Assert.Contains("Pikachu", host.Asked[0].Question);
        Assert.Contains("Turtwig", host.Asked[1].Question);
        Assert.Same(host.Party.Members[2], host.Strolling);
        var body = square.NPCs.Single(n => n.Key == AmitySquare.Body);
        Assert.Same(body, host.StrollingAs);
        Assert.Equal("Turtwig", body.Species);
        Assert.Equal((12, 47), (body.GridX, body.GridY));
        Assert.Equal((12, 45), host.PlayerTile);
        Assert.Equal(1, host.Story.Var(AmitySquare.ActiveVar));
        Assert.Equal(2, host.Story.Var(AmitySquare.PartyVar));
        Assert.Equal(PokemonDatabase.Get("Turtwig")!.DexNumber, host.Story.Var(AmitySquare.SpeciesVar));
        Assert.False(host.Story.Has(AmitySquare.HideFlag));
        Assert.Equal(0, host.Story.Var(AmitySquare.NextKindVar));
        // HealParty as the Pokémon comes out
        Assert.Equal(host.Party.Members[0].MaxHP, host.Party.Members[0].CurrentHP);
        Assert.True(Said(host, "Turtwig"));
        Assert.Empty(host.Problems);
    }

    [Fact]
    public void WhoeverBringsNoneOfTheSpeciesIsToldWhichAndTurnedBack()
    {
        var square = Square();
        var host = Host(square, Team("Bidoof", "Skitty"), 51, 46);
        Run("EastGate", host);
        Assert.Empty(host.Asked);
        Assert.True(Said(host, "Pikachu, Clefairy, Psyduck"));
        Assert.False(Said(host, "Skitty"));
        Assert.Equal((51, 47), host.PlayerTile);
        Assert.Null(host.Strolling);
        Assert.Equal(0, host.Story.Var(AmitySquare.ActiveVar));
        Assert.DoesNotContain(square.NPCs, n => n.Key == AmitySquare.Body);

        // With the National Pokédex the list is longer, and the Skitty may walk
        var later = Host(Square(), Team("Bidoof"), 51, 46);
        later.NationalDex = true;
        Run("EastGate", later);
        Assert.True(Said(later, "Jigglypuff, Torchic, Skitty and Shroomish"));
        var withSkitty = Host(Square(), Team("Bidoof", "Skitty"), 51, 46);
        withSkitty.NationalDex = true;
        Run("EastGate", withSkitty);
        Assert.Equal("Skitty", withSkitty.Strolling?.Species.Name);
        Assert.Equal((51, 47), (withSkitty.StrollingAs!.GridX, withSkitty.StrollingAs.GridY));
        Assert.Equal((51, 45), withSkitty.PlayerTile);

        // Saying no to every one of them is being asked to come back
        var shy = Host(Square(), Team("Pikachu", "Piplup"), 12, 46);
        shy.Answers.Enqueue(1);
        shy.Answers.Enqueue(1);
        Run("WestGate", shy);
        Assert.Equal(2, shy.Asked.Count);
        Assert.True(Said(shy, "come back"));
        Assert.Equal((12, 47), shy.PlayerTile);
        Assert.Null(shy.Strolling);
    }

    [Fact]
    public void LeavingPutsThePokemonBackInItsBall()
    {
        var square = Square();
        var host = Host(square, Team("Pikachu"), 12, 46);
        Run("WestGate", host);
        Assert.NotNull(host.Strolling);
        host.PlayerTile = (12, 47);
        Run("LeaveWest", host);
        Assert.Null(host.Strolling);
        Assert.Equal(0, host.Story.Var(AmitySquare.ActiveVar));
        Assert.True(host.Story.Has(AmitySquare.HideFlag));
        Assert.DoesNotContain(square.NPCs, n => n.Key == AmitySquare.Body);
        Assert.Equal((12, 48), host.PlayerTile);
        Assert.Empty(host.Problems);
    }

    [Fact]
    public void SpeakingToItTellsItsMoodOrTurnsSomethingUp()
    {
        var square = Square();
        var host = Host(square, Team("Pikachu"), 12, 46);
        // The walk's three dice: an accessory, the row's rarest, a Belue Berry
        foreach (int roll in new[] { 1, 99, 99 }) host.Rolls.Enqueue(roll);
        Run("WestGate", host);

        // Too few steps: a mood, the fourth of nine
        host.Rolls.Enqueue(3);
        Run("FollowerMon", host, AmitySquare.Body);
        Assert.True(Said(host, "Pikachu is gazing up at you fondly."));
        Assert.Contains(host.Log, l => l.StartsWith("cry") && l.Contains("Pikachu"));
        Assert.True(host.FashionCase.IsEmpty);

        // Two hundred steps on: the Purple Scale decided as the walk began, and the next find decided (a berry)
        host.Story.SetVar(AmitySquare.StepsVar, 200);
        foreach (int roll in new[] { 0, 0, 20 }) host.Rolls.Enqueue(roll);
        Run("FollowerMon", host, AmitySquare.Body);
        Assert.True(Said(host, "Pikachu is holding something"));
        Assert.Equal(1, host.FashionCase.Count(SuperContestDatabase.Accessory("Purple Scale")));
        Assert.True(Said(host, "received the Purple Scale"));
        Assert.Equal(0, AmitySquare.Steps(host.Story));

        host.Story.SetVar(AmitySquare.StepsVar, 200);
        Run("FollowerMon", host, AmitySquare.Body);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Cornn Berry")!));
        Assert.True(Said(host, "received the Cornn Berry"));

        // A starter grown up has moods of its own
        var grown = Host(Square(), Team("Empoleon"), 12, 46);
        Run("WestGate", grown);
        grown.Rolls.Enqueue(6);
        Run("FollowerMon", grown, AmitySquare.Body);
        Assert.True(Said(grown, "Empoleon gave a long, contented yawn."));
    }

    [Fact]
    public void EveryMoodOfBothKindsHasItsLine()
    {
        foreach (string species in new[] { "Clefairy", "Torterra" })
            for (int mood = 0; mood < 9; mood++)
            {
                var host = Host(Square(), Team(species), 12, 46);
                Run("WestGate", host);
                int before = host.Transcript.Count;
                host.Rolls.Enqueue(mood);
                Run("FollowerMon", host, AmitySquare.Body);
                Assert.True(host.Transcript.Count == before + 1 && host.Transcript[^1].Text.StartsWith(species), $"{species}, mood {mood}");
            }
        // And the receptionist's six words for whoever stops by with one
        for (int word = 0; word < 6; word++)
        {
            var host = Host(Square(), Team("Buneary"), 12, 46);
            Run("WestGate", host);
            int before = host.Transcript.Count;
            host.Rolls.Enqueue(word);
            Run("ReceptionistWest", host, "receptionist_west");
            Assert.Equal(before + 1, host.Transcript.Count);
            Assert.Equal(Direction.Right, host.Map!.Everyone.First(n => n.Key == "receptionist_west").Facing);
        }
    }

    [Fact]
    public void TheManOfTheParkGivesOnceADay()
    {
        var square = Square();
        var host = Host(square, Team("Pikachu"), 20, 14);
        // The square is come into: the fourth berry, and the man where the plan has him
        host.Rolls.Enqueue(3);
        host.Rolls.Enqueue(4);
        Run(ScriptLibrary.OnEnter, host);
        var man = square.Everyone.First(n => n.Key == "gift_man");
        Assert.Equal((20, 13), (man.GridX, man.GridY));
        Assert.True(host.Story.Has(AmitySquare.HideFlag));

        Run("GiftMan", host, "gift_man");
        Assert.Contains("Nomel Berry", host.Asked.Single().Question);
        Assert.Equal(AmitySquare.GiftBerries, host.Bag.GetQuantity(ItemDatabase.Get("Nomel Berry")!));
        Assert.True(host.Story.Has(AmitySquare.GiftFlag));
        Run("GiftMan", host, "gift_man");
        Assert.Single(host.Asked);
        Assert.Equal(AmitySquare.GiftBerries, host.Bag.GetQuantity(ItemDatabase.Get("Nomel Berry")!));

        // Another day, another place and an accessory; turned down, it is still there to be had
        host.Story.ClearDaily();
        host.Rolls.Enqueue(15);
        host.Rolls.Enqueue(1);
        Run(ScriptLibrary.OnEnter, host);
        Assert.Equal((38, 8), (man.GridX, man.GridY));
        host.Answers.Enqueue(1);
        Run("GiftMan", host, "gift_man");
        Assert.False(host.Story.Has(AmitySquare.GiftFlag));
        Run("GiftMan", host, "gift_man");
        Assert.Equal(1, host.FashionCase.Count(SuperContestDatabase.Accessory("Stump")));
        Assert.True(host.Story.Has(AmitySquare.GiftFlag));
        Assert.Empty(host.Problems);
    }

    [Fact]
    public void AGameTakenUpAgainOnAWalkKeepsItsPokemonOut()
    {
        // The square's arrival script hides the Pokémon and draws the day afresh, but not in the middle of a walk: the
        // story's variables are the whole of what a save keeps of it
        var square = Square();
        var host = Host(square, Team("Bidoof", "Pachirisu"), 12, 46);
        Run("WestGate", host);
        int gift = host.Story.Var(AmitySquare.GiftVar);
        host.Rolls.Enqueue(9);
        Run(ScriptLibrary.OnEnter, host);
        Assert.False(host.Story.Has(AmitySquare.HideFlag));
        Assert.Equal(gift, host.Story.Var(AmitySquare.GiftVar));
        Assert.Contains(square.NPCs, n => n.Key == AmitySquare.Body);
        Assert.Same(host.Party.Members[1], AmitySquare.Walker(host.Story, host.Party));
        // Nobody walks with a place that is empty, or an Egg's
        host.Story.SetVar(AmitySquare.PartyVar, 5);
        Assert.Null(AmitySquare.Walker(host.Story, host.Party));
    }

    [Fact]
    public void TheSquaresPeopleStandWhereTheOriginalsDo()
    {
        var square = Square();
        foreach (var (key, x, y) in new[]
        {
            ("receptionist_west", 10, 46), ("receptionist_east", 53, 46), ("pokemon_breeder_m", 40, 26), ("drifloon", 41, 26),
            ("happiny", 37, 12), ("pokemon_breeder_f", 36, 12), ("collector", 17, 33), ("pikachu", 16, 33), ("twin", 47, 30),
            ("clefairy", 48, 30), ("gift_man", 20, 13), ("item_spooky_plate", 45, 42), ("item_amulet_coin", 41, 9),
        })
        {
            var who = square.Everyone.FirstOrDefault(n => n.Key == key);
            Assert.True(who != null && (who.GridX, who.GridY) == (x, y), $"{key} is not at ({x},{y})");
        }
        // Each receptionist is spoken to over her counter, from the lane
        Assert.True(square.IsCounter(11, 46));
        Assert.True(square.IsCounter(52, 46));
        Assert.Null(Sinnoh.Overlay(Place)!.HeldBack);
    }

    private static World Sinnoh => World.LoadAll().Single(w => w.Index.Region == "Sinnoh");

    // ------------------------------------------------------------------ beside it: a partner on the platform

    [Fact]
    public void APartnerStandsOnThePlatformBesideThePlayer()
    {
        // A tag battle's opening shows both of them, and both step off as the Pokémon are sent out
        Pokemon Of(string species) => new(PokemonDatabase.Get(species)!, 30);
        var party = new Party();
        party.Add(Of("Bidoof"));
        var marley = new Trainer { Id = "marley", Name = "Marley", TrainerClass = "Pokémon Trainer", Look = "Marley" };
        marley.Party.Add(Of("Arcanine"));
        var grunts = new List<Trainer>();
        foreach (string name in new[] { "A", "B" })
        {
            var grunt = new Trainer { Id = "grunt_" + name, Name = name, TrainerClass = "Galactic Grunt" };
            grunt.Party.Add(Of("Glameow"));
            grunts.Add(grunt);
        }
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = grunts, Partner = marley,
            Format = BattleFormat.Double, Random = new PokemonPlatinumEngine.Battle.Sim.BattleRandom(20261011), Rules = Ruleset.Platinum
        });
        Assert.Equal(CharacterStyles.Default.InBattle("Pokémon Trainer", "Marley"), battle.Anim.PartnerTrainer);
        Assert.NotNull(battle.Anim.PlayerTrainer);

        // Alone, nobody stands there
        var alone = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = { Of("Starly") },
            Random = new PokemonPlatinumEngine.Battle.Sim.BattleRandom(20261011), Rules = Ruleset.Platinum
        });
        Assert.Null(alone.Anim.PartnerTrainer);

        // The player's Pokémon sent out, both are gone once the player has stepped off
        battle.Anim.SendOut(BattleSide.Player, party.Members[0]);
        for (int i = 0; i < 240; i++) battle.Anim.Update(1f / 60f, (_, _) => null);
        Assert.Null(battle.Anim.PlayerTrainer);
        Assert.Null(battle.Anim.PartnerTrainer);
    }
}
