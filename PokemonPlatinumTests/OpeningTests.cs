using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The first chapter (plan 02 · S4, "Lake Verity and a Pokédex") played through with no screen, in the order a
/// player meets it: the television in the bedroom, the rival bursting in, Mom, the guitarist, the rival's room, the
/// professor on Route 201 and his briefcase, the first battle, the Running Shoes, the lake and the man in grey, the
/// assistant and the Pokédex, the professor's TM, the parcel and the catching lesson. Each scene is started as the
/// game starts it (a step onto its trigger, arriving somewhere, talking to someone) on maps of its own, so the
/// triggers' variables are tested along with the scripts.
/// </summary>
public class OpeningTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static readonly Lazy<World> Sinnoh = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Sinnoh));

    /// <summary>
    /// A game of its own: maps no other test touches (scripts walk people about and take them off the map), the
    /// story, the team and the bag, and the tile the player stands on. Everything is played as the engine plays it,
    /// and a script's warp brings on the script of the place it leads to.
    /// </summary>
    internal sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);

        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }

        /// <summary>Every script played, in order, each with the host it ran in.</summary>
        public List<(string Script, HeadlessScriptHost Host)> Played { get; } = new();

        public int Starter { get; init; }
        public BattleOutcome Fight { get; init; } = BattleOutcome.Won;
        public const string Rival = "Kit";

        public Game(int starter = 0, BattleOutcome fight = BattleOutcome.Won)
        {
            Starter = starter;
            Fight = fight;
            foreach (string path in Directory.GetFiles(GameDataFiles.PathOf(MapDatabase.Folder), "*.json"))
            {
                var map = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, Path.GetFileName(path))).ToMap();
                maps[map.Name] = map;
            }
            foreach (var map in Sinnoh.Value.BuildMaps()) maps[map.Name] = map;

            StoryMigration.BeginNewGame(Story, Scripts);
            var start = RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!;
            Arrive(start.Map, start.X, start.Y);
        }

        /// <summary>What the last script played said, line by line.</summary>
        public List<(string? Speaker, string Text)> Said => Played[^1].Host.Transcript;

        public HeadlessScriptHost Last => Played[^1].Host;

        /// <summary>Comes to a map as a warp brings the player: who is there is what the story says, and the place's own script runs.</summary>
        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        /// <summary>Through the door (or the stairs) of this map that leads to another.</summary>
        public void Through(string target)
        {
            var warp = Map.Warps.First(w => w.TargetMap.Equals(target, StringComparison.OrdinalIgnoreCase));
            Arrive(warp.TargetMap, warp.TargetX, warp.TargetY);
        }

        private StepTrigger TriggerOf(string script) =>
            Map.Triggers.First(t => t.Script == script);

        /// <summary>A step onto the trigger that starts a script, which must start it now.</summary>
        public HeadlessScriptHost Step(string script)
        {
            var trigger = TriggerOf(script);
            Tile = (trigger.X, trigger.Y);
            Assert.Same(trigger, FieldScripts.TriggerAt(Map, trigger.X, trigger.Y, Story));
            return Play(Scripts.Find(trigger.Script, trigger.ScriptFile ?? Map.Name)!);
        }

        /// <summary>Whether a step onto a script's trigger would start it now.</summary>
        public bool Fires(string script)
        {
            var trigger = TriggerOf(script);
            return FieldScripts.TriggerAt(Map, trigger.X, trigger.Y, Story) == trigger;
        }

        /// <summary>Someone on the map now, of a place (the player's own, left out): the map of Sinnoh has a rival in every town of the chapter.</summary>
        public NPC? Present(string key, string? place = null)
        {
            string where = place ?? Map.ScriptFileAt(Tile.X, Tile.Y);
            return Map.NPCs.FirstOrDefault(n => n.Key == key && Place(n) == where);
        }

        private string Place(NPC npc) => npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY);

        /// <summary>Talks to someone of a place, standing below them.</summary>
        public HeadlessScriptHost Talk(string key, string? place = null)
        {
            var npc = Map.NPCs.First(n => n.Key == key && (place == null || Map.ScriptFileAt(n.GridX, n.GridY) == place));
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, Place(npc))!, npc);
        }

        public HeadlessScriptHost Play(Script script, NPC? subject = null)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                StarterChoice = Starter,
                RivalName = Rival,
                Fight = _ => Fight
            };
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Played.Add((script.FullName, host));
            Assert.Empty(host.Problems);
            // As the engine does whenever the story has moved: whoever a flag brings on or takes off comes or goes
            foreach (var map in maps.Values.Distinct()) map.ApplyPresence(Story.Has);

            Tile = host.PlayerTile;
            if (host.Log.LastOrDefault(l => l.StartsWith("warp ")) != null && host.Map is { } there)
            {
                Map = there;
                if (Scripts.In(Map.ScriptFileAt(Tile.X, Tile.Y), ScriptLibrary.OnEnter) is { } onEnter) Play(onEnter);
            }
            return host;
        }
    }

    [Theory]
    [InlineData(0, BattleOutcome.Won)]
    [InlineData(1, BattleOutcome.Lost)]
    [InlineData(2, BattleOutcome.Won)]
    public void TheFirstChapterPlaysThroughFromTheBedroomToTheCatchingLesson(int starter, BattleOutcome fight)
    {
        var game = new Game(starter, fight);
        string mine = StoryState.Starters[starter];

        // The bedroom, with nothing yet: the television's special plays as the game begins, and only then
        Assert.Equal("PlayerHouse2F", game.Map.Name);
        Assert.Contains(game.Said, l => l.Speaker == "TV");
        Assert.Empty(game.Party.Members);
        Assert.False(game.Story.Has(StoryState.PokedexFlag));
        Assert.False(game.Story.Has(StoryState.RunningShoesFlag));
        Assert.Null(game.Present("rival"));

        // The first step: the rival bursts in from the stairs, says his piece by the name he was given, and goes
        game.Step("RivalRushesIn");
        Assert.Contains(game.Said, l => l.Speaker == Game.Rival);
        Assert.Null(game.Present("rival"));
        Assert.False(game.Fires("RivalRushesIn"));

        // Downstairs, Mom says where he went, and warns of the grass at the door
        game.Through("PlayerHouse");
        Assert.Contains(game.Said, l => l.Speaker == "Mom" && l.Text.Contains(Game.Rival));
        game.Step("DoorWarning");
        Assert.False(game.Fires("DoorWarning"));

        // The guitarist keeps the player in town; the rival runs into them outside his door, and back in
        game.Through("Sinnoh");
        var stopped = game.Step("GuitaristStops");
        Assert.Equal(stopped.PlayerTile.Y, game.Map.Triggers.First(t => t.Script == "GuitaristStops").Y + 1);
        game.Step("RivalRunsOut");
        Assert.Null(game.Present("rival"));
        Assert.False(game.Fires("RivalRunsOut"));
        Assert.True(game.Fires("GuitaristStops"));

        // His mother sends the player up; he is packing, and dashes off to the road north. The guitarist lets them by
        game.Through("RivalHouse");
        Assert.Contains("up in his room", game.Talk("rival_mom").Transcript[^1].Text);
        game.Through("RivalHouse2F");
        Assert.True(game.Story.Has("FLAG_RIVAL_LEFT_HOME"));
        Assert.Null(game.Present("rival"));
        game.Through("RivalHouse");
        game.Through("Sinnoh");
        Assert.False(game.Fires("GuitaristStops"));

        // Route 201: the professor stops them at the grass, asks until he hears that they love Pokémon, and leaves
        // the briefcase; walking off before choosing is turned back
        var rowan = game.Step("RowanAppears");
        Assert.Contains(rowan.Asked, a => a.Question.Contains("love Pokémon") && a.Answer == ScriptRunner.YesNo[0]);
        Assert.NotNull(game.Present("briefcase"));
        Assert.NotNull(game.Present("prof_rowan"));
        Assert.True(game.Fires("ChooseFirst"));
        game.Step("ChooseFirst");

        // The briefcase: the player's choice joins the team, the rival takes the one strong against it and battles
        // at once with Platinum's team for it, and win or lose the player wakes at home to the Running Shoes
        var briefcase = game.Talk("briefcase");
        Assert.Equal(mine, Assert.Single(game.Party.Members).Species.Name);
        string rivals = StoryState.RivalStarterFor(mine);
        Assert.Equal(rivals, game.Story.RivalStarter);
        // Platinum names the rival's team after the starter the player took
        string team = $"rival_route_201_{mine.ToLowerInvariant()}";
        Assert.Contains($"battle {team} first {fight}", briefcase.Log);
        Assert.Equal(rivals, Assert.Single(TrainerDatabase.Get(team)!.Party).Species);
        Assert.Contains(briefcase.Transcript, l => l.Speaker == Game.Rival && l.Text.Contains(fight == BattleOutcome.Won ? "wore me out" : "I won"));
        Assert.All(game.Party.Members, p => Assert.Equal(p.MaxHP, p.CurrentHP));
        Assert.Equal("PlayerHouse", game.Map.Name);
        Assert.True(game.Story.Has(StoryState.RunningShoesFlag));
        Assert.Contains(game.Said, l => l.Text.Contains("Running Shoes"));
        Assert.Null(game.Present("briefcase"));

        // Back on the road the rival is off to the lake, and the roads home and to Sandgem turn the player back
        game.Through("Sinnoh");
        game.Step("OffToTheLake");
        Assert.Null(game.Present("rival"));
        var turned = game.Step("NotHome");
        Assert.Equal(game.Map.Triggers.First(t => t.Script == "NotHome").Y - 1, turned.PlayerTile.Y);
        game.Step("NotSandgem");

        // He waits at the lakefront, and in they go: the man in grey speaks to the lake, names himself and leaves,
        // something cries out, and the rival races off to the lab
        Assert.NotNull(game.Present("rival", "verity_lakefront"));
        game.Step("IntoTheLake");
        Assert.Equal("LakeVerity", game.Map.Name);
        var lake = game.Last;
        Assert.Contains(lake.Transcript, l => l.Text.Contains("Cyrus"));
        Assert.Contains("cry Mesprit", lake.Log);
        Assert.Contains("camera Pan 48 44", lake.Log);
        Assert.Null(game.Present("cyrus"));
        Assert.Null(game.Present("rival"));
        Assert.Equal(4, game.Story.Var("VAR_FOLLOWER_RIVAL_STATE"));

        // Sandgem Town: the assistant takes the player to the lab, where the professor gives them the Pokédex and
        // the assistant names their own first Pokémon, the one neither child took
        game.Through("Sinnoh");
        game.Step("MeetAssistant");
        Assert.Equal("RowanLab", game.Map.Name);
        Assert.True(game.Story.Has(StoryState.PokedexFlag));
        string assistants = game.Story.AssistantStarter!;
        Assert.DoesNotContain(assistants, new[] { mine, rivals });
        Assert.Contains(game.Said, l => l.Text.Contains($"My first Pokémon is {assistants}!"));
        Assert.Null(game.Present("counterpart"));

        // Out of the lab, the professor's TM and the assistant's tour
        game.Through("Sinnoh");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM27")!));
        Assert.Contains(game.Last.Log, l => l.StartsWith("camera Pan"));
        Assert.Equal(2, game.Story.Var("VAR_SANDGEM_TOWN_STATE"));

        // Route 202 sends the player home first; Mom gives the Journal, and the rival's mother the parcel for him
        var sentBack = game.Step("CatchingLesson");
        Assert.Equal(game.Map.Triggers.First(t => t.Script == "CatchingLesson").X + 1, sentBack.PlayerTile.X);
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.True(game.Fires("CatchingLesson"));
        game.Through("PlayerHouse");
        game.Talk("mom");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Journal")!));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Parcel")!));
        Assert.Null(game.Present("rival_mom"));

        // The lesson: the assistant catches a Bidoof that never reaches the player's team, and gives five Poké Balls
        game.Through("Sinnoh");
        var lesson = game.Step("CatchingLesson");
        Assert.Contains("catchinglesson Bidoof 2 Caught", lesson.Log);
        Assert.Single(game.Party.Members);
        Assert.Equal(5, game.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.False(game.Fires("CatchingLesson"));
        Assert.Null(game.Present("counterpart"));

        // What the chapter leaves behind is exactly what an old save is given in its place (common.OpeningDone)
        var done = new StoryState();
        StoryMigration.BeginNewGame(done, Scripts);
        StoryMigration.PastTheOpening(done, Scripts);
        Assert.Equal(done.Variables.OrderBy(v => v.Key), game.Story.Variables.OrderBy(v => v.Key));
        Assert.Empty(done.Flags.Except(game.Story.Flags));
        Assert.Equal(new[] { "FLAG_TALKED_TO_ROUTE_202_COUNTERPART" }, game.Story.Flags.Except(done.Flags).Order());
    }

    [Fact]
    public void ASaveFromBeforeTheChapterIsTakenToHavePlayedIt()
    {
        var story = new StoryState();
        StoryMigration.BeginNewGame(story, Scripts);
        story.ChooseStarter("Turtwig");
        StoryMigration.Upgrade(story, 1, new[] { new Pokemon(PokemonDatabase.Get("Turtwig")!, 12) }, Scripts);

        Assert.True(story.Has(StoryState.PokedexFlag));
        Assert.True(story.Has(StoryState.RunningShoesFlag));
        Assert.True(story.Has("FLAG_HIDE_ROUTE_202_COUNTERPART"));
        Assert.Equal(1, story.Var("VAR_ROUTE_202_STATE"));
        // A save of today is left alone
        var today = new StoryState();
        StoryMigration.BeginNewGame(today, Scripts);
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, Array.Empty<Pokemon>(), Scripts);
        Assert.False(today.Has(StoryState.PokedexFlag));
    }

    [Fact]
    public void TheRunningShoesAreNeededToRun()
    {
        var map = new Map(8, 8);
        foreach (bool shoes in new[] { false, true })
        {
            var player = new Player(3, 3) { HasRunningShoes = shoes };
            player.Advance(0.01f, map, Direction.Right, run: true, onWildEncounter: null, onWarpTrigger: null);
            Assert.Equal(shoes, player.IsRunning);
        }
    }

    [Fact]
    public void TheStartMenuShowsThePokedexAndTheTeamOnlyOnceThePlayerHasThem()
    {
        var menu = new StartMenu { HasPokedex = false, HasPokemon = false };
        menu.Open();
        int all = new StartMenu().EntryCount;
        Assert.Equal(all - 2, menu.EntryCount);
        Assert.Equal(StartMenuChoice.Bag, menu.Confirm());

        menu = new StartMenu { HasPokedex = false };
        menu.Open();
        Assert.Equal(StartMenuChoice.Pokemon, menu.Confirm());
    }

    [Fact]
    public void TheRivalsNameIsKeptInTheSaveAndFilledIntoText()
    {
        Assert.Equal("Hi, Kit and Maya!", PlayerIdentity.Fill("Hi, {rival} and {player}!", "Maya", PlayerLook.Girl, "Kit"));
        Assert.Equal(PlayerIdentity.DefaultRivalName, new SaveData().RivalName);

        // A rival's team is named for the rival, whatever the data calls him
        var team = new Trainer { Id = "rival_route_201_piplup" };
        TrainerDatabase.Fill(team, TrainerDatabase.Get(team.Id)!);
        Assert.Equal("{rival}", team.Name);
    }

    [Fact]
    public void ABattleCanBeFoughtAsATrainerOfTheData()
    {
        var scripts = ScriptLibrary.FromSources(("route_201", "script Fight\n battle rival as \"rival_route_201_piplup\" first canlose\n end"));
        var instruction = scripts.Find("Fight", "route_201")!.Everything().Single(i => i.Op == Op.Battle);
        Assert.Equal("rival_route_201_piplup", instruction.AsTrainer);

        var wrong = ScriptLibrary.FromSources(("route_201", "script Fight\n battle rival as \"nobody_at_all\"\n end"));
        Assert.Contains(wrong.Problems(_ => true), p => p.Contains("there is no trainer"));
    }
}
