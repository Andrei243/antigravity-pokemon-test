using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The north of Sinnoh and the way to the League as plan 01 · M8 opened them: Route 211 and Mt. Coronet to Spear
/// Pillar, Routes 216 and 217, Acuity Lakefront, Lake Acuity and Snowpoint City in the snow; Route 222, Sunyshore
/// City, Route 223, Victory Road and the Pokémon League.
/// </summary>
public class NorthTests
{
    // Maps of this class's own, so nothing another test does to the database's maps shows here. Read only.
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));

    private static Map Overworld => BuiltMaps.Value["Sinnoh"];

    [Theory]
    [InlineData("snowpoint_city", true)]
    [InlineData("route_216", true)]
    [InlineData("route_217", true)]
    [InlineData("acuity_lakefront", true)]
    [InlineData("route_211_west", false)]
    [InlineData("sunyshore_city", false)]
    [InlineData("twinleaf_town", false)]   // its patches of snow don't make it snow country
    public void SnowCountryIsWhereSnowCoversMoreThanLawn(string area, bool snowbound)
    {
        var found = Overworld.FindArea(area);
        Assert.NotNull(found);
        Assert.True(found!.Open, area);
        Assert.Equal(snowbound, found.Snowbound);
    }

    [Fact]
    public void SpearPillarIsStoneAndNoForest()
    {
        // Its floor's textures (dun08_chip_*, colum_*) are paving, and the two blocked tiles beside its way in that
        // the original draws nothing on are rock, not a pine (World.BareRock): it was lawn and trees
        var pillar = BuiltMaps.Value["SpearPillar"];
        Assert.Equal(TileType.Paving, pillar.GetGroundTile(31, 40));
        for (int y = 0; y < pillar.Height; y++)
            for (int x = 0; x < pillar.Width; x++)
                Assert.NotEqual(TileType.Tree, pillar.GetGroundTile(x, y));
        Assert.DoesNotContain(Enumerable.Range(0, pillar.Height).SelectMany(y => Enumerable.Range(0, pillar.Width).Select(x => (x, y))),
            t => !pillar.IsSolid(t.x, t.y) && pillar.GetGroundTile(t.x, t.y) == TileType.Grass);
    }

    [Theory]
    [InlineData("route_216")]
    [InlineData("acuity_lakefront")]
    [InlineData("snowpoint_city")]
    public void TheCalendarsOtherThreePlacesTakeTheirWeatherFromIt(string area)
    {
        // Plan 01 · M7 imported sYearlyWeather; M8 opens the three places that follow it in the north
        var found = Overworld.FindArea(area)!;
        Assert.Equal(366, found.Calendar?.Count);
    }

    [Fact]
    public void FlyReachesTheLeagueOnceItsPokemonCentersHaveBeenComeTo()
    {
        var story = new StoryState();
        bool Open(string key) => Overworld.FindArea(key)?.Open == true;
        Assert.DoesNotContain(SpawnLocations.FlyDestinations(story, Open), s => s.Area == "pokemon_league");

        // The rooms' own scripts set the original's flags as the player comes in
        var host = new HeadlessScriptHost(story);
        foreach (string room in new[] { "PokemonLeagueSouthPokemonCenter", "PokemonLeagueNorthPokemonCenter" })
        {
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.In(room, ScriptLibrary.OnEnter)!);
            runner.RunToEnd();
        }
        var league = SpawnLocations.FlyDestinations(story, Open).Where(s => s.Area == "pokemon_league").Select(s => s.Id).ToList();
        Assert.Equal(new[] { 15, 20 }, league);
        // Pal Park's lobby is a place to come back to, never one to fly to
        story.Set(SpawnLocations.Get(SpawnLocations.PalParkLobby)!.ArrivalFlag);
        Assert.DoesNotContain(SpawnLocations.FlyDestinations(story, _ => true), s => s.Id == SpawnLocations.PalParkLobby);
    }

    [Theory]
    [InlineData("SnowpointPokeMart", "snowpoint")]
    [InlineData("SunyshorePokeMart", "sunyshore")]
    [InlineData("PokemonLeagueNorthPokemonCenter", "pokemon_league")]
    public void TheNorthsCountersSellTheirOwnGoods(string room, string counter)
    {
        var map = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, room + ".json")).ToMap();
        var clerks = map.Everyone.Where(n => n.IsPokeMartClerk).ToList();
        Assert.Contains(clerks, n => n.Mart == null);
        Assert.Contains(clerks, n => n.Mart == counter);
        Assert.NotEmpty(MartDatabase.Stock(counter, 8));
    }
}
