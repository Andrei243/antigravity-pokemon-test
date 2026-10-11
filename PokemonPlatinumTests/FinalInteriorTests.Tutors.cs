using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>The tutors' and the two move men's houses of plan 01 · M11, part d, and the rule they teach by (<see cref="MoveTutors"/>).</summary>
public partial class FinalInteriorTests
{
    /// <summary>Each house door part d opened: the area, its warp's number, the room, and the mat inside it.</summary>
    public static IEnumerable<object[]> HouseDoors => new[]
    {
        new object[] { "route_210_north", 0, "Route210GrandmaWilmaHouse", 4, 8 },
        new object[] { "route_212_south", 0, "Route212House", 4, 8 },
        new object[] { "pastoria_city", 4, "PastoriaCityEastHouse", 4, 8 },
        new object[] { "canalave_city", 9, "CanalaveCityEastHouse", 4, 8 },
        new object[] { "snowpoint_city", 1, "SnowpointCityEastHouse", 4, 8 },
        new object[] { "survival_area", 2, "SurvivalAreaNorthHouse", 4, 8 },
        new object[] { "route_228", 2, "Route228NorthHouse", 4, 8 },
    };

    [Theory]
    [MemberData(nameof(HouseDoors))]
    public void ATutorsDoorLeadsOntoItsRoomsMatAndTheRoomBackOutBeforeTheDoor(string area, int warp, string room, int matX, int matY)
    {
        var way = Sinnoh.Area(area)!.Warps[warp];
        var outside = BuiltMaps.Value.Values.First(m => m.FindArea(area) != null);
        var door = outside.GetWarpAt(way.X, way.Z);
        Assert.NotNull(door);
        Assert.Equal((room, matX, matY, Direction.Up), (door!.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        Assert.DoesNotContain(warp, Sinnoh.Overlay(area)!.Locked ?? new());

        var inside = Room(room);
        Assert.Equal(InteriorStyle.House, inside.Interior);
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(room)!.Id);
        Assert.True(inside.IsWalkable(matX, matY), $"{room}: the mat is no floor");
        var exit = inside.GetWarpAt(matX, matY + 1);
        Assert.NotNull(exit);
        Assert.Equal((outside.Name, way.X, way.Z + 1, Direction.Down), (exit!.TargetMap, exit.TargetX, exit.TargetY, exit.TargetFacing));
        Assert.True(outside.IsWalkable(way.X, way.Z + 1), $"{room}: nobody can stand before its door");
        Assert.Equal(outside.BgmTrackAt(way.X, way.Z + 1), inside.BgmTrack);
        // Everyone inside stands on floor with something to say
        foreach (var who in inside.Everyone)
        {
            Assert.False(inside.IsSolid(who.GridX, who.GridY), $"{room}: {who.Key} stands in something solid");
            Assert.True(who.DialogLines.Count > 0 || who.Script != null && ScriptLibrary.Default.Find(who.Script, room) != null, $"{room}: {who.Key} has nothing to say");
        }
        Assert.DoesNotContain(ScriptLibrary.Default.Problems().ToList(), p => p.Contains(room + ".txt", StringComparison.Ordinal));
    }

    // ------------------------------------------------------------------ the rule

    [Fact]
    public void TheShardTutorsTeachTheOriginalsMovesAtItsPrices()
    {
        // res/pokemon/move_tutors.json of the decompilation: thirteen moves on Route 212, seventeen in the Survival
        // Area and eight in Snowpoint City, every one costing eight Shards in all
        Assert.Equal(new[] { MoveTutors.Route212, MoveTutors.Snowpoint, MoveTutors.SurvivalArea }, MoveTutors.Places.Order());
        Assert.Equal((13, 17, 8), (MoveTutors.At(MoveTutors.Route212).Count, MoveTutors.At(MoveTutors.SurvivalArea).Count, MoveTutors.At(MoveTutors.Snowpoint).Count));
        var all = MoveTutors.Places.SelectMany(MoveTutors.At).ToList();
        Assert.Equal(38, all.Select(m => m.Move).Distinct().Count());
        Assert.All(all, m => Assert.NotNull(MoveDatabase.Get(m.Move)));
        Assert.All(all, m => Assert.Equal(8, m.Red + m.Blue + m.Yellow + m.Green));
        TutorMove Of(string place, string move) => MoveTutors.At(place).Single(m => m.Move == move);
        Assert.Equal(new TutorMove("Dive", 2, 4, 2, 0), Of(MoveTutors.Route212, "Dive"));
        Assert.Equal(new TutorMove("Sucker Punch", 0, 6, 2, 0), Of(MoveTutors.Route212, "Sucker Punch"));
        Assert.Equal(new TutorMove("Superpower", 8, 0, 0, 0), Of(MoveTutors.SurvivalArea, "Superpower"));
        Assert.Equal(new TutorMove("Signal Beam", 2, 2, 2, 2), Of(MoveTutors.SurvivalArea, "Signal Beam"));
        Assert.Equal(new TutorMove("Last Resort", 0, 0, 0, 8), Of(MoveTutors.Snowpoint, "Last Resort"));
        Assert.Equal("2 Red, 4 Blue, 2 Yellow", Of(MoveTutors.Route212, "Dive").Price);
        // Each tutor has a colour: blue on Route 212, red in the Survival Area, yellow and green in Snowpoint City
        Assert.True(MoveTutors.At(MoveTutors.Route212).Sum(m => m.Blue) > MoveTutors.At(MoveTutors.Route212).Sum(m => m.Red + m.Yellow + m.Green));
        Assert.True(MoveTutors.At(MoveTutors.SurvivalArea).Sum(m => m.Red) > MoveTutors.At(MoveTutors.SurvivalArea).Sum(m => m.Blue + m.Yellow + m.Green));
        Assert.True(MoveTutors.At(MoveTutors.Snowpoint).Sum(m => m.Yellow + m.Green) > MoveTutors.At(MoveTutors.Snowpoint).Sum(m => m.Red + m.Blue));
        // Every move a species' list names is one some tutor teaches
        var taught = all.Select(m => m.Move).ToHashSet();
        Assert.All(PokemonDatabase.GetAll().SelectMany(s => s.TutorMoves ?? new()), move => Assert.Contains(move, taught));
    }

    [Fact]
    public void ATutorTeachesWhatASpeciesListNamesAndItDoesntKnow()
    {
        // Bulbasaur's list in the original: Mud-Slap, Fury Cutter, Snore, Synthesis, Seed Bomb, Knock Off
        var bulbasaur = Mon("Bulbasaur");
        Assert.Equal(new[] { "Fury Cutter", "Knock Off" }, MoveTutors.Teachable(bulbasaur, MoveTutors.Route212).Select(m => m.Move));
        Assert.Equal(new[] { "Mud-Slap", "Seed Bomb" }, MoveTutors.Teachable(bulbasaur, MoveTutors.SurvivalArea).Select(m => m.Move));
        Assert.Equal(new[] { "Snore", "Synthesis" }, MoveTutors.Teachable(bulbasaur, MoveTutors.Snowpoint).Select(m => m.Move));
        bulbasaur.Moves.Add(MoveDatabase.Create("Snore"));
        Assert.Equal(new[] { "Synthesis" }, MoveTutors.Teachable(bulbasaur, MoveTutors.Snowpoint).Select(m => m.Move));
        bulbasaur.IsEgg = true;
        Assert.Empty(MoveTutors.Teachable(bulbasaur, MoveTutors.Route212));
        Assert.Throws<ArgumentException>(() => MoveTutors.At("jubilife_city"));

        // The Shards are all taken or none
        var bag = new Inventory();
        var dive = MoveTutors.At(MoveTutors.Route212)[0];
        bag.AddItem(ItemDatabase.Get("Red Shard")!, 2);
        bag.AddItem(ItemDatabase.Get("Blue Shard")!, 4);
        bag.AddItem(ItemDatabase.Get("Yellow Shard")!, 1);
        Assert.False(MoveTutors.CanAfford(bag, dive));
        Assert.False(MoveTutors.Pay(bag, dive));
        Assert.Equal(4, bag.GetQuantity(ItemDatabase.Get("Blue Shard")!));
        bag.AddItem(ItemDatabase.Get("Yellow Shard")!, 2);
        Assert.True(MoveTutors.Pay(bag, dive));
        Assert.Equal((0, 0, 1), (bag.GetQuantity(ItemDatabase.Get("Red Shard")!), bag.GetQuantity(ItemDatabase.Get("Blue Shard")!), bag.GetQuantity(ItemDatabase.Get("Yellow Shard")!)));
    }

    [Fact]
    public void TheTwoTutorsOfTheStrongestMovesAskTheTypeOrTheSpeciesAndFullTrust()
    {
        var gible = Mon("Gible");
        gible.Friendship = 254;
        Assert.Equal(TutorAnswer.NotClose, MoveTutors.ForDracoMeteor(gible));
        gible.Friendship = MoveTutors.FullTrust;
        Assert.Equal(TutorAnswer.Able, MoveTutors.ForDracoMeteor(gible));
        gible.Moves.Add(MoveDatabase.Create(MoveTutors.DracoMeteor));
        Assert.Equal(TutorAnswer.Knows, MoveTutors.ForDracoMeteor(gible));
        var turtwig = Mon();
        turtwig.Friendship = 255;
        Assert.Equal(TutorAnswer.NotAble, MoveTutors.ForDracoMeteor(turtwig));
        Assert.Equal(TutorAnswer.Nobody, MoveTutors.ForDracoMeteor(null));
        gible.IsEgg = true;
        Assert.Equal(TutorAnswer.Egg, MoveTutors.ForDracoMeteor(gible));

        // The twelve of the poster, each with the move of its type; nobody else, and not a starter still growing
        Assert.Equal(12, MoveTutors.UltimateSpecies.Count);
        foreach (var (species, move) in new[] { ("Charizard", "Blast Burn"), ("Infernape", "Blast Burn"), ("Empoleon", "Hydro Cannon"), ("Swampert", "Hydro Cannon"), ("Torterra", "Frenzy Plant"), ("Sceptile", "Frenzy Plant") })
        {
            var starter = Mon(species, 50);
            Assert.Equal(move, MoveTutors.UltimateMoveOf(starter));
            starter.Friendship = 255;
            Assert.Equal(TutorAnswer.Able, MoveTutors.ForUltimateMove(starter));
            starter.Friendship = 200;
            Assert.Equal(TutorAnswer.NotClose, MoveTutors.ForUltimateMove(starter));
        }
        Assert.Null(MoveTutors.UltimateMoveOf(turtwig));
        Assert.Equal(TutorAnswer.NotAble, MoveTutors.ForUltimateMove(turtwig));
        Assert.Equal(TutorAnswer.NotAble, MoveTutors.ForUltimateMove(Mon("Grotle", 30)));
        Assert.All(MoveTutors.UltimateSpecies, s => Assert.NotNull(PokemonDatabase.Get(s)));
    }

    // ------------------------------------------------------------------ in their rooms

    private static HeadlessScriptHost WithShards(int each = 20)
    {
        var host = new HeadlessScriptHost();
        foreach (string colour in new[] { "Red", "Blue", "Yellow", "Green" }) host.Bag.AddItem(ItemDatabase.Get(colour + " Shard")!, each);
        return host;
    }

    [Theory]
    [InlineData("Route212House", "Fury Cutter", "Blue Shard", 8)]
    [InlineData("SurvivalAreaNorthHouse", "Mud-Slap", "Red Shard", 4)]
    [InlineData("SnowpointCityEastHouse", "Snore", "Yellow Shard", 4)]
    public void AShardTutorTeachesTheMoveChosenAndTakesItsShards(string room, string move, string shard, int cost)
    {
        var host = WithShards();
        var bulbasaur = Mon("Bulbasaur");
        bulbasaur.Moves.Clear();
        bulbasaur.Moves.Add(MoveDatabase.Create("Tackle"));
        host.Party.Add(bulbasaur);
        host.PokemonChoice = 0;
        // Yes; then the first move on the list
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        Run("Tutor", room, host);
        Assert.True(bulbasaur.Knows(move), Said(host));
        Assert.Equal(20 - cost, Have(host, shard));
        Assert.Contains($"learned {move}", Said(host));
        Assert.Contains(host.Asked, a => a.Answer.StartsWith(move, StringComparison.Ordinal) && a.Answer.Contains("Shard", StringComparison.Ordinal) == false);

        // With too few Shards nothing is taught and nothing taken
        var poor = new HeadlessScriptHost();
        var other = Mon("Bulbasaur");
        poor.Party.Add(other);
        poor.PokemonChoice = 0;
        poor.Answers.Enqueue(0);
        poor.Answers.Enqueue(0);
        Run("Tutor", room, poor);
        Assert.False(other.Knows(move));
        Assert.Contains("Shards", Said(poor));
        // Refused, backed out of the team, an Egg, and a Pokémon the tutor has nothing for
        var no = WithShards();
        no.Answers.Enqueue(1);
        Run("Tutor", room, no);
        Assert.Single(no.Asked);
        var nobody = WithShards();
        nobody.Party.Add(Mon("Bulbasaur"));
        nobody.PokemonChoice = 255;
        Run("Tutor", room, nobody);
        Assert.Single(nobody.Asked);
        var egg = WithShards();
        var unhatched = Mon("Bulbasaur");
        unhatched.IsEgg = true;
        egg.Party.Add(unhatched);
        egg.PokemonChoice = 0;
        Run("Tutor", room, egg);
        Assert.Contains("Egg", Said(egg));
        var magikarp = WithShards();
        magikarp.Party.Add(Mon("Ditto"));
        magikarp.PokemonChoice = 0;
        Run("Tutor", room, magikarp);
        Assert.Contains("no move of mine", Said(magikarp));
        Assert.Equal(20, Have(magikarp, shard));
    }

    [Fact]
    public void APokemonThatKnowsFourMovesGivesOneUpForATutorsButNeverAnHms()
    {
        Pokemon Full()
        {
            var p = Mon("Bulbasaur");
            p.Moves.Clear();
            foreach (string m in new[] { "Cut", "Tackle", "Growl", "Vine Whip" }) p.Moves.Add(MoveDatabase.Create(m));
            return p;
        }
        // The second move goes
        var host = WithShards();
        var pupil = Full();
        host.Party.Add(pupil);
        host.PokemonChoice = 0;
        foreach (int answer in new[] { 0, 0, 1 }) host.Answers.Enqueue(answer);
        Run("Tutor", "Route212House", host);
        Assert.Equal(new[] { "Cut", "Fury Cutter", "Growl", "Vine Whip" }, pupil.Moves.Select(m => m.Name));
        Assert.Contains("forgot Tackle", Said(host));
        Assert.Equal(12, Have(host, "Blue Shard"));
        // An HM's move stays, nothing is learned and nothing paid
        var hm = WithShards();
        var stubborn = Full();
        hm.Party.Add(stubborn);
        hm.PokemonChoice = 0;
        foreach (int answer in new[] { 0, 0, 0 }) hm.Answers.Enqueue(answer);
        Run("Tutor", "Route212House", hm);
        Assert.Equal(new[] { "Cut", "Tackle", "Growl", "Vine Whip" }, stubborn.Moves.Select(m => m.Name));
        Assert.Equal(20, Have(hm, "Blue Shard"));
        Assert.Contains("did not learn Fury Cutter", Said(hm));
        // Given up
        var gaveUp = WithShards();
        var same = Full();
        gaveUp.Party.Add(same);
        gaveUp.PokemonChoice = 0;
        foreach (int answer in new[] { 0, 0, 4 }) gaveUp.Answers.Enqueue(answer);
        Run("Tutor", "Route212House", gaveUp);
        Assert.False(same.Knows("Fury Cutter"));
        Assert.Equal(20, Have(gaveUp, "Blue Shard"));
    }

    [Fact]
    public void GrandmaWilmaTeachesDracoMeteorToADragonThatTrustsItsTrainer()
    {
        const string room = "Route210GrandmaWilmaHouse";
        // No Dragon type on the team: she says so before anyone is chosen
        var none = new HeadlessScriptHost();
        none.Party.Add(Mon());
        Run("Wilma", room, none);
        Assert.True(none.Story.Has("FLAG_TALKED_TO_ROUTE_210_GRANDMA_WILMA"));
        Assert.Contains("not one of the Pokémon", Said(none));
        // A Dragon that doesn't trust its Trainer yet
        var host = new HeadlessScriptHost();
        var gible = Mon("Gible");
        gible.Moves.Clear();
        gible.Moves.Add(MoveDatabase.Create("Tackle"));
        gible.Friendship = 100;
        host.Party.Add(Mon());
        host.Party.Add(gible);
        host.PokemonChoice = 1;
        Run("Wilma", room, host);
        Assert.False(gible.Knows("Draco Meteor"));
        Assert.Contains("trust you fully", Said(host));
        // Fully trusting, it learns the move, and she says what it costs the user
        gible.Friendship = 255;
        host.Transcript.Clear();
        Run("Wilma", room, host);
        Assert.True(gible.Knows("Draco Meteor"));
        Assert.Contains("Special Attack", Said(host));
        Assert.DoesNotContain("Grandma Wilma, they call me", Said(host));
        // And the next time it knows it already; someone who isn't a Dragon can't learn it
        host.Transcript.Clear();
        Run("Wilma", room, host);
        Assert.Contains("knows Draco Meteor already", Said(host));
        host.PokemonChoice = 0;
        host.Transcript.Clear();
        Run("Wilma", room, host);
        Assert.Contains("cannot learn this move", Said(host));
        var here = new HeadlessScriptHost();
        Run("OnEnter", room, here);
        Assert.True(here.Story.Has("FLAG_FIRST_ARRIVAL_DRAGON_MASTERS_HOUSE"));
    }

    [Fact]
    public void TheOldManOfRoute228TeachesAStarterFullyGrownItsStrongestMove()
    {
        const string room = "Route228NorthHouse";
        var host = new HeadlessScriptHost();
        var torterra = Mon("Torterra", 50);
        torterra.Moves.Clear();
        torterra.Moves.Add(MoveDatabase.Create("Razor Leaf"));
        torterra.Friendship = 255;
        host.Party.Add(torterra);
        host.Party.Add(Mon("Bidoof"));
        host.PokemonChoice = 0;
        Run("Master", room, host);
        Assert.True(torterra.Knows("Frenzy Plant"), Said(host));
        Assert.True(host.Story.Has("FLAG_TALKED_TO_ROUTE_228_NORTH_HOUSE_EXPERT_M"));
        Assert.Contains(host.Asked, a => a.Question.Contains("Frenzy Plant") && a.Question.Contains(torterra.DisplayName));
        host.PokemonChoice = 1;
        host.Transcript.Clear();
        Run("Master", room, host);
        Assert.Contains("Read the poster", Said(host));
        // His poster lists the twelve
        var poster = Room(room).Signboards.Single();
        Assert.All(MoveTutors.UltimateSpecies, s => Assert.Contains(s, poster.Value));
    }

    [Fact]
    public void TheMoveDeleterMakesAPokemonForgetAnyMoveButItsLast()
    {
        const string room = "CanalaveCityEastHouse";
        var host = new HeadlessScriptHost();
        var bidoof = Mon("Bidoof");
        bidoof.Moves.Clear();
        foreach (string m in new[] { "Cut", "Tackle" }) bidoof.Moves.Add(MoveDatabase.Create(m));
        host.Party.Add(bidoof);
        host.PokemonChoice = 0;
        // Yes; the first move, an HM's, which nobody else can take away; yes
        foreach (int answer in new[] { 0, 0, 0 }) host.Answers.Enqueue(answer);
        Run("Deleter", room, host);
        Assert.Equal(new[] { "Tackle" }, bidoof.Moves.Select(m => m.Name));
        Assert.Contains("forgotten Cut", Said(host));
        // Its last move stays
        host.Transcript.Clear();
        Run("Deleter", room, host);
        Assert.Single(bidoof.Moves);
        Assert.Contains("only the one move", Said(host));
        // Changing one's mind at the last question leaves the move
        bidoof.Moves.Add(MoveDatabase.Create("Growl"));
        foreach (int answer in new[] { 0, 1, 1 }) host.Answers.Enqueue(answer);
        Run("Deleter", room, host);
        Assert.Equal(2, bidoof.Moves.Count);
        Assert.False(MoveTutors.CanForget(new Pokemon(PokemonDatabase.Get("Bidoof")!, 5, Gender.Male, Nature.Hardy, false) { IsEgg = true }));
    }

    [Fact]
    public void TheMoveRelearnerAndTheNameRaterSayWhatTheyDoAndTakeNothing()
    {
        // Their rules are plan 12 · Q19's (Models/Relearning.cs) and plan 13 · V4's (the naming prompt)
        var without = Run("Relearner", "PastoriaCityEastHouse", new HeadlessScriptHost());
        Assert.Contains("Heart Scale", Said(without));
        var with = new HeadlessScriptHost();
        with.Bag.AddItem(ItemDatabase.Get("Heart Scale")!, 1);
        Run("Relearner", "PastoriaCityEastHouse", with);
        Assert.Equal(1, Have(with, "Heart Scale"));
        Assert.NotEqual(Said(without), Said(with));

        var rater = Room("EternaCondominiums1F").Everyone.Single(n => n.Key == "expert_m");
        Assert.Equal(("Name Rater", "NameRater"), (rater.Name, rater.Script));
        var host = new HeadlessScriptHost();
        var named = Mon("Bidoof");
        host.Party.Add(named);
        host.PokemonChoice = 0;
        Run("NameRater", "EternaCondominiums1F", host);
        Assert.Contains(named.DisplayName, Said(host));
        Assert.Equal("Bidoof", named.DisplayName);
    }

    [Fact]
    public void TheTutorCommandReadsItsWordsAndNamesAPlaceThatHasATutor()
    {
        var wrong = ScriptLibrary.FromSources(("test", "script A\n  tutor moves \"jubilife_city\"\n  end\n"));
        Assert.Contains(wrong.Problems(), p => p.Contains("jubilife_city"));
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script A\n  tutor sing\n"));
        // Nothing chosen and nothing settled on: every word answers without a question
        var host = new HeadlessScriptHost();
        var script = ScriptParser.Parse("test", "script A\n  setvar VAR_X 0\n  tutor afford\n  tutor pay\n  tutor learn\n  tutor delete\n  tutor dragons\n  end\n")[0];
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(script);
        runner.RunToEnd();
        Assert.Empty(host.Asked);
        Assert.Empty(host.Problems);
    }
}
