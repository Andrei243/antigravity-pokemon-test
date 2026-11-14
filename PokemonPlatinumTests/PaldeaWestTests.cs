using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Paldea's West Province and its two gyms (plan 34 · PD6, its first session): Asado Desert north of Cortondo with the
/// Quaking Earth Titan's basin at its south end, Cascarrafa by its falls, Porto Marinada on the western sea, the West
/// Province's first area with the Open Sky Titan's gully, and Medali; Kofu's errand to Porto Marinada's auction and
/// Larry's secret order at the Treasure Eatery, each leader's team scaled to the badges held (plan 34 · decision 3 at its
/// recommendation), and the badges four and five won in either order.
/// </summary>
public class PaldeaWestTests
{
    private static Region Region => RegionDatabase.Get(RegionDatabase.Paldea)!;
    private static readonly Lazy<OpenMap> Open = new(() => OpenRegions.Load(Region).Single());
    private static readonly Lazy<Func<string, PokemonPlatinumEngine.Overworld.Map>> Rooms = new(MapDatabase.Fresh);

    private sealed class Game
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();

        public Game()
        {
            StoryMigration.BeginNewGame(Story, ScriptLibrary.Default);
            Party.Add(new Pokemon(PokemonDatabase.Get("Fuecoco")!, 40, new Random(0)));
        }

        public HeadlessScriptHost Play(string file, string script, params int[] answers)
        {
            bool room = char.IsUpper(file[0]);
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = room ? Rooms.Value(file) : null,
                OpenMap = room ? null : Open.Value,
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

        public string? Battled(HeadlessScriptHost host) => host.Log.FirstOrDefault(l => l.Contains("paldea_kofu") || l.Contains("paldea_larry"));

        // Kofu's errand: asked, the crate won at the auction, brought back
        public HeadlessScriptHost Kofu()
        {
            Play("Cascarrafa_GymTest", "Leader");
            Assert.True(Story.Has("FLAG_PALDEA_CASCARRAFA_ERRAND"));
            Assert.False(Story.HasBadge(RegionDatabase.Paldea, "Water"));
            // Bid too soon: the chef takes the crate, and the lot is bid for again
            Play("porto_marinada", "Auctioneer", 0);
            Assert.False(Story.Has("FLAG_PALDEA_CASCARRAFA_TEST_DONE"));
            // Wait for the last call, then raise
            Play("porto_marinada", "Auctioneer", 1, 1, 0);
            Assert.True(Story.Has("FLAG_PALDEA_CASCARRAFA_TEST_DONE"));
            return Play("Cascarrafa_GymTest", "Leader");
        }

        // Larry's test: the three hints about Medali, the secret order at the eatery, then his office
        public HeadlessScriptHost Larry()
        {
            Play("Medali_GymTest", "Leader");
            Assert.False(Story.HasBadge(RegionDatabase.Paldea, "Normal"));
            // A wrong order is served from the menu, and the test isn't passed
            Play("Medali_TreasureEatery", "Order", 1);
            Assert.False(Story.Has("FLAG_PALDEA_MEDALI_TEST_DONE"));
            Play("medali", "Hint1"); Play("medali", "Hint2"); Play("medali", "Hint3");
            for (int n = 1; n <= 3; n++) Assert.True(Story.Has($"FLAG_PALDEA_MEDALI_TEST_{n}"));
            Play("Medali_TreasureEatery", "Order", 0, 0, 0);
            Assert.True(Story.Has("FLAG_PALDEA_MEDALI_TEST_DONE"));
            return Play("Medali_GymTest", "Leader");
        }
    }

    [Fact]
    public void TheWestProvinceIsAreasOfPaldeaWithTheirTownsRoomsAndTitans()
    {
        var map = Open.Value;
        foreach (string area in new[] { "asado_desert", "cascarrafa", "west_province_area_two", "porto_marinada", "west_province_area_one", "medali" })
        {
            Assert.Contains(map.Areas, a => a.Key == area);
            Assert.Equal("Paldea", OpenRegions.MapOfArea(area));
        }
        // The two Titans moved to their provinces: Bombirdier's gully off the West Province's first area, the Quaking Earth
        // Titan's basin at the desert's south end
        Assert.Equal("open_sky", map.AreaAt(map.People.Single(p => p.Key == "titan_bombirdier").Home)?.Key);
        Assert.Equal("quaking_earth", map.AreaAt(map.People.Single(p => p.Key == "titan_great_tusk").Home)?.Key);
        Assert.True(map.People.Single(p => p.Key == "titan_bombirdier").Home.MetresZ < -600);
        // Each town's Pokémon Center a spawn, a door and a room
        foreach (string room in new[] { "Cascarrafa_PokemonCenter", "PortoMarinada_PokemonCenter", "Medali_PokemonCenter" })
        {
            Assert.Contains(Region.File!.Spawns, s => s.Room == room);
            Assert.Contains(map.Doors, d => d.Room == room);
            Assert.Contains(Rooms.Value(room).NPCs, n => n.IsHealingNurse);
        }
        Assert.Contains(map.Doors, d => d.Room == "Medali_TreasureEatery");
        // The auctioneer stands on Porto Marinada's quay, and the three who know the secret order about Medali
        Assert.Equal("porto_marinada", map.AreaAt(map.People.Single(p => p.Key == "porto_auctioneer").Pos)?.Key);
        foreach (string hint in new[] { "medali_regular", "medali_waiter", "medali_office_worker" })
            Assert.Equal("medali", map.AreaAt(map.People.Single(p => p.Key == hint).Pos)?.Key);
        // Porto Marinada stands lower than Cascarrafa, on the sea
        Assert.True(map.Marker("porto_quay")!.Pos.MetresY < map.Marker("cascarrafa_square")!.Pos.MetresY - 5);
    }

