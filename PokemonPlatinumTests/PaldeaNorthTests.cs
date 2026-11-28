using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Paldea's North Province and its last three gyms (plan 34 · PD6, its second session): the North Province's first area
/// north of Medali to the foot of Glaseado Mountain and its snowslope up to the top, Montenevera east along the foot, the
/// East Province's third area south to Levincia, Alfornada through its cavern west of Cortondo, and the last two Titans
/// moved out of their stand-ins (Lurking Steel north of Levincia, False Dragon at Casseroya Lake); Ryme's rap battle,
/// Tulip's photographs and Grusha's snowslope run, each leader scaled to the badges held as Kofu and Larry are; and the
/// done-when's eight badges won in the academy's order and in another.
/// </summary>
public class PaldeaNorthTests
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
            Party.Add(new Pokemon(PokemonDatabase.Get("Fuecoco")!, 50, new Random(0)));
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

        public static string? Battled(HeadlessScriptHost host, string leader) => host.Log.FirstOrDefault(l => l.Contains("paldea_" + leader));

        /// <summary>A gym won: its test passed as the town's own scripts pass it, then its leader; the leader's battle.</summary>
        public string? Win(string badge)
        {
            switch (badge)
            {
                case "Bug":
                    foreach (string test in new[] { "Test1", "Test2", "Test3" }) Play("Cortondo_GymTest", test);
                    return Battled(Play("Cortondo_GymTest", "Leader"), "katy");
                case "Grass":
                    foreach (string test in new[] { "HiddenSunflora1", "HiddenSunflora2", "HiddenSunflora3" }) Play("artazon", test);
                    return Battled(Play("Artazon_GymTest", "Leader"), "brassius");
                case "Electric":
                    foreach (string test in new[] { "HiddenWattrel1", "HiddenWattrel2", "HiddenWattrel3" }) Play("levincia", test);
                    return Battled(Play("Levincia_GymTest", "Leader"), "iono");
                case "Water":
                    Play("Cascarrafa_GymTest", "Leader");
                    Play("porto_marinada", "Auctioneer", 1, 1, 0);
                    return Battled(Play("Cascarrafa_GymTest", "Leader"), "kofu");
                case "Normal":
                    Play("medali", "Hint1"); Play("medali", "Hint2"); Play("medali", "Hint3");
                    Play("Medali_TreasureEatery", "Order", 0, 0, 0);
                    return Battled(Play("Medali_GymTest", "Leader"), "larry");
                case "Ghost":
                    Play("Montenevera_GymTest", "RapBattle", 0, 1, 1);
                    return Battled(Play("Montenevera_GymTest", "Leader"), "ryme");
                case "Psychic":
                    Play("Alfornada_GymTest", "Photos", 0, 1, 1);
                    return Battled(Play("Alfornada_GymTest", "Leader"), "tulip");
                case "Ice":
                    Run();
                    return Battled(Play("Glaseado_GymTest", "Leader"), "grusha");
            }
            throw new ArgumentException(badge);
        }

        // Grusha's run: the coach at the top, the three gates in order, the line at the foot
        public void Run()
        {
            Play("glaseado_mountain", "Coach", 0);
            Assert.Equal(1, Story.Var("VAR_PALDEA_GLASEADO_RUN"));
            Play("glaseado_mountain", "GateOne");
            Play("glaseado_mountain", "GateTwo");
            Play("glaseado_mountain", "GateThree");
            Assert.Equal(4, Story.Var("VAR_PALDEA_GLASEADO_RUN"));
            Play("glaseado_mountain", "Finish");
            Assert.True(Story.Has("FLAG_PALDEA_GLASEADO_TEST_DONE"));
            Assert.Equal(0, Story.Var("VAR_PALDEA_GLASEADO_RUN"));
        }
    }

    private static readonly string[] AcademysOrder = { "Bug", "Grass", "Electric", "Water", "Normal", "Ghost", "Psychic", "Ice" };

    [Fact]
    public void TheNorthIsAreasOfPaldeaWithItsTownsRoomsAndTheLastTwoTitans()
    {
        var map = Open.Value;
        foreach (string area in new[] { "north_province_area_one", "glaseado_mountain", "montenevera", "east_province_area_three", "alfornada_cavern", "alfornada" })
        {
            Assert.Contains(map.Areas, a => a.Key == area);
            Assert.Equal("Paldea", OpenRegions.MapOfArea(area));
        }
        // The last two Titans moved out of their stand-ins east off the East Province's first area: Orthworm north of
        // Levincia in its cutting off the third area, the Tatsugiri and its Dondozo at Casseroya Lake in the north
        Assert.Equal("lurking_steel", map.AreaAt(map.People.Single(p => p.Key == "titan_orthworm").Home)?.Key);
        Assert.True(map.People.Single(p => p.Key == "titan_orthworm").Home.MetresZ < -880);
        Assert.Equal("false_dragon", map.AreaAt(map.People.Single(p => p.Key == "titan_tatsugiri").Home)?.Key);
        Assert.Equal("false_dragon", map.AreaAt(map.People.Single(p => p.Key == "titan_dondozo").Home)?.Key);
        Assert.True(map.People.Single(p => p.Key == "titan_dondozo").Home.MetresZ < -800);
        Assert.Equal("Casseroya Lake", map.Areas.Single(a => a.Key == "false_dragon").Name);
        // Each new town's Pokémon Center a spawn, a door and a room; the gyms' doors, and their tests' people in the halls
        foreach (string room in new[] { "Montenevera_PokemonCenter", "Alfornada_PokemonCenter", "Glaseado_PokemonCenter" })
        {
            Assert.Contains(Region.File!.Spawns, s => s.Room == room);
            Assert.Contains(map.Doors, d => d.Room == room);
            Assert.Contains(Rooms.Value(room).NPCs, n => n.IsHealingNurse);
        }
        foreach (string gym in new[] { "Montenevera_Gym", "Alfornada_Gym", "Glaseado_Gym" })
            Assert.Contains(map.Doors, d => d.Room == gym);
        Assert.Contains(Rooms.Value("Montenevera_GymTest").NPCs, n => n.Script == "RapBattle");
        Assert.Contains(Rooms.Value("Alfornada_GymTest").NPCs, n => n.Script == "Photos");
        // Glaseado's gym and Pokémon Center stand at the top of the snowslope, 40 m over its foot, and the coach with them
        Assert.True(map.Marker("glaseado_top")!.Pos.MetresY > map.Marker("glaseado_foot")!.Pos.MetresY + 35);
        Assert.Equal("glaseado_mountain", map.AreaAt(map.People.Single(p => p.Key == "glaseado_coach").Pos)?.Key);
        // The run's three gates are passed in their order, and the line at the foot ends it whichever gate was reached
        var run = map.Triggers.Where(t => t.Variable == "VAR_PALDEA_GLASEADO_RUN").ToList();
        Assert.Equal(new[] { ("GateOne", 1), ("GateTwo", 2), ("GateThree", 3), ("Finish", 4), ("Missed", 1), ("Missed", 2), ("Missed", 3) },
            run.Select(t => (t.Script, t.Value)));
        // Each gate lies lower down the slope than the one before it, and the line lowest of all
        var heights = run.Take(4).Select(t => t.Shape.Base.MetresY).ToList();
        Assert.True(heights.SequenceEqual(heights.OrderByDescending(y => y)), string.Join(", ", heights));
    }

    [Fact]
    public void TheEightBadgesAreWonInTheAcademysOrderAndInAnother()
    {
        // Plan 34 · PD6's done-when: the eight gyms won in the academy's suggested order, the later leaders meeting their
        // originals' teams there, and in another order (the last three first and the first three last), each later
        // leader's team then scaled to the badges held (plan 34 · decision 3 at its recommendation)
        var orders = new[]
        {
            AcademysOrder,
            new[] { "Ice", "Psychic", "Ghost", "Normal", "Water", "Electric", "Grass", "Bug" },
        };
        var originals = new Dictionary<string, string> { ["Water"] = "kofu ", ["Normal"] = "larry ", ["Ghost"] = "ryme ", ["Psychic"] = "tulip ", ["Ice"] = "grusha " };
        foreach (var order in orders)
        {
            var game = new Game();
            // No leader battles before the academy's welcome
            game.Play("Montenevera_GymTest", "Leader");
            game.Play("Alfornada_GymTest", "Leader");
            game.Play("Glaseado_GymTest", "Leader");
            Assert.Equal(0, game.Story.BadgeCount(RegionDatabase.Paldea));
            game.Story.SetVar("VAR_PALDEA_STORY", 7);
            int won = 0;
            foreach (string badge in order)
            {
                string? battle = game.Win(badge);
                Assert.True(battle != null, $"{badge}: no leader's battle in the order {string.Join(", ", order)}");
                Assert.True(game.Story.HasBadge(RegionDatabase.Paldea, badge), $"{badge} in the order {string.Join(", ", order)}");
                Assert.Equal(++won, game.Story.BadgeCount(RegionDatabase.Paldea));
                // In the academy's order the later leaders battle with their originals' teams; out of it, the team for
                // the badges held as the battle began
                if (originals.TryGetValue(badge, out string? leader))
                {
                    string expected = order == AcademysOrder ? "paldea_" + leader : $"paldea_{leader.Trim()}_b{won - 1}";
                    Assert.Contains(expected, battle + " ");
                }
            }
            Assert.Equal(8, game.Story.BadgeCount(RegionDatabase.Paldea));
            foreach (string badge in AcademysOrder) Assert.True(game.Story.HasBadge(RegionDatabase.Paldea, badge), badge);
            // Spoken to again, a leader gives nothing more
            game.Play("Montenevera_GymTest", "Leader");
            game.Play("Alfornada_GymTest", "Leader");
            game.Play("Glaseado_GymTest", "Leader");
            Assert.Equal(8, game.Story.BadgeCount(RegionDatabase.Paldea));
        }
    }

    [Fact]
    public void TheLastThreeLeadersAreScaledToTheBadgesHeldAsKofuAndLarryAre()
    {
        var caps = Region.File!.Caps;
        foreach (var (leader, held, tera) in new[] { ("ryme", 5, "Ghost"), ("tulip", 6, "Psychic"), ("grusha", 7, "Ice") })
        {
            // The original's team is the academy's order's, its best at the cap of that many badges
            Assert.Equal(caps[held], TrainerDatabase.Get("paldea_" + leader, "paldea")!.Party.Max(p => p.Level));
            for (int n = 0; n < 8; n++)
            {
                string id = n == held ? "paldea_" + leader : $"paldea_{leader}_b{n}";
                var record = TrainerDatabase.Get(id, "paldea");
                Assert.True(record != null, id);
                Assert.Equal("Leader", record!.Class);
                Assert.True(record.TeraOrb);
                Assert.Equal(caps[n], record.Party.Max(p => p.Level));
                Assert.Equal(tera, record.Party[^1].TeraType);
            }
        }
        // The gym trainers keep fixed levels
        foreach (string trainer in new[] { "paldea_gym_trainer_tas", "paldea_gym_trainer_bruno", "paldea_gym_trainer_yuki" })
            Assert.Equal("Gym Trainer", TrainerDatabase.Get(trainer, "paldea")!.Class);
    }

    [Fact]
    public void ARhymeMissedAPoseWrongAndAGateSkippedEachSendTheChallengerBack()
    {
        var game = new Game();
        game.Story.SetVar("VAR_PALDEA_STORY", 7);
        // The rap battle: a line that doesn't rhyme ends the turn with the lines before it kept, and Ryme waits for the
        // test; spoken to again, the MC gives the line missed
        game.Play("Montenevera_GymTest", "RapBattle", 0, 2);
        Assert.Equal(1, game.Story.Var("VAR_PALDEA_MONTENEVERA_RAP"));
        Assert.False(game.Story.Has("FLAG_PALDEA_MONTENEVERA_TEST_DONE"));
        Assert.Null(Game.Battled(game.Play("Montenevera_GymTest", "Leader"), "ryme"));
        game.Play("Montenevera_GymTest", "RapBattle", 1, 1);
        Assert.True(game.Story.Has("FLAG_PALDEA_MONTENEVERA_TEST_DONE"));
        // The photographs: a pose that doesn't fit ends the turn; the cancel button picks the last pose, never the right one
        game.Play("Alfornada_GymTest", "Photos", 0, 1, 2);
        Assert.Equal(2, game.Story.Var("VAR_PALDEA_ALFORNADA_PHOTOS"));
        Assert.False(game.Story.Has("FLAG_PALDEA_ALFORNADA_TEST_DONE"));
        Assert.Null(Game.Battled(game.Play("Alfornada_GymTest", "Leader"), "tulip"));
        game.Play("Alfornada_GymTest", "Photos", 1);
        Assert.True(game.Story.Has("FLAG_PALDEA_ALFORNADA_TEST_DONE"));
        // The run: declined, nothing starts; a gate skipped, the line says so and the run must be begun again
        game.Play("glaseado_mountain", "Coach", 1);
        Assert.Equal(0, game.Story.Var("VAR_PALDEA_GLASEADO_RUN"));
        game.Play("glaseado_mountain", "Coach", 0);
        game.Play("glaseado_mountain", "GateOne");
        game.Play("glaseado_mountain", "Missed");
        Assert.Equal(0, game.Story.Var("VAR_PALDEA_GLASEADO_RUN"));
        Assert.False(game.Story.Has("FLAG_PALDEA_GLASEADO_TEST_DONE"));
        Assert.Null(Game.Battled(game.Play("Glaseado_GymTest", "Leader"), "grusha"));
        game.Run();
        Assert.NotNull(Game.Battled(game.Play("Glaseado_GymTest", "Leader"), "grusha"));
    }

    [Fact]
    public void TheRunsScenesPlayedAndSkippedLeaveTheSame()
    {
        // The scenes of an open place's person and trigger, which the walk of every script doesn't start
        // (SceneSkipTests.BeyondTheWalk): the coach's start and the line's end, each played and then hurried
        foreach (var (script, var, answers) in new[] { ("Coach", 0, new[] { 0 }), ("Finish", 4, Array.Empty<int>()) })
        {
            var ends = new List<string>();
            foreach (bool hurry in new[] { false, true })
            {
                var game = new Game();
                game.Story.SetVar("VAR_PALDEA_STORY", 7);
                game.Story.SetVar("VAR_PALDEA_GLASEADO_RUN", var);
                var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag)
                {
                    OpenMap = Open.Value,
                    OpenMapNamed = name => name == Open.Value.Name ? Open.Value : null,
                    Region = RegionDatabase.Paldea
                };
                foreach (int a in answers) host.Answers.Enqueue(a);
                var runner = new ScriptRunner(ScriptLibrary.Default, host) { Impatient = hurry };
                runner.Start(ScriptLibrary.Default.Find(script, "glaseado_mountain")!, null);
                runner.RunToEnd();
                Assert.Empty(host.Problems);
                ends.Add(System.Text.Json.JsonSerializer.Serialize(game.Story.Snapshot()) + "|" + string.Join(",", host.Asked.Select(q => q.Answer)));
            }
            Assert.True(ends[0] == ends[1], $"{script} skipped leaves otherwise than played:\n{ends[0]}\n{ends[1]}");
        }
    }
}
