using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Paldea's Path of Legends (plan 34 · PD4): Arven at Poco Path's lighthouse, the first Titan, Klawf, beaten in the
/// Stony Cliff's gully, chased to its den and beaten again there, its Herba Mystica shared with the mount, which then
/// dashes; the Titan's battle a totem's aura fought alone; and on Paldea's own bake the sprint the grant gives, and the
/// gully's crag, whose painted face the mount climbs only with the climb (the fifth Titan's, PD4's second session).
/// </summary>
public class PaldeaLegendsTests
{
    const int T = OpenClock.TicksPerSecond;

    private static Region Region => RegionDatabase.Get(RegionDatabase.Paldea)!;
    private static readonly Lazy<OpenMap> Open = new(() => OpenRegions.Load(Region).Single());

    // A game past the academy's welcome, with Koraidon and a team, its scripts played headless on the open map
    private sealed class Game
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();

        public Game()
        {
            StoryMigration.BeginNewGame(Story, ScriptLibrary.Default);
            Story.SetVar("VAR_PALDEA_STORY", 7);
            Story.Unset("FLAG_PALDEA_HIDE_ARVEN_LIGHTHOUSE");
            Story.SetVar(MountRules.MountVariable, MountRules.Koraidon);
            Party.Add(new Pokemon(PokemonDatabase.Get("Fuecoco")!, 30, new Random(0)));
        }

