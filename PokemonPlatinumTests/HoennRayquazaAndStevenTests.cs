using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The post-game of Hoenn, part c (plan 21 · H12): Sky Pillar cracked all over once Rayquaza has calmed Sootopolis, its
/// floors given way under a walker and ridden over on the Bicycle, Rayquaza back on the top to be battled at level 70,
/// and Steven's cave behind Meteor Falls, whose mouth opens once Hoenn's Hall of Fame is entered (docs/mechanics/rulings.md,
/// "Rayquaza on Sky Pillar and Steven at Meteor Falls"). Each scene starts as the game starts it, on maps of this class's own.
/// </summary>
public class HoennRayquazaAndStevenTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Hoenn = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Hoenn));
    private const string Cracked = "FLAG_HOENN_SKY_PILLAR_CRACKED";
    private static readonly string[] Floors = { "SkyPillar_1F", "SkyPillar_2F", "SkyPillar_3F", "SkyPillar_4F", "SkyPillar_5F", "SkyPillar_Top" };

    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public HeadlessScriptHost Last { get; private set; } = null!;

        public Game()
        {
            foreach (var m in Hoenn.Value.BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            var pokemon = new Pokemon(PokemonDatabase.Get("Swampert")!, 70, new Random(1));
            Party.Add(pokemon);
            Story.SetBadges("Hoenn", 0xFF);
        }

        public Map MapOf(string name) => maps[name];

        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Settle(Map);
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        private void Settle(Map map)
        {
            map.ApplySwappedPlaces(Story.Var, Story.Has);
            map.ApplyPresence(Story.Has);
            map.ApplyDoors(Story.Has);
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
        }

        public HeadlessScriptHost Play(Script script, NPC? subject = null)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => Fight,
                NeedsPokemon = true
            };
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct()) Settle(map);
            Tile = host.PlayerTile;
            return Last = host;
        }
    }

    private static int Count(Map map, TileBehavior behaviour)
    {
        int n = 0;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map.BehaviourAt(x, y) == behaviour) n++;
        return n;
    }

    [Fact]
    public void SkyPillarIsLaidCleanUntilItsFlagAndCrackedAllOverAfterIt()
    {
        var game = new Game();
        var two = game.MapOf("SkyPillar_2F");
        foreach (string floor in Floors)
            Assert.Contains(game.MapOf(floor).SwappedPlaces, p => p.Flag == Cracked);
        int clean = Count(two, TileBehavior.CrackedFloor);
        Assert.Empty(two.ApplySwappedPlaces(_ => 0, _ => false));
        Assert.Single(two.ApplySwappedPlaces(_ => 0, f => f == Cracked));
        Assert.True(Count(two, TileBehavior.CrackedFloor) > clean + 20, $"the second floor cracked has {Count(two, TileBehavior.CrackedFloor)} cracked tiles");
        // The top is broken through in places
        var top = game.MapOf("SkyPillar_Top");
        Assert.Equal(0, Count(top, TileBehavior.Hole));
        top.ApplySwappedPlaces(_ => 0, f => f == Cracked);
        Assert.True(Count(top, TileBehavior.Hole) > 0);
    }

    [Fact]
    public void ACrackedFloorDropsAWalkerToTheSameTileBelowAndAHoleDropsARiderToo()
    {
        var two = new Game().MapOf("SkyPillar_2F");
        two.ApplySwappedPlaces(_ => 0, f => f == Cracked);
        var (x, y) = Enumerable.Range(0, two.Height).SelectMany(yy => Enumerable.Range(0, two.Width).Select(xx => (xx, yy)))
            .First(t => two.BehaviourAt(t.xx, t.yy) == TileBehavior.CrackedFloor);
        var below = two.HoleTargetAt(x, y)!;
        Assert.Equal(("SkyPillar_1F", x, y), (below.Map, below.X, below.Y));
        // A cracked tile still bears a rider; a hole doesn't (none is broken through on the second floor)
        Assert.False(two.IsOpenHole(x, y));
        // The top's holes lead nowhere: Emerald gives the top no floor to fall to
        var top = new Game().MapOf("SkyPillar_Top");
        top.ApplySwappedPlaces(_ => 0, f => f == Cracked);
        var hole = Enumerable.Range(0, top.Height).SelectMany(yy => Enumerable.Range(0, top.Width).Select(xx => (xx, yy)))
            .First(t => top.BehaviourAt(t.xx, t.yy) == TileBehavior.Hole);
        Assert.Null(top.HoleTargetAt(hole.xx, hole.yy));
    }

    [Fact]
    public void RayquazaCalmingSootopolisCracksThePillarAndAnOlderSaveIsCrackedOnTheWayUp()
    {
        var game = new Game();
        game.Story.SetVar("VAR_SOOTOPOLIS_CITY_STATE", 5);
        game.Story.SetVar("VAR_SKY_PILLAR_STATE", 1);
        game.Arrive("SootopolisCity", 31, 50);
        Assert.Equal(2, game.Story.Var("VAR_SKY_PILLAR_STATE"));
        Assert.True(game.Story.Has(Cracked));

        // A save that saw the calm before this part was written is cracked as it comes to the pillar's island
        var older = new Game();
        older.Story.SetVar("VAR_SKY_PILLAR_STATE", 2);
        older.Arrive("SkyPillar_Outside", 17, 14);
        Assert.True(older.Story.Has(Cracked));
        Assert.True(older.MapOf("SkyPillar_2F").SwappedPlaces.Single().Swapped);
        // ... and one before the calm is not
        var before = new Game();
        before.Story.SetVar("VAR_SKY_PILLAR_STATE", 1);
        before.Arrive("SkyPillar_Outside", 17, 14);
        Assert.False(before.Story.Has(Cracked));
    }

    [Fact]
    public void RayquazaWaitsOnTheTopAfterTheCalmAndIsBattledAtSeventyUntilBeatenOrCaught()
    {
        var game = new Game();
        // Before the calm it is not there
        game.Story.SetVar("VAR_SKY_PILLAR_STATE", 1);
        game.Story.SetVar("VAR_SKY_PILLAR_RAYQUAZA_CRY_DONE", 1);
        game.Arrive("SkyPillar_Top", 16, 14);
        Assert.Null(game.Present("rayquaza_still"));

        game.Story.SetVar("VAR_SKY_PILLAR_STATE", 2);
        game.Arrive("SkyPillar_Top", 16, 14);
        Assert.NotNull(game.Present("rayquaza_still"));
        // Run from, it flies off and is back the next time
        game.Fight = BattleOutcome.Fled;
        game.Talk("rayquaza_still");
        Assert.Contains("wildbattle Rayquaza 70 Fled", game.Last.Log);
        Assert.False(game.Story.Has("FLAG_DEFEATED_RAYQUAZA"));
        Assert.Null(game.Present("rayquaza_still"));
        game.Arrive("SkyPillar_Top", 16, 14);
        Assert.NotNull(game.Present("rayquaza_still"));
        // Caught, it is gone for good
        game.Fight = BattleOutcome.Caught;
        game.Talk("rayquaza_still");
        Assert.True(game.Story.Has("FLAG_DEFEATED_RAYQUAZA"));
        game.Arrive("SkyPillar_Top", 16, 14);
        Assert.Null(game.Present("rayquaza_still"));
    }

    [Fact]
    public void BeatenRayquazaIsGoneTooAndTheSleeperNeverComesBack()
    {
        var game = new Game();
        game.Story.SetVar("VAR_SKY_PILLAR_STATE", 2);
        game.Story.SetVar("VAR_SKY_PILLAR_RAYQUAZA_CRY_DONE", 1);
        game.Story.Set("FLAG_HIDE_SKY_PILLAR_TOP_RAYQUAZA");
        game.Arrive("SkyPillar_Top", 16, 14);
        game.Talk("rayquaza_still");
        Assert.Contains("wildbattle Rayquaza 70 Won", game.Last.Log);
        Assert.True(game.Story.Has("FLAG_DEFEATED_RAYQUAZA"));
        game.Arrive("SkyPillar_Top", 16, 14);
        Assert.Null(game.Present("rayquaza_still"));
        Assert.Null(game.Present("rayquaza_sleeping"));
    }

    [Fact]
    public void StevensCaveOpensInMeteorFallsOnlyAfterTheHallOfFame()
    {
        Assert.Contains("MeteorFalls_StevensCave", RegionDatabase.Get(RegionDatabase.Hoenn)!.Maps);
        Assert.Contains("meteor_falls_stevens_cave", Hoenn.Value.Index.Areas);
        var game = new Game();
        var falls = game.MapOf("MeteorFalls_1F_1R");
        Assert.Equal("MeteorFalls_StevensCave", falls.GetWarpAt(4, 2)!.TargetMap);
        // Shut before, a mouth to walk into after
        game.Arrive("MeteorFalls_1F_1R", 6, 39);
        Assert.True(falls.IsSolid(4, 2));
        game.Story.Set("HoennHallOfFame");
        game.Arrive("MeteorFalls_1F_1R", 6, 39);
        Assert.False(falls.IsSolid(4, 2));
        Assert.True(falls.OpenOnceSwapped(4, 2));
        // and the cave's way back lands on it
        var back = game.MapOf("MeteorFalls_StevensCave").Warps.Single();
        Assert.Equal(("MeteorFalls_1F_1R", 4, 2), (back.TargetMap, back.TargetX, back.TargetY));
    }

    [Fact]
    public void StevenBattlesOnceWithHisOwnTeamAndRemembersIt()
    {
        var game = new Game();
        game.Story.Set("HoennHallOfFame");
        game.Arrive("MeteorFalls_StevensCave", 10, 28);
        Assert.NotNull(game.Present("meteor_falls_steven"));
        var first = game.Talk("meteor_falls_steven");
        Assert.Contains(first.Log, l => l.StartsWith("battle ") && l.Contains("hoenn_steven"));
        Assert.True(game.Story.Has("FLAG_DEFEATED_METEOR_FALLS_STEVEN"));
        var again = game.Talk("meteor_falls_steven");
        Assert.DoesNotContain(again.Log, l => l.StartsWith("battle "));
        Assert.Contains(again.Transcript, l => l.Text.Contains("Granite Cave"));
    }
}
