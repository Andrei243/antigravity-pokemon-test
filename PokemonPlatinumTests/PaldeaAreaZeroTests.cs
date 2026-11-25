using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Area Zero and the Way Home (plan 34 · PD8, its second session): the Zero Gate in the North Province, shut until the
/// League, the Path of Legends and Starfall Street are done and the friends have met before it; the crater's four
/// research stations down a cave-set grid map; the Zero Lab with the professor's AI (Sada or Turo by the version) and
/// the legendary in its battle form beside the player; and the Way Home, which sets <c>FLAG_PALDEA_WAY_HOME</c>.
/// </summary>
public class PaldeaAreaZeroTests
{
    private static Region Region => RegionDatabase.Get(RegionDatabase.Paldea)!;
    private static readonly Lazy<OpenMap> Open = new(() => OpenRegions.Load(Region).Single());
    private static readonly Lazy<Func<string, PokemonPlatinumEngine.Overworld.Map>> Rooms = new(MapDatabase.Fresh);

    private sealed class Game
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();

        public Game(int version = 1)
        {
            StoryMigration.BeginNewGame(Story, ScriptLibrary.Default);
            Party.Add(new Pokemon(PokemonDatabase.Get("Skeledirge")!, 70, new Random(0)));
            Story.ChooseStarter("Fuecoco");
            Story.SetVar("VAR_PALDEA_STORY", 7);
            Story.SetVar("VAR_PALDEA_VERSION", version);
            Story.SetVar("VAR_PALDEA_MOUNT", version);
        }