        public HeadlessScriptHost Play(string file, string script, bool hurry = false)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                OpenMap = Open.Value,
                OpenMapNamed = name => name == Open.Value.Name ? Open.Value : null,
                Region = RegionDatabase.Paldea
            };
            var runner = new ScriptRunner(ScriptLibrary.Default, host) { Impatient = hurry };
            runner.Start(ScriptLibrary.Default.Find(script, file)!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
    }

    [Fact]
    public void TheStonyCliffIsAnAreaOfPaldeaWithItsTitanItsDenAndArven()
    {
        var map = Open.Value;
        Assert.Contains(map.Areas, a => a.Key == "stony_cliff");
        Assert.Equal("Paldea", OpenRegions.MapOfArea("stony_cliff"));
        var titan = map.People.Single(p => p.Key == "titan_klawf");
        Assert.Equal("Klawf", titan.Species);
        Assert.True(titan.Size > 1);
        Assert.Equal("FLAG_PALDEA_LEGENDS_BEGUN", titan.ShownBy);
        var den = map.People.Single(p => p.Key == "titan_klawf_den");
        Assert.Equal("Klawf", den.Species);
        Assert.Equal("FLAG_PALDEA_KLAWF_FLED", den.ShownBy);
        // The den is up on the ledge, six metres over the gully's floor where the Titan first waits
        Assert.True(den.Home.MetresY - titan.Home.MetresY > 5, $"{den.Home} over {titan.Home}");
        Assert.Contains(map.People, p => p.Key == "arven_lighthouse" && p.Look == "arven");
        Assert.Contains(map.Triggers, t => t.Name == "klawf_den");
        // The first battle is met as the gully is walked up, not only by speaking to the giant
        Assert.Contains(map.Triggers, t => t.Name == "klawf_gully");
        // A person of the place is no Pokémon unless the place says so
        Assert.All(map.People.Where(p => p.Species == null), p => Assert.Equal(1, p.Size));
    }

    [Fact]
    public void TheFirstTitanIsBeatenTwiceAndItsHerbsLetTheMountDash()
    {
        var game = new Game();
        var story = game.Story;
        Assert.Equal(MovementGrants.None, MountRules.Grants(story));
        // Before Arven has asked, the Titan isn't there and its script does nothing
        game.Play("stony_cliff", "Klawf");
        Assert.Equal(0, story.Var("VAR_PALDEA_LEGENDS"));
        Assert.False(story.Has("FLAG_PALDEA_KLAWF_FLED"));

        // Arven at the lighthouse: the path begins
        game.Play("poco_path", "ArvenLighthouse");
        Assert.Equal(1, story.Var("VAR_PALDEA_LEGENDS"));
        Assert.True(story.Has("FLAG_PALDEA_LEGENDS_BEGUN"));
        // Spoken to again, he only tells the way
        game.Play("poco_path", "ArvenLighthouse");
        Assert.Equal(1, story.Var("VAR_PALDEA_LEGENDS"));

        // The gully's mouth: a Titan's battle, fought alone under its aura, and its flight to the den
        var first = game.Play("stony_cliff", "Klawf");
        Assert.Contains(first.Log, l => l.StartsWith("titanbattle Klawf 16 Defense+1 Won"));
        Assert.DoesNotContain(first.Log, l => l.StartsWith("totembattle"));
        Assert.True(story.Has("FLAG_PALDEA_KLAWF_FLED"));
        Assert.Equal(2, story.Var("VAR_PALDEA_LEGENDS"));
        Assert.Equal(MovementGrants.None, MountRules.Grants(story));

        // The den: the second battle, bigger, then the herbs shared with the mount
        var den = game.Play("stony_cliff", "KlawfDen");
        Assert.Contains(den.Log, l => l.StartsWith("titanbattle Klawf 16 Attack,Defense,SpAttack,SpDefense,Speed+1 Won"));
        Assert.True(story.Has("FLAG_PALDEA_KLAWF_BEATEN"));
        Assert.True(story.Has("FLAG_PALDEA_MOUNT_DASH"));
        Assert.Equal(3, story.Var("VAR_PALDEA_LEGENDS"));
        // The first power is the dash, and only the dash (plan 30 · decision 16's order)
        Assert.Equal(MovementGrants.Dash, MountRules.Grants(story));
        // The den's script and the gully's do nothing more
        game.Play("stony_cliff", "KlawfDen");
        game.Play("stony_cliff", "Klawf");
        Assert.Equal(3, story.Var("VAR_PALDEA_LEGENDS"));
        // No Pokémon was caught or given
        Assert.Single(game.Party.Members);
    }

    [Fact]
    public void ThePathsScenesPlayedAndSkippedLeaveTheSame()
    {
        var scenes = new List<(string, string, int)> { ("poco_path", "ArvenLighthouse", 0) };
        for (int i = 0; i < Titans.Length; i++)
        {
            scenes.Add((Titans[i].File, Titans[i].First, 1 + 2 * i));
            scenes.Add((Titans[i].File, Titans[i].Den, 2 + 2 * i));
        }
        scenes.Add(("poco_path", "ArvenLighthouse", 11));
        foreach (var (file, script, legends) in scenes)
        {
            var ends = new List<string>();
            foreach (bool hurry in new[] { false, true })
            {
                var game = new Game();
                // Every scene before this one played, so whoever it walks about stands where the story has them
                game.Play("poco_path", "ArvenLighthouse");
                foreach (var (f, first, den, _) in Titans)
                {
                    if (game.Story.Var("VAR_PALDEA_LEGENDS") >= legends) break;
                    game.Play(f, first);
                    if (game.Story.Var("VAR_PALDEA_LEGENDS") >= legends) break;
                    game.Play(f, den);
                }
                if (legends == 0) game = new Game();
                Assert.Equal(legends, game.Story.Var("VAR_PALDEA_LEGENDS"));
                var host = game.Play(file, script, hurry);
                Assert.True(game.Story.Var("VAR_PALDEA_LEGENDS") > legends, $"{file}.{script} left the path where it was");
                ends.Add(System.Text.Json.JsonSerializer.Serialize(game.Story.Snapshot()) + "|" + string.Join(",", game.Party.Members.Select(p => p.Species.Name))
                    + "|" + string.Join(",", host.OnMarker.OrderBy(m => m.Key).Select(m => m.Key + "=" + m.Value)));
            }
            Assert.True(ends[0] == ends[1], $"{file}.{script} skipped leaves otherwise than played:\n{ends[0]}\n{ends[1]}");
        }
    }

    [Fact]
    public void AfterTheFirstTitanTheMountSprintsUpTheGullyWhereBeforeItOnlyTrotted()
    {
        var map = Open.Value;
        var game = new Game();
        var before = MountRules.Grants(game.Story);
        game.Play("poco_path", "ArvenLighthouse");
        game.Play("stony_cliff", "Klawf");
        game.Play("stony_cliff", "KlawfDen");
        var after = MountRules.Grants(game.Story);

        double Ridden(MovementGrants grants)
        {
            lock (OpenRegions.Reading)
            {
                var body = new OpenBody(On(map, 226, -364));
                body.Mount();
                for (int i = 0; i < 2; i++) body.Step(map.Ground, OpenInput.None, grants);
                double z0 = body.Pos.MetresZ;
                for (int i = 0; i < T; i++) body.Step(map.Ground, new OpenInput(0, -1, Run: true), grants);
                return z0 - body.Pos.MetresZ;
            }
        }
        Assert.InRange(Ridden(before), 5.4, 6.05);
        Assert.InRange(Ridden(after), 10.0, 11.05);
    }

    [Fact]
    public void TheGullysCragIsClimbedOnlyWithTheClimbNeverWithTheDashAlone()
    {
        // The crag's painted south face (terrain/titans.json, carve/titans.json): its top is the mount's only once it
        // climbs, which is the fifth Titan's power (PD4's second session)
        var map = Open.Value;
        var mesh = map.Land!.Nav!;
        OpenPos foot, top;
        lock (OpenRegions.Reading)
        {
            foot = On(map, 243, -394);
            top = On(map, 243, -402);
        }
        Assert.True(top.MetresY - foot.MetresY > OpenBody.HighJumpHeight + 1, $"{top} over {foot}");
        Assert.Contains(mesh.Links, l => (l.Flags & OpenNavFlags.Climb) != 0 && l.A.MetresX is > 236 and < 250 && l.A.MetresZ is > -400 and < -390);
        const MovementGrants AllButClimb = MovementGrants.Dash | MovementGrants.Swim | MovementGrants.HighJump | MovementGrants.Glide;
        var climbed = NavPath.Find(mesh, foot, top, OpenNavMesh.Allowed(MovementGrants.Climb, mounted: true));
        Assert.NotNull(climbed);
        Assert.True(climbed!.Uses.HasFlag(OpenNavFlags.Climb), climbed.Uses.ToString());
        Assert.Null(NavPath.Find(mesh, foot, top, OpenNavMesh.Allowed(AllButClimb, mounted: true)));
        Assert.Null(NavPath.Find(mesh, foot, top, OpenNavMesh.Allowed(AllButClimb | MovementGrants.Climb)));

        // The capsule itself: mounted with the climb it clings to the face and stands on the top; with the dash alone it
        // stays at the foot
        OpenBody Up(MovementGrants grants)
        {
            lock (OpenRegions.Reading)
            {
                var body = new OpenBody(foot);
                body.Mount();
                bool clung = false;
                for (int i = 0; i < 12 * T; i++)
                {
                    body.Step(map.Ground, new OpenInput(0, -1), grants);
                    clung |= body.Stance == OpenStance.Climbing;
                    if (clung && body.Stance == OpenStance.Standing) break;
                }
                return body;
            }
        }
        var climber = Up(MovementGrants.Dash | MovementGrants.Climb);
        Assert.Equal(OpenStance.Standing, climber.Stance);
        Assert.True(climber.Pos.MetresY > top.MetresY - 0.2, $"{climber.Pos}");
        var stuck = Up(MovementGrants.Dash);
        Assert.True(stuck.Pos.MetresY < foot.MetresY + 0.5, $"{stuck.Pos}");
    }

    // The five Titans in their order, each a place's two scripts and the power its herbs give (PD4's second session)
    private static readonly (string File, string First, string Den, string Power)[] Titans =
    {
        ("stony_cliff", "Klawf", "KlawfDen", "FLAG_PALDEA_MOUNT_DASH"),
        ("open_sky", "Bombirdier", "BombirdierDen", "FLAG_PALDEA_MOUNT_SWIM"),
        ("lurking_steel", "Orthworm", "OrthwormDen", "FLAG_PALDEA_MOUNT_HIGH_JUMP"),
        ("quaking_earth", "Quaking", "QuakingDen", "FLAG_PALDEA_MOUNT_GLIDE"),
        ("false_dragon", "Tatsugiri", "Dondozo", "FLAG_PALDEA_MOUNT_CLIMB"),
    };

    [Fact]
    public void TheFourOtherTitansHaveTheirPlacesBesideTheLandThatIsBuilt()
    {
        var map = Open.Value;
        foreach (string area in new[] { "open_sky", "lurking_steel", "quaking_earth", "false_dragon" })
        {
            Assert.Contains(map.Areas, a => a.Key == area);
            Assert.Equal("Paldea", OpenRegions.MapOfArea(area));
        }
        foreach (var (titan, species) in new[] { ("titan_bombirdier", "Bombirdier"), ("titan_orthworm", "Orthworm"), ("titan_great_tusk", "Great Tusk"),
            ("titan_iron_treads", "Iron Treads"), ("titan_tatsugiri", "Tatsugiri"), ("titan_dondozo", "Dondozo") })
            Assert.Equal(species, map.People.Single(p => p.Key == titan).Species);
        // Each place's two triggers, the second a den up a ramp or across the lake
        foreach (string trigger in new[] { "bombirdier_gully", "bombirdier_den", "orthworm_cutting", "orthworm_den", "quaking_basin", "quaking_den", "tatsugiri_shore", "dondozo_shore" })
            Assert.Contains(map.Triggers, t => t.Name == trigger);
        // Every Titan and den stands on ground walked to on foot (the bot rides no link yet): its foot is walkable land
        lock (OpenRegions.Reading)
            foreach (var p in map.People.Where(p => p.Key.StartsWith("titan_")))
                Assert.True(map.Ground.Footing(p.Home.MetresX, p.Home.MetresZ, p.Home.MetresY + 2, p.Home.MetresY - 3, walkableOnly: true) != null, p.Key);
    }

    [Fact]
    public void TheFiveTitansAreBeatenInOrderAndEachGrantsTheNextPower()
    {
        foreach (int mount in new[] { MountRules.Koraidon, MountRules.Miraidon })
        {
            var game = new Game();
            var story = game.Story;
            story.SetVar(MountRules.MountVariable, mount);
            game.Play("poco_path", "ArvenLighthouse");
            var powers = new List<string>();
            foreach (var (file, first, den, power) in Titans)
            {
                // A later Titan does nothing before the one before it is beaten
                int at = story.Var("VAR_PALDEA_LEGENDS");
                game.Play(file, den);
                Assert.Equal(at, story.Var("VAR_PALDEA_LEGENDS"));
                var a = game.Play(file, first);
                Assert.Contains(a.Log, l => l.StartsWith("titanbattle") && l.EndsWith("Won"));
                Assert.Equal(at + 1, story.Var("VAR_PALDEA_LEGENDS"));
                Assert.False(story.Has(power), $"{power} before {file}'s den");
                var b = game.Play(file, den);
                Assert.Contains(b.Log, l => l.StartsWith("titanbattle") && l.Contains("Attack,Defense,SpAttack,SpDefense,Speed") && l.EndsWith("Won"));
                Assert.Equal(at + 2, story.Var("VAR_PALDEA_LEGENDS"));
                Assert.True(story.Has(power), power);
                powers.Add(power);
            }
            // The version's own fourth Titan
            Assert.Equal(mount == MountRules.Miraidon, story.Has("FLAG_PALDEA_IRON_TREADS_FLED"));
            Assert.Equal(mount == MountRules.Koraidon, story.Has("FLAG_PALDEA_GREAT_TUSK_FLED"));
            Assert.Equal(MovementGrants.Dash | MovementGrants.Swim | MovementGrants.HighJump | MovementGrants.Glide | MovementGrants.Climb, MountRules.Grants(story));
            Assert.Equal(11, story.Var("VAR_PALDEA_LEGENDS"));
            // Arven at the lighthouse: his Mabosstiff well again, and his battle
            var arven = game.Play("poco_path", "ArvenLighthouse");
            Assert.Contains(arven.Log, l => l.Contains("paldea_arven"));
            Assert.Equal(12, story.Var("VAR_PALDEA_LEGENDS"));
            Assert.True(story.Has("FLAG_PALDEA_LEGENDS_DONE"));
            Assert.Single(game.Party.Members);
        }
    }

    [Fact]
    public void TheLastTitansHerbsLetTheMountClimbTheStonyCliffsCragItCouldntBefore()
    {
        // Plan 34 · PD4's done-when: the grants as the story has them just before the False Dragon's herbs and just after
        var map = Open.Value;
        var game = new Game();
        game.Play("poco_path", "ArvenLighthouse");
        foreach (var (file, first, den, _) in Titans.Take(4)) { game.Play(file, first); game.Play(file, den); }
        game.Play("false_dragon", "Tatsugiri");
        var before = MountRules.Grants(game.Story);
        game.Play("false_dragon", "Dondozo");
        var after = MountRules.Grants(game.Story);
        Assert.Equal(MovementGrants.Climb, after & ~before);

        var mesh = map.Land!.Nav!;
        OpenPos foot, top;
        lock (OpenRegions.Reading)
        {
            foot = On(map, 243, -394);
            top = On(map, 243, -402);
        }
        Assert.Null(NavPath.Find(mesh, foot, top, OpenNavMesh.Allowed(before, mounted: true)));
        Assert.NotNull(NavPath.Find(mesh, foot, top, OpenNavMesh.Allowed(after, mounted: true)));
        OpenBody Up(MovementGrants grants)
        {
            lock (OpenRegions.Reading)
            {
                var body = new OpenBody(foot);
                body.Mount();
                bool clung = false;
                for (int i = 0; i < 12 * T; i++)
                {
                    body.Step(map.Ground, new OpenInput(0, -1), grants);
                    clung |= body.Stance == OpenStance.Climbing;
                    if (clung && body.Stance == OpenStance.Standing) break;
                }
                return body;
            }
        }
        Assert.True(Up(before).Pos.MetresY < foot.MetresY + 0.5);
        Assert.True(Up(after).Pos.MetresY > top.MetresY - 0.2);
    }

    private static OpenPos On(OpenMap map, double x, double z) =>
        OpenPos.FromMetres(x, map.Ground.Footing(x, z, 60, -10, walkableOnly: true)!.Value.Y, z);
}
