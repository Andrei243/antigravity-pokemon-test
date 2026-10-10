using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The second chapter (plan 02 · S5, "Jubilife and Roark") played through with no screen, in the order a player
/// meets it, from the end of the first: the assistant and Looker in Jubilife City, the parcel at the Trainers' School,
/// the Pokétch campaign, the rival on Route 203, Rock Smash in Oreburgh Gate, the rival at the Gym's door and Roark
/// down the mine, the Coal Badge, the rival on the way out and Team Galactic's grunts at Jubilife's north gate. Each
/// scene is started as the game starts it, on maps of its own (<see cref="OpeningTests.Game"/>).
/// </summary>
public class JubilifeAndRoarkTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    /// <summary>A game at the end of the first chapter: its flags, the starter on the team, the parcel in the bag.</summary>
    private static OpeningTests.Game AfterTheOpening(int starter)
    {
        var game = new OpeningTests.Game(starter);
        game.Play(Scripts.Find(ScriptLibrary.OpeningDone)!);
        string mine = StoryState.Starters[starter];
        game.Story.ChooseStarter(mine);
        game.Party.Add(new Pokemon(PokemonDatabase.Get(mine)!, 14));
        game.Bag.AddItem(ItemDatabase.Get("Parcel")!, 1);
        return game;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TheSecondChapterPlaysThroughFromJubilifeToTheCoalBadgeAndTheNorthGate(int starter)
    {
        var game = AfterTheOpening(starter);
        string mine = StoryState.Starters[starter];

        // A new game keeps the chapter's people out of sight until their scenes
        Assert.True(game.Story.Has("FLAG_HIDE_JUBILIFE_CITY_COUNTERPART"));
        Assert.True(game.Story.Has("FLAG_HIDE_JUBILIFE_GALACTIC_GRUNTS"));
        Assert.Equal(1, game.Story.Var("VAR_OREBURGH_GATE_1F_HIKER_STATE"));

        // Jubilife City: the assistant comes to meet the player, Looker gives a Vs. Recorder
        game.Arrive("Sinnoh", 174, 797);
        Assert.Null(game.Present("counterpart", "jubilife_city"));
        game.Step("FirstArrival");
        Assert.Contains(game.Said, l => l.Speaker == "Looker");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Vs. Recorder")!));
        Assert.Equal(1, game.Story.Var("VAR_JUBILIFE_CITY_STATE"));
        Assert.Null(game.Present("counterpart", "jubilife_city"));
        Assert.False(game.Fires("FirstArrival"));

        // Looker turns the player back from Route 203; the clowns' first two and the president aren't out yet
        Assert.True(game.Fires("LookerBlocks"));
        game.Step("LookerBlocks");
        Assert.Contains(game.Said, l => l.Text.Contains("Trainers' School"));
        Assert.Null(game.Present("clown_1", "jubilife_city"));
        Assert.Null(game.Present("poketch_co_president", "jubilife_city"));
        Assert.False(game.Fires("PoketchCampaign"));

        // The Trainers' School: the rival takes his parcel and gives a Town Map; two pupils battle when asked, and the
        // second beaten gives a Potion
        game.Through("TrainersSchool");
        game.Talk("rival");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Parcel")!));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Town Map")!));
        Assert.True(game.Story.Has("FLAG_TALKED_TO_TRAINERS_SCHOOL_RIVAL"));
        Assert.Null(game.Present("rival"));
        Assert.Contains("battle school_kid_harrison Won", game.Talk("school_kid_harrison").Log);
        Assert.Contains("battle school_kid_christine Won", game.Talk("school_kid_christine").Log);
        game.Talk("school_kid_harrison");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Potion")!));

        // Back outside: the president stops the player and tells of the campaign; the clowns are out
        game.Through("Sinnoh");
        Assert.NotNull(game.Present("clown_1", "jubilife_city"));
        Assert.True(game.Fires("PoketchCampaign"));
        game.Tile = (174, 776);
        game.Step("PoketchCampaign");
        Assert.Equal(2, game.Story.Var("VAR_POKETCH_CAMPAIGN_STATE"));
        Assert.Contains("collect", game.Talk("poketch_co_president", "jubilife_city").Transcript[^1].Text, StringComparison.OrdinalIgnoreCase);

        // Three right answers, three coupons, and the Pokétch for them; Looker lets the player by
        foreach (string clown in new[] { "clown_1", "clown_2", "clown_3" }) game.Talk(clown, "jubilife_city");
        foreach (string coupon in new[] { "Coupon 1", "Coupon 2", "Coupon 3" })
            Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get(coupon)!));
        var given = game.Talk("poketch_co_president", "jubilife_city");
        Assert.True(given.Poketch.Enabled);
        Assert.Contains(PoketchApp.PartyStatus, given.Poketch.Apps);
        Assert.True(game.Story.Has("FLAG_RECEIVED_POKETCH"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Coupon 1")!));
        Assert.Equal(2, game.Story.Var("VAR_JUBILIFE_CITY_STATE"));
        Assert.False(game.Fires("LookerBlocks"));
        Assert.Null(game.Present("poketch_co_president", "jubilife_city"));

        // Route 203: the rival battles with the team that hangs on the player's starter, and runs on
        Assert.True(game.Fires("Rival"));
        var rival = game.Step("Rival");
        string expected = mine switch { "Turtwig" => "rival_route_203_turtwig", "Chimchar" => "rival_route_203_chimchar", _ => "rival_route_203_piplup" };
        Assert.Contains($"battle {expected} Won", rival.Log);
        Assert.Equal(1, game.Story.Var("VAR_ROUTE_203_RIVAL_STATE"));
        Assert.False(game.Fires("Rival"));

        // Oreburgh Gate: the hiker gives HM06 as the player passes him, once
        game.Through("OreburghGate1F");
        Assert.True(game.Fires("HikerStops"));
        game.Step("HikerStops");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM06")!));
        Assert.False(game.Fires("HikerStops"));
        Assert.DoesNotContain(game.Talk("hiker").Log, l => l.StartsWith("fanfare"));

        // Oreburgh City: a boy takes the player to the Gym, where the rival stands at the door
        game.Arrive("Sinnoh", 266, 749);
        game.Tile = (266, 749);
        game.Step("ToTheGym");
        Assert.Equal(1, game.Story.Var("VAR_OREBURGH_CITY_STATE"));
        var door = game.Present("rival", "oreburgh_city");
        Assert.NotNull(door);
        Assert.Equal((282, 757), (door!.GridX, door.GridY));
        Assert.Contains(game.Talk("rival", "oreburgh_city").Transcript, l => l.Text.Contains("mine", StringComparison.OrdinalIgnoreCase));
        Assert.True(game.Story.Has("FLAG_TALKED_TO_OREBURGH_CITY_RIVAL"));

        // The mine: Roark smashes a rock and goes back up; the rival leaves the Gym's door
        game.Arrive("OreburghMineB2F", 15, 2);
        Assert.NotNull(game.Present("roark"));
        game.Talk("roark");
        Assert.True(game.Story.Has("FLAG_ROARK_RETURNED_TO_OREBURGH_GYM"));
        Assert.Null(game.Present("roark"));
        game.Arrive("Sinnoh", 282, 758);
        Assert.Null(game.Present("rival", "oreburgh_city"));

        // The Gym: its two trainers, and Roark, for the Coal Badge and TM76
        game.Arrive("OreburghGym", 5, 24);
        Assert.Contains("battle youngster_darius Won", game.Talk("youngster_darius").Log);
        var gym = game.Talk("roark");
        Assert.Contains("battle leader_roark Won", gym.Log);
        Assert.Contains($"fanfare {PokemonPlatinumEngine.Audio.MusicRole.FanfareBadge}", gym.Log);
        Assert.True(game.Story.HasBadge(Badge.Coal));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM76")!));
        Assert.Equal(2, game.Story.Var("VAR_OREBURGH_CITY_STATE"));
        Assert.Equal(3, game.Story.Var("VAR_JUBILIFE_CITY_STATE"));
        // and what the original's Roark sets besides (scripts_oreburgh_city_gym.s)
        Assert.True(game.Story.Has("FLAG_HIDE_POKECENTER_BASEMENT_BLOCKADE"));
        Assert.Equal(1, game.Story.Var("VAR_GTS_ACCESS_STATE"));
        Assert.Equal(1, game.Story.Var("VAR_JUBILIFE_LOOKER_PAL_PAD_STATE"));
        Assert.DoesNotContain(game.Talk("roark").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM76")!));

        // On the way out, the rival runs into the player and is off to Eterna City
        game.Arrive("Sinnoh", 262, 749);
        Assert.True(game.Fires("RivalOnTheWayOut"));
        game.Tile = (262, 749);
        game.Step("RivalOnTheWayOut");
        Assert.Equal(3, game.Story.Var("VAR_OREBURGH_CITY_STATE"));
        Assert.Null(game.Present("rival", "oreburgh_city"));

        // Jubilife's north gate: the professor, the assistant and two grunts; the assistant battles beside the player
        game.Arrive("Sinnoh", 174, 745);
        Assert.NotNull(game.Present("prof_rowan", "jubilife_city"));
        Assert.NotNull(game.Present("grunt_m_1", "jubilife_city"));
        var assistant = game.Present("counterpart", "jubilife_city");
        Assert.Equal((176, 739), (assistant!.GridX, assistant.GridY));
        game.Tile = (174, 743);
        var galactic = game.Step("TeamGalactic");
        // The assistant's team is the one Platinum gives them for the player's starter (Dawn's or Lucas's)
        Assert.Contains(galactic.Log, l => l.StartsWith("battle galactic_grunt_jubilife_city_1 and galactic_grunt_jubilife_city_2 with ")
            && l.Contains("_jubilife_city_" + mine.ToLowerInvariant()) && l.EndsWith("Won"));
        Assert.Equal(4, game.Story.Var("VAR_JUBILIFE_CITY_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_JUBILIFE_GALACTIC_GRUNTS"));
        Assert.False(game.Story.Has("FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_ROWAN"));
        Assert.Null(game.Present("prof_rowan", "jubilife_city"));
        Assert.Null(game.Present("counterpart", "jubilife_city"));
        // The campaign's clowns leave with them, the third from in front of Jubilife TV's door
        Assert.Null(game.Present("clown_3", "jubilife_city"));
        Assert.True(game.Story.Has("FLAG_HIDE_JUBILIFE_CITY_CLOWN_3"));
        Assert.False(game.Fires("TeamGalactic"));
    }

    [Fact]
    public void ASaveFromAfterTheNorthGateLosesTheCampaignsClowns()
    {
        var after = new StoryState();
        after.SetVar("VAR_JUBILIFE_CITY_STATE", 4);
        StoryMigration.Upgrade(after, 7, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.True(after.Has("FLAG_HIDE_JUBILIFE_CITY_CLOWNS_1_AND_2"));
        Assert.True(after.Has("FLAG_HIDE_JUBILIFE_CITY_CLOWN_3"));
        // Before the battle the third is still at the door, waiting to give out his coupon
        var before = new StoryState();
        before.SetVar("VAR_JUBILIFE_CITY_STATE", 3);
        StoryMigration.Upgrade(before, 7, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.False(before.Has("FLAG_HIDE_JUBILIFE_CITY_CLOWN_3"));
    }

    [Fact]
    public void LookerAsksForThePoketchOnceTheSchoolIsDone()
    {
        var game = AfterTheOpening(0);
        game.Arrive("Sinnoh", 174, 797);
        game.Step("FirstArrival");
        game.Story.Set("FLAG_TALKED_TO_TRAINERS_SCHOOL_RIVAL");
        game.Step("LookerBlocks");
        Assert.Contains(game.Said, l => l.Text.Contains("Pokétch"));
    }

    [Fact]
    public void ARoomsTrainerOfPlatinumsDataTakesItsTeamFromIt()
    {
        var gym = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "OreburghGym.json")).ToMap();
        var darius = gym.Everyone.Single(n => n.Key == "youngster_darius").TrainerData!;
        Assert.Equal(new[] { ("Geodude", 9), ("Onix", 9) }, darius.Party.Members.Select(p => (p.Species.Name, p.Level)));
        Assert.Equal("Youngster", darius.TrainerClass);
        Assert.Equal(3, gym.Everyone.Single(n => n.Key == "youngster_jonathon").TrainerData!.SightRange);
    }

    [Fact]
    public void ASaveFromBeforeTheChapterHidesItsPeopleUntilTheirScenes()
    {
        var story = new StoryState();
        StoryMigration.Upgrade(story, 2, Array.Empty<Pokemon>(), Scripts);
        Assert.True(story.Has("FLAG_HIDE_JUBILIFE_ROWAN"));
        Assert.True(story.Has("FLAG_HIDE_JUBILIFE_CITY_CLOWNS_1_AND_2"));
        Assert.Equal(1, story.Var("VAR_OREBURGH_GATE_1F_HIKER_STATE"));
        // and what the later chapters keep hidden with it
        Assert.True(story.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_GIRATINA"));
    }

    [Fact]
    public void ASaveFromBeforeTheThirdChapterIsGivenItsStepAlone()
    {
        // Version 3 already has the second chapter's flags; the third chapter's step runs on its own
        var story = new StoryState();
        StoryMigration.Upgrade(story, 3, Array.Empty<Pokemon>(), Scripts);
        Assert.False(story.Has("FLAG_HIDE_JUBILIFE_ROWAN"));
        Assert.True(story.Has("FLAG_HIDE_TURNBACK_CAVE_GIRATINA_ROOM_ITEM"));
    }

    [Fact]
    public void ASaveThatWonTheCoalBadgeBeforeTheHikerStoodInTheGateHasRockSmash()
    {
        // Version 4 and older: a save that beat Roark before Oreburgh Gate's hiker gave out HM06 never passed him
        // with it, and the Ravaged Path's cracked rocks would shut it out of the north. The Badge says it has been by.
        var hm = ItemDatabase.Get("HM06")!;
        var story = new StoryState();
        story.GiveBadge(Badge.Coal);
        var bag = new Inventory();
        StoryMigration.Upgrade(story, 4, Array.Empty<Pokemon>(), Scripts, bag);
        Assert.Equal(1, bag.GetQuantity(hm));
        Assert.True(story.Has(StoryMigration.ReceivedRockSmashFlag));
        Assert.Equal(2, story.Var("VAR_OREBURGH_GATE_1F_HIKER_STATE"));

        // Without the Badge it still has the hiker to pass on the way to Oreburgh
        var early = new StoryState();
        var none = new Inventory();
        StoryMigration.Upgrade(early, 4, Array.Empty<Pokemon>(), Scripts, none);
        Assert.Equal(0, none.GetQuantity(hm));
        Assert.False(early.Has(StoryMigration.ReceivedRockSmashFlag));

        // One the hiker already gave it to has nothing more; and a save of today is left as it is
        var given = new StoryState();
        given.GiveBadge(Badge.Coal);
        given.Set(StoryMigration.ReceivedRockSmashFlag);
        var one = new Inventory();
        one.AddItem(hm);
        StoryMigration.Upgrade(given, 4, Array.Empty<Pokemon>(), Scripts, one);
        Assert.Equal(1, one.GetQuantity(hm));
        var today = new StoryState();
        today.GiveBadge(Badge.Coal);
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.False(today.Has(StoryMigration.ReceivedRockSmashFlag));
        Assert.True(StoryState.CurrentVersion >= 5);
    }
}
