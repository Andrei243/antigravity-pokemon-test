using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Johto's first chapter (plan 20 · J4, "Elm's lab to the Zephyr Badge") played through with no screen, as a player
/// meets it: the bedroom, Mom and the Pokégear, Elm's favour and the three in his machine, the aide's Potion and Mom's
/// Running Shoes, the Guide Gent's Map Card, Mr. Pokémon's Egg and Oak's Pokédex, the red-haired boy in Cherrygrove
/// City, the theft and the policeman, the catching lesson on Route 29, Sprout Tower's Elder, Falkner and the Zephyr
/// Badge, the Togepi Egg in Violet City and the man at Route 32's head. Each scene starts as the game starts it (a step
/// onto its trigger, a door, someone spoken to), on maps of this class's own.
/// </summary>
public class JohtoStoryTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private static readonly Lazy<World> Johto = new(() => World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Johto));

    /// <summary>A game of Johto of its own: its maps, story, team and bag, and the tile the player stands on.</summary>
    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public int Starter { get; init; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public bool Impatient { get; init; }
        public bool PuzzleSolved { get; set; } = true;
        public Queue<int> Answers { get; } = new();
        public SpecialEncounters Encounters { get; } = SpecialEncounters.NewGame(new Random(0));
        public TimeOfDay Time { get; set; } = TimeOfDay.Morning;
        public DayOfWeek Weekday { get; set; } = DayOfWeek.Monday;
        public List<(string Script, HeadlessScriptHost Host)> Played { get; } = new();
        public HeadlessScriptHost Last => Played[^1].Host;
        public const string Rival = "Silver";

        public Game(int starter = 0, BattleOutcome fight = BattleOutcome.Won, bool impatient = false)
        {
            Starter = starter;
            Fight = fight;
            Impatient = impatient;
            foreach (var map in Johto.Value.BuildMaps()) maps[map.Name] = map;
            StoryMigration.BeginNewGame(Story, Scripts);
            var start = RegionDatabase.Get(RegionDatabase.Johto)!.Start!;
            Arrive(start.Map, start.X, start.Y);
        }

        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
            Map.ApplyDoors(Story.Has);
            Wandering.SendHome(Map);
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y), ScriptLibrary.OnEnter) is { } script) Play(script);
        }

        /// <summary>Through a door of this map that leads to another (the one nearest the player, where there are several).</summary>
        public void Through(string target)
        {
            var warp = Map.Warps.Where(w => w.TargetMap.Equals(target, StringComparison.OrdinalIgnoreCase))
                .OrderBy(w => Math.Abs(w.SourceX - Tile.X) + Math.Abs(w.SourceY - Tile.Y)).First();
            Arrive(warp.TargetMap, warp.TargetX, warp.TargetY);
        }

        private StepTrigger TriggerOf(string script) => Map.Triggers.First(t => t.Script == script);

        public HeadlessScriptHost Step(string script)
        {
            var trigger = TriggerOf(script);
            Tile = (trigger.X, trigger.Y);
            Assert.Same(trigger, FieldScripts.TriggerAt(Map, trigger.X, trigger.Y, Story));
            return Play(Scripts.Find(trigger.Script, trigger.ScriptFile ?? Map.Name)!);
        }

        public bool Fires(string script)
        {
            var trigger = TriggerOf(script);
            return FieldScripts.TriggerAt(Map, trigger.X, trigger.Y, Story) == trigger;
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
        }

        /// <summary>A tile of this map read by facing it (the lab's machine).</summary>
        public HeadlessScriptHost Read(int x, int y)
        {
            Tile = (x, y + 1);
            Assert.True(Map.TileScripts.TryGetValue((x, y), out var name), $"Nothing is read at {x},{y} of {Map.Name}");
            return Play(Scripts.Find(name!, Map.ScriptFileAt(x, y))!);
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
                Fight = _ => Fight,
                NeedsPokemon = true,
                PuzzleSolved = PuzzleSolved,
                Encounters = Encounters,
                TimeOfDay = Time,
                Weekday = Weekday
            };
            while (Answers.Count > 0) host.Answers.Enqueue(Answers.Dequeue());
            var runner = new ScriptRunner(Scripts, host) { Impatient = Impatient };
            runner.Start(script, subject);
            runner.RunToEnd();
            Played.Add((script.FullName, host));
            Assert.Empty(host.Problems);
            foreach (var map in maps.Values.Distinct())
            {
                map.ApplyPresence(Story.Has);
                map.ApplyDoors(Story.Has);
            }
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
    public void TheChapterPlaysFromTheBedroomToTheZephyrBadge(int starter, BattleOutcome fight)
    {
        var game = new Game(starter, fight);
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        string mine = johto.Starters[starter];
        string rivals = johto.RivalStarterFor(mine);

        // The bedroom, with nothing at all: no Pokémon, no Pokégear, no Pokédex, no Running Shoes
        Assert.Equal("NewBark_PlayersHouse_2F", game.Map.Name);
        Assert.Empty(game.Party.Members);
        Assert.Equal(0, game.Bag.AllItems.Count());
        Assert.False(game.Story.Has("FLAG_GOT_POKEGEAR"));
        Assert.False(game.Story.Has(StoryState.PokedexFlag));
        Assert.False(game.Story.Has(StoryState.RunningShoesFlag));

        // Downstairs, Mom has her word and the Pokégear
        game.Through("NewBark_PlayersHouse_1F");
        Assert.True(game.Story.Has("FLAG_GOT_POKEGEAR"));
        Assert.Equal(1, game.Story.Var("VAR_SCENE_PLAYERS_HOUSE_1F"));
        Assert.Contains(game.Last.Transcript, l => l.Speaker == "Mom");

        // Out in the street: the boy at the lab's window, the friend and their Marill; west, the friend turns the player back
        game.Through("Johto");
        Assert.NotNull(game.Present("t20_gsrivel"));
        Assert.NotNull(game.Present("t20_var_1_2"));
        Assert.True(game.Fires("WestExit"));
        var turned = game.Step("WestExit");
        Assert.NotEqual((676, 396), turned.PlayerTile);
        Assert.True(game.Fires("WestExit"));

        // Elm's favour as the player walks in, then the same step turns them back until they have chosen
        game.Through("NewBark_ElmsLab");
        Assert.True(game.Fires("Errand"));
        game.Step("Errand");
        Assert.True(game.Story.Has("FLAG_ELMS_LAB_PREVENT_PLAYER_ESCAPE"));
        Assert.True(game.Fires("Errand"));
        game.Step("Errand");
        Assert.Contains(game.Last.Transcript, l => l.Speaker == "Prof. Elm");

        // The machine behind his desk: the choice of three, the number in the Pokégear, the rival gone from the window
        game.Read(6, 3);
        Assert.Equal(mine, Assert.Single(game.Party.Members).Species.Name);
        Assert.Equal(rivals, game.Story.RivalStarter);
        Assert.Equal(1, game.Story.Var("VAR_SCENE_ELMS_LAB"));
        Assert.True(game.Story.Has("FLAG_HIDE_NEW_BARK_RIVAL"));
        Assert.False(game.Fires("Errand"));

        // On the way out, the aide's Potion; on the step, Mom's Running Shoes
        game.Step("AideStops");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Potion")!));
        Assert.Equal(2, game.Story.Var("VAR_SCENE_ELMS_LAB"));
        game.Through("Johto");
        Assert.Null(game.Present("t20_gsrivel"));
        Assert.True(game.Fires("RunningShoes"));
        game.Step("RunningShoes");
        Assert.True(game.Story.Has(StoryState.RunningShoesFlag));
        Assert.False(game.Fires("RunningShoes"));
        Assert.Null(game.Present("t20_gsmama"));

        // West now: the way is free, and Cherrygrove's Guide Gent gives the tour and the Map Card
        game.Step("WestExit");
        Assert.False(game.Fires("WestExit"));
        Assert.True(game.Fires("GuideGentStops"));
        game.Step("GuideGentStops");
        Assert.True(game.Story.Has("FLAG_GOT_MAP_CARD"));
        Assert.Null(game.Present("t21_gsoldman1"));
        Assert.False(game.Fires("GuideGentStops"));
        Assert.False(game.Fires("RivalAmbush"));

        // Mr. Pokémon's: the Mystery Egg, Oak and the Pokédex, and Elm's call
        game.Through("Route30_MrPokemonsHouse");
        Assert.True(game.Story.Has("FLAG_GOT_MYSTERY_EGG"));
        Assert.True(game.Story.Has(StoryState.PokedexFlag));
        Assert.Null(game.Present("r30r0201_ookido"));
        Assert.Equal(3, game.Story.Var("VAR_SCENE_ELMS_LAB"));
        Assert.Contains(game.Last.Transcript, l => l.Speaker == "Prof. Oak");

        // Back through Cherrygrove: the red-haired boy, HeartGold's team for the starter he took, win or lose
        game.Through("Johto");
        Assert.True(game.Fires("RivalAmbush"));
        var ambush = game.Step("RivalAmbush");
        string team = rivals switch { "Cyndaquil" => "johto_rival_silver_2", "Totodile" => "johto_rival_silver_3", _ => "johto_rival_silver_6" };
        Assert.Contains($"battle {team} first {fight}", ambush.Log);
        Assert.Equal(rivals, Assert.Single(TrainerDatabase.Get(team)!.Party).Species);
        Assert.Equal(4, game.Story.Var("VAR_SCENE_CHERRYGROVE_CITY_OW"));
        Assert.Equal(fight == BattleOutcome.Won, game.Story.Has("FLAG_BEAT_RIVAL_IN_CHERRYGROVE"));
        Assert.False(game.Fires("RivalAmbush"));

        // The lab after the theft: the policeman, the Egg handed over, five Poké Balls; the lesson waits on Route 29
        game.Fight = BattleOutcome.Won;
        game.Through("NewBark_ElmsLab");
        Assert.Equal(4, game.Story.Var("VAR_SCENE_ELMS_LAB"));
        Assert.True(game.Story.Has("FLAG_HIDE_ELMS_LAB_OFFICER"));
        Assert.Null(game.Present("t20r0101_policeman"));
        Assert.Equal(5, game.Bag.GetQuantity(ItemDatabase.Get("Poké Ball")!));
        Assert.Contains(game.Last.Transcript, l => l.Text.Contains(Game.Rival));
        game.Through("Johto");
        Assert.True(game.Fires("Lesson"));
        var lesson = game.Step("Lesson");
        Assert.Contains(lesson.Log, l => l.StartsWith("catchinglesson"));
        Assert.False(game.Fires("Lesson"));

        // Sprout Tower: the boy leaves the top by an Escape Rope, and Elder Li battles for Flash
        game.Arrive("SproutTower_3F", 15, 26);
        Assert.True(game.Fires("RivalLeaves"));
        game.Step("RivalLeaves");
        Assert.True(game.Story.Has("FLAG_HIDE_SPROUT_TOWER_RIVAL"));
        Assert.False(game.Fires("RivalLeaves"));
        var li = game.Talk("d15r0103_chourou");
        Assert.Contains(li.Log, l => l.StartsWith("battle johto_elder_li"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM70")!));
        Assert.True(game.Story.Has("FLAG_GOT_TM70"));

        // Falkner: the Zephyr Badge, TM51, his Bird Keepers counted beaten, and Elm's call
        game.Arrive("Violet_Gym", 15, 27);
        Assert.NotNull(game.Present("t22gym0101_sunglasses_2"));
        Assert.Null(game.Present("t22gym0101_sunglasses"));
        var falkner = game.Talk("t22gym0101_gsleader1");
        Assert.Contains(falkner.Log, l => l.StartsWith("battle johto_leader_falkner_falkner"));
        Assert.True(game.Story.HasBadge("Johto", "Zephyr"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM51")!));
        Assert.True(game.Story.HasDefeated("johto_bird_keeper_gs_abe"));
        Assert.True(game.Story.HasDefeated("johto_bird_keeper_gs_rod"));
        Assert.Equal(5, game.Story.Var("VAR_SCENE_ELMS_LAB"));

        // Violet City's Pokémon Center: Elm's aide with the Egg
        game.Arrive("Violet_PokemonCenter_1F", 8, 18);
        Assert.NotNull(game.Present("violet_pokecenter_1f.Aide"));
        game.Talk("violet_pokecenter_1f.Aide");
        Assert.Contains(game.Party.Members, p => p.IsEgg && p.Species.Name == "Togepi");
        Assert.True(game.Story.Has("FLAG_GOT_TOGEPI_EGG"));
        Assert.Null(game.Present("violet_pokecenter_1f.Aide"));

        // Route 32's head: with the Badge, the man's Miracle Seed, and the way south open
        game.Arrive("Johto", 475, 304);
        Assert.True(game.Fires("BadgeCheck"));
        var seed = game.Step("BadgeCheck");
        Assert.Equal((475, 305), seed.PlayerTile);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Miracle Seed")!));
        Assert.False(game.Fires("BadgeCheck"));
    }

    [Fact]
    public void WhitneyCriesAndThenGivesThePlainBadge()
    {
        // Plan 20 · J5, part b: the Goldenrod Gym. Whitney battles, loses and cries; spoken to again, she gives the Plain
        // Badge and TM45; after that she only talks. Her four trainers count as beaten with the battle
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machop")!, 25));
        game.Arrive("Goldenrod_Gym", 6, 24);
        Assert.Contains(game.Talk("t25gym0101_sunglasses").Transcript, l => l.Speaker == "Gym Guide");

        var battle = game.Talk("t25gym0101_gsleader3");
        Assert.Contains(battle.Log, l => l.StartsWith("battle johto_leader_whitney "));
        Assert.True(game.Story.Has("FLAG_DEFEATED_WHITNEY"));
        Assert.False(game.Story.HasBadge("Johto", "Plain"));
        foreach (string trainer in new[] { "johto_lass_cathy", "johto_lass_carrie", "johto_beauty_victoria", "johto_beauty_samantha" })
            Assert.True(game.Story.HasDefeated(trainer), trainer);

        game.Talk("t25gym0101_gsleader3");
        Assert.True(game.Story.HasBadge("Johto", "Plain"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM45")!));
        Assert.True(game.Story.Has("FLAG_GOT_TM45_FROM_WHITNEY"));

        var after = game.Talk("t25gym0101_gsleader3");
        Assert.DoesNotContain(after.Log, l => l.StartsWith("battle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM45")!));
        Assert.Contains(game.Talk("t25gym0101_sunglasses").Transcript, l => l.Text.Contains("cry"));
    }

    [Fact]
    public void ALostBattleWithWhitneyLeavesNoBadge()
    {
        // A lost battle ends the script before she cries: nothing is set
        var game = new Game(fight: BattleOutcome.Lost);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machop")!, 25));
        game.Arrive("Goldenrod_Gym", 6, 24);
        game.Talk("t25gym0101_gsleader3");
        Assert.False(game.Story.Has("FLAG_DEFEATED_WHITNEY"));
        Assert.False(game.Story.HasBadge("Johto", "Plain"));
    }

    [Fact]
    public void RouteThirtyTwosManTurnsBackWhoeverHasNoBadge()
    {
        var game = new Game();
        game.Arrive("Johto", 475, 304);
        var turned = game.Step("BadgeCheck");
        Assert.Equal((475, 304), turned.PlayerTile);
        Assert.True(game.Fires("BadgeCheck"));
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Miracle Seed")!));
    }

    [Fact]
    public void ANewGameOfJohtoHidesTheChaptersPeopleUntilTheirScenes()
    {
        var game = new Game();
        foreach (string flag in new[]
                 {
                     "FLAG_HIDE_NEW_BARK_MOM", "FLAG_HIDE_ELMS_LAB_OFFICER", "FLAG_HIDE_CHERRYGROVE_RIVAL",
                     "FLAG_HIDE_ROUTE_29_FRIEND", "FLAG_HIDE_SPROUT_TOWER_RIVAL", "FLAG_HIDE_VIOLET_POKECENTER_AIDE"
                 })
            Assert.True(game.Story.Has(flag), flag);
        // Its start is the bedroom, and a Champion of Kanto who sails over steps out before the house
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        Assert.True(johto.HasStory);
        Assert.Equal(("NewBark_PlayersHouse_2F", 4, 9), (johto.Start!.Map, johto.Start.X, johto.Start.Y));
        Assert.Equal("Johto", johto.ArrivalSpot!.Map);
    }

    [Fact]
    public void JohtosRolesWithNoSongOfTheirOwnPlayJohtosAndSinnohsAreAsTheyWere()
    {
        Assert.Equal("johto/battle_wild", MusicDirector.Resolve(MusicRole.BattleWild, "Johto", MusicLibrary.Exists));
        Assert.Equal("johto/battle_rival", MusicDirector.Resolve(MusicRole.BattleRival, "Johto", MusicLibrary.Exists));
        Assert.Equal("johto/battle_gym", MusicDirector.Resolve(MusicRole.BattleGymLeader, "Johto", MusicLibrary.Exists));
        Assert.Equal("johto/battle_galactic", MusicDirector.Resolve(MusicRole.BattleGalacticBoss, "Johto", MusicLibrary.Exists));
        Assert.Equal("johto/victory_trainer", MusicDirector.Resolve(MusicRole.VictoryGalactic, "Johto", MusicLibrary.Exists));
        Assert.Equal("johto/eye_mystery", MusicDirector.Resolve(MusicRole.EyeMystery, "Johto", MusicLibrary.Exists));
        // Plan 20 · J5, part b: the Beauties', the tough ones' and the cheerful ones' eye themes of Johto's own
        Assert.Equal("johto/eye_lady", MusicDirector.Resolve(MusicRole.EyeLady, "Johto", MusicLibrary.Exists));
        Assert.Equal("johto/eye_fighter", MusicDirector.Resolve(MusicRole.EyeFighter, "Johto", MusicLibrary.Exists));
        Assert.Equal("johto/eye_fun", MusicDirector.Resolve(MusicRole.EyeFun, "Johto", MusicLibrary.Exists));
        Assert.Equal("common/eye_lady", MusicDirector.Resolve(MusicRole.EyeLady, "Sinnoh", MusicLibrary.Exists));
        Assert.Equal("common/battle_galactic", MusicDirector.Resolve(MusicRole.BattleGalactic, "Sinnoh", MusicLibrary.Exists));
        Assert.Equal("common/battle_wild", MusicDirector.Resolve(MusicRole.BattleWild, "Sinnoh", MusicLibrary.Exists));
    }

    [Fact]
    public void ASaveOfJohtoFromBeforeTheChapterIsTakenPastItsOpening()
    {
        // A game begun in Johto before J4 began in front of the house with a Chikorita and no starter recorded
        var story = new StoryState();
        StoryMigration.Upgrade(story, 20, new[] { new Pokemon(PokemonDatabase.Get("Chikorita")!, 5) }, Scripts);
        Assert.Equal("Chikorita", story.PlayerStarter);
        Assert.Equal(4, story.Var("VAR_SCENE_ELMS_LAB"));
        Assert.True(story.Has(StoryState.PokedexFlag));
        Assert.True(story.Has("FLAG_GOT_POKEGEAR"));
        Assert.True(story.Has("FLAG_HIDE_SPROUT_TOWER_RIVAL"));
        // A game of Sinnoh is only given Johto's flags
        var sinnoh = new StoryState();
        StoryMigration.Upgrade(sinnoh, 20, Array.Empty<Pokemon>(), Scripts);
        Assert.Null(sinnoh.PlayerStarter);
        Assert.Equal(0, sinnoh.Var("VAR_SCENE_ELMS_LAB"));
        Assert.True(sinnoh.Has("FLAG_HIDE_NEW_BARK_MOM"));
    }

    [Theory]
    [InlineData("northeast", "kabuto")]
    [InlineData("southeast", "aerodactyl")]
    [InlineData("southwest", "omanyte")]
    [InlineData("northwest", "hooh")]
    public void AChambersPanelIsRefusedLeftOrPutBackTogether(string chamber, string panel)
    {
        // Plan 20 · J5: the panel in the floor of each of the Ruins of Alph's chambers (HeartGold's background event at
        // 6, 6, wherever the room is cut to) runs the room's Panel script, which asks, opens the puzzle and keeps it whole once solved
        string room = "RuinsOfAlph_" + char.ToUpperInvariant(chamber[0]) + chamber[1..] + "Chamber";
        string flag = $"FLAG_ALPH_{panel.ToUpperInvariant()}_PANEL_SOLVED";
        var game = new Game();
        game.Arrive(room, 6, 16);
        var (panelTile, _) = Assert.Single(game.Map.TileScripts, kv => kv.Value == "Panel");
        var script = Scripts.Find("Panel", $"ruins_of_alph_{chamber}_entrance_first_room")!;
        game.Tile = (panelTile.X, panelTile.Y + 1);

        // Refused: nothing opens
        game.Answers.Enqueue(1);
        var refused = game.Play(script);
        Assert.DoesNotContain(refused.Log, l => l.StartsWith("open AlphPuzzle"));
        Assert.False(game.Story.Has(flag));

        // Opened and left half done: the panel stays as it was
        game.PuzzleSolved = false;
        var left = game.Play(script);
        Assert.Contains($"open AlphPuzzle {panel} left", left.Log);
        Assert.False(game.Story.Has(flag));

        // Put back together: kept so, and the panel isn't offered again
        game.PuzzleSolved = true;
        var solved = game.Play(script);
        Assert.Contains($"open AlphPuzzle {panel} solved", solved.Log);
        Assert.True(game.Story.Has(flag));
        var after = game.Play(script);
        Assert.DoesNotContain(after.Log, l => l.StartsWith("open AlphPuzzle"));
        Assert.Empty(after.Asked);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TheSecondChapterPlaysFromTheSlowpokeWellToTheFogBadge(int starter)
    {
        // Plan 20 · J7: the Slowpoke Well and Kurt, Bugsy and the Hive Badge, the red-haired boy at Azalea's gate, the
        // Farfetch'd and Cut in Ilex Forest, Whitney, the SquirtBottle, the Bicycle and the Radio Card in Goldenrod City,
        // the strange tree on Route 36, Bill and his Eevee, the Burned Tower and the beasts set loose, the Dance Theater
        // and Surf, Morty and the Fog Badge, and the Kimono Girls on their stage
        var game = new Game(starter);
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        game.Story.ChooseStarter(johto.Starters[starter]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machop")!, 30));

        // Azalea Town: the grunt shoves a passer-by away from the well and stands guard
        game.Arrive("Johto", 420, 465);
        Assert.Null(game.Present("t23_rocketm"));
        Assert.NotNull(game.Present("t23_rocketm_3"));
        game.Step("Shove");
        Assert.NotNull(game.Present("t23_rocketm"));
        Assert.Null(game.Present("t23_rocketm_3"));
        Assert.False(game.Fires("Shove"));
        Assert.Null(game.Present("t23_tsure_poke_static_slowpoke"));

        // Kurt runs to the well, and the guard is gone with him
        game.Arrive("Azalea_KurtsHouse", 4, 8);
        game.Talk("t23r0501_gantetsu");
        Assert.Null(game.Present("t23r0501_gantetsu"));
        game.Arrive("Johto", 420, 465);
        Assert.Null(game.Present("t23_rocketm"));

        // The well: Kurt's bad back, the grunts and Proton; beaten, the player is brought to Kurt's for his Lure Ball
        game.Arrive("SlowpokeWell_Entrance", 17, 9);
        Assert.Contains(game.Talk("d26r0101_gantetsu").Transcript, l => l.Speaker == "Kurt");
        game.Arrive("SlowpokeWell_B1F", 42, 26);
        foreach (string grunt in new[] { "d26r0102_rocketm", "d26r0102_rocketm_2", "d26r0102_rocketw" })
            Assert.True(game.Present(grunt)?.IsTrainer, grunt);
        var proton = game.Talk("d26r0102_rkanbum3");
        Assert.Contains("battle johto_executive_proton_proton", string.Join("|", proton.Log));
        Assert.True(game.Story.Has("FLAG_BEAT_AZALEA_ROCKETS"));
        Assert.Equal("Azalea_KurtsHouse", game.Map.Name);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Lure Ball")!));
        Assert.Equal(3, game.Story.Var("VAR_UNK_4080"));
        game.Arrive("Johto", 420, 465);
        Assert.NotNull(game.Present("t23_tsure_poke_static_slowpoke"));

        // The Azalea Gym: a cart across the web, Bugsy, the Hive Badge and TM89
        game.Arrive("Azalea_Gym", 9, 36);
        Assert.Equal((9, 24), game.Step("Cart1").PlayerTile);
        var bugsy = game.Talk("t23gym0102_gsleader2");
        Assert.Contains(bugsy.Log, l => l.StartsWith("battle johto_leader_bugsy_bugsy"));
        Assert.True(game.Story.HasBadge("Johto", "Hive"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM89")!));
        Assert.True(game.Story.HasDefeated("johto_bug_catcher_josh"));

        // The red-haired boy at the gate to Ilex Forest, with the team that beats the player's starter
        game.Arrive("Johto", 400, 463);
        Assert.True(game.Fires("Rival"));
        var rival = game.Step("Rival");
        string team = starter switch { 0 => "johto_rival_silver_7", 1 => "johto_rival_silver_10", _ => "johto_rival_silver" };
        Assert.Contains(rival.Log, l => l.StartsWith($"battle {team} "));
        Assert.False(game.Fires("Rival"));

        // Ilex Forest: the apprentice, both Farfetch'd found, his master and HM01
        game.Arrive("IlexForest", 15, 70);
        Assert.NotNull(game.Present("d36r0101_gsboy1"));
        game.Step("ApprenticeStops");
        foreach (string bird in new[] { "d36r0101_tsure_poke_static_farfetchd", "d36r0101_tsure_poke_static_farfetchd_2" })
            for (int tries = 0; tries < 3; tries++) game.Talk(bird);
        Assert.True(game.Story.Has("FLAG_GOT_HM01"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM01")!));
        Assert.Null(game.Present("d36r0101_gsboy1"));
        game.Arrive("Azalea_CharcoalKiln", 4, 8);
        game.Talk("t23r0201_gsboy1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Charcoal")!));

        // Goldenrod City: Whitney's Badge, the SquirtBottle, the Bicycle and the Radio Card
        game.Arrive("Goldenrod_Gym", 6, 24);
        game.Talk("t25gym0101_gsleader3");
        game.Talk("t25gym0101_gsleader3");
        game.Arrive("Goldenrod_FlowerShop", 5, 8);
        Assert.Null(game.Present("t25r0601_gsgirl1"));
        game.Talk("t25r0601_gswoman1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("SquirtBottle")!));
        game.Arrive("Goldenrod_BikeShop", 6, 10);
        game.Talk("t25r0201_gsmiddleman1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Bicycle")!));
        game.Arrive("Goldenrod_RadioTower_1F", 2, 13);
        foreach (int answer in new[] { 0, 0, 0, 1, 0, 1 }) game.Answers.Enqueue(answer);
        game.Talk("d23r0101_gswoman6_3");
        Assert.True(game.Story.Has(PokemonPlatinumEngine.Models.Pokegear.RadioCardFlag));

        // Route 36: the strange tree, watered and battled; the sister's berries and the way north
        game.Arrive("Johto", 420, 246);
        Assert.NotNull(game.Present("r36_usokky"));
        game.Answers.Enqueue(0);
        var tree = game.Talk("r36_usokky");
        Assert.Contains("wildbattle Sudowoodo 20 Won", tree.Log);
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_36_SUDOWOODO"));
        Assert.Null(game.Present("r36_usokky"));
        Assert.Equal(3, game.Bag.GetQuantity(ItemDatabase.Get("Pecha Berry")!));
        Assert.Null(game.Present("r36_gsgirl1_2"));
        game.Talk("r36_gsbigman");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM06")!));

        // Ecruteak City: the Gym's old man sees the player out while Morty is away
        game.Arrive("Ecruteak_Gym", 16, 52);
        Assert.Equal("Johto", game.Map.Name);
        Assert.Equal(1, game.Story.Var("VAR_UNK_4079"));

        // Bill on his way home, and his Eevee in Goldenrod
        game.Arrive("Ecruteak_PokemonCenter_1F", 8, 18);
        Assert.True(game.Story.Has("FLAG_SYS_MET_BILL"));
        game.Arrive("Goldenrod_BillsHouse", 4, 7);
        game.Talk("t25r0401_masaki");
        Assert.Contains(game.Party.Members, p => p.Species.Name == "Eevee");

        // The Burned Tower: Eusine, the red-haired boy, and the three below set loose
        game.Arrive("BurnedTower_1F", 15, 25);
        game.Step("Eusine");
        var tower = game.Step("Rival");
        string towerTeam = starter switch { 0 => "johto_rival_silver_8", 1 => "johto_rival_silver_11", _ => "johto_rival_silver_4" };
        Assert.Contains(tower.Log, l => l.StartsWith($"battle {towerTeam} "));
        game.Arrive("BurnedTower_B1F", 22, 16);
        game.Step("Awaken");
        foreach (string beast in new[] { "Raikou", "Entei" })
        {
            var roamer = game.Encounters.Roamers[Roamers.SlotOf(beast)!.Value];
            Assert.True(roamer.Active, beast);
            Assert.Contains(Roamers.PlaceOf(roamer), Roamers.JohtoRoutes);
        }
        Assert.Equal(2, game.Story.Var("VAR_UNK_4079"));
        game.Arrive("BurnedTower_1F", 15, 25);
        Assert.Null(game.Present("d18r0101_gsleader4"));

        // The Dance Theater: the grunt, and Surf from the gentleman
        game.Arrive("Ecruteak_DanceTheater", 7, 16);
        Assert.Equal(1, game.Story.Var("VAR_UNK_410C"));
        game.Talk("t27r0501_rocketm");
        Assert.Null(game.Present("t27r0501_rocketm"));
        game.Step("Surf");
        Assert.True(game.Story.Has("FLAG_GOT_HM03"));

        // Morty: a hole drops the player back by the door; the Fog Badge and TM30
        game.Arrive("Ecruteak_Gym", 16, 52);
        Assert.Equal("Ecruteak_Gym", game.Map.Name);
        Assert.Equal((16, 49), game.Step("Fall").PlayerTile);
        var morty = game.Talk("t27gym0101_gsleader4");
        Assert.Contains(morty.Log, l => l.StartsWith("battle johto_leader_morty_morty"));
        Assert.True(game.Story.HasBadge("Johto", "Fog"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM30")!));
        Assert.Equal(3, game.Story.Var("VAR_UNK_4079"));

        // The red-haired boy at the theater's door, and the five Kimono Girls on the stage
        game.Arrive("Johto", 397, 176);
        Assert.True(game.Fires("RivalBumps"));
        game.Step("RivalBumps");
        game.Arrive("Ecruteak_DanceTheater", 7, 16);
        Assert.NotNull(game.Present("t27r0501_dancer"));
        Assert.Null(game.Present("t27r0501_gsgentleman"));
        foreach (string badge in new[] { "Hive", "Plain", "Fog" }) Assert.True(game.Story.HasBadge("Johto", badge), badge);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void TheThirdChapterPlaysFromOlivinesLighthouseToTheMineralBadge(int starter)
    {
        // Plan 20 · J9: the red-haired boy at Olivine's Gym, Jasmine by Amphy and the lighthouse's lift, the Secret
        // Potion from Cianwood's pharmacy, the winch and Chuck's Storm Badge, his wife's Fly, Suicune and Eusine on
        // Cianwood's shore, Amphy cured, Jasmine's Mineral Badge, and the Moomoo Farm's Miltank fed back to health
        var game = new Game(starter);
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        game.Story.ChooseStarter(johto.Starters[starter]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machamp")!, 45));
        // As the second chapter leaves it: the three of the Burned Tower fled and the Fog Badge won
        game.Story.SetVar("VAR_UNK_4079", 4);
        game.Story.GiveBadge("Johto", "Fog");

        // Olivine City: the red-haired boy comes out of the Gym once, and is gone
        game.Arrive("Johto", 272, 248);
        Assert.Null(game.Present("t26_gsrivel"));
        Assert.True(game.Fires("RivalAtTheGym"));
        game.Step("RivalAtTheGym");
        Assert.Equal(1, game.Story.Var("VAR_UNK_4078"));
        Assert.False(game.Fires("RivalAtTheGym"));
        Assert.Null(game.Present("t26_gsrivel"));

        // Jasmine is away: the Gym has only its guide
        game.Arrive("Olivine_Gym", 6, 26);
        Assert.Null(game.Present("t26gym0101_gsleader6"));
        Assert.Null(game.Present("t26gym0101_gsgirl1"));

        // The light room: Jasmine tells of the pharmacy and steps aside from the lift
        game.Arrive("OlivineLighthouse_LightRoom", 9, 9);
        bool LiftOpen() => game.Map.Warps.Any(w => w.TargetMap == "OlivineLighthouse_Elevator" && !game.Map.IsSolid(w.SourceX, w.SourceY));
        Assert.False(LiftOpen());
        game.Talk("d27r0107_gsleader6");
        Assert.Equal(1, game.Story.Var("VAR_SCENE_LIGHTHOUSE_JASMINE"));
        Assert.True(LiftOpen());
        // The lift carries whoever comes in to the other end
        game.Arrive("OlivineLighthouse_Elevator", 3, 6);
        Assert.Equal("OlivineLighthouse_1F", game.Map.Name);
        Assert.True(game.Map.Warps.Any(w => w.TargetMap == "OlivineLighthouse_Elevator" && !game.Map.IsSolid(w.SourceX, w.SourceY)));
        game.Arrive("OlivineLighthouse_Elevator", 3, 6);
        Assert.Equal("OlivineLighthouse_LightRoom", game.Map.Name);

        // Cianwood's pharmacy gives the Secret Potion once, and sells afterwards
        game.Arrive("Cianwood_Pharmacy", 5, 8);
        game.Talk("t24r0501_sunglasses");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Secret Potion")!));
        Assert.Contains(game.Talk("t24r0501_sunglasses").Log, l => l == "open Shop cianwood_pharmacy");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Secret Potion")!));

        // The Cianwood Gym: Chuck hears nothing until the winch stops his waterfall
        game.Arrive("Cianwood_Gym", 13, 18);
        Assert.DoesNotContain(game.Talk("t24gym0101_gsleader5").Log, l => l.StartsWith("battle"));
        game.Answers.Enqueue(0);
        game.Read(10, 2);
        Assert.True(game.Story.Has("FLAG_MAP_LOCAL_CIANWOOD_GYM_FALLS_STOPPED"));
        var chuck = game.Talk("t24gym0101_gsleader5");
        Assert.Contains(chuck.Log, l => l.StartsWith("battle johto_leader_chuck_chuck"));
        Assert.True(game.Story.HasBadge("Johto", "Storm"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM01")!));
        Assert.True(game.Story.HasDefeated("johto_black_belt_lung"));

        // Out of the Gym, Chuck's wife gives Fly
        game.Arrive("Johto", 172, 358);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM02")!));
        Assert.Equal(2, game.Story.Var("VAR_UNK_4116"));
        Assert.Equal("Storm", FieldMoveRules.BadgeFor(FieldMove.Fly, "Johto"));

        // Suicune on the north shore bounds off, and Eusine battles
        Assert.NotNull(game.Present("t24_tsure_poke_static_suicune"));
        Assert.Null(game.Present("t24_minaki"));
        var eusine = game.Step("Suicune");
        Assert.Contains(eusine.Log, l => l.StartsWith("battle johto_mystery_man_eusine"));
        Assert.Equal(2, game.Story.Var("VAR_UNK_4076"));
        Assert.False(game.Fires("Suicune"));
        game.Arrive("Johto", 172, 358);
        Assert.Null(game.Present("t24_tsure_poke_static_suicune"));
        Assert.Null(game.Present("t24_minaki"));

        // Amphy cured: the potion is taken and Jasmine goes back to her Gym
        game.Arrive("OlivineLighthouse_LightRoom", 9, 9);
        game.Answers.Enqueue(0);
        game.Talk("d27r0107_gsleader6");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Secret Potion")!));
        Assert.Equal(2, game.Story.Var("VAR_SCENE_LIGHTHOUSE_JASMINE"));
        Assert.Null(game.Present("d27r0107_gsleader6"));

        // Jasmine, the Mineral Badge and TM23
        game.Arrive("Olivine_Gym", 6, 26);
        Assert.NotNull(game.Present("t26gym0101_gsgirl1"));
        var jasmine = game.Talk("t26gym0101_gsleader6");
        Assert.Contains(jasmine.Log, l => l.StartsWith("battle johto_leader_jasmine_jasmine"));
        Assert.True(game.Story.HasBadge("Johto", "Mineral"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM23")!));
        foreach (string badge in new[] { "Fog", "Storm", "Mineral" }) Assert.True(game.Story.HasBadge("Johto", badge), badge);
    }

    [Fact]
    public void TheFourthChapterPlaysFromTheLakeOfRageToTheGlacierBadge()
    {
        // Plan 20 · J11, part a: the toll on Route 43, the red Gyarados and Lance, the souvenir shop's stairs, the Team
        // Rocket Hideout floor by floor (the statues' alarm, the computer, a trap, Lance's welcome, the two halves of a
        // password, the rival, Petrel in disguise, the Murkrow's chase, Ariana's tag battle beside Lance, the generator's
        // three Electrode), the toll gate empty afterwards and Pryce's Glacier Badge
        var game = new Game();
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        game.Story.ChooseStarter(johto.Starters[0]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machamp")!, 45));
        // As the third chapter leaves it: the Storm and Mineral Badges won
        game.Story.GiveBadge("Johto", "Storm");
        game.Story.GiveBadge("Johto", "Mineral");

        // Route 43's gate house: the grunts take the toll on arrival, while the guard is out of the way
        game.Arrive("Route43_Gate", 5, 11);
        Assert.Equal(2000, game.Last.Money);
        Assert.NotNull(game.Present("r43r0201_rocketm"));
        Assert.Null(game.Present("r43r0201_counterm"));

        // The souvenir shop: its stairs are a wall behind the counter until Lance finds them
        game.Arrive("Mahogany_SouvenirShop", 5, 8);
        var stairs = game.Map.Warps.First(w => w.TargetMap == "TeamRocketHQ_B1F");
        Assert.True(game.Map.IsSolid(stairs.SourceX, stairs.SourceY));
        Assert.Null(game.Present("d35r0101_wataru"));

        // The lake: the red Gyarados, met shiny, and Lance on the shore once it is gone
        var gyarados = Johto.Value.Area("lake_of_rage")!.Objects.Single(o => o.Id == "t29_rgyaradosu");
        game.Arrive("Johto", gyarados.X, gyarados.Z);
        Assert.Null(game.Present("t29_wataru"));
        var rage = game.Talk("t29_rgyaradosu");
        Assert.Contains(rage.Log, l => l.StartsWith("wildbattle Gyarados 30"));
        Assert.True(game.Story.Has("FLAG_HIDE_LAKE_OF_RAGE_RED_GYARADOS"));
        Assert.True(game.Story.Has("FLAG_GOT_RED_SCALE"));
        Assert.Null(game.Present("t29_rgyaradosu"));
        Assert.NotNull(game.Present("t29_wataru"));
        // Lance asked to wait, then agreed with
        game.Answers.Enqueue(1);
        game.Talk("t29_wataru");
        Assert.True(game.Story.Has("FLAG_UNK_0C3"));
        Assert.Equal(0, game.Story.Var("VAR_UNK_40A8"));
        game.Answers.Enqueue(0);
        game.Talk("t29_wataru");
        Assert.Equal(1, game.Story.Var("VAR_UNK_40A8"));
        Assert.Null(game.Present("t29_wataru"));

        // The shop again: Lance's Dragonite knocks the lookout aside and the stairs are found
        game.Arrive("Mahogany_SouvenirShop", 5, 8);
        Assert.Equal(2, game.Story.Var("VAR_UNK_40A8"));
        Assert.True(game.Story.Has("FLAG_MAHOGANY_SHOP_STAIRS_FOUND"));
        Assert.False(game.Map.IsSolid(stairs.SourceX, stairs.SourceY));

        // B1F: a statue's alarm brings a grunt out twice, the computer turns the rest off, a trap throws a Koffing
        game.Through("TeamRocketHQ_B1F");
        Assert.Equal("TeamRocketHQ_B1F", game.Map.Name);
        var alarm = game.Step("StatueNorthEast");
        Assert.Contains(alarm.Log, l => l.StartsWith("battle johto_team_rocket_grunt_20"));
        Assert.Contains(alarm.Log, l => l.StartsWith("battle johto_team_rocket_grunt_21"));
        Assert.Equal(2, game.Story.Var("VAR_UNK_40AD"));
        Assert.False(game.Fires("StatueNorthEast"));
        game.Answers.Enqueue(0);
        game.Play(Scripts.Find("Computer", "team_rocket_headquarters_b1f")!);
        Assert.True(game.Story.Has("FLAG_UNK_0D6"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_40AE"));
        Assert.Equal(2, game.Story.Var("VAR_UNK_40AD"));
        Assert.False(game.Fires("StatueNorthWest"));
        var trap = game.Step("KoffingTrap1");
        Assert.Contains(trap.Log, l => l.StartsWith("wildbattle Koffing 21"));
        Assert.False(game.Fires("KoffingTrap1"));

        // B2F: Lance's welcome at the foot of the stairs
        game.Through("TeamRocketHQ_B2F");
        Assert.Equal(1, game.Story.Var("VAR_UNK_40A9"));
        Assert.Null(game.Present("d35r0103_wataru"));

        // B3F: Lance again, the two grunts' halves of the password, the shutters, the rival, Petrel
        game.Tile = (50, 29);
        game.Through("TeamRocketHQ_B3F");
        Assert.Equal(1, game.Story.Var("VAR_UNK_40AC"));
        Assert.Null(game.Present("d35r0104_wataru"));
        game.Talk("d35r0104_babyboy1_9");
        Assert.NotNull(game.Present("d35r0104_babyboy1_9"));
        Assert.Contains(game.Talk("d35r0104_rocketw").Log, l => l.StartsWith("battle johto_team_rocket_f_grunt_5"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_40AC"));
        Assert.Contains(game.Talk("d35r0104_rocketm_2").Log, l => l.StartsWith("battle johto_team_rocket_grunt_19"));
        Assert.Equal(2, game.Story.Var("VAR_UNK_40AC"));
        game.Talk("d35r0104_babyboy1_9");
        Assert.True(game.Story.Has("FLAG_ROCKET_HIDEOUT_B3F_DOOR_OPEN"));
        Assert.Null(game.Present("d35r0104_babyboy1_9"));
        Assert.Null(game.Present("d35r0104_babyboy1_9_2"));
        Assert.True(game.Fires("Rival"));
        game.Step("Rival");
        Assert.Equal(3, game.Story.Var("VAR_UNK_40AC"));
        Assert.Null(game.Present("d35r0104_gsrivel"));
        Assert.NotNull(game.Present("d35r0104_sakaki"));
        var petrel = game.Step("Petrel");
        Assert.Contains(petrel.Log, l => l.StartsWith("battle johto_executive_petrel_petrel_2"));
        Assert.Equal(4, game.Story.Var("VAR_UNK_40AC"));
        Assert.Null(game.Present("d35r0104_sakaki"));
        Assert.Null(game.Present("d35r0104_rkanbum2"));
        Assert.Null(game.Present("d35r0104_tsure_poke_static_murkrow"));

        // The Murkrow's chase: B2F's north corridor, B3F's east one, B2F's south one, and the password it says
        game.Tile = (11, 4);
        game.Through("TeamRocketHQ_B2F");
        Assert.NotNull(game.Present("d35r0103_tsure_poke_static_murkrow"));
        game.Step("MurkrowOne");
        Assert.Equal(5, game.Story.Var("VAR_UNK_40AC"));
        game.Tile = (50, 4);
        game.Through("TeamRocketHQ_B3F");
        Assert.NotNull(game.Present("d35r0104_tsure_poke_static_murkrow_2"));
        game.Step("MurkrowTwo");
        Assert.Equal(6, game.Story.Var("VAR_UNK_40AC"));
        game.Tile = (50, 29);
        game.Through("TeamRocketHQ_B2F");
        game.Step("MurkrowTwo");
        Assert.Equal(7, game.Story.Var("VAR_UNK_40AC"));
        Assert.NotNull(game.Present("d35r0103_tsure_poke_static_murkrow_3"));
        game.Step("Password");
        Assert.Equal(8, game.Story.Var("VAR_UNK_40AC"));
        Assert.True(game.Story.Has("FLAG_UNK_0D3"));
        game.Talk("d35r0103_babyboy1_9");
        Assert.True(game.Story.Has("FLAG_ROCKET_HIDEOUT_B2F_DOOR_OPEN"));

        // Ariana and a grunt, battled beside Lance
        var ariana = game.Step("Ariana");
        Assert.Contains(ariana.Log, l => l.StartsWith("battle johto_executive_ariana_ariana_2 and johto_team_rocket_grunt_25 with johto_pkmn_trainer_lance_lance"));
        Assert.Equal(9, game.Story.Var("VAR_UNK_40AC"));
        Assert.True(game.Story.Has("FLAG_HIDE_MAHOGANY_SHOP_SHADY_SALESMAN"));
        Assert.Null(game.Present("d35r0103_rkanbuw"));
        Assert.Null(game.Present("d35r0103_rocketm"));
        Assert.NotNull(game.Present("d35r0103_wataru"));
        Assert.True(game.Fires("LanceStops"));

        // The generator's three Electrode: the hideout is cleared with the last
        foreach (string electrode in new[] { "d35r0103_tsure_poke_static_electrode", "d35r0103_tsure_poke_static_electrode_2", "d35r0103_tsure_poke_static_electrode_3" })
        {
            Assert.False(game.Story.Has("FLAG_ROCKET_HIDEOUT_CLEARED"));
            Assert.Contains(game.Talk(electrode).Log, l => l.StartsWith("wildbattle Electrode 23"));
        }
        Assert.True(game.Story.Has("FLAG_ROCKET_HIDEOUT_CLEARED"));
        Assert.Equal(10, game.Story.Var("VAR_UNK_40AC"));
        Assert.Equal(1, game.Story.Var("VAR_ROCKET_TRAP_GEODUDE_5"));
        Assert.True(game.Story.Has("FLAG_UNK_1F9"));
        Assert.Null(game.Present("d35r0103_wataru"));
        Assert.Null(game.Present("d35r0103_tsure_poke_static_electrode_4"));

        // The gate house is the guard's again, with TM36 for the player
        game.Arrive("Route43_Gate", 5, 11);
        Assert.Equal(3000, game.Last.Money);
        Assert.Null(game.Present("r43r0201_rocketm"));
        game.Talk("r43r0201_counterm");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM36")!));

        // Pryce, the Glacier Badge and TM07; with Chuck's and Jasmine's Badges held, Team Rocket's takeover begins
        game.Arrive("Mahogany_Gym_3", 5, 18);
        var pryce = game.Talk("t28gym0101_gsleader7");
        Assert.Contains(pryce.Log, l => l.StartsWith("battle johto_leader_pryce_pryce"));
        Assert.True(game.Story.HasBadge("Johto", "Glacier"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM07")!));
        Assert.True(game.Story.HasDefeated("johto_skier_diana"));
        Assert.Equal(1, game.Story.Var("VAR_SCENE_ROCKET_TAKEOVER"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TeamRocketsTakeoverPlaysFromTheThirdBadgeToTheRainbowWing(int starter)
    {
        // Plan 20 · J11, part b: Pryce's Badge as the third of the three, Elm's call in the next town, Goldenrod taken,
        // the uniform under the city, the red-haired boy at the tower's door and the grunt who sees through it, the
        // false Director and the Basement Key, the locked door, the red-haired boy under the city, the shutters' three
        // switches, the real Director and his Card Key, the slot on the third floor, Archer on the deck and the
        // Rainbow Wing, and the town and the tower as they are afterwards
        var game = new Game(starter);
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        game.Story.ChooseStarter(johto.Starters[starter]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machamp")!, 50));
        game.Story.GiveBadge("Johto", "Storm");
        game.Story.GiveBadge("Johto", "Mineral");
        var world = Johto.Value;

        // The Glacier Badge is the last of the three: the takeover begins, and Elm rings in the next town come to
        game.Arrive("Mahogany_Gym_3", 5, 18);
        game.Talk("t28gym0101_gsleader7");
        Assert.Equal(1, game.Story.Var("VAR_SCENE_ROCKET_TAKEOVER"));
        var mahogany = world.Area("mahogany")!.Warps[0];
        game.Arrive("Johto", mahogany.X, mahogany.Z + 1);
        Assert.Equal(2, game.Story.Var("VAR_SCENE_ROCKET_TAKEOVER"));
        Assert.True(game.Story.Has("FLAG_ROCKET_TAKEOVER_ACTIVE"));

        // Goldenrod City: the townspeople indoors and the grunts in the streets
        var goldenrod = world.Area("goldenrod")!.Warps[5];
        game.Arrive("Johto", goldenrod.X, goldenrod.Z + 1);
        Assert.True(game.Story.Has("FLAG_HIDE_ROCKET_TAKEOVER_2"));
        Assert.False(game.Story.Has("FLAG_HIDE_ROCKET_TAKEOVER_1"));

        // The tower's stairs are for Team Rocket in uniform
        game.Arrive("Goldenrod_RadioTower_1F", 2, 13);
        Assert.NotNull(game.Present("d23r0101_rocketm"));
        Assert.DoesNotContain(game.Talk("d23r0101_rocketm").Log, l => l.StartsWith("battle "));
        Assert.Equal(2, game.Story.Var("VAR_SCENE_ROCKET_TAKEOVER"));

        // Under the city: the floor's Trainers away, and the grunt by the photo studio hands over a uniform
        game.Arrive("Goldenrod_Underground_B1F", 5, 4);
        Assert.Null(game.Present("d37r0102_mania"));
        Assert.NotNull(game.Present("d37r0102_rocketm"));
        Assert.True(game.Fires("Disguise"));
        game.Step("Disguise");
        Assert.Equal(3, game.Story.Var("VAR_SCENE_ROCKET_TAKEOVER"));
        Assert.False(game.Fires("Disguise"));
        // The locked door wants a key
        game.Talk("d37r0102_babyboy1_8");
        Assert.False(game.Story.Has("FLAG_UNK_1C0"));

        // The grunt at the stairs lets the uniform by, the red-haired boy walks in, and the grunt sees through it
        game.Arrive("Goldenrod_RadioTower_1F", 2, 13);
        var door = game.Talk("d23r0101_rocketm");
        Assert.Contains(door.Log, l => l.StartsWith("battle johto_team_rocket_grunt_3"));
        Assert.Equal(4, game.Story.Var("VAR_SCENE_ROCKET_TAKEOVER"));
        Assert.Null(game.Present("d23r0101_rocketm"));
        Assert.Null(game.Present("radio_tower_rival"));

        // The fifth floor: the Director who isn't, Petrel and the Basement Key
        game.Arrive("Goldenrod_RadioTower_5F", 5, 14);
        Assert.NotNull(game.Present("d23r0105_rkanbuw"));
        var petrel = game.Talk("d23r0105_gsgentleman");
        Assert.Contains(petrel.Log, l => l.StartsWith("battle johto_executive_petrel_petrel"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Basement Key")!));
        Assert.Null(game.Present("d23r0105_gsgentleman"));
        Assert.NotNull(game.Present("d23r0105_rkanbum2"));

        // The locked door opens to the key
        game.Arrive("Goldenrod_Underground_B1F", 5, 4);
        game.Talk("d37r0102_babyboy1_8");
        Assert.True(game.Story.Has("FLAG_UNK_1C0"));
        Assert.Null(game.Present("d37r0102_babyboy1_8"));

        // The second basement: the red-haired boy comes down after the player
        game.Arrive("Goldenrod_Underground_B2F", 26, 5);
        Assert.NotNull(game.Present("d37r0104_rocketm"));
        Assert.True(game.Fires("Rival"));
        var rival = game.Step("Rival");
        string team = starter switch { 0 => "johto_rival_silver_18", 1 => "johto_rival_silver_12", _ => "johto_rival_silver_17" };
        Assert.Contains(rival.Log, l => l.StartsWith($"battle {team} "));
        Assert.Equal(1, game.Story.Var("VAR_UNK_40A0"));
        Assert.False(game.Fires("Rival"));
        Assert.Null(game.Present("goldenrod_b2f_rival"));

        // Green, red and blue open the way to the warehouse; the purple switch opens its corner for good
        Assert.NotNull(game.Present("b2f_gate_9_0"));
        game.Read(9, 8);
        game.Read(10, 8);
        game.Read(8, 8);
        foreach (int shut in new[] { 0, 1, 2 }) Assert.NotNull(game.Present($"b2f_gate_{shut}_0"));
        foreach (int open in new[] { 3, 4, 5, 6, 7, 8, 9 }) Assert.Null(game.Present($"b2f_gate_{open}_0"));
        Assert.NotNull(game.Present("b2f_purple_gate_1"));
        game.Read(23, 14);
        Assert.Null(game.Present("b2f_purple_gate_1"));
        // Coming in again puts the shutters back
        game.Arrive("Goldenrod_Underground_B2F", 26, 5);
        Assert.NotNull(game.Present("b2f_gate_9_0"));
        Assert.Null(game.Present("b2f_gate_0_0"));
        Assert.Null(game.Present("b2f_purple_gate_1"));

        // The warehouse: the real Director and his Card Key; the basement's shutter is open behind him
        game.Arrive("Goldenrod_Underground_Warehouse", 4, 17);
        game.Talk("d37r0105_gsgentleman");
        Assert.True(game.Story.Has("FLAG_UNK_1C1"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Card Key")!));
        game.Arrive("Goldenrod_DeptStore_Basement", 18, 7);
        Assert.Null(game.Present("d37r0103_babyboy1_9"));

        // The third floor's slot opens the shutters to the east stairs
        game.Arrive("Goldenrod_RadioTower_3F", 22, 12);
        Assert.NotNull(game.Present("d23r0103_babyboy1_9"));
        game.Read(22, 11);
        Assert.True(game.Story.Has("FLAG_UNK_1BF"));
        Assert.Null(game.Present("d23r0103_babyboy1_9"));

        // The lift up from the fifth floor, and Archer on the deck
        game.Arrive("Goldenrod_RadioTower_Elevator", 3, 5);
        Assert.Equal("Goldenrod_RadioTower_ObservationDeck", game.Map.Name);
        var archer = game.Talk("d23r0106_rkanbum");
        Assert.Contains(archer.Log, l => l.StartsWith("battle johto_executive_archer_archer"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Rainbow Wing")!));
        Assert.Equal(5, game.Story.Var("VAR_SCENE_ROCKET_TAKEOVER"));
        Assert.True(game.Story.Has("FLAG_BEAT_RADIO_TOWER_ROCKETS"));
        Assert.False(game.Story.Has("FLAG_ROCKET_TAKEOVER_ACTIVE"));
        Assert.True(game.Story.Has("FLAG_HIDE_BLACKTHORN_GYM_GUARD_INFRONT"));
        Assert.Null(game.Present("d23r0106_rkanbum"));
        Assert.Null(game.Present("deck_director"));

        // Afterwards: the Director at his desk, the third floor's thanks, and the town out in its streets again
        game.Arrive("Goldenrod_RadioTower_5F", 5, 14);
        Assert.NotNull(game.Present("d23r0105_gsgentleman"));
        Assert.Null(game.Present("d23r0105_rkanbum2"));
        Assert.Null(game.Present("d23r0105_rkanbuw"));
        game.Arrive("Goldenrod_RadioTower_3F", 22, 12);
        game.Talk("d23r0103_gswoman2");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM11")!));
        game.Arrive("Johto", goldenrod.X, goldenrod.Z + 1);
        Assert.False(game.Story.Has("FLAG_HIDE_ROCKET_TAKEOVER_2"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROCKET_TAKEOVER_1"));
    }

    [Fact]
    public void ALostBattleWithPetrelLeavesTheDirectorsDisguiseAndNoKey()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machamp")!, 50));
        game.Story.SetVar("VAR_SCENE_ROCKET_TAKEOVER", 4);
        game.Arrive("Goldenrod_RadioTower_5F", 5, 14);
        game.Fight = BattleOutcome.Lost;
        game.Talk("d23r0105_gsgentleman");
        Assert.Equal(0, game.Bag.GetQuantity(ItemDatabase.Get("Basement Key")!));
        Assert.NotNull(game.Present("d23r0105_gsgentleman"));
        Assert.Null(game.Present("d23r0105_rkanbum2"));
        Assert.False(game.Story.Has("FLAG_HIDE_RADIO_TOWER_5F_OFFICE_DIRECTOR"));
    }

    [Fact]
    public void TheDepartmentStoresLiftGoesDownToTheBasement()
    {
        // Plan 20 · J11, part b: "B1F" among the lower floors, the ride down, and up again from the basement
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machamp")!, 50));
        game.Arrive("Goldenrod_DeptStore_Elevator", 2, 3);
        game.Answers.Enqueue(4);
        game.Answers.Enqueue(2);
        var down = game.Talk("t25r1007_gswoman6");
        Assert.Contains(down.Log, l => l.StartsWith("anim elevator down"));
        Assert.Equal("Goldenrod_DeptStore_Basement", game.Map.Name);
        Assert.Equal(7, game.Story.Var("VAR_GOLDENROD_STORE_LIFT_FLOOR"));
        // Its shutter keeps the way down to the warehouse until the Director's Card Key
        Assert.NotNull(game.Present("d37r0103_babyboy1_9"));
        game.Arrive("Goldenrod_DeptStore_Elevator", 2, 3);
        game.Answers.Enqueue(0);
        var up = game.Talk("t25r1007_gswoman6");
        Assert.Contains(up.Log, l => l.StartsWith("anim elevator up"));
        Assert.Equal("Goldenrod_DeptStore_6F", game.Map.Name);
    }

    [Fact]
    public void TheMoomooFarmsMiltankIsFedSevenOranBerriesAndItsMilkIsSoldAfterwards()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machamp")!, 45));
        // Before the farmer has told what ails her, she is only looked at
        game.Arrive("Route39_MoomooFarmStable", 7, 8);
        game.Bag.AddItem(ItemDatabase.Get("Oran Berry")!, 8);
        Assert.Empty(game.Talk("r39r0201_tsure_poke_static_miltank").Asked);
        game.Arrive("Route39_MoomooFarmHouse", 6, 9);
        game.Talk("r39r0101_gsmiddleman1");
        Assert.True(game.Story.Has("FLAG_UNK_0A8"));
        // Six berries are not enough; the seventh cures her
        game.Arrive("Route39_MoomooFarmStable", 7, 8);
        for (int berry = 1; berry <= 7; berry++)
        {
            Assert.False(game.Story.Has("FLAG_UNK_0AA"));
            game.Answers.Enqueue(0);
            game.Talk("r39r0201_tsure_poke_static_miltank");
        }
        Assert.True(game.Story.Has("FLAG_UNK_0AA"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Oran Berry")!));
        // The farmer sells a bottle for 500, and his wife gives TM83 once
        game.Arrive("Route39_MoomooFarmHouse", 6, 9);
        game.Answers.Enqueue(0);
        var milk = game.Talk("r39r0101_gsmiddleman1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Moomoo Milk")!));
        Assert.Equal(2500, milk.Money);
        game.Talk("r39r0101_middlewoman1_2");
        game.Talk("r39r0101_middlewoman1_2");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM83")!));
    }

    [Fact]
    public void WardenBaobaOpensTheSafariZoneAndMakesAnOwnerOfWhoeverShowsHimAGeodudeAndASandshrew()
    {
        // Plan 20 · J8, part b, and J9, part b (the rulings' "The Johto Safari Zone")
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machamp")!, 45));
        var world = Johto.Value;
        var cliffWay = world.Area("cianwood")!.Warps[6];
        var olivine = world.Area("olivine")!.Warps[1];
        var plaza = world.Area("safari_zone_gate")!.Warps[0];

        // Until the warden rings, the workmen stand in the cliff gate
        game.Arrive("Johto", cliffWay.X, cliffWay.Z);
        game.Through("CliffEdgeGate");
        Assert.NotNull(game.Present("d48r0101_workman"));

        // Amphy cured, Olivine City rings once: the zone open, the test begun, the warden's number in the Pokégear
        game.Story.SetVar("VAR_SCENE_LIGHTHOUSE_JASMINE", 2);
        game.Arrive("Johto", olivine.X, olivine.Z + 1);
        Assert.Equal(1, game.Story.Var("VAR_UNK_4057"));
        Assert.Equal(3, game.Story.Var("VAR_SCENE_LIGHTHOUSE_JASMINE"));
        Assert.True(game.Story.Has("FLAG_UNK_0F5"));
        Assert.True(game.Story.Has(Pokegear.ContactFlag("baoba")));
        game.Arrive("Johto", cliffWay.X, cliffWay.Z);
        game.Through("CliffEdgeGate");
        Assert.Null(game.Present("d48r0101_workman"));

        // The gate hall: his welcome and the first test
        game.Answers.Enqueue(0);
        game.Arrive("SafariZone_Gate", 4, 14);
        Assert.Equal(2, game.Story.Var("VAR_UNK_4057"));

        // The counter: $500 for thirty Safari Balls and no steps, and into the zone
        game.Answers.Enqueue(0);
        var counter = game.Talk("d47r0101_counterm_3");
        Assert.True(counter.Safari.Endless);
        Assert.Equal(SafariGame.StartBalls, counter.Safari.Balls);
        Assert.Equal(2500, counter.Money);
        Assert.Equal("SafariZone", game.Map.Name);
        Assert.Equal(1, game.Story.Var("VAR_SCENE_SAFARI_ZONE_ENTRANCE"));

        // The last ball thrown brings the player back before the counter, which sees them off
        game.Play(Scripts.Find("SafariOutOfBalls", "common")!);
        Assert.Equal("SafariZone_Gate", game.Map.Name);
        Assert.Equal((4, 6), game.Tile);
        Assert.Equal(0, game.Story.Var("VAR_SCENE_SAFARI_ZONE_ENTRANCE"));

        // A Geodude met anywhere else is not the one he asked for; one met in the zone passes the first test
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Geodude")!, 15) { MetLocation = "Route 46" });
        game.Talk("d47r0101_gsgentleman");
        Assert.Equal(2, game.Story.Var("VAR_UNK_4057"));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Geodude")!, 15) { MetLocation = "Safari Zone" });
        game.Talk("d47r0101_gsgentleman");
        Assert.Equal(3, game.Story.Var("VAR_UNK_4057"));

        // The same day the plaza is quiet; the next, it plays his second call
        game.Arrive("Johto", plaza.X, plaza.Z + 1);
        Assert.Equal(3, game.Story.Var("VAR_UNK_4057"));
        Assert.Null(game.Present("d47_workman"));
        game.Story.ClearDaily();
        game.Arrive("Johto", plaza.X, plaza.Z + 1);
        Assert.Equal(4, game.Story.Var("VAR_UNK_4057"));

        // The second test, and the workmen come out on the plaza
        game.Arrive("SafariZone_Gate", 4, 14);
        Assert.Equal(5, game.Story.Var("VAR_UNK_4057"));
        game.Arrive("Johto", plaza.X, plaza.Z + 1);
        Assert.NotNull(game.Present("d47_workman"));

        // A Sandshrew of the zone makes the player its Owner
        game.Arrive("SafariZone_Gate", 4, 14);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Sandshrew")!, 15) { MetLocation = "Safari Zone" });
        game.Talk("d47r0101_gsgentleman");
        Assert.Equal(6, game.Story.Var("VAR_UNK_4057"));
    }

    [Fact]
    public void ASmashedRockLetsOutItsPlacesPokemonAtItsRate()
    {
        // Plan 20 · J9 (J7's leftover): Cianwood's rocks hold HeartGold's Krabby and Shuckle, met 90 and 10 times in a
        // hundred, one rock in three and a little under (its rockSmashRate of 30)
        var overworld = Johto.Value.BuildMaps().Single(m => m.Name == "Johto");
        var rock = overworld.Everyone.First(n => n.Key == "t24_breakrock");
        var area = overworld.AreaAt(rock.GridX, rock.GridY)!;
        Assert.Equal(30, area.RockSmashRate);
        Assert.Equal(new[] { 90, 10 }, area.RockSmashEncounters.Select(e => e.Weight));
        var random = new Random(9);
        var met = Enumerable.Range(0, 2000).Select(_ => overworld.SmashRock(rock.GridX, rock.GridY, random: random)).OfType<WildEncounterEntry>().ToList();
        Assert.InRange(met.Count, 500, 700);
        Assert.All(met, m => Assert.Contains(m.SpeciesName, new[] { "Krabby", "Shuckle" }));
        Assert.InRange(met.Count(m => m.SpeciesName == "Shuckle"), 20, 110);
        // Sinnoh's rocks hide nothing
        var sinnoh = MapDatabase.Get("Sinnoh");
        Assert.DoesNotContain(sinnoh.Areas, a => a.RockSmashEncounters.Count > 0);

        // The common script lets out what lives in the rock once it is smashed, and battles it
        var game = new Game();
        var smasher = new Pokemon(PokemonDatabase.Get("Machamp")!, 45);
        smasher.Moves.Clear();
        smasher.Moves.Add(new Move(MoveDatabase.Get("Rock Smash")!));
        game.Party.Add(smasher);
        game.Story.GiveBadge("Johto", "Zephyr");
        game.Arrive("Johto", rock.GridX, rock.GridY + 1);
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Map = game.Map, PlayerTile = game.Tile, InTheRock = ("Shuckle", 25) };
        host.Answers.Enqueue(0);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find("Rock", "common")!, game.Map.Everyone.First(n => n.Key == "t24_breakrock"));
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.Contains(host.Log, l => l.StartsWith("wildbattle Shuckle 25"));
    }

    [Fact]
    public void FlyOverJohtoLandsBeforeATownsPokemonCenterOnceItHasBeenComeTo()
    {
        // Plan 20 · J9: Johto's towns are its region's spawns, opened by their first arrivals, on its own map
        var story = new StoryState();
        var olivine = SpawnLocations.ArrivedIn(RegionDatabase.Johto, "olivine")!;
        Assert.Equal("Johto", olivine.Map);
        Assert.Equal("Olivine_PokemonCenter_1F", olivine.Room);
        Assert.Null(SpawnLocations.ArrivedIn(RegionDatabase.Johto, "route_39"));
        var map = RegionMap.Of("johto")!;
        Assert.False(new TownMapState(map, story).CanFlyTo(8, 8));
        story.Set(olivine.ArrivalFlag);
        var state = new TownMapState(map, story);
        Assert.True(state.CanFlyTo(8, 8));
        Assert.Equal(olivine, state.SpotAt(8, 8)!.Town);
        Assert.False(state.CanFlyTo(5, 11));
        // Every town of Johto's map has its place, standing before its Center's door
        var overworld = Johto.Value.BuildMaps().Single(m => m.Name == "Johto");
        foreach (var spawn in SpawnLocations.In(RegionDatabase.Johto))
        {
            Assert.Contains(map.FlySpots, f => f.Spawn == spawn.Id);
            Assert.True(overworld.IsWalkable(spawn.X, spawn.Y), $"{spawn.Area}'s spawn stands on a blocked tile");
            Assert.Equal(spawn.Room, overworld.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
        // A flight lands on Johto's map, not Sinnoh's
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { FlyTo = olivine, Region = RegionDatabase.Johto };
        Assert.True(host.Fly());
        Assert.Contains(host.Log, l => l.StartsWith("warp Johto 272 258"));
        // Sinnoh's are as they were
        Assert.Equal("Sinnoh", SpawnLocations.Get(3)!.Map);
        Assert.Same(SpawnLocations.All, SpawnLocations.In(RegionDatabase.Sinnoh));
    }

    [Fact]
    public void TheStrangeTreeWaitsForTheSquirtBottleAndStaysWhenRunFrom()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machop")!, 30));
        game.Arrive("Johto", 420, 246);
        // No SquirtBottle: it only wiggles
        var wiggle = game.Talk("r36_usokky");
        Assert.DoesNotContain(wiggle.Log, l => l.StartsWith("wildbattle"));
        Assert.Empty(wiggle.Asked);
        // Run from, it stands still again in the way
        game.Bag.AddItem(ItemDatabase.Get("SquirtBottle")!, 1);
        game.Fight = BattleOutcome.Fled;
        game.Answers.Enqueue(0);
        game.Talk("r36_usokky");
        Assert.False(game.Story.Has("FLAG_HIDE_ROUTE_36_SUDOWOODO"));
        Assert.NotNull(game.Present("r36_usokky"));
        Assert.NotNull(game.Present("r36_gsgirl1_2"));
    }

    [Theory]
    [InlineData(TimeOfDay.Day, false)]
    [InlineData(TimeOfDay.Night, true)]
    [InlineData(TimeOfDay.LateNight, true)]
    public void TheDayCareRoadsOfficersBattleOnlyAfterDark(TimeOfDay time, bool battles)
    {
        // HeartGold's R34_000 and R35_001: Officer Keith and Officer Dirk stand on their tiles at night and late at
        // night; the rest of the day their doubles, who only talk, stand there instead
        var game = new Game { Time = time };
        game.Arrive("Johto", 365, 420);
        Assert.Equal(battles, game.Present("r34_policeman") != null);
        Assert.Equal(!battles, game.Present("r34_policeman_2") != null);
        game.Arrive("Johto", 366, 300);
        Assert.Equal(battles, game.Present("r35_policeman") != null);
        Assert.Equal(!battles, game.Present("r35_policeman_2") != null);
    }

    /// <summary>A game past the Glacier Badge with Team Rocket out of the Radio Tower (part b), and a strong lead.</summary>
    private static Game PastTheTakeover()
    {
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machamp")!, 50));
        foreach (string badge in new[] { "Zephyr", "Hive", "Plain", "Fog", "Storm", "Mineral", "Glacier" }) game.Story.GiveBadge("Johto", badge);
        game.Story.SetVar("VAR_SCENE_ROCKET_TAKEOVER", 5);
        game.Story.Set("FLAG_BEAT_RADIO_TOWER_ROCKETS");
        game.Story.Set("FLAG_HIDE_BLACKTHORN_GYM_GUARD_INFRONT");
        return game;
    }

    [Fact]
    public void TheIcePathsManHisHMTheKimonoGirlOnTheIceAndTheHolesInItsFloor()
    {
        // Plan 20 · J11, part c: the man who points out HM07 and then says what it is for, the Kimono Girl pushed off
        // the ice from the tile behind her, a fall through each hole of B1F onto its own tile of B2F, and a boulder
        // pushed into a hole standing on the ice below from then on
        var game = PastTheTakeover();
        game.Arrive("IcePath_1F", 47, 5);
        Assert.True(game.Fires("Noticed"));
        game.Step("Noticed");
        Assert.Equal(0, game.Story.Var("VAR_UNK_4100"));
        Assert.True(game.Fires("Noticed"));
        game.Talk("d39r0101_monstarball");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("HM07")!));
        game.Step("Noticed");
        Assert.Equal(1, game.Story.Var("VAR_UNK_4100"));
        Assert.False(game.Fires("Noticed"));

        // Spoken to from in front she can't be helped; from the tile behind her she slides off and goes
        var dancer = game.Present("d39r0101_dancer");
        Assert.NotNull(dancer);
        game.Talk("d39r0101_dancer");
        Assert.False(game.Story.Has("FLAG_UNK_239"));
        game.Tile = (55, 28);
        game.Play(Scripts.Find("KimonoGirl", "ice_path_1f")!, dancer);
        Assert.True(game.Story.Has("FLAG_UNK_239"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_40E8"));
        game.Arrive("IcePath_1F", 47, 5);
        Assert.Null(game.Present("d39r0101_dancer"));

        // B2F as nothing has fallen yet: the four places under the holes are bare ice
        game.Arrive("IcePath_B2F", 26, 12);
        foreach (int n in new[] { 1, 2, 3, 4 }) Assert.Null(game.Present($"fallen_boulder_{n}"));
        foreach (string rock in new[] { "d39r0103_rock", "d39r0103_rock_2", "d39r0103_rock_3", "d39r0103_rock_4" }) Assert.Null(game.Present(rock));

        // Each hole drops the player onto its own tile below, with a jolt that is over as they land
        foreach (var (hole, x, y) in new[] { ("Hole1", 12, 12), ("Hole2", 10, 21), ("Hole3", 20, 10), ("Hole4", 21, 23) })
        {
            game.Arrive("IcePath_B1F", 25, 9);
            Assert.True(game.Fires(hole), hole);
            var fall = game.Step(hole);
            Assert.Contains(fall.Log, l => l.StartsWith("warp IcePath_B2F"));
            Assert.Equal("IcePath_B2F", game.Map.Name);
            Assert.Equal((x, y), game.Tile);
            Assert.Equal(0, game.Story.Var("VAR_UNK_40AB"));
        }

        // A boulder pushed into the first hole is gone from B1F for good and stands under it on B2F
        game.Arrive("IcePath_B1F", 25, 9);
        Assert.Equal("Boulder1", game.Map.BoulderPits[(11, 10)]);
        var boulder = game.Present("d39r0102_rock")!;
        Assert.Equal(PropType.StrengthBoulder, boulder.Obstacle);
        game.Play(Scripts.Find("Boulder1", "ice_path_b1f")!, boulder);
        Assert.True(game.Story.Has("FLAG_HIDE_ICE_PATH_BOULDER_1_INIT"));
        Assert.True(game.Story.Has("FLAG_ICE_PATH_HOLE_1_FILLED"));
        game.Arrive("IcePath_B1F", 25, 9);
        Assert.Null(game.Present("d39r0102_rock"));
        game.Arrive("IcePath_B2F", 26, 12);
        var fallen = game.Present("fallen_boulder_1");
        Assert.NotNull(fallen);
        Assert.Equal(NPC.FallenBoulderType, fallen!.NpcType);
        Assert.Equal((12, 13), (fallen.GridX, fallen.GridY));
        Assert.Null(game.Present("fallen_boulder_2"));
    }

    [Fact]
    public void ClairsTestTheShrinesQuestionsTheRisingBadgeAndTheEldersDratini()
    {
        // Plan 20 · J11, part c: the guards at Blackthorn's two doors, the Gym's guide, Clair beaten and sending the
        // player to the shrine, a wrong answer that ends the talk and is taken up again at the same question, Clair
        // made to give the Rising Badge, TM59 on the way out of the Dragon's Den, and the elder's Dratini without
        // Extreme Speed, since an answer was wrong
        var game = PastTheTakeover();
        var town = Johto.Value.Area("blackthorn")!.Warps[0];

        // Before Clair: the way into the Gym is open, the Den's old man stands in its door
        game.Arrive("Johto", town.X, town.Z + 1);
        Assert.Null(game.Present("t30_gsman1"));
        Assert.NotNull(game.Present("t30_gsman1_2"));
        Assert.NotNull(game.Present("t30_gsoldman1"));
        Assert.Null(game.Present("t30_gsoldman1_2"));

        // The Gym: its slabs, its guide at the door, and Clair
        game.Arrive("Blackthorn_Gym", 13, 86);
        Assert.IsType<BlackthornPlatforms>(game.Map.Puzzle);
        Assert.True(game.Fires("Guide"));
        game.Step("Guide");
        Assert.True(game.Story.Has("FLAG_UNK_138"));
        Assert.False(game.Fires("Guide"));
        var clair = game.Talk("t30gym0101_gsleader8");
        Assert.Contains(clair.Log, l => l.StartsWith("battle johto_leader_clair_clair"));
        Assert.True(game.Story.Has("FLAG_UNK_0D1"));
        Assert.False(game.Story.HasBadge("Johto", "Rising"));
        Assert.True(game.Story.HasDefeated("johto_ace_trainer_m_paulo"));
        Assert.DoesNotContain(game.Talk("t30gym0101_gsleader8").Log, l => l.StartsWith("battle "));

        // The Den's old man stands aside
        game.Arrive("Johto", town.X, town.Z + 1);
        Assert.Null(game.Present("t30_gsoldman1"));
        Assert.NotNull(game.Present("t30_gsoldman1_2"));

        // The shrine: the elder asks at once; the third answer is wrong and the talk ends there
        foreach (int answer in new[] { 0, 0, 0 }) game.Answers.Enqueue(answer);
        game.Arrive("DragonsDen_Shrine", 6, 19);
        Assert.True(game.Story.Has("FLAG_FAILED_DRAGONS_DEN_QUIZ"));
        Assert.Equal(2, game.Story.Var("VAR_DRAGON_SHRINE_QUESTION"));
        Assert.Equal(0, game.Story.Var("VAR_UNK_40C3"));
        Assert.Null(game.Present("d44r0103_gsleader8"));

        // Spoken to again he goes on from the third, and Clair comes in to give the Badge
        foreach (int answer in new[] { 1, 0, 2 }) game.Answers.Enqueue(answer);
        game.Talk("d44r0103_chourou");
        Assert.True(game.Story.HasBadge("Johto", "Rising"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_40C3"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_40C4"));
        Assert.True(game.Story.Has("FLAG_UNK_0EA"));
        Assert.False(game.Story.Has("FLAG_HIDE_VICTORY_ROAD_CLAIR"));
        Assert.Null(game.Present("d44r0103_gsleader8"));
        // Nothing yet without TM59
        game.Talk("d44r0103_chourou");
        Assert.DoesNotContain(game.Party.Members, p => p.Species.Name == "Dratini");

        // On the way out of the Den Clair gives TM59 and goes home
        game.Arrive("DragonsDen", 28, 12);
        Assert.NotNull(game.Present("d44r0102_gsleader8"));
        Assert.True(game.Fires("Clair"));
        game.Step("Clair");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM59")!));
        Assert.True(game.Story.Has("FLAG_GOT_TM59_FROM_CLAIR"));
        Assert.Equal(2, game.Story.Var("VAR_UNK_40C4"));
        Assert.Null(game.Present("d44r0102_gsleader8"));
        Assert.False(game.Fires("Clair"));

        // The elder's Dratini, without Extreme Speed
        game.Arrive("DragonsDen_Shrine", 6, 11);
        game.Talk("d44r0103_chourou");
        var dratini = Assert.Single(game.Party.Members, p => p.Species.Name == "Dratini");
        Assert.Equal(15, dratini.Level);
        Assert.DoesNotContain(dratini.Moves, m => m.Name == "Extreme Speed");
        Assert.True(game.Story.Has("FLAG_GOT_DRATINI_FROM_MASTER_JUST_NOW"));
        game.Arrive("DragonsDen_Shrine", 6, 11);
        Assert.True(game.Story.Has("FLAG_GOT_DRATINI_FROM_MASTER_LONG_AGO"));
        game.Talk("d44r0103_chourou");
        Assert.Single(game.Party.Members, p => p.Species.Name == "Dratini");
    }

    [Theory]
    [InlineData(0, "johto_rival_silver_9")]
    [InlineData(1, "johto_rival_silver_13")]
    [InlineData(2, "johto_rival_silver_5")]
    public void TheFifthChapterPlaysFromElmsMasterBallToTheHallOfFame(int starter, string rival)
    {
        // Plan 20 · J12: Elm's Master Ball for the eighth Badge, the rival's last challenge in Victory Road with the team
        // that beats the player's starter, the Elite Four's doors opening one by one with no way back, Lance, the Hall of
        // Fame and the credits, home to New Bark Town and Elm's call, and the gate's door to Kanto open to the Champion
        var game = PastTheTakeover();
        game.Story.ChooseStarter(RegionDatabase.Get(RegionDatabase.Johto)!.Starters[starter]);
        game.Story.GiveBadge("Johto", "Rising");
        game.Story.Set("FLAG_UNK_0EA");
        // (Mom has had her word on the first morning long before, as in any game this far: home after the Hall of Fame
        // goes to Elm's call, not the first morning's scene)
        game.Story.SetVar("VAR_SCENE_PLAYERS_HOUSE_1F", 1);

        // The lab: Elm's Master Ball, once
        var lab = Johto.Value.Area("new_bark_elms_lab_1f")!.Warps[0];
        game.Arrive("NewBark_ElmsLab", lab.X, lab.Z - 1);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Master Ball")!));
        Assert.True(game.Story.Has("FLAG_GOT_MASTER_BALL_FROM_ELM"));
        Assert.Equal(8, game.Story.Var("VAR_SCENE_ELMS_LAB"));
        game.Arrive("NewBark_ElmsLab", lab.X, lab.Z - 1);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Master Ball")!));

        // Victory Road: the rival is off the floor until his scene, battles once and goes on ahead
        game.Arrive("IndigoVictoryRoad_3F", 38, 25);
        Assert.Null(game.Present("d43r0103_gsrivel"));
        Assert.True(game.Fires("Rival"));
        var road = game.Step("Rival");
        Assert.Contains(road.Log, l => l.StartsWith($"battle {rival} Won"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_40C5"));
        Assert.False(game.Fires("Rival"));
        game.Arrive("IndigoVictoryRoad_3F", 38, 25);
        Assert.Null(game.Present("d43r0103_gsrivel"));

        // The hall puts the League back whenever it is come into
        game.Story.Set("FLAG_HIDE_WILLS_ROOM_EXIT");
        game.Story.SetVar("VAR_UNK_40C7", 1);
        game.Arrive("IndigoLeague_PokemonCenter_1F", 11, 24);
        Assert.False(game.Story.Has("FLAG_HIDE_WILLS_ROOM_EXIT"));
        Assert.Equal(0, game.Story.Var("VAR_UNK_40C7"));
        foreach (var (room, who, id, flag, next) in new[]
                 {
                     ("IndigoLeague_WillRoom", "t10r0201_gsbigfour1", "johto_elite_four_will_will", "FLAG_HIDE_WILLS_ROOM_EXIT", "IndigoLeague_KogaRoom"),
                     ("IndigoLeague_KogaRoom", "t10r0301_gsbigfour2", "johto_elite_four_koga_koga", "FLAG_HIDE_KOGAS_ROOM_EXIT", "IndigoLeague_BrunoRoom"),
                     ("IndigoLeague_BrunoRoom", "t10r0401_gsbigfour3", "johto_elite_four_bruno_bruno", "FLAG_HIDE_BRUNOS_ROOM_EXIT", "IndigoLeague_KarenRoom"),
                     ("IndigoLeague_KarenRoom", "t10r0501_gsbigfour4", "johto_elite_four_karen_karen", "FLAG_HIDE_KARENS_ROOM_EXIT", "IndigoLeague_LanceRoom")
                 })
        {
            game.Through(room);
            var ahead = game.Map.Warps.Where(w => w.TargetMap == next).ToList();
            Assert.NotEmpty(ahead);
            // The door ahead is shut until the master of the room is beaten, and there is no way back to the hall
            Assert.All(ahead, w => Assert.True(game.Map.IsSolid(w.SourceX, w.SourceY), room));
            Assert.DoesNotContain(game.Map.Warps, w => w.TargetMap == "IndigoLeague_PokemonCenter_1F");
            var battle = game.Talk(who);
            Assert.Contains(battle.Log, l => l.StartsWith($"battle {id} Won"));
            Assert.True(game.Story.Has(flag));
            Assert.All(ahead, w => Assert.False(game.Map.IsSolid(w.SourceX, w.SourceY), room));
            Assert.DoesNotContain(game.Talk(who).Log, l => l.StartsWith("battle"));
        }

        // The Champion's room: Lance on the dais, his battle, Mary and Oak, and on through the door he opens
        game.Through("IndigoLeague_LanceRoom");
        Assert.DoesNotContain(game.Map.Warps, w => w.TargetMap == "IndigoLeague_KarenRoom");
        Assert.Null(game.Present("t10r0601_ookido"));
        Assert.True(game.Fires("Lance"));
        var lance = game.Step("Lance");
        Assert.Contains(lance.Log, l => l.StartsWith("battle johto_champion_lance Won"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_40C7"));
        Assert.True(game.Story.Has("FLAG_HIDE_LANCES_ROOM_EXIT"));
        Assert.True(game.Story.Has("FLAG_LANCES_ROOM_VISITORS"));

        // The Hall of Fame: the team recorded, Johto's story ended, the credits, and home
        var hall = game.Played.Single(p => p.Script.Contains("johto_pokemon_league_hall_of_fame") && p.Script.EndsWith("OnEnter")).Host;
        Assert.Contains(hall.Log, l => l.StartsWith("halloffame"));
        Assert.Contains(hall.Log, l => l == "open Credits");
        Assert.True(game.Story.Has(RegionDatabase.Get(RegionDatabase.Johto)!.StoryCompleteFlag));
        Assert.Equal("NewBark_PlayersHouse_2F", game.Map.Name);
        Assert.Equal(TravelCheck.Ready, RegionDatabase.CheckTravel(RegionDatabase.LinkFrom(RegionDatabase.Johto)!, game.Story));

        // Downstairs Elm rings, once
        game.Through("NewBark_PlayersHouse_1F");
        Assert.True(game.Story.Has("FLAG_ELM_CALLED_THE_CHAMPION"));
        int plays = game.Played.Count;
        game.Through("NewBark_PlayersHouse_2F");
        game.Through("NewBark_PlayersHouse_1F");
        Assert.DoesNotContain(game.Played.Skip(plays), p => p.Host.Transcript.Any(t => t.Text.Contains("Brrrring")));

        // The gate's door to Route 22 lets the Champion through and turns nobody back again
        game.Arrive("Route22_LeagueReceptionGate", 11, 22);
        Assert.True(game.Fires("KantoShut"));
        var gate = game.Step("KantoShut");
        Assert.DoesNotContain(gate.Log, l => l.StartsWith("walk player"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_4121"));
        Assert.False(game.Fires("KantoShut"));
    }

    [Fact]
    public void TheLeaguesDoorsStayShutToWhoeverHasNotBeatenTheRoomsMaster()
    {
        // Plan 20 · J12: a battle lost in Will's room leaves his door shut, and the hall's arrival puts it all back
        var game = PastTheTakeover();
        game.Story.GiveBadge("Johto", "Rising");
        game.Fight = BattleOutcome.Lost;
        game.Arrive("IndigoLeague_PokemonCenter_1F", 11, 24);
        game.Through("IndigoLeague_WillRoom");
        game.Talk("t10r0201_gsbigfour1");
        Assert.False(game.Story.Has("FLAG_HIDE_WILLS_ROOM_EXIT"));
        Assert.All(game.Map.Warps.Where(w => w.TargetMap == "IndigoLeague_KogaRoom"), w => Assert.True(game.Map.IsSolid(w.SourceX, w.SourceY)));
        // and the Hall of Fame's flag is not had for the asking
        Assert.False(game.Story.Has("JohtoHallOfFame"));
    }

    [Fact]
    public void EveryAnswerRightGivesADratiniThatKnowsExtremeSpeedAndClairGivesTheTMInHerGym()
    {
        // Plan 20 · J11, part c: the five answers right at once; the city sends Clair home from the Den if she wasn't
        // met there, and she gives TM59 in her Gym instead
        var game = PastTheTakeover();
        game.Story.Set("FLAG_UNK_0D1");
        foreach (int answer in new[] { 0, 0, 1, 0, 2 }) game.Answers.Enqueue(answer);
        game.Arrive("DragonsDen_Shrine", 6, 19);
        Assert.False(game.Story.Has("FLAG_FAILED_DRAGONS_DEN_QUIZ"));
        Assert.True(game.Story.HasBadge("Johto", "Rising"));
        Assert.Equal(1, game.Story.Var("VAR_UNK_40C4"));

        var town = Johto.Value.Area("blackthorn")!.Warps[0];
        game.Arrive("Johto", town.X, town.Z + 1);
        Assert.Equal(2, game.Story.Var("VAR_UNK_40C4"));
        Assert.True(game.Story.Has("FLAG_HIDE_VICTORY_ROAD_CLAIR"));
        game.Arrive("DragonsDen", 28, 12);
        Assert.Null(game.Present("d44r0102_gsleader8"));
        Assert.False(game.Fires("Clair"));

        game.Arrive("Blackthorn_Gym", 13, 86);
        game.Talk("t30gym0101_gsleader8");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM59")!));
        Assert.True(game.Story.Has("FLAG_GOT_TM59_FROM_CLAIR"));

        game.Arrive("DragonsDen_Shrine", 6, 11);
        game.Talk("d44r0103_chourou");
        var dratini = Assert.Single(game.Party.Members, p => p.Species.Name == "Dratini");
        Assert.Contains(dratini.Moves, m => m.Name == "Extreme Speed");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TheDragonsDenAfterTheLeagueHasItsRivalALanceAndClairDoubleBattleAndClairsNumber(int starter)
    {
        // Plan 20 · J13, part c: HeartGold's post-game in the Den, begun by the first arrival after the Hall of Fame
        var game = new Game(starter);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machop")!, 60));
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        game.Story.ChooseStarter(johto.Starters[starter]);
        int variant = johto.Starters[starter] switch { "Chikorita" => 2, "Cyndaquil" => 3, _ => 1 };

        // Before the League nobody waits there and the row before the shrine is plain
        game.Arrive("DragonsDen", 28, 12);
        Assert.Equal(0, game.Story.Var("VAR_UNK_40FC"));
        Assert.Null(game.Present("d44r0102_gsrivel_2"));
        Assert.Null(game.Present("d44r0102_gsrivel"));
        Assert.False(game.Fires("Rival"));

        // After it, and once his battle in Mt. Moon is behind him (plan 20 · J15, part f: VAR_UNK_40FC 1), the boy waits on
        // the island's bank, and the first step onto the row brings Lance and Clair
        game.Story.Set("JohtoHallOfFame");
        game.Arrive("DragonsDen", 28, 12);
        Assert.Equal(0, game.Story.Var("VAR_UNK_40FC"));
        Assert.Null(game.Present("d44r0102_gsrivel_2"));
        game.Story.SetVar("VAR_UNK_40FC", 1);
        game.Arrive("DragonsDen", 28, 12);
        Assert.Equal(1, game.Story.Var("VAR_UNK_40FC"));
        Assert.NotNull(game.Present("d44r0102_gsrivel_2"));
        Assert.Null(game.Present("d44r0102_wataru"));
        Assert.True(game.Fires("Rival"));
        var scene = game.Step("Rival");
        Assert.Contains($"battle johto_champion_lance_3 and johto_leader_clair_clair_3 with johto_partner_rival_{variant} Won", scene.Log);
        Assert.Equal(2, game.Story.Var("VAR_UNK_40FC"));
        Assert.False(game.Fires("Rival"));
        Assert.Null(game.Present("d44r0102_wataru"));
        Assert.Null(game.Present("d44r0102_gsleader8_2"));
        Assert.Null(game.Present("d44r0102_gsrivel_2"));

        // From then on: the boy trains here on Tuesdays and Thursdays, not on a Monday
        Assert.Null(game.Present("d44r0102_gsrivel"));
        game.Weekday = DayOfWeek.Tuesday;
        game.Arrive("DragonsDen", 28, 12);
        Assert.NotNull(game.Present("d44r0102_gsrivel"));
        Assert.Contains(game.Talk("d44r0102_gsrivel").Transcript, l => l.Text.Contains("training my Pokémon"));
        Assert.Contains(game.Talk("d44r0102_gsrivel").Transcript, l => l.Text.Contains("stay out of my way"));

        // Clair walks the island on her mornings, and her number goes into the Pokégear if the player asks for it
        game.Time = TimeOfDay.Day;
        game.Arrive("DragonsDen", 28, 12);
        Assert.Null(game.Present("d44r0102_gsleader8"));
        game.Time = TimeOfDay.Morning;
        game.Arrive("DragonsDen", 28, 12);
        Assert.NotNull(game.Present("d44r0102_gsleader8"));
        game.Answers.Enqueue(1);
        game.Talk("d44r0102_gsleader8");
        Assert.False(game.Story.Has(Pokegear.ContactFlag("clair")));
        game.Talk("d44r0102_gsleader8");
        Assert.True(game.Story.Has(Pokegear.ContactFlag("clair")));
        Assert.True(game.Story.Has("FLAG_HIDE_VICTORY_ROAD_CLAIR"));
        // Registered, she is there in the afternoons and not the mornings
        game.Arrive("DragonsDen", 28, 12);
        Assert.Null(game.Present("d44r0102_gsleader8"));
        game.Time = TimeOfDay.Day;
        game.Arrive("DragonsDen", 28, 12);
        Assert.NotNull(game.Present("d44r0102_gsleader8"));
        // Plan 20 · J13: registered, she offers a photograph together (HeartGold's CameronPhoto); a no takes none
        game.Answers.Enqueue(1);
        var refused = game.Talk("d44r0102_gsleader8");
        int before = refused.Photographs;
        game.Answers.Enqueue(0);
        var taken = game.Talk("d44r0102_gsleader8");
        Assert.Equal(before + 1, taken.Photographs);
        Assert.Contains(taken.Transcript, l => l.Text.Contains("Keep it somewhere safe"));
    }

    [Fact]
    public void RedIsFoughtAtMtSilversSummitBySixteenBadgesAndComesBackWithTheHallOfFame()
    {
        // Plan 20 · J15: a Champion of Johto past the middle guard, at the line before Route 28's door
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 70));
        game.Story.Set(RegionDatabase.Get(RegionDatabase.Johto)!.StoryCompleteFlag);
        game.Story.SetBadges(RegionDatabase.Johto, 0xFF);
        game.Story.SetVar("VAR_UNK_4110", 1);
        game.Arrive("Route22_LeagueReceptionGate", 11, 12);
        Assert.True(game.Fires("MtSilverCheck"));

        // Johto's eight alone are turned back: Kanto's eight are wanted too, sixteen in all
        var shut = game.Step("MtSilverCheck");
        Assert.Equal((9, 8), shut.PlayerTile);
        Assert.Equal(1, game.Story.Var("VAR_UNK_4110"));
        Assert.True(game.Fires("MtSilverCheck"));

        // ... and so is a trainer with Kanto's who is no Champion of Johto
        game.Story.SetBadges(RegionDatabase.Kanto, 0xFF);
        game.Story.Unset(RegionDatabase.Get(RegionDatabase.Johto)!.StoryCompleteFlag);
        Assert.Equal((9, 8), game.Step("MtSilverCheck").PlayerTile);
        game.Story.Set(RegionDatabase.Get(RegionDatabase.Johto)!.StoryCompleteFlag);
        var through = game.Step("MtSilverCheck");
        Assert.Equal((8, 8), through.PlayerTile);
        Assert.Equal(2, game.Story.Var("VAR_UNK_4110"));
        Assert.False(game.Fires("MtSilverCheck"));
        // the middle guard still knows the player, and the door to Route 28 is open
        Assert.Contains(game.Talk("r22r0101_policeman").Transcript, l => l.Text.Contains("all eight Badges"));
        var door = game.Map.Warps.Single(w => w.TargetMap == "Johto" && w.SourceX < 5);
        Assert.False(game.Map.IsSolid(door.SourceX, door.SourceY));

        // Route 28's house: TM47 once
        game.Tile = (door.SourceX, door.SourceY);
        game.Through("Johto");
        game.Through("Route28_House");
        game.Talk("r28r0101_gsgirl1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM47")!));
        game.Talk("r28r0101_gsgirl1");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM47")!));
        game.Through("Johto");

        // Up the mountain: the cave's first floor, the second, the upper mountainside, the third and the summit
        game.Through("MtSilverCave_1F");
        game.Through("MtSilverCave_2F");
        game.Through("MtSilverCave_UpperMountainside");
        game.Through("MtSilverCave_3F");
        game.Through("MtSilverCave_Summit");
        Assert.NotNull(game.Present("d41r0108_red"));

        // A battle lost leaves him waiting
        game.Fight = BattleOutcome.Lost;
        Assert.Contains(game.Talk("d41r0108_red").Log, l => l.StartsWith("battle johto_pkmn_trainer_red_red Lost"));
        game.Arrive("MtSilverCave_Summit", 30, 30);
        Assert.NotNull(game.Present("d41r0108_red"));
        Assert.False(game.Story.Has("FLAG_JOHTO_BEAT_RED"));

        // Beaten, he is gone, until the Hall of Fame is entered again
        game.Fight = BattleOutcome.Won;
        Assert.Contains(game.Talk("d41r0108_red").Log, l => l.StartsWith("battle johto_pkmn_trainer_red_red Won"));
        Assert.True(game.Story.Has("FLAG_JOHTO_BEAT_RED"));
        Assert.Null(game.Present("d41r0108_red"));
        game.Arrive("MtSilverCave_Summit", 30, 30);
        Assert.Null(game.Present("d41r0108_red"));
        var hall = Scripts.In("johto_pokemon_league_hall_of_fame", ScriptLibrary.OnEnter)!;
        Assert.Contains(hall.Code, i => i.Op == Op.ClearFlag && i.Name == "FLAG_UNK_25F");
        game.Story.Unset("FLAG_UNK_25F");
        game.Arrive("MtSilverCave_Summit", 30, 30);
        Assert.NotNull(game.Present("d41r0108_red"));

        // Moltres waits in its chamber until it is fought; run from, it is back on the next visit
        game.Arrive("MtSilverCave_MoltresChamber", 10, 20);
        Assert.NotNull(game.Present("d41r0105_tsure_poke_static_moltres"));
        game.Fight = BattleOutcome.Fled;
        game.Talk("d41r0105_tsure_poke_static_moltres");
        Assert.Null(game.Present("d41r0105_tsure_poke_static_moltres"));
        game.Arrive("MtSilverCave_MoltresChamber", 10, 20);
        Assert.NotNull(game.Present("d41r0105_tsure_poke_static_moltres"));
        game.Fight = BattleOutcome.Won;
        game.Talk("d41r0105_tsure_poke_static_moltres");
        game.Arrive("MtSilverCave_MoltresChamber", 10, 20);
        Assert.Null(game.Present("d41r0105_tsure_poke_static_moltres"));
    }

    [Fact]
    public void TheSSAquaSailsAChampionToVermilionWhereLtSurgeGivesKantosThunderBadge()
    {
        // Plan 20 · J15, part b: Olivine's port hall, its sailor before the quay
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Quagsire")!, 60));
        const string Sailor = "ss_aqua_olivine_port_interior.Sailor";
        game.Arrive("Olivine_PortHall", 7, 3);
        Assert.Equal("SSAqua_OlivinePort", game.Map.GetWarpAt(7, 11)?.TargetMap);

        // No passage before the Hall of Fame
        Assert.Contains(game.Talk(Sailor).Transcript, l => l.Text.Contains("Champion"));
        Assert.False(game.Story.Has("FLAG_JOHTO_SS_AQUA_PASSAGE"));

        // A Champion bound for Vermilion: he steps aside, and stays aside on every visit after
        game.Story.Set(RegionDatabase.Get(RegionDatabase.Johto)!.StoryCompleteFlag);
        game.Answers.Enqueue(0);
        game.Talk(Sailor);
        Assert.True(game.Story.Has("FLAG_JOHTO_SS_AQUA_PASSAGE"));
        game.Arrive("Olivine_PortHall", 7, 3);
        Assert.Equal((8, 10), (game.Present(Sailor)!.GridX, game.Present(Sailor)!.GridY));

        // The quay, and aboard: the gangway leads back to Olivine until the ship has crossed
        game.Arrive("SSAqua_OlivinePort", 16, 16);
        game.Through("SSAqua_1F");
        Assert.Equal("SSAqua_OlivinePort", game.Map.GetWarpAt(24, 19)?.TargetMap);
        game.Answers.Enqueue(1);
        game.Talk("p01r0301_seaman_2_2");
        Assert.False(game.Story.Has("FLAG_JOHTO_SS_AQUA_AT_VERMILION"));
        game.Answers.Enqueue(0);
        game.Talk("p01r0301_seaman_2_2");
        Assert.True(game.Story.Has("FLAG_JOHTO_SS_AQUA_AT_VERMILION"));
        Assert.Equal("SSAqua_VermilionPort", game.Map.GetWarpAt(24, 19)?.TargetMap);

        // Down the gangway to Vermilion's quay, its port hall (its sailor aside already) and the city
        game.Tile = (24, 19);
        game.Through("SSAqua_VermilionPort");
        game.Through("Vermilion_PortHall");
        Assert.Equal(8, game.Present("p01r0103_seaman_2")!.GridX);
        game.Through("Johto");
        Assert.Equal("vermilion", game.Map.AreaAt(game.Tile.X, game.Tile.Y)?.Key);

        // The Gym: the bins' two switches take the gates down
        game.Arrive("Vermilion_Gym", 6, 21);
        Assert.NotNull(game.Present("t06gym0101_stop"));
        int first = game.Story.Var("VAR_JOHTO_VERMILION_GYM_SWITCH_1"), second = game.Story.Var("VAR_JOHTO_VERMILION_GYM_SWITCH_2");
        Assert.InRange(first, 1, 15);
        Assert.NotEqual(first, second);
        (int X, int Y) Bin(int n) => (2 + 2 * ((n - 1) % 5), 13 + 2 * ((n - 1) / 5));
        game.Read(Bin(first).X, Bin(first).Y);
        Assert.False(game.Story.Has("FLAG_HIDE_VERMILION_GYM_STOPS"));
        game.Read(Bin(second).X, Bin(second).Y);
        Assert.True(game.Story.Has("FLAG_HIDE_VERMILION_GYM_STOPS"));
        Assert.Null(game.Present("t06gym0101_stop"));

        // Lt. Surge: Kanto's Thunder Badge, not Johto's, and TM57 once
        int johtoBadges = game.Story.BadgeCount(RegionDatabase.Johto);
        Assert.Contains(game.Talk("t06gym0101_gsleader9").Log, l => l.StartsWith("battle johto_leader_lt_surge_lt__surge Won"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Kanto, "Thunder"));
        Assert.Equal(johtoBadges, game.Story.BadgeCount(RegionDatabase.Johto));
        Assert.True(game.Story.HasDefeated("johto_juggler_horton"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM57")!));
        game.Talk("t06gym0101_gsleader9");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM57")!));

        // And home again: the crewman sails back to Olivine
        game.Arrive("SSAqua_1F", 24, 18);
        game.Answers.Enqueue(0);
        game.Talk("p01r0301_seaman_2_2");
        Assert.False(game.Story.Has("FLAG_JOHTO_SS_AQUA_AT_VERMILION"));
        Assert.Equal("SSAqua_OlivinePort", game.Map.GetWarpAt(24, 19)?.TargetMap);
    }

    [Fact]
    public void TheMagnetTrainTakesAChampionToSaffronWhereSabrinaGivesKantosMarshBadge()
    {
        // Plan 20 · J15, part c: Goldenrod's station, its officer at the gate to the platform
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Tyranitar")!, 60));
        const string Officer = "t25r0501_policeman";
        game.Arrive("Goldenrod_MagnetTrainStation", 6, 16);

        // No ride before the Hall of Fame
        Assert.Contains(game.Talk(Officer).Transcript, l => l.Text.Contains("Champion"));
        Assert.Equal("Goldenrod_MagnetTrainStation", game.Map.Name);

        // A Champion who says no stays; one who says yes rides to Saffron
        game.Story.Set(RegionDatabase.Get(RegionDatabase.Johto)!.StoryCompleteFlag);
        game.Answers.Enqueue(1);
        game.Talk(Officer);
        Assert.Equal("Goldenrod_MagnetTrainStation", game.Map.Name);
        game.Answers.Enqueue(0);
        game.Talk(Officer);
        Assert.Equal("Saffron_MagnetTrainStation", game.Map.Name);
        Assert.Equal((14, 10), game.Tile);

        // Out into the city, and the Gym
        game.Through("Johto");
        Assert.Equal("saffron", game.Map.AreaAt(game.Tile.X, game.Tile.Y)?.Key);
        game.Arrive("Saffron_Gym", 15, 27);
        Assert.Contains(game.Read(13, 23).Transcript, l => !l.Text.Contains("Winning"));

        // Sabrina: Kanto's Marsh Badge, not Johto's, her trainers counted beaten, and TM48 once
        int johtoBadges = game.Story.BadgeCount(RegionDatabase.Johto);
        Assert.Contains(game.Talk("t11gym0101_gsleader10").Log, l => l.StartsWith("battle johto_leader_sabrina_sabrina Won"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Kanto, "Marsh"));
        Assert.Equal(johtoBadges, game.Story.BadgeCount(RegionDatabase.Johto));
        Assert.True(game.Story.HasDefeated("johto_medium_darcy"));
        Assert.True(game.Story.HasDefeated("johto_psychic_m_franklin"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM48")!));
        game.Talk("t11gym0101_gsleader10");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM48")!));
        Assert.Contains(game.Read(13, 23).Transcript, l => l.Text.Contains("Winning"));

        // Mr. Psychic's TM29, once
        game.Arrive("Saffron_MrPsychicHouse", 4, 7);
        game.Talk("t11r0501_fishing_2");
        game.Talk("t11r0501_fishing_2");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM29")!));

        // And back to Goldenrod by the train
        game.Arrive("Saffron_MagnetTrainStation", 14, 16);
        game.Answers.Enqueue(0);
        game.Talk("t11r0601_policeman");
        Assert.Equal("Goldenrod_MagnetTrainStation", game.Map.Name);
        Assert.Equal((6, 9), game.Tile);
    }

    [Fact]
    public void ErikaGivesKantosRainbowBadgeInCeladonsGardenGym()
    {
        // Plan 20 · J15, part d: the Celadon Gym, come to by Route 7 from Saffron City
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Arrive("Celadon_Gym", 7, 45);
        Assert.Contains(game.Read(5, 44).Transcript, l => !l.Text.Contains("Winning"));

        // Erika: Kanto's Rainbow Badge, not Johto's, her four trainers counted beaten, and TM19 once
        int johtoBadges = game.Story.BadgeCount(RegionDatabase.Johto);
        Assert.Contains(game.Talk("t07gym0101_gsleader12").Log, l => l.StartsWith("battle johto_leader_erika_erika Won"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Kanto, "Rainbow"));
        Assert.Equal(johtoBadges, game.Story.BadgeCount(RegionDatabase.Johto));
        foreach (string trainer in new[] { "johto_twins_jo_and_zoe", "johto_lass_michelle", "johto_picnicker_tanya", "johto_beauty_julia" })
            Assert.True(game.Story.HasDefeated(trainer), trainer);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM19")!));
        game.Talk("t07gym0101_gsleader12");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM19")!));
        Assert.Contains(game.Read(9, 44).Transcript, l => l.Text.Contains("Winning"));
    }

    [Fact]
    public void MistyIsFetchedFromTheCapeAndGivesKantosCascadeBadgeInCeruleansGym()
    {
        // Plan 20 · J15, part e: the Cerulean Gym, come to by Route 5 from Saffron City, stands without its Leader
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Typhlosion")!, 60));
        game.Arrive("Cerulean_Gym", 10, 24);
        Assert.Null(game.Present("t04gym0101_gsleader11"));
        Assert.NotNull(game.Present("t04gym0101_gsswimmerw"));
        Assert.Contains(game.Talk("t04gym0101_sunglasses").Transcript, l => l.Text.Contains("Route 25"));

        // Misty on Route 25's cape: the date spoiled by a step onto the cape's way in, once
        game.Arrive("Johto", 1416, 41);
        Assert.NotNull(game.Present("r25_gsleader11"));
        Assert.True(game.Fires("MistysDate"));
        game.Step("MistysDate");
        Assert.True(game.Story.Has("FLAG_JOHTO_MISTY_BACK_IN_GYM"));
        Assert.Null(game.Present("r25_gsleader11"));
        Assert.Null(game.Present("r25_gsman1"));
        Assert.False(game.Fires("MistysDate"));

        // Misty at home: Kanto's Cascade Badge, not Johto's, her five trainers counted beaten, and TM55 once
        game.Arrive("Cerulean_Gym", 10, 24);
        Assert.NotNull(game.Present("t04gym0101_gsleader11"));
        Assert.DoesNotContain(game.Read(8, 21).Transcript, l => l.Text.Contains("Winning"));
        int johtoBadges = game.Story.BadgeCount(RegionDatabase.Johto);
        Assert.Contains(game.Talk("t04gym0101_gsleader11").Log, l => l.StartsWith("battle johto_leader_misty_misty Won"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Kanto, "Cascade"));
        Assert.Equal(johtoBadges, game.Story.BadgeCount(RegionDatabase.Johto));
        foreach (string trainer in new[] { "johto_swimmer_f_diana", "johto_swimmer_f_joy", "johto_swimmer_f_briana", "johto_sailor_eddie", "johto_sailor_parker" })
            Assert.True(game.Story.HasDefeated(trainer), trainer);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM55")!));
        game.Talk("t04gym0101_gsleader11");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM55")!));
        Assert.Contains(game.Read(12, 21).Transcript, l => l.Text.Contains("Winning"));
    }

    [Fact]
    public void BrockGivesKantosBoulderBadgeInPewtersGym()
    {
        // Plan 20 · J15, part f: Brock at the head of his Gym, come to by Route 2 or Mt. Moon
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Feraligatr")!, 60));
        game.Arrive("Pewter_Gym", 7, 26);
        Assert.NotNull(game.Present("t03gym0101_gsleader14"));
        Assert.NotNull(game.Present("t03gym0101_campboy_2"));
        Assert.DoesNotContain(game.Read(4, 25).Transcript, l => l.Text.Contains("Winning"));
        Assert.Contains(game.Talk("t03gym0101_sunglasses").Transcript, l => l.Text.Contains("Rock"));

        // Kanto's Boulder Badge, not Johto's, his two trainers counted beaten, and TM80 once
        int johtoBadges = game.Story.BadgeCount(RegionDatabase.Johto);
        Assert.Contains(game.Talk("t03gym0101_gsleader14").Log, l => l.StartsWith("battle johto_leader_brock_brock Won"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Kanto, "Boulder"));
        Assert.Equal(johtoBadges, game.Story.BadgeCount(RegionDatabase.Johto));
        foreach (string trainer in new[] { "johto_camper_jerry", "johto_hiker_edwin" })
            Assert.True(game.Story.HasDefeated(trainer), trainer);
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM80")!));
        game.Talk("t03gym0101_gsleader14");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM80")!));
        Assert.Contains(game.Read(10, 25).Transcript, l => l.Text.Contains("Winning"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TheRivalWaitsInMtMoonAfterTheLeagueAndHisBattleBeginsTheDragonsDensPostGame(int starter)
    {
        // Plan 20 · J15, part f: HeartGold's battle in Mt. Moon, which the Dragon's Den's post-game (J13, part c) waits for
        var game = new Game(starter);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Machop")!, 60));
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        game.Story.ChooseStarter(johto.Starters[starter]);
        int team = johto.Starters[starter] switch { "Chikorita" => 15, "Cyndaquil" => 16, _ => 14 };

        // Before the League nobody waits in the cave
        game.Arrive("MountMoon", 5, 4);
        Assert.Null(game.Present("d02r0101_gsrivel"));

        // After it the boy waits by the western mouth, and the Den waits for him
        game.Story.Set("JohtoHallOfFame");
        game.Arrive("DragonsDen", 28, 12);
        Assert.Equal(0, game.Story.Var("VAR_UNK_40FC"));
        Assert.Null(game.Present("d44r0102_gsrivel_2"));
        game.Arrive("MountMoon", 5, 4);
        Assert.NotNull(game.Present("d02r0101_gsrivel"));
        var scene = game.Talk("d02r0101_gsrivel");
        Assert.Contains($"battle johto_rival_silver_{team} Won", scene.Log);
        Assert.Equal(1, game.Story.Var("VAR_UNK_40FC"));
        Assert.Null(game.Present("d02r0101_gsrivel"));
        game.Arrive("MountMoon", 5, 4);
        Assert.Null(game.Present("d02r0101_gsrivel"));

        // and now the Den's post-game begins
        game.Arrive("DragonsDen", 28, 12);
        Assert.NotNull(game.Present("d44r0102_gsrivel_2"));
    }
    [Fact]
    public void BlaineGivesKantosVolcanoBadgeInHisGymInTheSeafoamIslands()
    {
        // Plan 20 · J15, part g: Blaine at the head of his Gym in the Seafoam Islands, come to by Route 20 from Cinnabar
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Feraligatr")!, 60));
        game.Arrive("Seafoam_Gym", 14, 24);
        Assert.NotNull(game.Present("d11r0106_gsleader15"));
        Assert.Contains(game.Talk("d11r0106_sunglasses").Transcript, l => l.Text.Contains("Water"));

        // Kanto's Volcano Badge, not Johto's, and TM50 once
        int johtoBadges = game.Story.BadgeCount(RegionDatabase.Johto);
        Assert.Contains(game.Talk("d11r0106_gsleader15").Log, l => l.StartsWith("battle johto_leader_blaine_blaine Won"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Kanto, "Volcano"));
        Assert.Equal(johtoBadges, game.Story.BadgeCount(RegionDatabase.Johto));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM50")!));
        game.Talk("d11r0106_gsleader15");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM50")!));
        Assert.Contains(game.Talk("d11r0106_sunglasses").Transcript, l => l.Text.Contains("You beat Blaine"));
    }

    [Fact]
    public void BlueOnCinnabarAsksForKantosOtherSevenBadges()
    {
        // Plan 20 · J15, part g: Blue by the ruins of Cinnabar; with Kanto's other seven Badges he goes home to Viridian
        // (part h)
        var game = new Game();
        game.Arrive("Johto", 1037, 494);
        Assert.NotNull(game.Present("t09_gsleader16"));
        Assert.Contains(game.Talk("t09_gsleader16").Transcript, l => l.Text.Contains("other seven Badges"));
        Assert.False(game.Story.Has("FLAG_JOHTO_BLUE_BACK_IN_GYM"));
        foreach (string badge in new[] { "Boulder", "Cascade", "Thunder", "Rainbow", "Soul", "Marsh", "Volcano" })
            game.Story.GiveBadge(RegionDatabase.Kanto, badge);
        var talk = game.Talk("t09_gsleader16");
        Assert.Contains(talk.Transcript, l => l.Text.Contains("heading back to Viridian"));
        Assert.DoesNotContain(talk.Log, l => l.StartsWith("battle"));
        Assert.True(game.Story.Has("FLAG_JOHTO_BLUE_BACK_IN_GYM"));
        game.Arrive("Johto", 1037, 494);
        Assert.Null(game.Present("t09_gsleader16"));
    }

    [Fact]
    public void JaninesDecoysDropTheirDisguisesAndSheGivesKantosSoulBadge()
    {
        // Plan 20 · J15, part h: the Fuchsia Gym's four decoys dressed as Janine, and Janine in the middle of the floor
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 70));
        game.Arrive("Fuchsia_Gym", 7, 23);
        Assert.NotNull(game.Present("t08gym0101_gsleader13_4"));
        Assert.Null(game.Present("t08gym0101_gsgirl1"));
        Assert.Contains(game.Talk("t08gym0101_sunglasses").Transcript, l => l.Text.Contains("dress up as her"));

        // A decoy drops the disguise, and the trainer under it battles
        var decoy = game.Talk("t08gym0101_gsleader13_4");
        Assert.Contains(decoy.Log, l => l.StartsWith("battle johto_lass_linda Won"));
        Assert.True(game.Story.Has("FLAG_JOHTO_FUCHSIA_GYM_LINDA_REVEALED"));
        Assert.True(game.Story.HasDefeated("johto_lass_linda"));
        game.Arrive("Fuchsia_Gym", 7, 23);
        Assert.Null(game.Present("t08gym0101_gsleader13_4"));
        Assert.NotNull(game.Present("t08gym0101_gsgirl1"));
        Assert.DoesNotContain(game.Talk("t08gym0101_gsgirl1").Log, l => l.StartsWith("battle"));

        // Kanto's Soul Badge, not Johto's, and TM84 once; the other three decoys are unmasked
        int johtoBadges = game.Story.BadgeCount(RegionDatabase.Johto);
        Assert.Contains(game.Talk("t08gym0101_gsleader13").Log, l => l.StartsWith("battle johto_leader_janine_janine Won"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Kanto, "Soul"));
        Assert.Equal(johtoBadges, game.Story.BadgeCount(RegionDatabase.Johto));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM84")!));
        game.Talk("t08gym0101_gsleader13");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM84")!));
        Assert.True(game.Story.HasDefeated("johto_picnicker_cindy"));
        game.Arrive("Fuchsia_Gym", 7, 23);
        Assert.Null(game.Present("t08gym0101_gsleader13_2"));
        Assert.NotNull(game.Present("t08gym0101_picnicgirl_2"));
        Assert.Contains(game.Talk("t08gym0101_sunglasses").Transcript, l => l.Text.Contains("You beat Janine"));
    }

    [Fact]
    public void BlueComesHomeToViridiansGymAndGivesKantosEarthBadge()
    {
        // Plan 20 · J15, part h: the Viridian Gym, open once Blue is home, and Blue at the far end of its hall
        var game = new Game();
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 70));
        game.Story.Set("FLAG_JOHTO_BLUE_BACK_IN_GYM");
        game.Arrive("Viridian_Gym", 5, 46);
        Assert.NotNull(game.Present("t02gym0101_gsleader16"));
        Assert.Contains(game.Talk("t02gym0101_sunglasses").Transcript, l => l.Text.Contains("one type"));

        // Kanto's Earth Badge, not Johto's, and TM92 once
        int johtoBadges = game.Story.BadgeCount(RegionDatabase.Johto);
        Assert.Contains(game.Talk("t02gym0101_gsleader16").Log, l => l.StartsWith("battle johto_leader_blue_blue Won"));
        Assert.True(game.Story.HasBadge(RegionDatabase.Kanto, "Earth"));
        Assert.Equal(johtoBadges, game.Story.BadgeCount(RegionDatabase.Johto));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM92")!));
        game.Talk("t02gym0101_gsleader16");
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM92")!));
        Assert.True(game.Story.HasDefeated("johto_double_team_elan_and_ida"));
        Assert.Contains(game.Talk("t02gym0101_sunglasses").Transcript, l => l.Text.Contains("You beat Blue"));
    }

    [Fact]
    public void TheCyclingRoadsGatesTurnBackWhoeverHasNoBicycleAndTheRoadIsRidden()
    {
        // Plan 20 · J15, part i: Route 16's gate between Celadon City's side and the Cycling Road, and Route 18's
        foreach (string gate in new[] { "Route16_CyclingGate", "Route18_CyclingGate" })
        {
            var game = new Game();
            game.Arrive(gate, 10, 7);
            Assert.Contains(game.Step("NeedBike").Transcript, l => l.Text.Contains("Bicycles only"));
            Assert.Equal(0, game.Story.Var("VAR_JOHTO_CYCLING_ROAD"));
            game.Bag.AddItem(ItemDatabase.Get("Bicycle")!, 1);
            Assert.Empty(game.Step("NeedBike").Transcript);
            Assert.Equal(1, game.Story.Var("VAR_JOHTO_CYCLING_ROAD"));
            // On the road the rider is kept on the Bicycle until the next door
            game.Arrive("Johto", 1140, 330);
            Assert.True(game.Story.Has("FLAG_ON_CYCLING_ROAD"));
            game.Arrive(gate, 1, 7);
            game.Step("OffTheRoad");
            Assert.Equal(0, game.Story.Var("VAR_JOHTO_CYCLING_ROAD"));
        }
    }
}
