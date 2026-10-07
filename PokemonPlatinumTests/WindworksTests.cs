using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The third chapter's first half (plan 02 · S6, "Windworks and Eterna") played through with no screen, in the order a
/// player meets it: the grunts at Floaroma Meadow's door, the little girl on Route 205, the meadow's two battles and the
/// Works Key, the honey man's Honey, the grunt at the Valley Windworks' door and its lock, Looker outside, and Cheryl
/// through Eterna Forest. And the field's new rules the chapter brought: someone travelling with the player, two
/// trainers who come together, a door locked until the story opens it.
/// </summary>
public class WindworksTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static OpeningTests.Game NewGame(BattleOutcome fight = BattleOutcome.Won)
    {
        var game = new OpeningTests.Game(0, fight);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Grotle")!, 20));
        return game;
    }

    [Fact]
    public void ANewGameKeepsTheChaptersLatecomersOutOfSight()
    {
        var game = NewGame();
        foreach (string flag in new[] { "FLAG_HIDE_ROUTE_205_SOUTH_YOUNGSTER", "FLAG_HIDE_VALLEY_WINDWORKS_BUILDING_LITTLE_GIRL",
                     "FLAG_HIDE_VALLEY_WINDWORKS_OUTSIDE_LOOKER", "FLAG_HIDE_ETERNA_FOREST_GARDENIA", "FLAG_HIDE_FLOAROMA_MEADOW_WORKS_KEY" })
            Assert.True(game.Story.Has(flag), flag);
        // Those the chapter sends away are there from the start
        Assert.False(game.Story.Has("FLAG_HIDE_FLOAROMA_TOWN_GRUNTS"));
        Assert.False(game.Story.Has("FLAG_HIDE_ETERNA_FOREST_CHERYL"));
    }

    [Fact]
    public void TheWorksKeyIsWonInTheMeadowAndOpensTheWindworks()
    {
        var game = NewGame();

        // Floaroma Town: two grunts stand in the way into the meadow
        game.Arrive("Sinnoh", 170, 650);
        Assert.NotNull(game.Present("grunt_m_west", "floaroma_town"));
        game.Talk("grunt_m_west", "floaroma_town");
        Assert.All(game.Said, l => Assert.Equal("Team Galactic Grunt", l.Speaker));

        // Route 205: the little girl stops the player on the road out of town, and the grunts in town are gone
        Assert.True(game.Fires("LittleGirl"));
        game.Step("LittleGirl");
        Assert.Equal(1, game.Story.Var("VAR_VALLEY_WINDWORKS_STATE"));
        Assert.Contains(game.Said, l => l.Speaker == "Little Girl" && l.Text.Contains("Valley Windworks"));
        Assert.Null(game.Present("grunt_m_west", "floaroma_town"));
        Assert.False(game.Fires("LittleGirl"));
        game.Talk("little_girl", "route_205_south");
        Assert.Single(game.Said);

        // The bridge's grunts send the player back south
        game.Step("BridgeGrunts");
        Assert.Equal((217, 654), game.Tile);
        Assert.NotNull(game.Present("grunt_m_west", "route_205_south"));

        // Floaroma Meadow: two battles one after the other, then the key and ten jars of Honey
        game.Arrive("FloaromaMeadow", 12, 49);
        Assert.Null(game.Present("item_works_key"));
        var meadow = game.Step("Grunts");
        Assert.Equal(new[] { "battle galactic_grunt_floaroma_meadow_1 Won", "battle galactic_grunt_floaroma_meadow_2 Won" },
            meadow.Log.Where(l => l.StartsWith("battle ")));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Works Key")!));
        Assert.Equal(10, game.Bag.GetQuantity(ItemDatabase.Get("Honey")!));
        Assert.True(game.Story.Has("FLAG_OBTAINED_FLOAROMA_MEADOW_WORKS_KEY"));
        Assert.Null(game.Present("grunt_m_west"));
        Assert.Null(game.Present("item_works_key"));
        Assert.False(game.Fires("Grunts"));

        // The Valley Windworks: the grunt at the door battles and locks himself in
        game.Arrive("Sinnoh", 243, 656);
        var door = game.Talk("grunt_m", "valley_windworks_outside");
        Assert.Contains("battle galactic_grunt_valley_windworks_1 Won", door.Log);
        Assert.Null(game.Present("grunt_m", "valley_windworks_outside"));

        // The door is read from its mat and opens with the key
        Assert.Equal("Door", game.Map.TileScripts[(243, 654)]);
        Assert.Equal("FLAG_UNLOCKED_VALLEY_WINDWORKS_DOOR", game.Map.GetWarpAt(243, 654)!.OpenedBy);
        game.Tile = (243, 655);
        game.Play(Scripts.Find("Door", "valley_windworks_outside")!);
        Assert.True(game.Story.Has("FLAG_UNLOCKED_VALLEY_WINDWORKS_DOOR"));

        // Inside: a grunt runs to warn the Commander
        game.Through("ValleyWindworksBuilding");
        Assert.Contains(game.Said, l => l.Text.Contains("warn the Commander"));
        Assert.Equal(1, game.Story.Var("VAR_VALLEY_WINDWORKS_TEAM_GALACTIC_STATE"));
        Assert.Null(game.Present("galactic_grunt_1"));
        Assert.Null(game.Present("little_girl"));

        // Commander Mars beside the controls: beaten, Team Galactic goes, the bridge opens, the girl runs in to her papa
        var mars = game.Step("Mars");
        Assert.Contains("battle commander_mars_valley_windworks Won", mars.Log);
        Assert.Contains(mars.Transcript, l => l.Speaker == "Charon");
        Assert.Equal(2, game.Story.Var("VAR_VALLEY_WINDWORKS_STATE"));
        Assert.Equal(2, game.Story.Var("VAR_VALLEY_WINDWORKS_TEAM_GALACTIC_STATE"));
        Assert.Null(game.Present("mars"));
        Assert.Null(game.Present("charon"));
        Assert.NotNull(game.Present("little_girl"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_205_SOUTH_GRUNTS"));
        Assert.False(game.Story.Has("FLAG_HIDE_ROUTE_205_SOUTH_YOUNGSTER"));
        Assert.False(game.Fires("Mars"));

        // Out again: Looker
        game.Through("Sinnoh");
        Assert.Contains(game.Said, l => l.Speaker == "Looker" && l.Text.Contains("Eterna City"));
        Assert.Equal(3, game.Story.Var("VAR_VALLEY_WINDWORKS_TEAM_GALACTIC_STATE"));
        Assert.Null(game.Present("grunt_m_west", "route_205_south"));
        Assert.NotNull(game.Present("youngster", "route_205_south"));

        // The papa and the girl at home afterwards
        game.Through("ValleyWindworksBuilding");
        var girl = game.Present("little_girl")!;
        Assert.Equal((21, 5), (girl.GridX, girl.GridY));
        Assert.Contains(game.Talk("scientist_papa").Transcript, l => l.Text.Contains("turbines"));
    }

    [Theory]
    [InlineData("OreburghNorthHouse1F", "school_kid_f", "kazza", "Machop", "Abra", "FLAG_TRADED_FOR_KAZZA_ABRA")]
    [InlineData("EternaCondominiums1F", "ninja_boy", "charap", "Buizel", "Chatot", "FLAG_TRADED_FOR_CHARAP_CHATOT")]
    [InlineData("SnowpointWestHouse", "mindy", "gaspar", "Medicham", "Haunter", "FLAG_TRADED_FOR_GASPAR_HAUNTER")]
    public void ThePeopleWhoTradeTakeTheSpeciesTheyAskForAndNothingElse(string room, string who, string trade, string wants, string gives, string flag)
    {
        var map = MapDatabase.Get(room);
        var person = map.FindPerson(who)!;
        foreach (bool right in new[] { false, true })
        {
            var host = new HeadlessScriptHost { Map = map, PokemonChoice = 1 };
            host.Party.Add(new Pokemon(PokemonDatabase.Get("Grotle")!, 20));
            host.Party.Add(new Pokemon(PokemonDatabase.Get(right ? wants : "Bidoof")!, 20));
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Scripts.Find(person.Script!, room)!, person);
            runner.RunToEnd();
            Assert.Equal(right, host.Story.Has(flag));
            Assert.Equal(right ? gives : "Bidoof", host.Party.Members[1].Species.Name);
            Assert.Contains(host.Log, l => l.StartsWith($"trade {trade} {(right ? "done" : "refused")}"));
        }
    }

    [Fact]
    public void LosingInTheMeadowLeavesTheGruntsAndTheKeyWhereTheyWere()
    {
        var game = NewGame(BattleOutcome.Lost);
        game.Arrive("FloaromaMeadow", 12, 49);
        var meadow = game.Step("Grunts");
        Assert.Single(meadow.Log, l => l.StartsWith("battle "));
        Assert.Equal(0, game.Story.Var("VAR_FLOAROMA_MEADOW_STATE"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Works Key")!));
        Assert.True(game.Fires("Grunts"));
    }

    [Fact]
    public void TheDoorStaysLockedWithoutTheKey()
    {
        var game = NewGame();
        game.Arrive("Sinnoh", 243, 655);
        game.Play(Scripts.Find("Door", "valley_windworks_outside")!);
        Assert.Contains(game.Said, l => l.Text.Contains("locked"));
        Assert.False(game.Story.Has("FLAG_UNLOCKED_VALLEY_WINDWORKS_DOOR"));
    }

    [Theory]
    [InlineData(0, 1, 2900)]
    [InlineData(1, 10, 2000)]
    [InlineData(2, 0, 3000)]
    public void TheHoneyManSellsHoney(int answer, int jars, int moneyLeft)
    {
        var map = new Map(4, 4);
        var host = new HeadlessScriptHost { Map = map, Money = 3000 };
        host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("HoneyMan", "floaroma_meadow")!);
        runner.RunToEnd();
        Assert.Equal(jars, host.Bag.GetQuantity(ItemDatabase.Get("Honey")!));
        Assert.Equal(moneyLeft, host.Money);
    }

    [Fact]
    public void LookerLooksInsideOnceCommanderMarsIsBeaten()
    {
        var game = NewGame();
        game.Story.SetVar("VAR_VALLEY_WINDWORKS_LOOKER_STATE", 1);
        game.Story.SetVar("VAR_VALLEY_WINDWORKS_TEAM_GALACTIC_STATE", 2);
        game.Story.Unset("FLAG_HIDE_VALLEY_WINDWORKS_OUTSIDE_LOOKER");
        game.Arrive("Sinnoh", 243, 655);
        Assert.Contains(game.Said, l => l.Speaker == "Looker" && l.Text.Contains("Eterna City"));
        Assert.Equal(2, game.Story.Var("VAR_VALLEY_WINDWORKS_LOOKER_STATE"));
        Assert.Equal(3, game.Story.Var("VAR_VALLEY_WINDWORKS_TEAM_GALACTIC_STATE"));
        Assert.Null(game.Present("looker", "valley_windworks_outside"));
    }

    [Fact]
    public void CherylComesThroughEternaForestWaitsIfThePlayerTurnsBackAndPartsWithASootheBell()
    {
        var game = NewGame();
        game.Arrive("EternaForest", 28, 85);
        Assert.Equal(0, game.Story.Var("VAR_ETERNA_FOREST_FOLLOWER_CHERYL_STATE"));

        var join = game.Step("Join");
        Assert.Equal("cheryl_eterna_forest", join.Partner);
        Assert.Equal(1, game.Story.Var("VAR_ETERNA_FOREST_FOLLOWER_CHERYL_STATE"));
        Assert.True(game.Story.Has("FLAG_TALKED_TO_ETERNA_FOREST_CHERYL"));

        // Back out the way they came: she stays behind, at her post
        var wait = game.Step("Wait");
        Assert.Contains("partner off", wait.Log);
        Assert.Equal(0, game.Story.Var("VAR_ETERNA_FOREST_FOLLOWER_CHERYL_STATE"));
        var cheryl = game.Present("cheryl")!;
        Assert.Equal((28, 83), (cheryl.GridX, cheryl.GridY));

        // Coming in again she joins at once, and at the far side gives a Soothe Bell and goes
        game.Step("Join");
        Assert.DoesNotContain(game.Said, l => l.Text.Contains("My name is Cheryl"));
        game.Story.SetVar("VAR_ETERNA_FOREST_FOLLOWER_CHERYL_STATE", 1);
        var part = game.Step("Part");
        Assert.Contains("partner off", part.Log);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Soothe Bell")!));
        Assert.True(game.Story.Has("FLAG_TRAVELED_WITH_CHERYL"));
        Assert.Null(game.Present("cheryl"));
        Assert.Equal(2, game.Story.Var("VAR_ETERNA_FOREST_FOLLOWER_CHERYL_STATE"));
    }

    [Fact]
    public void CherylSaysThreeThingsAndTheLastOverAgain()
    {
        var game = NewGame();
        game.Arrive("EternaForest", 28, 85);
        game.Story.SetVar("VAR_ETERNA_FOREST_FOLLOWER_CHERYL_STATE", 1);
        var lines = Enumerable.Range(0, 4).Select(_ => game.Talk("cheryl").Transcript.Single().Text).ToList();
        Assert.Equal(3, lines.Distinct().Count());
        Assert.Equal(lines[2], lines[3]);
    }

    [Fact]
    public void TwoTrainersWhoCameTogetherBattleBesideWhoeverTravelsWithThePlayer()
    {
        var map = new Map(8, 8);
        NPC Trainer(string id, int x) => MapFile.BuildNpc(new MapFile.NpcRecord
        {
            Id = id, Name = id, X = x, Y = 2,
            Trainer = new MapFile.TrainerRecord { Id = id, Name = id, TrainerClass = "Psychic", DialogueBefore = id + ": Before." }
        }, "Forest");
        var first = Trainer("psychic_lindsey", 1);
        var second = Trainer("psychic_elijah", 4);
        map.NPCs.Add(first);
        map.NPCs.Add(second);

        foreach (bool along in new[] { true, false })
        {
            var host = new HeadlessScriptHost { Map = map };
            if (along) host.TravelWith(new NPC { Name = "Cheryl" }, "cheryl_eterna_forest");
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Scripts.Find(FieldScripts.TrainerPair)!, first, pair: second);
            runner.RunToEnd();
            Assert.Equal(2, host.Transcript.Count);
            Assert.Contains(along ? "battle psychic_lindsey and psychic_elijah with cheryl_eterna_forest Won" : "battle psychic_lindsey and psychic_elijah Won", host.Log);
            Assert.True(host.Story.HasDefeated("psychic_elijah"));
            first.HasBattled = second.HasBattled = false;
        }
    }

    [Fact]
    public void TheSecondOfTwoTrainersIsSomeoneElseWithATeamOfTheirOwn()
    {
        var map = new Map(10, 10);
        NPC Watching(string id, int x, Direction facing) => new()
        {
            Name = id, GridX = x, GridY = 5, Facing = facing, IsTrainer = true,
            TrainerData = new Trainer { Id = id, Name = id, SightRange = 4 }
        };
        var west = Watching("bug_catcher_jack", 2, Direction.Right);
        var east = Watching("lass_briana", 6, Direction.Left);
        map.NPCs.Add(west);
        map.NPCs.Add(east);
        Assert.Same(west, TrainerApproach.FindSpotter(map, 4, 5));
        Assert.Same(east, TrainerApproach.FindSpotter(map, 4, 5, except: west));
        // Twins who battle as one trainer are one trainer, not a pair who came together
        east.TrainerData!.Id = "bug_catcher_jack";
        Assert.Null(TrainerApproach.FindSpotter(map, 4, 5, except: west));
    }

    [Fact]
    public void ADoorWaitsForItsFlag()
    {
        var map = new Map(6, 6);
        map.Warps.Add(new Warp { SourceX = 3, SourceY = 1, TargetMap = "Inside", OpenedBy = "FLAG_UNLOCKED_TEST_DOOR" });
        map.ApplyDoors(_ => false);
        Assert.True(map.IsSolid(3, 1));
        Assert.Equal(StepKind.Blocked, FieldMovement.Step(map, 3, 2, Direction.Up, new Walker()).Kind);
        map.ApplyDoors(flag => flag == "FLAG_UNLOCKED_TEST_DOOR");
        Assert.False(map.IsSolid(3, 1));
        Assert.Equal(StepKind.Walk, FieldMovement.Step(map, 3, 2, Direction.Up, new Walker()).Kind);
    }

    [Fact]
    public void SomeoneWalkingBehindFollowsInThePlayersSteps()
    {
        var cheryl = new NPC { Name = "Cheryl", GridX = 5, GridY = 6 };
        var follower = new Follower(cheryl, "cheryl_eterna_forest");
        // The player sets off north from (5,5), then east from (5,4)
        follower.PlayerLeft(5, 5);
        Run(follower);
        Assert.Equal((5, 5), (cheryl.GridX, cheryl.GridY));
        Assert.Equal(Direction.Up, cheryl.Facing);
        follower.PlayerLeft(5, 4);
        Run(follower);
        Assert.Equal((5, 4), (cheryl.GridX, cheryl.GridY));

        // Leaving her own tile is no step for her: walking back into her swaps the two round
        follower.PlayerLeft(5, 4);
        Assert.False(follower.Moving);

        // Far behind, she is brought up to the last tiles
        foreach (var (x, y) in new[] { (6, 4), (7, 4), (8, 4), (9, 4) }) follower.PlayerLeft(x, y);
        Run(follower);
        Assert.Equal((9, 4), (cheryl.GridX, cheryl.GridY));

        follower.Behind(12, 4, Direction.Right);
        Assert.Equal((11, 4), (cheryl.GridX, cheryl.GridY));
        Assert.Equal(Direction.Right, cheryl.Facing);
    }

    private static void Run(Follower follower)
    {
        for (int frame = 0; frame < 120; frame++) follower.Update(1f / 60f, 4.5f);
    }

    [Fact]
    public void NobodyWalkingBehindThePlayerIsInTheirWay()
    {
        var map = new Map(6, 6);
        var cheryl = new NPC { Name = "Cheryl", GridX = 2, GridY = 3 };
        map.NPCs.Add(cheryl);
        Assert.Equal(StepKind.Blocked, FieldMovement.Step(map, 2, 2, Direction.Down, new Walker()).Kind);
        map.Follower = cheryl;
        Assert.Equal(StepKind.Walk, FieldMovement.Step(map, 2, 2, Direction.Down, new Walker()).Kind);
        // Anyone else still finds her in the way
        Assert.False(Wandering.CanStep(map, new NPC { GridX = 3, GridY = 3 }, PersonMovement.Parse("wander_around", 2, 2, 3, 3, Direction.Down)!,
            Direction.Left, (0, 0), (0, 0)));
    }

    [Fact]
    public void BesideAPartnerTheGrassBringsASecondPokemon()
    {
        var map = MapDatabase.Get("EternaForest");
        var (x, y) = Enumerable.Range(0, map.Height).SelectMany(row => Enumerable.Range(0, map.Width).Select(col => (col, row)))
            .First(t => map.IsTallGrass(t.col, t.row));
        var second = map.MeetAnother(x, y);
        Assert.NotNull(second);
        Assert.Contains(map.WildAt(x, y).Table, e => e.SpeciesName == second!.SpeciesName);
        Assert.Equal(second!.MinLevel, second.MaxLevel);
    }

    [Fact]
    public void CherylsChanseyFightsBesideThePlayerAgainstTwoWildPokemon()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Grotle")!, 20));
        var cheryl = new Trainer { Id = "cheryl_eterna_forest" };
        TrainerDatabase.Fill(cheryl, TrainerDatabase.Get("cheryl_eterna_forest")!);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party,
            Inventory = new Inventory(),
            Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { new(PokemonDatabase.Get("Wurmple")!, 10), new(PokemonDatabase.Get("Hoothoot")!, 10) },
            Format = BattleFormat.Double,
            Partner = cheryl,
            Random = new System.Random(3)
        });
        Assert.Equal(BattleFormat.Double, battle.Core.Format);
        Assert.Equal("Chansey", battle.Core.PlayerSlots[1].Pokemon!.Species.Name);
        Assert.Equal(2, battle.Core.EnemySlots.Count(b => b.IsActive));
    }
}