        public HeadlessScriptHost Play(string file, string script)
        {
            bool room = char.IsUpper(file[0]);
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = room ? Rooms.Value(file) : null,
                OpenMap = room ? null : Open.Value,
                OpenMapNamed = name => name == Open.Value.Name ? Open.Value : null,
                Region = RegionDatabase.Paldea
            };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(script, file)!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }

        public bool GateOpen => Open.Value.Doors.Single(d => d.Room == "AreaZero_Gate").Open(Story.Has);

        /// <summary>The three paths behind the player: the League's Hall of Fame, the fifth Titan and Arven's battle, the fifth base.</summary>
        public void AllThreePathsDone()
        {
            Story.Set("PaldeaHallOfFame");
            Story.SetVar("VAR_PALDEA_LEGENDS", 12);
            Story.SetVar("VAR_PALDEA_STARFALL", 6);
        }
    }

    [Fact]
    public void TheZeroGateStandsInTheNorthProvinceWithAreaZerosRoomsBehindIt()
    {
        var map = Open.Value;
        var door = map.Doors.Single(d => d.Room == "AreaZero_Gate");
        Assert.Equal("FLAG_PALDEA_ZERO_GATE_OPEN", door.OpenedBy);
        Assert.Equal("north_province_area_one", map.AreaAt(map.Marker("zero_gate_door")!.Pos)?.Key);
        Assert.Equal("north_province_area_one", map.AreaAt(map.Marker("zero_gate_front")!.Pos)?.Key);
        foreach (string room in new[] { "AreaZero_Gate", "AreaZero", "AreaZero_ZeroLab" })
            Assert.Contains(Region.Maps, m => m == room);
        // The gate's lift leads down into the crater, and the crater's way in at its bottom to the lab
        Assert.Contains(Rooms.Value("AreaZero_Gate").Warps, w => w.TargetMap == "AreaZero");
        var crater = Rooms.Value("AreaZero");
        Assert.Contains(crater.Warps, w => w.TargetMap == "AreaZero_ZeroLab");
        // Four research stations, each a trigger across the whole way down at its own step of the count
        string[] stations = { "StationOne", "StationTwo", "StationThree", "StationFour" };
        for (int i = 0; i < stations.Length; i++)
            Assert.Contains(crater.Triggers, t => t.Script == stations[i] && t.Value == i + 1);
        var lab = Rooms.Value("AreaZero_ZeroLab");
        foreach (string who in new[] { "ai_sada", "ai_turo", "koraidon_ai", "miraidon_ai", "arven", "nemona", "penny" })
            Assert.Contains(lab.Everyone, n => n.Id == who);
    }

    [Fact]
    public void TheGateIsShutBeforeTheLeagueAndOpensWithTheFriendsAfter()
    {
        var game = new Game();
        // Before the League nobody waits and the door stays shut
        Assert.False(game.GateOpen);
        game.Story.SetVar("VAR_PALDEA_LEGENDS", 12);
        game.Story.SetVar("VAR_PALDEA_STARFALL", 6);
        game.Play("north_province_area_one", "ZeroGate");
        Assert.False(game.GateOpen);
        // The League won, but a Titan still out: Arven holds back
        game.Story.Set("PaldeaHallOfFame");
        game.Story.SetVar("VAR_PALDEA_LEGENDS", 11);
        game.Play("north_province_area_one", "ZeroGate");
        Assert.False(game.GateOpen);
        // Everything done: the meeting, and the gate opens
        game.AllThreePathsDone();
        game.Play("north_province_area_one", "ZeroGate");
        Assert.True(game.GateOpen);
        Assert.Equal(1, game.Story.Var("VAR_PALDEA_ZERO"));
        // The friends are gone from before the gate once it is open (they went down ahead)
        var friends = Open.Value.People.Where(p => p.Key.EndsWith("_zero_gate")).ToList();
        Assert.Equal(3, friends.Count);
        Assert.All(friends, p => Assert.Equal(("PaldeaHallOfFame", "FLAG_PALDEA_ZERO_GATE_OPEN"), (p.ShownBy, p.HiddenBy)));
    }

    [Theory]
    [InlineData(1, "paldea_ai_sada", "paldea_ai_sada_legend", "paldea_partner_koraidon", "Koraidon")]
    [InlineData(2, "paldea_ai_turo", "paldea_ai_turo_legend", "paldea_partner_miraidon", "Miraidon")]
    public void TheStationsAreReachedTheAIAndTheLegendAreBeatenAndTheWayHomeIsTaken(int version, string ai, string legend, string partner, string mount)
    {
        var game = new Game(version);
        game.AllThreePathsDone();
        game.Play("north_province_area_one", "ZeroGate");
        // The crater: the version's Paradox Pokémon stand, the other version's are hidden
        game.Play("AreaZero", "OnEnter");
        Assert.Equal(version == 2, game.Story.Has("FLAG_PALDEA_ZERO_HIDE_PARADOX_A_SCARLET"));
        Assert.Equal(version == 1, game.Story.Has("FLAG_PALDEA_ZERO_HIDE_PARADOX_A_VIOLET"));
        // The four stations, each passed in turn
        int step = 1;
        foreach (string station in new[] { "StationOne", "StationTwo", "StationThree", "StationFour" })
        {
            game.Play("AreaZero", station);
            Assert.Equal(++step, game.Story.Var("VAR_PALDEA_ZERO"));
        }
        // The lab shows the version's professor and legendary
        game.Play("AreaZero_ZeroLab", "OnEnter");
        Assert.Equal(version == 2, game.Story.Has("FLAG_PALDEA_ZERO_HIDE_SADA"));
        Assert.Equal(version == 1, game.Story.Has("FLAG_PALDEA_ZERO_HIDE_TURO"));
        // The AI's battle, then the legendary's battle form beside the player's own, then the Way Home
        var end = game.Play("AreaZero_ZeroLab", "Professor");
        var battles = end.Log.Where(l => l.StartsWith("battle ")).ToList();
        Assert.Equal(2, battles.Count);
        Assert.Contains(ai, battles[0]);
        Assert.Contains(legend, battles[1]);
        Assert.Contains("with " + partner, battles[1]);
        Assert.Equal(8, game.Story.Var("VAR_PALDEA_ZERO"));
        Assert.True(game.Story.Has("FLAG_PALDEA_WAY_HOME"));
        Assert.True(Objectives.Find("paldeaAreaZero")!.Done!(game.Story));
        // The mount is on the team at last, home in Cabo Poco, the save and the credits
        Assert.Contains(game.Party.Members, p => p.Species.Name == mount);
        Assert.Contains(end.Log, l => l.StartsWith("warp CaboPoco_PlayersHouse_1F"));
        Assert.Contains("open Credits", end.Log);
        // Afterwards the lab is empty of the AI, and nobody battles again
        game.Play("AreaZero_ZeroLab", "OnEnter");
        Assert.True(game.Story.Has("FLAG_PALDEA_ZERO_HIDE_SADA") && game.Story.Has("FLAG_PALDEA_ZERO_HIDE_TURO"));
        Assert.DoesNotContain(game.Play("AreaZero_ZeroLab", "Professor").Log, l => l.StartsWith("battle "));
    }

    [Fact]
    public void TheAIsTeamsAreTheVersionsParadoxPokemonAndTheLegendsTheirBattleForms()
    {
        foreach (var (id, best, tera) in new[] { ("paldea_ai_sada", 67, "Dark"), ("paldea_ai_turo", 67, "Fairy") })
        {
            var record = TrainerDatabase.Get(id, "paldea")!;
            Assert.Equal(6, record.Party.Count);
            Assert.Equal(best, record.Party.Max(p => p.Level));
            Assert.Equal(tera, record.Party[^1].TeraType);
        }
        Assert.Equal(new[] { "Koraidon", "Roaring Moon" }, TrainerDatabase.Get("paldea_ai_sada_legend", "paldea")!.Party.Select(p => p.Species));
        Assert.Equal(new[] { "Miraidon", "Iron Valiant" }, TrainerDatabase.Get("paldea_ai_turo_legend", "paldea")!.Party.Select(p => p.Species));
        Assert.Equal("Koraidon", TrainerDatabase.Get("paldea_partner_koraidon", "paldea")!.Party.Single().Species);
        Assert.Equal("Miraidon", TrainerDatabase.Get("paldea_partner_miraidon", "paldea")!.Party.Single().Species);
    }

    [Fact]
    public void TheMeetingAtTheGatePlayedAndSkippedLeavesTheSame()
    {
        // The scene of an open place's person, which the walk of every script doesn't start (SceneSkipTests.BeyondTheWalk)
        var ends = new List<string>();
        foreach (bool hurry in new[] { false, true })
        {
            var game = new Game();
            game.AllThreePathsDone();
            var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag)
            {
                OpenMap = Open.Value,
                OpenMapNamed = name => name == Open.Value.Name ? Open.Value : null,
                Region = RegionDatabase.Paldea
            };
            var runner = new ScriptRunner(ScriptLibrary.Default, host) { Impatient = hurry };
            runner.Start(ScriptLibrary.Default.Find("ZeroGate", "north_province_area_one")!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            ends.Add(System.Text.Json.JsonSerializer.Serialize(game.Story.Snapshot()));
        }
        Assert.True(ends[0] == ends[1], $"the meeting skipped leaves otherwise than played:\n{ends[0]}\n{ends[1]}");
    }
}