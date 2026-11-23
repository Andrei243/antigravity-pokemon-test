using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Paldea's Pokémon League (plan 34 · PD8, its first session): the League's building on its terrace under Mesagoza's
/// south wall, its lobby with Rika's interview and the battle court behind it, the Elite Four (Rika, Poppy, Larry and
/// Hassel), the Top Champion Geeta, Nemona's battle as the champion assessment's end, and <c>PaldeaHallOfFame</c> set with
/// the credits and home; refused without the eight badges.
/// </summary>
public class PaldeaLeagueTests
{
    private static Region Region => RegionDatabase.Get(RegionDatabase.Paldea)!;
    private static readonly Lazy<OpenMap> Open = new(() => OpenRegions.Load(Region).Single());
    private static readonly Lazy<Func<string, PokemonPlatinumEngine.Overworld.Map>> Rooms = new(MapDatabase.Fresh);

    private sealed class Game
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();

        public Game(string starter = "Fuecoco")
        {
            StoryMigration.BeginNewGame(Story, ScriptLibrary.Default);
            Party.Add(new Pokemon(PokemonDatabase.Get(starter)!, 62, new Random(0)));
            Story.ChooseStarter(starter);
            Story.SetVar("VAR_PALDEA_STORY", 7);
        }

        public HeadlessScriptHost Play(string file, string script, params int[] answers)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Rooms.Value(file),
                OpenMapNamed = name => name == Open.Value.Name ? Open.Value : null,
                Region = RegionDatabase.Paldea
            };
            foreach (int a in answers) host.Answers.Enqueue(a);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(script, file)!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }

        public static string? Battled(HeadlessScriptHost host) => host.Log.FirstOrDefault(l => l.StartsWith("battle "));
    }

    private static readonly (string Script, string Trainer)[] Court =
    {
        ("Rika", "paldea_rika"), ("Poppy", "paldea_poppy"), ("Larry", "paldea_larry_elite"), ("Hassel", "paldea_hassel"),
        ("Geeta", "paldea_geeta"),
    };

    [Fact]
    public void TheLeagueStandsUnderMesagozasSouthWallWithItsLobbyAndCourt()
    {
        var map = Open.Value;
        var door = map.Doors.Single(d => d.Room == "PaldeaLeague_Lobby");
        Assert.Equal("south_province_area_one", map.AreaAt(map.Marker("league_door")!.Pos)?.Key);
        // Just south of Mesagoza's wall (z -300), east of the road north
        Assert.InRange(map.Marker("league_door")!.Pos.MetresZ, -300, -270);
        Assert.True(map.Marker("league_door")!.Pos.MetresX > 20);
        Assert.Contains(RegionDatabase.Get(RegionDatabase.Paldea)!.Maps, m => m == "PaldeaLeague_Court");
        var lobby = Rooms.Value("PaldeaLeague_Lobby");
        Assert.Contains(lobby.NPCs, n => n.IsHealingNurse);
        Assert.Contains(lobby.NPCs, n => n.Script == "Interview");
        var court = Rooms.Value("PaldeaLeague_Court");
        foreach (var (script, _) in Court) Assert.Contains(court.NPCs, n => n.Script == script);
        Assert.Contains(court.NPCs, n => n.Script == "Nemona");
        Assert.NotNull(door);
    }

    [Fact]
    public void TheLeagueIsWonFromEightBadgesToTheHallOfFame()
    {
        var game = new Game();
        game.Story.SetBadges(RegionDatabase.Paldea, 0xFF);
        Assert.False(game.Story.Has(Region.StoryCompleteFlag));
        // The court asks for the interview first
        Assert.Null(Game.Battled(game.Play("PaldeaLeague_Court", "Rika")));
        // The interview: three questions, every answer heard
        game.Play("PaldeaLeague_Lobby", "Interview", 2, 2, 0);
        Assert.Equal(1, game.Story.Var("VAR_PALDEA_LEAGUE"));
        Assert.True(game.Story.Has("FLAG_PALDEA_LEAGUE_INTERVIEWED"));
        // The court shows whoever is due as it is come to, and each in turn battles at the original's levels
        int step = 1;
        foreach (var (script, trainer) in Court)
        {
            game.Play("PaldeaLeague_Court", "OnEnter");
            Assert.True(game.Story.Has("FLAG_PALDEA_LEAGUE_" + script.ToUpperInvariant()), script);
            string? battle = Game.Battled(game.Play("PaldeaLeague_Court", script));
            Assert.True(battle != null && battle.Contains(trainer), $"{script}: {battle}");
            Assert.Equal(++step, game.Story.Var("VAR_PALDEA_LEAGUE"));
            // Spoken to again, nobody battles twice
            Assert.Null(Game.Battled(game.Play("PaldeaLeague_Court", script)));
        }
        // Nemona ends the assessment with the team strong against the player's partner (Quaxly's line against Fuecoco)
        game.Play("PaldeaLeague_Court", "OnEnter");
        Assert.True(game.Story.Has("FLAG_PALDEA_LEAGUE_NEMONA"));
        var last = game.Play("PaldeaLeague_Court", "Nemona");
        Assert.Contains("paldea_nemona_8b", Game.Battled(last));
        Assert.Contains("halloffame", last.Log);
        Assert.Contains(last.Log, l => l.StartsWith("warp CaboPoco_PlayersHouse_2F"));
        Assert.True(game.Story.Has("PaldeaHallOfFame"));
        Assert.Equal(7, game.Story.Var("VAR_PALDEA_LEAGUE"));
        Assert.True(Objectives.Find("paldeaLeague")!.Done!(game.Story));
        // The region's Hall of Fame counts as the others' do: with Galar's, the hub opens
        game.Story.Set("GalarHallOfFame");
        Assert.True(RegionDatabase.HubOpen(game.Story) || RegionDatabase.Get(RegionDatabase.Hub) is not { IsBuilt: true });
        // Afterwards, nobody battles again
        game.Play("PaldeaLeague_Court", "OnEnter");
        Assert.Null(Game.Battled(game.Play("PaldeaLeague_Court", "Nemona")));
    }

    [Fact]
    public void TheInterviewIsRefusedWithoutEightBadges()
    {
        var game = new Game();
        game.Story.SetBadges(RegionDatabase.Paldea, 0x7F);
        game.Play("PaldeaLeague_Lobby", "Interview", 0, 0, 0);
        Assert.Equal(0, game.Story.Var("VAR_PALDEA_LEAGUE"));
        game.Play("PaldeaLeague_Court", "OnEnter");
        Assert.False(game.Story.Has("FLAG_PALDEA_LEAGUE_RIKA"));
        Assert.Null(Game.Battled(game.Play("PaldeaLeague_Court", "Rika")));
        Assert.False(game.Story.Has("PaldeaHallOfFame"));
    }

    [Fact]
    public void NemonasTeamAnswersThePartnerAndTheLeaguesTeamsAreTheOriginalsLevels()
    {
        foreach (var (starter, id) in new[] { ("Sprigatito", "paldea_nemona_8a"), ("Fuecoco", "paldea_nemona_8b"), ("Quaxly", "paldea_nemona_8c") })
        {
            var game = new Game(starter);
            game.Story.SetVar("VAR_PALDEA_LEAGUE", 6);
            Assert.Contains(id, Game.Battled(game.Play("PaldeaLeague_Court", "Nemona")));
        }
        var caps = Region.File!.Caps;
        foreach (var (id, best, tera) in new[] { ("paldea_rika", 58, "Ground"), ("paldea_poppy", 59, "Steel"), ("paldea_larry_elite", 60, "Flying"), ("paldea_hassel", 61, "Dragon"), ("paldea_geeta", 62, "Rock") })
        {
            var record = TrainerDatabase.Get(id, "paldea")!;
            Assert.Equal(best, record.Party.Max(p => p.Level));
            Assert.Equal(tera, record.Party[^1].TeraType);
            Assert.True(record.TeraOrb);
            Assert.Equal(id == "paldea_geeta" ? "Champion" : "Elite Four", record.Class);
        }
        // Geeta's best is Paldea's last cap, the Champion's
        Assert.Equal(caps[^1], TrainerDatabase.Get("paldea_geeta", "paldea")!.Party.Max(p => p.Level));
        Assert.Equal(66, TrainerDatabase.Get("paldea_nemona_8a", "paldea")!.Party.Max(p => p.Level));
    }
}