    [Fact]
    public void KofuAndLarryAreWonInTheAcademysOrderAtTheOriginalsLevels()
    {
        var game = new Game();
        game.Story.SetVar("VAR_PALDEA_STORY", 7);
        foreach (string badge in new[] { "Bug", "Grass", "Electric" }) game.Story.GiveBadge(RegionDatabase.Paldea, badge);
        var kofu = game.Kofu();
        Assert.Contains("paldea_kofu ", game.Battled(kofu) + " ");
        Assert.True(game.Story.HasBadge(RegionDatabase.Paldea, "Water"));
        Assert.Equal("paldeaArven", Objectives.Current(game.Story, RegionDatabase.Paldea)?.Name);
        var larry = game.Larry();
        Assert.Contains("paldea_larry ", game.Battled(larry) + " ");
        Assert.True(game.Story.HasBadge(RegionDatabase.Paldea, "Normal"));
        Assert.Equal(5, game.Story.BadgeCount(RegionDatabase.Paldea));
        // Spoken to again, a leader gives nothing more
        game.Play("Cascarrafa_GymTest", "Leader");
        game.Play("Medali_GymTest", "Leader");
        Assert.Equal(5, game.Story.BadgeCount(RegionDatabase.Paldea));
        // In the academy's order the teams are the original's: Kofu's best at the fourth cap, Larry's at the fifth
        Assert.Equal(30, TrainerDatabase.Get("paldea_kofu", "paldea")!.Party.Max(p => p.Level));
        Assert.Equal(36, TrainerDatabase.Get("paldea_larry", "paldea")!.Party.Max(p => p.Level));
    }

    [Fact]
    public void TakenFirstTheWestsGymsAreScaledDownToTheBadgesHeld()
    {
        // Plan 34 · decision 3 at its recommendation: Larry first of all, then Kofu with one badge
        var game = new Game();
        game.Story.SetVar("VAR_PALDEA_STORY", 7);
        var larry = game.Larry();
        Assert.Contains("paldea_larry_b0", game.Battled(larry));
        Assert.True(game.Story.HasBadge(RegionDatabase.Paldea, "Normal"));
        var kofu = game.Kofu();
        Assert.Contains("paldea_kofu_b1", game.Battled(kofu));
        Assert.Equal(2, game.Story.BadgeCount(RegionDatabase.Paldea));
        // Each variant's best is the cap of the badges held, and each Terastallizes its last into the gym's type
        var caps = Region.File!.Caps;
        for (int held = 0; held < 8; held++)
        {
            string kofuId = held == 3 ? "paldea_kofu" : $"paldea_kofu_b{held}";
            string larryId = held == 4 ? "paldea_larry" : $"paldea_larry_b{held}";
            foreach (var (id, tera) in new[] { (kofuId, "Water"), (larryId, "Normal") })
            {
                var record = TrainerDatabase.Get(id, "paldea")!;
                Assert.Equal("Leader", record.Class);
                Assert.True(record.TeraOrb);
                Assert.Equal(caps[held], record.Party.Max(p => p.Level));
                Assert.Equal(tera, record.Party[^1].TeraType);
            }
        }
    }

    [Fact]
    public void TheAuctionPlayedAndSkippedLeavesTheSame()
    {
        // The scene of an open place's person, which the walk of every script doesn't start (SceneSkipTests.BeyondTheWalk):
        // each way through it, won, bid too soon and missed, played and then hurried
        foreach (int[] answers in new[] { new[] { 1, 1, 0 }, new[] { 0 }, new[] { 1, 0 }, new[] { 1, 1, 1 } })
        {
            var ends = new List<string>();
            foreach (bool hurry in new[] { false, true })
            {
                var game = new Game();
                game.Story.SetVar("VAR_PALDEA_STORY", 7);
                game.Story.Set("FLAG_PALDEA_CASCARRAFA_ERRAND");
                var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag)
                {
                    OpenMap = Open.Value,
                    OpenMapNamed = name => name == Open.Value.Name ? Open.Value : null,
                    Region = RegionDatabase.Paldea
                };
                foreach (int a in answers) host.Answers.Enqueue(a);
                var runner = new ScriptRunner(ScriptLibrary.Default, host) { Impatient = hurry };
                runner.Start(ScriptLibrary.Default.Find("Auctioneer", "porto_marinada")!, null);
                runner.RunToEnd();
                Assert.Empty(host.Problems);
                Assert.Contains("porto_marinada.The auction", game.Story.ScenesSeen);
                ends.Add(System.Text.Json.JsonSerializer.Serialize(game.Story.Snapshot()) + "|" + string.Join(",", host.Asked.Select(q => q.Answer)));
            }
            Assert.True(ends[0] == ends[1], $"the auction answered {string.Join(".", answers)} skipped leaves otherwise than played:\n{ends[0]}\n{ends[1]}");
        }
    }

    [Fact]
    public void BeforeTheWelcomeNeitherLeaderBattlesAndTheirTestsWait()
    {
        var game = new Game();
        game.Play("Cascarrafa_GymTest", "Leader");
        game.Play("Medali_GymTest", "Leader");
        Assert.False(game.Story.Has("FLAG_PALDEA_CASCARRAFA_ERRAND"));
        Assert.Equal(0, game.Story.BadgeCount(RegionDatabase.Paldea));
        // The auctioneer sells nothing to someone Kofu hasn't sent
        game.Play("porto_marinada", "Auctioneer", 1, 1, 0);
        Assert.False(game.Story.Has("FLAG_PALDEA_CASCARRAFA_TEST_DONE"));
    }
}
