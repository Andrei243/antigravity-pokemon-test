using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Hau'oli City's Trainers' School (plan 23, Melemele's last door; rulings, "Hau'oli City's Trainers' School"): a room of
/// ours behind a door of the city's street, four pupils who battle once each and a teacher who battles once all four are
/// beaten and gives three Quick Balls. Played headless.
/// </summary>
[Collection("Shared clocks and dice")]
public class AlolaTrainersSchoolTests
{
    private const string Room = "HauoliCity_TrainersSchool";
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    [Fact]
    public void TheSchoolsDoorOnHauolisStreetLeadsInAndOutAndItIsReachedFromTheNewHome()
    {
        var city = MapNamed("Melemele");
        var door = city.Warps.Single(w => w.TargetMap == Room);
        Assert.Equal("hauoli_city", city.AreaAt(door.SourceX, door.SourceY)?.Key);
        var room = MapNamed(Room);
        Assert.Equal(RegionDatabase.Alola, RegionDatabase.RegionOfMap(Room)?.Id);
        Assert.False(room.IsSolid(door.TargetX, door.TargetY));
        var back = room.Warps.Single();
        Assert.Equal("Melemele", back.TargetMap);
        Assert.True(city.IsWalkable(back.TargetX, back.TargetY));
        var walked = WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Alola)!.Start!);
        Assert.True(walked.Keys.Any(m => m.Name == Room), "the school is never reached");
    }

    [Fact]
    public void EachPupilBattlesOnceAndTheTeacherOnlyOnceAllFourAreBeatenThenGivesThreeQuickBalls()
    {
        var story = new StoryState();
        var party = new Party();
        var bag = new Inventory();
        party.Add(new Pokemon(PokemonDatabase.Get("Litten")!, 12));
        var room = Rooms.Value(Room);
        HeadlessScriptHost Play(string person)
        {
            var host = new HeadlessScriptHost(story, party, bag) { Map = room, MapNamed = MapNamed, Region = RegionDatabase.Alola };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            string script = person == "teacher" ? "Teacher" : char.ToUpperInvariant(person[0]) + person[1..];
            runner.Start(ScriptLibrary.Default.Find(script, Room)!, room.Everyone.Single(n => n.Key == person));
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }

        // Too early: the teacher sends the player to the pupils first
        Assert.DoesNotContain(Play("teacher").Log, l => l.StartsWith("battle"));
        foreach (var (person, id) in new[] { ("keahi", "alola_youngster_keahi"), ("pualani", "alola_lass_pualani"), ("aukai", "alola_school_kid_aukai"), ("kanoe", "alola_school_kid_kanoe") })
        {
            Assert.NotNull(TrainerDatabase.Get(id, RegionDatabase.Alola));
            Assert.Contains(Play(person).Log, l => l.StartsWith("battle " + id));
            Assert.DoesNotContain(Play(person).Log, l => l.StartsWith("battle"));
        }
        Assert.Contains(Play("teacher").Log, l => l.StartsWith("battle alola_teacher_iwalani"));
        Assert.Equal(3, bag.GetQuantity(ItemDatabase.Get("Quick Ball")!));
        Assert.DoesNotContain(Play("teacher").Log, l => l.StartsWith("battle"));
        Assert.Equal(3, bag.GetQuantity(ItemDatabase.Get("Quick Ball")!));
    }
}
