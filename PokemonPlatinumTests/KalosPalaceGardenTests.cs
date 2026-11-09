using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// What Kalos still lacked after "Kalos finished" (plan 23): Parfum Palace's garden maze behind its hall with the owner's
/// fireworks after dark, and Camphrier castle's keeper showing the player round the first time they come in, with the
/// Leftovers once the Snorlax is woken. Played headless.
/// </summary>
public class KalosPalaceGardenTests
{
    private const string Hall = "KalosRoute6_ParfumPalace", Garden = "KalosRoute6_ParfumGarden", Castle = "CamphrierTown_Castle";
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Kalos"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Kalos)!.Start!));

    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    [Fact]
    public void TheGardenIsBehindTheHallAndEveryPathOfTheMazeIsWalked()
    {
        Assert.Contains(Garden, RegionDatabase.Get(RegionDatabase.Kalos)!.Maps);
        var hall = MapNamed(Hall);
        var back = hall.Warps.Single(w => w.TargetMap == Garden);
        var garden = MapNamed(Garden);
        Assert.True(garden.IsWalkable(back.TargetX, back.TargetY));
        var out_ = garden.Warps.Single();
        Assert.Equal(Hall, out_.TargetMap);
        Assert.True(hall.IsWalkable(out_.TargetX, out_.TargetY));

        // The walk of the whole game comes into the garden and to every open tile of the maze, the fountain's plaza and the
        // item at the far dead end among them
        var walked = Walked.Value.Single(p => p.Key.Name == Garden).Value;
        for (int y = 0; y < garden.Height; y++)
            for (int x = 0; x < garden.Width; x++)
                if (garden.IsWalkable(x, y) && garden.GetNpcAt(x, y) == null)
                    Assert.True(walked.Contains((x, y)), $"the maze's tile {x}, {y} is never come to");
        Assert.Contains((13, 13), walked);
        Assert.Contains(garden.Everyone, n => n.IsItemBall && n.GridX == 1 && n.GridY == 1);
        Assert.Contains(garden.Props, p => p.Type == PropType.Fountain);
        Assert.True(garden.Props.Count(p => p.Type == PropType.Hedge) > 40, "the maze is hedges");
    }

    [Fact]
    public void TheOwnerWaitsByTheFountainOnlyInTheEveningAfterHisThanks()
    {
        var garden = MapNamed(Garden);
        foreach (var (time, gift, there) in new[]
                 {
                     (TimeOfDay.Night, false, false), (TimeOfDay.Day, true, false), (TimeOfDay.Morning, true, false),
                     (TimeOfDay.Twilight, true, true), (TimeOfDay.Night, true, true), (TimeOfDay.LateNight, true, true),
                 })
        {
            var story = new StoryState();
            if (gift) story.Set("FLAG_KALOS_PALACE_GIFT");
            Play(story, Garden, "OnEnter", garden, time: time);
            garden.ApplyPresence(story.Has);
            Assert.Equal(there, garden.NPCs.Any(n => n.Key == "owner"));
        }
    }

    [Fact]
    public void TheFireworksArePlayedWhenAskedForAndMayBeWatchedAgain()
    {
        var garden = MapNamed(Garden);
        var story = new StoryState();
        story.Set("FLAG_KALOS_PALACE_GIFT");
        var owner = garden.Everyone.Single(n => n.Key == "owner");
        var no = Play(story, Garden, "Owner", garden, owner, answers: new[] { 1 }, time: TimeOfDay.Night);
        Assert.False(story.Has("FLAG_KALOS_PALACE_FIREWORKS"));
        Assert.DoesNotContain(no.Log, l => l.StartsWith("effect", StringComparison.Ordinal));
        var yes = Play(story, Garden, "Owner", garden, owner, answers: new[] { 0 }, time: TimeOfDay.Night);
        Assert.True(story.Has("FLAG_KALOS_PALACE_FIREWORKS"));
        Assert.True(yes.Log.Count(l => l.StartsWith("effect sparkles", StringComparison.OrdinalIgnoreCase)) >= 10, string.Join("\n", yes.Log));
        var again = Play(story, Garden, "Owner", garden, owner, answers: new[] { 0 }, time: TimeOfDay.Night);
        Assert.Contains(again.Log, l => l.StartsWith("effect burst", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheCastlesKeeperShowsThePlayerRoundOnceAndGivesTheLeftoversOnceTheSnorlaxIsWoken()
    {
        var castle = MapNamed(Castle);
        var story = new StoryState();
        story.SetVar("VAR_KALOS_STORY", 14);
        // Not home yet: no tour
        Play(story, Castle, "OnEnter", castle, spot: (7, 10));
        Assert.False(story.Has("FLAG_KALOS_CASTLE_TOUR"));

        story.Set("FLAG_KALOS_HIDE_CASTELLAN_STREET");
        var keeper = castle.Everyone.Single(n => n.Key == "castellan");
        var tour = Play(story, Castle, "OnEnter", castle, spot: (7, 10));
        Assert.True(story.Has("FLAG_KALOS_CASTLE_TOUR"));
        Assert.Contains(tour.Transcript, l => l.Text.Contains("Snorlax", StringComparison.Ordinal));
        Assert.Equal((7, 4), (keeper.GridX, keeper.GridY));
        // Only once
        var again = Play(story, Castle, "OnEnter", castle, spot: (7, 10));
        Assert.Empty(again.Transcript);

        var bag = new Inventory();
        Play(story, Castle, "Castellan", castle, keeper, bag: bag);
        Assert.Equal(0, bag.GetQuantity(ItemDatabase.Get("Leftovers")!));
        story.SetVar("VAR_KALOS_STORY", 16);
        Play(story, Castle, "Castellan", castle, keeper, bag: bag);
        Play(story, Castle, "Castellan", castle, keeper, bag: bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Leftovers")!));
    }

    private static HeadlessScriptHost Play(StoryState story, string file, string script, Map map, NPC? who = null, int[]? answers = null,
        Inventory? bag = null, TimeOfDay time = TimeOfDay.Day, (int X, int Y)? spot = null)
    {
        map.ForgetForced();
        map.ApplyPresence(story.Has);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 80));
        var host = new HeadlessScriptHost(story, party, bag ?? new Inventory())
        {
            Map = map, Region = RegionDatabase.Kalos, MapNamed = MapNamed, TimeOfDay = time,
        };
        if (spot is { } tile) host.PlayerTile = tile;
        foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }
}
