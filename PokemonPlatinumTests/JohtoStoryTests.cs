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
}
