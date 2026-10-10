using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Spiritomb's tower on Route 209 (plan 08 · P12): the Hallowed Tower is read from its four stones' tiles, takes the
/// Odd Keystone when the player says so, stirs a little more the more people the player has spoken to, and lets
/// Spiritomb out at thirty-two (the original's Underground count, stood in for by people met anywhere:
/// docs/mechanics/rulings.md). The people spoken to are counted once each and kept in the save, and the Pokédex's
/// area page names the tower, which no table of wild Pokémon lists.
/// </summary>
public class HallowedTowerTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    /// <summary>The tower's stones (the original's four tile events of type 0, script 2).</summary>
    private static readonly (int X, int Y)[] Stones = { (566, 713), (567, 713), (566, 714), (567, 714) };

    private static OpeningTests.Game NewGame()
    {
        var game = new OpeningTests.Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Grotle")!, 30));
        game.Arrive("Sinnoh", 566, 715);
        return game;
    }

    /// <summary>The tower read from the south, answering as told.</summary>
    private static HeadlessScriptHost Read(OpeningTests.Game game, BattleOutcome fight = BattleOutcome.Won, params int[] answers)
    {
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag)
        {
            Map = game.Map, PlayerTile = (566, 715), PlayerFacing = Direction.Up,
            Fight = _ => fight, NeedsPokemon = true
        };
        foreach (int answer in answers) host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(game.Map.TileScripts[(566, 714)], game.Map.ScriptFileAt(566, 714))!);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    private static void Greet(StoryState story, int people)
    {
        for (int i = 0; i < people; i++) story.Greet($"Sinnoh/somewhere/person_{i}");
    }

    private static string Said(HeadlessScriptHost host) => string.Join(" ", host.Transcript.Select(l => l.Text));

    [Fact]
    public void TheTowerIsReadFromEachOfItsStones()
    {
        var game = NewGame();
        foreach (var (x, y) in Stones)
        {
            Assert.Equal("HallowedTower", game.Map.TileScripts[(x, y)]);
            Assert.Equal("route_209", game.Map.ScriptFileAt(x, y));
            Assert.NotNull(Scripts.Find("HallowedTower", game.Map.ScriptFileAt(x, y)));
        }
        // The stones stand in the way: the tower is read, never walked into
        Assert.All(Stones, s => Assert.True(game.Map.IsSolid(s.X, s.Y)));
    }

    [Fact]
    public void WithoutTheKeystoneTheTowerIsOnlyBroken()
    {
        var game = NewGame();
        var host = Read(game);
        Assert.Contains("missing", Said(host));
        Assert.Empty(host.Asked);
        Assert.Equal(0, game.Story.Var("VAR_HALLOWED_TOWER_STATE"));
    }

    [Fact]
    public void TheKeystoneIsSetOnlyWhenThePlayerSaysSo()
    {
        var game = NewGame();
        game.Bag.AddItem(ItemDatabase.Get("Odd Keystone")!, 1);

        Read(game, answers: 1);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Odd Keystone")!));
        Assert.Equal(0, game.Story.Var("VAR_HALLOWED_TOWER_STATE"));

        var set = Read(game, answers: 0);
        Assert.Contains(set.Transcript, l => l.Text.Contains("set the Odd Keystone"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Odd Keystone")!));
        Assert.Equal(1, game.Story.Var("VAR_HALLOWED_TOWER_STATE"));
    }

    [Theory]
    [InlineData(0, "long time ago")]
    [InlineData(7, "long time ago")]
    [InlineData(8, "shifted")]
    [InlineData(14, "shifted")]
    [InlineData(15, "crying")]
    [InlineData(22, "trembling")]
    [InlineData(29, "presence")]
    [InlineData(31, "presence")]
    public void TheTowerStirsMoreTheMorePeopleHaveBeenMet(int people, string stirring)
    {
        var game = NewGame();
        game.Story.SetVar("VAR_HALLOWED_TOWER_STATE", 1);
        Greet(game.Story, people);

        var host = Read(game);
        Assert.Contains(stirring, Said(host));
        Assert.DoesNotContain(host.Log, l => l.StartsWith("wildbattle"));
        Assert.Equal(1, game.Story.Var("VAR_HALLOWED_TOWER_STATE"));
    }

    [Theory]
    [InlineData(BattleOutcome.Won)]
    [InlineData(BattleOutcome.Caught)]
    [InlineData(BattleOutcome.Fled)]
    public void AtThirtyTwoSpiritombComesOutAndTheTowerIsEmptyAgain(BattleOutcome outcome)
    {
        var game = NewGame();
        game.Story.SetVar("VAR_HALLOWED_TOWER_STATE", 1);
        Greet(game.Story, 32);

        var host = Read(game, outcome);
        Assert.Contains("cry Spiritomb", host.Log);
        Assert.Contains($"wildbattle Spiritomb 25 {outcome}", host.Log);
        Assert.Equal(0, game.Story.Var("VAR_HALLOWED_TOWER_STATE"));

        // Once: without another keystone the tower is a broken one again
        var after = Read(game);
        Assert.DoesNotContain(after.Log, l => l.StartsWith("wildbattle"));
        Assert.Contains("missing", Said(after));
    }

    [Fact]
    public void LosingToSpiritombLeavesItInTheTower()
    {
        var game = NewGame();
        game.Story.SetVar("VAR_HALLOWED_TOWER_STATE", 1);
        Greet(game.Story, 40);

        var host = Read(game, BattleOutcome.Lost);
        Assert.Contains("wildbattle Spiritomb 25 Lost", host.Log);
        Assert.Equal(1, game.Story.Var("VAR_HALLOWED_TOWER_STATE"));
        Assert.Contains("wildbattle Spiritomb 25 Won", Read(game).Log);
    }

    [Fact]
    public void WithNoPokemonToBattleSpiritombWaits()
    {
        var game = NewGame();
        game.Party.Clear();
        game.Story.SetVar("VAR_HALLOWED_TOWER_STATE", 1);
        Greet(game.Story, 32);

        var host = Read(game);
        Assert.DoesNotContain(host.Log, l => l.StartsWith("wildbattle"));
        Assert.Equal(1, game.Story.Var("VAR_HALLOWED_TOWER_STATE"));
    }

    [Fact]
    public void EveryoneSpokenToIsCountedOnceAndThingsAreNot()
    {
        var game = NewGame();
        var map = game.Map;
        var people = map.Everyone.Where(n => !n.IsThing && !n.IsPokemon).ToList();
        Assert.True(people.Count > 32);

        // Each person is someone of their own, whichever town they stand in
        var greetings = people.Select(n => FieldScripts.GreetingOf(map, n)).ToList();
        Assert.DoesNotContain(null, greetings);
        Assert.Equal(greetings.Count, greetings.Distinct().Count());

        // Things and Pokémon are nobody to greet
        Assert.Contains(map.Everyone, n => n.IsItemBall);
        Assert.All(map.Everyone.Where(n => n.IsThing || n.IsPokemon), n => Assert.Null(FieldScripts.GreetingOf(map, n)));

        // Speaking to someone twice counts them once, and moves the story on only the first time
        var story = new StoryState();
        string first = greetings[0]!;
        Assert.True(story.Greet(first));
        int revision = story.Revision;
        Assert.False(story.Greet(first));
        Assert.Equal(revision, story.Revision);
        Assert.False(story.Greet(""));
        foreach (string? someone in greetings.Skip(1).Take(31)) story.Greet(someone!);
        Assert.Equal(32, story.Greeted.Count);
    }

    [Fact]
    public void ThePeopleSpokenToGoThroughTheSave()
    {
        var story = new StoryState();
        story.Greet("Sinnoh/route_209/fisherman");
        story.Greet("Sinnoh/hearthome_city/gym_guide");
        story.Greet("ContestHallLobby//clown");

        var told = story.Snapshot();
        var save = new SaveData { GreetedPeople = told.Greeted!, StoryVersion = StoryState.CurrentVersion };
        var loaded = JsonSerializer.Deserialize<SaveData>(JsonSerializer.Serialize(save))!;
        var back = new StoryState();
        back.Restore(loaded.ToStory());
        Assert.Equal(story.Greeted.Order(), back.Greeted.Order());

        // A save from before has spoken to nobody
        var old = JsonSerializer.Deserialize<SaveData>("""{ "PlayerName": "Dawn", "StoryVersion": 6 }""")!;
        Assert.Empty(old.GreetedPeople);
        var fresh = new StoryState();
        fresh.Restore(old.ToStory());
        Assert.Empty(fresh.Greeted);
    }

    [Fact]
    public void ThePokedexsAreaPageNamesTheTower()
    {
        var habitats = Habitats.Sinnoh;
        Assert.NotNull(habitats);
        var tower = Assert.Single(habitats!.Of("Spiritomb"));
        Assert.Equal("Hallowed Tower", tower.Name);
        Assert.Equal(HabitatWays.Special, tower.Ways);
        Assert.False(string.IsNullOrWhiteSpace(tower.How));

        // Its cell is the chunk of the overworld the stones stand in, on the area page's own map of Sinnoh
        Assert.Equal((566 / 32, 713 / 32), Assert.Single(tower.Cells));
        Assert.NotEqual(' ', habitats.LookAt(566 / 32, 713 / 32));

        // The tables' own places are still theirs: a species of Route 209's grass keeps its ways
        Assert.All(habitats.Of("Bibarel"), h => Assert.Null(h.How));
    }
}
